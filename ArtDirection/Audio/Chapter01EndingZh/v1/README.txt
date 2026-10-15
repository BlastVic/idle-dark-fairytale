第一章结束 · 中文纯旁白 v1（审听版）

声线：Serena - Calm & Soothing Mandarin，QAe8SYEl8U8qnxlfuLRz。
模型：eleven_v4。沿用序章 page06-v3 的最终声线与低稳起声方式。
台词逐字保留，标题不朗读；第二幅留言为中文“是你的话，一定可以。”。
每页单独生成，加入克制的表演及停顿标记；没有音乐、环境音、翻页声。
服务默认每页生成四个候选，全部 20 份原始 MP3 留存 raw/。
第 1–4 页选候选 1；第 5 页选候选 3：独立转写中的“炉边”“那座村庄”等更吻合原稿。
生成 ID、原始哈希、处理参数见 processing-report.json；原始提示词及会话记录见 generation-config.json 和 runs.json。

交付：
page01–page05.wav：44.1 kHz / 16 bit / 单声道 PCM。
page01–page05.mp3：192 kbps 试听。
每页 SRT 和 timings.json：保留批准原文的逐句字幕与时间。
chapter01-ending-full.wav / .mp3 / .srt：按五页顺序串联的纯旁白。

后期：不裁切源音频、不拼接句中语音、不整体变速。两遍 loudnorm，目标 -20 LUFS、true peak -3 dBTP；每页前加 0.35 秒、后加 0.8 秒余量，保留原始内部停顿。
没有以门限强切弱字头和尾音，没有把环境层烘焙进去。尚未接入 Unity。

校对范围和限制：
供应商转写包含表演标签，因此不能当成独立语音识别证明；原始响应留档，最终字幕采用本地 faster-whisper small 独立识别后与原稿对齐。
独立识别五页分别为 87、79、119、126、104 个汉字，与原稿对应；对齐未发现插入或删除，剩余替换为代词、同音字或异体字。对比明细见 transcription-comparison.json。
这些是自动校对结果，不代表逐字人工听审或情感表现已获确认。
字幕边界未经人工逐帧校准，建议留出约 ±0.5 秒复核窗口；个别停顿或弱音误差可能更大，±0.5 秒不是实测精度保证。
已验证 WAV 无削波，首尾余量存在。请以本版试听确认音色、停顿、起声和收尾。

复现：prepare.py 生成交付音频；check_transcription.py 独立转写；align_subtitles.py 输出原文字幕。
工具依赖位于项目忽略目录 output/chapter01-ending-tools 与 output/prologue-film/tools。
