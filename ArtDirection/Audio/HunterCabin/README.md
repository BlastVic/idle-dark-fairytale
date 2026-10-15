# 猎人小屋主界面环境音 · 用户柴火素材版 v4

输出：`Assets/Audio/HunterCabin/HunterCabinLoop.wav`，44秒、44.1kHz、立体声PCM16。保留原GUID和Gameplay MusicManager.audioClips[0]绑定，小屋主界面沿用循环播放。

以用户提供的2秒柴火录音替换三处旧瞬态，分别在7.4、23.8、38.2秒完整播放一次，保留原始节奏；仅做5毫秒淡入、60毫秒淡出与音量调整。三处峰值目标仍为0.072、0.056、0.064，左侧略近。室内底音与其余区间逐样本保持不变，循环首尾亦保持不变。没有加入持续燃烧层。

源文件：`source/user-firewood.mp3`，用户于2026-10-15提供的 ElevenLabs_audio_elevenlabs-sound-effects-v2_Firewood burnin_2026-10-15T14_12_19.mp3。旧环境源记录见sources.json。build_loop.py可重建，依赖output/hunter-cabin/source下的原始室内风声素材及output/prologue-film/tools中的ffmpeg。

技术数据见audio-validation.json，验证范围见replacement-validation.txt。原import-validation.txt及runtime-validation.txt为历史验证，本次未重新做Unity运行时试听。

双循环试听：`output/hunter-cabin/quiet-cabin-v4.wav`。替换前资源备份：`output/hunter-cabin/before-user-firewood.wav`。
