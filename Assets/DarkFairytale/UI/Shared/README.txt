Shared bottom navigation

Edit StorybookNavigation.prefab to adjust all five main pages: equipment, skills, shelter, map and shop.
Each page contains a nested prefab instance and only supplies pageRoot and selectedIndex (0 through 4).
Authored buttons, icons, captions, bookmark borders and badge remain editable UGUI nodes. Runtime does not create them.
Layout: 1080x1920 reference, bottom inset 39.022316, height 218.06589, supported portrait range 1:2 to 3:4, respects Screen.safeArea. Content panels reserve its height.
Audio: shared onClick handler, one CLICK per frame; existing route guards and transitions are retained.
Selected and normal font/colour, bookmark visibility and badge count are data-bound. Disabled art uses whole-button grayscale and press uses ShelterButtonFeedback.
Original home artwork is reused; frame uses the same approved reference attribute border to avoid baked center-bookmark remnants. Map silhouette and red-paper removal are material masks, not newly generated artwork.

Validation: 15 Unity screenshots (five pages at 1080x1920, 768x1024, 720x1440), pixel-aligned menu bounds across pages, all five button raycasts, selected state, navigation cycle and one click-sound invocation per transition passed. Local screenshots and logs: output/shared-navigation/.
Authoring is explicit only: Tools/Dark Fairytale/UI/Unify Bottom Navigation (Explicit). Never auto-rebuild on import or play.
