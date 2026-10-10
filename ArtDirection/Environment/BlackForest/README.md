# 黑森林分层与环境动画

2026-10-09。接入现有 `Gameplay` 场景和 `MoonlitCastleBattleStyle`，沿用当前小红帽、蘑菇的尺寸与站位。

## 图层

- `Background.png`：补绘后的城堡、森林、地面和无眼睛月面，BG / -100。
- 四盏提灯和月亮眼睛：独立 Spine 3.8 SkeletonAnimation，BG / -90，位于战斗角色之后。
- 角色和怪物：沿用 Character 层。
- `Foreground.png`：真正带透明通道的近景芦苇、栅栏和暗红荆棘，Character_Front / 100，遮挡角色经过的外侧部分。中央战斗区域留空。
- 前后景及氛围物体共享背景根节点的相机适配，避免不同比例、相机移动时图层错位；随背景销毁，不会积累重复实例。HUD 沿用 UI 层。

素材使用内置 imagegen 编辑/生成，完整提示词保存在 `prompts.txt`。旧的单张背景 `Assets/DarkFairytale/MoonlitCastleBattle.png` 保留供对比；当前资源位于 `Assets/DarkFairytale/Environment/BlackForest`。

## Spine 动画

### BF_Lantern / Ambient

固定支架在 root 骨骼上，灯链、灯体、柔光在 swing 骨骼上。近处两盏与远处两盏均从背景拆出，远灯使用 0.60–0.63 倍大小，挂在保留的木桩上。

最大摆幅从 ±2.2° 增强到 ±6.5°。每盏灯在创建时独立随机选择初始相位和 0.86–1.22 倍的 Spine 播放速度。光晕直径从 85 增加到 100 个源图像单位，正常透明度约 0.24–0.58。

`BlackForestLanternMotion` 在 Spine 应用关键帧后控制灯体亮度和光晕：每次闪烁独立抽取间隔（动画时间 1.8–5.5 秒）、持续时间（0.28–0.62 秒）、深浅及单次/双次闪烁。不是四盏灯重复同一套固定闪烁序列。各灯独立使用 System.Random，避免影响游戏战斗的 Unity 随机数状态。Spine 导出仍包含增强的基础摇摆/灯光循环；在 Unity 中附加此组件才获得持续随机调度。

四灯专项检查：每灯采样 32 秒 / 60 fps，确认摆角跨度超过 11°、光晕透明度跨度超过 0.25、有多次闪烁且播放速度不同。结果在 `four-lantern-validation.txt`。

### BF_WatchingMoon / Ambient

每轮 22 秒。0–8 秒闭眼，8–8.55 秒睁眼，随后向左下、右下观察地面，13 秒回到下方中央，13.45 秒闭眼休息。虹膜中心从 +11 下移至 -2 个源图像单位，横向巡视幅度从 ±20 收至 ±12。按眼白轮廓增加 Spine clipping attachment，只裁剪虹膜，保留下眼皮遮挡，避免眼球越界。

两套动画：`GazeDown` 为普通灰紫眼、`GazeDownRed` 为暗红虹膜配淡血色眼白。`Ambient` 保留为普通俯视版本，兼容旧调用。`BlackForestMoonMotion` 每轮用独立随机数各 50% 选择一种，整轮保持同一种颜色，在闭眼阶段衔接下一轮，不强制交替。组件已经接入场景及可复用月亮 Prefab。

验证：连续采样 12 轮，两套均出现（固定验证种子下普通 7 次、红眼 5 次），并保存 `moon-detail-GazeDown.png`、`moon-detail-GazeDownRed.png` 供检查向下视线与眼皮遮挡。

这两套环境动画为本次新制作的 Spine JSON 时间轴，与怪物沿用的原模型动画无关。提供 JSON、atlas、PNG、SkeletonData 和可复用 Prefab；可编辑的未打包部件与 JSON 位于本目录 `source/<ID>/`。尚不包含 Spine 编辑器保存的原生 `.spine` 工程。

运行时导出同步到项目同级 `Spine/export/environment/BF_Lantern` 和 `BF_WatchingMoon`。兼容当前 3.8 runtime，不需要升级 SDK。

## 重建与检查

- Unity 菜单：Tools → Dark Fairytale → Environment → Build Black Forest Atmosphere。
- 独立检查：Validate and Render Atmosphere。采样完整月亮周期（60 fps），检查有限顶点、实际分层和瞳孔两向位移；输出闭眼、左看、右看预览。
- 实景检查：打开现有 Gameplay 场景并进入 Play，执行 Test First Level Atmosphere。在首关三波蘑菇战斗中检查图层并截取三种眼睛状态，同时沿用战斗事件检查。
- 检查结果与实景截图位于 `output/black-forest-atmosphere/`。截图是视觉核验依据；没有以静态截图代替动画采样。

调整悬挂点和图层位置：`Assets/Scripts/BlackForestAtmosphere.cs`。
调整 Spine 动画关键帧：`Assets/Editor/BlackForestAtmosphereBuilder.cs`，修改后重新 Build。

## 本轮验收

Unity 内完整采样通过；Gameplay 实测前景遮挡、月亮完整循环、四盏提灯动画通过，运行异常为 0。首关三波、4 个怪物均完成死亡并抵达胜利。已人工查看闭眼、左右巡视及小红帽与左侧芦苇交叠截图。当前验证环境为 Unity Editor，未单独进行手机性能测试。

## 第一章连续场景参考

第一章五节的当前概念设定与衔接规则见[第一章连续场景设定](../Chapter01/README.md)。这是后续场景制作参考，区别于本页记录的现有黑森林环境实现。
