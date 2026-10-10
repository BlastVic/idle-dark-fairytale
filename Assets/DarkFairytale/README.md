# Red Hood battle integration

`Assets/Prefab/Player.prefab` uses `RedHoodBattle_SkeletonData.asset` and the existing
`Assets/RedHoodPrototype` atlas. The gameplay root, Actor_Base, Player controller,
damage anchor and sorting layer are retained. The smaller source rig is scaled to
fit the battle actor. The shared player also appears in camp and inventory.

`RedHoodBattle.json` is a production copy of the prototype's animation data:

- Skill4, Skill5 and Skill7 reuse Skill1's scythe action, with their original names
  so Player's single-target, area and boss damage rules still receive the right event.
- Death1 has an OnComplete event at 1.3 seconds for the resurrection path.
- The prototype's original animations and test scene are unchanged.

The Player prefab disables `useEquipmentAppearance`: equipment stats remain active,
but knight helmets, shields and armor are not attached to Red Hood's different rig.
`RedHoodWhite.mat` uses her atlas and straight alpha for damage flashes.

V4 motion sample: Idle1 and Attack1 now use the refined 28-bone rig. Cape and
skirt have three additional controls each; the skirt is a weighted mesh. The
attack retains OnHit at 0.56 s and OnComplete at 1.18 s. Other animation timelines
and the death completion event are unchanged. See the prototype README for the
explicit refinement script and Unity motion review menu. Local captures are in
`output/red-hood-motion/` and are not committed.

After updating the prototype animation, copy its animation data into this battle
JSON and reapply the three aliases and death event above. Run
**Tools > Dark Fairytale > Validate Player** to check the prefab, animation events,
costume and flash materials. Reports and the render are written under
`output/dark-fairytale-battle/`. **Open Gameplay** opens the existing game scene.
