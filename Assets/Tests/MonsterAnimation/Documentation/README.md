# 统一怪物动画测试

制作与战斗接入要求参见[怪物制作手册](../../../../ArtDirection/monster-production-manual.md)。

打开 `Assets/Tests/MonsterAnimation/Scenes/MonsterAnimationTest.unity`，点击 Play。
也可使用菜单 `Tools > Dark Fairytale > Monsters > Open Test Scene`（Ctrl+Shift+F11）。

- 搜索并点击怪物名称，切换预览对象；动画列表自动读取当前骨骼，优先播放 idle。
- 支持暂停、循环、全部动画轮播、重播、速度和缩放调节，以及 Fit camera 重新取景。
- 切换怪物时重置动画状态并自动取景；没有动画时显示初始姿态，资源加载失败时显示错误，仍可选择其他怪物。
- 场景独立于游戏主场景、战斗及存档，未加入 Build Settings。

## 新增怪物

1. 将与本项目 Spine 3.8 Runtime 兼容的 JSON（或 skel.bytes）、atlas.txt 和 PNG 导入 `Assets/Tests/Monsters/<怪物名>/`。
2. 等待 Spine 导入器生成 SkeletonDataAsset，确认材质与贴图的透明度设置匹配。测试器不会转换源文件或修改版本标记。
3. 进入统一场景并重新点击 Play：默认扫描 `Assets/Tests` 下全部 SkeletonDataAsset，自动加入列表。

其他目录的资源：在 Animation Controls 的 MonsterAnimationTest 组件上调整 Discovery Folders，或将 SkeletonDataAsset 拖入 Monsters 数组。
自动扫描仅在 Unity 编辑器可用。要保存列表用于独立构建，退出 Play，执行 `Tools > Dark Fairytale > Monsters > Refresh Monster List` 并保存场景。
移除资源时同时移除 Monsters 数组中的手动引用；已删除的空引用会在刷新时跳过。

## 验证

退出 Play 并打开统一场景，执行 `Tools > Dark Fairytale > Monsters > Validate Imported Monsters`（Ctrl+Shift+F10）。
按 60 Hz 采样列表中每个怪物的全部动画，检查能否加载以及顶点是否为有限数；某个资源失败后继续检查其余资源。
报告：`output/monster-animation/validation.txt`。此检查不代替视觉验收。

mon_7070 原有 Art 资源保持原路径；旧场景和控制脚本已迁移到此目录并保留 Unity GUID。

## 黑森林三色蘑菇

已将 `Assets/DarkFairytale/Monsters/BlackForestMushroom` 加入统一场景的扫描目录。
选择 `MobTallowkin`、`MobGnarlo`、`MobDrowseed` 对应的 SkeletonDataAsset 即可预览朝左的四个基础动画；优先播放 `Idle1`。
完整交付、实战入口、重建步骤和源工程限制见[三色蘑菇说明](../../../ArtDirection/Monsters/BlackForestMushroom/README.md)。

## MobHexmaw 红眼狼

已加入 `Assets/DarkFairytale/Monsters/MobHexmaw` 和对应 Prefab。Play 后搜索 `MobHexmaw` 并选择 `MobHexmaw_SkeletonData`，可测试原始十套动作及 `Idle1`、`Attack1`、`Hit`、`Death1` 四个游戏别名。骨架沿用 C027_S01D，预览方向与游戏 Prefab 一致。

实际战斗验证：在 Gameplay 的 Play 模式执行 `Tools > Dark Fairytale > Monsters > MobHexmaw > Run Combat Test (Play mode)`，使用临时单狼和混编波次，不改正式关卡。资源和事件记录见 [MobHexmaw 交付说明](../../../ArtDirection/Monsters/C027_S01D-Wolf/DELIVERY.txt)。

## UGUI 预览工作台（2026-10-11）

