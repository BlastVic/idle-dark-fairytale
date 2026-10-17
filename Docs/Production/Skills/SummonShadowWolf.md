# 召唤：影狼

替换原“三连斩”，保留枚举数值 2、技能树节点位置、回旋镰斩前置及后续依赖。旧存档的 `pathCleanerLevel` 继续作为持久化键，读写新的 `SkillsController.shadowWolfLevel`；场景序列化通过 `FormerlySerializedAs` 兼容。原三连斩随机触发与 Skill4 伤害分支已移除。

## 能力

学习后每场战斗自动召唤一只 `PetShadowWolf`，可以与书脊龙同时出战。影狼使用现有 Attack1 的 OnHit 事件，攻击当前首个可选敌人；无目标时待机。每次攻击后等待 Prefab 中的 `attackDelay`（当前 0.8 秒）。没有额外手动施放按钮。

初版成长沿用书脊龙，后续可在 SkillsDataDescriptions 中单独调整：

| 等级 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| 攻击力倍率 | 150% | 175% | 200% | 225% | 250% | 275% | 300% | 325% | 350% | 400% |

伤害有 ±10% 浮动，经过敌人防御、取整和最低伤害计算，不暴击；使用伙伴伤害飘字和通用命中特效。影狼倍率由 Prefab 的 `damageSkill = PetShadowWolf` 绑定，幽灵狼保持 NONE 与独立倍率，不受此技能升级影响。

角色死亡或返回营地时退出；胜利返回复用 Pet 标签清理；角色复活时重新召唤已学习的伙伴。两只伙伴通过不同持久化站位分开，骨架和用户已设定的显示尺寸保持不变。

## 正式资源

- 影狼：`Assets/DarkFairytale/Companions/PetShadowWolf/Prefabs/PetShadowWolf.prefab`，复用现有 GENERAL / LocalPrefabs 注册。
- 技能 UI：`Assets/DarkFairytale/UI/Skills/Prefabs/StorybookSkills.prefab`，只更新原槽位的名称、说明、效果标签与图标。
- 图标：`Assets/DarkFairytale/UI/Skills/Textures/SummonShadowWolf.png`，256×256 RGBA；2026-10-17 按用户要求改为专门绘制的黑灰狼头与荆棘环，暗红眼睛与现有墨绘技能风格统一，源图及生成提示词归档在 `Assets/ArtSources/UI/Skills/ShadowWolfHead/`，透明轮廓固化，无运行时抠除。节点显示约 138×120 设计像素，使用普通 UI 材质，并复用到详情、技能数据库与战斗状态栏。
- 战斗状态栏：正式 HUD 内持久化新增影狼节点，沿用书脊龙的显示方式。
- `ShadowWolfSkillAuthoring.Apply` 是明确调用的一次性、定点迁移，不在导入或运行时重建 Prefab。后续 Inspector 修改保留；不要为日常游玩反复执行迁移。

## 验证

实际 Unity 验证与截图存放在忽略目录 `output/shadow-wolf-skill/`，不提交游戏截图。验证脚本 `ShadowWolfSkillValidation` 禁用存档写入，通过正常技能路由与战斗初始化运行。

Unity 编译及以下实际运行检查通过：

- 未学习不召唤，学习消费 1 点，升级显示 150% → 175%，9 → 10 级消费 2 点并显示 400%。
- 9:16、3:4、1:2 下未学习、已学习、按下、技能点不足禁用、恢复、满级与详情关闭后的射线状态；前后截图保存本地。
- 正式战斗初始化自动召唤；书脊龙与影狼同场；影狼 OnHit 实际降低敌人生命；1 级和 10 级倍率正确。
- 同一场两波战斗不会重建影狼；胜利返回清理；死亡取消攻击并退出；完整死亡/Revive 后重新召唤且只有一只活跃影狼。
- 三种比例下的正式战斗站位、双伙伴状态图标及全部六枚技能状态图标实际截图检查通过。验证场景临时提高敌人和玩家生命以避免过早结束，不写用户存档。
- 静态核对旧存档键双向映射、全部 10 级数值与 Attack1 单次命中事件。未进行真实用户存档的写入迁移测试。

本次没有执行 APK 构建或真机性能验收，这两部分仍未完成。

## 狼头图标更新（2026-10-17）

用户要求将全身影狼图标改为专门绘制的狼头。现使用内置 image_gen 生成的黑灰墨绘狼头、暗红眼睛与荆棘圆环；正式透明 PNG 256×256，保留 GUID，技能树、详情、数据库和战斗状态图标同步引用。源图与完整提示词见 `Assets/ArtSources/UI/Skills/ShadowWolfHead/README.md`。移除制作工具的全身伙伴图标渲染回退，防止重建时恢复旧外观。

本次 `output/shadow-wolf-skill/icon.txt` 的三比例 UI 检查通过；`icon-0/1/2-*.png` 保存实际 Unity 的未学习、按下、已学习、禁用和满级画面，检查狼头轮廓、透明边缘及文字未被遮挡。原 before 截图为此前版本的基线，并非本次同存档数据的逐像素对照。本次只更换图标，未重新进行 APK 或真机验收。
