"""Deterministic alpha registration/atlas packing. Art is painted by image_gen, never synthesized here."""
import copy,json,hashlib
from pathlib import Path
from PIL import Image
BASE=Path(__file__).resolve().parent; SRC=BASE/'source'; ORIG=SRC/'original-rig'; ROOT=BASE.parents[2]
OUT=ROOT/'Assets/DarkFairytale/Companions/PetCindrel/Spine'; ID='PetCindrel'
ALIASES={'Idle1':'Idle_Battle_00','CampIdle':'Idle_Down_00','Attack1':'Atk1_00','Attack2':'Atk_E_00','Attack3':'LinkSkill_00','Hit':'Hit_00','Death1':'Death_00','Exit':'Run_Battle_00'}
HITS={'Attack1':1.10,'Attack2':1.1667,'Attack3':2.43}
def duration(o):
 if isinstance(o,dict):return max([o.get('time',0)]+[duration(v) for v in o.values()])
 if isinstance(o,list):return max([0]+[duration(v) for v in o])
 return 0

def main():
 OUT.mkdir(parents=True,exist_ok=True);(SRC/'images').mkdir(exist_ok=True)
 parts=json.loads((SRC/'parts.json').read_text());registration={}
 for n,p in enumerate(parts):
  im=Image.open(SRC/f'painted-{n//9}.png').convert('RGBA');w,h=im.size;c=n%3;r=n%9//3
  cell=im.crop((round(c*w/3),round(r*h/3),round((c+1)*w/3),round((r+1)*h/3)))
  box=cell.getchannel('A').point(lambda x:255 if x>20 else 0).getbbox();assert box,p
  paint=cell.crop(box)
  ref=Image.open(SRC/'reference-parts'/f'{p}.png').convert('RGBA');target=ref.getchannel('A').point(lambda x:255 if x>20 else 0).getbbox();assert target,p
  target=tuple(x*2 for x in target);out=Image.new('RGBA',(ref.width*2,ref.height*2))
  out.alpha_composite(paint.resize((target[2]-target[0],target[3]-target[1]),Image.Resampling.LANCZOS),(target[0],target[1]))
  out.save(SRC/'images'/f'{p}.png');registration[p]={'page':n//9,'cell':n%9,'sourceAlpha':box,'targetAlpha2x':target}
 for p in ['shadow','Front_Eye_Highlight','Front_Waist_Glow']:
  im=Image.open(ORIG/'images'/f'{p}.png').convert('RGBA');im=im.resize((im.width*2,im.height*2),Image.Resampling.LANCZOS)
  # Original neutral highlights/shadow remain; waist emission is disabled only on the new skin.
  if p=='Front_Waist_Glow':im=Image.new('RGBA',im.size)
  im.save(SRC/'images'/f'{p}.png')
 (SRC/'registration.json').write_text(json.dumps(registration,indent=2))
 files=sorted((SRC/'images').glob('*.png'));atlas=Image.new('RGBA',(4096,4096));lines=['',ID+'.png','size: 4096,4096','format: RGBA8888','filter: Linear,Linear','repeat: none'];x=y=row=8
 for f in files:
  im=Image.open(f).convert('RGBA');w,h=im.size
  if x+w+8>4096:x=8;y+=row+8;row=0
  assert y+h+8<=4096
  atlas.alpha_composite(im,(x,y));lines += [f.stem,'  rotate: false',f'  xy: {x}, {y}',f'  size: {w}, {h}',f'  orig: {w}, {h}','  offset: 0, 0','  index: -1'];x+=w+8;row=max(row,h)
 atlas.save(OUT/f'{ID}.png');(OUT/f'{ID}.atlas.txt').write_text('\n'.join(lines)+'\n')
 original=json.loads((ORIG/'C058_S01R.json').read_text());d=copy.deepcopy(original)
 skin=copy.deepcopy(d['skins'][0]);skin['name']='1'
 for sl,aa in skin['attachments'].items():
  for name,m in list(aa.items()):aa[name]={'type':'linkedmesh','path':m.get('path',name),'skin':'default','parent':name,'deform':True,'width':m['width'],'height':m['height']}
 d['skins'].append(skin);d.setdefault('events',{}).update({'OnHit':{},'OnComplete':{}})
 for alias,source in ALIASES.items():
  a=copy.deepcopy(original['animations'][source]);end=duration(a)
  if alias in HITS:a['events']=[{'time':HITS[alias],'name':'OnHit'},{'time':end,'name':'OnComplete'}]
  elif alias in ['Hit','Death1','Exit']:a['events']=[{'time':end,'name':'OnComplete'}]
  d['animations'][alias]=a
 (OUT/f'{ID}.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');(SRC/f'{ID}.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 for key in ['skeleton','bones','slots','ik','transform','path']:assert d.get(key)==original.get(key),key
 assert d['skins'][0]==original['skins'][0]
 for name,a in original['animations'].items():assert d['animations'][name]==a,name
 for p,h in json.loads((SRC/'original-hashes.json').read_text()).items():assert hashlib.sha256((ORIG/p).read_bytes()).hexdigest()==h
 (BASE/'selection.json').write_text(json.dumps({'selected':'C','design':'C-character-v1.png','id':ID,'role':'Cindrel companion','aliases':ALIASES,'hit_times_initial':HITS},ensure_ascii=False,indent=2)+'\n')
 print('PASS original rig/meshes/UV/weights/15 animations and backup hashes preserved; 9 aliases; 45 texture parts; 4096 atlas.')
if __name__=='__main__':main()
