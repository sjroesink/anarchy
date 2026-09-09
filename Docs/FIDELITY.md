# AO fidelity — active goal, not complete

## Camera views

F8 now switches between first-person and the current third-person camera, preserving its requested zoom. Basis: the Funcom-authored [Getting Started on Rubi-Ka manual, page 17](https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf), and original client `cd_image/gui/Default/ActionMenu/CameraMenu.xml`. The first-person camera follows a provisional 1.8-unit eye anchor; that height is authored for the current Soldier, not a recovered breed scale.

The camera excludes the local player's render layer in first-person and when scenery pushes the third-person camera within 0.9 units of the upper body. This prevents the model from covering the view without changing equipment activation or character simulation. The proximity threshold is a presentation choice. Original client options (`OptionPanel/Root.xml`) expose showing one's character in first-person and zooming into first-person; these preferences, Ctrl+F8 camera variants, independent Ctrl+right-button look and original camera timing are not yet implemented. Mouse wheel in first-person currently leaves the remembered third-person zoom intact.

## Mission terminal pointer interaction

The playable booth now accepts a right click, matching the interaction described by [AO Universe's mission guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/how-to-pull-a-mission). A press and release must resolve to the nearby visible booth. Moving more than five screen pixels cancels use so camera dragging does not open the mission window. HUD, modal, text-entry, pause, focus loss, scenery occlusion and leaving range are covered by runtime fixtures. The five-pixel gesture threshold and existing four-unit proximity limit are prototype choices, not recovered AO constants. E remains a convenience shortcut.

The scene builder adds estimated colliders for the authored terminal's base, housing and sign. The original mission generator, costs, sliders, alignment checks and team mission distribution are still missing; right-click access does not establish their implementation.

## Official client reference acquired

The client-item decoder subsequently read 120,569 stat tables and structurally consumed 108,078 complete records; remaining suffixes and opaque blocks are explicitly retained. All seven reviewed records have complete structural parses. A 66-field comparison against Unity's reviewed definitions passes, covering selected exact-QL equipment and rifle fields. This does not establish complete action semantics or matching patch versions. Coverage, unresolved layouts and reproducible commands are in [CLIENT-REFERENCE.md](CLIENT-REFERENCE.md).

The official downloadable installer yielded client **18.8.50_EP1**. The resource database is now available locally under ignored Research for read-only reference. All 459,308 index identities matched their record headers and their payloads passed segment-boundary reads; all seven reviewed item IDs were located. See [CLIENT-REFERENCE.md](CLIENT-REFERENCE.md) for provenance, hashes, reproducible extraction and audit commands. This older snapshot has not been patched or semantically imported, and does not establish current live-version parity.

## Full acceptance target

The user requires a visually recognisable Anarchy Online remake in Unity, with Blender models and 1:1 gameplay: the same skills, stats, items and associated systems. A modern AO-inspired game with simplified progression does **not** satisfy this target. A catalogue of item names does **not** constitute implemented items. Passing internal tests does **not** establish equivalence to the original game.

Reference version: provisionally the live game including expansions, pending the user's version choice. Published sources currently mixed: Nadybot item index **18.08.58.01**, weapon table **18.08.58.00**, and manually inspected AO Galaxy items **18.8.62**. No single verified target client snapshot is available in the workspace. Preserve the differences; do not quietly describe the combined dataset as a complete live database.

## Current evidence and remaining work

| Requirement | Current implementation/evidence | What still prevents completion |
|---|---|---|
| Recognisable AO world | Blender landmark kit; GridTerminal rebuilt against an inspected in-game image (see VISUAL-REFERENCE.md); original supplied concept retained | Individual Grid terminal silhouette corrected, dimensions still estimated. Actual Borealis map, streets, terrain, faction architecture, vegetation and textures unverified; intact dish is historical, Jobe route removed from label |
| Modern visual presentation | Unity lighting, fog, cyan/amber palette and orbit camera | Character rig, animation, materials, VFX, audio, LOD and render quality remain preliminary |
| Same stat identities | 626 mapped numeric stat IDs, source commit recorded | Not all stats have runtime semantics, defaults, modifier ordering or serialization |
| Same skills and abilities | 6 abilities + 67 active skill entries + 2 historical identities; full grouped skills UI, numeric investments, weighted trickle-down | Live fixtures for all dependencies and rounding; profession cost table currently historical CellAO; breed maxima, ability-dependent caps, SL caps and resets incomplete |
| Professions / breeds | All 14 profession and 4 breed identities and cost columns imported | Only Soldier is playable. Character creation and the other 13 profession mechanics absent |
| Same item identities and QL | 33,836 searchable index records preserve low/high AOID, low/high QL, flags and slots | Most records lack requirements, modifier trees, actions, attack/defence skills, interpolation and acquisition. They cannot be equipped from the index |
| Real starter weapon | Solar-Powered Assault Rifle AOID 121569, QL1; 3–24 (18), 1s/1.5s, 20m, Assault Rifle vs Dodge-Rng | Complete hit/crit/AC, critical bonus application, initiative behaviour above thresholds, equip sequencing and animations not verified |
| Nanos | 3,375 metadata records with names, IDs, requirements, cost and nanoline; two self-cast paths for Assault Rifle Expertise and TMS Mk I; timed effect modifiers feed shared stats and NCU; same-program refresh and explicit cancellation | Acquisition/upload, full nanoline replacement rules, interrupts/resists, buffs on other targets, cooldown variants and nearly all other nanos unimplemented. TMS Heal Reactivity, root/snare resistance and version-specific lockout remain missing; full-NCU refresh and cancellation permissions need client fixtures |
| NCU | Baseline 8; exact QL1 belt/deck requirement and capacity modifiers pass local fixtures; active effects drive actual NCU occupancy and HUD | Belt/module equip timings sourced and runtime-tested; acquisition unavailable; full buff occupancy/replacement conformance absent |
| Implants / symbiants / twinking | Fictional implant and QL30 carbine removed | Actual implant cluster/slot/requirement data, construction, equipment transactions, ability buffs and OE tiers must be connected |
| Combat / specials / pets | Solar rifle uses source damage/range/timing; normal PvM damage now subtracts typed AC after AR scaling, respects scaled minimum and adds typed damage bonuses; fictional heal removed; Burst capability corrected from raw Can flags but execution pending | Current hit checks remain approximations; AR above 1000 is explicitly unsupported rather than extrapolated; integer rounding and MBS still require fixtures; dual-wield, all specials, perk combat, pets, threat, crowd control, damage types and healing require implementation |
| Progression | AO XP/SK threshold reference rows imported; RK XP thresholds used; provisional IP grant table | Actual kill/reward formulas, death/insurance, SK/faction, Alien XP, research, perks, resets, caps and level220 behaviour incomplete |
| Missions / quests | Previous local drone mission retained only as a **training fixture** | It is not AO mission generation: mission types/sliders, location selection, indoor modules, locks, rewards, tokens, timers and quests incomplete |
| Inventory / equipment / loot | Owned item instances, seven exact item definitions across QL1/20/200, including body armor, atomic slot commit, starter equip delay/visuals and v3 persistence; QL1 belt/deck requirements and six-slot QL200 NCU setup tested; inventory paging and explicit NCU-slot selection | Exact QL1 belt and memory equip/unequip timings connected; acquisition absent. Containers, stacks, nodrop/unique enforcement, remaining slots/actions, interpolation, tradeskills and loot rights incomplete |
| World travel | AO landmark models visible | Whompah routes and destinations, Grid instances/skill checks, zoning and maps not implemented |
| Social / economy / PvP | No implementation | Teams, raids, organizations, factions, gas mechanics, towers, trading, crafting, shops, market, housing and social systems remain in full scope |
| MMO/server behaviour | Offline Unity simulation and local character JSON | Server authority, persistence, duplication resistance, networking, recovery and load tests absent |

## Source decisions

- Numeric identities: [AOSharp Stat.cs](https://github.com/anarchydevs/aosharp/blob/0a80bac8d3e743cea021e7864ec18f758cd6a337/AOSharp.Common/GameData/Stat.cs). These are community mappings, not Funcom server source.
- Modern skill groupings and ability weights: [Nadybot trickle.csv](https://github.com/Nadybot/Nadybot/blob/de9e3b2c8d2f91df87c614a3d9f91bc16c2eacf2/src/Modules/TRICKLE_MODULE/trickle.csv). The old CellAO dependency table was rejected where it disagrees: e.g. MM erroneously refers to Agility in that table. No claim that Nadybot itself proves every live rule.
- Profession cost and breed baselines: [CellAO SkillUpdate.cs](https://github.com/CellAO/CellAO-NightPredator/blob/ca77f375a7dabe3be769da94e6c2a3d093344c2c/CellAO/Libraries/Source/CellAO.Stats/SkillUpdate.cs). Imported as provisional factual reference, not as authentic Funcom internals.
- Item index, nanos and XP thresholds: versioned Nadybot CSVs. Numeric facts are transformed by `Tools/import_ao_reference.py`. Source attribution/licenses accompany the data in `Unity/Assets/StreamingAssets/ThirdParty` and are bundled in the build.
- [Solar-Powered Assault Rifle](https://www.aogalaxy.com/_items/item.php?aoid=121569), [Assault Rifle Expertise](https://www.aogalaxy.com/_items/item.php?aoid=26370), [Total Mirror Shield Mk I](https://www.aogalaxy.com/_items/item.php?aoid=70308): direct published item records. Current implementation covers only the explicitly connected subset of their actions.
- Whompah / Grid visual direction: *Getting Started on Rubi-Ka*, pages 52 and 56, and the supplied user image. These inform a new Blender blockout; they are not measured reproduction evidence.

- Equipment fixtures: [Ti-100X QL1](https://www.aogalaxy.com/_items/item.php?aoid=36783) requires Computer Literacy 6 and grants one belt deck; [2–3 NCU Memory QL1](https://www.aogalaxy.com/_items/item.php?aoid=36779) requires Computer Literacy 6 and grants 2 NCU. Exact QL1 only. AO Galaxy does not expose their equip delay. AO Index raw stat 211 now supplies it: belt 1000 centiseconds (10 seconds), memory 100 centiseconds (1 second). Runtime equipment is enabled for owned instances. Acquisition is still absent. Solar rifle equip delay is 1000 ms; applying it to unequip still needs original-client comparison.

`self-effects.json` records the two supported self-effect subsets and their omitted actions explicitly. Expertise now modifies stat 116 itself, so the skill panel, attack rating and equipment requirement evaluator consume the same value. IP costs and permanent investments remain independent of timed effects. TMS publishes the eight sourced reflect/max-reflect pairs; the local drone uses projectile damage. All remaining damage types still need combat execution fixtures.

The importer records repository commits and SHA-256 hashes in `Unity/Assets/Resources/AO/provenance.json`. Local clones in `Research/` are ignored by source control; pinned facts in Unity Resources remain reproducible after cloning those commits.

## Verification and next work

`Artifacts/ao-reference-validation.txt` tests imported identifiers, dependency totals, selected source fixtures, profession cost mapping, XP boundaries and save schema. `Artifacts/inventory-validation.txt` tests executable-item and ownership invariants, including equipment requirements, modifiers and v2 migration. `Artifacts/damage-validation.txt` covers normal PvM damage for AR 0..1000 and the corrected Burst flag; it excludes critical/hit chance, MBS, OE and rounding conformance. `Artifacts/effects-validation.txt` tests 24 modifier, NCU, refresh, expiry and transient-save invariants. `Artifacts/runtime-validation.txt` tests a running local player and training mission, including nano preconditions and timing. These reports are not evidence of full 1:1 behaviour. Nano runtime captures use artificial skill investments above level-one training caps; they verify execution paths, not acquisition or progression.

Next actions toward the unchanged full goal:

1. Select and fingerprint the exact AO reference build; locate a complete, version-matched item/nano database and establish character/stat fixtures from that client.
2. Extend the initial exact-QL item format to complete requirements/actions/modifiers and verified QL interpolation. Extend starter inventory and persistence to acquisition, nanos, implants and all equipment; keep unsupported actions explicit.
3. Record and test original-game fixtures for IP costs/caps, trickle-down, HP/NP, NCU replacement, attack/recharge, specials, OE and XP.
4. Build the first location against actual AO map and screenshots with identifiable landmarks and movement paths. Extend the Blender source kit and character rig while checking each Unity render.
5. Continue the remaining professions, content and MMO systems in the table above. Do not close the goal at a vertical slice or at catalogue completeness.

## Weapon evidence correction, 2026-09-08

The previous assertion that Solar-Powered Assault Rifle has no Burst was wrong. [AO Index AOID 121569](https://www.aoindex.com/item/121569), database 18.08.62, exposes raw Can stat 30 = 3077. Bit 11 is Burst in both pinned AOSharp and CellAO enums. The earlier AO Galaxy presentation omitted the Can list; absence from that page was not evidence of absence. Executable metadata now preserves the flag and Burst identity. Its execution remains pending; the UI describes that implementation gap instead of denying the capability. Raw On Activate functions 53065/53076/53075 also remain to be decoded. AO Index is a promising version-matched source for further item requirements/actions; no bulk completeness claim has been made.

The normal damage implementation follows the [AO Universe Combat Guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/combat-guide): scale the weapon roll, subtract the corresponding AC divided by ten, enforce scaled minimum, then add typed damage. It is restricted to AR 0..1000; critical damage, profession scaling, MBS, hit chance, OE and PvP require further evidence. Internal health retains fractional damage; integer rounding is unverified. Training drones expose explicit AC fixture arrays, initially zero, not invented authentic mob stats. Critical-floor descriptions differ between published guides, so they have not been silently treated as settled.

## Versioned belt and memory evidence

[Ti-100X](https://www.aoindex.com/item/36783) and [2–3 NCU Memory](https://www.aoindex.com/item/36779), version 18.08.62, expose the previously missing equipment timings. Both have Can=5. Their raw equip requirement is stat161, operator2, operand5; the source presents this as Computer Literacy >=6. Preserve the raw operator and operand: interpreting the raw 5 directly as a >= threshold would introduce an off-by-one error.

`Tools/capture_item_reference.py` captures explicitly selected public raw tables and can parse cached HTML with `--html-dir`. `Resources/AO/RawItems` stores the two captured records with source URLs, version and HTML hashes. This preserves unsigned fields, requirement operators and effect functions without assuming executable semantics. Direct Python retrieval returned HTTP403 in this environment; the committed captures were parsed from the already inspected PowerShell downloads. No bulk catalogue import was performed.

Runtime fixtures now check the actual ten-second belt transaction, one-second memory transaction, deck dependency, NCU activation timing and memory removal. They grant owned items only inside isolated QA; the starter inventory has not been changed into a fabricated acquisition path. Applying the published equip time to unequip remains a client-conformance assumption.

## Exact endpoint expansion and unresolved QL interpolation

The request `https://www.aoindex.com/item/36779/ql/19` returned both displayed and raw QL1 values. This is not a QL19 fixture and was not imported as one. Source QL family bounds also differ from the historical Nadybot index, so endpoints must not be interpolated using the old ranges.

Five reviewed utility records now compile reproducibly from raw tables via `Tools/import_utility_items.py`. The new records are [4–7 NCU Memory QL20](https://www.aoindex.com/item/36778), [6K-X QL200](https://www.aoindex.com/item/36787), and [64 NCU Memory QL200](https://www.aoindex.com/item/95520). Their requirements are CL35, CL401 and CL750; effects are +4 NCU, six deck slots and +64 NCU respectively. The QL200 requirements/modifiers were cross-checked against AO Galaxy. These are exact sourced points, not proof of the entire QL range.

The importer accepts only the reviewed static utility shape: equip requirement stat161 with raw operator2, and one-time On Wear Modify actions. Unknown functions/operators fail; modifiers must agree with the raw summary. Six independent QL200 modules produce capacity392 including base8, and the slot state survives save/load in local tests. This does not establish acquisition, complete equipment semantics, original-client parity or full interpolation. `Artifacts/utility-import-validation.txt` records the importer checks.

## Movement interruption and cast feedback

The [ICC Shuttleport nano instructions](https://wiki.aodb.us/wiki/ICC_Shuttleport) describe movement cancelling execution and normal actions being unavailable during a cast. The local player now checks actual displacement before cast completion, including the final update; camera rotation alone does not cancel. Normal weapon damage calls and automatic shots are blocked while execution is pending; equipment changes and casting also reject concurrent execution. A cast-progress bar shows the current nano and time remaining. Death clears the pending cast and its displayed timer.

The displacement threshold is a local 0.02m tolerance against CharacterController settling, not a measured AO threshold. No assertion is made about server ticks, latency, knockback exceptions, combat-interrupt chance, moving instant casts, full action locking or the original weapon/nano queue. Existing policy spends nano only on successful completion; interrupted-resource/recharge semantics still require client fixtures. Nano Initiative and Agg/Def cast-speed modifiers are now connected using the published reference formulas below. The movement test exercises the running CharacterController; native keyboard input is not separately verified.

## Initiative and Agg/Def timing

[AO Universe: Weapon Initiatives and the AggDef Slider](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/weapon-initiatives-and-the-aggdef-slider), updated 2025-05-20, supplies the formulas implemented in AoInitiative. Initiative above 1200 contributes at one-third effectiveness. Weapon timing uses attack/600 and recharge/300; nano cast timing uses initiative/200 and leaves recharge unchanged. The local slider ranges 0..1, with weapon neutral at 0.875 and nano neutral at 0.5. The page includes a measured linear nano slider adjustment; inconsistent arithmetic in its special-cap example was not copied.

Both current nanos use zero as their minimum cast time; all other nano/weapon caps require item-specific data before execution support. Negative initiative behavior and server rounding still need original-client fixtures. An instant cast completes in StartNano and costs/recharges once. Cast duration is sampled at the start; mid-cast slider/stat changes apply to subsequent casts. The slider is saved with character data; older saves without the field receive the previous 0.65 default and malformed values are rejected. Its effect on evasion remains the older approximate combat formula, not verified AO defense behavior.

`Artifacts/initiative-validation.txt` covers 19 formula/save fixtures including the 1200 breakpoint, slider extremes, a published example and explicit caps. Runtime checks verify actual shared NanoCInit stat use and an instant cast with fixed recharge. These remain local tests, not full AO queue or client conformance.

## Body armor and incoming mitigation

[Battered Leather Body Armor AOID 85697](https://www.aoindex.com/item/85697), exact QL1 from AO Index 18.08.62, requires Agility 8 and Sense 8. The raw record supplies 0.1s equip delay, body placement, four AC modifiers of 5 (projectile/melee/cold/radiation) and four of 1 (energy/chemical/fire/poison). These modifiers now enter the shared character stats; incoming training-projectile damage uses player AC before reflection. The equipment panel shows all eight AC values.

Source placement 32 belongs to the clothing inventory and maps to body slot 21, not weapon slot 5. ApplyTexture function 53039 with parameters 8732,1 is preserved in appearanceActions and explicitly marked unimplemented. The character's visible armor therefore does not yet match the equipped leather item. No original texture is included and no cosmetic substitute is claimed faithful. Item acquisition, OE reduction, general damage types, absorbs and full incoming-combat parity remain incomplete. The training drone still uses synthetic base/minimum damage (current difficulty amount/minimum 1); it is not an authentic AO NPC fixture.

## Armor over-equipping

The [AO Universe OE guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/over-equipped) reproduces historical publisher rules and describes exceptions. The reviewed leather armor now receives tiered AC reduction from the weakest required ability; other modifiers and the equipped identity are not removed. The selected local thresholds retain full effect at 80%, then 75/50/25/0% effect at decreasing 20-point bands. Integer AC is truncated per item after applying the percentage. Source wording/examples are inconsistent at some boundaries, so exact threshold inclusivity and rounding remain provisional pending client fixtures.

This implementation is explicitly opted in per reviewed armor definition. It applies only to AC 90..97 and does not claim complete OE for weapon damage, pets, HUD/utilities, HP or initiative modifiers. NCU and belt capacity remain unaffected when Computer Literacy drops. The equipment panel highlights reduced armor and displays its effective percentage. Requirements for initially equipping an item are still checked in full; OE is evaluated after it is worn.

`Artifacts/oe-validation.txt` covers tier boundaries, live ability changes and the NCU exception. Runtime checks connect reduced AC to actual incoming mitigation and restore protection without re-equipping. These tests verify the selected local rules rather than exact original-client behavior.

### Extended client wire layouts

Latest coverage: 120,549 structurally consumed item records, twenty explicit unresolved records, and all 120,569 stat tables decoded. String/hash arguments and shop records retain raw values and opaque data. Nine parser checks and the 66-field comparison pass. See CLIENT-REFERENCE.md and the schema-2 decoding artifact. This does not change the incomplete Unity action, content, visual or target-version requirements.

### Nano client records and casting definitions

All 10,815 nano resource records in client 18.8.50_EP1 now have complete structural parses. The two supported nanos read reviewed costs, base cast/recharge times and requirements from their definitions, corroborated by the client payloads. This does not implement the remaining nanos, conditional functions, VisualProfession disguise, stacking or lockouts. See CLIENT-REFERENCE.md and Artifacts/client-nano-decoding.json.

### Reviewed buff replacement

Assault Rifle Proficiency is now the third supported self buff, explicitly sourced to client 18.8.50_EP1. The nano library can cast the three supported programs. Proficiency and Expertise share line 27; order 3 Expertise replaces order 2 Proficiency, with replaced NCU deducted before checking capacity. Weaker attempts preserve the current buff and resource state. Current tests do not verify equal-priority distinct programs, composite lines or original-server boundary behavior. Learning/acquisition remains incomplete; see CLIENT-REFERENCE.md.

### Complete structural record traversal

The decoder now consumes every item (120,569) and nano (10,815) record in the older client snapshot. Five incorrect legacy function layouts and zero-length string consumption were corrected using client bytes and an independent pinned data model. Fourteen parser tests and 66 selected field comparisons pass. Structural traversal still leaves opaque fields and runtime semantics unimplemented; see CLIENT-REFERENCE.md for corrections and evidence.

### General skill buff catalog

The runtime now has 97 general single-skill buffs plus partial TMS, imported from explicit client records with strict supported-shape checks. Nano IDs flow through the shared cast path, and the library can attempt each compiled record. Eleven remaining family candidates are listed in general-skill-buff-import.json; missing timing values and composite replacement are not fabricated. Learning/acquisition, other targets, the rest of the nano catalog and patch conformity remain incomplete.

### Learned nano programs

Casting now requires a learned program. 98 ordinary crystal definitions have an Upload action using their own requirements, with success-only consumption and persisted learned IDs. Fresh/legacy characters do not automatically know every supported nano. Acquisition, starter crystals, shops/drops and full item-use timing remain unfinished; QA grants are isolated and not saved. See CLIENT-REFERENCE.md for source evidence and limitations.

### Body Boost and startup acquisition evidence

Body Boost 29091 and startup crystal 29092 are implemented from exact client values (+20 MaxHealth, 1 NCU, 11 nano, 3s cast/4s recharge, BM/MC 5, Soldier). The maximum-HP modifier is integrated. Current-HP preservation/clamping remains a local transition rule pending client fixtures. The current Arete source points to Soldier Nanoprogram Container 248265; its spawn hashes/acquisition remain unresolved, so no direct starter grant is claimed. See soldier-startup-evidence.json and CLIENT-REFERENCE.md.

### Composite skill program execution

Seven general +20 composite programs now execute for learned-program characters, bringing the supported self-effect catalogue to 106. The implementation uses the explicit primary and five additional stacking lines, whole-program replacement and atomic NCU accounting. Every skill modifier and expiry is tested. Original-client cross-line conformance remains unverified; crystal conditions and world acquisition remain incomplete, so fresh characters are not automatically granted these nanos. Martial Prowess and other composite families remain outside this implemented set.

### Soldier Blender proportion and armor study — 2026-09-08

Reworked the editable Soldier model from the supplied mood image: smaller helmet, visible neck seal, separate upper/lower limbs, knee protection, layered chest armor, belt pouches, segmented boots and a compact backpack. Added matte Fabric and muted Armor materials consistently in Blender and Unity. Preserved Rifle/Barrel mesh names for inventory visibility. The source remains Art/Blender/DistrictKit.blend and exports through Tools/create_models.py; Tools/render_soldier_study.py produces a separate front review render without saving cameras/lights into the source.

This remains a geometric armor study, not a reproduced AO armor item or a finished human character. Rounded block geometry, lack of skinning/animation, texture detail and exact equipment appearance are still visible limitations. The source image's production quality and the user's recognizable AO world requirement are not achieved by this revision. No item stats or grants were added for the visual plates.

### Shaped Soldier meshes — 2026-09-08

Replaced the Soldier's cube-based torso, pelvis, upper arms, forearms, thighs and calves with closed elliptical cross-section meshes. Added tapered polygonal chest and abdominal armor panels in place of the rectangular plates. Blender source and FBX remain reproducibly generated; weapon names and dimensions/collision setup remain compatible with existing inventory handling. Inspected the front render after export. This improves silhouette construction only: the model remains unrigged, without detailed textures and without exact AO equipment identity. Production character and recognizable AO-world fidelity remain unproven.

### Soldier rig and binding — 2026-09-08

The Blender Soldier now has an authored 17-bone skeleton (hips/spine/chest/neck/head, upper/lower arms/hands, thighs/shins/feet). Sixty meshes and 3,570 vertices have normalized bone weights; torso cloth blends between spine/chest, while armor and other separate parts bind rigidly. Rifle and Barrel bind to the right hand and retain their inventory visibility names. Corrected source-kit arrangement to offset parent roots only so the skeleton does not translate its children twice.

The renderer supports --pose for a disposable bent-arm preview without saving that pose into the source. Blender verifies all vertices have valid normalized weights. Unity import validation checks skinned meshes and non-null bone bindings. The runtime QA additionally rotates the imported forearm, bakes the hand mesh to measure deformation, then restores the bone and compares the rest mesh.

This is a rig foundation, not locomotion/combat animation or an AO animation replica. Fingers, face, detailed joint deformation, texture maps and exact armor appearance remain unfinished. Sixty separate skinned meshes are not a production rendering budget; combination/LOD and removable-equipment handling remain further work.

### Blender locomotion clips — 2026-09-08

Added authored Soldier_Idle and Soldier_Walk actions to the Blender source and FBX export. The 32-frame cyclic walk is authored at 24 fps with alternating hip/knee/ankle and arm rotations; idle restores the rest pose. Unity imports looping Generic animation clips and a two-state controller. SoldierLocomotion selects a state from measured horizontal character displacement, with bounded playback-speed scaling and root motion disabled. The clip never drives collision, movement speed, initiative or attack timing.

Import validation samples two opposite phases and checks actual thigh rotation, not merely clip existence. Runtime QA checks visible thigh rotation during measured movement, transition to walk, return to idle and absence of root displacement from animation; the earlier manual bone deformation check temporarily disables the Animator to avoid it overwriting the test pose.

This is an original provisional walk cycle, not verified AO motion. Foot sliding at higher speeds, separate run/strafe/jump and weapon-ready animations, ground adaptation and exact original motion/timing remain unfinished. The gameplay locomotion itself still has provisional speeds/gravity, which this visual change does not validate.

### Separate Blender run clip — 2026-09-08

Added Soldier_Run as a separate 24-frame/24-fps action with larger hip/knee excursions, bent elbows and forward torso lean. The walk remains a 32-frame cycle. Unity now uses Idle/Walk/Run states with explicit integer selection from measured horizontal speed; the 4 m/s visual threshold and playback scaling are provisional presentation choices, not sourced AO movement rules. Root motion stays disabled and gameplay speeds are untouched.

Runtime QA measures a knee bend above 45 degrees during fast displacement (beyond the walk's authored range), checks the Run state, then checks Run-to-Walk and Walk-to-Idle transitions. Foot locking, run/strafe/jump blending, weapon-ready handling and exact AO animation fidelity remain incomplete.

## 2026-09-08 — Broader weapon geometry reference coverage

The local mesh decoder now accepts the observed multiple-SimpleMesh reference array and validates their combined stored bounds. Base-client weapon-reference coverage rises from 243 to 306 of 740 IDs. The remaining 433 resolved records are rejected for unresolved layouts or validation differences, and one ID is missing. Eleven decoder tests pass. See Artifacts/weapon-mesh-audit.json and Artifacts/multi-mesh-reference.json. This expands reference analysis only: the current authored Blender models, Unity executable, gameplay implementation and unverified visual fidelity are unchanged.

## 2026-09-08 — Observed vertex flag variants

Weapon reference coverage is now 308/740 after accepting observed descriptor flag values while preserving the original geometric checks. Two additional complete source fixtures pass, with 13 total decoder tests. The remaining references comprise 431 rejected records and one missing ID. Flags remain opaque and original rendering is still unverified. This is source-analysis progress, not additional playable weapons or a visual upgrade to the demo.

## 2026-09-08 — Conservative original bounding values

Weapon reference decoding reaches 348/740: 308 have exact tight bounds; 40 additionally require the observed float32 minimum-positive sentinel as a conservative maximum on non-positive geometry. These cases are separately reported, without modifying source vertices or accepting arbitrary tolerance. Seventeen tests pass. Other bounds/layout differences and one missing resource remain unresolved. Unity gameplay and authored Blender art are unchanged; full original rendering and 1-op-1 gameplay remain unverified/incomplete.

## 2026-09-08 — Original weapon texture identities

All 348 accepted weapon meshes now have resolved material texture-channel graphs; all 256 unique referenced texture resources exist in the original database. Seven material-graph tests pass. The rifle extractor reads its texture identity from the graph. This proves reference linkage and existence only; original shader behavior, material appearance, transforms and the finished Blender/Unity visuals remain unverified.

## 2026-09-08 — Multi-material Blender reference scenes

Tools/render_weapon_reference.py now reads original geometry and material graphs directly from the local database and produces packed reference.blend files and reference.png renders under ignored Research/ClientReference/WeaponStudies/<mesh-id>. Run Blender in background with --python-exit-code 1 --python Tools/render_weapon_reference.py -- --mesh-id <id>. Original resources remain outside Unity art.

Validated/rendered and visually inspected: 156747 (four mesh parts/four material assignments/two texture resources), 15839 (three parts/one material/one texture), and 30234 (two parts/two materials/two textures). The latter exercises a RRefFrame_t root, a non-rendering collision leaf and a multi-SimpleMesh data object. Unknown scene classes reject; traversal detects cycles and verifies that no decoded mesh was omitted. Each mesh uses its linked material, source normals and provisional V-flipped UVs. Camera framing follows the transformed bounds. Reports Artifacts/weapon-study-<id>.json record original hashes, part/material bindings and render/packed-scene hashes.

The initial image.has_data check hit Blender lazy loading and was corrected to validate image dimensions and pixel availability. The first 30234 run rejected its unreviewed group root; inspection established its matrix/children fields before adding traversal. Both failures were resolved and the successful renders were inspected.

Transforms still use the provisional transposed anim_matrix hierarchy and coordinate conversion. Lighting and roughness are study settings, not original shader behavior; collisions/attractors are omitted from visible geometry. These are original reference reconstructions, not newly authored game assets or verified original-client frames.

## 2026-09-08 — Authored hollow rifle shroud and movement threshold fix

The authored Blender rifle replaces raised dark vent markers with twelve actual through-slots across three hollow sleeve sections. Boolean cutters create rounded openings on both sides, and a separate narrow inner barrel is visible inside. Twelve per-slot ray checks during generation reject obstructed sleeve openings. All parts are still joined into Rifle/Barrel before skinning, preserving equipment visibility. The detailed Artifacts/solar-rifle-study.png was rendered and inspected. This is a geometric improvement based on the original reference silhouette, not an exact copy: proportions, surface materials and grip remain unfinished.

Regenerated DistrictKit.blend, FBX files, Unity scene and Windows build. Asset validation passes 134 checks; Blender binding validation reports 17 bones, 60 skinned meshes and 5,224 normalized-weight vertices.

Runtime verification initially failed the walk-animation fixture in a hidden window. AlwaysAnimate alone did not resolve it. Diagnostics found 15,975 frames in the 1.8-second observation, only 0.0312m movement and Locomotion=0. The generated controller had m_MinMoveDistance=0.001: very small per-frame movement was discarded. Its minimum movement threshold is now zero. The isolated capture also runs at 60fps, explicitly evaluates animation when hidden, and observes a complete walk cycle plus transition. A new distance assertion supplements the unchanged angular threshold. This is a movement bug fix, not a change to AO movement-speed values.

Final capture: 108 walk frames, maximum thigh angle 23.89823 degrees, Walk state, 3.618722m traveled; all 96 runtime checks pass with RUNTIME_CAPTURE_OK. The earlier failing runs were terminated only after their exception logs established that the capture coroutine had failed. Tests still do not establish full original-game conformance.

## 2026-09-08 — Authored rifle material separation

Added dedicated WeaponMetal, WeaponEdge and WeaponGrip materials in Blender and Unity. The authored receiver, stock rod and shroud now use warmer dark metal, collars/guards use a lighter metal and the grip has a brown nonmetal material. These approximate the previously inspected original reference's broad material separation; they do not reproduce its texture, weathering, detailed reflectance or exact colors. The original model reference remains outside shipped art.

Rebuilt the editable Blender kit, FBX, scene and Windows player. Inspected Artifacts/solar-rifle-study.png. Verified the generated scene references all three dedicated Unity material assets (two Metal, two Edge, one Grip slots). The build passes 134 import/animation checks, and its capture passes 96 runtime checks with RUNTIME_CAPTURE_OK. Gameplay/stat data did not change. Fine surface detail, proportions and grip posing still need work.

## 2026-09-08 — Burst input coverage and unresolved defaults

Tools/extract_burst_reference.py projects original Burst-capable item inputs from the local catalogue through inspected patch 18.8.62. The Can flag is bit 11 (2048), confirmed independently by the local CellAO CanFlags enum and AOSharp WeaponItem.GetSpecialAttacks; Burst skill ID is 148 and BurstRecharge is 374. It preserves raw attack/recharge, clip-size, damage and cycle values and explicit field presence. No missing cycle becomes zero.

Results: 2,282 Burst-flagged records, all with attack/recharge fields; 2,238 have an explicit cycle value, 44 omit it, none explicitly store zero. Starter 121569 is one of the missing-cycle cases, retaining base 18.8.50 provenance, Can=3077, raw attack=100, recharge=150, clip=4294967295 and damage 3–24. Output Research/ClientReference/Catalog-through-18.8.62/burst-reference.jsonl is local reference data; Artifacts/burst-reference-audit.json records coverage and hashes. Four tests distinguish absent/zero, preserve raw values and reject non-Burst items.

Published formula evidence: https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-33 (Tepamina, updated 2012) describes recharge as weapon recharge seconds times 20, plus cycle/100, minus skill/25 and states a nine-second cap. https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/attack-rating-weapon-damage-and-special-attacks (updated 2013) gives the same unbounded expression and identifies delay as BurstRecharge/100, while noting special-specific caps. These are historical authored guides, not verified live-client rules for all speeds/versions. Their formulas do not establish the absent-value default of item 121569.

The local CellAO character-stat default for BurstRecharge is the sentinel 1234567890, so it was not imported as an item default. AOSharp delegates actual special execution to the game client and supplies no independent local recharge implementation. Burst execution remains unfinished: missing/default cycle semantics, caps/rounding, hit resolution, ammunition and interaction with ordinary attacks need further evidence. No guessed Burst combat action or cooldown was added; gameplay and the Windows build are unchanged.

## 2026-09-08 — Original Burst arithmetic checked in isolation

A recovered raw Burst calculation now agrees with original x86 arithmetic and conversion code for 3,582 emulated comparisons, including all 445 explicit timing/cycle pairs and their floor boundaries. This is stronger evidence than guide formulas for that isolated branch. Default lookup precedence, final timer units/additions, hit resolution and action execution remain unfinished, so Burst is not enabled yet. See Artifacts/burst-arithmetic-emulation.json and Tools/verify_burst_arithmetic.py.

## 2026-09-08 — Special action gating evidence

Sixteen original-code emulation checks confirm the special lock list's membership/value readers. The exported action rejects when an entry is present, even with zero remaining value. The recovered Burst arithmetic still has no established connection to this exported action's timer path; its prior 3,582 comparisons must not be presented as proof of the final gameplay cooldown. Burst execution, lock creation/expiry and full conformance remain incomplete.


### 2026-09-08 — Lock application evidence

Original-client emulation now covers lock insertion and extension as well as lookup and expiry (52 passing comparisons). Existing locks are preserved by insertion and accumulated by the separate extension routine. Burst bypasses the duration routine's general SkillLockModifier lookup. Upstream duration, timing cadence and the connection to recovered Burst arithmetic remain open. Burst is still not implemented in the Unity build.


### 2026-09-08 — Special-lock message route

Original CharacterAction code 170 (AOSharp: SpecialUsed) routes skill/duration fields to lock insertion; code 20 routes to extension. Six original switch/argument emulation fixtures pass. Upstream duration calculation, cadence and Unity Burst execution are still incomplete.


### 2026-09-08 — Original interval timing

Identified the lock clock input as N3 engine delta time and verified 199 original interval-gate updates. Tested long frames trigger one tick without backlog catch-up; fractional overshoot is not retained in the tested path. Platform-clock units, full actor scheduling and upstream Burst duration calculation remain unverified. This does not yet make Unity Burst executable.


### 2026-09-08 — Platform timer units

Six original DeltaTimer conversion cases confirm milliseconds-to-float-seconds behavior, including unsigned and wraparound cases. Two engine overrides forward their input unchanged. Higher-level Timer_t processing and scheduling remain an evidence gap; the complete special-lock time path is not yet proved. No Unity gameplay changes.


### 2026-09-08 — Primary clock evidence correction

AFCM FrameProcess normally uses performance-counter ticks divided by frequency; DeltaTimer is its fallback. Six bounded original-code probes expose residual handling and boundary-sensitive millisecond conversion, which is not yet independently verified. No end-to-end timing or Unity fidelity claim follows from these observations.


### 2026-09-08 — Timer precision qualification

An independent native x86 arithmetic probe reproduces the observed boundary rounding with 64-bit x87 precision; 24/53-bit settings differ. Eighteen native observations are recorded. Actual client precision configuration remains unknown, so prior emulation results are conditional on their explicit control word and do not prove live timer behavior.


### 2026-09-08 — Precision setter found

The original executable includes a _PC_53 precision setter. All 18 sampled first timer conversions match independent native arithmetic under their corresponding control words. Later renderer configuration and full runtime scheduling remain unverified; this is not yet end-to-end Burst timing evidence.


### 2026-09-08 — Unity skill-lock state

AoSkillLocks now matches 44 fixtures projected from original list-reader/application/expiry instruction results. Burst checks lock presence and HUD progress uses stored lock fields. The fixed nine-second HUD denominator and placeholder per-frame Burst decrement are removed. This is source-level integration only: network/result producers, automatic clock events, upstream duration calculation and Burst damage execution remain incomplete. The running executable has not been replaced by this change.


### 2026-09-08 — Separate server and anti-cheat boundary

User explicitly confirmed separate client/server and requested good anti-cheat. A standalone .NET server now owns guest character training, IP accounting, revisions and persistent training state. Unity has an explicit server connection mode; its skills panel sends requests and applies responses, with no local fallback. Both sides share the existing rule implementation. Sixteen real-process tests pass, including Unity-to-server purchase, hostile inputs, concurrency and restart persistence. The separate ServerPreview build succeeds; the old running offline build is unchanged.

This is only the first authoritative slice. Accounts, session lifecycle, server movement/collision, inventory, combat, cooldowns, economy, multiplayer replication and abuse detection remain required. The listener is loopback development only; production anti-cheat/security and full AO rule accuracy are not achieved. See CLIENT-SERVER.md for the concrete boundary and remaining migration.


### 2026-09-08 — Look/feel priority and combat presentation

User now prioritizes general look/feel and gameplay before further account/security work. The full AO fidelity, separate-server and anti-cheat scope remains. Added target-facing and two-arm visual aiming on the authored Blender Soldier rig, brief recoil/muzzle lighting, smoother camera tracking and an aim shoulder offset. A Blender-authored RifleMuzzle bone attachment supplies the shot origin. Added irregular planet surface/cloud detail in the sky shader. These are authored presentation improvements, not verified original AO animation clips. Damage and cooldown formulas were not changed. The city/character remain blockout-quality relative to the supplied mood image.

The separate LookFeelPreview build is the current visual iteration. Account drafts are noncompiled under Server/Drafts; the active skill server remains the previously verified guest slice. No new account/security completion is claimed.


### 2026-09-08 — Street presentation iteration

Added two authored Blender models (planted street island and transit bench), placed alongside the district's travel landmarks, and introduced subtle procedural wear to paving/concrete. Model catalogue now has 13 types. The repeated city silhouette, character detail and full AO map reconstruction remain unfinished. This iteration improves atmosphere without claiming an exact original city layout or altering combat rules. StreetPreview is the newest visual build.

### 2026-09-08 — Readable combat impacts

CombatPreview adds fading weapon tracers, offset miss endpoints, impact sparks and a brief destruction discharge. These are authored sci-fi presentation effects, not extracted AO effects or a claim of exact visual parity. Effects use deterministic geometric directions rather than Unity's shared random generator. Existing hit chance, damage, fixture enemy behavior and attack timing remain unchanged and retain their documented fidelity limitations. The new preview remains an offline prototype; the full gameplay and server requirements are incomplete.

### 2026-09-08 — Mouse target selection and overhead target information

Added left-click selection of live training enemies and a name/health display over the selected visible target. Source: Funcom's Anarchy Online game guide, Targeting (PDF page 27) and mouse controls (PDF page 18), mirrored at https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf (accessed 2026-09-08). This covers the documented left-click selection and selected-target overhead information behavior. The guide also describes self/player targeting and detailed target information, which are not implemented by this enemy-only slice.

Picking currently uses each renderer's world bounds, nearest ray intersection and solid-scene occlusion. The 45 m selection radius is the prototype's existing Tab limit, not newly verified as AO's exact targeting distance. HUD regions and open character/mission/pause windows block world clicks. No damage or weapon-hitbox rule changes. Exact AO overlap cycling, hover cursor behavior, self/friendly targets and configurable controls remain unfinished.

### 2026-09-08 — Inspect target

T and Shift+left-click now open information for a selected live training enemy. The control binding is documented in Funcom's game guide, PDF page 27: https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf#page=27 (reviewed in the preceding targeting iteration). This prototype information panel contains an authored training description, distance and active/disabled status. It does not yet implement the original monster difficulty comparison, player level/breed/profession information or all target types. Keeping the inspected subject while changing selection is an authored UI choice, not a verified exact AO window lifecycle. Full fidelity remains unfinished.

### 2026-09-08 — Combat keyboard controls

Q begin/end combat, Shift+Tab previous hostile, and nearest initial Tab selection follow the Funcom guide (https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf, PDF pages 27/109; accessed 2026-09-08). The current implementation sorts eligible training enemies by distance on each key press; exact AO subsequent ordering/tie behavior is not proven. Friendly/self targeting, AO movement bindings and configurable controls remain unfinished. The prototype's 45 m target radius is unchanged. Hotbar 1 is retained as another attack binding.

### 2026-09-08 — Floating damage presentation

White outgoing damage and red incoming damage follow the Funcom manual's combat-number color description (PDF page 28, https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf#page=28). Numbers use the current resolved damage values. Current fractional damage is displayed to two decimals rather than inventing an integer combat rule. Font, rise speed, fade duration and placement are authored presentation choices, not verified original values. Healing/XP/team color cases are still absent; existing combat formula limitations remain. No server authority is added by these visuals.

### 2026-09-08 — Movement controls

W/S forward/back, A/D turn, Z/C strafe, Backspace walk/run and U skills follow the movement/shortcut tables in Funcom's guide (https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf, PDF pages 17/108). Removed the extra Shift sprint behavior. Runtime speeds, turn rate, shared camera/movement yaw, strafe/backward animation and full original mouse-mode behavior remain authored approximations. AO RunSpeed stat integration is still missing; this iteration does not establish exact movement parity.

### 2026-09-08 — Directional animation

The Blender Soldier now exports six actions: Idle, Walk, Run, StrafeLeft, StrafeRight and Backward. Directional state selection uses actual displacement relative to visual heading. Imported clip deformation and live state transitions are checked. These remain authored approximations: no original AO animation curves, planted-foot solver, diagonal blend set or exact movement cadence is claimed. Current 600-pixel desktop prevents the normal-resolution visual gate from passing.

### 2026-09-08 — Shortcut layers

Ten layers of ten shortcuts, Shift+number layer selection, number activation and Y visibility follow the Funcom guide (https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf, PDF pages 18–19). The original guide also describes dragging actions/items and removing shortcuts by dragging them out; this implementation currently uses an authored right-click assignment menu. Item shortcuts, macros, dragging and exact default AO starting contents remain missing. Seven prototype actions and learned executable self nanos can be assigned. Existing unimplemented Burst/First Aid actions remain explicitly unimplemented. Character save schema remains 3 with optional hotbar data and defaults for old saves; no server-authoritative action bar is claimed.

### 2026-09-08 — Shortcut dragging

Shortcuts can now be rearranged by dragging within or between layers. This is progress toward the guide's drag interaction, but the current occupied-slot swap and outside-drop cancellation are authored interim behavior. The original guide describes carrying/removing a shortcut outside the bar and dismissing it with right-click; that precise cursor-held removal flow and dragging actions/items from other windows are still absent. Runtime tests cover gesture/assignment transactions; direct live-pointer interaction and normal-resolution visual review are not yet verified.

### 2026-09-08 — Direct pointer evidence

Live @oai/sky mouse interaction verified ordinary shortcut activation, dragging to an empty slot without activation, activation after relocation, opening the assignment menu, assigning Skills and activating that assignment. Normal-size desktop capture became available again during this turn. These observations supersede the prior open direct-pointer gap for those specific cases; cross-layer pointer gestures, exact original drag-removal flow and item shortcuts still need work. The outdated C-to-close hint was corrected to U.

### 2026-09-08 — Item shortcuts

Inventory/worn item shortcuts use the same right-click-equivalent behavior described by Funcom's guide (https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf, PDF page 42): supported nano crystals upload, wearable items equip/unequip. References bind to an instance UUID and persist with saves; action-only older saves receive an empty item-reference array. Automatic destination chooses the first empty supported equipment slot, otherwise the first supported slot; this ordering is an authored interim choice. General item-use effects, consumable stacks, containers, original item drag-to-hotbar behavior and complete catalog execution remain missing. Existing equipment timing and requirement limitations remain unchanged.

### 2026-09-08 — XP/health presentation

HUD now exposes the existing character XP against the imported level threshold, and successful training claims report XP/credits and level advancement. Health uses red, consistent with Funcom guide PDF page 28 (https://www.az-net.at/wp-content/uploads/2014/05/gameguide.pdf#page=28). XP bar color/placement are authored. The existing fixed training reward amounts are not original AO mission reward formulas. SK/alien XP and associated progress bars remain unimplemented; no ordinary-XP progress is displayed for level 200+. This does not expand gameplay progression beyond the existing rules.
