2026-10-17：用户要求 Boss1、Boss2 前 2.5 秒裁掉。
原始运行 MP3 保存在本目录；正式 Assets/Audio/Music/Boss1.mp3 与 Boss2.mp3 用 FFmpeg atrim=start=2.5,asetpts=PTS-STARTPTS 精确裁剪后以 libmp3lame q=2 编码。不调整音量、速度与尾部，保留正式 meta GUID。