已用可编辑 UGUI 替换全部 OnGUI 控件，场景中保存当前黑森林战斗背景、氛围节点与小红帽 Idle 参照模型。测试场景不会加载战斗、存档或关卡流程。

- 顶部“小红帽”开关控制显示／隐藏；小红帽只循环 Idle1，不受怪物暂停、速度、轮播影响。
- 点击当前怪物栏展开／收起怪物库，默认折叠。按名称或编号搜索（英文忽略大小写）；每张卡片包含真实模型缩略图和名称，向下滚动可查看全部怪物。选择后自动折叠。
- 动画区优先显示 Idle1、Attack1、Hit、Death1，原始动画全部保留并支持滚动。暂停／继续、重播、循环和自动轮播沿用原功能。
- 速度范围 0.1–2 倍；缩放范围 0.3–2 倍，仅缩放怪物预览，小红帽保持参照大小。重置视角将当前姿态重新适配预览区。手动放大到极限可能超出预览区，重置即可恢复。
- 720×1280 为设计核心，在 9:16、3:4、1:2 屏幕中等比居中并兼顾安全区；背景等比覆盖，角色按独立预览区域摆放。

### 手动编辑

- `UI/MonsterPreviewHUD.prefab`：所有标题、搜索框、按钮、开关、滑条、滚动列表、角色预览区域均为持久化节点，可在 Prefab Mode / Inspector 调整。
- `UI/MonsterCard.prefab`、`UI/AnimationButton.prefab`：列表项模板。现有怪物卡片和动画按钮实例也已保存于 HUD，运行时只绑定数据与事件；新增项复用模板，不用代码绘制控件。
- `Core_9x16/RedHoodPreviewArea` 和 `MonsterPreviewArea` 决定角色适配位置、大小与落脚线；UI 的 `BattleHudSafeArea` 只负责核心容器适配。
- 场景的 `Preview Stage` 保存背景、小红帽、月亮与灯笼；`Animation Controls` 保留怪物资产和原 Prefab 引用，用于复制预览朝向。原版怪物和正式 Gameplay 未改。
- `UI/Portraits/` 为界面实际引用的缩略图素材，需要随 UI 保留；游戏验收截图仍只存本地 `output/`。

### 新增资源和工具

编辑器菜单 `Tools > Dark Fairytale > Monsters > Refresh Monster List` 会追加新怪物卡片与缩略图并保留已有名称、图片和手工布局；之后保存场景。Play 时自动发现的新怪物可立即测试，正式保留缩略图时执行该菜单。运行中缺失的卡片使用模板占位，既有卡片不重建。

`Install Preview UGUI (Once)` 只创建缺失的初版 Prefab／场景引用，重复执行不会覆盖已有 UI。不会在导入或启动时自动重建。

`Validate Preview UGUI` 在 Play 模式验证控件绑定、搜索与无结果反馈、怪物切换与全部动作、暂停／重播／循环／轮播、独立 Idle、角色边界、真实运行帧射线及三种比例，并输出实际 Game View 截图。使用该工具前先退出 Play。

本地自动化也可显式创建 `output/monster-preview-ui/install.request` 或 `validate.request` 调用对应菜单操作；没有请求文件时不执行。测试结束退出 Play 并恢复原 Game View 分辨率。输出：`output/monster-preview-ui/validation.txt` 与 `collapsed-*.png` / `expanded-*.png`。

## MobGloamweb 验收（2026-10-11）

后续怪物运行测试默认直接使用本场景。MobGloamweb 的目录、独立 Prefab 和缩略图已加入；预览现在采用 Prefab 的初始皮肤与朝向。尺寸规则已统一为战斗配置，不再按四个动作的包围盒缩放；可按需要使用整体缩放查看大幅位移动作。

