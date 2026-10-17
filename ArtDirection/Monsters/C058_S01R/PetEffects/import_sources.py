from pathlib import Path
import json,hashlib,shutil
root=Path(__file__).resolve().parents[4]
src=root.parent/'Spine/effect'
out=root/'Assets/DarkFairytale/Companions/PetCindrel/Effects'
archive=Path(__file__).parent/'source'
names={'EmberBolt':'kongmingdeng_attack_dd','EmberImpact':'kongmingdeng_attack_baodian','ClawArc':'xiaopangmao_atk_tx_1','AshDust':'xuchu_skill_tx_2','EmberBreath':'S_S_zhouyu_skill_1'}
manifest={}
for key,name in names.items():
 p=src/name; dest=out/key; dest.mkdir(parents=True,exist_ok=True);backup=archive/name;backup.mkdir(parents=True,exist_ok=True)
 atlas=(p/(name+'.atlas')).read_text();pages=[line for line in atlas.splitlines() if line.endswith('.png')]
 files=[name+'.json',name+'.atlas',name+'.spine']+pages+[str(f.relative_to(p)) for f in (p/'images').rglob('*.png')]
 manifest[key]={'source':name,'hashes':{}}
 for f in files:
  (backup/f).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p/f,backup/f);manifest[key]['hashes'][f]=hashlib.sha256((p/f).read_bytes()).hexdigest()
 for f in pages:shutil.copy2(p/f,dest/f)
 shutil.copy2(p/(name+'.json'),dest/(key+'.json'))
 (dest/(key+'.atlas.txt')).write_text(atlas)
(Path(__file__).parent/'sources.json').write_text(json.dumps(manifest,indent=2))
print('Imported 5 original animated effects, 6 atlas pages; original sources unchanged')

# The original second EmberBreath page does not match its atlas. Restore the verified matching frames.
import runpy
runpy.run_path(str(Path(__file__).parent/"repair_breath_atlas.py"))

# Keep the approved claw-only replacement when reimporting the full collection.
runpy.run_path(str(Path(__file__).parent/"import_claw.py"))
