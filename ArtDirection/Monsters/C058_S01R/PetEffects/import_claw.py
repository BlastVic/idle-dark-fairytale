"""Derive the book dragon claw without modifying original Spine assets."""
from pathlib import Path
import json, shutil
root = Path(__file__).resolve().parents[4]
src = root.parent / 'Spine/effect/diaochan_skill_beaten_2'
dest = root / 'Assets/DarkFairytale/Companions/PetCindrel/Effects/ClawArc'
data = json.loads((src / 'diaochan_skill_beaten_2.json').read_text())
slots = {'dao3', 'dao3_add', 'dao4', 'dao4_add'}
bones = slots | {'root', 'effect'}
data['slots'] = [s for s in data['slots'] if s['name'] in slots]
data['bones'] = [b for b in data['bones'] if b['name'] in bones]
for skin in data['skins']:
    skin['attachments'] = {k:v for k,v in skin['attachments'].items() if k in slots}
anim = data['animations']['texiao']
for kind, keep in [('slots',slots),('bones',bones)]:
    anim[kind] = {k:v for k,v in anim[kind].items() if k in keep}
    for timelines in anim[kind].values():
        for frames in timelines.values():
            for frame in frames:
                frame['time'] = round(max(0,frame.get('time',0)-.3),4)
(dest / 'ClawArc.json').write_text(json.dumps(data,ensure_ascii=False,indent=2))
shutil.copy2(src / 'diaochan_skill_beaten_2.atlas',dest / 'ClawArc.atlas.txt')
shutil.copy2(src / 'diaochan_skill_beaten_2.png',dest / 'diaochan_skill_beaten_2.png')
print('Only four dao claw slots retained; cat, impact burst and particles removed. Duration 0.3s.')
