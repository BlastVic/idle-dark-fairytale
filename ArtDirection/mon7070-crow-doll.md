# mon_7070：缝魂鸦偶美术制作方案

状态：方案与骨骼适配已整理，尚未生成或替换美术。

## 视觉方向

依据用户提供的暗黑童话游戏参考图：手绘墨线、低饱和冷色、粗布缝线、破损补丁、乌鸦羽毛和少量酒红点缀。
将当前明黄色头盔小鸟改成矮胖的缝合鸦偶。保留短腿、宽身、短翅和夸张表情，让原有奔跑、受击、倒地、出生动作继续成立。

- 身体：旧骨白粗布，暗灰接缝，胸腹带不对称暗紫补丁；避免整片纯白或塑料高光。
- 头部：以分片布兜帽与羽冠替代金属头盔，不新增长耳或大幅伸出的肢体。
- 翅膀：炭黑、灰紫短羽，沿原双臂的长轴分层排布，肩根充分重叠遮住接缝。
- 眼睛：一枚暗红纽扣眼、一枚黑纽扣眼；同步绘制闭眼附件，保留现有表情切换。
- 嘴部：小型缝合喙嘴，准备两套张合形状；不把嘴画死在身体贴图上。
- 足部：旧皮革包裹的短鸟爪，与原脚掌轮廓相近。
- 领结：酒红小领结绘入身体下方不受表情影响的区域；不新增独立动画骨骼。

## 现有附件适配

现有资源含 15 个槽、18 个网格附件，非简单的整张角色图片。应逐附件制作、对齐原 atlas 和 UV，不能把完整概念图直接替换成图集。

| 原附件 | 替换内容 | 动画约束 |
| --- | --- | --- |
| body | 骨白布偶身体、补丁、领结 | 保持躯干边界与肩腿连接位置 |
| arm_left / arm_right | 分层短鸦翅 | 保留肩根、长轴、末梢位置，先保留权重 |
| head_front / head_hat | 兜帽前沿与主布片 | 保持片间搭接与遮挡顺序 |
| head_back / head_back_B | 后侧暗羽与布片 | 不露出原黄色/金属边缘 |
| head_center | 紧凑羽冠 | 复用原帽尖骨骼，必要时缩短网格 |
| head_cap | 完整备用帽形 | 即使默认不可见也需重绘，覆盖动画切换 |
| L_eye / R_eye | 两枚不对称纽扣 | 保留眼部骨骼与网格变形 |
| LC_eye / RC_eye | 挤眼、闭眼表情 | 与正常眼睛尺寸、位置一致 |
| mouth / mouth2 | 缝合喙嘴的两套表情 | 保留动画中的附件名和切换 |
| leg_left / leg_right | 皮革鸟爪 | 保留 IK、接地点和脚踝位置 |
| shadow | 接地阴影 | 复用 |

## 生成提示词

Use case: style-transfer
Asset type: production artwork for a Spine 3.8 animated game enemy, delivered as separate transparent cutout parts.
Primary request: redesign the existing squat round bird enemy as a haunted stitched crow doll, matching the supplied dark fairytale game art reference.
Input images: the user's moonlit forest battle illustration is the style reference; the original assembled mon_7070 and extracted attachment images are geometry and registration references, not a style reference.
Style/medium: hand-painted 2D dark storybook game art, irregular confident near-black ink contours, restrained textured shading, clearly readable at small game scale.
Color palette: aged bone linen, charcoal feathers, muted plum patches, tiny oxblood bow and button accents.
Materials/textures: worn woven cloth, visible irregular stitches, matte layered feathers, battered leather claws. No glossy metal helmet.
Subject: compact pear-shaped crow rag doll, short wings, short legs, asymmetric red and black button eyes, small stitched beak-like mouth, patched cloth hood with a compact feather crest.
Constraints: match each supplied attachment's orientation, silhouette footprint, pivot overlap and registration. Keep eyes and mouth off the body texture because they animate independently. Keep feather wings inside the existing arm mesh silhouettes as much as possible. No new limbs, no detached props, no background, no ground shadow baked into body, no labels or watermark. Use real transparency where the generation route supports it; preserve the source alpha/registration during atlas assembly.
Avoid: bright yellow cartoon bird, shiny fantasy armor, smooth 3D rendering, excessive saturated purple glow, intricate details that disappear at gameplay scale.

## 落地与验收

1. 保存原始版本，新美术作为项目内独立资源；保留参考图、最终提示词与生成来源记录。
2. 从原 atlas 提取附件制作配准模板，先完成主要身体、翅膀和头部，再处理表情与遮挡部件。
3. 若生成方式不支持透明背景，使用现有附件的 Alpha 轮廓作为严格配准蒙版；不能把背景颜色带入游戏。
4. 保持动画名、槽名、附件名和默认权重。只有画面验证发现必须修正时才微调局部顶点或骨骼偏移。
5. 更新测试场景到新美术，并调整导入工具，防止重新生成场景时恢复旧图集。
6. 验证 9 个动画：attack_1、xxxx/death、die、idle、run、spawn、stun、victory、hit；检查闭眼、张嘴、肩根、帽片、脚底、倒地遮挡和完整轮廓。
7. 在 Unity Play 模式检查连续动作，并输出待机、攻击、受击、倒地截图供审阅。
