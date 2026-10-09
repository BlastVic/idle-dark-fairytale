# mon_7070 独立动画测试

打开 `Mon7070AnimationTest.unity`，点击 Unity 的 Play。
顶部面板提供 9 个动画切换、Pause、Loop、Auto cycle、Restart、播放速度与镜头缩放。
默认播放 idle；测试场景不依赖游戏主场景、战斗逻辑或存档，也未加入 Build Settings。

## 导入兼容性

原始文件来自项目根目录 `Spine/mon_7070`，原文件未修改。
项目自带 Spine 3.8 运行库在 SkeletonJson.cs 中明确拒绝 `3.8.75`。
本次仅将 `Art/mon_7070.json` 副本的 `skeleton.spine` 标记调整为 `3.8.99`，以测试其数据是否可由现有运行库读取；这不是 Spine 官方重新导出或通用版本转换。
这个资源使用 3.8 的数组 skins、IK、网格及线性/stepped 时间线，未修改骨骼、蒙皮、动画、顶点或曲线数据。
9 个动画均已在项目现有运行库中加载、逐帧执行并渲染验证。若后续重新导出资源，优先使用与运行库匹配的 Spine 3.8 导出版本。

原 PNG 使用预乘 Alpha，但项目使用 Linear 色彩空间。导入工具将纹理副本 RGB 除以 Alpha 转换为直通透明，保留 Alpha；Spine/Skeleton 材质开启 Straight Alpha Input，纹理开启 Alpha Is Transparency、关闭 Mip Maps 和压缩。原始 PNG 不变。

## 验证和预览

Unity 菜单 `Tools > Dark Fairytale > Mon7070`：

- `Create and Validate Test Scene`（Ctrl+Shift+F10）：从原始目录重新生成导入副本并创建场景，以 60 Hz 遍历每个动画，检查网格非空、顶点为有限数、姿态有变化、完成回调恰好一次，并渲染预览。重新生成前需退出 Play 并打开其他场景。
- `Open Test Scene`（Ctrl+Shift+F11）：打开测试场景。

验证输出在项目根目录 `output/mon7070/validation.txt`；各动画 PNG 也在该目录。
测试列表：attack_1、xxxx/death、die、idle、run、spawn、stun、victory、hit。
保留原始动画名称，包括原文件中的 `xxxx/death`。
