制作源素材与视觉对照（不用于正式游戏运行）

UI/：按界面或用途分组的已选设计稿、干净底稿、大尺寸原图及制作中间纹理。
UI/Intermediate/FrameTextures/：拆成紧凑边框之前的完整纹理，仅供重新制作与追溯。
UI/SkillRepairs/、UI/MapRepairs/：已批准补绘的源文件和提示词记录。
Characters/RedHood/Parts/：角色制作拆件；正式 Spine 图集仍在原运行目录。

以上路径相对于 Assets/ArtSources。以后新增的同类源素材统一放在 Assets/ArtSources，并按用途新建子目录。
当前迁移 97 个文件。旧素材 .meta GUID 和文件内容保持不变；原路径与新路径见迁移清单.csv。
正式图片、字体、运行图集、Prefab、插件界面图标和功能测试场景不属于本目录。
不要从正式场景、Prefab、材质或运行数据库引用此目录；不要设置为随包 Addressables，也不要在此创建 Resources 或 StreamingAssets。
制作工具已使用新路径；旧式动态路径由 Assets/Editor/ResourceAudit/Tools/ArtSourcePaths.cs 的迁移映射处理。
构建前自动检查此目录未进入运行依赖链。Assets 下普通文件夹不会自动排除打包，所以必须保留这项依赖检查。
历史审计报告可能仍记录原路径，用迁移清单对照；本次未更改视觉内容或运行布局。
