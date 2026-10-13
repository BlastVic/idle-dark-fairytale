"""Independent audit of the saved chapter design. Run from the repository root."""
import json
import math
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
config = json.loads((ROOT / "Docs/Balance/chapter01.json").read_text())
monsters = {m["key"]: m for m in config["monsters"]}
scene = (ROOT / "Assets/Scenes/Gameplay.unity").read_text()
blocks = re.split(r"^  - levelKey: ", scene, flags=re.M)[1:]


def stat(text, key):
    body = re.search(r"^  baseStat:\n(.*?)(?=^  \w)", text, re.M | re.S)[1]
    return float(re.search(r"^    " + key + r": ([\d.]+)", body, re.M)[1])


def waves(block):
    body = block.split("    waves:\n", 1)[1].split("    crystalsMin:", 1)[0]
    return [re.findall(r"^      - (.+)$", w, re.M) for w in body.split("    - enemies:\n")[1:]]


first = dict(zip(monsters, [1, 1, 2, 2, 3, 3, 4, 5]))
for m in monsters.values():
    text = (ROOT / m["path"]).read_text()
    for design, serialized in [("hp", "hpNow"), ("hp", "hpMax"), ("damage", "dmg"), ("crit", "crit"), ("defense", "def"), ("pierce", "pierce"), ("critDamage", "critDmg"), ("hpPercent", "hpPerc"), ("damagePercent", "dmgPerc"), ("defensePercent", "defPerc"), ("attackSpeed", "atkSpd"), ("block", "block"), ("absorb", "absorb")]:
        assert stat(text, serialized) == m[design], (m["key"], serialized)
    for design, serialized in [("interval", "waitBetweenAttacks"), ("experience", "expGiven"), ("interruptChance", "chanceInterruptAttack")]:
        assert math.isclose(float(re.search(r"^  " + serialized + r": ([\d.]+)", text, re.M)[1]), m[design], rel_tol=1e-6)
    assert m["drops"] and all(0 < item["m_ChanceDrop"] <= 1 for item in m["drops"])
    assert m["hp"] > 0 and m["damage"] > 0 and m["interval"] > 0
    animations = json.loads(next((ROOT / m["path"]).parent.glob(m["key"] + ".json")).read_text())["animations"]
    hits = [e["time"] for e in animations["Attack1"]["events"] if e["name"] == "OnHit"]
    assert len(hits) == 1 and hits[0] / .9 < m["interval"] * .9, "Attack could restart before hit"

summaries = []
for i, node in enumerate(config["nodes"]):
    assert blocks[i].splitlines()[0] == node["key"]
    rewards = node["rewards"]
    for key in ["crystalsMin", "crystalsMax", "chestSeconds", "chestGrade"]:
        assert float(re.search(r"^    " + key + r": ([\d.]+)", blocks[i], re.M)[1]) == rewards[key]
    for key in ["normal", "gold", "diamond"]:
        assert rewards[key] and all(0 < item["m_ChanceDrop"] <= 1 for item in rewards[key])
    actual = waves(blocks[i])
    if node["index"] == 5:
        assert all("MobDrownedMoon" not in wave for wave in actual), "Node 5 must not include MobDrownedMoon"
    assert actual == [w["enemies"] for w in node["waves"]]
    for wave in actual:
        assert 1 <= len(wave) <= 3
        assert all(k in monsters and first[k] <= i + 1 for k in wave)
    if i in [2, 4]:
        key = "MobDrownedKing" if i == 2 else "MobHollowWolf"
        assert actual[-1] == [key]
        assert sum(w.count(key) for b in blocks for w in waves(b)) == 1
    flat = [monsters[k] for w in actual for k in w]
    summaries.append({"node": i + 1, "key": node["key"], "waves": actual,
                      "count": len(flat), "hp": sum(m["hp"] for m in flat),
                      "experience": sum(m["experience"] for m in flat),
                      "peak_raw_dps": round(max(sum(monsters[k]["damage"] / monsters[k]["interval"] * (1 + monsters[k]["crit"] / 200) for k in w) for w in actual), 2)})
for key, expected in first.items():
    assert next(n["index"] for n in config["nodes"] if any(key in w["enemies"] for w in n["waves"])) == expected
assert all(a["hp"] < b["hp"] and a["damage"] < b["damage"] for a, b in zip(config["monsters"], config["monsters"][1:]))

# Reference the initial import, before art-preview monster replacements.
original = subprocess.check_output(["git", "show", "b759ee1:Assets/Scenes/MainScenes/Gameplay.unity"], cwd=ROOT, text=True)
old_blocks = re.split(r"^  - levelKey: ", original, flags=re.M)[1:]
baseline = []
for key in ["Khorasan Ruins I", "Khorasan Ruins II", "Mount Zion", "The Great Cave Entrance", "The Cave of Dry Bones"]:
    block = next(b for b in old_blocks if b.splitlines()[0] == key)
    hp = xp = count = 0
    for wave in waves(block):
        for name in wave:
            path = next((ROOT / "Assets/Prefab").rglob(name + ".prefab"))
            text = subprocess.check_output(["git", "show", "b759ee1:" + str(path.relative_to(ROOT))], cwd=ROOT, text=True)
            hp += stat(text, "hpMax")
            xp += float(re.search(r"^  expGiven: ([\d.]+)", text, re.M)[1])
            count += 1
    baseline.append({"key": key, "hp": hp, "experience": xp, "count": count})

report = {"status": "PASS", "original_commit": "b759ee1", "original_formal_nodes": baseline,
          "chapter01": summaries,
          "limitations": ["Raw DPS ignores defense, interrupts, entrance time, target selection and player actions.",
                          "Original test01/test02 are excluded from the formal reference curve; they remain the new chapter's node keys.",
                          "Unity smoke validation checks integration with controlled kills, not natural-clear win rates."]}
(ROOT / "Docs/Balance/chapter01-audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
print(json.dumps(report, ensure_ascii=False, indent=2))
