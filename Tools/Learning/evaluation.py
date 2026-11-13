"""Real-time, isolated baseline/candidate evaluation. Invoked only by the learning supervisor/user."""

import hashlib
import os
import subprocess
import time
from pathlib import Path
from policy_io import atomic, read, stopped

PROJECT = Path(__file__).resolve().parents[2]
HOST_PROJECT = PROJECT / "Tools/Learning/RlHost/RlHost.csproj"
HOST_DLL = HOST_PROJECT.parent / "bin/Release/net8.0/RlHost.dll"
BALANCE_FILES = ("game-rules.json", "combat-rules.json", "game-balance.json", "monster-tuning.json")


def balance_hash(folder, environment=None):
    sha = hashlib.sha256()
    for name in BALANCE_FILES:
        sha.update(name.encode())
        path = Path(folder) / name
        sha.update(path.read_bytes() if path.exists() else b"<defaults>")
    environment = environment or {}
    sha.update(
        f"|ground:{environment.get('ownerSeconds', 30) * 1000}:{environment.get('lifetimeSeconds', 60) * 1000}".encode()
    )
    # must equal RlBridge.BalanceHash: the movement revision comes from the game (environment.json, TileMotion.Revision)
    sha.update(
        f"|motion:{environment.get('motionRevision', 'tile-motion-1')}:{environment.get('moveStepPx', 60)}".encode()
    )
    return sha.hexdigest()


def owned_process(command, log):
    flags = (
        (subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS)
        if os.name == "nt"
        else 0
    )
    return subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT, creationflags=flags)


def run_checked(command, root, log):
    process = owned_process(command, log)
    try:
        while process.poll() is None:
            if stopped(root):
                raise InterruptedError("learning stopped")
            budget = read(root / "budget.json", {})
            settings = read(root / "settings.json", {})
            if budget and (
                not budget.get("learning", False)
                or budget.get("fps", 0) < 60
                or not settings.get("evaluationEnabled", False)
                or abs(time.time() * 1000 - budget.get("at", 0)) > 15000
            ):
                raise InterruptedError("evaluation paused to protect the game")
            time.sleep(1)
        if process.returncode:
            raise RuntimeError(f"Evaluator exit {process.returncode}; see trainer.log")
    finally:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()  # only the exact child this supervisor started


def summarize(runs):
    by = {}
    for run in runs:
        for a in run["agents"]:
            if not a.get("online"):
                raise ValueError("An evaluator bot failed to log in")
            p = by.setdefault(
                a["name"], dict(completed=0, progress=0, deaths=0, stuck=0, exp=0, runs=0)
            )
            q = a["quest"]
            p["completed"] += int(q["id"] >= 4)
            p["progress"] += q["id"] * 100 + q["index"] * 10 + min(9, q["count"])
            p["deaths"] += a["deaths"]
            p["stuck"] += a["learning"]["stuck"]
            p["exp"] += a["exp"]
            p["runs"] += 1
    return by


def run_score(result):
    """Totals of one isolated run over its bots (the same quantities the quality gate compares)."""
    agents = result.get("agents") or []
    return dict(
        bots=len(agents),
        quests=sum(int(a["quest"]["id"] >= 4) for a in agents),
        progress=sum(
            a["quest"]["id"] * 100 + a["quest"]["index"] * 10 + min(9, a["quest"]["count"])
            for a in agents
        ),
        exp=sum(a.get("exp", 0) for a in agents),
        deaths=sum(a.get("deaths", 0) for a in agents),
        stuck=sum(a.get("learning", {}).get("stuck", 0) for a in agents),
    )


def quality_gate(baseline, candidate):
    """Gate every personality; zero baseline failures permits zero candidate failures."""
    reasons = []
    for name, b in baseline.items():
        c = candidate.get(name)
        if not c or c["runs"] != b["runs"]:
            reasons.append(name + ": missing trials")
            continue
        if c["completed"] / c["runs"] < b["completed"] / b["runs"] - 0.05:
            reasons.append(name + ": quest completion regressed")
        if c["progress"] < b["progress"] * 0.95 or c["exp"] < b["exp"] * 0.85:
            reasons.append(name + ": no useful progression")
        if c["deaths"] > b["deaths"] * 1.10 or c["stuck"] > b["stuck"] * 1.10:
            reasons.append(name + ": death/stuck regression")
    # Do not approve a model because both arms did nothing.
    if not any(b["completed"] > 0 for b in baseline.values()):
        reasons.append("Baseline did not finish quests 0-3; increase evaluation duration")
    return reasons


