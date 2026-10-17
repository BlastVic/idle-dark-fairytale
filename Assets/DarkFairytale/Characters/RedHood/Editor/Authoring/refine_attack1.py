"""Explicit Attack1-only authoring: high hold, accelerated chop, flowing recovery."""
import copy
import json
from pathlib import Path
from refine_motion import clips

ROOT = next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings').is_dir())
TIMES = [0, .12, .32, .46, .56, .60, .72, .91, 1.18]


def sample(values, t):
    for i in range(len(TIMES)-1):
        if t <= TIMES[i+1]+1e-8:
            h = TIMES[i+1]-TIMES[i]
            u = max(0, min(1, (t-TIMES[i])/h))
            if i < 5:
                u = u*u if i == 3 else u*u*(3-2*u)
                return values[i]+(values[i+1]-values[i])*u
            def tangent(k):
                if k == 5 or k == len(TIMES)-1:
                    return 0
                h0, h1 = TIMES[k]-TIMES[k-1], TIMES[k+1]-TIMES[k]
                d0, d1 = (values[k]-values[k-1])/h0, (values[k+1]-values[k])/h1
                if d0*d1 <= 0:
                    return 0
                return 3*(h0+h1)/((2*h1+h0)/d0+(h1+2*h0)/d1)
            return ((2*u**3-3*u*u+1)*values[i]+(u**3-2*u*u+u)*h*tangent(i)
                    +(-2*u**3+3*u*u)*values[i+1]+(u**3-u*u)*h*tangent(i+1))
    return values[-1]


def build(data):
    reference = copy.deepcopy(data)
    clips(reference)
    source = reference['animations']['Attack1']
    attack = copy.deepcopy(data['animations']['Attack1'])
    attack['bones'] = {}
    times = sorted(set([round(i/120, 6) for i in range(142)]+TIMES))
    # Move the old highest pose to .32, hold it through .46, then hit at .56.
    # The loaded torso and pelvis stay still with the grip during anticipation.
    for bone, timelines in source['bones'].items():
        attack['bones'][bone] = {}
        for kind, keys in timelines.items():
            fields = ['angle'] if kind == 'rotate' else ['x', 'y']
            curves = {field: [keys[i].get(field, 0) for i in [0,1,3,3,5,6,7,8,9]]
                      for field in fields}
            attack['bones'][bone][kind] = [dict(time=t, **{
                field: round(sample(values, t), 5) for field, values in curves.items()
            }) for t in times]
    return attack


if __name__ == '__main__':
    for relative in ['Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json']:
        path = ROOT/relative
        data = json.loads(path.read_text())
        data['animations']['Attack1'] = build(data)
        path.write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':'))+'\n')
        print(relative)
