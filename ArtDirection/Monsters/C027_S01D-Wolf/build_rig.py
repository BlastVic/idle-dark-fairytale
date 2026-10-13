"""Preserve the supplied converted source rig; add game aliases and linked skin only."""
import copy, hashlib, json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
ORIGINAL=ROOT.parent/'Spine/monster2/动画/C027_S01D'
SOURCE=BASE/'source'
ALIASES={'Idle1':'Idle_Battle_00','Attack1':'Atk1_00','Hit':'Hit_00','Death1':'Death_00'}
EVENTS={'Attack1':[{'time':0.6333,'name':'OnHit'},{'time':1.2333,'name':'OnComplete'}], 'Hit':[{'time':0.5333,'name':'OnComplete'}], 'Death1':[{'time':1.5333,'name':'OnComplete'}]}

def build():
    original=json.loads((ORIGINAL/'C027_S01D.json').read_text())
    rig=copy.deepcopy(original)
    skin=copy.deepcopy(rig['skins'][0]);skin['name']='1'
    for slot,attachments in skin['attachments'].items():
        for name,mesh in list(attachments.items()):
            if mesh.get('type')=='mesh':
                attachments[name]={'type':'linkedmesh','path':mesh.get('path',name),'skin':'default','parent':name,'deform':True,'width':mesh['width'],'height':mesh['height']}
    rig['skins'].append(skin)
    rig.setdefault('events',{}).update({'OnHit':{},'OnComplete':{}})
    for name,source in ALIASES.items():
        rig['animations'][name]=copy.deepcopy(original['animations'][source])
        if name in EVENTS:rig['animations'][name]['events']=EVENTS[name]
    SOURCE.mkdir(parents=True,exist_ok=True)
    (SOURCE/'MobRuneWolf.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n')
    manifest={'source':str(ORIGINAL),'source_version':original['skeleton']['spine'],'changes':['Added linked skin 1 inheriting default deform','Added four game animation aliases and events; no changes to original timelines or rig','Repacked nonrotated straight-alpha atlas from repainted source parts'], 'aliases':ALIASES,'events':EVENTS,'original_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ORIGINAL.iterdir() if p.is_file()}}
    (SOURCE/'provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    verify(rig,original)

def verify(rig=None,original=None):
    if rig is None:rig=json.loads((ROOT/'Assets/DarkFairytale/Monsters/MobRuneWolf/MobRuneWolf.json').read_text())
    if original is None:original=json.loads((ORIGINAL/'C027_S01D.json').read_text())
    for field in ('skeleton','bones','slots','ik','transform'):assert rig[field]==original[field],field
    assert rig['skins'][0]==original['skins'][0]
    for n,a in original['animations'].items():assert rig['animations'][n]==a,n
    for n,src in ALIASES.items():
        a=copy.deepcopy(rig['animations'][n]);a.pop('events',None);assert a==original['animations'][src],n
    assert len(rig['bones'])==66 and len(rig['slots'])==25
    print('PASS: original 66 bones / 25 slots / IK / transforms / meshes / UV / weights / 10 complete source animations unchanged; aliases differ only by game events.')
if __name__=='__main__':
    import sys
    verify() if '--verify' in sys.argv else build()
