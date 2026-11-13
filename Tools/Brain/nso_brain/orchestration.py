"""Literal-compatible LlmPlanner.Decide context budget and summary workflow."""

import copy
import math

from .llm import chat, parse_decision
from .prompts import SYSTEM_PROMPT, build_prompt, recall_query, short


async def decide(client, memory, settings, observation):
    s = copy.deepcopy(observation)
    recalled = await memory.recall(s["Account"], recall_query(s), settings["RagTopK"])
    if recalled is not None:
        s["EventsInPrompt"] = max(1, settings["ContextEvents"])
    system = SYSTEM_PROMPT
    if recalled is None and settings["LlmGuide"]:
        system += "\n\n" + settings["LlmGuide"]
    budget = settings["LlmContextTokens"] - settings["LlmMaxTokens"] - 150

    def tokens(value):
        return math.ceil(
            len(value.encode("utf-16-le", errors="surrogatepass"))
            / 2
            / max(1.0, settings["CharsPerToken"])
        )

    user = build_prompt(s, settings, recalled, True)
    summary, cut = None, 0
    if tokens(system) + tokens(user) > budget or len(s["Memory"]) >= max(2, settings["MemorySize"]):
        keep = max(1, settings["ContextEvents"])
        if len(s["Memory"]) > keep:
            try:
                count = len(s["Memory"]) - keep
                prompt = (
                    f'You are {s["Name"]}, a ninja player. Rewrite your memories as ONE short '
                    f'paragraph (at most {settings["SummaryWords"]} words, first person, English) '
                    "keeping what matters for playing: dangers, good hunting places, where NPCs "
                    "are, quest progress, what failed and why. Plain text only.\n"
                )
                if s["Summary"]:
                    prompt += "Earlier summary: " + s["Summary"] + "\n"
                prompt += "Events (oldest first):\n" + "".join(
                    "- " + e + "\n" for e in s["Memory"][:count]
                )
                summary = (await chat(client, settings, None, prompt, 300, False)).strip()
                if not summary:
                    raise ValueError("empty summary")
                summary = short(summary.replace("\n", " "), 800)
                cut = count
                s["Summary"] = summary
                del s["Memory"][:cut]
            except Exception:
                summary, cut = None, 0
            s["EventsInPrompt"] = keep
            user = build_prompt(s, settings, recalled, True)
    if tokens(system) + tokens(user) > budget:
        user = build_prompt(s, settings, recalled, False)
    while recalled is not None and len(recalled) > 1 and tokens(system) + tokens(user) > budget:
        recalled.pop()
        user = build_prompt(s, settings, recalled, False)
    raw = await chat(client, settings, system, user, settings["LlmMaxTokens"], True)
    return {"decision": parse_decision(raw), "raw": raw, "summary": summary, "summaryCut": cut}
