"""Explicit, idempotent refinement of Red Hood's rig and two sample clips.

Run from any directory. Does not run on import or touch textures/prefabs.
Existing combat event times and all other clips are retained.
"""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PATHS = [ROOT / 'Assets/RedHoodPrototype/RedHood.json',
         ROOT / 'Assets/DarkFairytale/RedHoodBattle.json']


def world_transforms(bones):
    out = {}
    for b in bones:
        p = out.get(b.get('parent'), (1, 0, 0, 1, 0, 0))
        a, c, b0, d, x, y = p
        r = math.radians(b.get('rotation', 0))
        co, si = math.cos(r), math.sin(r)
        lx, ly = b.get('x', 0), b.get('y', 0)
        out[b['name']] = (a*co+c*si, -a*si+c*co, b0*co+d*si,
                          -b0*si+d*co, a*lx+c*ly+x, b0*lx+d*ly+y)
    return out


def local(t, x, y):
    a, b, c, d, tx, ty = t
    det = a*d-b*c
    return ((d*(x-tx)-b*(y-ty))/det, (-c*(x-tx)+a*(y-ty))/det)


def weighted_vertex(world, ids, transforms):
    x, y, weights = world
    weights = [(n, w) for n, w in weights if w > 1e-6]
    out = [len(weights)]
    for n, w in weights:
        lx, ly = local(transforms[n], x, y)
        out.extend([ids[n], round(lx, 6), round(ly, 6), round(w, 6)])
    return out


def refine_rig(data):
    if any(b['name'] == 'cloth_back' for b in data['bones']):
        return
    old = world_transforms(data['bones'])
    # Children of the original cape controller retain the other clips' sway.
    for name, parent, x, y in [
        ('cloth_root', 'cape_tip', 98, -7),
        ('cloth_mid', 'cape_tip', 38, -17),
        ('cloth_back', 'cape_tip', -32, -22),
        ('hem_back', 'skirt', -85, -35),
        ('hem_mid', 'skirt', -18, -35),
        ('hem_front', 'skirt', 65, -35),
    ]:
        data['bones'].append(dict(name=name, parent=parent, x=x, y=y))
    transforms = world_transforms(data['bones'])
    ids = {b['name']: i for i, b in enumerate(data['bones'])}
    attachments = data['skins'][0]['attachments']
    cape = attachments['cape']['cape']
    verts, i, rebuilt = cape['vertices'], 0, []
    while i < len(verts):
        count = verts[i]; i += 1
        x = y = 0
        for _ in range(count):
            idx, lx, ly, w = verts[i:i+4]; i += 4
            a, b, c, d, tx, ty = old[data['bones'][idx]['name']]
            x += (a*lx+b*ly+tx)*w
            y += (c*lx+d*ly+ty)*w
        distance = max(0, min(1, (79-x)/291))
        segment = distance*3
        names = ['torso', 'cloth_root', 'cloth_mid', 'cloth_back']
        left = min(2, int(segment)); blend = segment-left
        rebuilt.extend(weighted_vertex((x, y, [(names[left], 1-blend),
                                               (names[left+1], blend)]), ids, transforms))
    cape['vertices'] = rebuilt

    region = attachments['skirt']['skirt']
    cols, rows = 6, 4
    mesh = dict(type='mesh', width=region['width'], height=region['height'],
                uvs=[], triangles=[], vertices=[])
    tx, ty = transforms['skirt'][4:]
    for row in range(rows+1):
        v = row/rows
        for col in range(cols+1):
            u = col/cols
            x = tx+region['x']+(u-.5)*region['width']
            y = ty+region['y']+(.5-v)*region['height']
            mesh['uvs'].extend([u, v])
            swing = v*v*.85
            s = u*2; left = min(1, int(s)); blend = s-left
            names = ['hem_back', 'hem_mid', 'hem_front']
            mesh['vertices'].extend(weighted_vertex((x,y,[('skirt',1-swing),
                (names[left],swing*(1-blend)), (names[left+1],swing*blend)]),ids,transforms))
            if row < rows and col < cols:
                k = row*(cols+1)+col
                mesh['triangles'].extend([k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2])
    attachments['skirt']['skirt'] = mesh


def anchor_cape(data):
    bones = {b['name']: b for b in data['bones']}
    if bones['cloth_mid']['parent'] == 'cloth_root':
        return
    old = world_transforms(data['bones'])
    cape = data['skins'][0]['attachments']['cape']['cape']
    points = []
    verts, i = cape['vertices'], 0
    while i < len(verts):
        count = verts[i]; i += 1
        x = y = 0
        for _ in range(count):
            idx, lx, ly, w = verts[i:i+4]; i += 4
            a,b,c,d,tx,ty = old[data['bones'][idx]['name']]
            x += (a*lx+b*ly+tx)*w
            y += (c*lx+d*ly+ty)*w
        points.append((x,y))
    # Pivot at the shoulder, not in the middle of the hanging fabric.
    # Serial joints carry the bend outward without translating the whole cape.
    bones['cape_tip'].update(x=-18,y=0)
    bones['cloth_root'].update(parent='cape_tip',x=0,y=0)
    bones['cloth_mid'].update(parent='cloth_root',x=-85,y=-25)
    bones['cloth_back'].update(parent='cloth_mid',x=-75,y=-45)
    transforms = world_transforms(data['bones'])
    ids = {b['name']: i for i,b in enumerate(data['bones'])}
    rebuilt = []
    for x,y in points:
        # The collar-side band is rigidly attached to the torso. Smooth weights
        # then hand control to successive joints along the length of the cloth.
        segment = max(0,min(3,(-x+10)/220*3))
        left = min(2,int(segment)); blend = segment-left
        blend = blend*blend*(3-2*blend)
        names = ['torso','cloth_root','cloth_mid','cloth_back']
        rebuilt.extend(weighted_vertex((x,y,[(names[left],1-blend),
            (names[left+1],blend)]),ids,transforms))
    cape['vertices'] = rebuilt


