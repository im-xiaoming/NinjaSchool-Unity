"""Session tunables come from C#; provider credentials remain in this process."""

import ipaddress
import os
from pathlib import Path
from urllib.parse import urlparse


def load_local_environment():
    """Read only supported provider keys; do not evaluate shell syntax or log secret values."""
    if os.environ.get("NSO_BRAIN_SKIP_DOTENV") == "1":
        return
    path = Path(__file__).resolve().parents[3] / ".env"
    if not path.exists():
        return
    allowed = {"NSO_LLM_ENDPOINT", "NSO_QDRANT_URL", "NSO_EMBED_ENDPOINT", "NSO_LLM_API_KEY"}
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        key, separator, value = line.partition("=")
        key = key.strip().removeprefix("export ")
        if separator and key in allowed:
            value = value.strip()
            if len(value) >= 2 and value[0] == value[-1] and value[0] in ("'", '"'):
                value = value[1:-1]
            os.environ.setdefault(key, value)


def is_local(url):
    parsed = urlparse(url)
    if parsed.scheme not in ("http", "https"):
        return False
    if (parsed.hostname or "").lower() == "localhost":
        return True
    try:
        return ipaddress.ip_address(parsed.hostname or "").is_loopback
    except ValueError:
        return False


def load_settings(settings):
    load_local_environment()
    result = dict(settings)
    # Explicit environment overrides support an independently launched developer service.
    for key, env in (
        ("LlmEndpoint", "NSO_LLM_ENDPOINT"),
        ("QdrantUrl", "NSO_QDRANT_URL"),
        ("EmbedEndpoint", "NSO_EMBED_ENDPOINT"),
    ):
        if os.environ.get(env):
            result[key] = os.environ[env]
    result["LlmApiKey"] = os.environ.get("NSO_LLM_API_KEY", "")
    for key in ("QdrantUrl", "EmbedEndpoint") if result["RagEnabled"] else ():
        if not is_local(result[key]):
            raise ValueError("RAG endpoints must be on this machine")
    if result["LlmLocalOnly"] and not is_local(result["LlmEndpoint"]):
        raise ValueError("endpoint is not on this machine (LlmLocalOnly)")
    return result
