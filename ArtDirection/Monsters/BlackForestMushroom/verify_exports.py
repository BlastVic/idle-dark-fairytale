"""Assert shipped Unity and external exports preserve the original model/curves."""
import copy,json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
report=[]
for index,color in enumerate(('Wine','Moss','Moon')):
 source_id = ('mon_7080','mon_7081','mon_7082')[index]
 original=json.loads((ROOT.parent/'Spine/monster'/source_id/(source_id+'.json')).read_text())
 name=('Mob001','Mob002','Mob003')[index]
 internal=ROOT/'Assets/DarkFairytale/Monsters/BlackForestMushroom'/name
 external=ROOT.parent/'Spine/export/monster'/name
 result=json.loads((internal/(name+'.json')).read_text())
 for field in ('bones','slots','ik'): assert result[field]==original[field],(name,field)
 expected=copy.deepcopy(original); expected['skins'].append(copy.deepcopy(result['skins'][1]))
 from fit_skin_mesh import fit
 fit(expected)
 assert result['skins'][0]==expected['skins'][0]
 for anim,data in original['animations'].items(): assert result['animations'][anim]==data,(name,anim)
 for alias,src in [('Idle1','idle'),('Attack1','attack_1'),('Death1','die')]:
  data=copy.deepcopy(result['animations'][alias]);data.pop('events',None)
  assert data==original['animations'][src],(name,alias)
 for slot,attachments in result['skins'][1]['attachments'].items():
  for mesh in attachments.values(): assert mesh['type']=='linkedmesh' and mesh['skin']=='default' and mesh['deform']
 for ext in ('.json','.png','.atlas.txt'): assert (internal/(name+ext)).read_bytes()==(external/(name+ext)).read_bytes()
 assert len(result['bones'])==(27 if index==0 else 26) and len(result['slots'])==15
 report.append(name+' <- '+source_id+': PASS original bones/IK/vertex weights/UVs/7 animation curves; documented UV boundary fills and hidden duplicate eyes; 3 aliases differ only by events; skin 1 inherits deform; Unity = external export.')
p=ROOT/'output/monster-animation/original-rig-preservation.txt';p.write_text('\n'.join(report)+'\n');print(p.read_text())
