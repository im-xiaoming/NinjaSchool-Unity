"""Low-priority local supervisor: demonstrations -> MaskablePPO -> isolated evaluation -> immutable release.

The game never waits for this process. Run with --root <NsoLearning>. No downloads or production-save edits.
"""
import argparse
from collections import defaultdict, deque
import contextlib
import hashlib
import json
import os
from pathlib import Path
import random
import sys
import time
import traceback

from policy_io import ACTIONS, INPUTS, SCHEMA, atomic, export_policy, numpy_forward, read, stopped

# Learning capacity (user 2026-11-13: "strengthen RL learning, resources are fine, the game is light").
# Older 64-wide checkpoints are replaced by a fresh HIDDEN-wide model cloned from the teacher (then evaluated as usual).
HIDDEN = 256
SPOOL_ROWS = 65536          # experience kept in memory per domain
CLONE_ROWS = 32768          # newest teacher/human rows used for behaviour cloning
CLONE_EPOCHS = 8
CLONE_BATCH = 256
TRAIN_INTERVAL_SECONDS = 20
TORCH_THREADS = 4
HISTORY_ROUNDS = 100        # training rounds kept in training-history.json (shown on /agents.html)
VALIDATION_SHARE = .1       # teacher rows held out to measure behaviour-cloning accuracy


class Tracker:
    """Live training progress for /agents.html (trainer-status.json, read by RlBridge): phase, epoch/batch progress,
    loss/accuracy and PPO statistics, like a deep-learning training log. Writes at most once a second."""
    def __init__(self, root, status):
        self.root, self.status, self.written = root, status, 0
        self.history = read(root / "training-history.json", [])
        if not isinstance(self.history, list):
            self.history = []
        status["history"] = self.history[-HISTORY_ROUNDS:]
        status.setdefault("metrics", {})

    def write(self, force=False):
        now = time.time()
        if force or now - self.written >= 1:
            self.written = now
            self.status["at"] = int(now * 1000)
            atomic(self.root / "trainer-status.json", self.status)

    def begin(self, phase, domain, total, unit, **extra):
        self.status["progress"] = dict(phase=phase, domain=domain, done=0, total=int(total), unit=unit,
                                       startedAt=int(time.time() * 1000), **extra)
        self.write(True)

    def step(self, done, **values):
        p = self.status.get("progress")
        if p is None:
            return
        p["done"] = int(done)
        p.update(values)
        self.write()

    def end(self):
        self.status.pop("progress", None)
        self.write(True)

    def record(self, domain, kind, **values):
        """One finished training round (per domain): appended to the history and kept as the latest metrics."""
        entry = dict(at=int(time.time() * 1000), domain=domain, kind=kind, **values)
        self.status["metrics"][domain] = entry
        self.history.append(entry)
        del self.history[:-HISTORY_ROUNDS]
        self.status["history"] = self.history
        atomic(self.root / "training-history.json", self.history)
        self.write(True)


TRACK = None   # the Tracker of the running supervisor (None in tests: progress is simply not reported)


def track(method, *args, **kwargs):
    if TRACK is not None:
        getattr(TRACK, method)(*args, **kwargs)


def domain_of(rows):
    return rows[-1].get("domain", "") if rows else ""


class Spool:
    """Read only complete JSONL records. Bounded in-memory data; files remain evidence across restarts."""
    def __init__(self, root):
        self.root, self.offsets = root, {}
        self.rows = {d: deque(maxlen=SPOOL_ROWS) for d in ("strategy", "tactical")}

    def poll(self):
        for path in sorted(self.root.glob("experience-*.jsonl"), key=lambda p: p.stat().st_mtime):
            with path.open("rb") as f:
                f.seek(self.offsets.get(path.name, 0))
                # At most 2 MB per file per poll; an incomplete last line is retried next time.
                for _ in range(16384):
                    begin = f.tell()
                    line = f.readline()
                    if not line.endswith(b"\n"):
                        f.seek(begin)
                        break
                    self.offsets[path.name] = f.tell()
                    try:
                        row = json.loads(line)
                        if row.get("schema") == SCHEMA and row.get("domain") in self.rows:
                            if len(row["obs"]) == INPUTS and len(row["mask"]) == ACTIONS and row["mask"][row["action"]]:
                                self.rows[row["domain"]].append(row)
                    except (ValueError, KeyError, IndexError, TypeError):
                        continue
        # Retain up to 512 MB / 20 days, only our closed experience chunks, never checkpoints or player data.
        files = sorted(self.root.glob("experience-*.jsonl"), key=lambda p: p.stat().st_mtime)
        total = sum(p.stat().st_size for p in files)
        for p in files[:-1]:
            st = p.stat()
            if time.time() - st.st_mtime < 60:
                continue
            if total > 512 * 1024 * 1024 or time.time() - st.st_mtime > 20 * 86400:
                p.unlink()
                total -= st.st_size
                self.offsets.pop(p.name, None)


