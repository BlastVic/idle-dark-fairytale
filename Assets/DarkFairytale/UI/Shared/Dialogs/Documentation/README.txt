Common dialog chrome — 2026-10-17

User-approved change: common informational/confirmation dialogs share the tutorial paper frame and the equipment page's upper-left return bookmark.
Persistent nested prefab: Prefabs/CommonDialogChrome.prefab. All images and return controls remain editable in Inspector. Runtime only forwards the return action and mirrors its enabled state.
Consumers: prologue skip, chapter 1 ending skip, shelter generic details (including locked rebirth), equipment decomposition confirmation, rebirth confirmation.
Return invokes the existing cancel action; it never confirms skipping, decomposition or rebirth. Existing bottom confirmation/cancel buttons remain.

Art provenance:
Textures/GuideDialogFrame.png: 96x96 compact nine-slice extracted from Tutorial/GuidePanel.png; corners retained, edges tiled, empty centre. Border 32 pixels.
Textures/GuideDialogPaper.png: 64x64 clean centre-paper sample from the same panel. Tiled interior.
Back: existing Equipment prefab's actual return-button hierarchy and production sprite, shared without a copied texture.
No new design sheet is referenced. No runtime-generated hierarchy. No import-time automatic replacement.

Explicit authoring: Tools > Dark Fairytale > Unify Common Dialogs. Run only when intentionally applying the migration. Existing page builders also apply shared chrome when explicitly invoked.
Unity evidence (ignored local files): output/common-dialogs. Validation checks 9:16 / 3:4 / 1:2 bounds, return hit testing and cancellation without performing destructive actions. Player saves disabled for validation.
Build/package-size validation and physical-device validation are not performed by this change.
