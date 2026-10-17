# Wolf pets — 2026-10-17

- PetShadowWolf / 影狼: approved A-shadowwatch-v1 design, opaque black/violet fur and pale underside.
- PetGhostWolf / 幽灵狼: approved ghost-wolf-v3-deep-blue, blue spectral parts with real alpha. Forest is not baked into the body.
- Runtime resources: each `Assets/DarkFairytale/Companions/Pet*Wolf/Spine` and `Prefabs`.
- Source, extracted parts, painting prompts and deterministic exporter: `Assets/ArtSources/Companions/ShadowWolf/Production`.
- Unity authoring: Tools > Dark Fairytale > Companions > Build Wolf Pets. Explicit invocation only; existing prefab Inspector edits preserved.
- Pet tag, GENERAL database, Addressables authoring keys and Resources/LocalPrefabs runtime indexes: `PetShadowWolf`, `PetGhostWolf`. Spawn with `LevelController.Instance.AssetManager.SpawnGeneralObj(key)`. Both are independent pets; the existing book dragon skill has not been replaced. Pet selection/unlock UI is not introduced by this delivery.
- Shared WolfPetCompanion: Attack1 single OnHit applies pet damage with red damage numbers and normal target impact, Idle1 between attacks, Run_Battle_00 on exit. DamageMultiplier defaults to 1; balance has not been approved.
- Exact current MobHexmaw JSON copied, SHA256 recorded per source folder. 66 bones, 25 slots, 10 original clips + 4 game aliases. No rig, UV, deform, timeline or event changes.
- Atlas 2048×1024 RGBA, original-resolution full region canvases, 4px transparent gutters; straight-alpha Spine material, no mipmaps. This is a runtime export, not a new editable native .spine project. Native project reassembly remains outstanding.
- Original low crouching pose and tail mesh silhouette deliberately retained to preserve the requested rig. Large free-floating wisps and high curled concept tail are not additional animated geometry.
- MonsterAnimationTest includes authored cards, portraits, prefab scale and left-side companion positioning. Preview uses the pet's size settings, not animation-specific fitting.
- Full-motion finite-geometry and linked-skin deformation checks, single hit/completion events, hit/death interruption: passed for both exports. Evidence is local `output/monster-animation/PetShadowWolf` and `PetGhostWolf`.
- Multi-aspect preview and runtime checks: local `output/wolf-pets`. Screenshots must not be committed.

## Acceptance result

Both pets passed actual GENERAL/LocalPrefabLoader spawning, damage reducing live enemy HP, OnHit observation and animated exit/destruction. 9:16, 3:4 and 1:2 combat screenshots inspected. Preview all 14 clips per pet retain fixed scale; clipped tail in first pass corrected by center x=.28. Idle, jump/bite and closed-eye death renders inspected. Victory return cleanup and player-death exit were also verified. Multi-wave progression and an APK build have not been run; shared Pet-tag BattleCleanup is used. New native .spine projects remain unassembled. No claim of pixel-identical concept silhouette or approved damage balancing.

Final screenshot pass re-applies normal player/enemy layout at each aspect ratio and waits 3 seconds for the original entrance animation before capture. This avoids comparing a newly fitted pet with a player still scaled for the previous resolution. Runtime/export data unchanged during this capture.

## Size adjustment — 2026-10-17

User requested Shadow Wolf, Ghost Wolf and MobHexmaw at 90% of their preceding size, and MobRivenfang at 110%. Both pet viewportSize values changed from (.48,.16) to (.432,.144); new-component defaults match. MobHexmaw displaySizeMultiplier: 1.95 → 1.755. MobRivenfang: 1.1 → 1.21. Positions and rigs unchanged. MonsterAnimationTest reads these same prefab parameters. Before/after Unity scale ratios measured exactly .9/.9/.9/1.1; all selected animation clips retain fixed scale. Actual Unity preview renders for 9:16, 3:4, 1:2 stored in ignored output/wolf-pets/sizes-before-* and sizes-after-*; summary size-review-after.jpg. This adjustment was checked in the test scene; a new live battle pass was not run.

## Shadow Wolf skill integration — 2026-10-17

PetShadowWolf is now summoned by skill slot 2 (召唤：影狼, replacing Path Cleaner). Its persisted `damageSkill` reads the new skill multiplier; PetGhostWolf remains independent. Shadow Wolf uses viewport feet (.28,.245) with the existing (.432,.144) display size, permitting simultaneous dragon/wolf combat. Full skill and lifecycle acceptance is recorded in `Docs/Production/Skills/SummonShadowWolf.md`; actual Unity screenshots remain local in `output/shadow-wolf-skill/`. The standalone wolf combat probe explicitly sets the Shadow Wolf skill level before manual spawning.
