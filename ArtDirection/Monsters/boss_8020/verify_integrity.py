"""Repeatable source, atlas-size and export integrity verification (stdlib only)."""
import hashlib,json,re,struct
from pathlib import Path
import build_rig
b=Path(__file__).resolve().parent;s=b/'source';r=build_rig.ROOT/'Assets/DarkFairytale/Monsters/MobDrownedKing'
build_rig.verify()
audit=json.loads((b/'source-audit.json').read_text())
for name,digest in audit['files'].items():assert hashlib.sha256((build_rig.ORIGINAL/name).read_bytes()).hexdigest()==digest,name
for p in (s/'original').rglob('*'):
    if p.is_file():assert p.read_bytes()==(build_rig.ORIGINAL/p.relative_to(s/'original')).read_bytes(),str(p)
for ext in ['.json','.atlas.txt','.png']:
    assert (r/('MobDrownedKing'+ext)).read_bytes()==(build_rig.ROOT.parent/'Spine/export/monster/MobDrownedKing'/('MobDrownedKing'+ext)).read_bytes()
assert (s/'MobDrownedKing.json').read_bytes()==(r/'MobDrownedKing.json').read_bytes()
regions={}
for name,w,h in re.findall(r'\n([^\n]+)\n  rotate: false\n  xy: [^\n]+\n  size: (\d+), (\d+)',(r/'MobDrownedKing.atlas.txt').read_text()):regions[name]=(int(w),int(h))
assert len(regions)==10
for name,size in regions.items():
    p=s/'images'/(name+'.png');assert struct.unpack('>II',p.read_bytes()[16:24])==size,name
rig=json.loads((r/'MobDrownedKing.json').read_text())
for skin in rig['skins']:
    for slot,atts in skin['attachments'].items():
        for name,a in atts.items():assert a.get('path',name) in regions,(slot,name)
print('PASS original files/source image backups unchanged; 10 atlas regions cover all attachments; packed sizes match source parts; external export byte-identical to project runtime.')
