# 邪眼诅咒书启动动画

## 编辑入口

- UGUI Prefab：`Assets/DarkFairytale/UI/StorybookIntro/CursedBookIntro.prefab`
- Spine SkeletonData：同目录 `CursedBook_SkeletonData.asset`
- Spine 源数据：本目录 `CursedBook.json` 和 `images/`，可在 Spine 3.8 中通过 Import Data 导入；这是可导入 JSON 源数据，不是原生 `.spine` 工程。
- 数据生成脚本：`build_animation.py`。只生成几何、插槽及时间轴，不修改图片。手工编辑 Spine 后请直接导出到资源目录，不要运行脚本覆盖手工调整。
- 正面封皮及纸页由内置 imagegen 制作；提示词见 `texture-prompts.txt`，原批准封面仍保留在上一级目录。

## 播放与结构

`Open` 为 4.6 秒非循环 Spine 3.8 动画：封面停留约 1 秒，封皮沿书脊转开，八张独立网格书页密集接续翻动，最后停在打开状态。22 个骨骼、20 个网格层，包含书脊、后封板、三层书芯、前封板、内封纸和八张翻页。纹章是压纹图案，无真实眼球。

开场通过 `SkeletonGraphic` 播放，不是视频或运行时拼建的界面。书、背景、加载文字和版本号都是 Prefab 中的持久节点。版本号为真实 Text，并读取 `Application.version`。

`CursedBookIntro` 提前加载真实主界面，并分段播放显影和推进：

- 0–1 秒展示闭合封面。
- 1–4.6 秒播放翻开与八张翻页，镜头缓慢推向右页；以参考截图为准，停在主界面画幅约占屏幕高度 65% 的位置，周围保留纸页与封皮。
- 4.6–6.4 秒在上述中间构图保持镜头静止，小红帽和庇护所从纸面洇开至完全显现（操作 UI 隐藏）。
- 6.4–6.7 秒保持完整画面，随后于 6.7–8.9 秒以右页为中心推近至全屏。
- 8.9–9.8 秒淡入操作 UI；最终完整画面至少渲染一帧后，将显示和输入交给原主界面，无黑场淡出。加载较慢时停留在打开的书，等待真实界面就绪。

庇护所场景与延后显现的操作层通过持久化 `HomeCaptureCamera` 实时渲染到固定 3:4 的 RenderTexture，并显示在持久化 `LiveHomePage` RawImage 中；不是固定截图，界面动画继续运行。临时调整原 Canvas 的渲染模式、图层和射线检测，交接或中断时恢复并释放纹理。沿用原 `IntroScript` 完成回调，保留教程和每日奖励流程。启动期间屏蔽操作，交接后恢复原按钮。

所有时间使用独立于 Time.timeScale 的动画时钟，限制单帧步长避免首次加载卡顿跳过封面；9.8 秒为正常帧率下的动画时长，额外加载等待不计入。镜头推进采用 UGUI 节点变换，参数及节点均可在 Inspector 调整。

以居中的 1080×1920 核心区域均匀缩放到安全区，适配 9:16、3:4、1:2，不横向拉伸美术。

## 生成与验证

编辑器菜单 `Tools > Dark Fairytale > Storybook > Install Opening Prefab (Once)` 为显式安装入口。已有 Prefab 不重建、不覆盖手工布局；播放或普通导入不自动执行安装。Gameplay 中旧 `Intro (KEEP ON)` 完整保留但停用，不覆盖原 Logo 动画资源。

已安装旧版启动 Prefab 时，使用菜单 `Tools > Dark Fairytale > Storybook > Upgrade to Live Page Entry (Once)` 显式增加推进节点、内页画面和预渲染相机；已升级的 Prefab 不重复覆盖。

本地验证命令：在退出 Play Mode 后创建 `output/storybook-intro/entry-validate.request`。检查三种比例的自然启动、延迟就绪等待、最终页边界与屏幕对齐、交接前后实际截图差异、Canvas 和输入恢复、纹理释放、战斗进入与返回。测试期间复用存档写入保护。

