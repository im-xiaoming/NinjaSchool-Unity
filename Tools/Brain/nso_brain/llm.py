"""OpenAI-compatible completion transport and the legacy permissive reply parser."""

import asyncio
import json
import struct

from .prompts import PRIORITIES, RESOURCES, short

REPLY_SCHEMA = json.loads((RESOURCES / "reply_schema.json").read_text(encoding="utf-8"))
GOALS = {}
for canonical, aliases in (
    ("hunt", "hunt fight train hit attack kill farm grind"),
    ("collect", "collect gather loot"),
    ("rest", "rest heal recover drink"),
    ("explore", "explore"),
    ("travel", "travel go move walk"),
    ("idle", "idle wait chat"),
    ("quest", "quest mission task questline"),
    ("shop", "shop buy buy_potions potions"),
    ("goto", "goto go_to moveto position"),
):
    GOALS.update(dict.fromkeys(aliases.split(), canonical))


class InvalidReply(ValueError):
    def __init__(self, error, response):
        super().__init__(error)
        self.response = response


def parse_decision(content):
    if not content:
        raise InvalidReply("empty answer (thinking used the whole token budget?)", content or "")
    original = content
    content = content.rsplit("</think>", 1)[-1]
    start, end = content.find("{"), content.rfind("}")
    if start < 0 or end <= start:
        raise InvalidReply("no JSON object in the answer", original)
    try:
        root = json.loads(content[start : end + 1])
    except ValueError as error:
        raise InvalidReply("invalid JSON: " + str(error), original) from error
    if not isinstance(root, dict):
        raise InvalidReply("answer is not a JSON object", original)
    if not isinstance(root.get("steps"), list) or not root["steps"]:
        raise InvalidReply("no steps", original)

    def integer(d, key, default):
        try:
            value = d[key]
            # NsoServer.Data.Json.Long truncates doubles and accepts numeric strings.
            return (
                max(-2147483648, min(2147483647, int(value)))
                if not isinstance(value, bool)
                else default
            )
        except (KeyError, ValueError, TypeError, OverflowError):
            return default

    steps = []
    for st in root["steps"]:
        if not isinstance(st, dict):
            continue
        goal = GOALS.get(str(st.get("goal") or "").strip().lower())
        if goal:
            steps.append(
                {
                    "goal": goal,
                    **{
                        key: integer(st, key, default)
                        for key, default in (
                            ("map", -1),
                            ("mob", -1),
                            ("count", 5),
                            ("seconds", 30),
                            ("x", -1),
                            ("y", -1),
                        )
                    },
                }
            )
    if not steps:
        raise InvalidReply("no known goal in the steps", original)
    result = {
        "reason": str(root.get("reason") or ""),
        "say": str(root.get("say") or ""),
        "fleeHp": integer(root, "fleeHp", -1),
        "steps": steps,
    }
    if isinstance(root.get("priorities"), dict):
        result["priorities"] = {}
        for name in PRIORITIES:
            try:
                value = root["priorities"].get(name)
                result["priorities"][name] = float(value) if value is not None else 1.0
            except (ValueError, TypeError):
                result["priorities"][name] = 1.0
    return result


async def chat(client, settings, system, user, max_tokens, structured):
    body = {
        "model": settings["LlmModel"],
        "temperature": struct.unpack("f", struct.pack("f", settings["LlmTemperature"]))[0],
        "max_tokens": max_tokens,
        "stream": False,
        "messages": [],
    }
    if settings["LlmReasoningEffort"]:
        body["reasoning_effort"] = settings["LlmReasoningEffort"]
    if structured and settings["LlmJsonMode"]:
        body["response_format"] = (
            REPLY_SCHEMA if settings["LlmStructuredOutputs"] else {"type": "json_object"}
        )
    if system:
        body["messages"].append({"role": "system", "content": system})
    body["messages"].append({"role": "user", "content": user})
    headers = {"Authorization": "Bearer " + settings["LlmApiKey"]} if settings["LlmApiKey"] else {}
    # Match C#'s per-request CancellationToken, rather than a separate timeout for each HTTP phase.
    async with asyncio.timeout(max(1, settings["LlmTimeoutSeconds"])):
        response = await client.post(
            settings["LlmEndpoint"],
            json=body,
            headers=headers,
            timeout=max(1, settings["LlmTimeoutSeconds"]),
        )
    if not response.is_success:
        raise RuntimeError(f"HTTP {response.status_code}: " + short(response.text, 160))
    root = response.json()
    choices = root.get("choices", [])
    message = choices[0].get("message", {}) if choices else root.get("message", {})
    if "content" in message:
        return message["content"] or ""
    if "error" in root:
        raise RuntimeError("server error: " + short(str(root["error"]), 160))
    raise ValueError("no message content in the response")
