"""Explicit combat resurrection; reverse the continuous Death1 motion, not events."""
import copy,json
from pathlib import Path
from build_death import value
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings').is_dir())
def build(d):
    death=d['animations']['Death1']
    duration=max(k.get('time',0) for k in death['events'])
    a={'bones':{},'slots':{},'ik':{},'drawOrder':[{'time':0}],'events':[{'time':duration,'name':'OnComplete'}]}
    # Sample numeric tracks so reversed sparse/stepped keys cannot interpolate
    # across the original IK handoff or reverse the completion event.
    for bone,tls in death['bones'].items():
        a['bones'][bone]={}
        for kind,keys in tls.items():
            fields=['angle'] if kind=='rotate' else ['x','y'];default=1 if kind=='scale' else 0
            times=sorted(set([0,duration]+[round(duration-k['time'],6) for k in keys if k['time']<=duration]))
            a['bones'][bone][kind]=[dict(time=t,**{f:round(value(keys,duration-t,f,default),6) for f in fields}) for t in times]
    for name in death['ik']:
        a['ik'][name]=[{'time':0,'mix':0,'bendPositive':False,'curve':'stepped'},
                       {'time':duration,'mix':1,'bendPositive':False}]
    # Attachments are constant in Death1; reverse every opacity key, including eyes.
    for name,tracks in death['slots'].items():
        a['slots'][name]={}
        for kind,keys in tracks.items():
            if kind=='attachment':
                a['slots'][name][kind]=[dict(copy.deepcopy(keys[-1]),time=0)]
            else:
                reversed_keys=[dict(copy.deepcopy(k),time=round(duration-k.get('time',0),6)) for k in keys]
                reversed_keys.sort(key=lambda k:k['time'])
                if reversed_keys[0]['time']>0:
                    reversed_keys.insert(0,dict(copy.deepcopy(reversed_keys[0]),time=0))
                a['slots'][name][kind]=reversed_keys
    d['animations']['Revive']=a
if __name__=='__main__':
    for p in [ROOT/'Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json']:
        d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
