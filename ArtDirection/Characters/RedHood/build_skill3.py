"""Skill3: Victory recovery, right-handed overhead counterclockwise cast, one-second hold, return."""
import json,math
from pathlib import Path
from build_death import value
ROOT=Path(__file__).resolve().parents[3]
RECOVER=1.48;RAISE=.60;HOLD=1.;LOWER=.75;END=5.31
def ease(u):
 u=max(0,min(1,u));return u*u*(3-2*u)
def build(d):
 source=d['animations']['Victory']
 a={'bones':{},'slots':{},'drawOrder':[{'time':0}],'events':[{'time':2.08,'name':'OnHit'},{'time':END,'name':'OnComplete'}]}
 times=sorted(set([round(i/120,6) for i in range(656) if i/120<END]+[0,1.48,2.08,3.08,3.83,END]))
 for bone,tls in source['bones'].items():
  a['bones'][bone]={}
  for kind,keys in tls.items():
   fields=['angle'] if kind=='rotate' else ['x','y'];default=1 if kind=='scale' else 0
   a['bones'][bone][kind]=[]
   for t in times:
    clock=t if t<RECOVER else max(0,END-t) if t>3.83 else RECOVER
    a['bones'][bone][kind].append(dict(time=t,**{f:round(value(keys,clock,f,default),5) for f in fields}))
 a['bones']['upper_arm']['scale']=[{'time':t,'x':1,'y':1} for t in times]
 for i,t in enumerate(times):
  u=ease((t-RECOVER)/RAISE) if t<=2.08 else 1 if t<=3.08 else 1-ease((t-3.08)/LOWER)
  if u<=0:continue
  # Raise the existing near/right arm. IK always targets the same weapon grip.
  a['bones']['upper_arm']['translate'][i]['y']=a['bones']['upper_arm']['translate'][i]['y']*(1-u)+22*u
  hip=a['bones']['hips']['translate'][i];tor=a['bones']['torso']['translate'][i];angle=a['bones']['torso']['rotate'][i]['angle'];r=math.radians(angle)
  ox=hip['x']+10+tor['x'];oy=153+hip['y']+84+tor['y']
  shoulderX=ox+math.cos(r)*25-math.sin(r)*36
  shoulderY=oy+math.sin(r)*25+math.cos(r)*36
  # Circular hand path: in front of the chest, then counterclockwise overhead.
  key=a['bones']['weapon']['translate'][i]
  localX=99+key['x'];localY=-24+key['y']
  startX=ox+math.cos(r)*localX-math.sin(r)*localY
  startY=oy+math.sin(r)*localX+math.cos(r)*localY
  # Use the moving shoulder to preserve the same right-arm IK chain.
  shoulderY+=a['bones']['upper_arm']['translate'][i]['y']*math.cos(r)
  shoulderX-=a['bones']['upper_arm']['translate'][i]['y']*math.sin(r)
  theta0=math.atan2(startY-shoulderY,startX-shoulderX)
  theta=theta0+(math.pi/2-theta0)*u
  reach=min(96+23.5*u,math.hypot(startX-shoulderX,startY-shoulderY)*(1-u)+119.5*u)
  a['bones']['upper_arm']['scale'][i]['x']=round(1+.25*u,6)
  wx=shoulderX+reach*math.cos(theta);wy=shoulderY+reach*math.sin(theta)
  dx=wx-ox;dy=wy-oy
  key['x']=round(math.cos(r)*dx+math.sin(r)*dy-99,5)
  key['y']=round(-math.sin(r)*dx+math.cos(r)*dy+24,5)
  rot=a['bones']['weapon']['rotate'][i];rot['angle']=round(rot['angle']*(1-u)+(97-angle)*u,5)
  head=a['bones']['head']['rotate'][i];head['angle']=round(head['angle']*(1-u)-angle*u,5)
  a['bones']['lantern']['rotate'][i]['angle']*=1-u
 # Lantern remains gravity-aligned, behind the foreground hand at the apex.
 a['bones']['lantern']['translate']=[]
 for i,t in enumerate(times):
  u=ease((t-RECOVER)/RAISE) if t<=2.08 else 1 if t<=3.08 else 1-ease((t-3.08)/LOWER)
  angle=a['bones']['torso']['rotate'][i]['angle']+a['bones']['weapon']['rotate'][i]['angle']
  r=math.radians(angle)
  # Apex anchor 25px behind and 47px above grip; interpolate local offset.
  tx=math.cos(r)*(-25)+math.sin(r)*47-47
  ty=-math.sin(r)*(-25)+math.cos(r)*47+20
  a['bones']['lantern']['translate'].append({'time':t,'x':round(tx*u,5),'y':round(ty*u,5)})
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
 # Near arm and held weapon must remain in front of the hood when lifted.
 names=[s['name'] for s in d['slots']]
 foreground=['scythe','scythe_return','lantern','upper_arm','forearm','hand']
 reordered=[n for n in names if n not in foreground]+foreground
 offsets=[{'slot':n,'offset':reordered.index(n)-names.index(n)} for n in foreground]
 a['drawOrder']=[{'time':0},{'time':1.48,'offsets':offsets},{'time':3.83}]
 d['animations']['Skill3']=a
if __name__=='__main__':
 for p in [ROOT/'Assets/RedHoodPrototype/RedHood.json',ROOT/'Assets/DarkFairytale/RedHoodBattle.json']:
  d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
