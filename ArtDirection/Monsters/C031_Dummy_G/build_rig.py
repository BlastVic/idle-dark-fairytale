"""Preserve the supplied converted source rig; add game aliases and linked skin only."""
import copy, hashlib, json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
ORIGINAL=ROOT.parent/'Spine/monster2/动画/C031_Dummy_G'
SOURCE=BASE/'source'
ALIASES={'Idle1':'Idle_Battle_00','Attack1':'Atk1_00','Hit':'Hit_00','Death1':'Death_00'}
EVENTS={'Attack1':[{'time':0.4667,'name':'OnHit'},{'time':1.4333,'name':'OnComplete'}], 'Hit':[{'time':0.7667,'name':'OnComplete'}], 'Death1':[{'time':1.5333,'name':'OnComplete'}]}

def build():
    original=json.loads((ORIGINAL/'C031_Dummy_G.json').read_text())
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
    (SOURCE/'MobVesperling.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n')
    manifest={'monster_id':'MobVesperling','approved_design':'A-character-v1.png','painting':'built-in image_gen; exact model identifier not exposed','painting_prompts':'painting-prompts.json','native_project_status':'Original C031_Dummy_G.spine preserved; new skin delivered as JSON and part PNGs, no new native .spine saved','source':str(ORIGINAL),'source_version':original['skeleton']['spine'],'changes':['Added linked skin 1 inheriting default deform','Added four game animation aliases and events; no changes to original timelines or rig','Repacked nonrotated straight-alpha atlas from repainted source parts'], 'aliases':ALIASES,'events':EVENTS,'original_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ORIGINAL.iterdir() if p.is_file()}}
    manifest['original_images_sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ORIGINAL/'images').glob('*.png')}
    manifest['registration']='2x original canvas; UV-visible source alpha bounds; cap inset 5% canvas width and 2.5% height total to avoid original mesh clipping; generated alpha retained'
    manifest['atlas']='Unity PackTextures; 8 px padding; 4096 maximum; straight alpha; no rotation; verify packed dimensions equal part canvas'
    (SOURCE/'provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    verify(rig,original)

def verify(rig=None,original=None):
    if rig is None:rig=json.loads((ROOT/'Assets/DarkFairytale/Monsters/MobVesperling/MobVesperling.json').read_text())
    if original is None:original=json.loads((ORIGINAL/'C031_Dummy_G.json').read_text())
    for field in ('skeleton','bones','slots','ik','transform'):assert rig.get(field)==original.get(field),field
    assert rig['skins'][0]==original['skins'][0]
    for n,a in original['animations'].items():assert rig['animations'][n]==a,n
    for n,src in ALIASES.items():
        a=copy.deepcopy(rig['animations'][n]);a.pop('events',None);assert a==original['animations'][src],n
    assert len(rig['bones'])==20 and len(rig['slots'])==11
    print('PASS: original 20 bones / 11 slots / IK / transforms / meshes / UV / weights / 14 complete source animations unchanged; aliases differ only by game events.')
if __name__=='__main__':
    import sys
    verify() if '--verify' in sys.argv else build()
