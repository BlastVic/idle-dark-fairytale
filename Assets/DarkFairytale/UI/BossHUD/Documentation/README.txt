Boss HUD — 2026-10-17

正式 Prefab：../Prefabs/StorybookBossHUD.prefab，嵌套于 CursedStorybookBattleHUD，所有图片、文字、充能光标与进度条为持久节点。运行时只绑定和动画，不重建 UI。制作工具仅显式 output/boss-hud/build.request 执行；已有 Prefab 不覆盖。

波次流程：WaveManager.CallNextWave 在发起怪物加载前 BeginWave；目前匹配两个正式 Boss 资源键 MobRivenfang、MobStillmere。先等待血条1.25秒充能与0.25秒收尾完成，再执行该波次怪物加载及原有进场动画。Actor_Base 完成初始化后绑定实例。转场仍覆盖画面时，充能计时暂停，确保玩家能完整看到血条演出。离开战斗或波次替换时取消待出场请求。充能1.25秒，从左到右，伴随0.22秒淡入/轻移、前沿亮线、充满金色闪光及头像7%脉冲。全程不修改生命值、不暂停战斗；充能完显示实际 HP。死亡、离开战斗、普通波次隐藏。头像/名称映射、时长、间距在 Inspector 调整。

布局：尊重安全区，可用宽度减左右18逻辑像素，不横向拉伸头像；标题实际下边缘向下16逻辑像素定位，面板高度170。道具提示根据横幅实际下边缘避让。普通怪物脚下血条不变；两个 Boss 原有嵌套 EnemyLifebar 的 Canvas 在 Prefab 中禁用，仍保留 Actor_Base 绑定和数值刷新，无运行时替换。

资源：新头像源稿 Assets/ArtSources/UI/BossHUD；正式256×256带alpha，设计120像素，脉冲峰值129像素。字体复用 NotoSerifCJKsc-Bold；PanelFrameV3、CleanPaperV3、HealthInk、MeterTransparent 复用正式独立素材。面板边框九宫格、纸纹平铺。所有功能文字为 UGUI Text。

验证：output/boss-hud/validation.txt 全部 PASS/DONE。两位Boss × 1080×1920、768×1024、720×1440 实际Unity截图（充能、全血、半血）；已目视检查。测试通过正式 WaveManager.CallNextWave 和 SpawnWave 加载怪物；数值测试用1200/600HP。验证立即启动、加载后绑定、旧血条禁用、充满、真实扣血显示、标题/提示间距、视口内布局、死亡清除、暂停下演出、普通波次隐藏与关闭清理。截图只保留output，不提交。

未完成项：APK构建、真机性能与用户最终演出验收。此轮没有设计稿与实景100%一致的声明，头像系基于现有角色新绘制。

最新调整验证：两位Boss×三种比例全部通过“充能期间无怪物实例”“血条演出完成后才生成并绑定Boss”的运行断言；头像改为120逻辑像素，三种比例实景已检查。
