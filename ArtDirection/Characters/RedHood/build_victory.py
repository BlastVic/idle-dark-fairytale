"""Explicit victory and relaxed idle authoring. Existing combat clips are preserved."""
import copy,json,math
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
ASSETS=ROOT/'Assets/RedHoodPrototype'
T=[0,.5,1.3,1.55,1.8,2.2,2.65,3]

def sample(v,t):
    for i in range(len(T)-1):
        if t<=T[i+1]+1e-8:
            h=T[i+1]-T[i];u=max(0,min(1,(t-T[i])/h))
            def m(k):
                if k==0 or k==len(T)-1:return 0
                h0,h1=T[k]-T[k-1],T[k+1]-T[k]
                a,b=(v[k]-v[k-1])/h0,(v[k+1]-v[k])/h1
                return 0 if a*b<=0 else 3*(h0+h1)/((2*h1+h0)/a+(h1+2*h0)/b)
            return (2*u**3-3*u*u+1)*v[i]+(u**3-2*u*u+u)*h*m(i)+(-2*u**3+3*u*u)*v[i+1]+(u**3-u*u)*h*m(i+1)
    return v[-1]

def build(data):
    # A second expression slot permits a short eyelid dissolve without a head pop.
    if not any(s['name']=='head_rest' for s in data['slots']):
        data['slots'].append({'name':'head_rest','bone':'head','color':'ffffff00'})
    skin=data['skins'][0]['attachments'];head=copy.deepcopy(skin['head']['head']);head['path']='head_closed'
    skin['head_rest']={'head_closed':head}
    a={'bones':{},'slots':{}};times=sorted(set([round(i/120,6) for i in range(361)]+T))
    def tl(bone,kind,fields,values):
        a['bones'].setdefault(bone,{})[kind]=[dict(time=t,**{f:round(sample(v,t),5) for f,v in zip(fields,values)}) for t in times]
    def rot(n,v):tl(n,'rotate',['angle'],[v])
    hx=[0,0,-54,-84,-105,-110,-110,-110];hy=[0,0,5,10,16,20,20,20]
    torso=[0,0,2,3,4,5,6,6]
    tl('hips','translate',['x','y'],[hx,hy]);rot('torso',torso)
    tl('torso','translate',['x','y'],[[0]*8,[0,0,-2,-3,-3,-2,0,0]])
    rot('head',[0,0,-3,-8,-12,-12,-12,-12])
    tl('upper_arm','translate',['x','y'],[[0]*8,[0,0,-9,-10,-10,-9,-8,-8]])
    # Author the world-space right-hand position to keep the original scythe safe above ground.
    gx=[109,109,26,-24,-57,-69,-69,-69];gy=[213,213,211,207,201,191,187.99,187.99]
    wa=[0,0,-7,-12,-17,-20,-20,-20]
    a['bones']['weapon']={'translate':[],'rotate':[]}
    for t in times:
        r=math.radians(sample(torso,t));ty=sample([0,0,-2,-3,-3,-2,0,0],t)
        x=sample(gx,t)-(10+sample(hx,t));y=sample(gy,t)-(237+sample(hy,t)+ty)
        a['bones']['weapon']['translate'].append({'time':t,'x':round(math.cos(r)*x+math.sin(r)*y-99,5),'y':round(-math.sin(r)*x+math.cos(r)*y+24,5)})
        a['bones']['weapon']['rotate'].append({'time':t,'angle':round(sample(wa,t)-sample(torso,t),5)})
    # Plant -> lift -> travel -> lower -> plant. Horizontal travel only in the air.
    feet=[]
    def ease(u):u=max(0,min(1,u));return u*u*(3-2*u)
    for t in times:
        x=-195*ease((t-1.03)/.72)
        y=18*ease((t-.85)/.20) if t<1.05 else 18*(1-ease((t-1.74)/.33))
        feet.append({'time':t,'x':round(x,5),'y':round(y,5)})
    a['bones']['near_foot']={'translate':feet}
    # The far leg drawing has a longer authored limb; match its rest length to the standing support.
    tl('far_thigh','scale',['x','y'],[[1,1,.99,.98,.96,.95,.95,.95],[1]*8])
    tl('near_thigh','scale',['x','y'],[[1,1,1.01,1.02,1.04,1.05,1.05,1.05],[1]*8])
    tl('far_thigh','translate',['x','y'],[[0,0,12,21,29,32,32,32],[0]*8])
    tl('near_thigh','translate',['x','y'],[[0,0,-10,-18,-25,-29,-29,-29],[0]*8])
    tl('scythe_projection','scale',['x','y'],[[1,1,1.03,1.06,1.09,1.117,1.117,1.117]]*2)
    tl('far_foot','translate',['x','y'],[[0]*8,[0]*8])
    rot('skirt',[0,0,-2,-3,-4,-3,-1,-1])
    rot('cape_tip',[0]*8)
    rot('cloth_root',[0,0,4,7,10,12,10,10])
    tl('cloth_root','translate',['x','y'],[[0,0,-3,-6,-9,-12,-12,-12],[0,0,4,8,12,15,15,15]])
    rot('cloth_mid',[0,0,-1,-2,-3,-4,-3,-3])
    rot('cloth_back',[0,0,0,1,2,1,0,0])
    tl('cloth_root','scale',['x','y'],[[1]*8,[1]*8])
    for n,v in [('hem_back',[0,0,1,2,1,-1,0,0]),('hem_mid',[0,0,.5,1,.5,-.5,0,0]),('hem_front',[0,0,-.5,-1,-.5,.5,0,0]),('lantern',[0,0,5,2,-4,2,-.5,0])]:rot(n,v)
    # Compress the physical recovery while keeping the later head/exhale beat.
    # Foot lands at 1.207 s; body and weapon settle by 1.468 s.
    for bone,timelines in a['bones'].items():
        if bone=='head':continue
        for keys in timelines.values():
            for key in keys:
                if key['time']>.5:key['time']=round(.5+(key['time']-.5)*.45,6)
    def color(t,v):return {'time':t,'color':'ffffff'+format(round(v*255),'02x')}
    a['slots']={'scythe':{'attachment':[{'time':0,'name':'scythe'}],'color':[color(0,1)]},
        'scythe_return':{'attachment':[{'time':0,'name':None}],'color':[color(0,0)]},
        'head':{'color':[color(t,v) for t,v in [(0,1),(1.65,1),(1.78,0),(2.18,0),(2.35,0),(3,0)]]},
        'head_rest':{'attachment':[{'time':0,'name':'head_closed'}],'color':[color(t,v) for t,v in [(0,0),(1.65,0),(1.78,1),(2.18,1),(2.35,1),(3,1)]]}}
    a['drawOrder']=[{'time':0}]
    idle=copy.deepcopy(a)
    for bone,timelines in idle['bones'].items():
        for kind,keys in timelines.items():
            end={k:v for k,v in keys[-1].items() if k!='time'}
            timelines[kind]=[dict(time=0,**end),dict(time=3.6,**end)]
    idle['slots']={n:{kind:[dict(keys[-1],time=0)] for kind,keys in tls.items()} for n,tls in a['slots'].items()}
    # Low-amplitude breathing without moving the pelvis or feet.
    for n,amp in [('head',.3),('cloth_back',.5),('lantern',.7)]:
        base=idle['bones'][n]['rotate'][0]['angle']
        idle['bones'][n]['rotate']=[{'time':round(i*.03,5),'angle':round(base+amp*(1-math.cos(2*math.pi*i/120))*.5,5)} for i in range(121)]
    data['animations']['Victory']=a;data['animations']['VictoryIdle']=idle

if __name__=='__main__':
    # Uniform resampling and atlas packing only; expression art comes from ImageGen.
    im=Image.open(ROOT/'ArtDirection/Characters/RedHood/Expressions/head-closed-source.png').convert('RGBA').resize((163,146),Image.Resampling.LANCZOS)
    im.save(ASSETS/'Parts/head_closed.png')
    atlas=Image.open(ASSETS/'RedHood.png');atlas.paste(im,(4,256));atlas.save(ASSETS/'RedHood.png')
    p=ASSETS/'RedHood.atlas.txt';s=p.read_text()
    if '\nhead_closed\n' not in s:s+='head_closed\n  rotate: false\n  xy: 4, 256\n  size: 163, 146\n  orig: 163, 146\n  offset: 0, 0\n  index: -1\n'
    p.write_text(s)
    for p in [ASSETS/'RedHood.json',ROOT/'Assets/DarkFairytale/RedHoodBattle.json']:
        d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
