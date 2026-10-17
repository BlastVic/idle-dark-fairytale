from pathlib import Path
from PIL import Image
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[5]
BASE=Path(__file__).resolve().parent
SRC=BASE/'RigReference'
parts=json.loads((SRC/'parts.json').read_text())
for pet in ['PetShadowWolf','PetGhostWolf']:
 out=ROOT/'Assets/DarkFairytale/Companions'/pet/'Spine';out.mkdir(parents=True,exist_ok=True)
 local=BASE/pet; (local/'Parts').mkdir(exist_ok=True)
 sheets=[Image.open(local/f'painted-{i}.png').convert('RGBA') for i in range(2)]
 imgs=[]
 for k,n in enumerate(parts):
  orig=Image.open(SRC/'images'/(n+'.png')).convert('RGBA')
  if n=='shadow': im=Image.new('RGBA',orig.size)
  else:
   sheet=sheets[k//16];cw,ch=sheet.width//4,sheet.height//4;i=k%16
   cell=sheet.crop((i%4*cw,i//4*ch,(i%4+1)*cw,(i//4+1)*ch))
   bb=cell.getchannel('A').point(lambda a:255 if a>35 else 0).getbbox();assert bb,n
   # Register painted extents to original mesh-reference extents, retaining generated RGBA.
   ref=Image.open(SRC/f'reference-{k//16}.png').convert('RGBA').crop((i%4*384,i//4*384,(i%4+1)*384,(i//4+1)*384))
   rb=ref.getchannel('A').point(lambda a:255 if a>35 else 0).getbbox()
   ow,oh=orig.size; scale=330/max(ow/2,oh/2)
   # Source/images are the existing 2x production canvases. Output one original-resolution canvas.
   w,h=round(ow/2),round(oh/2);rw,rh=round(w*330/max(w,h)),round(h*330/max(w,h));rx,ry=(384-rw)//2,(384-rh)//2
   target=(round((rb[0]-rx)/rw*w),round((rb[1]-ry)/rh*h),round((rb[2]-rx)/rw*w),round((rb[3]-ry)/rh*h))
   piece=cell.crop(bb).resize((max(1,target[2]-target[0]),max(1,target[3]-target[1])),Image.Resampling.LANCZOS)
   im=Image.new('RGBA',(w,h));im.paste(piece,target[:2])
   if pet=='PetGhostWolf':im.putalpha(im.getchannel('A').point(lambda a:round(a*.78)))
  im.save(local/'Parts'/(n+'.png'));imgs.append((n,im))
 # Tight deterministic shelves, with 4px transparent spacing. Preserve full-canvas regions.
 x=y=row=0; positions=[]
 for n,im in imgs:
  if x+im.width+8>2048:x=0;y+=row+8;row=0
  positions.append((n,im,x+4,y+4));x+=im.width+8;row=max(row,im.height)
 height=1
 while height<y+row+8:height*=2
 atlas=Image.new('RGBA',(2048,height));lines=[pet+'.png',f'size: 2048,{height}','format: RGBA8888','filter: Linear,Linear','repeat: none']
 for n,im,x,y in positions:
  atlas.paste(im,(x,y));lines += [n,'  rotate: false',f'  xy: {x}, {y}',f'  size: {im.width}, {im.height}',f'  orig: {im.width}, {im.height}','  offset: 0, 0','  index: -1']
 atlas.save(out/(pet+'.png'));(out/(pet+'.atlas.txt')).write_text('\n'.join(lines)+'\n')
 source=ROOT/'Assets/DarkFairytale/Monsters/MobHexmaw/MobHexmaw.json';shutil.copy2(source,out/(pet+'.json'))
 (local/'rig-source-sha256.txt').write_text(hashlib.sha256(source.read_bytes()).hexdigest()+'\n')
 print(pet,atlas.size,'rig byte-identical')