class Budget:
    def __init__(self, root):
        self.root, self.stable = root, None

    def ready(self):
        data = read(self.root / "budget.json", {})
        now = time.time()
        healthy = (data.get("learning", False) and data.get("fps", 0) >= 60
                   and abs(now * 1000 - data.get("at", 0)) < 15000)
        if not healthy:
            self.stable = None
            return False
        if self.stable is None:
            self.stable = now
        return now - self.stable >= 30


def load_dependencies():
    global np, torch, MaskablePPO, gym, spaces
    import numpy as np
    import torch
    import gymnasium as gym
    from gymnasium import spaces
    from sb3_contrib import MaskablePPO
    torch.set_num_threads(TORCH_THREADS)
    torch.set_num_interop_threads(1)


def make_model(device="cpu"):
    class ContractEnv(gym.Env):
        # Actual observations/rewards arrive from the running C# server, not a duplicate Python game.
        observation_space = spaces.Box(-np.inf, np.inf, shape=(INPUTS,), dtype=np.float32)
        action_space = spaces.Discrete(ACTIONS)

        def reset(self, *, seed=None, options=None):
            super().reset(seed=seed)
            return np.zeros(INPUTS, dtype=np.float32), {}

        def step(self, action):
            raise RuntimeError("Use server rollouts; the trainer must never invent gameplay outcomes")

    return MaskablePPO("MlpPolicy", ContractEnv(), policy_kwargs=dict(net_arch=dict(pi=[HIDDEN, HIDDEN], vf=[HIDDEN, HIDDEN])),
                       learning_rate=0.0003, n_steps=1024, batch_size=256, n_epochs=10, gamma=.99,
                       gae_lambda=.95, clip_range=.2, ent_coef=.02, target_kl=.02, device=device, verbose=0)


def width(model):
    """Hidden width of a checkpoint's actor (64 for models made before HIDDEN was raised)."""
    return int(model.policy.state_dict()["mlp_extractor.policy_net.0.bias"].shape[0])


