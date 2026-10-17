用户于2026-10-17提供并指定两段音效。音效1=Single sharp wh_2026-10-17T07_35_55.mp3；音效2=Seamless loopin_2026-10-17T07_33_35.mp3。同目录保留原始MP3。
音效1保留前0.65秒有效挥砍和余音，末0.1秒淡出；从Skill5动画时间0.46秒起播，主要峰值约0.1秒后落在OnHit=0.56秒附近。随TrackEntry攻速调整播放速度。
音效2保持原始音高和转速，截取1.2667秒，最后0.2秒淡出。AOE Particles.prefab上的spawnSound直接绑定ScytheSpin.wav，在CommonSpineBattleEffect.Start真正播放特效时起声，跟随对象清理，不依赖异步加载前的预估等待。
标准攻速：0.46秒挥砍，0.56秒发起特效，实例可见持续1.2667秒，约1.8267秒结束。2倍攻速：0.23、0.28、1.5467秒。实例化可能有帧级偏差，旋转声跟实例起点。
正式资源Assets/Resources/Audio/Skills/ScytheSlash.wav、ScytheSpin.wav。未覆盖Jump_Skill.wav；回旋施法开头的旧调用已移除，Boss_Killer继续原绑定。技能测试场景与正式战斗共用Player和正式AOE Prefab，无单独测试音频逻辑。
SkillTimedAudio使用独立2D声源，避免SoundManager共享pitch影响旋转，跟随音效音量和游戏时间。试听源的增益调整用于避免源MP3解码超峰，不变调拉伸。
验证工具Assets/Editor/Validation/ScytheAudioValidation.cs；日志output/scythe-skill-check/runtime.txt。无图形批处理验证触发/时间/销毁，听感由用户在技能测试场景验收。