- `Tools > Dark Fairytale > Monsters > Validate MobGloamweb`：原骨架/网格/动画比对、14 动作 × 2 皮肤、事件次数、攻击被受击/死亡打断、配置引用和关键帧渲染。
- `Validate MobGloamweb In Test Scene`：从本场景自动进入 Play，验证运行时数据库/Addressables 加载、目录和按钮交互、三种竖屏比例、数字皮肤与四动作关键帧；结束后退出 Play 并恢复分辨率。
- `Prepare MobGloamweb Acceptance`：显式修复 MobGloamweb 的 Alpha 配套设置、独立闪白材质和朝向，并追加目录/刷新该怪物缩略图；不自动执行，不覆盖既有 UI 布局。

报告与真实 Unity 截图仅输出至 `output/monster-animation/MobGloamweb/`。本场景是预览和资源验证环境，事件触发检查不能代替伤害结算、掉落和波次推进的完整战斗验证。验收结论见 `ArtDirection/Monsters/C032_S01B/ACCEPTANCE.txt`。

## MobRivenfang 霜牙狼王

源 C030_S01B，资源目录 `Assets/DarkFairytale/Monsters/MobRivenfang`，独立键 `MobRivenfang`。已加入预览目录、Prefab 和实际缩略图；保留16原动作，加4个游戏别名。选择 MobRivenfang 后 Idle 可见血刃滴血，暂停同步冻结，攻击停止发射，死亡清理。组件参数在 Prefab 的 `MonsterIdleBlood` 中编辑。

此前的 `fitSelectedAnimation` 按动作取景已在 2026-10-12 移除，现统一采用下文的战斗尺寸模式。

菜单 `MobRivenfang > Validate and Render` 检查全动作/两皮肤与事件；`MonsterPreviewValidation.BeginFor("MobRivenfang")` 在统一场景检查运行时与三比例。实战探针使用临时波次，开启仅编辑器有效的禁止存档标记，完成后退出Play并返回统一场景。交付与源工程限制见 `ArtDirection/Monsters/C030_S01B/DELIVERY.txt`。

## MobVesperling 枯叶菇

源 C031_Dummy_G，已选 A 方案。测试场景怪物库搜索 `MobVesperling` 或 `枯叶菇`。
保留 14 段原动作，新增 Idle1、Attack1、Hit、Death1；单数字皮肤 1。
采用统一战斗尺寸，原跳跃攻击的移动范围保持；可使用缩放同时调整两位角色的预览大小。

显式制作入口：先运行 `python3 ArtDirection/Monsters/C031_Dummy_G/build_rig.py`，再使用菜单 `Tools > Dark Fairytale > Monsters > MobVesperling > Build and Register`。已有 Prefab 保留 Inspector 手调。
运行中的 Unity 可接受 `output/monster-animation/MobVesperling/command.request` 单次命令：`build`、`validate`、`preview`、`combat`、`setup`、`play`、`stop`；更新脚本后先等待编译完成，再写命令，读取新的 command-result.txt。命令只在显式请求时执行。
`preview` 从编辑模式运行实际三比例与 UGUI 检查。`combat` 从编辑模式临时进入 Gameplay，使用隔离单怪/混编波次并禁用存档写入，结束返回 MonsterAnimationTest。命令受理不代表测试完成，必须检查最终验证报告。
报告与截图：`output/monster-animation/MobVesperling/`。制作资料：`ArtDirection/Monsters/C031_Dummy_G/source/`。

## MobBramblet 刺帽菇

源 C031_S01R，选定 A 方案，独立键 `MobBramblet`。怪物库搜索编号或中文名即可选择。
保留 11 段原动作，加四游戏别名，共15段；数字皮肤1。现采用统一战斗尺寸。
构建：`python3 ArtDirection/Monsters/C031_S01R/build_rig.py`，再执行 Unity 菜单 `MobBramblet > Build and Register`。
显式队列 `output/monster-animation/MobBramblet/command.request` 支持 `build`、`validate`、`preview`、`combat`、`setup`、`play`、`stop`。更新脚本后等待编译完成再写请求。
三比例与真实临时单怪/混编波次已验收；详情及原生工程待补项见 [交付记录](../../../ArtDirection/Monsters/C031_S01R/DELIVERY.txt)。

