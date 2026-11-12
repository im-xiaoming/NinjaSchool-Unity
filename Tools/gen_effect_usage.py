"""Builds Assets/StreamingAssets/NsoWiki/effect-usage.json: where and for whom each effect animation (nj_effect id) plays.

Scans the client sources for ServerEffect.addServerEffect / addServerEffectWithTime calls. For each call it records:
  - file, enclosing method and line;
  - "when": the conditions that really enclose the call (tracked by brace nesting, switch/case aware);
  - "how": what it is attached to (a character or a point), loop count / duration, direction;
  - "who": the enclosing conditions resolved against the wiki data: mob template ids -> mob name + level,
    status-effect types -> effect names, element (getSys) -> element name, packet cases -> server message.
Calls whose id is not a literal are listed under "dynamic". Rules that are not literal calls (the default mob hit effect,
sent by the server as id -1) are added from RULES below.

Needs NsoWiki/db.json (written by the dev wiki). Run from the project root after changing effect calls:
    python Tools/gen_effect_usage.py
"""
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCES = [os.path.join(ROOT, "Assets", "Scripts")]
DB = os.path.join(ROOT, "NsoWiki", "db.json")
OUT = os.path.join(ROOT, "Assets", "StreamingAssets", "NsoWiki", "effect-usage.json")

CALL = re.compile(r"ServerEffect\.(addServerEffect|addServerEffectWithTime)\((.*)\);")
METHOD = re.compile(r"^\s*(public|private|protected|internal|static)[^;=]*\b(\w+)\s*\([^;]*\)\s*$")
HEADER = re.compile(r"^\s*((else\s+)?if\s*\(.*\)|else|switch\s*\((.*)\))\s*$")
CASE = re.compile(r"^\s*case\s+([^:]+):")

# Element names of getSys() (Char.getSys: 1 fire, 2 water, 3 wind).
ELEMENTS = {"1": "Hỏa", "2": "Thủy", "3": "Phong"}

# Server message names of the Controller cases that play an effect (client Controller.onMessage).
PACKETS = {"-78": "server gửi hiệu ứng lên nhân vật mình", "-110": "server gửi hiệu ứng lên một nhân vật",
           "-5": "quái bị đánh", "5": "nhân vật lên cấp", "7": "nhận hiệu ứng", "75": "nhận hiệu ứng",
           "77": "đánh bù nhìn", "78": "đánh quái", "-58": "hiệu ứng tại chỗ", "-59": "hiệu ứng tại chỗ", "-73": "gói -73"}

# Usages that are not a literal call: the offline server (MobBrain, like Java Service.npcAttackMe) sends attack effect -1,
# so the client plays effect 59 (Mob.cs: idSkill_atk != -1 ? idSkill_atk : 59) for every non-boss mob hit.
RULES = [{"id": 59, "file": "Assets/Scripts/Gameplay/Mobs/Mob.cs", "method": "updateMobAttack", "line": 0,
          "how": "tại người/quái bị đánh, lặp 1 lần", "when": "server gửi idSkill_atk = -1 (mọi đòn đánh thường của quái)",
          "who": [{"kind": "allMobs"}]}]


def split_args(text):
    args, depth, cur = [], 0, ""
    for ch in text:
        if ch in "([":
            depth += 1
        elif ch in ")]":
            depth -= 1
        if ch == "," and depth == 0:
            args.append(cur.strip())
            cur = ""
        else:
            cur += ch
    if cur.strip():
        args.append(cur.strip())
    return args


def describe(kind, args):
    if kind == "addServerEffectWithTime":
        target = "bám theo nhân vật" if len(args) == 3 else "tại một điểm"
        return f"{target}, kéo dài {args[-1]} giây"
    if len(args) == 3:
        return f"bám theo nhân vật ({args[1]}), lặp {args[2]} lần"
    direction = ", lật theo hướng" if len(args) == 5 else ""
    return f"tại điểm ({args[1]}, {args[2]}), lặp {args[3]} lần{direction}"


class Data:
    """Name lookups from the wiki database."""

    def __init__(self):
        c = json.load(open(DB, encoding="utf-8"))["collections"]
        self.mobs = {str(m["id"]): m for m in c.get("mobs", [])}
        self.effect_types = {}
        for e in c.get("effects", []):
            self.effect_types.setdefault(str(e.get("type")), []).append(e["name"])

    def mob(self, mid):
        m = self.mobs.get(mid)
        return {"kind": "mob", "id": int(mid), "name": m["name"] if m else f"quái #{mid}",
                "level": m.get("level") if m else None, "boss": bool(m and m.get("boss"))}


