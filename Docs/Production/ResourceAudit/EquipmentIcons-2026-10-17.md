# 全套装备图标替换

2026-10-17：按旧版名称重制 140 张独立装备图标，正式文件均为 256×256 RGBA PNG。早期朴素，后期逐步增加结构与装饰；初始兜帽为圆顶。以轮廓、类别、材质及结构区分装备，未采用同图换色生产。

| 路径（Assets/DarkFairytale/UI/Equipment/Icons 下） | 名称 | 数量 |
| --- | --- | --- |
| Weapons | Wep1–Wep28 | 28 |
| Helmet | Head1–Head26 | 26 |
| Shields | Sh1–Sh25 | 25 |
| Armors | Body1–Body30 | 30 |
| Accesories | Acc1–Acc31 | 31 |

使用内置 imagegen 逐件生成；高分辨率原稿及逐件提示词保存在 `Assets/ArtSources/UI/Equipment/FullSetV1`。正式资源由显式执行的 `Tools/ResourceAudit/prepare_equipment_icons.py --apply` 缩至 256，不在运行或导入时自动生成。Wep17 清理了护手后的多余光晕，Acc16 收敛为单只耳饰，相应修订提示词另存。

保留正式资源 GUID 与 Sprite fileID。恢复 Wep25 原 GUID，修正 Gameplay 中 Wep25 错指 Wep26 的旧登记，补回 Wep26，删除重复 Head1。旧清理工具已移除 Wep25 候选。没有更改装备数值、掉落或游戏运行代码。

验证：

- 140 张尺寸、透明通道、唯一图像哈希及同名场景引用通过；没有缺失或重复装备图标键。
- 原装备界面功能回归通过：`output/equipment-fullset/after-ui/validation.txt`，`PASS COMPLETE failures=0 errors=0`。
- 新增显式触发的编辑器检查工具 `Assets/Editor/EquipmentIconSetValidation.cs`：全部 140 个键加载为同名 256px Sprite；五类装备在 941×1672、768×1024、720×1440 渲染，45 张截图；`unity-validation.txt` 为 `PASS COMPLETE failures=0 errors=0`。
- 五类缩略总览已检查，并抽查三种比例的实际 Unity 图像。截图与替换前备份仅放在忽略的 `output/equipment-fullset`。
- 测试使用禁止写入存档的隔离开关；测试前后 savedata 键哈希一致。

正式 PNG 源文件合计由 7,341,824 字节变为 9,867,644 字节；PNG 文件体积不等于平台压缩后的纹理大小或 APK 增量。本次没有重新打 APK，未宣称 APK 大小变化。

## 正式资源迁移

140 张正式图标与全部文件及目录 `.meta` 已由 `Assets/Grfx/Menus/Items` 整体移至 `Assets/DarkFairytale/UI/Equipment/Icons`。迁移前后逐文件 SHA-256 一致，GUID 保留，场景引用无需改写；生产缩图工具已更新至新路径。此次仅迁移装备图标，不代表 Grfx 剩余内容已可整目录删除。

迁移后重新运行全量 Unity 检查，140 个图标键及三种比例渲染通过（PASS COMPLETE failures=0 errors=0）。检查沿用禁止写入存档的测试隔离；本次未记录新的迁移前存档哈希，不能用上轮绘制前的旧哈希判定本轮存档变化。
