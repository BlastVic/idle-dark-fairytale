"""Compose an offline catalog and contact sheets from the Unity-rendered previews."""
from pathlib import Path
import json,html,math
from PIL import Image,ImageDraw,ImageFont
D=Path(__file__).resolve().parent
m=json.loads((D/'manifest.json').read_text()); rows=m['items']; root=D.parents[2]
labels={'Mob':'普通怪','MiniBoss':'小 Boss','Boss':'Boss','Extra':'额外美术资源'}
order={'Mob':0,'MiniBoss':1,'Boss':2,'Extra':3}
rows.sort(key=lambda r:(order[r['category']],int(''.join(filter(str.isdigit,r['id'])) or 0) if r['category']!='Extra' else r['id']))
font=ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',23)
small=ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',18)
for group in ['all','Mob','Bosses','Extra']:
 rs=[r for r in rows if group=='all' or r['category']==group or group=='Bosses' and r['category'] in ['MiniBoss','Boss']]
 cols=6 if group=='all' else 5 if group=='Mob' else 4
 w=cols*260; h=100+math.ceil(len(rs)/cols)*300
 im=Image.new('RGB',(w,h),'#10161e'); dr=ImageDraw.Draw(im)
 dr.text((24,22),f'LEGACY MONSTERS / {group.upper()} / {len(rs)} entries',font=font,fill='#eee4cd')
 dr.text((24,57),'Unity renders | original prefab skin | normalized scale',font=small,fill='#93a1b4')
 for i,r in enumerate(rs):
  x=i%cols*260; y=100+i//cols*300
  tile=Image.open(D/'images'/r['image']);tile.thumbnail((250,240));im.paste(tile,(x+(260-tile.width)//2,y))
  dr.text((x+12,y+242),r['id'].replace('Extra_','')+' / '+r['skin'],font=font,fill='#e6d9be')
  dr.text((x+12,y+272),r['family'],font=small,fill='#9fb0c5')
 im.save(D/f'overview-{group}.jpg',quality=93)
head='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>旧怪物图鉴</title><style>
*{box-sizing:border-box}body{margin:0;background:#10161e;color:#eadfc9;font:16px/1.6 system-ui,sans-serif}header,main{max-width:1500px;margin:auto;padding:32px}h1{font-size:38px;margin:0}p{color:#a9b5c5;max-width:1100px}nav{display:flex;gap:10px;flex-wrap:wrap;position:sticky;top:0;padding:14px;background:#10161ef5;z-index:1}input,select{background:#253242;color:#fff;border:1px solid #536078;padding:12px;border-radius:8px;font:inherit}input{flex:1;min-width:220px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:18px}article{border:1px solid #334052;border-radius:12px;overflow:hidden;background:#192028}img{width:100%;display:block}section{padding:16px}h2{font-size:20px;margin:0}small{color:#9eacbf}details{font-size:12px;overflow-wrap:anywhere;margin-top:12px;color:#a9b5c5}.tag{color:#dcb679;font-size:13px}a{color:#baceed}.hidden{display:none}</style>
<header><small>PROJECT ARCHIVE · 2026-10-09</small><h1>旧怪物图鉴</h1><p>42 个普通怪 · 5 个小 Boss · 7 个 Boss · 5 套额外美术。共 59 条记录，对应 58 套独立骨骼资源（Mob24 和 Mob42 共用同一资源）。点击图片查看 640×640 原图。</p><p>展示图为项目现存旧 Spine 资源的 Unity 实际渲染，采用 Git HEAD 预制体记录的皮肤和初始动画，采样时间 0.1 秒；每张独立缩放，不能用于比较游戏内体型。额外资源使用首个非 default 皮肤及 Idle 动画。名称沿用资源文件夹名。Fish1 原 SkeletonData 缺少图集引用，导出时仅在内存副本中补接同目录图集，未修改源资源。</p><p>Mob1、Mob2、Mob4 当前已换成蘑菇，本图鉴保留其原始史莱姆形象。“额外”仅指未被这 54 个敌人预制体引用，不代表整个项目未使用。每条记录显示一套代表皮肤，非全部换色图集。</p><nav><input id="search" placeholder="搜索 ID、名称、路径…"><select id="kind"><option value="">全部 59 条</option value="Mob">普通怪 42</option><option value="MiniBoss">小 Boss 5</option><option value="Boss">Boss 7</option><option value="Extra">额外资源 5</option></select><small id="count"></small></nav></header><main><div class="grid">'''
cards=[]
for r in rows:
 e=lambda x:html.escape(str(x),quote=True)
 j=json.loads((root/Path(r['asset']).parent/'skeleton.json').read_text()); skins=j.get('skins',[]); names=[s['name'] for s in skins] if isinstance(skins,list) else list(skins)
 replaced=r['category']!='Extra' and r['asset']!=r['currentAsset']
 cards.append(f'''<article data-kind="{r['category']}" data-search="{e(r['id']+' '+r['family']+' '+r['asset'])}"><a href="images/{e(r['image'])}" target="_blank"><img loading="lazy" src="images/{e(r['image'])}" alt="{e(r['id']+' '+r['family'])}"></a><section><small>{labels[r['category']]}</small><h2>{e(r['id'].replace('Extra_',''))} · {e(r['family'])}</h2><small>展示皮肤 {e(r['skin'])} · {e(r['animation'])}</small>{'<div class="tag">当前预制体已替换为蘑菇</div>' if replaced else ''}<details><summary>资源路径与皮肤</summary><p>预制体：{e(r['prefab'] or '无对应敌人预制体')}</p><p>旧资源：{e(r['asset'])}</p><p>皮肤：{e(', '.join(names))}</p>{'<p>当前资源：'+e(r['currentAsset'])+'</p>' if replaced else ''}</details></section></article>''')
end='''</div><p>数据来源：manifest.json · 导出结果：export.log · 预制体基准：'''+m['revision']+'''</p></main><script>const s=document.querySelector('#search'),k=document.querySelector('#kind');function filter(){let n=0;document.querySelectorAll('article').forEach(a=>{const yes=(!k.value||a.dataset.kind===k.value)&&a.dataset.search.toLowerCase().includes(s.value.toLowerCase());a.classList.toggle('hidden',!yes);n+=yes});document.querySelector('#count').textContent=n+' 条'}s.oninput=k.onchange=filter;filter()</script></html>'''
(D/'index.html').write_text(head+''.join(cards)+end)
print('Generated catalog and four overview sheets')
