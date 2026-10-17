"""Explicit combat resurrection; reverse the continuous Death1 motion, not events."""
import copy,json
from pathlib import Path
from build_death import value
ROOT=Path(__file__).resolve().parents[3]
def build(d):
    death=d['animations']['Death1'];duration=2.73
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
        a['ik'][name]=[{'time':0,'mix':0,'bendPositive':False,'curve':'stepped'},{'time':1.25,'mix':1,'bendPositive':False}]
    def color(t,v):return {'time':t,'color':'ffffff'+format(round(v*255),'02x')}
    for name in ['scythe','lantern']:
        a['slots'][name]={'attachment':[{'time':0,'name':name}],'color':[color(0,0),color(1,0),color(1.25,1),color(duration,1)]}
    a['slots']['scythe_return']={'attachment':[{'time':0,'name':None}],'color':[color(0,0)]}
    a['slots']['head']={'color':[color(0,0),color(.12,0),color(.35,1),color(duration,1)]}
    a['slots']['head_rest']={'attachment':[{'time':0,'name':'head_closed'}],'color':[color(0,1),color(.12,1),color(.35,0),color(duration,0)]}
    d['animations']['Revive']=a
if __name__=='__main__':
    for p in [ROOT/'Assets/RedHoodPrototype/RedHood.json',ROOT/'Assets/DarkFairytale/RedHoodBattle.json']:
        d=json.loads(p.read_text());build(d);p.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
