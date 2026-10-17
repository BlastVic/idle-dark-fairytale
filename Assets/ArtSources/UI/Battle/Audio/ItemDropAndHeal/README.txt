2026-10-17 用户提供并指定：第一个物品掉落，第二个治疗。
ItemDrop.mp3 → Assets/Audio/Sounds/Drop1.wav → ITEM_DROP
Heal.mp3 → Assets/Audio/Sounds/Heal.wav → HEAL
正式 Gameplay 和 SkillTest 场景分别绑定。保留完整时长与原速度；转换为 44.1kHz 立体声 PCM16，峰值超过 0.95 时仅整体衰减防削波，关闭导入归一化。
[
  {
    "asset": "Drop1.wav",
    "duration": 1.0,
    "source_peak": 0.3696160912513733,
    "gain": 1.0
  },
  {
    "asset": "Heal.wav",
    "duration": 2.0,
    "source_peak": 0.37242963910102844,
    "gain": 1.0
  }
]
