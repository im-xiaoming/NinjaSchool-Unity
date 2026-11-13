"""RAG document rendering. Authoritative item-use descriptions are supplied by C#."""


def text(d, key):
    value = d.get(key)
    return "" if value is None else str(value)


def number(d, key):
    try:
        return int(round(d[key])) if isinstance(d[key], float) else int(d[key])
    except (KeyError, TypeError, ValueError):
        return -1


def records(d, key):
    return [x if isinstance(x, dict) else {"value": x} for x in d.get(key, [])]


def join(values, separator=", "):
    return separator.join(text(v, "value") for v in values)


def price(d):
    parts = [
        f"{number(d, k)} {label}"
        for k, label in (("yen", "yen"), ("xu", "xu"), ("luong", "lượng"))
        if number(d, k) > 0
    ]
    return " / ".join(parts) or "free"


def build_documents(wiki, item_use):
    c = wiki.get("collections", {})
    sellers = {}
    for s in records(c, "shops"):
        name = text(s, "name")
        plain = name[:-5] if name.endswith(" khóa") else name
        for n in records(c, "npcs"):
            for m in records(n, "menu"):
                entry = text(m, "value")
                if entry.startswith(plain + " (") or entry == plain:
                    sellers[number(s, "id")] = text(n, "name")
                    break
    mob_levels = {number(m, "id"): number(m, "level") for m in records(c, "mobs")}
    documents = []

    def emit(key, kind, value):
        documents.append({"key": key, "kind": kind, "text": value, "agent": "*"})

    for it in records(c, "items"):
        ident = number(it, "id")
        gender = text(it, "gender")
        gender = "both genders" if gender in ("", "cả hai") else "gender " + gender
        value = (
            f'ITEM {text(it, "name")} (id {ident}): {text(it, "typeName")}, '
            f'level {number(it, "level")}, {gender}.'
        )
        if text(it, "description"):
            value += " Description: " + text(it, "description").rstrip(".") + "."
        get = []
        for s in records(it, "shops"):
            seller = sellers.get(number(s, "shop"))
            location = (
                f'{seller} ({text(s, "shopName")})'
                if seller is not None
                else "the shop " + text(s, "shopName")
            )
            get.append("bought at " + location + " for " + price(s))
        for d in records(it, "droppedBy"):
            get.append(
                "dropped by "
                + text(d, "mobName")
                + (
                    " during quest " + str(number(d, "quest"))
                    if "quest" in d and number(d, "quest") >= 0
                    else ""
                )
            )
        get.extend("collected for quest step " + text(q, "value") for q in records(it, "questPick"))
        value += (
            " How to get: "
            + (
                "; ".join(get)
                or "not sold and not dropped in this offline game (quest reward, event or unavailable)"
            )
            + "."
        )
        use = item_use.get(str(ident), {})
        value += " Use: " + use.get("text", "unknown") + "."
        if use.get("usable"):
            value += f' Needs level {number(it, "level")}'
            if use.get("gender") in (0, 1):
                value += " and the right gender"
            value += "."
        emit(f"item:{ident}", "item", value)

    for m in records(c, "mobs"):
        value = (
            f'MONSTER {text(m, "name")} (id {number(m, "id")}): level {number(m, "level")}, '
            f'HP {number(m, "hp")}' + (", BOSS" if m.get("boss") is True else "") + "."
        )
        where = [f'{text(x, "name")} (map {number(x, "id")})' for x in records(m, "maps")]
        value += " Lives in: " + (", ".join(where) or "no normal map") + "."
        for key, label in (
            ("questKill", " Killed for quest steps "),
            ("questDrops", " Drops quest items for steps "),
        ):
            if records(m, key):
                value += label + join(records(m, key)) + "."
        value += (
            " Train on monsters from your level to about 3 above; 10 levels below give no "
            "experience. Monsters drop yen (about one kill in ten), sometimes stones and potions."
        )
        emit(f'mob:{number(m, "id")}', "monster", value)

    for m in records(c, "maps"):
        unlock = number(m, "unlockQuest")
        value = f'MAP {text(m, "name")} (id {number(m, "id")}): {text(m, "typeName")}'
        value += (f", opens after quest {unlock}" if unlock > 0 else "") + "."
        mobs = [
            f'{text(x, "name")} level {mob_levels.get(number(x, "id"), 0)}'
            for x in records(m, "mobs")
        ]
        npcs = list(
            dict.fromkeys(
                f'{text(x, "name")} ({number(x, "x")},{number(x, "y")})' for x in records(m, "npcs")
            )
        )
        exits = [f'{text(x, "mapName")} (map {number(x, "map")})' for x in records(m, "exits")]
        for values, label in ((mobs, " Monsters: "), (npcs, " NPCs: "), (exits, " Exits to: ")):
            if values:
                value += label + ", ".join(values) + "."
        emit(f'map:{number(m, "id")}', "map", value)

    for n in records(c, "npcs"):
        value = f'NPC {text(n, "name")} (id {number(n, "id")}).'
        for key, label, separator in (
            ("menu", " Menu: ", "; "),
            ("questGiver", " Gives quests ", ", "),
        ):
            if records(n, key):
                value += label + join(records(n, key), separator) + "."
        where = [
            f'{text(x, "mapName")} (map {number(x, "map")} at '
            f'{number(x, "x")},{number(x, "y")})'
            for x in records(n, "maps")
        ]
        if where:
            value += " Found in: " + ", ".join(where) + "."
        if text(n, "status") != "done":
            value += " Some of its features are not ported offline."
        emit(f'npc:{number(n, "id")}', "npc", value)

    for s in records(c, "shops"):
        seller = sellers.get(number(s, "id"))
        value = f'SHOP {text(s, "name")} (shop {number(s, "id")})'
        value += ", opened at NPC " + seller if seller is not None else ""
        value += ". Shops open after quest 3. Sells: "
        value += ", ".join(text(x, "name") + " " + price(x) for x in records(s, "items")) + "."
        if text(s, "name").endswith(" khóa"):
            value += " Items bought with yen are locked (cannot be traded)."
        emit(f'shop:{number(s, "id")}', "shop", value)

    for q in records(c, "quests"):
        value = (
            f'QUEST {number(q, "id")} {text(q, "name")}: from {text(q, "giverNpcName")}, '
            f'level {number(q, "levelRequire")}, reward {number(q, "rewardExp")} exp and '
            f'{number(q, "rewardYen")} yen.'
        )
        steps = []
        for s in records(q, "steps"):
            step = f'{number(s, "index") + 1}. {text(s, "name")}'
            if number(s, "count") > 0:
                step += f' x{number(s, "count")}'
            for key, label in (
                ("npcName", "NPC"),
                ("pickItemName", "item"),
                ("droppedByName", "dropped by"),
            ):
                if text(s, key):
                    step += f" ({label} {text(s, key)})"
            steps.append(step)
        if steps:
            value += " Steps: " + "; ".join(steps) + "."
        if text(q, "detail"):
            value += " " + text(q, "detail").rstrip(".") + "."
        if number(q, "unsupportedSteps") > 0:
            value += " Some steps are not ported offline."
        emit(f'quest:{number(q, "id")}', "quest", value)

    for s in records(c, "skills"):
        value = (
            f'SKILL {text(s, "name")} (id {number(s, "id")}): {text(s, "className")}, '
            f'{text(s, "typeName")}, up to {number(s, "maxPoint")} points.'
        )
        levels = records(s, "levels")
        if levels:
            first, last = levels[0], levels[-1]
            value += (
                f' From level {number(first, "level")}; mana {number(first, "mana")}-'
                f'{number(last, "mana")}, cooldown {number(first, "cooldown")}-'
                f'{number(last, "cooldown")} ms, up to {number(last, "targets")} targets.'
            )
        if text(s, "description"):
            value += " " + text(s, "description").rstrip(".") + "."
        emit(f'skill:{number(s, "id")}', "skill", value)
    return documents
