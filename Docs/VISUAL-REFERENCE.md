# AO visual comparison — 2026-09-08

This is evidence for individual model corrections, not acceptance of the entire world or visual target.

## Grid access terminal

Reference: [AO Universe, The Grid](https://www.ao-universe.com/guides/classic-ao/daily-missions-2/the-grid), illustration [grid entrance](https://www.ao-universe.com/user/upload/knowledge/497/2_grid_entrance.jpg), inspected directly. The guide identifies the pictured object as a grid terminal. Its exact location and client version are not established by the image.

Observed features: narrow vertical housing, flared dark foot, recessed dark monitor near the top, blue illuminated top cap, rectangular sign cantilevered to the left reading GRID ACCESS, segmented service panels. The old local model was a large cyan pyramid on a round plinth; these defining shapes did not match the reference.

The Blender source now reproduces that observed silhouette with separately authored geometry: monitor/bezel, service collar, panel seams, fasteners, blue cap and modeled lettering. Approximate height is 2.78 Unity metres. Dimensions and rear details are estimates, not extracted AO geometry. Existing cyan/graphite materials follow the supplied concept image. Source textures and image pixels are not bundled in the model or build.

Artifacts: `Art/Blender/DistrictKit.blend`, GridTerminal collection; `Unity/Assets/Art/Models/GridTerminal.fbx`; `Artifacts/grid-terminal-detail.png` from the actual Windows player. The terminal still has no Grid travel interaction; visual correction does not establish gameplay functionality.

## Borealis version conflicts discovered

The [Borealis reference](https://wiki.aodb.us/wiki/Borealis) lists Newland City and Stret West Bank Whompah connections, and explicitly marks the Jobe connection discontinued. The local destination sign has been corrected to Stret West Bank; no working route or original map positioning is implied.

The same page warns that parts of its historical narrative are outdated. Its [Satellite Dish page](https://wiki.aodb.us/wiki/Satellite_Dish) describes the dish as destroyed and replaced by a memorial. The existing intact dish is therefore a historical model, not verified current Borealis content. Its inclusion needs to follow the final chosen reference version. Do not silently use this mixed blockout as a live city reconstruction.

## Remaining visual target

The city layout, terrain, buildings, Whompah geometry, character anatomy/rig/animations, textures, vegetation and lighting still need measured comparisons. The user concept informs modernization; its slogans and pictured numeric stats do not define gameplay rules. The full 1:1 AO gameplay and visually recognizable world objective remains open.

## Individual mission terminal

Reference: [AO Universe, How to pull a mission](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/how-to-pull-a-mission), [terminal photograph](https://www.ao-universe.com/user/upload/knowledge/386/mission_terminals.jpg), inspected directly and retained under `Research/pages/mission-terminals-reference.jpg`. The left booth has a single-person indicator, narrow pedestal, vertical dark screen with cyan lettering, projecting hood and pale M identifier above it. Image location, scale and exact client version remain unknown.

Replaced the broad glowing prototype console with independently authored Blender geometry following those features. Estimated dimensions and unseen rear details are not source measurements. Removed the overlapping floating mission label; identification is now part of the model. The terminal retains the existing local training interaction, not original mission generation.

Rebuilt `Builds/HotbarPreview/AnarchyReborn.exe`; Unity generation/import validation passes 259 checks. Inspected the actual player's district and close-up captures, corrected an initially occluded person indicator and recaptured. Final close-up: `Builds/Artifacts/SceneArt/mission-terminal.png` (1600x900, HUD excluded). This art-only capture run does not rerun the full gameplay suite.
