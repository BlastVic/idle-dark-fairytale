"""Author a Spine 3.8 mesh animation; imports into Spine with images/ beside JSON.
No image synthesis or image editing: geometry and animation data only.
"""
import json, math
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / 'Assets/DarkFairytale/UI/StorybookIntro'
OUT.mkdir(parents=True, exist_ok=True)
bones = [{'name': 'root'}, {'name': 'book', 'parent': 'root', 'x': -250}]
slots, attachments, deform, timelines = [], {}, {}, {}
cols, rows = 12, 4

def mesh(name, path, width=500, height=700):
    bones.append({'name': name, 'parent': 'book'})
    slots.append({'name': name, 'bone': name, 'attachment': name})
    uv, vertices, triangles = [], [], []
    for y in range(rows+1):
        for x in range(cols+1):
            # UV inset omits the generated texture's transparent packing margin, without editing the art.
            uv += [(.035+.93*x/cols) if path=='cover' else (.025+.95*x/cols), .01+.97*y/rows]
            vertices += [x/cols*width, (0.5-y/rows)*height]
    for y in range(rows):
        for x in range(cols):
            a=y*(cols+1)+x
            triangles += [a,a+1,a+cols+1,a+1,a+cols+2,a+cols+1]
    m={'type':'mesh','path':path,'uvs':uv,'triangles':triangles,'vertices':vertices,'hull':0,'width':1086,'height':1448}
    attachments[name]={name:m}
    return m

def pose(m, p, curl=0, left=False):
    """Projected hinge with a curved sheet, in unweighted mesh local coordinates."""
    result=[]
    for i in range(0,len(m['vertices']),2):
        x,y=m['vertices'][i:i+2];u=x/500
        nx=x*math.cos(p*math.pi)
        ny=y*(1+0.13*math.sin(p*math.pi)*u)+curl*math.sin(u*math.pi)*math.sin(p*math.pi)
        result += [round(nx-x,3),round(ny-y,3)]
    return result

def turning(name, start, end, path, curl=0):
    m=mesh(name,path)
    frames=[]
    for i in range(33):
        q=i/32;p=q*q*(3-2*q)
        frames.append({'time':round(start+(end-start)*q,4),'vertices':pose(m,p,curl)})
    deform[name]={name:frames}
    return m

# The rigid back board and page stack sit below the independently articulated cover.
mesh('binding','cover',36,704)
bones[-1]['x']=-18
timelines['binding']={'color':[{'color':'77706aff'}]}
mesh('back_board','cover',506,710)
timelines['back_board']={'color':[{'color':'66605cff'}]}
for i in range(3):
    name='page_block_'+str(i);mesh(name,'page',494-i*2,684-i*3)
    bones[-1].update(x=3+i,y=-6+i*3)
    timelines[name]={'color':[{'color':c} for c in ['aaa399ff' if i<2 else 'ddd9ceff']]}
# A rigid, extruded board. Only the leather hinge flexes; the panel stays planar.
cover_keys=[(0,0),(.12,.015),(.28,.10),(.48,.43),(.66,.78),(.85,.992),(.92,.982),(1,1)]
def cover_phase(q):
    for k in range(len(cover_keys)-1):
        x0,y0=cover_keys[k];x1,y1=cover_keys[k+1]
        if q<=x1:
            u=(q-x0)/(x1-x0)
            m0=0 if k==0 else (y1-cover_keys[k-1][1])/(x1-cover_keys[k-1][0])
            m1=0 if k+1==len(cover_keys)-1 else (cover_keys[k+2][1]-y0)/(cover_keys[k+2][0]-x0)
            if k>=4: m0=m1=0
            return (2*u**3-3*u*u+1)*y0+(u**3-2*u*u+u)*(x1-x0)*m0+(-2*u**3+3*u*u)*y1+(u**3-u*u)*(x1-x0)*m1
    return 1

def board_point(x,y,depth,q):
    angle=math.pi*cover_phase(q)
    lift=math.sin(angle)
    # Modest perspective from the lifted free edge, plus a yielding leather hinge.
    z=x*lift+depth*math.cos(angle)
    perspective=1/(1-z/3400)
    hinge=-7*lift
    return ((x*math.cos(angle)-depth*lift+hinge)*perspective,
            (y+z*.045)*perspective)

def rigid_layer(name,path,points,uvs=None,color=None):
    m=mesh(name,path)
    if len(points)==4:
        m['vertices']=[v for x,y,z in points for v in (x,y)]
        m['uvs']=uvs or [.04,.02,.055,.02,.055,.97,.04,.97]
        m['triangles']=[0,1,2,0,2,3]
    else:
        m['vertices']=[v for x,y,z in points for v in (x,y)]
    frames=[]
    for i in range(81):
        q=i/80;values=[]
        for x,y,z in points:
            nx,ny=board_point(x,y,z,q);values += [round(nx-x,3),round(ny-y,3)]
        frames.append({'time':round(1+1.65*q,5),'vertices':values})
    deform[name]={name:frames}
    if color: timelines[name]={'color':color}
    return m

def grid_points(inset=0,depth=0):
    return [(inset+(500-2*inset)*x/cols,(.5-y/rows)*(700-2*inset),depth) for y in range(rows+1) for x in range(cols+1)]