def clone_teacher(model, rows, budget):
    teacher = [r for r in rows if r["source"] in ("rules", "human")][-CLONE_ROWS:]
    if len(teacher) < 32:
        return False
    domain = domain_of(rows)
    random.shuffle(teacher)
    held = int(len(teacher) * VALIDATION_SHARE) if len(teacher) >= 320 else 0
    validation, teacher = teacher[:held], teacher[held:]
    batches = (len(teacher) + CLONE_BATCH - 1) // CLONE_BATCH
    track("begin", "clone", domain, CLONE_EPOCHS * batches, "batch", epochs=CLONE_EPOCHS, rows=len(teacher), validationRows=held)
    epochs = []
    # Initial model has no dataset requirement: records are produced by the existing working rules during Play.
    for epoch in range(CLONE_EPOCHS):
        model.policy.set_training_mode(True)
        random.shuffle(teacher)
        total_loss = correct = seen = 0
        for b, begin in enumerate(range(0, len(teacher), CLONE_BATCH)):
            if not budget.ready():
                raise InterruptedError("training paused by frame budget")
            batch = teacher[begin:begin + CLONE_BATCH]
            obs = torch.tensor(np.asarray([r["obs"] for r in batch], dtype=np.float32), device=model.device)
            masks = np.asarray([r["mask"] for r in batch], dtype=bool)
            actions = torch.tensor([r["action"] for r in batch], dtype=torch.long, device=model.device)
            weights = torch.tensor([2.0 if r["source"] == "human" else 1.0 for r in batch], device=model.device)
            dist = model.policy.get_distribution(obs, action_masks=masks)
            loss = -(dist.log_prob(actions) * weights).mean() - .005 * dist.entropy().mean()
            model.policy.optimizer.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.policy.parameters(), .5)
            model.policy.optimizer.step()
            with torch.no_grad():
                correct += int((dist.distribution.probs.argmax(dim=1) == actions).sum())
            total_loss += float(loss.detach()) * len(batch)
            seen += len(batch)
            track("step", epoch * batches + b + 1, epoch=epoch + 1, loss=total_loss / seen, accuracy=correct / seen)
            time.sleep(.001)
        row = dict(epoch=epoch + 1, loss=total_loss / max(1, seen), accuracy=correct / max(1, seen))
        row.update(clone_validation(model, validation))
        epochs.append(row)
        track("step", (epoch + 1) * batches, epoch=epoch + 1, epochsDone=epochs)
    last = epochs[-1] if epochs else {}
    track("record", domain, "clone", rows=len(teacher), validationRows=held, epochs=epochs, loss=last.get("loss"),
          accuracy=last.get("accuracy"), valLoss=last.get("valLoss"), valAccuracy=last.get("valAccuracy"))
    return True


def clone_validation(model, rows):
    """Loss and accuracy (the teacher's action is the policy's most likely one) on held-out teacher rows."""
    if not rows:
        return {}
    model.policy.set_training_mode(False)
    total_loss = correct = 0
    with torch.no_grad():
        for begin in range(0, len(rows), 1024):
            batch = rows[begin:begin + 1024]
            obs = torch.tensor(np.asarray([r["obs"] for r in batch], dtype=np.float32), device=model.device)
            masks = np.asarray([r["mask"] for r in batch], dtype=bool)
            actions = torch.tensor([r["action"] for r in batch], dtype=torch.long, device=model.device)
            dist = model.policy.get_distribution(obs, action_masks=masks)
            total_loss += float(-dist.log_prob(actions).sum())
            correct += int((dist.distribution.probs.argmax(dim=1) == actions).sum())
    return dict(valLoss=total_loss / len(rows), valAccuracy=correct / len(rows))