## MobSplinterjaw 疯松鼠

源 `mon_7040`，已选 B 方案。独立资源键 `MobSplinterjaw`；怪物库可按编号或中文名搜索。
原七段动作保留，添加 Idle1/Attack1/Death1 别名，并在原骨架补作0.4秒Hit，共11段；数字皮肤1。现采用统一战斗尺寸。
构建：`python3 ArtDirection/Monsters/mon_7040/build_rig.py`，然后 Unity 菜单 `MobSplinterjaw > Build and Register`。
显式请求 `output/monster-animation/MobSplinterjaw/command.request` 支持 `build`、`validate`、`preview`、`combat`、`setup`、`play`、`stop`。等待脚本编译完再写请求。
三比例与临时单怪/混编实战已验收。原生新皮肤工程待补，详见[交付记录](../../../ArtDirection/Monsters/mon_7040/DELIVERY.txt)。

## 怪物名称迁移（2026-10-12）

九只新怪物现统一使用 `Mob+英文名称`，资源键、Prefab、数据库、Addressables、制作工具和测试目录已同步。对照表与验收见 `ArtDirection/Monsters/name-migration-2026-10-12.json` 和同名 `.txt`。旧验收文件中的数字编号仅为历史记录。

退出 Play 并等待编译完成后，在 `output/monster-name-migration/command.request` 写入 `validate`，可逐只检查新地址/数据库加载、目录选择、名称搜索及三比例截图；此检查不代替完整战斗验收。

## 统一战斗尺寸（2026-10-12）

预览使用 Gameplay 相同的 Player Prefab 和 MoonlitCastleBattleStyle，以 setup pose 的尺寸调用共用 DisplayFitScale 计算角色大小；默认对应前排普通怪（Preview Stage 的 Battle Enemy Slot=0）。两个角色再一起等比映射到预览区，因此相对体量与同宽高比的战斗一致。Inspector 中可切换其他普通怪站位作为参照。

Idle1、Attack1、Hit、Death1 和扩展动画共享同一个角色缩放及固定待机落脚锚点；切换、重播、轮播、暂停或重置不会按动作包围盒重新缩放或居中。动作本身的骨骼伸缩、跳跃和死亡位移保持不变；大幅动作不会被强行缩小塞回框内。缩放滑条同时缩放小红帽和怪物，重置回 1×。

编辑模式、编译结束后，在 `output/monster-preview-scale/command.request` 写入 `validate`，自动检查九只怪物全部动画的缩放/锚点不变、实际战斗 Fit 结果、共同缩放与重置，并保存三比例 Idle/Attack 实景截图。

## MobMurmlet 水史莱姆

源 mon_7320，采用 B v3：水边软体、无顶珠、虚化闭眼月。独立键 `MobMurmlet`，目录/Prefab/缩略图已加入。7 段原动作保留，新增三个游戏别名和短 Hit，共11段。
构建与验收见 `ArtDirection/Monsters/mon_7320/DELIVERY.txt`。单怪三比例可在编辑模式向 `output/monster-preview-scale/command.request` 写入 `validate MobMurmlet`；`validate` 无参数仍检查完整列表（现10只）。
实战队列为 `output/monster-animation/MobMurmlet/command.request` 写入 `combat`，等开书过渡结束后在临时溪畔小屋单怪/混编波次验证，结束返回本场景。新原生 .spine 工程仍待补。

## MobStillmere 史莱姆之王

源 `boss_8020`（素材目录 `mon_8020`），选定 A，独立键 `MobStillmere`。在怪物库搜索英文名或“史莱姆之王”。小 BOSS 使用现有可编辑 `CursedStorybookEnemyBar`，保留 10 段原动作及王冠脱落，增加 Idle1、Attack1、Hit、Death1。