# Shadow remains on the paper, narrowing as the board rises and clears the page.
shadow=mesh('cover_contact_shadow','cover')
shadow_frames=[]
for i in range(41):
    q=i/40;p=cover_phase(q);values=[]
    for x,y in zip(shadow['vertices'][::2],shadow['vertices'][1::2]):
        nx=x*max(.035,math.cos(p*math.pi)) + 12*math.sin(p*math.pi)
        values += [round(nx-x,3),round(-8*math.sin(p*math.pi),3)]
    shadow_frames.append({'time':round(1+1.65*q,5),'vertices':values})
deform['cover_contact_shadow']={'cover_contact_shadow':shadow_frames}
timelines['cover_contact_shadow']={'color':[{'time':0,'color':'15100dcc'},{'time':1.5,'color':'15100d88'},{'time':1.85,'color':'15100d55'},{'time':2.25,'color':'15100d00'}]}
rigid_layer('front_board','cover',grid_points(),color=[{'time':0,'color':'ffffffff'},{'time':1.45,'color':'d9d1c3ff'},{'time':1.86,'color':'74675aff'},{'time':2.4,'color':'a49988ff'}])
# Leather-wrapped board edge: 12 units thick, still visible at the edge-on angle.
rigid_layer('board_fore_edge','cover',[(490,337,0),(490,337,12),(490,-337,12),(490,-337,0)],color=[{'color':'9d8668ff'}])
rigid_layer('board_top_edge','cover',[(6,337,0),(490,337,0),(490,337,12),(6,337,12)],color=[{'color':'baa184ff'}])
rigid_layer('board_bottom_edge','cover',[(6,-337,0),(490,-337,0),(490,-337,12),(6,-337,12)],color=[{'color':'594536ff'}])
rigid_layer('front_lining','page',grid_points(12,12),color=[{'time':0,'color':'ffffff00'},
    {'time':1.86,'color':'80736400'},{'time':1.88,'color':'807364ff'},{'time':2.4,'color':'c6c2b9ff'},{'time':2.65,'color':'d6d1c6ff'}])
# Supple spine strip connects the stationary book block to the moving rigid panel.
hinge=mesh('leather_hinge','cover',20,694)
bones[-1]['x']=-10
frames=[]
for i in range(41):
    q=i/40;lift=math.sin(math.pi*cover_phase(q));values=[]
    for x,y in zip(hinge['vertices'][::2],hinge['vertices'][1::2]):
        values += [round(-7*lift*(x/20),3),round(3*lift*math.sin(x/20*math.pi),3)]
    frames.append({'time':round(1+1.65*q,5),'vertices':values})
deform['leather_hinge']={'leather_hinge':frames}
timelines['leather_hinge']={'color':[{'time':0,'color':'5b4938ff'},{'time':1.8,'color':'88745aff'},{'time':2.65,'color':'5b4938ff'}]}
# Eight closely spaced leaves, retaining the original 4.6-second opening duration.
for i in range(8):
    start=2.12+i*.22
    duration=.82
    name='turning_page_'+str(i);turning(name,start,start+duration,'page',65+i%3*12)
    timelines[name]={'color':[{'time':0,'color':'ffffff00'},{'time':start-.02,'color':'ffffff00'},
        {'time':start,'color':'eeeae0ff'},{'time':start+duration*.5,'color':'aaa69eff'},{'time':start+duration,'color':'dfdcd4ff'}]}

animation={'bones':{'book':{
    # Keep the right page fixed throughout the opening. The cover opens to the left.
    'translate':[{'time':0,'x':250,'y':0},{'time':4.6,'x':250,'y':0}],
    'scale':[{'time':0,'x':.82,'y':.82},{'time':4.6,'x':.82,'y':.82}]}},
    'slots':timelines,'deform':{'default':deform},
    'events':[{'time':4.6,'name':'book_opened'}]}
data={'skeleton':{'hash':'cursed-book-open-v3','spine':'3.8.99','width':1000,'height':720,'images':'./images/'},'bones':bones,'slots':slots,
      'skins':[{'name':'default','attachments':attachments}],
      'events':{'book_opened':{}},'animations':{'Open':animation}}
# First-time story opens its cover directly onto the first readable leaf.
import copy
first=copy.deepcopy(animation)
first.pop('events',None)
for name,timeline in first['slots'].items():
    if name.startswith('turning_page_'):
        timeline['color']=[{'time':0,'color':'ffffff00'},{'time':1.85,'color':'ffffff00'},{'time':2.3,'color':'dfdcd4ff'}]
for name,timeline in first['deform']['default'].items():
    if name.startswith('turning_page_'):
        for attachment,keys in timeline.items():
            timeline[attachment]=[dict(keys[-1],time=0)]
for timeline in first['bones'].values():
    for keys in timeline.values():
        for key in keys:key['time']=min(key.get('time',0),2.65)
data['animations']['OpenFirstPage']=first
text=json.dumps(data,separators=(',',':'))
(HERE/'CursedBook.json').write_text(text)
(OUT/'CursedBook.json').write_text(text)
print('Created Spine 3.8 Open: 4.6s,',len(bones),'bones,',len(slots),'mesh layers')
