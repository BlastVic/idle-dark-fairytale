# Boss HUD 构建依赖修复

本轮 Android 打包被 ArtSourceBuildGuard 拦截：StorybookBossHUD 的两个 Image 引用 PanelFrameV3，间接依赖 Assets/ArtSources/UI/Intermediate/FrameTextures/5218655c2ca43aa51530.png。

使用现有 ProductionUiArt.FinalizePrefab 将两个边框转换为已拆分的正式四角和边缘素材，持久化为 Prefab 子节点。保持原锚点、颜色、尺寸和非射线阻挡；没有移动原稿入运行目录或绕过构建检查。新增显式修复菜单，并让 Boss HUD 初次制作执行相同收尾逻辑，避免再次引入原稿依赖。

Unity Boss HUD 回归完成，两个 Boss、9:16/3:4/1:2、入场蓄满、血量更新、普通波隐藏及离开战斗清除均 PASS。检查三比例实景边框完整和文字可读。日志与截图位于忽略目录 output/boss-hud；既有修复前截图保存在 before-build-frame-fix，历史玩家数据不同，不作为全屏逐像素一致的证据。尚未进行 Android 真机验证。
