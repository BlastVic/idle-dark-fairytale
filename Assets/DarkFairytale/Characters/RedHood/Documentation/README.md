# 小红帽战斗角色：唯一正式资源目录

所有路径相对于 `Assets/DarkFairytale/Characters/RedHood/`。

## 目录职责

- `Spine/`：唯一正式 `RedHoodBattle.json`、SkeletonData、两页图集、贴图、普通与受击闪白材质。
- `Prefabs/Player.prefab`：正式角色及游戏逻辑绑定，保留原 GUID。
- `Tests/RedHoodAnimationTest.unity`：动作预览场景；直接引用正式 SkeletonData，含 15 个动作，无第二套试作动画数据。
- `Tests/RedHoodAnimationPreview.cs`：预览播放控制。
- `Editor/Tools/`：Unity 显式制作、预览和验证菜单。
- `Editor/Authoring/`：显式运行的 Python 动作制作工具，只更新唯一正式 JSON。不会随导入或游戏运行自动执行。
- `Documentation/`：历史制作记录。旧路径、动作数量和双份同步说明仅描述历史状态，以本 README 为准。

高分辨率原稿、分件、表情和武器视角参考统一在 `Assets/ArtSources/Characters/RedHood/`，不被正式运行资源引用。营地坐姿的小红帽属于独立 UI 演出，继续由 `UI/Shelter` 管理，本目录是战斗角色。

## 使用与维护

正式场景、怪物测试、特效测试、动画预览共用 `Spine/RedHoodBattle_SkeletonData.asset`，不再复制 JSON。装备仅影响数值和 UI，不给角色切换旧骑士服装。

`Tools > Red Hood > Rebuild Test Scene` 仅显式重建预览场景，读取正式骨架，不改贴图导入设置、不生成重复 Prefab、不覆盖正式动画。

`Tools > Dark Fairytale > Validate Player` 检查正式 Player、动画事件、复活回调和材质映射。测试截图写入忽略的 `output/`。

2026-10-17 整理：删除重复的 RedHood.json、RedHood_SkeletonData.asset 和无人引用的 RedHood.prefab；移除 Assets/RedHoodPrototype 与 ArtDirection/Characters/RedHood。正式动画与美术内容保持原样，运行资源移动时保留 .meta GUID。

## 角色动画测试界面（2026-10-17）

角色测试场景使用 `Tests/UI/Prefabs/RedHoodPreviewHUD.prefab`，全部文本、按钮、滑杆、列表节点持久化并可在 Inspector 调整。参考怪物测试的深色面板、纸色文字、暗红选择态、森林背景与居中 9:16 核心布局。仍引用唯一正式 SkeletonData，测试不加载玩家存档或战斗业务。

- 顶部：重置视角、全部动画列表、按英文动画名搜索，包含全部 15 个现有动画。
- 底部：暂停/继续、重播、时间轴、前后 1/60 秒逐帧、循环、轮播、Attack1/Attack2 连击、结束接回 Idle、速度与缩放。
- 胜利自然接 VictoryIdle；死亡末姿保持；失败播放倍率与正式战斗一样为 1.2×，滑杆倍率在此基础上作用。时间轴显示骨架动画时间。
- 默认预览取景覆盖全部动作最大范围，举刀与下坐不会引发镜头自动缩放；缩放仅为调试视图，不作为正式战斗尺寸标定。
- `Tools > Red Hood > Install Editable Preview UI` 仅显式安装缺失界面、测量动作范围，不覆盖已有布局。原 Rebuild Test Scene 菜单也复用已保存 HUD。
- 本地验证入口为 `output/red-hood-preview-ui/validate.request`；截图和检查结果留在该忽略目录。

验证结果：2026-10-17 Unity 实际 Play 检查全部 15 个动画、时间轴、逐帧、暂停恢复、事件、循环/轮播/连击、失败倍率与回到 Idle；720×1280、768×1024、720×1440 的展开、收起、空搜索和禁用状态均保留本地截图。全部通过。

## 2026-10-17 死亡动作调整

Death1 从 Idle1 直接连续下沉至既有脚跟坐姿，取消胜利式收腿站直阶段。动画资源 1.30 秒，1.15 秒落稳；战斗保留 1.2 倍播放速度，约 0.96 秒落稳、1.08 秒完成。镰刀和提灯在资源时间 0.10–0.35 秒渐隐。Revive 按新版 Death1 完整倒放，保留相同 1.30 秒资源时长与 1.2 倍战斗/预览速度；起身末段镰刀、提灯渐显并睁眼，末帧恢复腿部 IK 后衔接 Idle。

实际 Unity 60 Hz 渲染记录：`output/red-hood-motion/death/validation.txt`；复活接缝检查：`output/red-hood-motion/revive/validation.txt`。截图仅保留本地。
