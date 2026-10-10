# 当前交付：独立怪物编号 v11（2026-10-10）

| 配置键 / Prefab | 怪物 | Spine 模板 |
| --- | --- | --- |
| Mob001 | 蜡斑菇 | mon_7080 |
| Mob002 | 木纹菇 | mon_7081 |
| Mob003 | 梦眼菇 | mon_7082 |

Unity 目录为 `Assets/DarkFairytale/Monsters/BlackForestMushroom/Mob001/` 至 `Mob003/`；可编辑分件在 `source/Mob001/` 至 `Mob003/`；同级 `Spine/export/monster/` 下也使用新编号。原 Prefab、贴图、SkeletonData 的 GUID 保留，资源文件名、Addressables 地址、敌人数据库键和现有蘑菇波次配置同步改名。

原版 `Mob1`、`Mob2`、`Mob4` 已恢复为 Slime、Slime2、Slime3；ItemDrop、Map1、Map2、Map3 中先前被替换的史莱姆也恢复。原版资源仅作参照，后续不得覆盖。通过 Gameplay 中 WaveManager 的波次 enemies 配置选择 `Mob001`、`Mob002`、`Mob003` 使用新怪物，旧关卡的原版键保持有效。注意 `Mob1` 与 `Mob001` 是两个独立键。

构建器已删除覆盖原版 Prefab 和场景史莱姆的入口。动画出场测试只针对新编号；`Validate Black Forest Mushrooms` 同时检查六个新旧资源的数据库、Addressables 与 Spine 来源。旧版报告中的“全游戏替换”和旧键加载蘑菇已废止，以下仅保留历史。

验证：六个新旧资源的数据库、Addressables 和 Spine 来源通过；三只蘑菇原七套动画与十二次战斗动画检查通过；实际 Gameplay 使用新编号完成四次出场，运行错误 0。原版三个 Prefab 与四个场景逐字节匹配替换前版本 d97a307。报告和实景截图仅存本地 `output/monster-animation/identity-migration/` 与 `output/monster-animation/entrance/`。

---

# 历史交付：独立模板蘑菇 v10（2026-10-10）

三只蘑菇现在分别使用对应的原始 Spine 模板：

| 外观 | 来源模板 | 骨骼 | Unity 资源／旧键 |
| --- | --- | --- | --- |
| 蜡斑菇 | mon_7080 | 27 | BF_Mushroom_Wine / Mob1（本次未改） |
| 木纹菇 | mon_7081 | 26 | BF_Mushroom_Moss / Mob2 |
| 梦眼菇 | mon_7082 | 26 | BF_Mushroom_Moon / Mob4 |

木纹菇、梦眼菇依据各自新概念图与原始 UV 分件参考重新绘制。扁斜木纹帽与圆拱月白帽使用各自的原始网格、权重和动画，不再套用 mon_7080。梦眼菇的下颚追加了一次平滑边缘修正，避免原尖齿遮住细齿笑脸。所有图像由内置 imagegen 绘制；原始分件和修订提示词在 `source/original-rig/separate-rigs-prompts.txt` 及 `mon_7082/jaw-correction-prompt.txt`。

## 可用交付

- Unity 现有两套资源已更新：`Assets/DarkFairytale/Monsters/BlackForestMushroom/BF_Mushroom_Moss/`、`BF_Mushroom_Moon/`。包含 JSON、atlas.txt、PNG、材质、SkeletonData 和可直接使用的 Prefab。
- 外部同内容导出：项目同级 `Spine/export/monster/BF_Mushroom_Moss/`、`BF_Mushroom_Moon/`。
- 可编辑分件 PNG 与骨架 JSON：`source/BF_Mushroom_Moss/`、`source/BF_Mushroom_Moon/`。
- 对应模板的坐标参考、透明绘制源图、源文件校验值：`source/original-rig/mon_7081/`、`mon_7082/`。
- 本次交付为 Spine 3.8 JSON／图集及 Unity 资源，没有另存新的原生 `.spine` 工程。原始 `.spine` 与原导出未改。

现有 SkeletonData、Prefab 的 GUID 和资源键保持不变，因此既有关卡、场景、Mob2/Mob4 引用直接获得新外观。Prefab 数值、掉落、血条节点和原地 spawn 配置保持原样。本次未重建酒红蘑菇，也未改战斗布局或 UI。

## 重建与验证

