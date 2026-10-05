# 小红帽侧面 Spine 动画测试 · V3

打开 `RedHoodAnimationTest.unity`，Game 视图选择 **9:16**，点击 Play。
默认交替播放 Attack1（蓄力下斩）与 Attack2（上挑）；可暂停、调整速度、单独选择动作、拖动时间轴和逐帧检查。

## 本次修正

以 `output/imagegen/vertical-battle-sprite-focus-v2.png` 左下角朝右的小红帽为参考。
红兜帽、金发、破损斗篷、深色裙装、长靴、镰刀和独立挂灯均按参考重新拆分。
这是独立的侧面骨骼与全新时间线，不再套用原骑士背面动作。

- 19 根骨骼；手臂两骨 IK 跟随武器握点，两条腿各有独立膝关节 IK 和脚掌目标。
- 大腿、靴筒、披风和兜帽领口使用加权网格；攻击时脚掌固定，下蹲由膝关节弯曲完成。
- 修正 atlas 页首空行导致材质列表被清空的问题；直通 alpha 图集使用 Straight Alpha Input，避免粉色材质及透明矩形。
- 调整下斩和技能的命中姿态，避免刀尖扫入前靴；死亡改为屈膝失力，披风逐渐下垂，复起从完整结束姿态还原。
- 斗篷、裙摆、头部和挂灯有独立的关键帧摆动。
- 8 个动作：Idle1、Attack1、Attack2、Hit、Skill1、Death1、Victory、Rebirth。
- 两种攻击及 Skill1 各有一个 OnHit；普攻末尾保留 OnComplete 供预览连续播放。
- 独立预览，不接入正式战斗逻辑。

## 资产及重建

`RedHood.json` 为 Spine 3.8.99 数据；搭配 `RedHood.atlas.txt`、`RedHood.png`、
`RedHood_SkeletonData.asset` 和 `RedHood.prefab` 使用。

美术流程：参考图手工描边拆分 → 经用户授权的 imagegen CLI/API 清理分件 → 图集与骨骼组装。
生成素材：`output/imagegen/red-hood-side-parts-v2.png`。
完整提示词：`output/imagegen/red-hood-side-parts-v2-prompt.txt`。
可复现脚本：`output/red-hood-prototype/build_side_rig.py`（Python + Pillow）。
运行后在 Unity 选择 Tools > Red Hood > Rebuild Test Scene。
`build_assets.py` 是旧版骑士换皮脚本，不应再运行。

## 验证与范围

在隔离项目和主项目中用 Unity 6000.6.3f1 / 项目自带 Spine 运行时逐帧采样全部动作，
检查有限顶点、精确命中/完成事件次数和前臂末端到握点的误差（不超过 0.05 Unity 单位）。
主项目可执行 Tools > Red Hood > Validate Current Scene（退出 Play 后），输出到 PreviewOutput。
另已在主项目实际播放和拖动时间轴复查材质、命中姿态及连续攻击。
预览 GIF、关键帧表和报告保存在 `output/red-hood-prototype/`，当前文件名包含 v3。

当前尚未通过商业质量验收：大幅姿态中的关节覆盖、受力表现和死亡动作可读性仍需进一步美术调优；
披风与挂灯为关键帧动画，不是物理模拟。自动验证不代表视觉质量验收。
交付包含可导入 Spine 的 JSON / atlas / 分件图片，没有原生 `.spine` 编辑工程。
旧版 Skill4 / Skill5 / Skill7 未迁移；正式接入游戏时需单独制作和映射。
