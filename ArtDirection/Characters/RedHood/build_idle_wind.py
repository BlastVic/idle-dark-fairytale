"""Manual Idle1 cloth wind authoring; keep shoulder anchors and first pose."""
import json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def build(d):
    idle=d['animations']['Idle1']['bones']
    # Wave travels from the shoulder towards the torn free edge, with a smaller
    # second harmonic adding a flutter rather than a rigid pendulum motion.
    for name,base,amp,lag,flutter in [
        ('cloth_root',.025,.65,0,.10),
        ('cloth_mid',-.1732,3.2,.65,.65),
        ('cloth_back',-.734,6.2,1.35,1.45)]:
        keys=[]
        for i in range(217):
            t=i/60;phase=2*math.pi*t/3.6
            wave=amp*(math.sin(phase-lag)-math.sin(-lag))
            wave+=flutter*(math.sin(2*phase-lag*1.4)-math.sin(-lag*1.4))
            keys.append({'time':round(t,6),'angle':round(base+wave,5)})
        idle[name]['rotate']=keys
if __name__=='__main__':
    for p in [ROOT/'Assets/RedHoodPrototype/RedHood.json',ROOT/'Assets/DarkFairytale/RedHoodBattle.json']:
        d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
