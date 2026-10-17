# 项目目录结构

分类层只放子目录，文件放在明确用途的末级目录；完整 Spine 导出组作为同一个制作单元保留。不要把已有目录中的文件再任意散放到功能根目录。

## 正式内容

| 目录 | 用途 |
| --- | --- |
| `Assets/DarkFairytale/Characters` | 正式角色，内部区分 Spine、Prefabs、Tests、Editor、Documentation |
| `Assets/DarkFairytale/Monsters` / `Companions` | 怪物与伙伴，按资源键组织 |
| `Assets/DarkFairytale/Battle/Settings` | 通用战斗布局参数，现为 `BattleStyle.asset` |
| `Assets/DarkFairytale/Environment` | 正式章节环境与环境动画 |
| `Assets/DarkFairytale/Effects` | 正式特效；公共贴图位于 `Shared/Textures` |
| `Assets/DarkFairytale/UI` | 按页面/功能分类，功能内按需使用 Prefabs、Sprites、Textures、Materials、Shaders、Data |
| `Assets/ArtSources` | 不进入运行依赖的制作原稿，说明与迁移清单位于 Documentation |
| `Assets/Audio` | 按使用场景与声音用途分类，存在下级分类时声音文件放 Clips |
| `Assets/Prefab` | 通用系统对象、动画及仍使用的功能包 |
| `Assets/Scenes` / `Assets/Tests` | 正式场景与功能测试；怪物测试的场景、脚本、UI Prefab 分层 |

`Assets/DarkFairytale/UI/Shared/RuntimeArt` 是正式共享贴图末级目录；其衍生 Sprite 归入 `Shared/Sprites/Variants` 与 `FrameParts`，不能在贴图目录里混放代码或新分类。

## 代码与配置

- `Assets/Scripts`：Core、Combat、Environment、Effects、Companions、Audio、UI 等；保留现有命名空间和程序集边界。
- `Assets/Editor/Authoring`：显式制作/修复工具。
- `Assets/Editor/Validation`：运行验证与渲染检查。
- `Assets/Editor/BuildPostprocess`：Android 构建与构建后处理。
- `Assets/Editor/ResourceAudit/Tools` 和 `Data`：审计工具、制作清单与迁移记录。
- `Assets/Resources/Data`：GameSettings、SkillsDataDescriptions、LevelDataDescriptions；单例通过 `Data/<类型名>` 加载。
- `Assets/Resources/Environment`：Chapter01Environments，通过 `Environment/Chapter01Environments` 加载。
- `Assets/Resources/Localization`：IdleLocalTexts，通过 `Localization/IdleLocalTexts` 加载。
- `Tools`：项目外的命令行制作/审计脚本；`Docs`：项目说明与文字审计报告。

## 明确的固定路径例外

以下位置有供应商代码约束，不能为目录外观擅自移动：

- `Assets/Editor/SpineSettings.asset`：SpinePreferences 的固定设置路径。
- `Assets/Resources/IAPProductCatalog.json`：Unity IAP 的 ProductCatalog 固定路径与 Resources 键。
- `Assets/Resources/BillingMode.json`：Unity IAP 的 UnityPurchasingEditor 与 SelectedAndroidStoreHelper 固定入口。

第三方插件、AddressableAssetsData、StreamingAssets、Packages、ProjectSettings 保持工具规定的内部结构，尤其不能移动 asmdef 或原生插件改变作用域。工程根目录的 README、AGENTS、Git 配置是入口文件。ArtDirection 和 Spine 中的既有外部制作工程保留源工程及其相对依赖结构，不按 Unity 运行资源规则拆散；新增独立美术原稿仍统一归入 Assets/ArtSources。Library、Temp、Build、Logs、output 是缓存或输出，崩溃转储已收至 Logs/Crashes。

## 检查与追溯

- 命令行：`python3 Tools/ResourceAudit/check_structure.py`，检查自有 Assets 混放、迁移 GUID、旧路径残留和重复 GUID。
- Unity：`Tools > Resource Optimization > Validate Project Structure`，检查实际导入、Prefab 脚本、Spine、Resources 和运行依赖。
- 迁移清单：`Assets/Editor/ResourceAudit/Data/ProjectStructureMigration.json`。sha256 是迁移前快照；代码和当前文档可能因路径更新而改变。
- 检查工具只读，不在导入时重建 Prefab，不覆盖用户调整。截图与详细运行结果放忽略的 output。
