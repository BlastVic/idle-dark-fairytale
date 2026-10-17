"""Skill2: Victory recovery, right-handed level cast, one-second hold, return."""
import json,math
from pathlib import Path
from build_death import value
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings').is_dir())
RECOVER=1.48;RAISE=.55;HOLD=1.;LOWER=.55;END=5.06
def ease(u):
 u=max(0,min(1,u));return u*u*(3-2*u)
def build(d):
 source=d['animations']['Victory']
 a={'bones':{},'slots':{},'drawOrder':[{'time':0}],'events':[{'time':2.03,'name':'OnHit'},{'time':END,'name':'OnComplete'}]}
 times=sorted(set([round(i/120,6) for i in range(608) if i/120<END]+[0,1.48,2.03,3.03,3.58,END]))
 for bone,tls in source['bones'].items():
  a['bones'][bone]={}
  for kind,keys in tls.items():
   fields=['angle'] if kind=='rotate' else ['x','y'];default=1 if kind=='scale' else 0
   a['bones'][bone][kind]=[]
   for t in times:
    clock=t if t<RECOVER else max(0,END-t) if t>3.58 else RECOVER
    a['bones'][bone][kind].append(dict(time=t,**{f:round(value(keys,clock,f,default),5) for f in fields}))
 for i,t in enumerate(times):
  u=ease((t-RECOVER)/RAISE) if t<=2.03 else 1 if t<=3.03 else 1-ease((t-3.03)/LOWER)
  if u<=0:continue
  # Raise the existing near/right arm. IK always targets the same weapon grip.
  a['bones']['upper_arm']['translate'][i]['y']*=1-u
  hip=a['bones']['hips']['translate'][i];tor=a['bones']['torso']['translate'][i];angle=a['bones']['torso']['rotate'][i]['angle'];r=math.radians(angle)
  ox=hip['x']+10+tor['x'];oy=153+hip['y']+84+tor['y']
  shoulderX=ox+math.cos(r)*25-math.sin(r)*36
  shoulderY=oy+math.sin(r)*25+math.cos(r)*36
  # 96 px reach for a 96.22 px chain: nearly straight, without IK overextension.
  wx=shoulderX+96;wy=shoulderY
  dx=wx-ox;dy=wy-oy
  targetX=math.cos(r)*dx+math.sin(r)*dy-99
  targetY=-math.sin(r)*dx+math.cos(r)*dy+24
  key=a['bones']['weapon']['translate'][i]
  key['x']=round(key['x']*(1-u)+targetX*u,5);key['y']=round(key['y']*(1-u)+targetY*u,5)
  rot=a['bones']['weapon']['rotate'][i];rot['angle']=round(rot['angle']*(1-u)+(7-angle)*u,5)
  head=a['bones']['head']['rotate'][i];head['angle']=round(head['angle']*(1-u)-angle*u,5)
  a['bones']['lantern']['rotate'][i]['angle']*=1-u
 a['slots']={'scythe':{'attachment':[{'time':0,'name':'scythe'}],'color':[{'time':0,'color':'ffffffff'}]},'scythe_return':{'attachment':[{'time':0,'name':None}],'color':[{'time':0,'color':'ffffff00'}]},'lantern':{'color':[{'time':0,'color':'ffffffff'}]},'head':{'color':[{'time':0,'color':'ffffffff'}]},'head_rest':{'color':[{'time':0,'color':'ffffff00'}]}}
 # Blend residual breathing offsets into the exact Idle start, after feet plant.
 for bone,tls in d['animations']['Idle1']['bones'].items():
  for kind,keys in tls.items():
   track=a['bones'].get(bone,{}).get(kind)
   if not track:continue
   for key in track:
    w=ease((key['time']-(END-.35))/.35)
    for field in ('angle','x','y'):
     if field in key:key[field]=round(key[field]*(1-w)+keys[0].get(field,1 if kind=='scale' else 0)*w,5)
 d['animations']['Skill2']=a
if __name__=='__main__':
 for p in [ROOT/'Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json']:
  d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