测试器现在读取所选 Prefab 的 `EnemyLifebar.isMiniBoss/isBoss`，与真实战斗共用 `EnemyDisplaySize`；普通怪尺寸规则不变，动作间仍使用固定缩放。720×1280 同前排基准测量：可见总高为 MobMurmlet 的约 2.03 倍，身体高约 1.44 倍、宽约 1.43 倍。透明顶珠附件不计入可见尺寸比较，但骨架保持完整。

制作入口：`python3 ArtDirection/Monsters/boss_8020/build_rig.py`，然后 Unity 菜单 `MobStillmere > Build and Register`。请求目录为 `output/monster-animation/MobStillmere/`，`command.request` 支持 `references/build/configure/validate/preview/combat/setup/play/stop`；等待编译和命令完成，再发下一条。`configure` 只补缺失的小 BOSS 血条引用。

单怪三比例：编辑模式写入 `validate MobStillmere` 至 `output/monster-preview-scale/command.request`。`combat` 使用临时溪畔单王及王+普通水团波次，禁用存档，结束返回本场景。新原生 .spine 待补；运行资源与源分件齐备，详见 `ArtDirection/Monsters/boss_8020/DELIVERY.txt`。

## PetCindrel 书脊龙 · 伙伴

C058_S01R 方案 C，用于 Cindrel 技能。独立资源在 `Assets/DarkFairytale/Companions/PetCindrel`，通过 General DB 的 PetCindrel 键切换，原飞龙 Prefab 保留。怪物库可搜索英文名或“书脊龙”。

保留 15 原动作，增加 9 个游戏别名；Attack1/2/3 沿用原生攻击，事件驱动原技能伤害，死亡时转为地面奔跑退场。测试器读取伙伴 Prefab 的 viewportSize，所有动作固定比例。

编辑模式向 `output/monster-animation/PetCindrel/command.request` 写入 `preview` 或 `combat` 运行验收；后者使用 Gameplay 临时两波敌人，隔离属性并禁止存档，结束自动回到 MonsterAnimationTest。截图只保留在 output。构建、限制与结果见 `ArtDirection/Monsters/C058_S01R/BUILD.txt` 和 `DELIVERY.txt`。

### 书脊龙攻击特效

选择书脊龙后，Attack1显示余烬火球，Attack2显示爪痕和烟尘，Attack3显示火流与扩散焰团；原始源动作不带游戏触发。预览时宠物与小红帽共用左侧，右侧保留特效空间，预览不造成伤害。循环播放会重复触发，换怪物会清理效果。正式战斗由原OnHit事件处理一次伤害。

独立资源：`Assets/DarkFairytale/Companions/PetCindrel/Effects`。显式制作命令`effects-build`，实战回归`effects`，文档`ArtDirection/Monsters/C058_S01R/PetEffects/BUILD.txt`。

## 出场测试（2026-10-16）

Play 后选择怪物，点击动画区右上角的 **测试出场**。预览读取对应敌人 Prefab 的 `DropIn` 配置，复用战斗中的出场状态机：原地 spawn、下落后 spawn、无 spawn 时直接落地，或从右侧屏幕外跑步入场。没有对应 DropIn 配置的资源，按钮整体灰化并禁用。

