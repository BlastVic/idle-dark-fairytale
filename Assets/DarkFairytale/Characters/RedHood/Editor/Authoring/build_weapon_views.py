"""Pack generated alternate views; explicitly install Attack2 attachments only.

Requires Pillow. Source images are preserved; processing is limited to uniform
resampling and atlas packing. Grip anchors are measured on the source images.
"""
import json
import copy
from pathlib import Path
from PIL import Image

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings').is_dir())
ASSETS=ROOT/'Assets/DarkFairytale/Characters/RedHood/Spine'
SOURCES=ROOT/'Assets/ArtSources/Characters/RedHood/WeaponViews'
VIEWS=[('rear',.16,(520,265),(4,4)),('front',.18,(560,265),(234,4)),
       ('oblique',.16,(606,210),(4,256)),('sweep',.105,(580,235),(4,412))]

def install():
    atlas=Image.new('RGBA',(512,512))
    lines=['','RedHoodWeaponViews.png','size: 512,512','format: RGBA8888',
           'filter: Linear,Linear','repeat: none']
    attachments={}
    for name,scale,grip,xy in VIEWS:
        source=Image.open(SOURCES/f'scythe-{name}-source.png').convert('RGBA')
        size=tuple(round(v*scale) for v in source.size)
        sprite=source.resize(size,Image.Resampling.LANCZOS)
        atlas.paste(sprite,xy)
        key='scythe_'+name+'_v2'
        attachments[key]={'x':size[0]/2-grip[0]*scale,
                          'y':grip[1]*scale-size[1]/2,
                          'width':size[0],'height':size[1]}
        lines += [key,'  rotate: false',f'  xy: {xy[0]}, {xy[1]}',
                  f'  size: {size[0]}, {size[1]}',f'  orig: {size[0]}, {size[1]}',
                  '  offset: 0, 0','  index: -1']
    atlas.save(ASSETS/'RedHoodWeaponViews.png')
    (ASSETS/'RedHoodWeaponViews.atlas.txt').write_text('\n'.join(lines)+'\n')
    for path in [ROOT/'Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json']:
        data=json.loads(path.read_text())
        data['skins'][0]['attachments']['scythe'].update(attachments)
        data['animations']['Attack2'].setdefault('slots',{})['scythe']={
            'attachment':[{'time':t,'name':n} for t,n in [
                (0,'scythe'),(.49,'scythe_oblique_v2'),
                (.52,'scythe_sweep_v2'),(.82,'scythe_oblique_v2'),
                (1.02,'scythe'),(1.18,'scythe')]]}
        install_return_blend(data)
        path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')

