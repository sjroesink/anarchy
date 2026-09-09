# Current priorities

User steering, 2026-09-08: prioritize the general look and feel and gameplay first. The full goal remains recognizable Anarchy Online visuals and matching AO skills, stats, items and rules. Separate client/server and anti-cheat are still required, but further account/security expansion follows the playable visual/gameplay work.

Immediate iteration order:

1. Make the existing district and Soldier feel convincing in actual gameplay: character silhouette, weapon handling, camera, movement, lighting, sky and readable combat feedback. Author model changes in Blender. Compare real player captures with the supplied mood reference and actual AO visual references.
2. Improve the playable explore/target/fight/recover loop and mission interaction. Keep AO data and documented rules as the basis; record incomplete behavior explicitly. Detailed client-clock reverse engineering should not hold up independent visual and gameplay improvements.
3. Improve environment composition and material detail: reduce the repeated blockout appearance, make AO landmarks readable, and add deliberate foreground/midground/background composition.
4. Resume authoritative gameplay migration, account/reconnect work and broader anti-cheat controls as these gameplay systems settle.

The existing development skill server remains working. Unfinished account code is preserved as noncompiled drafts under `Server/Drafts`; it is not a delivered account system. The offline look/feel preview is not presented as secure multiplayer.

## Next reviewable milestone

Deliver a short playable Borealis slice: walk through a recognizable street, select a target, fight, and use a mission terminal. Review the street at player height, with the HUD visible, as well as character movement and weapon handling. Keep the Blender sources editable.

The separate `BorealisReconstruction` scene now contains source-derived terrain and placement anchors, but is still a diagnostic study. Its coloured ground categories and building mask are not finished environment art. Promote reconstruction work into the playable scene only after its scale, navigation and visual presentation have been checked in play.

Source-format investigation supports this milestone when it resolves a concrete placement or gameplay question. It must not become a prerequisite for independent improvements to the playable slice. Judge progress primarily by visible and playable changes; extractor coverage alone does not deliver this milestone.

Retain separate client/server architecture as the intended foundation. Schedule further account and anti-cheat expansion after this slice; the current local preview does not establish server authority or cheat resistance.
