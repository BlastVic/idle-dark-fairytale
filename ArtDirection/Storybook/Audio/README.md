# 开场音效

2026-10-12。用户授权代选和继续制作，采用原试听第二版（2 秒）作为纸张素材，裁切 0.32–1.14 秒并编排为八层，起点为 2.12、2.34、2.56、2.78、3.00、3.22、3.44、3.66 秒；每层 0.82 秒，与当前 Spine 一致。不是把慢速单页整段加速。

ElevenLabs Sound Effects v2 原始 MP3 保留于本目录；生成 ID、提示词见 sources.json。封皮和氛围各生成四个变体，本版取各自第一版作为基础。另两层为同源纸纹/氛围素材的淡入淡出、反转和叠加，不是额外的音乐或配音。

`mix_intro.py` 用 numpy 重建成品；先用 macOS afconvert 将本目录 MP3 转为 `output/storybook-audio/source/{page,cover,ambience}.wav`（page 对应 page-take-2）。从仓库根目录运行脚本。正常时长 9.8 秒，原始采样率 44100 Hz，立体声 PCM WAV，峰值低于 0 dBFS。

CursedBookIntro Prefab 持久保存 Book Foley 和 Book Atmosphere 两个 AudioSource。CursedBookIntroAudio 跟随视觉 Elapsed 时钟，加载等待暂停实体音效、继续环境底声，结束或禁用时停止两条音轨并恢复原 BGM mute 状态。沿用 SFX 音量；开场期间暂时静音营地 BGM。音量可在 Inspector 调整。

音频安装工具只在显式菜单或 install.request 下运行，不自动重建 UI。未修改视觉节点、布局或动画。试听及验证输出只在忽略的 output/storybook-audio 下。