1. `python3 ArtDirection/Monsters/BlackForestMushroom/build_rig.py`：为三套来源分别生成骨架模板；原七套动画、骨骼、IK、UV 和权重逐项保持，继续使用已记录的 UV 凹边补面与旧眼睛隐藏策略。
2. Unity 退出 Play，执行 `Tools > Dark Fairytale > Monsters > Update 7081 and 7082 Skins`。此入口只更新后两套资源，不覆盖现有 Prefab 手工设置。完整三怪初建菜单仍可用，但会重建 Prefab。
3. `python3 ArtDirection/Monsters/BlackForestMushroom/verify_exports.py`：按每只的来源分别比较，检查 Unity 与外部导出一致。
4. `Validate Black Forest Mushrooms`：七套原动画的双皮肤变形一致性、战斗别名事件、受击打断、朝向、材质、有限顶点；八类动作抽帧覆盖三种骨架的整体动作范围。
5. `Test Legacy Slime Replacement (Play mode)`：旧键加载、完整原地 spawn、出场伤害与选敌保护。已通过，运行错误 0。

最终实战回归：9:16（720×1280）、3:4（768×1024）、1:2（720×1440）均完成实际 Unity 渲染及边界检查；三怪同屏、胜利结算、失败结算、返回营地通过，最终完整一轮运行错误为 0。流程测试采用临时玩家生命恢复以确定性覆盖胜利路径，不作为关卡平衡结论。曾有一次测试重复启动报错，之后已完成独立回归。

本次 Unity 实景、动画检查与报告归档于 `output/monster-animation/separate-rigs/`，截图仅保留本地。历史 v9 及以下的“三只共用 mon_7080”描述已被本节替代。

---

# 历史交付：差异化蘑菇 v9（2026-10-09）

以 `concepts/mushroom-trio-redesign-v4-fists.png` 为已选方案：蜡斑菇（酒红粗牙脸／菌瘤拳）、木纹菇（苔绿木脸／木节拳）、梦眼菇（月白面具／孢子壳拳）。内置 imagegen 重绘的三套透明分件位于 `source/original-rig/`，提示词在 `redesign-v4-prompts.txt`，旧图保存在 `before-v4-redesign/`。当前为 Unity JSON／atlas／PNG／Prefab 交付，未制作新的原生 .spine 编辑工程。

## 骨架与网格适配

保持原 27 骨骼、15 插槽、IK、所有顶点权重与 UV、原七套动画关键帧。`fit_skin_mesh.py` 只在头部、下颚、菌帽、拳头的旧 UV 凹边补三角面，避免原毛刺边界裁掉新木脸和面具；顶点数量、顺序和坐标不变，原 deform 继续工作。已绘在脸上的眼睛替代旧眼睛，默认和 linked skin 的旧 eye 附件透明度设为 0。未新增动画骨骼或重制原动作。此前“默认皮肤完全不改”的记录仅适用于 v5，本版有上述明确的局部皮肤网格调整。

## 全游戏普通史莱姆替换

- Mob1 → BF_Mushroom_Wine 外观。
- Mob2 → BF_Mushroom_Moss 外观。
- Mob4 → BF_Mushroom_Moon 外观。

原 Mob1/Mob2/Mob4 Prefab 的 GUID 和资源键保留，替换它们引用的 SkeletonData、朝向、材质、皮肤和出场配置；原生命、伤害、经验、掉落、攻击间隔保留。因此关卡仍可出现这些旧键，但实际加载的是蘑菇，不是史莱姆。三套独立 BF_Mushroom Prefab 同步更新，维持之前首关测试数值。史莱姆小 Boss 不变。

旧 ItemDrop、Map1、Map2、Map3 场景中直接嵌入的普通史莱姆也已替换。旧美术源文件仅作为存档保留；不应再有场景／Prefab 引用三套普通史莱姆 SkeletonData。

原地 spawn 完整播放后才显示血条、开放攻击与受击；Unity 显示节点朝左。重建步骤仍使用下述 build_rig.py 和 Unity Build 菜单，构建器会同步更新旧资源键的三个 Prefab。旧场景单次迁移菜单为 `Replace Legacy Scene Slimes`。

## 验证入口

- `verify_exports.py` 检查原骨架／动画保留、明确的皮肤网格调整和 Unity／外部导出一致性。
- `Validate Black Forest Mushrooms` 检查事件、挥拳朝向、皮肤 deform、闪白材质并渲染 8 类动作抽帧。
- Play 中 `Test Mushroom Entrance` 验证首关；`Test Legacy Slime Replacement` 在仅运行时的临时波次经旧键生成 Mob1/Mob2/Mob4，检查对应新骨架、原地 spawn、不可攻击／受伤及解锁。后者不修改磁盘关卡配置。
- 证据输出：`output/monster-animation/redesign-v4/`、`slime-replacement-validation.txt`、`original-rig-preservation.txt`。

