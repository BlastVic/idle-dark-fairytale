"""Preserve mon_7320; add linked skin, three aliases and one authored short Hit."""
import copy, hashlib, json
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[2]
ORIGINAL=ROOT.parent/'Spine/monster/mon_7320'
SOURCE=BASE/'source'
ALIASES={'Idle1':'idle','Attack1':'attack_1','Death1':'die'}
EVENTS={'Attack1':[{'time':.3333,'name':'OnHit'},{'time':.6667,'name':'OnComplete'}],'Death1':[{'time':1.2333,'name':'OnComplete'}],'Hit':[{'time':.4,'name':'OnComplete'}]}
def hit_animation(original):
    bones={}
    for bone,tracks in original['animations']['idle']['bones'].items():
        bones[bone]={}
        for track,keys in tracks.items():
            key={k:v for k,v in keys[0].items() if k not in ('time','curve','c2','c3','c4')}
            bones[bone][track]=[dict(key),dict(key,time=.4)]
    # A brief recoil from idle, using the same weighted rig and original mesh weights.
    for bone,track,offset in [('center','rotate',{'angle':-9}),('center','translate',{'x':-60,'y':-20}),('head','rotate',{'angle':13}),]:
        base=bones.get(bone,{}).get(track,[{}])[0]
        recoil={k:base.get(k,0)+v for k,v in offset.items()}
        peak=dict(base,**recoil,time=.08)
        bones.setdefault(bone,{})[track]=[dict(base),peak,dict(base,time=.4)]
    return {'bones':bones,'events':EVENTS['Hit']}
def build():
    original=json.loads((ORIGINAL/'mon_7320.json').read_text());rig=copy.deepcopy(original)
    skin=copy.deepcopy(rig['skins'][0]);skin['name']='1'
    for slot,atts in skin['attachments'].items():
        for name,a in list(atts.items()):
            if a.get('type')=='mesh':atts[name]={'type':'linkedmesh','path':a.get('path',name),'skin':'default','parent':name,'deform':True,'width':a['width'],'height':a['height']}
    rig['skins'].append(skin);rig.setdefault('events',{}).update({'OnHit':{},'OnComplete':{}})
    for name,source in ALIASES.items():
        rig['animations'][name]=copy.deepcopy(original['animations'][source])
        if name in EVENTS:rig['animations'][name]['events']=EVENTS[name]
    rig['animations']['Hit']=hit_animation(original)
    SOURCE.mkdir(exist_ok=True)
    (SOURCE/'MobMurmlet.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n')
    manifest={'monster_id':'MobMurmlet','approved_design':'B-character-v3.png','painting':'built-in image_gen; exact model not exposed','source':str(ORIGINAL),'source_version':original['skeleton']['spine'],'native_project_status':'Original native project preserved; new native MobMurmlet.spine pending','aliases':ALIASES,'events':EVENTS,'authored_Hit':'0.4s recoil on original center/head from idle first pose; no source curve changes','original_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in ORIGINAL.iterdir() if p.is_file()},'original_images_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ORIGINAL/'images').glob('*.png')},'atlas':'straight alpha, nonrotated, 8px padding, max4096, 2x source canvas; generated alpha retained','registration':'UV-visible source bounds; measured cell borders and uniform registration.json transforms; no UV/weight edits'}
    manifest['approved_changes']=['top PNG fully transparent; original rig/curves unchanged','Hazy closed-eye full moon: body alone uses painted-body-v2.png']
    manifest['utility_parts']='top: transparent; shadow: original; other nine parts generated with native alpha'
    (SOURCE/'provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');verify(rig,original)
def verify(rig=None,original=None):
    if original is None:original=json.loads((ORIGINAL/'mon_7320.json').read_text())
    if rig is None:rig=json.loads((ROOT/'Assets/DarkFairytale/Monsters/MobMurmlet/MobMurmlet.json').read_text())
    for k,v in original.items():
        if k not in ('skins','animations','events'):assert rig[k]==v,k
    assert rig['skins'][0]==original['skins'][0]
    for n,a in original['animations'].items():assert rig['animations'][n]==a,n
    for n,src in ALIASES.items():
        a=copy.deepcopy(rig['animations'][n]);a.pop('events',None);assert a==original['animations'][src],n
    assert rig['animations']['Hit']==hit_animation(original)
    assert len(rig['bones'])==18 and len(rig['slots'])==14
    print('PASS original 18 bones / 14 slots / mesh UV weights / seven complete animation timelines unchanged; added linked skin, three aliases/events and authored Hit.')
if __name__=='__main__':
    import sys
    verify() if '--verify' in sys.argv else build()
