# 目录结构整理（2026-10-17）

用户要求：分类文件夹之间不混放零散文件，整理项目为合理、可逐目录核查的结构。

## 完成内容

- 迁移 714 个文件（另保留对应 meta），包括 UI Prefab、材质、Shader、环境/公共贴图、伙伴/开场 Spine、音频、测试场景、运行脚本和编辑器工具。分类层只放子目录，文件进入对应末级目录；没有拆散完整 Spine 导出组。
- 原 `MoonlitCastleBattleStyle.asset` 归入 `Assets/DarkFairytale/Battle/Settings/BattleStyle.asset`；ContactShadow 与 SoftGlow 归入 `Effects/Shared/Textures`。
- 共用 RuntimeArt 贴图保留为末级目录，Variants/FrameParts Sprite 转入 `UI/Shared/Sprites`。
- Resources 自有配置、章节环境、文本分别归入 Data、Environment、Localization；同步更新正式加载键、泛型单例、编辑器生成默认路径和验证工具。
- 编辑器代码分 Authoring、Validation、BuildPostprocess、ResourceAudit；资源审计进一步分 Tools/Data。角色专属工具仍在角色 Editor 子树。
- 处理完整路径、Root 拼接、动态材质命名、音频页码拼接、Python 输出目录和制作清单。没有调用 UI 重建工具。
- 根目录崩溃转储移至忽略的 Logs/Crashes，清除 Assets 的 Finder 缓存文件。
- 更新 AGENTS.md；目录说明见 `Docs/Production/ProjectStructure.md`。完整旧/新路径与原 GUID 位于 `Assets/Editor/ResourceAudit/Data/ProjectStructureMigration.json`。

## 保留的约束

自有 Assets 中只保留 3 个固定入口例外：SpineSettings.asset、IAPProductCatalog.json、BillingMode.json。已从 Spine/Unity IAP 代码核对其固定路径。第三方、Unity 配置和既有外部源工程的内部结构不做强行拆分，原因与边界见目录说明。

## 验证

- Python 结构检查：自有分类目录无混放问题、迁移旧路径无残留、GUID 保留且无重复。
- 所有原有序列化 GUID 引用集合与迁移前一致；迁移的图片、音频、Prefab、场景、材质、Shader、动画文件内容哈希不变。
- Unity 实际导入验证：714 个迁移文件可加载，25 个 Prefab 无缺失脚本，2 个移动的 Spine SkeletonData 可解析；Resources 新键及数据单例能加载，本地 Prefab 索引完整，ArtSources 无正式运行依赖。
- 装备回归通过：三种比例、安全区、属性加点、装备操作、地图/技能导航、返回营地，`COMPLETE failures=0 errors=0`。
- Resources 改动后营地回归通过：三种比例、角色与篝火动画、章节解锁、战斗进入和返回，测试禁用存档持久化。
- 证据仅在忽略的 `output/project-structure`、`output/equipment-ui`、`output/black-forest-camp`。本次未构建 APK。

测试前后 savedata 哈希一致，未回滚或修改用户存档。最后一轮编辑器编译无新增 C# 错误，修改过的 Python 工具语法检查通过。