本次验收：三套资源的骨架／七套原动作保留及 Unity 与外部导出一致性通过；三套四基础动画的事件、受击打断、闪白材质通过；已查看待机、攻击、死亡、spawn 抽帧。旧键 Mob1/Mob2/Mob4 实际生成三种新蘑菇，朝左、当前战斗血条、原地 spawn 与伤害保护通过（运行错误 0）；正常首关三波四次出场保护通过（运行错误 0）。当前存档在该轮战斗第三波战败，本次不记录通关通过。

以下为历史过程记录，若与本节冲突，以本节为准。

# 黑森林梦孢菇：原骨架复用版 v5

2026-10-09。酒红、苔绿、月紫三色改为复用外部 `Spine/monster/mon_7080/mon_7080.json`。保留原 27 根骨骼、15 个插槽、两组腿部 IK、原始加权网格与 UV、原七个动画的完整关键帧和曲线。源模型朝右；Unity Prefab 的 SkeletonAnimation 子节点 X = -1，游戏中朝左。

## 动画来源

| 游戏动画 | 来源 | 时长 | 增补事件 |
| --- | --- | --- | --- |
| Idle1 | 原 idle 的完整副本 | 1.0 秒 | 无 |
| Attack1 | 原 attack_1 的完整副本 | 0.8333 秒 | 0.3667 OnHit；0.8333 OnComplete |
| Death1 | 原 die 的完整副本 | 2.0 秒 | 2.0 OnComplete |
| Hit | 在原骨架上补做短促受击 | 0.34 秒 | 0.34 OnComplete |

战斗攻击数量按连续的精确名称 `Attack1`、`Attack2`…识别；原始小写 `attack_1` 不计入随机攻击范围，避免误播不存在的 Attack2。

原名 `idle`、`attack_1`、`die`、`run`、`spawn`、`stun`、`victory` 也保留，不插入战斗事件。战斗使用上述大写别名。原 `stun` 是持续低头摇晃的眩晕循环，不承担即时受击，因此单独补做 Hit；其基础姿态取自原 idle，未更改其他动画。

`build_rig.py` 逐项断言原骨骼、插槽、IK、默认皮肤以及七个原动作一致；来源校验值保存在 `source/original-rig/provenance.json`。

## 皮肤与图集

`source/original-rig/reference.png` 是原网格分件的坐标参考，四乘四网格包含 14 张纹理；重绘三色为同目录 `wine-red.png`、`moss-green.png`、`moon-violet.png`。使用内置 imagegen，完整提示词见同目录 `prompts.txt`。保留原分件画布与原 UV；不按透明区域裁掉边界，不修改骨骼朝向。编辑器工具按原画布裁分并重新打包为 Spine 3.8 图集。

菌帽层级沿用原 `head_back` → 头部相关插槽 → `head`，两片菌帽沿用原加权网格和绑定。阴影使用原纹理。普通与闪白材质共用图集，Straight Alpha、无 Mip Maps、无压缩。

## 输出与使用

- Unity：`Assets/DarkFairytale/Monsters/BlackForestMushroom/BF_Mushroom_{Wine,Moss,Moon}/`。
- 外部导出：项目同级 `Spine/export/monster/BF_Mushroom_{Wine,Moss,Moon}/`。
- 分件和 JSON：`source/BF_Mushroom_{Wine,Moss,Moon}/`。
- 统一测试场景：`Assets/Tests/MonsterAnimation/MonsterAnimationTest.unity`。
- 首关：Gameplay 的 `Khorasan Ruins I`，第一波酒红、第二波苔绿、第三波月紫＋酒红。保留已有资源键、GUID 和战场布局。

`1` 皮肤使用 linked mesh 继承默认皮肤，确保 spawn 的原 deform 时间线仍然生效。三者保留 `default`、`1` 皮肤以及 Enemy / Actor_Base / DropIn / 血条结构，HP 6、伤害 1、攻击间隔 2.8 秒、经验 1；数值仍为测试配置。

## 重建和验收

1. 从项目根运行 `python3 ArtDirection/Monsters/BlackForestMushroom/build_rig.py`。
2. Unity 退出 Play，执行 `Tools > Dark Fairytale > Monsters > Build Black Forest Mushrooms`。
3. 运行 `python3 ArtDirection/Monsters/BlackForestMushroom/verify_exports.py`，检查最终导出的原数据一致性和 Unity／外部导出一致性。自动检查三套四基础动画事件、逐帧有限顶点、朝向、受击打断、闪白材质；八类动作的逐帧图在 `output/monster-animation/audit-*.png`，需另行视觉检查。
4. 打开 Gameplay、Play、等待初始化，执行 `Run First Level (Play mode)`，检查首关生成、受击、攻击、死亡和通关。报告为 `output/monster-animation/mushroom-battle-validation.txt`。实战会使用当前存档并获得测试经验。

旧 8 骨骼动画不再用于构建；旧版脚本、模板与预览备份在 `output/monster-animation/before-original-rig/`。旧六分件源图仅作为历史参考。

