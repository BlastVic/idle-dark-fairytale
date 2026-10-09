"""Adapt mon_7080 without changing source bones, IK, weighted meshes or animation curves.
Adds aliases/combat events, linked skin, short Hit, and documented skin boundary fills.
Run from any directory. Does not modify image pixels or the external original.
"""
import copy, hashlib, json
from pathlib import Path
BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[2]
SOURCE = ROOT.parent / 'Spine/monster/mon_7080/mon_7080.json'
original = json.loads(SOURCE.read_text())
rig = copy.deepcopy(original)
# Linked meshes inherit original default-skin deform timelines (notably spawn).
skin1={slot:{name:dict(type='linkedmesh',skin='default',parent=name,path=mesh.get('path',name),deform=True) for name,mesh in attachments.items()} for slot,attachments in rig['skins'][0]['attachments'].items()}
rig['skins'].append(dict(name='1', attachments=skin1))
rig['events'] = {'OnHit': {}, 'OnComplete': {}}
for alias, name, end in [('Idle1','idle',None), ('Attack1','attack_1',.8333), ('Death1','die',2.0)]:
    rig['animations'][alias] = copy.deepcopy(original['animations'][name])
    if end:
        rig['animations'][alias]['events'] = ([{'time':.3667,'name':'OnHit'}] if alias=='Attack1' else []) + [{'time':end,'name':'OnComplete'}]
# Stun is a one-second continuous bowed sway, unsuitable for an immediate impact.
# Hold the original idle starting pose, then add one damped recoil on its existing bones.
hit={}
for section, tracks in original['animations']['idle'].items():
    if section not in ('bones','slots','ik'): continue
    if section=='ik':
        hit[section]={key:[dict(value[0],time=0)] for key,value in tracks.items()}
    else:
        hit[section]={bone:{kind:[dict(keys[0],time=0)] for kind,keys in timelines.items()} for bone,timelines in tracks.items()}
for bone, kind, values in [
    ('center','translate',[(0,0,0),(.055,-95,-30),(.13,-40,8),(.23,12,0),(.34,0,0)]),
    ('center','rotate',[(0,0),(.055,8),(.13,-3),(.23,1),(.34,0)]),
    ('head','rotate',[(0,0),(.07,-9),(.16,4),(.25,-1),(.34,0)])]:
    baseline=hit['bones'].get(bone,{}).get(kind,[{}])[0]
    keys=('x','y') if kind=='translate' else ('angle',)
    hit['bones'].setdefault(bone,{})[kind]=[dict(time=t, **{k:round(baseline.get(k,0)+v,4) for k,v in zip(keys,delta)}) for t,*delta in values]
hit['events']=[{'time':.34,'name':'OnComplete'}]
rig['animations']['Hit']=hit
for key in ('bones','slots','ik'): assert rig[key]==original[key]
assert rig['skins'][0]==original['skins'][0]
for key,value in original['animations'].items(): assert rig['animations'][key]==value
# Repainted smooth faces need UV coverage across old fur notches; same vertex weights.
from fit_skin_mesh import fit
fit(rig)
(BASE/'source/rig-template.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n')
report={'source':str(SOURCE),'sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'bones':len(rig['bones']),'slots':len(rig['slots']),'original_animations':list(original['animations']),'unchanged':['bones','slots','ik','default skin vertex weights/UVs (boundary coverage extended; duplicate eye hidden)' ,'all seven original animation timelines'],'aliases':{'Idle1':'idle','Attack1':'attack_1','Death1':'die'},'authored':['Hit (0.34s)', 'combat events only on aliases and Hit']}
(BASE/'source/original-rig/provenance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