实际 Unity 截图位于已忽略的 `output/storybook-intro/page-entry/`，不提交 Git。该目录的 `book-entry-preview.mp4` 是实际 Unity 渲染帧合成的 10 fps 预览；游戏本身不限制为 10 fps。文本验收记录见 `VALIDATION.txt`。

## 视觉边界

批准图是静态三分之四视角概念，本实现为其正面封皮动画适配；保留图形身份，但不是概念图的逐像素复刻。纸页是新增的空白辅助材质；右页承载现有小红帽和庇护所场景；操作层仅在进入全屏后显示，未另行定义新的功能页美术。

## 洇墨显现调整（2026-10-12）

按用户要求，在原 4.6 秒翻书段内增加到八张连续翻页，不缩短整体演出。右页使用 `HiddenInk.mat` / `HiddenInk.shader`，从多处起点扩散出不规则湿润边缘，暗色细节先显现，再发展成原主界面完整色彩；最终输出原始画面，保持无缝交接。材质参数可调湿边柔度与显影墨色。运行时只实例化材质控制进度，不生成界面节点。

显式菜单 `Tools > Dark Fairytale > Storybook > Install Hidden Ink Reveal` 只为既有 LiveHomePage 绑定材质，不重建布局。

节奏调整：显影与最后一段推进严格分离，`revealDuration`、`revealedHoldDuration` 和 `enterPageDuration` 可分别调整；Spine 书体取消自身平移与缩放，封皮和纸页向左翻动。镜头在翻页时推进到中间构图，显影期间静止，最终以右页为中心继续推近。`pageHoldViewportFraction` 控制中间停留位置，当前为 0.65。

## 硬封皮质感优化

封板使用保持平面的透视投影，增加 12 个骨架单位厚度的板芯侧面、上沿和下沿，沿用已批准封皮纹理，不另换美术。独立内衬保留皮革包边；窄书脊皮革轻微牵动，纸面接触阴影随抬起收窄消散。封皮动画在原 1–2.65 秒内细分为起始阻力、加速翻开、减速落定及微小回落。八张纸页、4.6 秒翻书段、洇墨效果及两段镜头推进保持不变。

## 场景入书与操作界面分离（最新流程）

书页仅显现现有小红帽与庇护所，使用固定 3:4 的实时画面填入纸页，不再把屏幕纵横比赋给插画。持久化 `ShelterResponsiveLayout.presentationControls` CanvasGroup 控制原有操作层；翻书、洇墨与推进期间隐藏操作层。推进时锁定场景自身缩放与位置，仅移动书本镜头；不同屏幕通过裁切观看同一构图，不拉伸美术；进入全屏后再用 0.9 秒淡入原操作层，然后恢复点击。当前完整演出约 9.8 秒，不包含额外加载等待。

场景与原主界面共享实际角色、壁炉动画及存档数据。临时构图使用 Canvas 设计单位，操作层退出时恢复常规响应式布局；背景保留与书页推进终点相同的归一化取景，避免交接时二次缩放。原八张翻页、硬封皮厚度与动作、洇墨显现及中途停留保留。编辑器显式请求 `output/storybook-intro/scene-only.request` 为既有 Core 节点保存 CanvasGroup 绑定，不在运行时创建 UI。

## 单一镜头与纸面融合

移除推进时的 `introCompositionSize` 插值。场景在固定书页构图完成后锁定，进入主界面及战斗返回后仍沿用该取景，操作 UI 独立响应屏幕。`HiddenInk` 的 `_PaperEdge` 使用多尺度静态噪声和渐薄透明度，让纸纹从不规则颜料边缘透出；边缘略带旧纸暖色。进入场景过程中逐步退出纸面融合，最终输出完整场景，避免全屏残留白边。