原 `.spine` 工程和外部原始 JSON 未修改。本次使用原 3.8.75 导出数据派生 Unity 资源，未通过正式 Spine 编辑器保存新的原生工程；可编辑原生工程的往返导出验收仍待完成。不能把 Unity 运行验收等同于制作手册全项完成。

## 本次验收记录

- 最终三套 Unity / 外部导出逐项比对通过：27 骨骼、15 插槽、原 IK／网格／UV、七个原动画未修改；三个别名只增加事件。
- Unity 三套四基础动画事件测试通过；七个原动作在 default／1 皮肤下的采样顶点一致，包含 spawn 变形。
- 已查看八类动画的三色抽帧图，检查菌帽遮挡、拳头连接和倒地表现；完整动作范围已纳入预览镜头。
- 实际首关 `Khorasan Ruins I` 三波、4 个实例正常完成 Death1，触发胜利；命中与 Hit 完成事件已记录。
- 已在统一场景选择月紫版本，确认原动作列表和 Idle1 实时预览。报告见 `output/monster-animation/original-rig-preservation.txt`、`mushroom-validation.txt`、`mushroom-battle-validation.txt`。

最终实战复测：修复原名／别名重复计数造成的 Attack2 查找异常后，再次跑完首关三波，4 个实例完成死亡并通关，`runtime errors=0`。测试探针现在将运行错误计入失败条件。

## v6：缩小同屏占比、拉开交战距离

主角尺寸框由屏幕宽高 44%／28% 调整为 35%／23.5%，前排怪物由 30%／23% 调整为 24%／18.5%，实际缩放保持原宽高比。主角脚下锚点为 (23%, 30%)，前排怪物为 (68%, 31%)；后排为 (84%, 48%)、尺寸框 18%／14%。其余普通怪物位置也同步缩小并错开，Boss 专用参数保留。血条、接地阴影沿用随角色尺寸自动适配的逻辑。

配置集中在 `Assets/DarkFairytale/MoonlitCastleBattleStyle.asset`，脚本默认值同步。修改前双怪画面备份：`output/monster-animation/before-spacing-v6/wave3.png`。

## v7：后排落脚点回到道路

第三波第二只怪物原锚点 (84%, 48%) 落在右侧草坡，造成站在草丛上的错觉。将普通怪物第二站位改为 (48%, 45%)，移到道路中后部。保留原尺寸框 18%／14%，接地阴影与血条随落脚点一起移动。脚本默认值与场景风格资产同步。视觉检查需同时确认脚下路面、前排菌帽与后排血条的间距，以及小红帽武器附近的留白。

最终第三波实景截图：`output/monster-animation/layout-wave3-road-v7.png`。已核对后排脚下为石板路、后排血条与前排菌帽之间有留白、主角武器附近仍有间距。本次最终站位验证已抵达第三波并完成截图，该轮玩家随后战败，因此仅记录布局检查通过，不记录该轮通关。

## v8：原始 spawn 出场流程

`DropIn` 将入场拆为 Falling → Spawning → Ready：固定原 `spawn` 第 0 帧从上方下落，落地后以正常速度完整播放一次原动画（0.9333 秒），由 Spine TrackEntry.Complete 解锁战斗。入场结束前隐藏血条，阻止 Enemy 攻击、动画切换和命中回调；玩家与宠物选敌排除入场者，Actor_Base.Hit 统一拦截直接、范围、宠物及闪电伤害。波次存活统计仍包含入场者，避免提前切波。

原动画第 0 帧插槽全透明，Unity 在下落及动画最初 0.0667 秒恢复有附件插槽的基础透明度，使初始收缩姿态可见、落地无透明闪断；骨骼、变形、附件隐藏时间线和原始 JSON 均保留。无 spawn 的旧怪物保留原下落后直接进入战斗的流程。

Gameplay 运行后可执行 `Tools > Dark Fairytale > Monsters > Test Mushroom Entrance (Play mode)`。该测试真实运行首关三波，主动尝试攻击、普通受击、宠物和闪电伤害，验证冻结首帧可见、全动画播放、解锁选敌及波次统计。报告与三阶段实景截图保存于 `output/monster-animation/entrance/`。

## 蘑菇出场修订：原地生长（2026-10-09）

本条替代此前蘑菇“冻结首帧下落”的约定。三色蘑菇 Prefab 的 DropIn.spawnInPlace 开启，直接在最终站位播放原 spawn，从小变大后进入正常战斗。保持原动画透明度和骨骼时间线，不再应用首帧可见性补偿。动画完成前仍隐藏血条、禁止攻击与受击，并计入波次存活统计。其他怪物默认保持原下落流程；蘑菇构建器同步保存此设置，重建不会丢失。
