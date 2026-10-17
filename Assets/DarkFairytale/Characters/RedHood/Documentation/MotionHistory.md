> 历史制作记录：旧路径和双份动画同步流程已于 2026-10-17 退役。当前资源结构与操作以 ../README.md 为准。

# 小红帽侧面 Spine 动画测试 · V4 动作样片

## 横劈武器视角试作

Attack2 新增两张镰刀视角：0.16 秒切至蓄力侧后视，0.485 秒以原图过渡，0.52 秒切至斜前视，0.85 秒恢复原图。挂灯保持独立；切换不改变身体时间线或命中时点。

原图集不覆盖。新增资源在 `RedHoodWeaponViews.png` / `.atlas.txt` / `_Atlas.asset`，两个 SkeletonData 同时引用原图集与新图集。正式 Player 的受击闪白映射补齐新材质。

原始生成图与完整提示词在 `ArtDirection/Characters/RedHood/WeaponViews/`，使用内置 image_gen，参考 `Parts/scythe.png`。用带 Pillow 的 Python 显式运行 `ArtDirection/Characters/RedHood/build_weapon_views.py` 进行等比缩放、图集打包和附件安装，再在 Unity 执行 **Tools > Red Hood > Install Weapon Views (Explicit)**。生成工具不会自动运行；`refine_sweep.py` 会保留已安装的附件时间线。

**Review Weapon Views** 输出实际 Unity 对比到 `output/red-hood-motion/weapon-views/`。这是两张离散视角的试作，非连续三维旋转；攻击阶段的 `blade_tip` 是原有逻辑参考点，不代表不同透视贴图的每一处视觉刀刃位置。

## 2026-10-16 横劈发力调整

Attack2 加大抬手后拉与转腰幅度，0.32–0.46 秒保持蓄力姿势，0.46–0.56 秒快速横斩，随后短暂定住并顺势收招。刀尖在横斩阶段保持水平，肩部跟随前送保持握持。OnHit 仍为 0.56 秒，OnComplete 仍为 1.18 秒。

显式运行 `python3 ArtDirection/Characters/RedHood/refine_sweep.py` 只更新原型与战斗数据的 Attack2，不修改其他动作、骨架、网格或服装。Unity 菜单 **Tools > Red Hood > Review Sweep Power** 渲染前后对比，结果保存在 `output/red-hood-motion/sweep-power/`；前版数据须保存在该目录的 `before/` 下。

## V4：待机与镰刀下斩

- 骨骼从 22 根增至 28 根：披风和裙摆各增加 3 个控制骨；裙摆从整块图片改为加权网格。
- 披风反馈修正：摆动中心移到肩部，领口侧网格固定到躯干，三段控制改为串联骨链；待机小幅弯曲向末端传递，下斩跟随幅度相应重调。保留已有骨骼名称以兼容其他动作。
- Idle1 为 3.6 秒循环，骨盆和双腿维持稳定站姿；呼吸集中在上半身，辅以轻微头部、武器跟随，以及错相的布料和挂灯运动。取消初版反复屈膝的重心摆动。
- Attack1 强化压低蓄力、延后加速挥击、身体前压制动和布料滞后；命中仍为 0.56 秒，完成仍为 1.18 秒。
- 靴筒上端网格向膝部延伸，改善加深屈膝时暴露的关节接缝。
- 同步到正式战斗 JSON。其他动作时间线、技能别名和复活事件保留；共享布料网格也用于其他动作。
- 手动运行 `python3 ArtDirection/Characters/RedHood/refine_motion.py` 可重现本次调整，不会在运行、导入时自动执行。重复执行结果相同；执行会替换 Idle1/Attack1 的手工时间线修改。
- Unity 菜单 **Tools > Red Hood > Review Motion Sample** 在临时预览场景中逐帧验证、渲染，不保存或覆盖当前场景和 Prefab。
- **Review Idle Correction** 将本轮站姿修正与上一版比较，输出到 `output/red-hood-motion/idle-correction/`；校验包含待机双膝位置稳定性。
- **Review Cape Correction** 输出披风修正前后对比到 `output/red-hood-motion/cape-correction/`。披风仍为关键帧动画，不是实时布料物理。
- 本地结果位于 `output/red-hood-motion/`，左右分别为修改前/后。前后对比需要该目录的 `before/RedHoodBattle.json` 本地备份；没有备份时只渲染修改后。
- 此次为两个动作的质量样片，未重做 Attack2、技能或多武器动作，也未补画转身视角。验证覆盖运行时网格、事件、握持、脚掌固定、待机循环和中断切换，不等于商业美术质量验收。

## 原 V3 预览说明

打开 `RedHoodAnimationTest.unity`，Game 视图选择 **9:16**，点击 Play。
默认交替播放 Attack1（蓄力下斩）与 Attack2（蓄力横劈）；可暂停、调整速度、单独选择动作、拖动时间轴和逐帧检查。

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
主项目可执行 Tools > Red Hood > Validate Current Scene（退出 Play 后），输出到 `output/red-hood-prototype/`。
另已在主项目实际播放和拖动时间轴复查材质、命中姿态及连续攻击。
预览 GIF、关键帧表和报告保存在 `output/red-hood-prototype/`，当前文件名包含 v3。

当前尚未通过商业质量验收：大幅姿态中的关节覆盖、受力表现和死亡动作可读性仍需进一步美术调优；
披风与挂灯为关键帧动画，不是物理模拟。自动验证不代表视觉质量验收。
交付包含可导入 Spine 的 JSON / atlas / 分件图片，没有原生 `.spine` 编辑工程。
旧版 Skill4 / Skill5 / Skill7 未迁移；正式接入游戏时需单独制作和映射。

## 最新横劈版本（2026-10-16）

Attack2 已改为抬手 0.32 秒、停顿 0.14 秒、快速斩击 0.10 秒，再短暂停留及收势，总时长 1.18 秒。右手由左上向右下带刀；命中阶段采用内弧开刃、刀尖朝镜头的独立倾斜贴图，中间角度用于翻腕衔接，不强行压扁原竖刀。

显式更新流程：运行 `ArtDirection/Characters/RedHood/refine_sweep.py` 和 `build_weapon_views.py`；Unity 执行 Tools > Red Hood > Install Weapon Views (Explicit)，然后 Review Sweep Perspective。不会自动重建场景或界面。
也可以在编辑模式向 `output/red-hood-motion/sweep-perspective/review.request` 写入 `install`：一次性安装、逐帧渲染、正式 Prefab 检查；空文件只触发验证，不重建资产。结果在同目录 `result.txt`。

正式与原型骨架均已接入两张新视角；1×/1.5×/2× 连续播放、命中与结束事件、中断恢复、握持和正式材质映射已检查。实际 Unity 动图见 `output/red-hood-motion/sweep-perspective/playback-1.0.gif`。上文为早期记录；正式骨架已保留 Skill4/5/7 的现有映射，本轮没有重做这些技能。
