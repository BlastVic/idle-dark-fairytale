"""Preserve the supplied converted source rig; add game aliases and linked skin only."""
import copy, hashlib, json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
ORIGINAL=ROOT.parent/'Spine/monster2/动画/C030_S01B'
SOURCE=BASE/'source'
ALIASES={'Idle1':'Idle_Battle_00','Attack1':'Atk1_00','Hit':'Hit_00','Death1':'Death_00'}
EVENTS={'Attack1':[{'time':0.2667,'name':'OnHit'},{'time':1.0,'name':'OnComplete'}], 'Hit':[{'time':0.6667,'name':'OnComplete'}], 'Death1':[{'time':1.3667,'name':'OnComplete'}]}

def build():
    original=json.loads((ORIGINAL/'C030_S01B.json').read_text())
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
    (SOURCE/'MobRivenfang.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n')
    manifest={'monster_id':'MobRivenfang','approved_design':'B-character-v2.png','painting':'built-in image_gen; exact model identifier not exposed','painting_prompts':'painting-prompts.json','native_project_status':'Original C030_S01B.spine preserved; new skin delivered as JSON and part PNGs, no new native .spine saved','source':str(ORIGINAL),'source_version':original['skeleton']['spine'],'changes':['Added linked skin 1 inheriting default deform','Added four game animation aliases and events; no changes to original timelines or rig','Repacked nonrotated straight-alpha atlas from repainted source parts'], 'aliases':ALIASES,'events':EVENTS,'original_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ORIGINAL.iterdir() if p.is_file()}}
    (SOURCE/'provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    verify(rig,original)

def verify(rig=None,original=None):
    if rig is None:rig=json.loads((ROOT/'Assets/DarkFairytale/Monsters/MobRivenfang/MobRivenfang.json').read_text())
    if original is None:original=json.loads((ORIGINAL/'C030_S01B.json').read_text())
    for field in ('skeleton','bones','slots','ik','transform'):assert rig.get(field)==original.get(field),field
    assert rig['skins'][0]==original['skins'][0]
    for n,a in original['animations'].items():assert rig['animations'][n]==a,n
    for n,src in ALIASES.items():
        a=copy.deepcopy(rig['animations'][n]);a.pop('events',None);assert a==original['animations'][src],n
    assert len(rig['bones'])==62 and len(rig['slots'])==41
    print('PASS: original 62 bones / 41 slots / IK / transforms / meshes / UV / weights / 16 complete source animations unchanged; aliases differ only by game events.')
if __name__=='__main__':
    import sys
    verify() if '--verify' in sys.argv else build()
