"""Explicit Attack2-only refinement. Never rebuilds the rig or other clips."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TIMES = [0,.12,.32,.46,.56,.60,.72,.9,1.18]


def sample(values, t):
    for i in range(len(TIMES)-1):
        if t <= TIMES[i+1]+1e-8:
            u = max(0,min(1,(t-TIMES[i])/(TIMES[i+1]-TIMES[i])))
            if i >= 5:
                # Shared tangents through recovery poses: do not stop at .9 s.
                # Preserve true direction reversals without overshooting them.
                def tangent(k):
                    if k <= 5 or k == len(TIMES)-1:
                        return 0
                    h0,h1=TIMES[k]-TIMES[k-1],TIMES[k+1]-TIMES[k]
                    d0,d1=(values[k]-values[k-1])/h0,(values[k+1]-values[k])/h1
                    if d0*d1 <= 0:
                        return 0
                    w0,w1=2*h1+h0,h1+2*h0
                    return (w0+w1)/(w0/d0+w1/d1)
                h=TIMES[i+1]-TIMES[i]
                return ((2*u**3-3*u*u+1)*values[i]+(u**3-2*u*u+u)*h*tangent(i)
                        +(-2*u**3+3*u*u)*values[i+1]+(u**3-u*u)*h*tangent(i+1))
            # Deliberate pause at the raised pose, then accelerate the cut.
            u = u*u if i == 3 else u*u*(3-2*u)
            return values[i]+(values[i+1]-values[i])*u
    return values[-1]


def build(previous):
    clip = {'bones':{}, 'events':previous['events'],
            'drawOrder':[{'time':0}, {'time':.18,'offsets':[
                {'slot':s,'offset':-2} for s in
                ['scythe','lantern','upper_arm','forearm','hand']]},
                {'time':.49}, {'time':1.18}]}
    times = sorted(set([round(i/120,6) for i in range(142)]+TIMES))
    torso = [0,4,16,16,-14,-14,-18,-5,0]
    hips_x = [0,-5,-18,-18,24,24,20,4,0]
    hips_y = [0,-5,-12,-12,-12,-12,-10,-3,0]
    # Author the right-hand grip directly. A rolled blade has a different
    # projected tip: the old upright blade_tip locator cannot define this cut.
    grip_x = [109,70,5,5,142,142,150,120,109]
    grip_y = [213,240,300,300,224,224,210,214,213]
    angle = [0,20,48,48,-18,-18,-25,-8,0]
    def rotate(name, values):
        clip['bones'][name]={'rotate':[{'time':t,'angle':round(sample(values,t),5)} for t in times]}
    rotate('torso',torso)
    clip['bones']['hips']={'translate':[{'time':t,'x':round(sample(hips_x,t),5),
                                        'y':round(sample(hips_y,t),5)} for t in times]}
    weapon = {'rotate':[], 'translate':[]}
    for t in times:
        r = math.radians(sample(torso,t))
        x = sample(grip_x,t)-(10+sample(hips_x,t))
        y = sample(grip_y,t)-(237+sample(hips_y,t))
        weapon['translate'].append({'time':t,'x':round(math.cos(r)*x+math.sin(r)*y-99,5),
                                    'y':round(-math.sin(r)*x+math.cos(r)*y+24,5)})
        weapon['rotate'].append({'time':t,'angle':round(sample(angle,t)-sample(torso,t),5)})
    clip['bones']['weapon']=weapon
    # Shoulder follows the final extension so the wrist never exceeds IK reach.
    clip['bones']['upper_arm']={'translate':[{'time':t,
        'x':round(sample([0,0,0,0,2,2,5,1,0],t),5),'y':0} for t in times]}
    rotate('head',[0,-3,-13,-13,12,12,9,2,0])
    rotate('skirt',[0,1,4,4,-4,-6,-8,2,0])
    rotate('cape_tip',[0,-.5,-2,-2,0,2,5,-1,0])
    rotate('cloth_root',[0,-.2,-1,-1,0,.8,2,-.5,0])
    rotate('cloth_mid',[0,0,-1.5,-2,-1,1,4,-1,0])
    rotate('cloth_back',[0,0,-2,-3,-3,-1,6,-2,0])
    rotate('hem_back',[0,1,3,3,1,-3,-5,1,0])
    rotate('hem_mid',[0,.5,1.5,1.5,0,-1,-3,.5,0])
    rotate('hem_front',[0,-.5,-2,-2,0,2,4,-1,0])
    rotate('lantern',[0,5,17,12,-27,-20,15,-5,0])
    # Retain explicitly installed weapon perspective attachments on regeneration.
    if 'slots' in previous:
        clip['slots']=previous['slots']
    if 'deform' in previous:
        clip['deform']=previous['deform']
    return clip


if __name__ == '__main__':
    for relative in ['Assets/RedHoodPrototype/RedHood.json','Assets/DarkFairytale/RedHoodBattle.json']:
        path = ROOT/relative
        data = json.loads(path.read_text())
        data['animations']['Attack2']=build(data['animations']['Attack2'])
        path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')
        print(relative)
