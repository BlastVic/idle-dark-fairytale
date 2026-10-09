# 统一怪物动画测试

制作与战斗接入要求参见[怪物制作手册](../../../ArtDirection/monster-production-manual.md)。

打开 `Assets/Tests/MonsterAnimation/MonsterAnimationTest.unity`，点击 Play。
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
选择 `BF_Mushroom_Wine`、`BF_Mushroom_Moss`、`BF_Mushroom_Moon` 对应的 SkeletonDataAsset 即可预览朝左的四个基础动画；优先播放 `Idle1`。
完整交付、实战入口、重建步骤和源工程限制见[三色蘑菇说明](../../../ArtDirection/Monsters/BlackForestMushroom/README.md)。
