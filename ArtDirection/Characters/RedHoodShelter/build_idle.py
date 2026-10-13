"""Author a Spine 3.8 seated idle. No image generation or pixel editing.

Coordinates are measured in the 1254 px transparent source sprite. The weighted
mesh preserves the illustration, with separate local influences for face, hair,
cup, shoulders and cloth. Feet and the grounded hem remain anchored.
Run explicitly; Unity's separate installer never runs at import or in builds.
"""
import json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/DarkFairytale/UI/Shelter/RedHoodIdle'
N = 1254
bones, positions, indices = [], {}, {}

def bone(name, parent, px, py):
    world = (px-N/2, N-py)
    base = positions[parent] if parent else (0, 0)
    indices[name] = len(bones)
    positions[name] = world
    b = dict(name=name, x=round(world[0]-base[0], 4), y=round(world[1]-base[1], 4))
    if parent: b['parent'] = parent
    bones.append(b)

bone('root', None, N/2, N)
bone('pelvis_anchor', 'root', 540, 635)
bone('breathing_chest', 'pelvis_anchor', 565, 445)
bone('neck', 'breathing_chest', 644, 288)
bone('head', 'neck', 648, 212)
bone('hood_tip', 'head', 527, 175)
bone('hair_near', 'head', 580, 243)
bone('hair_near_tip', 'hair_near', 612, 330)
bone('hair_far', 'head', 721, 245)
bone('hair_far_tip', 'hair_far', 713, 312)
bone('shoulder', 'breathing_chest', 479, 410)
bone('hands_and_cup', 'breathing_chest', 753, 375)
bone('cloak_shoulder', 'breathing_chest', 425, 334)
bone('cloak_left', 'root', 320, 620)
bone('cloak_left_hem', 'root', 369, 855)
bone('cloak_right_hem', 'root', 934, 879)
bone('steam_origin', 'hands_and_cup', 765, 315)
for i in range(3):
    bone('steam_%d'%i, 'steam_origin', 765, 315)

def smooth(a, b, v):
    t = min(1, max(0, (v-a)/(b-a)))
    return t*t*(3-2*t)

def weights(x,y):
    chest = 1-smooth(470,660,y)
    w={'breathing_chest':chest,'pelvis_anchor':1-chest}
    def overlay(name, amount):
        amount=min(1,max(0,amount))
        for k in w: w[k]*=1-amount
        w[name]=w.get(name,0)+amount
    # The whole face has one rigid head influence, avoiding facial rubberiness.
    overlay('head',(1-smooth(285,400,y))*smooth(375,470,x))
    overlay('hood_tip',.8*math.exp(-((x-445)/75)**2-((y-191)/86)**2))
    overlay('hair_near',.7*math.exp(-((x-578)/24)**2-((y-267)/44)**2))
    overlay('hair_near_tip',.85*math.exp(-((x-616)/24)**2-((y-342)/58)**2))
    overlay('hair_far',.65*math.exp(-((x-724)/27)**2-((y-255)/37)**2))
    overlay('hair_far_tip',.8*math.exp(-((x-716)/24)**2-((y-310)/37)**2))
    overlay('shoulder',.6*math.exp(-((x-486)/80)**2-((y-432)/66)**2))
    overlay('hands_and_cup',smooth(627,699,x)*smooth(273,308,y)*(1-smooth(450,560,y)))
    overlay('cloak_shoulder',.9*(1-smooth(397,479,x))*smooth(205,320,y)*(1-smooth(430,540,y)))
    overlay('cloak_left',.85*(1-smooth(350,443,x))*smooth(400,520,y)*(1-smooth(730,830,y)))
    overlay('cloak_left_hem',.9*(1-smooth(457,545,x))*smooth(740,839,y)*(1-smooth(1070,1170,y)))
    overlay('cloak_right_hem',.9*smooth(875,985,x)*smooth(720,855,y)*(1-smooth(1090,1170,y)))
    overlay('root',smooth(650,760,y)*smooth(470,555,x)*(1-smooth(891,964,x)))
    overlay('root',smooth(1140,1210,y))
    # Four influences suffice at joins, with normalized sum exactly one.
    entries=sorted(((k,v) for k,v in w.items() if v>1e-5),key=lambda kv:-kv[1])[:4]
    total=sum(v for _,v in entries)
    return [(k,v/total) for k,v in entries]

def vertex(x,y,ws=None):
    ws=ws or weights(x,y)
    v=[len(ws)]
    for name,w in ws:
        bx,by=positions[name]
        v += [indices[name],round(x-N/2-bx,5),round(N-y-by,5),round(w,8)]
    return v

# Regular triangulation is deliberately fine enough for hair/cup boundaries.
uv, verts, triangles=[],[],[]
cols=rows=64
for iy in range(rows+1):
    for ix in range(cols+1):
        x,y=ix*N/cols,iy*N/rows
        uv += [round(ix/cols,6),round(iy/rows,6)]
        verts += vertex(x,y)
        if ix<cols and iy<rows:
            a=iy*(cols+1)+ix;b=a+1;c=a+cols+1;d=c+1
            triangles += [a,c,b,b,c,d]
body=dict(type='mesh',path='body',uvs=uv,triangles=triangles,vertices=verts,width=N,height=N)

# Use only the two closed-eye patches from the generated blink frame, never
# swap the entire character. Both patches share the head's rigid transform.
eye_polygons=[[(596,151),(626,152),(651,163),(661,183),(646,202),(617,199),(597,185)],
              [(681,182),(705,181),(722,192),(717,211),(701,225),(682,218),(676,202)]]
