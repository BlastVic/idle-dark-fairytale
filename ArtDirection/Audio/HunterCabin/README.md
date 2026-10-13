# 猎人小屋主界面环境音 · 安静版 v3

根据实际试听反馈修订：旧版持续燃烧声过大，低频轰鸣像烧水。新版完全移除持续火焰层与木梁响动，以接近安静的室内空气感为底，只在少数位置点缀轻微柴火响声。

- 输出：`Assets/Audio/HunterCabin/HunterCabinLoop.wav`，44 秒、44.1 kHz 立体声 PCM16，保留原 GUID 与场景绑定。
- 火声：只在 7.4、23.8、38.2 秒出现，每次约 0.16 秒，相邻间隔约 13–16 秒（含跨循环）。从既有 ElevenLabs 素材中截取高频瞬态，滤去 900 Hz 以下轰鸣，短促淡入与衰减收尾。没有持续燃烧、气流或沸腾声层。
- 室外/室内底音：RMS 约 -59 dBFS，滤去低频，200–550 Hz 范围保留很轻的隔墙空气感。
- 柴火点缀比 v2 单独提高约 12 dB（振幅 4 倍），底音和出现频率不变。混音峰值约 -23.75 dBFS；不做响度归一化，保留安静设计。技术数据见 audio-validation.json。
- 44 秒循环首尾连续，无整段淡出静音；游戏沿用 MusicManager 营地音轨的切换与背景音量设置。
- 接入 Gameplay 的 MusicManager.audioClips[0]；战斗曲仍为 Darkest Child var A；开场期间由 CursedBookIntroAudio 静音。
- 试听：`output/hunter-cabin/quiet-cabin-v3.wav`，连播两轮。旧版仅备份到忽略目录 output/hunter-cabin/previous-loud-loop.wav。

源素材和生成信息见 sources.json，build_loop.py 可复现。本次只重新剪辑、滤波和混音，没有重新生成付费素材。原 import-validation.txt 和 runtime-validation.txt 是首次接入记录；新版复用同一 AudioClip 绑定。
