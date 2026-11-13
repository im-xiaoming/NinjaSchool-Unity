"""Literal-compatible port of LlmPlanner.BuildPrompt and its context inputs."""

from pathlib import Path
from decimal import Decimal, ROUND_HALF_UP

RESOURCES = Path(__file__).with_name("resources")
SYSTEM_PROMPT = (RESOURCES / "system.txt").read_text(encoding="utf-8")
GRID_LEGEND = (RESOURCES / "legend.txt").read_text(encoding="utf-8")
PRIORITIES = ("quest", "hunt", "shop", "explore", "collect", "idle")


def short(text, limit):
    text = text or ""
    # C# limits use UTF-16 code units, including supplementary characters in names/events.
    encoded = text.encode("utf-16-le", errors="surrogatepass")
    return (
        text
        if len(encoded) <= limit * 2
        else encoded[: limit * 2].decode("utf-16-le", errors="surrogatepass") + "..."
    )


def decimal_text(value, places, precision=15):
    # .NET custom numeric formatting rounds ties away from zero (Python format uses ties to even).
    decimal = Decimal(format(value, f".{precision}g"))
    return format(
        decimal.quantize(Decimal(1).scaleb(-places), rounding=ROUND_HALF_UP), f".{places}f"
    )


def priority_text(weights):
    if weights is None:
        return "default (all 1)"
    return ", ".join(
        f"{name} {decimal_text(value, 2, 7)}".rstrip("0").rstrip(".")
        for name, value in zip(PRIORITIES, weights)
    )


def recall_query(s):
    return (
        f'{s["Trigger"]} | quest {s["QuestName"]}: {s["QuestStep"]} '
        f'{s["QuestHint"]} | {s["Activity"]} | map {s["MapId"]} {s["MapName"]}'
    )


def build_prompt(s, settings, recalled=None, terrain=True):
    b = []
    add = b.append
    add(f'YOU: {s["Name"]}. {s["Bio"]}\nTraits: {s["Traits"]}\n')
    spending = (
        "thrifty: you keep money back, carry few potions and rarely buy things just to try"
        if s["Greed"] >= 0.67
        else (
            "spender: you stock up and happily buy new things to try"
            if s["Greed"] <= 0.33
            else "balanced"
        )
    )
    add(f"SPENDING STYLE: {spending}\n")
    add(
        f'CHARACTER: level {s["Level"]} ({decimal_text(s["ExpPercent"], 1)}% to next), {s["ClassName"]}, '
        f'HP {s["HpPercent"]}%, MP {s["MpPercent"]}%, yen {s["Yen"]}, xu {s["Xu"]}'
        + ("" if s["HasWeapon"] else ", NO WEAPON")
        + "\n"
    )
    add(
        f'LIFE: {s["Kills"]} kills, {s["Deaths"]} deaths, {s["QuestsDone"]} quests finished, '
        f'{s["UnlockedMaps"]} maps unlocked\n'
    )
    add(
        f'BAG ({s["FreeSlots"]} free slots): HP potions {s["HpPotions"]}, MP potions '
        f'{s["MpPotions"]}, food {s["Foods"]}'
        + (" (eating: HP/MP regenerating)" if s["Fed"] else " (not eating)")
        + (", " + ", ".join(s["Bag"]) if s["Bag"] else "")
        + "\n"
    )
    add("EQUIPPED: " + (", ".join(s["Equipment"]) or "nothing") + "\n")
    if s["QuestId"] >= 0:
        add(
            f'MAIN QUEST {s["QuestId"]} "{s["QuestName"]}": {s["QuestState"]}'
            + (f', step "{s["QuestStep"]}"' if s["QuestStep"] else "")
            + (f' ({s["QuestHint"]})' if s["QuestHint"] else "")
            + "\n"
        )
    else:
        add("MAIN QUEST: all done\n")
    if s["QuestId"] >= 0 and s["QuestGuide"]:
        add(
            "QUEST NEXT: " + s["QuestGuide"] + '. The step {"goal":"quest"} does exactly this '
            "by itself (walks there, talks, hunts, picks up); prefer it over goto/hunt unless "
            "you must shop, rest or train for the level first.\n"
        )
    if s["TryItemId"] >= 0:
        add(
            "UNKNOWN ITEM YOU COULD TRY: "
            + s["TryItemText"]
            + ' - the step {"goal":"shop","mob":'
            + str(s["TryItemId"])
            + "} buys one and uses it once; what it did goes into your knowledge.\n"
        )
    add(
        f'LOCATION: map {s["MapId"]} "{s["MapName"]}"'
        + (" (village)" if s["InVillage"] else "")
        + f', players here {s["PlayersHere"]}, items on the ground you may take {s["ItemsNearby"]}\n'
    )
    add(
        f'CURRENT GROUND LOOT RULES: exclusive owner for {settings["GroundOwnerSeconds"]} '
        f'seconds; disappears after {settings["GroundLifetimeSeconds"]} seconds. '
        "These timings override the beginner guide.\n"
    )
    if s["CurrentPlan"]:
        add("CURRENT PLAN: " + s["CurrentPlan"] + "\n")
    add("YOUR PRIORITIES NOW: " + priority_text(s["Priorities"]) + "\n")
    if s["Activity"]:
        add("DOING NOW: " + s["Activity"] + "\n")
    if s["MapView"]:
        add("CURRENT MAP (pixels; x grows right, y grows DOWN and is the floor under the feet):\n")
        if terrain and s["Terrain"]:
            add(GRID_LEGEND + s["Terrain"])
        add(s["MapView"])
    if s["Summary"]:
        add("YOUR STORY SO FAR (summary of older memories): " + s["Summary"] + "\n")
    if recalled is not None:
        add("MEMORIES RELEVANT NOW (recalled from your long-term memory):\n")
        if not recalled:
            add("- nothing relevant\n")
        for memory in recalled:
            add("- " + short(memory, max(80, settings["RagMemoryChars"])) + "\n")
    else:
        add("KNOWLEDGE (what you learned playing):\n")
        add("".join("- " + k + "\n" for k in s["Knowledge"]) or "- nothing yet\n")
        if s["NpcMemory"]:
            add(
                "NPC POSITIONS YOU REMEMBER IN OTHER MAPS (goto them with the map id and coordinates):\n"
            )
            add("".join("- " + m + "\n" for m in s["NpcMemory"]))
    add("RECENT EVENTS (oldest first):\n")
    if not s["Memory"]:
        add("- nothing yet\n")
    for event in s["Memory"][max(0, len(s["Memory"]) - s["EventsInPrompt"]) :]:
        add("- " + event + "\n")
    add('HUNTING GROUNDS IN UNLOCKED MAPS (map id "name" hops: monster id name level):\n')
    for h in s["Hunts"][:16]:
        add(
            f'- map {h["MapId"]} "{h["MapName"]}" {h["Hops"]} hops: mob {h["MobId"]} '
            f'{h["MobName"]} lv {h["MobLevel"]}\n'
        )
    if not s["Hunts"]:
        add("- none suitable\n")
    add(
        "NEIGHBOUR MAPS: "
        + ", ".join(f'{n} "{s["MapNames"].get(str(n), "")}"' for n in s["Neighbors"])
        + "\n"
    )
    add("EVENT: " + s["Trigger"] + "\nDecide the next plan. JSON only.")
    return "".join(b)
