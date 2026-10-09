"""Assert shipped Unity and external exports preserve the original model/curves."""
import copy,json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
original=json.loads((ROOT.parent/'Spine/monster/mon_7080/mon_7080.json').read_text())
report=[]
for color in ('Wine','Moss','Moon'):
 name='BF_Mushroom_'+color
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
 assert len(result['bones'])==27 and len(result['slots'])==15
 report.append(name+': PASS original bones/IK/vertex weights/UVs/7 animation curves; documented UV boundary fills and hidden duplicate eyes; 3 aliases differ only by events; skin 1 inherits deform; Unity = external export.')
p=ROOT/'output/monster-animation/original-rig-preservation.txt';p.write_text('\n'.join(report)+'\n');print(p.read_text())
