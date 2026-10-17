"""Explicit death authoring: Victory-speed recovery, then continuous heel-sitting."""
import copy,json,math
from pathlib import Path
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings').is_dir())
def value(keys,t,field,default):
    if t<=keys[0].get('time',0):return keys[0].get(field,default)
    for a,b in zip(keys,keys[1:]):
        if t<=b.get('time',0):
            u=(t-a.get('time',0))/(b['time']-a.get('time',0))
            return a.get(field,default)*(1-u)+b.get(field,default)*u
    return keys[-1].get(field,default)
def build(d):
    source=d['animations']['Victory'];a={'bones':{},'slots':{},'events':[{'time':2.3,'name':'OnComplete'}],'drawOrder':[{'time':0}]}
    targets={('hips','translate'):{'x':-80,'y':-63},('torso','rotate'):{'angle':-15},('torso','translate'):{'x':0,'y':0},('head','rotate'):{'angle':-16},('weapon','translate'):{'x':-20,'y':-18},('weapon','rotate'):{'angle':-25},('far_foot','translate'):{'x':-38,'y':-10},('near_foot','translate'):{'x':-243,'y':-5},('cloth_root','rotate'):{'angle':5},('cloth_mid','rotate'):{'angle':-3},('cloth_back','rotate'):{'angle':0},('skirt','rotate'):{'angle':-8},('lantern','rotate'):{'angle':0}}
    for bone,tls in source['bones'].items():
        out=a['bones'].setdefault(bone,{})
        for kind,keys in tls.items():
            fields=['angle'] if kind=='rotate' else ['x','y'];default=1 if kind=='scale' else 0
            start={f:value(keys,1.48,f,default) for f in fields};end=targets.get((bone,kind),start)
            track=[]
            for i in range(277):
                t=round(i/120,6)
                if t<=.8:vals={f:value(keys,t/ .8*1.48,f,default) for f in fields}
                else:
                    u=max(0,min(1,(t-.8)/.7));u=u*u*(3-2*u)
                    vals={f:start[f]+(end.get(f,start[f])-start[f])*u for f in fields}
                # After knee contact, sit the pelvis back onto the heels.
                if t>1.5:
                    u=max(0,min(1,(t-1.5)/.6));u=u*u*(3-2*u)
                    seated={('hips','translate'):{'x':-137,'y':-105},('near_thigh','translate'):{'x':-26,'y':0},('torso','rotate'):{'angle':-10},('head','rotate'):{'angle':-20},('skirt','rotate'):{'angle':-3}}
                    if (bone,kind) in seated:
                        vals={f:vals[f]+(seated[(bone,kind)].get(f,vals[f])-vals[f])*u for f in fields}
                track.append(dict(time=t,**{f:round(v,5) for f,v in vals.items()}))
            out[kind]=track
    # One continuous descent: ankles roll back, knees lower, and hips sit back
    # together. There is no upright-kneeling waypoint or second easing restart.
    setup={b['name']:b for b in d['bones']}
    def standing_knee(px,py,ax,ay,L,L2,sx):
        tx=ax-px;ty=ay-py;l1=L*sx;aa=(sx*L2)**2;bb=L2*L2;dd=tx*tx+ty*ty
        c=bb*l1*l1+aa*dd-aa*bb;c1=-2*bb*l1;c2=bb-aa
        q=math.sqrt(c1*c1-4*c2*c);q=-q if c1<0 else q;q=-(c1+q)/2
        r0=q/c2;r1=c/q;r=r0 if abs(r0)<abs(r1) else r1
        y=-math.sqrt(max(0,dd-r*r));angle=math.atan2(ty,tx)-math.atan2(y,r)
        return px+l1*math.cos(angle),py+l1*math.sin(angle),angle
    pins=[('far',-100.6323087663,19.1887487735,-178,19,.95),
          ('near',-58.8895008951,19.9229388895,-133,20,1.05)]
    legs=[]
    for side,kx,ky,ax,ay,sx in pins:
        thigh=side+'_thigh';boot=side+'_boot';foot=side+'_foot'
        tx=value(source['bones'][thigh]['translate'],1.48,'x',0)+setup[thigh]['x']
        ty=setup[thigh]['y'];ax0=setup[foot]['x']+value(source['bones'][foot]['translate'],1.48,'x',0);ay0=setup[foot]['y']
        kx0,ky0,theta0=standing_knee(-110+tx,173+ty,ax0,ay0,setup[thigh]['length'],setup[boot]['length'],sx)
        legs.append((side,kx,ky,ax,ay,sx,ax0,ay0,kx0,ky0,theta0))
        a['bones'][thigh]['rotate']=[{'time':0,'angle':0}]
        a['bones'][boot]={'rotate':[{'time':0,'angle':0}],'scale':[{'time':0,'x':1,'y':1}]}
    a['ik']={name:[{'time':0,'mix':1,'bendPositive':False,'curve':'stepped'},
                   {'time':.8,'mix':0,'bendPositive':False}] for name in ['far_leg_plant','near_leg_plant']}
    ends={(bone,kind):dict(keys[-1]) for bone,tls in a['bones'].items() for kind,keys in tls.items()}
    for i,hip in enumerate(a['bones']['hips']['translate']):
        t=hip['time']
        if t<.8:continue
        u=max(0,min(1,(t-.8)/1.25));u=u*u*(3-2*u)
        for bone,tls in source['bones'].items():
            if bone in ['hips','far_thigh','near_thigh','far_foot','near_foot']:continue
            for kind,keys in tls.items():
                fields=['angle'] if kind=='rotate' else ['x','y'];default=1 if kind=='scale' else 0
                a['bones'][bone][kind][i]=dict(time=t,**{f:round(value(keys,1.48,f,default)*(1-u)+ends[(bone,kind)].get(f,default)*u,5) for f in fields})
        pose=[]
        for side,kx,ky,ax,ay,sx,ax0,ay0,kx0,ky0,theta0 in legs:
            alpha0=math.atan2(ky0-ay0,kx0-ax0);alpha1=math.atan2(ky-ay,kx-ax)
            alpha=alpha0+(alpha1-alpha0)*u
            shin=math.hypot(kx0-ax0,ky0-ay0)*(1-u)+math.hypot(kx-ax,ky-ay)*u
            fx=ax0+(ax-ax0)*u;fy=ay0+(ay-ay0)*u
            nx=fx+shin*math.cos(alpha);ny=fy+shin*math.sin(alpha)
            theta=theta0+(math.radians(-18)-theta0)*u
            length=setup[side+'_thigh']['length']*sx
            px=nx-length*math.cos(theta);py=ny-length*math.sin(theta)
            pose.append((side,sx,fx,fy,nx,ny,theta,px,py))
        hx=sum(p[7]-(-25 if p[0]=='far' else 25) for p in pose)/2
        hy=sum(p[8]-setup[p[0]+'_thigh']['y'] for p in pose)/2
        hx-=1.989*u;hy-=2.004*u
        hip.update(x=round(hx,5),y=round(hy-153,5))
        for side,sx,fx,fy,nx,ny,theta,px,py in pose:
            thigh=side+'_thigh';boot=side+'_boot';foot=side+'_foot'
            a['bones'][thigh]['translate'][i]={'time':t,'x':round(px-hx-setup[thigh]['x'],5),'y':round(py-hy-setup[thigh]['y'],5)}
            a['bones'][thigh]['rotate'].append({'time':t,'angle':round(math.degrees(theta)-setup[thigh]['rotation'],5)})
            dx=fx-nx;dy=fy-ny
            lx=(math.cos(theta)*dx+math.sin(theta)*dy)/sx;ly=-math.sin(theta)*dx+math.cos(theta)*dy
            a['bones'][boot]['rotate'].append({'time':t,'angle':round(math.degrees(math.atan2(ly,lx))-setup[boot]['rotation'],5)})
            a['bones'][boot]['scale'].append({'time':t,'x':round(math.hypot(lx,ly)/setup[boot]['length'],6),'y':1})
            a['bones'][foot]['translate'][i]={'time':t,'x':round(fx-setup[foot]['x'],5),'y':round(fy-setup[foot]['y'],5)}
    # Keep the fading blade at its ground contact; lower the empty hand after fade-out.
    for i,key in enumerate(a['bones']['weapon']['translate']):
        t=key['time']
        if t<=.8:continue
        hip=a['bones']['hips']['translate'][i];tor=a['bones']['torso']['translate'][i]
        angle=a['bones']['torso']['rotate'][i]['angle'];r=math.radians(angle)
        ox=hip['x']+10+tor['x'];oy=153+hip['y']+84+tor['y']
        x=99+key['x'];y=-24+key['y']
        wx=ox+math.cos(r)*x-math.sin(r)*y;wy=oy+math.sin(r)*x+math.cos(r)*y
        u=max(0,min(1,(t-1.05)/.3));u=u*u*(3-2*u)
        wx=-69+(wx+69)*u;wy=187.99+(wy-187.99)*u
        dx=wx-ox;dy=wy-oy
        key['x']=round(math.cos(r)*dx+math.sin(r)*dy-99,5)
        key['y']=round(-math.sin(r)*dx+math.cos(r)*dy+24,5)
        rot=a['bones']['weapon']['rotate'][i]
        rot['angle']=round((-20-angle)*(1-u)+rot['angle']*u,5)
    def alpha(t,v):return {'time':t,'color':'ffffff'+format(round(v*255),'02x')}
    for name,attachment in [('scythe','scythe'),('lantern','lantern')]:
        a['slots'][name]={'attachment':[{'time':0,'name':attachment}],'color':[alpha(0,1),alpha(.8,1),alpha(1.05,0),alpha(2.3,0)]}
    a['slots']['scythe_return']={'attachment':[{'time':0,'name':None}],'color':[alpha(0,0)]}
    a['slots']['head']={'color':[alpha(0,1),alpha(.8,1),alpha(.97,0),alpha(2.3,0)]}
    a['slots']['head_rest']={'attachment':[{'time':0,'name':'head_closed'}],'color':[alpha(0,0),alpha(.8,0),alpha(.97,1),alpha(2.3,1)]}
    # Restore the original Victory clock during recovery; shift the continuous
    # descent without changing its shape or speed.
    def restore_victory_clock(node):
        if isinstance(node,dict):
            if 'time' in node:
                t=node['time']
                node['time']=round(t*1.48/.8 if t<=.8 else t+.68,6)
            for child in node.values():restore_victory_clock(child)
        elif isinstance(node,list):
            for child in node:restore_victory_clock(child)
    restore_victory_clock(a)
    d['animations']['Death1']=a
if __name__=='__main__':
    for p in [ROOT/'Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json']:
        d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
