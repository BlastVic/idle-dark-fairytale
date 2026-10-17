# MoonlitCastleBattle 背景退役（2026-10-17）

用户批准删除旧图，全部改用第一章节点 1 图片。

- 实际消费者是正式胜利和失败结算页，并非仅测试场景。两个 Prefab 现直接引用 `Assets/DarkFairytale/Environment/Chapter01/Node01/Background.png`，尺寸参数改为 941×1672，保留等比覆盖适配。
- 删除 `Assets/DarkFairytale/MoonlitCastleBattle.png`、中间 Sprite `Assets/DarkFairytale/UI/BattleResult/ForestBackdrop.asset` 及对应 meta。旧 GUID 与图片路径在 Assets/Tools 中无残留引用。
- 结算制作工具改为复用节点 1 Sprite，删除旧切片创建方法，并修正过期 Gameplay 场景路径。未运行整套 UI 重建。
- `MoonlitCastleBattleStyle.asset` 是有效战斗布局配置，其背景原本已是 BlackForest；不是被删除图片，不删除其角色定位、特效和血条配置。
- Unity 验证胜利/失败 9:16、3:4、1:2 渲染，奖励发放、下一节点、重新挑战和返回庇护所；本地截图与测试报告位于 `output/battle-result/`，汇总图位于 `output/moonlit-background-removal/results.jpg`。验证工具修复了已删除旧引导节点的空引用。
- 测试前后 savedata 哈希一致；本次未构建 APK。
