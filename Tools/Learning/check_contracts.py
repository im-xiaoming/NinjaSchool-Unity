"""Short synthetic contract checks only: no GameServer, Play mode, production data or long training."""
import json
from pathlib import Path
import subprocess
import tempfile
import time

import trainer
from policy_io import ACTIONS, INPUTS, SCHEMA, atomic, export_policy, read


class AlwaysReady:
    def ready(self):
        return True


def main():
    trainer.load_dependencies()
    np, torch = trainer.np, trainer.torch
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = trainer.make_model(device)
    obs = np.random.default_rng(42).uniform(-1, 1, INPUTS).astype(np.float32)
    mask = np.zeros(ACTIONS, dtype=bool)
    mask[:3] = True
    examples = [dict(source="rules", action=1, obs=obs.tolist(), mask=mask.tolist()) for _ in range(64)]
    assert trainer.clone_teacher(model, examples, AlwaysReady())
    with tempfile.TemporaryDirectory(prefix="nso-rl-contract-") as temp:
        root = Path(temp)
        atomic(root / "budget.json", dict(fps=120, learning=True, at=time.time() * 1000))
        atomic(root / "settings.json", dict(evaluationEnabled=True))
        export_policy(model, root / "strategy.json", "contract", "strategy", 1)
        with (root / "checks.log").open("w") as log:
            trainer.verify_export(model, root, "strategy", examples[:1], root, log)
        # A malformed all-masked action set must fail in the actual C# inference boundary.
        from evaluation import HOST_DLL
        atomic(root / "invalid-mask.json", dict(obs=obs.tolist(), mask=[False] * ACTIONS))
        result = subprocess.run(["dotnet", str(HOST_DLL), "parity", "--model", str(root / "strategy.json"),
                                 "--observation", str(root / "invalid-mask.json")], capture_output=True, timeout=30)
        assert result.returncode != 0
        with torch.no_grad():
            x = torch.tensor(obs[None, :], device=model.device)
            values, lp, _ = model.policy.evaluate_actions(x, torch.tensor([1], device=model.device), action_masks=mask[None, :])
        rows = []
        n = model.n_steps   # one full PPO rollout
        for i in range(n):
            rows.append(dict(id=f"test:{i}", stream="test:strategy", domain="strategy", source="rl", version="contract",
                             obs=obs.tolist(), mask=mask.tolist(), action=1, next=obs.tolist(), nextMask=mask.tolist(),
                             value=float(values[0, 0]), logp=float(lp[0]), reward=.01, terminated=False, truncated=i == n - 1, at=i))
        invalid = dict(rows[0], id="test:999999", source="human", version="other")
        used = set()
        assert trainer.ppo_update(model, [invalid] + rows, "contract", used, AlwaysReady())
        assert len(used) == n and invalid["id"] not in used, (len(used), n)
        # A restarted trainer loads the maintained SB3 checkpoint with no Python game environment attached.
        model.save(root / "restart.zip")
        restored = trainer.MaskablePPO.load(root / "restart.zip", device=device)
        assert restored.policy is not None and restored.rollout_buffer is not None
        for r in rows:
            r["version"] = "restart"
        with torch.no_grad():
            values, lp, _ = restored.policy.evaluate_actions(x, torch.tensor([1], device=model.device), action_masks=mask[None, :])
        for r in rows:
            r["value"], r["logp"] = float(values[0, 0]), float(lp[0])
        assert trainer.ppo_update(restored, rows, "restart", set(), AlwaysReady())
        from evaluation import quality_gate
        baseline = {"ninja": dict(completed=1, progress=400, deaths=0, stuck=0, exp=100, runs=1)}
        assert not quality_gate(baseline, baseline)
        worse = {"ninja": dict(completed=0, progress=0, deaths=1, stuck=1, exp=0, runs=1)}
        assert quality_gate(baseline, worse)
        row = dict(rows[0], schema=SCHEMA)
        chunk = root / "experience-contract.jsonl"
        complete = json.dumps(row).encode() + b"\n"
        chunk.write_bytes(complete + complete[:30])
        spool = trainer.Spool(root)
        spool.poll()
        assert len(spool.rows["strategy"]) == 1
        spool.poll()
        assert len(spool.rows["strategy"]) == 1
        print(json.dumps(dict(result="PASS", device=device, checks=["teacher imitation", "masked C#/Python parity",
                         "reject empty mask", "PPO excludes demonstrations", "checkpoint restart and PPO update", "regression gate", "partial JSONL recovery"])))


if __name__ == "__main__":
    main()
