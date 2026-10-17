"""Repack seven matching original frames into the broken second atlas page.
No painting or resampling. Original source archive remains unchanged.
"""
from pathlib import Path
from PIL import Image
import hashlib, json, shutil
here=Path(__file__).resolve().parent
root=here.parents[3]
folder=root/'Assets/DarkFairytale/Companions/PetCindrel/Effects/EmberBreath'
backup=here/'repaired-breath-source'
backup.mkdir(exist_ok=True)
source=root.parent/'Spine/effect/S_S_zhouyu_skill_beaten_1/images'
lines=(folder/'EmberBreath.atlas.txt').read_text().splitlines()
page=Image.new('RGBA',(1211,610))
manifest={}
for i in range(lines.index('S_S_zhouyu_skill_12.png')+1,len(lines)):
 name=lines[i]
 if not name.startswith('effect/'):continue
 d=dict(x.strip().split(': ',1) for x in lines[i+1:i+7])
 x,y=map(int,d['xy'].split(','));w,h=map(int,d['size'].split(','))
 ox,oy=map(int,d['offset'].split(','));ow,oh=map(int,d['orig'].split(','))
 saved=backup/(Path(name).name+'.png')
 if not saved.exists():shutil.copy2(source/(name+'.png'),saved)
 im=Image.open(saved).convert('RGBA');assert im.size==(ow,oh),(name,im.size,(ow,oh))
 tile=im.crop((ox,oh-oy-h,ox+w,oh-oy))
 if d['rotate']=='true':tile=tile.transpose(Image.Transpose.ROTATE_90)
 page.paste(tile,(x,y))
 manifest[name]={'source':'S_S_zhouyu_skill_beaten_1/images/'+name+'.png','sha256':hashlib.sha256(saved.read_bytes()).hexdigest()}
page.save(folder/'S_S_zhouyu_skill_12.png')
(backup/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Repacked',len(manifest),'regions without resampling; original atlas coordinates preserved.')
