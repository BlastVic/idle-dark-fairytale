更正：旧版错误地将DongzhuoCast施法闪光0.4667秒当作完整回旋技能。
完整链：Player Skill5 OnHit(动画0.56秒)同时生成CursedSweepCast和AOE_Particle。后者由LevelDataDescriptions绑定AOE Particles.prefab，使用ChengpuSweep/Sweep.json，startTime=0.2333，duration=1.2667，动画末尾1.5秒清空残留贴片。最后1.3至1.5秒的余效淡出对应生成后1.0667至1.2667秒。对象在生成后1.3667秒销毁。
正常攻速：0.46劈砍，0.56生成，1.8267秒视觉消失；从劈砍开始计共1.3667秒。两倍攻速：0.23劈砍，0.28生成，1.5467秒消失。动作1.18秒结束不代表回旋特效结束。上述为游戏时间，异步实例化可能产生帧级延迟。
修正试听输出output/scythe-sweep-audio/corrected/。仅准备音频，未改运行代码。正式接入需要分离角色动作事件与实际特效实例起播，不可将整轨一起按攻速缩放。尚未Unity运行时同步验收。
