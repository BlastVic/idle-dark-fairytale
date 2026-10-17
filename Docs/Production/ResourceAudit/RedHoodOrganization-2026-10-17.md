# 小红帽角色资源归档

统一根目录：`Assets/DarkFairytale/Characters/RedHood`。Spine 为唯一正式动画/图集/材质，Prefabs 为正式 Player，Tests 为预览场景及播放控制，Editor 为 Unity 与 Python 显式制作工具，Documentation 保留标注过时路径的历史记录和迁移索引。

正式 Player、RedHoodBattle JSON/SkeletonData、两张图集及普通/闪白材质保留原 GUID，移动前后文件哈希一致。原型与正式 JSON 的骨架、皮肤、事件及共有 13 个动作完全一致，正式版额外含 Skill4/Skill7。因此预览改为共用正式 SkeletonData，删除重复 RedHood.json、RedHood_SkeletonData.asset、无人引用的 RedHood.prefab。删除旧 Assets/RedHoodPrototype 目录。

ArtDirection/Characters/RedHood 中的 Python 工具迁入 Editor/Authoring；生成原稿、表情、武器视角与参考图归入 Assets/ArtSources/Characters/RedHood。清除 Python 缓存，移除原 ArtDirection 角色目录。代码、制作工具中的路径全部更新；历史路径仅留在历史说明、迁移索引与校验禁用规则中。

预览重建工具现在只读取正式骨架，不再重写贴图压缩设置、建立重复 SkeletonData 或保存无用预览 Prefab。所有 Python 动作脚本只修改一份正式 JSON，且仍须显式运行。

验证：

- 正式资源内容哈希未改变，已删除资源 GUID 无残留引用。
- Python 工具语法与资源字面路径检查通过；没有为了验证执行会重写动画的制作脚本。
- Unity 编译、单骨架 15 动作加载、预览引用、运行依赖检查通过，正式资源不依赖 ArtSources。
- Player Prefab、攻击/技能事件、死亡复活完成事件、材质闪白映射验证通过。实际预览为 output/dark-fairytale-battle/player-integration.png。
- 营地三比例、章节切换、战斗进入及返回回归全部通过，见 output/redhood-organization/gameplay-validation.txt。
- 本轮存档前后哈希一致。资源备份与迁移证据位于本地 output/redhood-organization，不提交截图。

营地坐姿小红帽属于独立 UI 演出，继续保留在 UI/Shelter。此次未重绘或改变任何正式视觉效果，未构建 APK。