def resolve(conditions, data):
    """Turns the enclosing conditions into who/what triggers the effect."""
    who = []
    # mob ids: only the innermost condition naming mobs (outer ones are often wider gates listing other mobs)
    mob_conds = [c for c in conditions if re.search(r"(?:mobTemplateId|templateId|idBoss)\s*==\s*\d+", c)]
    if mob_conds:
        for mid in re.findall(r"(?:mobTemplateId|templateId|idBoss)\s*==\s*(\d+)", mob_conds[-1]):
            who.append(data.mob(mid))
    for cond in conditions:
        for t in re.findall(r"template\.type\s*==\s*(\d+)", cond):
            who.append({"kind": "status", "type": int(t), "names": data.effect_types.get(t, [])})
        for s in re.findall(r"getSys\(\)\s*==\s*(\d)", cond):
            who.append({"kind": "element", "name": ELEMENTS.get(s, s)})
        if re.search(r"isWolf|isHaveWolf|vitaWolf", cond):
            who.append({"kind": "mount", "name": "Sói (thú cưỡi)"})
        if re.search(r"isMoto|isHaveMoto|isNewMount|isHaveNewMount", cond):
            who.append({"kind": "mount", "name": "Xe máy / thú cưỡi mới"})
        if "moveFast" in cond:
            who.append({"kind": "other", "name": "Nhân vật dịch chuyển nhanh (kỹ năng lướt)"})
    seen, out = set(), []
    for w in who:
        key = json.dumps(w, sort_keys=True, ensure_ascii=False)
        if key not in seen:
            seen.add(key)
            out.append(w)
    return out


def scan(data):
    fixed, dynamic = {}, []
    for base in SOURCES:
        for folder, _, files in os.walk(base):
            for name in files:
                if not name.endswith(".cs"):
                    continue
                path = os.path.join(folder, name)
                rel = os.path.relpath(path, ROOT).replace("\\", "/")
                lines = open(path, encoding="utf-8", errors="replace").read().split("\n")
                # stack of open blocks: [condition or None, switch expression or None, current case labels]
                stack, pending, method, single = [], None, "?", None
                for i, line in enumerate(lines):
                    code = line.split("//")[0]
                    m = METHOD.match(code)
                    if m:
                        method = m.group(2)
                    h = HEADER.match(code)
                    case = CASE.match(code)
                    if case and stack and stack[-1][1] is not None:
                        top = stack[-1]
                        # consecutive case labels fall through together
                        prev = lines[i - 1].strip() if i else ""
                        top[2] = (top[2] if prev.startswith("case ") else []) + [case.group(1).strip()]
                    call = CALL.search(code)
                    if call and "static void" not in code:
                        conds = []
                        for cond, sw, cases in stack:
                            if cond:
                                conds.append(cond)
                            if sw is not None and cases:
                                conds.append(" / ".join(f"{sw} == {c}" for c in cases))
                        if single:
                            conds.append(single)
                        args = split_args(call.group(2))
                        use = {"file": rel, "line": i + 1, "method": method, "how": describe(call.group(1), args),
                               "when": " › ".join(conds[-3:]), "who": resolve(conds, data)}
                        if rel.endswith("Controller.cs"):
                            for c in conds:
                                mm = re.search(r"== (-?\d+)$", c)
                                if mm and mm.group(1) in PACKETS and "switch" not in c:
                                    use["who"].append({"kind": "packet", "name": f"Gói {mm.group(1)}: {PACKETS[mm.group(1)]}"})
                                    break
                        if re.fullmatch(r"\d+", args[0]):
                            fixed.setdefault(int(args[0]), []).append(use)
                        else:
                            use["idExpr"] = args[0]
                            dynamic.append(use)
                    # a header without a brace applies to the next statement only
                    if h and "{" not in code:
                        pending = h.group(1).strip()
                        single = None
                        continue
                    opens, closes = code.count("{"), code.count("}")
                    for _ in range(opens):
                        sw = None
                        if pending and pending.startswith("switch"):
                            sw = re.match(r"switch\s*\((.*)\)", pending).group(1)
                            stack.append([None, sw, []])
                        else:
                            stack.append([pending, None, []])
                        pending = None
                    for _ in range(closes):
                        if stack:
                            stack.pop()
                    if code.strip() and not h and opens == 0 and closes == 0:
                        single = pending if pending and not pending.startswith("switch") else None
                        pending = None
                    elif opens == 0 and closes == 0:
                        pass
                    if not (code.strip() and pending):
                        single = None if not pending else single
    return fixed, dynamic


def main():
    data = Data()
    fixed, dynamic = scan(data)
    for rule in RULES:
        rule = dict(rule)
        fixed.setdefault(rule.pop("id"), []).insert(0, rule)
    mobs = [{"id": int(k), "name": m["name"], "level": m.get("level")} for k, m in data.mobs.items() if not m.get("boss")]
    out = {"byId": {str(k): v for k, v in sorted(fixed.items())}, "dynamic": dynamic,
           "allMobs": sorted(mobs, key=lambda m: (m["level"] or 0, m["id"]))}
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print(f"{len(fixed)} ids, {sum(len(v) for v in fixed.values())} uses, {len(dynamic)} dynamic -> {OUT}")


if __name__ == "__main__":
    main()