ev,eu,et=[],[],[]
offset=0
for poly in eye_polygons:
    for x,y in poly:
        eu += [(x-588)/144,(y-140)/94]
        ev += vertex(x,y,[('head',1)])
    for i in range(1,len(poly)-1): et += [offset,offset+i,offset+i+1]
    offset+=len(poly)
eyes=dict(type='mesh',path='eyes',uvs=eu,vertices=ev,triangles=et,width=144,height=94)

slots=[dict(name='seated_character',bone='root',attachment='body'),
       dict(name='blink',bone='head',attachment='eyes',color='ffffff00')]
attachments={'seated_character':{'body':body},'blink':{'eyes':eyes}}
for i in range(3):
    name='tea_steam_%d'%i
    slots.append(dict(name=name,bone='steam_%d'%i,attachment='steam',color='ffffff00'))
    # Narrow ribbon mesh, independently deformable with its base at the cup.
    su,sv,st=[],[],[]
    for j in range(17):
        for side in range(2):
            su += [side,1-j/16]
            sv += [(side-.5)*64,j/16*210]
        if j<16:
            a=j*2;st += [a,a+2,a+1,a+1,a+2,a+3]
    attachments[name]={'steam':dict(type='mesh',path='steam',uvs=su,vertices=sv,triangles=st,width=310,height=1230)}

tracks={}
def sine(name,kind,amplitude,period,phase=0,axis='angle',bias=0):
    track=tracks.setdefault(name,{}).setdefault(kind,[])
    for f in range(49):
        t=f/4
        v=bias+amplitude*math.sin(2*math.pi*t/period+phase)
        key={'time':t,axis:round(v,5)}
        if kind=='translate': key.setdefault('x',0);key.setdefault('y',0)
        track.append(key)

sine('breathing_chest','translate',3.2,4,axis='y',bias=1.0)
sine('breathing_chest','rotate',.22,4,.5)
sine('neck','rotate',.28,12,.4)
sine('head','rotate',.65,12,1.1)
sine('hood_tip','rotate',.75,4,-.7)
sine('hair_near','rotate',1.05,4,-.8)
sine('hair_near_tip','rotate',1.7,4,-1.15)
sine('hair_far','rotate',.9,4,-1.0)
sine('hair_far_tip','rotate',1.4,4,-1.4)
sine('shoulder','rotate',.35,4,-.25)
sine('hands_and_cup','translate',1.0,4,-.28,axis='y')
sine('hands_and_cup','rotate',.32,6,.5)
sine('cloak_shoulder','rotate',.65,4,-.6)
sine('cloak_left','rotate',.5,6,-.9)
sine('cloak_left_hem','rotate',.7,6,-1.3)
sine('cloak_right_hem','rotate',.6,6,-1.7)

blink=[dict(time=0,color='ffffff00')]
for t in [2.6,6.95,7.38,10.7]:
    blink += [dict(time=t,color='ffffff00'),dict(time=round(t+.055,3),color='ffffffff'),
              dict(time=round(t+.125,3),color='ffffffff'),dict(time=round(t+.24,3),color='ffffff00')]
blink.append(dict(time=12,color='ffffff00'))
slot_tracks={'blink':{'color':blink}}
deform={}
for i in range(3):
    name='steam_%d'%i;slot='tea_steam_%d'%i
    translate=[];scale=[];rotate=[];color=[];meshkeys=[]
    # An invisible reset at every 4-second lifetime, staggered across ribbons.
    for f in range(145):
        t=f/12;age=(t+i*4/3)%4;u=age/4
        opacity=.95*math.sin(math.pi*u)**1.4
        translate.append(dict(time=round(t,5),x=round((i-1)*5+13*math.sin(u*4+i)*u,4),y=round(8*u,4)))
        scale.append(dict(time=round(t,5),x=round(.62+.52*u,4),y=round(.58+.58*u,4)))
        rotate.append(dict(time=round(t,5),angle=round(4*math.sin(u*5+i),4)))
        color.append(dict(time=round(t,5),color='ffffff%02x'%round(opacity*255)))
    tracks[name]=dict(translate=translate,scale=scale,rotate=rotate)
    slot_tracks[slot]=dict(color=color)
    for f in range(25):
        t=f*.5;delta=[]
        for j in range(17):
            a=j/16
            dx=7*a*math.sin(a*7-2*math.pi*t/4+i)
            for _ in range(2):delta += [round(dx,4),0]
        meshkeys.append(dict(time=t,vertices=delta))
    deform[slot]={'steam':meshkeys}

rig=dict(skeleton=dict(hash='redhood-shelter-idle-v1',spine='3.8.99',x=-N/2,y=0,width=N,height=N,images='./images/',audio=''),
         bones=bones,slots=slots,skins=[dict(name='default',attachments=attachments)],
         animations={'Idle':dict(bones=tracks,slots=slot_tracks,deform={'default':deform})})
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'RedHoodShelter.json').write_text(json.dumps(rig,separators=(',',':'))+'\n')
(Path(__file__).parent/'rig-summary.json').write_text(json.dumps(dict(bones=len(bones),body_vertices=len(uv)//2,body_triangles=len(triangles)//3,duration=12,animation='Idle',pinned=['feet','pelvis','grounded cloak'],source_size=N),indent=2)+'\n')
print('Authored',len(bones),'bones,',len(uv)//2,'weighted vertices, 12 second Idle')
