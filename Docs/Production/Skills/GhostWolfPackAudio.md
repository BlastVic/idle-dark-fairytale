# 召唤：幽灵狼群音效

- Skill2 的 OnHit（动画时间 2.03 秒，举起镰刀后）只触发一次音效序列。
- 先播放故事模式原有狼嚎，再播放用户提供的完整狼群音效；同一个独立 AudioSource 顺序播放，避免其他战斗音效改变音高。
- 狼嚎：`Assets/Resources/Audio/Skills/GhostWolfHowl.wav`，3 秒，来自已有故事混音原始素材 `ArtDirection/Audio/PrologueZh/full-mix-v3/distant-wolf.wav`。
- 狼群：`Assets/Resources/Audio/Skills/GhostWolfPack.mp3`，约 10 秒，原文件 `ElevenLabs_audio_elevenlabs-sound-effects-v2_Pack of wolves _2026-10-17T08_04_03.mp3`，未剪裁。
- 跟随音效音量及游戏时间倍率；暂停不推进序列。音效挂在施法角色下，不随狼群视觉节点提前销毁；死亡、返回营地、胜利、角色销毁及测试重置时停止。
- 五头狼的跑动、跨过敌人前方时结算五次伤害及动作队列保持原逻辑。
- 正式战斗与独立 SkillTest 场景共用此入口。编辑器验证：写入 `output/ghost-wolf-audio/run.request`，结果在同目录 `validation.txt`。检查举镰前静音、播放顺序、暂停、静音恢复、五次伤害、完整播放清理和重置/死亡中断。
