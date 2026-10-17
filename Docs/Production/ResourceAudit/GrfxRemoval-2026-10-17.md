# Grfx 目录退役

装备图标已在上一轮迁移至 `Assets/DarkFairytale/UI/Equipment/Icons`。本轮对 Grfx 剩余 10 个文件逐项审计，移除整个目录与目录元数据。

| 资源 | 实际引用 / 处理 |
| --- | --- |
| Particles/Light.png、Lit.mat、Light.mat | 仅用于 Gameplay 中已停用的旧 Intro 下两层 Image。删除两个叶节点及材质、贴图。 |
| PPP3.asset | 旧开场/战斗相机与 MapPackageAuto 的 PostProcessVolume；地图 Volume 已停用，新版战斗关闭旧 PostProcessLayer。删除三个旧 Volume 节点与配置。 |
| Cam/ChromaticAberration.cs、RGBSLPIT.shader | 旧开场、战斗相机和地图相机组件，地图组件已禁用，新版战斗关闭此效果。删除三个组件及脚本/Shader。 |
| Cam/FollowSceneView.cs | 仅在地图 Prefab 上禁用，旧编辑器相机跟随工具；删除组件及脚本。 |
| Menus/Bar.png | 无资源引用，删除。 |
| Menus/Crystal/Crystal.blend、Crystal.mat | 无资源引用，删除。 |

连带删除仅被旧 Light.mat 引用的 `Assets/Standard Assets/Effects/Projectors/Textures/Light.psd`。删除已完成使命的 `Assets/Editor/GrfxResourceCleanup.cs`；`DarkFairytaleBackdrop` 去除已不存在的 ChromaticAberration 类型分支，保留原有 Bloom/PostProcessLayer 处理。新版开场仍使用 IntroScript 的完成回调，因此保留该业务脚本。

修改正式场景 `Assets/Scenes/Gameplay.unity` 与 `Assets/Prefab/Addressable Prefabs/MapPackageAuto.prefab`：同步移除组件列表、对象块和父节点的子项引用，不留下空脚本或指向已删除对象的局部引用。

验证与证据：

- Assets、ProjectSettings、Tools 中无对已删除 Grfx GUID 的引用；运行与制作代码无 Grfx 路径、FollowSceneView 或已删除的自定义 ChromaticAberration 分支。
- Gameplay 和 MapPackageAuto 所有序列化脚本 GUID 均能在 Assets/Packages 中解析。
- Unity 刷新、脚本编译及 Play Mode 启动完成。
- 营地三比例、Spine 动画、章节切换、进入战斗与返回营地复测通过：`output/grfx-final/after-camp-validation.txt`。第一次清理后营地测试曾出现动画计数及流程失败，保留在 `first-after-camp.txt`；随后独立重跑未复现，最终各项通过。
- 本轮存档 savedata 键前后哈希一致。测试使用禁止保存开关。
- 删除前资源、场景和 Prefab 备份在 `output/grfx-final/before.zip`；前后截图仅保留在本地 output 目录。

本轮不包含 APK 构建，不用源文件删除体积推算 APK 减少量。

装备界面完整回归（含换装、锁定、分解、筛选以及地图/技能导航）通过：`output/grfx-final/equipment-validation.txt`，`PASS COMPLETE failures=0 errors=0`；最终存档哈希仍一致。
