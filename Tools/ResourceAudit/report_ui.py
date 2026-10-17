#!/usr/bin/env python3
"""Turn the read-only Unity production audit into reviewable CSV manifests (stdlib only)."""
import collections,csv,json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[2]
report=json.loads((ROOT/'output/ui-production-audit/ui-nodes.json').read_text())
catalog=json.loads((ROOT/'Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json').read_text())['entries']
converted={x['sprite']:x for x in catalog}
for entry in catalog:
 for part in entry.get('frameParts') or []: converted[part['sprite']]=entry
 for part in entry.get('variants') or []: converted.setdefault(part['sprite'],entry)
out=ROOT/'Docs/Production/ResourceAudit';out.mkdir(parents=True,exist_ok=True)
build={}
p=ROOT/'output/resource-audit/assets.csv'
if p.exists():
 for x in csv.DictReader(p.open()):build[x['path']]=any(x.get(k)=='True' for k in ['scene_dependency','resources_dependency','addressable_dependency','settings_dependency'])
repair={'UI/Reference Ink Cutout','UI/Chest Authored Cutout','UI/Shared Navigation Art','UI/Skill Illustration Cutout','UI/Storybook Map Cutout','UI/Equipment Light Ink','UI/Settings Wax Outline'}
known_text={'PanelFrameV3','DetailsFrameV2','CellFrame','SelectedCellFrame','ToolbarButtonFrame','CategoryBookmarkEdge','PaperButtonFrame','RedButtonFrame','AttributesFrameV2','StatCellFrame','PlusBorder','AttributeBorder','NavigationBookmarkBorder','SkillBadge','HeaderFrame','DetailFrame','EnterFrame','PageFrameV2','OuterFrameV2','HeaderBorder','HealthFrame','ExperienceFrame','ChestBorder','EmptyDashedBorder','ReadyPlate','BattleRibbon','CleanSkill5','CleanSkill11','CleanSkill9'}
def write(name,rows,fields):
 with (out/name).open('w',newline='',encoding='utf-8-sig') as f:
  w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
rows=[]
for a in report['entries']:
 t=a['texture'];s=a['sprite'];name=pathlib.Path(s).stem
 design=bool(re.search(r'(Approved|BlankLayout|BlankLabels|BlankDaily|GuideReference)',t))
 flags=[]
 if design:flags.append('整张设计图依赖')
 if a['shader'] in repair:flags.append('运行时材质抠图/遮盖/平铺，需要逐项替换')
 if design and (name in known_text or name.startswith('NodeLabelFrame')):flags.append('原裁片有文字/状态图案；边框不显示中心时不代表运行时可见')
 if not flags and s not in converted:continue
 mode=''
 if a['materialJson']:
  try:mode=next((p['second'] for p in json.loads(a['materialJson'])['Material']['m_SavedProperties']['m_Floats'] if p['first']=='_Mode'),'')
  except (KeyError,TypeError):pass
 rows.append(dict(owner=a['prefab'],node=a['node'],sprite=s,texture=t,shader=a['shader'],material=a['material'],mode=mode,image_type=a['imageType'],fill_center=a['fillCenter'],source_rect=str(a['spriteRect']),layout_rect=str(a['displayRect']),build_dependency=build.get(a['prefab'],'未确认'),issue='；'.join(flags),status=('已拆为可编辑四角/边缘' if '/FrameParts/' in s else '已拆独立资源') if s in converted else '待修复与实景确认'))
write('UI逐节点排查.csv',rows,list(rows[0]) if rows else [])
refs=[x for x in report.get('references',[]) if x['component'] not in ('Image','RawImage','Text','TMP_Text','TextMeshProUGUI') and '/UI/' in x['target']]
write('UI脚本与材质绑定.csv',refs,['owner','node','component','property','target','texture'])
tex=[]
for a in report.get('textures',[]):
 if not re.search(r'(Monster|Companion|RedHood|Effects|Spine)',a['path'],re.I):continue
 tex.append(dict(a,build_dependency=build.get(a['path'],'未确认'),assessment='待测各动画及相机下最大屏幕像素；图集总尺寸不能代替单部件密度',memory_note='runtimeBytes为编辑器运行内存估算，非APK压缩占用'))
write('角色怪物特效贴图.csv',tex,list(tex[0]) if tex else ['path','assessment'])
write('已拆资源来源.csv',catalog,sorted(set().union(*(x.keys() for x in catalog))))
density_path=ROOT/'output/ui-production-audit/spine-density.json'
density=json.loads(density_path.read_text()) if density_path.exists() else {'entries':[]}
write('角色怪物特效屏幕密度.csv',density['entries'],list(density['entries'][0]) if density['entries'] else [])
live_path=ROOT/'output/ui-production-audit/ui-density-live.json'
live=json.loads(live_path.read_text()) if live_path.exists() else {'entries':[]}
live['entries']=[x for x in live['entries'] if not converted.get(x['sprite'],{}).get('frameParts')]
write('UI实际显示密度.csv',live['entries'],list(live['entries'][0]) if live['entries'] else [])
for x in tex:
 matches=[r for r in density['entries'] if r['texture']==x['path']]
 if matches:x['assessment']='已测正式相机/缩放、各动画65个采样及四种屏幕规格；保留现尺寸，详见屏幕密度表'
 else:x['assessment']='未纳入本次正式战斗动态采样；保持原尺寸，不据此批准缩小'
