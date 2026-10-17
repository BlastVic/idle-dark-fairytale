"""Explicit production resize of generated equipment icons; never runs on import.
Run with bundled Python/Pillow. Default stages previews; --apply installs only a full set.
"""
import argparse, json, re, zipfile
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/ArtSources/UI/Equipment/FullSetV1'
OUT = ROOT / 'output/equipment-fullset'
RUNTIME = ROOT / 'Assets/DarkFairytale/UI/Equipment/Icons'
rows = json.loads((SOURCE / 'prompts.json').read_text())['items']
parser = argparse.ArgumentParser()
parser.add_argument('--apply', action='store_true')
args = parser.parse_args()
OUT.mkdir(parents=True, exist_ok=True)
missing = []
audit = []
for row in rows:
    src = SOURCE / (row['key'] + '.png')
    if not src.exists():
        missing.append(row['key'])
        continue
    im = Image.open(src).convert('RGBA')
    alpha = im.getchannel('A')
    assert alpha.getextrema()[0] == 0 and alpha.getextrema()[1] == 255, src
    # Preserve the generated art and alpha, only resize as explicitly requested.
    im = im.resize((256, 256), Image.Resampling.LANCZOS)
    dst = OUT / 'staged' / row['folder'] / (row['key'] + '.png')
    dst.parent.mkdir(parents=True, exist_ok=True)
    im.save(dst, optimize=True)
    audit.append(dict(key=row['key'], path=str(dst.relative_to(ROOT)), bytes=dst.stat().st_size,
                      size=list(im.size), alpha_bbox=im.getchannel('A').getbbox()))
for folder in dict.fromkeys(r['folder'] for r in rows):
    items = [r for r in rows if r['folder'] == folder and r['key'] not in missing]
    if not items: continue
    sheet = Image.new('RGB', (6 * 180, ((len(items) + 5)//6) * 202), '#c9c0ac')
    draw = ImageDraw.Draw(sheet)
    for i, row in enumerate(items):
        im = Image.open(OUT/'staged'/folder/(row['key']+'.png')).convert('RGBA')
        im.thumbnail((160,160), Image.Resampling.LANCZOS)
        x,y = (i%6)*180+10, (i//6)*202+8
        sheet.paste(im, (x,y), im)
        draw.text((x+5,y+165),row['key'],fill='#211b17')
    sheet.save(OUT/(folder+'-contact.png'))
(OUT/'pixel-audit.json').write_text(json.dumps(dict(items=audit, missing=missing),indent=2))
print(f'Staged {len(audit)}/140; missing: {missing}')
if args.apply:
    assert not missing, 'Full set required before switching runtime art'
    backup = OUT/'before.zip'
    if not backup.exists():
        with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED) as z:
            for p in RUNTIME.rglob('*'):
                if p.is_file(): z.write(p,p.relative_to(ROOT))
            z.write(ROOT/'Assets/Scenes/Gameplay.unity','Assets/Scenes/Gameplay.unity')
    for row in rows:
        dst=RUNTIME/row['folder']/(row['key']+'.png')
        meta=Path(str(dst)+'.meta')
        assert meta.exists(), f'Missing production GUID metadata: {meta}'
        text=meta.read_text()
        text=re.sub(r'maxTextureSize: \d+', 'maxTextureSize: 256',text)
        text=text.replace('filterMode: -1','filterMode: 1')
        meta.write_text(text)
        dst.write_bytes((OUT/'staged'/row['folder']/(row['key']+'.png')).read_bytes())
    print('Installed all 140 icons with preserved GUIDs.')