- 状态栏显示下落中、跑步进场、出场动画、完成／待机；完成后停留在循环待机。
- 暂停／继续和 0.1–2× 速度同时控制下落与骨骼动画；出场模式下“重播”重放完整出场，也可再次点击“测试出场”。暂停时重播保持暂停。
- 选择普通动画、切换循环／轮播或切换怪物会退出出场模式并清理临时状态。开始出场测试会关闭自动轮播。
- 出场参数在原怪物 Prefab 的 DropIn 上调整；测试本身不修改资源，不启动 Enemy、伤害、波次或存档流程，也不模拟血条与攻击解锁。
- 按钮持久保存在 `UI/MonsterPreviewHUD.prefab`，可直接调整布局和样式。菜单 `Add Entrance Test Button` 仅在缺失时增量添加，不覆盖已有按钮。
- 编辑模式菜单 `Tools > Dark Fairytale > Monsters > Validate Entrance Preview` 可验证三种比例、暂停、完成、重播和中途切换；本地报告及实际 Unity 截图位于 `output/monster-entrance/`。也可向该目录 `command.request` 写入 `install` 或 `validate` 显式执行；不得在 Play 时执行 install。

## 中英文独立命名（2026-10-16）

当前 11 个怪物使用 `Mob` 前缀，伙伴使用 `PetCindrel`。英文资源键迁移历史见 `ArtDirection/Monsters/name-migration-2026-10-16.json`；最新简化中文名见 `ArtDirection/Monsters/display-names-2026-10-16.txt`，运行时代码统一由 `CreatureNames` 提供显示名。测试卡片、选中标题只显示中文，搜索同时支持中文与英文资源键。原版参照模型保持独立。

`output/creature-display-names-2026-10-16/command.request` 支持显式命令 `author`、`preview`、`combat`、`story`：
- `author` 增量更新已保存的中文卡片与 Boss 名称，保留布局和引用。
- `preview` 验证 12 个数据库/Addressables/Prefab/Spine/图集引用、所有动作、中英文搜索、怪物出场及三比例真实渲染；包含宠物旧存档技能等级兼容测试。
- `combat` 在 Gameplay 使用临时 11 个单怪波次及 1 个混编波次，验证真实攻击伤害、书脊龙伤害、受击、死亡、经验、胜利及返回清理；禁用存档写入，结束回到测试场景。临时高血量只用于集成验收。

- `story` 打开 Gameplay，运行章末剧情验收，报告及截图位于 `output/chapter01-ending/`。

历史验收报告、旧迁移记录与美术提示词保留当时名称，用于追溯；当前脚本、配置和资源使用新键。普通怪的空 `bossName` 保留，避免改名引入原本仅用于 Boss 的死亡震动。

## 狼类跑步入场（2026-10-16）

红眼狼 `MobHexmaw` 和霜牙狼王 `MobRivenfang` 的 DropIn 启用 `runFromRight`，使用原生 `Run_Battle_00` 循环跑步动画。起点根据当前相机右边界与角色可见包围盒计算，默认 `runDuration=0.6` 秒，沿地面跑至原站位；阴影跟随，入场时血条隐藏、攻击和受伤保持锁定，到位后恢复正常战斗。

在各自 Prefab Inspector 的 DropIn 可调整跑入时长。其他怪物仍使用原出场方式。测试场景的“测试出场”、暂停、倍速、重播均复用相同逻辑。`CreatureNameMigrationValidation` 的 `preview` 和 `combat` 已包含跑步出场断言，跑步阶段截图保存在 `output/wolf-run-entrance/`。

## 当前出场方式补充

- 水史莱姆 `MobMurmlet`、史莱姆之王 `MobStillmere`、疯松鼠 `MobSplinterjaw`：`spawnInPlace=true`，在最终站位播放原生 `spawn`，不加下落。
- 枯叶菇 `MobVesperling`、刺帽菇 `MobBramblet`：与红眼狼、霜牙狼王一样，`runFromRight=true`，播放 `Run_Battle_00`，从屏幕右侧外跑入，时长均为 0.6 秒。
- 花背蛛 `MobGloamweb`：保留从天而降。
- 原地 spawn 和跑入仍共用入场期间的攻击/受伤锁定及血条隐藏，到位或 spawn 播放完成后恢复战斗。

本次原地 spawn 截图与验收记录在 `output/monster-entrance-rules/`；跑步阶段截图仍在 `output/wolf-run-entrance/`。