def ppo_update(model, rows, version, used, budget):
    """Fill SB3's rollout buffer with version-matched C# on-policy transitions; do not implement PPO ourselves."""
    groups = defaultdict(list)
    for row in rows:
        groups[row["stream"]].append(row)
    segments, current = [], []
    for stream_rows in groups.values():
        stream_rows.sort(key=lambda r: (r["at"], int(r["id"].rsplit(":", 1)[1])))
        current = []
        for row in stream_rows:
            valid = row["source"] == "rl" and row["version"] == version and row["id"] not in used
            if not valid:
                if current:
                    segments.append(current)
                current = []
                continue
            if current and current[-1]["next"] != row["obs"]:
                # A bounded queue may drop samples. Bootstrap at the gap instead of inventing a contiguous trajectory.
                segments.append(current)
                current = []
            current.append(row)
            if row["terminated"] or row["truncated"]:
                segments.append(current)
                current = []
        if current:
            segments.append(current)
    if sum(map(len, segments)) < model.n_steps:
        return False
    from stable_baselines3.common.logger import configure
    model.set_logger(configure(format_strings=[]))
    model.rollout_buffer.reset()
    selected = []
    model.policy.set_training_mode(False)
    track("begin", "ppo", domain_of(rows), model.n_steps, "step", epochs=model.n_epochs)
    rewards = []
    for segment in segments:
        for i, row in enumerate(segment):
            if not budget.ready():
                raise InterruptedError("training paused by frame budget")
            obs = np.asarray(row["obs"], dtype=np.float32)[None, :]
            mask = np.asarray(row["mask"], dtype=bool)[None, :]
            action = torch.tensor([row["action"]], dtype=torch.long, device=model.device)
            with torch.no_grad():
                value, logp, _ = model.policy.evaluate_actions(torch.as_tensor(obs, device=model.device), action, action_masks=mask)
            # Recompute against the exact frozen checkpoint. Reject mixed/stale/corrupt inference data.
            if abs(float(logp[0]) - row["logp"]) > .002 or abs(float(value[0]) - row["value"]) > .002:
                raise ValueError("C#/Python policy mismatch: refusing on-policy update")
            cut = i == len(segment) - 1 or len(selected) + 1 == model.n_steps
            reward = row["reward"]
            if cut and not row["terminated"]:
                with torch.no_grad():
                    nv = model.policy.predict_values(torch.tensor(np.asarray(row["next"], dtype=np.float32)[None, :], device=model.device))
                reward += model.gamma * float(nv[0, 0])
            model.rollout_buffer.add(obs, np.asarray([[row["action"]]]), np.asarray([reward]), np.asarray([i == 0]),
                                     value.flatten(), logp.flatten(), action_masks=mask)
            selected.append(row["id"])
            rewards.append(row["reward"])
            if len(selected) % 64 == 0:
                track("step", len(selected))
            if len(selected) == model.n_steps:
                break
        if len(selected) == model.n_steps:
            break
    model.rollout_buffer.compute_returns_and_advantage(last_values=torch.tensor([0.], device=model.device), dones=np.asarray([True]))
    model.num_timesteps += len(selected)
    track("step", len(selected), optimizing=True)
    model.train()  # maintained MaskablePPO optimiser, clipping, KL stopping and entropy implementation
    used.update(selected)
    # SB3 keeps the statistics of the last train() in the logger until it is dumped (never: no output format)
    stats = {k.split("/", 1)[1]: float(v) for k, v in model.logger.name_to_value.items() if k.startswith("train/")}
    track("record", domain_of(rows), "ppo", rows=len(selected), timesteps=int(model.num_timesteps),
          meanReward=float(np.mean(rewards)) if rewards else 0.0, sumReward=float(np.sum(rewards)) if rewards else 0.0,
          loss=stats.get("loss"), policyLoss=stats.get("policy_gradient_loss"), valueLoss=stats.get("value_loss"),
          entropy=-stats["entropy_loss"] if "entropy_loss" in stats else None, approxKl=stats.get("approx_kl"),
          clipFraction=stats.get("clip_fraction"), explainedVariance=stats.get("explained_variance"))
    return True


