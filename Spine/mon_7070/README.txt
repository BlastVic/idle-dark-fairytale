mon_7070 Unity 导出

来源：D:\Workspace\Spine\S238欧美风格怪物3.8.75版本\mon_7070\mon_7070.spine
导出工具：Spine 3.8.75 Professional 命令行
本次使用磁盘上的已保存工程，不包含编辑器中尚未保存的修改。

导入 Unity：
1. 安装与数据兼容的 spine-unity 3.8 运行时。
2. 将 mon_7070.json、mon_7070.atlas.txt、mon_7070.png 一起复制到 Assets 下同一文件夹。
3. 等待 spine-unity 生成 AtlasAsset、材质和 SkeletonDataAsset，再创建 SkeletonAnimation。
4. 图集为预乘 Alpha（PMA），使用匹配的 Spine PMA 材质；纹理关闭 Alpha Is Transparency，关闭压缩可避免细节和边缘损失。

动画：attack_1、xxxx/death、die、idle、run、spawn、stun、victory。
已新增 hit 受击动画：14 帧（30 FPS，约 0.4667 秒），非循环播放。保留原有 8 个动画。
动作包含后仰挤压、闭眼、手臂和头盔延迟回弹，首尾对齐 idle 起始姿态。
Unity 调用：skeletonAnimation.AnimationState.SetAnimation(0, "hit", false);
skeletonAnimation.AnimationState.AddAnimation(0, "idle", true, 0);
原始 JSON 备份：settings/mon_7070.before-hit.json.bak。
可编辑工程：../mon_7070_hit_preview/mon_7070_hit.spine。
新增 JSON 已成功通过 Spine 3.8.75 导入及再导出验证；原有数据与动画逐项比较一致。
命令行图像预览存在颜色/透明度异常，最终外观仍需在编辑器或 Unity 中确认。

检查结果：JSON 可解析；18 个附件路径均可在图集中找到；1024×512 PNG 可正常解码。
尚未在 Unity 工程中实际导入验证。
settings 文件夹保存 JSON 导出与图集打包参数，不必导入 Unity。
