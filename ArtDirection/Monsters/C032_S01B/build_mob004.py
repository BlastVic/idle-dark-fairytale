"""Explicit Mob004 source-sheet preparation and deterministic Spine export.

Never modifies the external original. Generated paint uses original part alpha,
canvas registration and geometry. Run prepare, then export after painting sheets.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[2]
ORIGINAL = ROOT.parent / 'Spine/monster2/C032_S01B'
SOURCE = BASE / 'source/Mob004'
UNITY = ROOT / 'Assets/DarkFairytale/Monsters/BlackForestSpider/Mob004'
EXTERNAL = ROOT.parent / 'Spine/export/monster/Mob004'


def part_image(path):
    im=Image.open(path).convert('RGBA')
    source=json.loads((ORIGINAL/'C032_S01B.json').read_text())
    for attachments in source['skins'][0]['attachments'].values():
        for name, attachment in attachments.items():
            if name!=path.stem or attachment.get('type')!='mesh': continue
            # Mesh rectangles in the source contain neighboring atlas fragments;
            # remove pixels outside the actual UV triangles, never edit the mesh.
            uv=attachment['uvs']; tri=attachment['triangles']
            mask=Image.new('L',im.size,0); draw=ImageDraw.Draw(mask)
            for i in range(0,len(tri),3):
                draw.polygon([(uv[v*2]*im.width,uv[v*2+1]*im.height) for v in tri[i:i+3]],fill=255)
            im.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
    return im


def duration(value):
    if isinstance(value, dict):
        return max([float(value.get('time', 0))] + [duration(v) for v in value.values()])
    if isinstance(value, list):
        return max([0] + [duration(v) for v in value])
    return 0


def prepare():
    SOURCE.mkdir(parents=True, exist_ok=True)
    files = sorted(p for p in (ORIGINAL / 'images').glob('*.png') if p.stem != 'shadow')
    main = [p for p in files if 'Leg' not in p.stem]
    legs = [p for p in files if 'Leg' in p.stem]
    layout = {}
    for group, paths in [('main', main), ('legs', legs)]:
        sheet = Image.new('RGBA', (1024, 1024), (232, 229, 222, 255))
        small = 0
        for i, p in enumerate(paths):
            im = part_image(p)
            if group == 'main' and p.stem == 'Front_Body_01':
                box = (28, 24, 572, 604)
            elif group == 'main' and p.stem == 'Front_Head_01':
                box = (624, 40, 964, 376)
            else:
                if group == 'legs':
                    x, y = (i % 4)*256, (i // 4)*340
                    area = (x+22, y+22, x+234, y+318)
                else:
                    x, y = (small % 6)*170, 660+(small // 6)*174
                    area = (x+24, y+16, x+146, y+152)
                    small += 1
                scale = min((area[2]-area[0])/im.width, (area[3]-area[1])/im.height)
                w, h = round(im.width*scale), round(im.height*scale)
                x = (area[0]+area[2]-w)//2
                y = (area[1]+area[3]-h)//2
                box = (x, y, x+w, y+h)
            sheet.alpha_composite(im.resize((box[2]-box[0], box[3]-box[1]), Image.Resampling.LANCZOS), box[:2])
            layout[p.stem] = dict(group=group, box=box, original_size=im.size)
        sheet.convert('RGB').save(SOURCE / (group+'-registration.png'))
    (SOURCE / 'layout.json').write_text(json.dumps(layout, indent=2)+'\n')
    print('Prepared', len(layout), 'parts in two registered sheets')


def export():
    layout = json.loads((SOURCE / 'layout.json').read_text())
    paints = {k: Image.open(SOURCE/(k+'-painted.png')).convert('RGB').resize((1024,1024),Image.Resampling.LANCZOS) for k in ['main','legs']}
    images = SOURCE / 'images'
    images.mkdir(exist_ok=True)
    parts = {}
    for name, info in layout.items():
        original = part_image(ORIGINAL/'images'/(name+'.png'))
        # Straight alpha source images; never derive an alpha mask from a flat background.
        crop = paints[info['group']].crop(info['box']).resize(original.size,Image.Resampling.LANCZOS).convert('RGBA')
        crop.putalpha(original.getchannel('A'))
        crop.save(images/(name+'.png'))
        parts[name] = crop
    parts['shadow'] = Image.open(ORIGINAL/'images/shadow.png').convert('RGBA')
    parts['shadow'].save(images/'shadow.png')
    atlas = Image.new('RGBA',(1024,1024),(0,0,0,0))
    entries=[]
    x=y=row=8
    for name, im in sorted(parts.items(), key=lambda item: -item[1].height):
        if x+im.width+8>1024: x=8; y+=row+16; row=0
        if y+im.height+8>1024: raise RuntimeError('Atlas overflow')
        atlas.paste(im,(x,y))
        # Edge extrusion in transparent padding protects linear filtering.
        atlas.paste(im.crop((0,0,1,im.height)).resize((2,im.height)),(x-2,y))
        atlas.paste(im.crop((im.width-1,0,im.width,im.height)).resize((2,im.height)),(x+im.width,y))
        entries.append(f'{name}\n  rotate: false\n  xy: {x}, {y}\n  size: {im.width}, {im.height}\n  orig: {im.width}, {im.height}\n  offset: 0, 0\n  index: -1\n')
        x+=im.width+16; row=max(row,im.height)
    atlas_text='\nMob004.png\nsize: 1024,1024\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n'+''.join(entries)
    original=json.loads((ORIGINAL/'C032_S01B.json').read_text())
    rig=copy.deepcopy(original)
    # Source JSON already uses 3.8 mesh/skin/timeline schema. Keep its honest version
    # provenance and validate it against the actual 3.8 loader rather than relabeling.
    rig['skeleton']['images']='./images/'
    rig['skeleton'].pop('audio',None)
    rig['skins'].append(dict(name='1',attachments=copy.deepcopy(rig['skins'][0]['attachments'])))
    rig['events']={'OnHit':{},'OnComplete':{}}
    mapping={'Idle1':'Idle_Battle_00','Attack1':'Atk1_00','Hit':'Hit_00','Death1':'Death_00'}
    for alias, source in mapping.items():
        rig['animations'][alias]=copy.deepcopy(original['animations'][source])
        if alias!='Idle1':
            events=([dict(time=.6667,name='OnHit')] if alias=='Attack1' else [])
            events.append(dict(time=duration(original['animations'][source]),name='OnComplete'))
            rig['animations'][alias]['events']=events
    for key in ['bones','slots','ik']: assert rig[key]==original[key]
    assert rig['skins'][0]==original['skins'][0]
    for name, value in original['animations'].items(): assert rig['animations'][name]==value
    for dest in [UNITY, EXTERNAL, SOURCE]:
        dest.mkdir(parents=True,exist_ok=True)
        (dest/'Mob004.json').write_text(json.dumps(rig,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
        (dest/'Mob004.atlas.txt').write_text(atlas_text,encoding='utf8')
        atlas.save(dest/'Mob004.png')
    report=dict(source=str(ORIGINAL),source_sha256=hashlib.sha256((ORIGINAL/'C032_S01B.json').read_bytes()).hexdigest(),
        bones=len(rig['bones']),slots=len(rig['slots']),parts=len(parts),model='gpt-image-2.5-flare',
        preserved=['bones','slots','IK','mesh UVs and weights','all ten original animations','original part alpha/canvas'],
        aliases={k:dict(source=v,duration=duration(original['animations'][v]),events=rig['animations'][k].get('events',[])) for k,v in mapping.items()},
        alpha='straight, original separate PNG alpha',native_source='Original .spine unchanged; adapted aliases supplied in editable JSON, not a newly saved .spine project')
    (SOURCE/'provenance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print(json.dumps(report,ensure_ascii=False,indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','export'])
    args=parser.parse_args()
    prepare() if args.action=='prepare' else export()
