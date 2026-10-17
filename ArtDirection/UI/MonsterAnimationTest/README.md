# 怪物动画测试 UI

`approved-concept.png` 是 2026-10-11 确认的设计参考（内置 imagegen 生成），不是游戏截图。

界面沿用 B3 灰黑墨色、细铜线与暗红选中态；顶部显示小红帽开关和视角重置，怪物库默认折叠，展开后提供搜索及图片／编号／名称卡片；底部提供动画、暂停、重播、循环、轮播、速度和角色缩放。中间使用实际黑森林战斗背景与 Spine 模型。

可编辑 UI：`Assets/Tests/MonsterAnimation/UI/Prefabs/MonsterPreviewHUD.prefab`。怪物卡片与动画按钮另存为 Prefab；运行时通过真实 Text、Button、Toggle、InputField、Slider 和 ScrollRect 交互，未将功能文字烘焙为图片。

验收截图仅保存到本地 `output/monster-preview-ui/`，不提交 Git。操作与编辑说明见 `Assets/Tests/MonsterAnimation/Documentation/README.md`。

## 验证结果

Unity 6000.6.3f1：720×1280、768×1024、720×1440 的展开／折叠实景已检查。搜索、列表末项滚动、全部五个怪物切换、动画切换、暂停／重播／循环／轮播、独立 Idle、隐藏／显示、角色边界、运行帧射线和模拟安全区检查通过，运行异常为 0。文字记录见 `validation.txt`。未进行手机真机测试。
