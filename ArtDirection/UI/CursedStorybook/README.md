# B3「诅咒童话」战斗 UI

已选视觉参考保存在本目录 `approved-concept.png`，后续 UI 扩展以此保持风格一致。

## 可编辑交付

- 主界面：`Assets/DarkFairytale/UI/CursedStorybook/CursedStorybookBattleHUD.prefab`。
- 敌人血条：`Assets/DarkFairytale/UI/CursedStorybook/CursedStorybookEnemyBar.prefab`。
- 场景：`Assets/Scenes/MainScenes/Gameplay.unity` 中的主 HUD Prefab 实例，通过 `GameplayCanvas.battleHud` 绑定。
- 敌人血条由 `MoonlitCastleBattleStyle.asset` 的 `enemyHealthBarPrefab` 引用；生成敌人时实例化此 Prefab，并交回现有 `EnemyLifebar`、`Actor_Base` 与 `DropIn` 控制。

Prefab Mode 中可直接修改 `Core_9x16/ReturnButton`、`WavePlaque` 和 `PlayerStatus`。状态面板内头像、等级纸签、蜡封、文字、血条框与填充均为独立节点。装饰不参与射线检测，只有返回按钮接受点击。

编辑器菜单 `Tools > Dark Fairytale > UI > Create B3 Prefabs and Install (Once)` 仅在缺少 Prefab 时创建初版；已经存在时不会重新生成或覆盖手工布局。运行时代码不生成主 UI 节点。后续调整应直接编辑 Prefab，不依赖重跑构建器。

## 屏幕适配

设计参考尺寸 1080×1920，核心比例 9:16。CanvasScaler 使用 Expand，`BattleHudSafeArea` 将固定比例核心等比缩放并居中于安全区：

- 9:16：填满核心设计区。
- 3:4：保持核心宽高比，两侧留给场景扩展，按钮不会向屏幕外缘漂移。
- 1:2：保持核心宽高比，扩展上下场景，不挤压状态面板。
- 刘海/底部手势区：在 `Screen.safeArea` 内重新计算核心，不非等比拉伸美术。

控制器只改变核心容器的尺寸、位置和等比缩放，子节点的锚点、布局及 Inspector 调整会保留。

## 功能与状态

沿用返回营地、当前/总波次、玩家等级、生命、经验、敌人生命及 Boss 名称。波次标记最多显示 8 个，超过时仅显示准确数字，避免误导。护盾存在时保留紫色生命反馈。结算入口会关闭战斗 HUD 并恢复旧状态栏，营地和其他界面沿用原系统。

`CursedBattleHud` 仅绑定状态与返回事件，`GameplayCanvas` 的既有刷新方法同步新界面；缺少新 Prefab 引用时保留旧 UI 路径。

## 美术与字体

`CursedStorybookAtlas.png` 由内置 imagegen 依据已选 B3 生成，透明 RGBA。图集经 Unity Sprite 矩形引用拆分为 `Sprites/*.asset`，没有把功能文字烘焙进贴图。纸页和纸签支持九宫格，头像与蜡封保留比例。生成提示词见同目录 `prompts.txt`。

字体为 Noto Serif CJK SC Regular，来源 `https://github.com/notofonts/noto-cjk`，SIL Open Font License 随字体存放于 `Assets/DarkFairytale/UI/CursedStorybook/Fonts/OFL.txt`。字体随项目打包，不依赖运行设备安装中文字体。

## 验证

编辑器菜单 `Tools > Dark Fairytale > UI > Validate B3 in Gameplay`：在真实 Gameplay 中检查 Prefab 引用、中文字符、数值绑定、护盾状态、波次标记、点击射线、安全区及营地/战斗/结算切换。使用 720×1280、768×1024、720×1440 输出三种比例实景。

验证报告与截图保存在 `output/battle-ui/`；此目录由项目忽略，正式 Prefab、脚本、美术、字体和本文档均在版本控制范围内。编辑器测试会运行真实战斗流程；不要在有未保存 Play 状态时启动。

截图检查会暂停战斗并重新摆放角色以匹配每个宽高比。恢复时由测试器重启被暂停的攻击链；胜利阶段临时补满生命以排除数值平衡干扰，再单独调用实际失败结算入口。以上辅助行为只存在于 Editor 验证器，不进入正式游戏。

## 本轮交付验收（2026-10-09）

Unity 6000.6.3f1 实景验证通过：720×1280、768×1024、720×1440；模拟上下安全区；返回按钮射线；生命、护盾、经验、等级、波次；首关完整三波胜利；失败结算；返回营地后再次进入战斗。运行异常为 0。尚未执行手机真机测试。

最终报告与三种比例截图已额外保存到本目录的 `validation/`，随项目保留：

- [9:16 实景](validation/gameplay-9x16.png)
- [3:4 实景](validation/gameplay-3x4.png)
- [1:2 实景](validation/gameplay-1x2.png)
- [验证报告](validation/validation.txt)