def timeline(times, values, kind='rotate', smooth=True):
    result = []
    for t, value in zip(times, values):
        key = {'time': t}
        if kind == 'rotate': key['angle'] = value
        else: key.update(x=value[0], y=value[1])
        if smooth: key.update(curve=.33, c2=0, c3=.67, c4=1)
        result.append(key)
    for k in ['curve','c2','c3','c4']: result[-1].pop(k, None)
    return result


def clips(data):
    idle = {'bones': {}}
    # Periodic samples: each part has its own phase, and identical seam values.
    times = [round(i*.06, 4) for i in range(61)]
    def wave(amp, phase=0):
        return [round(amp*math.sin(2*math.pi*t/3.6+phase),4) for t in times]
    def rot(name, amp, phase=0):
        idle['bones'][name] = {'rotate': timeline(times,wave(amp,phase),smooth=False)}
    # Stable support: do not drive leg IK with a periodic pelvis translation.
    # Explicit neutral keys also restore the pelvis after attack blending.
    idle['bones']['hips'] = {'translate': timeline([0,3.6],[(0,0),(0,0)],
                                                  'translate',smooth=False)}
    rot('torso', .9, .45)
    idle['bones']['torso']['translate'] = timeline(times,
        list(zip([0]*len(times),wave(.8,.45))), 'translate',smooth=False)
    rot('head', .7, 2.9)
    rot('weapon', 1.1, 1.3)
    rot('skirt', .35, -.4)
    rot('cape_tip', 0)
    rot('cloth_root', .25, .1)
    rot('cloth_mid', .7, -.25)
    rot('cloth_back', 1.3, -.6)
    rot('hem_back', .8, -1.0)
    rot('hem_mid', .5, -.5)
    rot('hem_front', .6, -.1)
    rot('lantern', 3, -1.6)
    data['animations']['Idle1'] = idle

    attack = {'bones': {}, 'events': data['animations']['Attack1']['events']}
    # Keep contact at .56 and completion at 1.18 for the combat controller.
    times = [0,.12,.32,.43,.49,.56,.615,.72,.91,1.18]
    def arot(name, values):
        attack['bones'].setdefault(name,{})['rotate'] = timeline(times,values)
    def move(name, values):
        attack['bones'].setdefault(name,{})['translate'] = timeline(times,values,'translate')
    arot('torso',[0,5,15,17,4,-23,-23,-16,-5,0])
    move('hips',[(0,0),(-6,-6),(-18,-17),(-20,-19),(-3,-22),(23,-17),(23,-17),(15,-12),(3,-3),(0,0)])
    arot('head',[0,-3,-11,-14,-9,13,13,8,1,0])
    arot('weapon',[0,12,103,117,100,-9,-9,-20,-4,0])
    move('weapon',[(0,0),(-12,25),(7,107),(9,113),(25,102),(20,39),(20,39),(6,27),(-9,8),(0,0)])
    move('upper_arm',[(0,0),(2,1),(12,2),(12,2),(10,1),(2,0),(2,0),(0,0),(0,0),(0,0)])
    arot('skirt',[0,2,5,7,4,-3,-6,-9,3,0])
    arot('cape_tip',[0,-.3,-1,-2,-3,-1,2,4,-1,0])
    arot('cloth_root',[0,-.3,-.7,-1,-1.5,0,1,2,-.5,0])
    arot('cloth_mid',[0,0,-1,-2,-3,-2,0,5,-1.5,0])
    arot('cloth_back',[0,.3,-1,-2,-4,-5,-2,7,-2,0])
    arot('hem_back',[0,2,5,7,8,3,-4,-10,4,0])
    arot('hem_mid',[0,1,3,4,5,2,-3,-6,2,0])
    arot('hem_front',[0,-1,-3,-4,-5,-2,4,7,-3,0])
    arot('lantern',[0,5,19,23,30,36,24,-24,10,0])
    # Downstroke accelerates continuously through the intermediate pose.
    for name in ['torso','hips','head','weapon','upper_arm']:
        for tl in attack['bones'][name].values():
            tl[3].update(curve=.55,c2=0,c3=.9,c4=.4)
            tl[4].update(curve=.2,c2=.5,c3=.5,c4=1)
    data['animations']['Attack1'] = attack


def close_knee_seams(data):
    # A stronger crouch exposed the existing boot/thigh seam. Extend the upper
    # boot mesh toward the knee; keep the planted foot vertices unchanged.
    if data['skeleton'].get('hash') == 'red-hood-motion-v4':
        return
    ids = {b['name']: i for i,b in enumerate(data['bones'])}
    for name in ['far_boot','near_boot']:
        verts = data['skins'][0]['attachments'][name][name]['vertices']
        length = data['bones'][ids[name]]['length']
        i = 0
        while i < len(verts):
            count = verts[i]; i += 1
            for _ in range(count):
                if verts[i] == ids[name]:
                    taper = max(0, min(1, 1-verts[i+1]/length))
                    verts[i+1] -= 9*taper
                i += 4
    data['skeleton']['hash'] = 'red-hood-motion-v4'


if __name__ == '__main__':
    for path in PATHS:
        data = json.loads(path.read_text())
        refine_rig(data)
        close_knee_seams(data)
        anchor_cape(data)
        clips(data)
        path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')
        print(path.relative_to(ROOT),len(data['bones']),'bones')
