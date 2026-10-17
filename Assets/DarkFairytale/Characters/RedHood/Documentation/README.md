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
