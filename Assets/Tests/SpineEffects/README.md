# Spine 特效测试场景

打开 `Assets/Tests/SpineEffects/SpineEffectsTest.unity` 后运行。也可使用 Unity 菜单 `Tools > Dark Fairytale > Effects > Open Test Scene`。

目前共 12 项：原有 EmberBolt（火球）、EmberImpact（命中爆炸）、ClawArc（爪击）、AshDust（扬尘）、EmberBreath（火焰爆发），以及墨痕命中、荆棘格挡、阴影践踏、灰叶治疗、残翼复生、裂隙雷霆、蜡封护佑 7 种通用战斗粒子。全部直接引用正式战斗使用的 Prefab 和材质。

- 顶部按钮选择特效；切换会清理上一只。
- 重播、暂停/继续、循环开关；每次循环末尾留 0.35 秒。
- 时间轴拖动会暂停并定位；步进每次 1/30 秒。
- 宽度范围为 9:16 核心区的 2%–70%，默认 25%；这是观察尺寸，不是实际战斗尺寸。调整不会修改游戏资源。
- 播放速度 0.1–2 倍；支持战斗背景和深色纯色背景。
- 宽度按完整动作的联合包围盒计算（粒子按可见粒子位置和尺寸测量），不会随当前帧忽大忽小。

本场景只验收单个特效本体，不模拟宠物起手、弹道或伤害。完整攻击仍需在 MonsterAnimationTest 中选择 MobBookSpineDragon，或在 Gameplay 中验证。原特效存在的尺寸、弹道和命中反馈问题尚未修复。

`SpineEffectsTestRig.prefab` 内含可编辑 UGUI、相机、背景及十二个资源引用；可在 Inspector 调整。构建入口只在缺失时创建，不在导入或运行时重建 UI，不覆盖已有 Prefab。

验收命令：编辑模式下写入 `validate` 至 `output/spine-effects-test/command.request`。结果与实际 Unity 截图仅保存在该忽略目录。验证选择、播放、暂停、步进、尺寸、背景开关以及 9:16、3:4、1:2 三比例。

通用贴图的设计说明、生成提示词与参数变更见 `ArtDirection/Effects/Common/`。通用粒子预览为固定随机种子，可暂停、拖动和步进；预览实例暂停销毁计时器，正式战斗的销毁机制不受影响。