def evaluate(folder, root, log, seconds=600, trials=3, progress=None):
    folder, root = Path(folder), Path(root)
    run_checked(
        ["dotnet", "build", str(HOST_PROJECT), "-c", "Release", "--nologo", "--verbosity", "quiet"],
        root,
        log,
    )
    baseline, candidate = [], []
    # Same initial characters, static data and frozen balance files, real clock; independent RNG trials.
    frozen = folder / "balance"
    frozen.mkdir(exist_ok=True)
    environment = read(root / "environment.json")
    source = Path(environment["rules"])
    context_file = folder / "evaluation-context.json"
    context = read(context_file)
    if context:
        if (
            context["balanceHash"] != balance_hash(source, environment)
            or context["static"] != environment["static"]
        ):
            raise ValueError("Evaluation context changed; do not reuse old trials")
    else:
        initial_hash = balance_hash(source, environment)
        for filename in BALANCE_FILES:
            if (source / filename).exists():
                (frozen / filename).write_bytes((source / filename).read_bytes())
        if initial_hash != balance_hash(frozen, environment):
            raise ValueError("Balance changed while creating the evaluation snapshot")
        atomic(context_file, dict(balanceHash=initial_hash, static=environment["static"]))
    results = []  # per-run scores shown live on /agents.html while the evaluation runs
    for trial in range(trials):
        for arm, target in (("rules", baseline), ("candidate", candidate)):
            output = folder / f"trial-{trial}-{arm}.json"
            if progress:
                progress(len(results), trials * 2, trial, arm, seconds, results)
            if not output.exists():
                cmd = [
                    "dotnet",
                    str(HOST_DLL),
                    "evaluate",
                    "--static",
                    str(environment["static"]),
                    "--rules",
                    str(frozen),
                    "--seconds",
                    str(seconds),
                    "--output",
                    str(output),
                    "--owner-seconds",
                    str(environment.get("ownerSeconds", 30)),
                    "--lifetime-seconds",
                    str(environment.get("lifetimeSeconds", 60)),
                    "--move-step-px",
                    str(environment.get("moveStepPx", 60)),
                ]
                if arm == "candidate":
                    cmd += ["--model", str(folder)]
                run_checked(cmd, root, log)
            result = read(output)
            if not result or not result.get("agents"):
                raise ValueError("Evaluation did not produce agent metrics")
            target.append(result)
            results.append(dict(trial=trial + 1, arm=arm, **run_score(result)))
    if progress:
        progress(len(results), trials * 2, trials - 1, "done", seconds, results)
    b, c = summarize(baseline), summarize(candidate)
    reasons = quality_gate(b, c)
    if any(run["learning"].get("inferenceTickP95Ms", float("inf")) > 5 for run in candidate):
        reasons.append("13-bot policy-selection p95 exceeded 5 ms/tick")
    if balance_hash(source, environment) != balance_hash(frozen, environment):
        reasons.append("Balance changed during evaluation; evaluate a fresh candidate")
    report = dict(
        version=folder.name,
        passed=not reasons,
        reasons=reasons,
        baseline=b,
        candidate=c,
        seconds=seconds,
        trials=trials,
        balanceHash=balance_hash(frozen, environment),
        timestamp=int(time.time() * 1000),
        inferenceTickP95Ms=max(
            run["learning"].get("inferenceTickP95Ms", float("inf")) for run in candidate
        ),
        hashes={
            f.name: hashlib.sha256(f.read_bytes()).hexdigest()
            for f in folder.glob("*.json")
            if f.name in ("strategy.json", "tactical.json")
        },
    )
    atomic(folder / "evaluation.json", report)
    return report
