# 新装备命名同步（2026-10-17）

此前仅替换图标，旧模板仍有 Headband 等与新外观不符的名称。现逐类核对五张实际图标联系表，完成 140 个唯一中文名称，最长 6 字；总表见 `Docs/Production/EquipmentNames.md`。

- 正式名称数据：`Assets/Resources/Localization/EquipmentNames.json`，不引用 ArtSources。
- 修改 10 个场景/怪物 Prefab/数据库文件中的 133 条装备模板，仅名称字段改变；静态比较确认非名称字段完全不变。
- 新掉落写入对应名称。装备详情、宝箱奖励、道具提示和兼容弹窗通过 EquipmentNames.Display 按图标键查询，旧存档缓存的旧英文全名不再覆盖新名称；非装备奖励保留原文案。品质继续由独立品质 UI 展示，已覆盖的正式装备不再叠加旧英文随机名称前后缀。
- Unity 全量测试：140 个名称、140 个旧存档展示样例、140 个新掉落命名均通过；三种比例实际装备 UI 验证通过，COMPLETE failures=0 errors=0。
- 图标键、物品 ID、品质、属性及背包归属不变，不执行存档迁移或回滚。
- 本地测试前后存档文件哈希有变化，未把它报告为“完全未变”；检查当前存档无本次测试的 997000–997139 物品 ID，保留当前存档。测试启用既有持久化隔离，未执行任何恢复操作。
- 本地报告与截图在 output/equipment-names 和 output/equipment-fullset，不提交截图。本次未打 APK。
