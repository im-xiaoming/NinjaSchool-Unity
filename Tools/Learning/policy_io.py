"""Versioned MaskablePPO <-> pure C# MLP interchange. No pickle is ever loaded by the game."""
import json
import os
import time
from pathlib import Path

SCHEMA, ACTIONS, INPUTS = 1, 48, 424
STOP_FILE = None


def stopped(root):
    return Path(STOP_FILE or (root / "stop")).exists()


def atomic(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".tmp")
    temp.write_text(json.dumps(data, ensure_ascii=True, allow_nan=False), encoding="utf-8")
    # Windows: replacing fails while the game (RlBridge) has the target open for reading; it closes within milliseconds.
    for attempt in range(40):
        try:
            os.replace(temp, path)
            return
        except PermissionError:
            if attempt == 39:
                raise
            time.sleep(.025)


def read(path, default=None):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return default


def export_policy(model, path, version, domain, stage):
    state = model.policy.state_dict()
    tensors = {
        "a1": "mlp_extractor.policy_net.0.weight", "ab1": "mlp_extractor.policy_net.0.bias",
        "a2": "mlp_extractor.policy_net.2.weight", "ab2": "mlp_extractor.policy_net.2.bias",
        "ao": "action_net.weight", "abo": "action_net.bias",
        "c1": "mlp_extractor.value_net.0.weight", "cb1": "mlp_extractor.value_net.0.bias",
        "c2": "mlp_extractor.value_net.2.weight", "cb2": "mlp_extractor.value_net.2.bias",
        "co": "value_net.weight", "cbo": "value_net.bias",
    }
    data = dict(schema=SCHEMA, inputs=INPUTS, actions=ACTIONS, version=version, domain=domain, stage=stage)
    data.update({key: state[name].detach().cpu().tolist() for key, name in tensors.items()})
    atomic(path, data)


def numpy_forward(data, obs, mask):
    """Independent export check (the CLI parity check also exercises the actual C# loader)."""
    import numpy as np
    def layer(x, weight, bias, tanh=True):
        x = np.asarray(data[weight], dtype=np.float32) @ x + np.asarray(data[bias], dtype=np.float32)
        return np.tanh(x) if tanh else x
    x = np.asarray(obs, dtype=np.float32)
    logits = layer(layer(layer(x, "a1", "ab1"), "a2", "ab2"), "ao", "abo", False)
    logits = np.where(mask, logits, -1e8)
    p = np.exp(logits - logits.max()) * np.asarray(mask)
    return p / p.sum(), float(layer(layer(layer(x, "c1", "cb1"), "c2", "cb2"), "co", "cbo", False)[0])