def verify_export(model, folder, domain, rows, root, log):
    from evaluation import HOST_PROJECT, HOST_DLL, run_checked
    sources = list((HOST_PROJECT.parents[2] / "Assets/Server/Ai/Rl").glob("*.cs")) + [HOST_PROJECT.parent / "Program.cs"]
    if not HOST_DLL.exists() or max(p.stat().st_mtime for p in sources) > HOST_DLL.stat().st_mtime:
        run_checked(["dotnet", "build", str(HOST_PROJECT), "-c", "Release", "--nologo", "--verbosity", "quiet"], root, log)
    export = read(folder / (domain + ".json"))
    for row in list(rows)[-3:]:
        obs, mask = row["obs"], row["mask"]
        with torch.no_grad():
            distribution = model.policy.get_distribution(torch.tensor(np.asarray(obs, dtype=np.float32)[None, :], device=model.device), action_masks=np.asarray(mask)[None, :])
            expected = distribution.distribution.probs[0].cpu().numpy()
            value = float(model.policy.predict_values(torch.tensor(np.asarray(obs, dtype=np.float32)[None, :], device=model.device))[0, 0])
        p, v = numpy_forward(export, obs, mask)
        if max(abs(p - expected)) > 1e-4 or abs(v - value) > 1e-4:
            raise ValueError("Export does not match Python policy")
        fixture = folder / "parity-input.json"
        atomic(fixture, dict(obs=obs, mask=mask))
        import subprocess
        result = subprocess.run(["dotnet", str(HOST_DLL), "parity", "--model", str(folder / (domain + ".json")),
                                 "--domain", domain, "--observation", str(fixture)], capture_output=True, text=True, timeout=30,
                                creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
        if result.returncode:
            raise ValueError("C# parity failed: " + result.stderr[-300:])
        actual = json.loads(result.stdout)
        if max(abs(np.asarray(actual["probabilities"]) - expected)) > 1e-4 or abs(actual["value"] - value) > 1e-4:
            raise ValueError("C# inference differs from MaskablePPO")


def promote(root, folder):
    from evaluation import balance_hash
    report = read(folder / "evaluation.json", {})
    if not report.get("passed") or report.get("version") != folder.name:
        raise ValueError("Cannot apply an unevaluated model")
    environment = read(root / "environment.json", {})
    if not environment.get("rules") or report.get("balanceHash") != balance_hash(environment["rules"], environment):
        raise ValueError("Balance differs from evaluation; candidate must be re-evaluated")
    for name, digest in report["hashes"].items():
        if hashlib.sha256((folder / name).read_bytes()).hexdigest() != digest:
            raise ValueError("Model changed since evaluation")
    previous = read(root / "active.json")
    if previous and previous["version"] != folder.name:
        atomic(root / "previous.json", previous)
    atomic(root / "active.json", dict(version=folder.name))


def evaluation_progress(run, total, trial, arm, seconds, results):
    """evaluate() callback: which isolated run is playing and the per-run scores so far."""
    if TRACK is None:
        return
    p = TRACK.status.get("progress")
    if not p or p.get("phase") != "evaluate":
        TRACK.begin("evaluate", "", total, "run", seconds=seconds)
        p = TRACK.status["progress"]
    p.update(trial=trial + 1, arm=arm, runStartedAt=int(time.time() * 1000), results=list(results))
    TRACK.step(run)


def main(root):
    root.mkdir(parents=True, exist_ok=True)
    global TRACK
    status = dict(status="starting", candidate="", evaluation="not evaluated")
    TRACK = Tracker(root, status)

    def publish(message):
        status["status"] = message
        if not message.startswith("evaluating") and message != "training":
            status.pop("progress", None)
        TRACK.write(True)
    try:
        load_dependencies()
    except ImportError as error:
        publish(f"Missing dependency: {error.name}. Run uv sync --project Tools/Learning; rules remain active.")
        return
    from evaluation import evaluate
    spool, budget = Spool(root), Budget(root)
    used = set()
    data_version = None
    last_training = 0
    request_id = read(root / "request-result.json", {}).get("id", "")
    candidate = None
    pending = read(root / "pending.json")
    if pending:
        candidate = root / "models" / pending["version"]
    while not stopped(root):
        try:
            spool.poll()
            settings = read(root / "settings.json", {})
            req = read(root / "request.json", {})
            if req.get("id") and req["id"] != request_id:
                request_id = req["id"]
                atomic(root / "request-result.json", dict(id=request_id, status="processing"))
                if req["command"] == "rollback":
                    previous = read(root / "previous.json")
                    if previous:
                        promote(root, root / "models" / previous["version"])
                elif req["command"] == "apply":
                    latest = read(root / "latest-candidate.json", {})
                    target = candidate or (root / "models" / latest.get("version", "missing"))
                    promote(root, target)
                atomic(root / "request-result.json", dict(id=request_id, status="processed"))
            if not budget.ready():
                publish("paused: learning off / FPS below 60 / waiting 30 stable seconds")
                time.sleep(2)
                continue
            if candidate:
                status["candidate"] = candidate.name
                report = read(candidate / "evaluation.json")
                if report is None:
                    if not settings.get("evaluationEnabled", False):
                        publish("candidate ready; enable isolated long evaluation on the AI page")
                        time.sleep(2)
                        continue
                    publish("evaluating candidate against rules in isolated real-time servers")
                    report = evaluate(candidate, root, sys.stdout, progress=evaluation_progress)
                status["evaluation"] = "passed" if report["passed"] else "; ".join(report["reasons"][:3])
                if report["passed"] and settings.get("autoApply", True):
                    promote(root, candidate)
                atomic(root / "pending.json", {})
                candidate = None
                last_training = time.time()
                publish("evaluation finished")
                continue
            if time.time() - last_training < TRAIN_INTERVAL_SECONDS:
                time.sleep(2)
                continue
            active = read(root / "active.json", {})
            if active.get("version") != data_version:
                used.clear()
                data_version = active.get("version")
            previous_folder = root / "models" / active.get("version", "missing")
            previous_export = read(previous_folder / "strategy.json", {})
            from evaluation import balance_hash
            environment = read(root / "environment.json", {})
            old_report = read(previous_folder / "evaluation.json", {})
            outdated_balance = bool(previous_export) and old_report.get("balanceHash") != balance_hash(environment["rules"], environment)
            # Change only one tier per release; next-stage locomotion is first bootstrapped by the teacher.
            stage = min(3, previous_export.get("stage", 0) + 1)
            models, changed = {}, False
            device = "cuda" if settings.get("device") == "cuda" and torch.cuda.is_available() else "cpu"
            status["device"] = device
            status["gpu"] = torch.cuda.get_device_name(0) if device == "cuda" else ""
            status["samples"] = {d: len(rows) for d, rows in spool.rows.items()}
            status["updates"] = {}
            publish("training")
            for domain in ("strategy", "tactical"):
                rows = spool.rows[domain]
                checkpoint = previous_folder / (domain + ".zip")
                model = MaskablePPO.load(checkpoint, device=device) if checkpoint.exists() else make_model(device)
                if checkpoint.exists() and width(model) != HIDDEN:
                    model = make_model(device)   # capacity changed: start over from the teacher instead of a small net
                    if len([r for r in rows if r["source"] in ("rules", "human")]) < 64:
                        models = {}
                        break
                    changed |= clone_teacher(model, rows, budget)
                elif checkpoint.exists():
                    changed |= ppo_update(model, rows, active["version"], used, budget)
                    # A newly-enabled tier learns first from rules; experienced tiers only use their own PPO rollouts.
                    if domain == "tactical" and previous_export.get("stage", 0) < stage:
                        changed |= clone_teacher(model, rows, budget)
                    elif outdated_balance:
                        changed |= clone_teacher(model, rows, budget)
                else:
                    if len([r for r in rows if r["source"] in ("rules", "human")]) < 64:
                        models = {}
                        break
                    changed |= clone_teacher(model, rows, budget)
                models[domain] = model
                status["updates"][domain] = int(model.num_timesteps)
            TRACK.end()
            if not models or not changed:
                publish("collecting demonstrations / fresh version-matched PPO rollouts")
                time.sleep(2)
                continue
            version = f"v{int(time.time() * 1000)}"
            folder = root / "models" / version
            folder.mkdir(parents=True)
            publish("exporting and verifying C#/Python inference parity")
            for domain, model in models.items():
                model.save(folder / (domain + ".zip"))
                export_policy(model, folder / (domain + ".json"), version, domain, stage)
                verify_export(model, folder, domain, spool.rows[domain], root, sys.stdout)
            candidate = folder
            atomic(root / "pending.json", dict(version=version))
            atomic(root / "latest-candidate.json", dict(version=version))
        except InterruptedError as error:
            publish(str(error))
            time.sleep(2)
        except Exception as error:
            traceback.print_exc()
            if request_id:
                previous_result = read(root / "request-result.json", {})
                if previous_result.get("status") == "processing":
                    atomic(root / "request-result.json", dict(id=request_id, status="error", error=str(error)))
            publish("error: " + str(error))
            candidate = None
            last_training = time.time()
            time.sleep(2)
    publish("stopped")


if __name__ == "__main__":
    args = argparse.ArgumentParser()
    args.add_argument("--root", type=Path, required=True)
    args.add_argument("--stop-file", type=Path)
    options = args.parse_args()
    import policy_io
    policy_io.STOP_FILE = options.stop_file
    options.root.mkdir(parents=True, exist_ok=True)
    with (options.root / "trainer.log").open("a", encoding="utf-8", buffering=1) as logfile:
        with contextlib.redirect_stdout(logfile), contextlib.redirect_stderr(logfile):
            main(options.root.resolve())
