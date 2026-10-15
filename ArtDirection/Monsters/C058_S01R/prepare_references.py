from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
base=Path(__file__).resolve().parent;src=base/'source';orig=src/'original-rig';d=json.loads((orig/'C058_S01R.json').read_text())
parts=[p.stem for p in sorted((orig/'images').glob('*.png')) if p.stem not in ['shadow','Front_Eye_Highlight','Front_Waist_Glow']]
(src/'parts.json').write_text(json.dumps(parts,indent=2))
(src/'original-hashes.json').write_text(json.dumps({str(p.relative_to(orig)):hashlib.sha256(p.read_bytes()).hexdigest() for p in orig.rglob('*') if p.is_file()},indent=2))
for page in range((len(parts)+8)//9):
 sheet=Image.new('RGBA',(1536,1536))
 for j,name in enumerate(parts[page*9:page*9+9]):
  im=Image.open(orig/'images'/f'{name}.png').convert('RGBA');w,h=im.size
  meshes=[a for s in d['skins'][0]['attachments'].values() for k,a in s.items() if a.get('path',k)==name]
  if meshes:
   mask=Image.new('L',im.size);dr=ImageDraw.Draw(mask)
   for m in meshes:
    uv=m['uvs'];tr=m['triangles']
    for t in range(0,len(tr),3):dr.polygon([(uv[tr[t+i]*2]*w,uv[tr[t+i]*2+1]*h) for i in range(3)],fill=255)
   from PIL import ImageChops
   im.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
  (src/'reference-parts').mkdir(exist_ok=True);im.save(src/'reference-parts'/f'{name}.png')
  im.thumbnail((440,440));sheet.alpha_composite(im,(j%3*512+(512-im.width)//2,j//3*512+(512-im.height)//2))
 sheet.save(src/f'reference-{page}.png')
 print(page,parts[page*9:page*9+9])