def install_return_blend(data):
    """Two colocated slots blend discrete views; setup hides the second slot."""
    name='scythe_return'
    if not any(s['name']==name for s in data['slots']):
        index=next(i for i,s in enumerate(data['slots']) if s['name']=='scythe')
        data['slots'].insert(index+1,{'name':name,'bone':'scythe_projection','color':'ffffff00'})
    skin=data['skins'][0]['attachments']
    tips={'scythe':(105,-141),'scythe_oblique_v2':(54.24,-94.4),
          'scythe_sweep_v2':(93.87,-40.215)}
    spines={'scythe':(142,-42),'scythe_oblique_v2':(170,-45),
            'scythe_sweep_v2':(161,-18)}
    # Recovery-only projection meshes. Keep the shaft fixed and interpolate
    # the blade's foreshortening between the independently painted views.
    # At endpoints these meshes are pixel-identical to their region attachment.
    for key in tips:
        region=skin['scythe'][key]
        width,height=region['width'],region['height']
        vertices=[];uvs=[];triangles=[]
        nx,ny=16,12
        for row in range(ny+1):
            for col in range(nx+1):
                u,v=col/nx,row/ny
                vertices.extend([region.get('x',0)+(u-.5)*width,
                                 region.get('y',0)+(.5-v)*height])
                uvs.extend([u,v])
        for row in range(ny):
            for col in range(nx):
                a=row*(nx+1)+col;b=a+1;c=a+nx+1;d=c+1
                triangles.extend([a,c,b,b,c,d])
        skin['scythe']['return_'+key]={'type':'mesh','path':key,'uvs':uvs,
            'triangles':triangles,'vertices':vertices,'width':width,'height':height}
    skin[name]=copy.deepcopy(skin['scythe'])
    slots=data['animations']['Attack2']['slots']
    slots['scythe']['attachment']=[{'time':t,'name':n} for t,n in [
        (0,'scythe'),(.49,'scythe_oblique_v2'),(.52,'scythe_sweep_v2'),
        (.72,'return_scythe_sweep_v2'),(.92,'return_scythe_oblique_v2'),
        (1.16,'scythe'),(1.18,'scythe')]]
    def color(t,a): return {'time':t,'color':'ffffff'+format(round(a*255),'02x')}
    # Complementary weights and identical attachment on hand-off frames.
    slots['scythe']['color']=[color(t,a) for t,a in [
        (0,1),(.82,1),(.88,0),(.9199,0),(.92,1),(1.04,1),(1.10,0),(1.1599,0),(1.16,1),(1.18,1)]]
    slots[name]={
        'attachment':[{'time':0,'name':None},{'time':.72,'name':'return_scythe_oblique_v2'},
                      {'time':.92,'name':'return_scythe'},
                      {'time':1.16,'name':None},{'time':1.18,'name':None}],
        'color':[color(t,a) for t,a in [
            (0,0),(.82,0),(.88,1),(.9199,1),(.92,0),(1.04,0),(1.10,1),(1.1599,1),(1.16,0),(1.18,0)]]}
    def landmark(table, t):
        keys=['scythe_sweep_v2','scythe_oblique_v2','scythe']
        times=[.72,.92,1.16]
        k=0 if t <= times[1] else 1
        h=times[k+1]-times[k]
        u=max(0,min(1,(t-times[k])/h))
        result=[]
        for axis in range(2):
            v=[table[key][axis] for key in keys]
            h0,h1=times[1]-times[0],times[2]-times[1]
            d0,d1=(v[1]-v[0])/h0,(v[2]-v[1])/h1
            middle=0 if d0*d1<=0 else 3*(h0+h1)/((2*h1+h0)/d0+(h1+2*h0)/d1)
            m=[0,middle,0]
            result.append((2*u**3-3*u*u+1)*v[k]+(u**3-2*u*u+u)*h*m[k]
                          +(-2*u**3+3*u*u)*v[k+1]+(u**3-u*u)*h*m[k+1])
        return result
    deform={s:{} for s in ['scythe',name]}
    for slot,key,start,end,from_tip,to_tip in [
        ('scythe','scythe_sweep_v2',.72,.92,tips['scythe_sweep_v2'],tips['scythe_oblique_v2']),
        (name,'scythe_oblique_v2',.72,.92,tips['scythe_sweep_v2'],tips['scythe_oblique_v2']),
        ('scythe','scythe_oblique_v2',.92,1.16,tips['scythe_oblique_v2'],tips['scythe']),
        (name,'scythe',.92,1.16,tips['scythe_oblique_v2'],tips['scythe'])]:
        points=skin[slot]['return_'+key]['vertices'];sx,sy=tips[key]
        from_key=next(k for k,v in tips.items() if v==from_tip)
        to_key=next(k for k,v in tips.items() if v==to_tip)
        ox,oy=spines[key];det=(sx-47)*oy-(ox-47)*sy
        frames=[]
        for i in range(21):
            t=start+(end-start)*i/20
            tx,ty=landmark(tips,t)
            ex,ey=landmark(spines,t)
            a=((tx-47)*oy-(ex-47)*sy)/det
            b=(-(tx-47)*(ox-47)+(ex-47)*(sx-47))/det
            c=(ty*oy-ey*sy)/det
            d=(-ty*(ox-47)+ey*(sx-47))/det
            offsets=[]
            for x,y in zip(points[::2],points[1::2]):
                # No deformation at the hand or along the horizontal shaft.
                w=max(0,min(1,(x-25)/25),min(1,(-y-10)/20))
                w=w*w*(3-2*w)
                offsets.extend([round(((a-1)*(x-47)+b*y)*w,5),
                                round((c*(x-47)+(d-1)*y)*w,5)])
            frames.append({'time':round(start+(end-start)*i/20,6),'vertices':offsets})
        deform[slot]['return_'+key]=frames
    data['animations']['Attack2']['deform']={'default':deform}

if __name__=='__main__': install()