write('角色怪物特效贴图.csv',tex,list(tex[0]) if tex else ['path','assessment'])
manual_path=out/'素材人工复核.csv'
if manual_path.exists():
 manual=list(csv.DictReader(manual_path.open(encoding='utf-8-sig')))
 for x in manual:
  if x['sprite'] in converted:x['状态']='已拆独立正式素材；原问题和来源留作追溯'
  known={x['sprite'] for x in manual}
 for entry in catalog:
  if entry['sprite'] not in known:manual.append(dict(sprite=entry['sprite'],source=entry['source'],人工复核='补充覆盖：状态变体/隐藏脚本图片/正式尺寸素材',材质=entry.get('originalMaterial',''),说明=entry.get('treatment',''),状态='已迁移；来源与具体处理见已拆资源来源表',本地接触表='output/ui-production-audit/'))
 write('素材人工复核.csv',manual,list(manual[0]))
issues=[x for x in rows if x['issue']]
design_refs=[x for x in report.get('references',[]) if re.search(r'(Approved|BlankLayout|BlankLabels|BlankDaily|GuideReference)',x.get('texture',''))]
frames=[x for x in catalog if x.get('frameParts')]
flow=ROOT/'output/ui-production-audit/flow-result.txt'
with (out/'排查状态.txt').open('w') as f:
 f.write(f"范围：{len(report['entries'])} 个非文字 Graphic、全部 Prefab/场景的序列化图片和材质引用。包含未启用节点与嵌套重复，节点数不等于独立资源数。\n迁移目录：{len(catalog)} 项（包含独立原 Sprite、4 个状态变体及尺寸优化素材）；保留原 GUID。\n当前整张设计图/旧抠图材质疑点节点：{len(issues)}；脚本残留设计图引用：{len(design_refs)}。\n\n已完成：独立透明素材、真实文本分离、公共资源按像素内容复用、3 个紧凑平铺边框、{len(frames)} 个独特装饰框拆成持久化四角/四边、纸纹独立化、制作工具同步接入。技能缺角按批准范围补绘；其余像素保持原主体。\n")
 f.write("原稿与制作参照可保留于工程，入包判断以构建依赖为准；Editor 目录的离线烘焙 Shader 不用于运行时抠图。正常灰化、纸纹重复、圆形纸面及光效材质保留。\n\n")
 f.write(f"角色/怪物/伙伴/特效：{len(density['entries'])} 组测量，1080×1920、1440×2560、1440×1920、1440×2880。逐三角形 UV 与屏幕投影比较，包含普通粒子和动画采样。薄三角形导致极大最大值时须结合面积加权 P99；该统计不取代少数细节的视觉检查。未统一缩图，未改变正式特效尺寸。\nUI 实际显示密度记录：{len(live['entries'])} 项，仅代表验证流程经过的活动状态；1440 宽数据为等比例外推，不是实机测量。\n\n")
 f.write("字体保持不变。纹理像素、编辑器内存、源文件大小、APK 压缩体积不能混算；本次不据拆图像素减少量宣称 APK 已达 150 MB。\n实景截图在忽略目录 output/；详细功能结果如下。未经完整渲染、交互、构建检查的部分不能称为上线验收通过。\n\n")
 if flow.exists():f.write(flow.read_text())
 f.write('\n完整页面结果见功能验证结果.csv；实际构建体积与比较见 APK构建核对.txt。\n注意：旧测试退出时曾误写本地编辑器存档，已修复并完成隔离验证；影响及处理状态详见编辑器存档影响与修复.txt。\n')
validation=[]
for label,folder in [('主界面','home-ui'),('装备','equipment-ui'),('技能','skills-ui'),('地图','map-ui'),('宝箱','chest-ui'),('融合','fusion-ui'),('转生','rebirth-ui'),('商店','shop-ui'),('设置/启动偏好','settings-ui'),('经验加成','experience-ui'),('引导','tutorial-ui'),('每日提醒','daily-reward'),('每日直接/广告领取','daily-supply'),('本地资源及战斗胜负返回','packaging-fix')]:
 p=ROOT/'output'/folder/'validation.txt'
 text=p.read_text() if p.exists() else ''
 failed=len(re.findall(r'^FAIL|^ERROR',text,re.M))
 complete=any(x in text for x in ['COMPLETE','DONE','final visual + interaction regression','all map checks'])
 validation.append(dict(页面=label,状态='失败' if failed else '通过' if complete else '未完成',失败或错误行=failed,本地证据=str(p.relative_to(ROOT))))
write('功能验证结果.csv',validation,list(validation[0]))
shared=[]
uses=collections.defaultdict(set)
for entry in report['entries']:
 if entry['sprite']:uses[entry['sprite']].add(entry['prefab'])
for sprite,owners in sorted(uses.items(),key=lambda p:-len(p[1])):
 if len(owners)>1 and sprite in converted:shared.append(dict(sprite=sprite,owner_count=len(owners),owners=' | '.join(sorted(owners))))
write('公共素材复用.csv',shared,['sprite','owner_count','owners'])
print('graphics',len(report['entries']),'flagged nodes',len(issues),'design refs',len(design_refs),'catalog',len(catalog),'density',len(density['entries']))
