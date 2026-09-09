# Official client reference

Retrieved 2026-09-08. The extracted `version.id` identifies **18.8.50_EP1**. This is an older reference snapshot, not a verified current live client. It does not replace the target-version decision or the mixed 18.08.58.01 / 18.8.62 web datasets.

## Acquisition evidence

The [official archived download page](https://archive.anarchy-online.com/wsp/anarchy/frontend.cgi?func=frontend.show&func_id=1088&navID=1003,1005,1070,1088&table=PUBLISH&template=drill) links to [Funcom's downloader](https://update.anarchy-online.com/download/AO/AnarchyOnline.exe). Its NSIS header points to `http://cdn-web-nj4.funcom.com/download/AO/current_EP1.txt`. The same resource fetched over HTTPS lists `install.exe` and five `.bin` parts below `https://update.anarchy-online.com/download/AO/18.8.50_EP1/`.

The downloader and installer both passed Windows Authenticode validation with Funcom signers. Neither installer was executed. The installer product version and extracted version file agree on 18.8.50_EP1. File metadata dates the payload to June 2020; the word `current` in the download manifest does not establish a current live patch.

`Artifacts/client-download-provenance.json` records fetched URLs, byte lengths, SHA-256 hashes and observed signature results. Downloaded packages, the raw download manifest and extracted files remain under ignored `Research/ClientReference`. Original client data is not included in the Unity build.

## Extraction and audit

[innoextract 1.9](https://constexpr.org/innoextract/) unpacks the Inno Setup 5.4.2 archive without installing or launching AO. Its Windows ZIP matched the author's published MD5; its SHA-256 is also in the provenance file. This Windows build requires backslashes in exact include paths. Directory include filters matched no database files; the successful extraction explicitly selected each filename:

```powershell
& Research/ClientReference/innoextract/innoextract.exe --extract --include 'app\version.id' --include 'app\cd_image\data\db\ResourceDatabase.dat' --include 'app\cd_image\data\db\ResourceDatabase.dat.001' --include 'app\cd_image\data\db\ResourceDatabase.dat.002' --include 'app\cd_image\data\db\ResourceDatabase.idx' --output-dir Research/ClientReference/18.8.50 Research/ClientReference/install.exe
python Tools/audit_client_reference.py Research/ClientReference/18.8.50/app --output Artifacts/client-reference-audit.json
```

The audit implements the index and segmented-record layout described in CellAO's `Tools/Algorithman/Extractor Serializer/Extractor.cs`, pinned at `ca77f375a7dabe3be769da94e6c2a3d093344c2c`. Adaptation attribution and BSD license are in `Tools/licenses/CellAO.txt`.

Observed result: **459,308 unique records across 2,117 index nodes**. Every indexed record's type and instance matched its database header, and its declared payload could be read within the extracted segments. All seven reviewed executable-item IDs were found as type 1000020; their raw payload hashes are recorded. Counts are resource records, not a count of unique playable items. Database segments are 1,073,741,824, 1,073,741,824 and 133,904,451 bytes; index size is 9,437,184 bytes.

Corrupted copies of the real index with a node cycle, negative entry count or invalid segment size were rejected. `Artifacts/client-reference-validation.txt` records those checks. This audit checks structure and records local fingerprints; it does not establish a publisher-provided cryptographic checksum for the database or decode item semantics.

## Item wire-data decoding (2026-09-08)

`Tools/decode_client_items.py` now decodes all **120,569 type-1000020 stat tables**, retaining unsigned raw values, the original record fingerprint and descriptions in the ignored local JSONL reference. **108,078 records** reach the end of their structural layout; **12,491** retain an explicit unparsed suffix with offset, size, reason and hash. There are no failures before the stat/text section. Structurally parsed does not mean understood or executable: function padding/opaque arguments and block 6 are retained as bytes or numerical pairs. String/hash function arguments and shop-hash blocks are among the remaining unsupported layouts. No full item data is imported into Unity by this tool.

The layout comes from pinned CellAO `NewParser.cs`, `HLFlat3F1Counter.cs` and `FunctionSets.cfg`, with its BSD license preserved. The decoder correctly consumes the additional four bytes after ApplyTexture's two numerical arguments and the attack/defence block's marker. Unknown layouts stop parsing explicitly. `Tools/test_client_decoder.py` checks a real belt payload plus corrupted counters, truncation and an unknown suffix; all five checks pass.

All seven reviewed item records have complete structural parses. `Tools/compare_client_items.py` compares **66 fields**, all matching the current Unity definitions: names, exact QLs, Can flags, equipment slots and timing, simple wear requirements, On Wear stat modifiers, armor texture/layer identifiers, and rifle damage, timing, range, initiative and attack/defence weights. Timing comparison uses the existing convention of 10 milliseconds per database unit. This is a data comparison across 18.8.50 and 18.8.62 snapshots, not an independent measurement of timing behavior. NoDrop flags, full special-attack execution and arbitrary requirement expressions are not covered by this gate.

The rifle additionally contains event 10 functions 53065, 53076 and 53075 with opaque arguments; these are retained in the evidence, not implemented in gameplay. The armor ApplyTexture's additional bytes are likewise preserved. Matching the selected fields therefore does not imply complete item behavior.

```powershell
python Tools/decode_client_items.py Research/ClientReference/18.8.50/app --output Research/ClientReference/items-18.8.50.jsonl --report Artifacts/client-item-decoding.json
python Tools/compare_client_items.py
python Tools/test_client_decoder.py
```

`Artifacts/client-item-decoding.json` records coverage, unsupported layouts and the seven numerical samples without original descriptions. `Artifacts/client-item-comparison.json` records expected/actual values for every comparison; `Artifacts/client-decoder-validation.txt` records corruption checks.

## Next work

Complete remaining wire layouts and generalize the importer while preserving unimplemented actions. Establish the patch chain and separately fingerprint an updated client before treating it as 18.8.62 or current live. Full item actions, interpolation, world data, server mechanics and original-client behavioral fixtures remain unverified. Unity code and the previous tested Windows build were not changed during these acquisition/decoding steps.

## Extended function and shop layouts (2026-09-08)

The latest decoder run supersedes the earlier coverage count: **120,549 of 120,569 item records** are structurally consumed, with all 120,569 stat tables still decoded. The twenty unresolved records are individually recorded in `Artifacts/client-item-decoding.json` with IDs, offsets, byte counts, reasons and suffix hashes. Eight have string length/terminator mismatches, six have truncated/unconsumed tails, and six have unsupported block values. These remain unresolved; their cause is not asserted to be client corruption.

Added the pinned CellAO layouts for string arguments, reversed four-byte hash identifiers and shop-hash blocks. Strings retain their raw bytes and declared lengths; the decoder validates termination, including the zero-length form that still consumes a null byte. Hash arguments retain raw byte order as well as the reversed display form. Shop entries preserve compact/extended values and their eleven opaque trailing bytes. No interpretation of shop inventory generation, hash resolution, opaque parameters or game execution is claimed.

Nine real-record and corruption checks now pass, including a recharge-item text argument, a hash argument, four shop entries and a corrupted string terminator. The original seven-item comparison still passes 66/66. Report schema 2 adds the unresolved-record list. Numerical arguments remain integers; string/hash arguments are typed objects, so future importers must explicitly handle them. The first terminator test used an incorrect hardcoded character offset; the test was corrected to locate the actual terminator before mutating it, and all nine checks were rerun successfully.

Next work remains resolving the twenty exceptional layouts, patching/fingerprinting the target client, importing complete item/nano actions and continuing the Unity gameplay and Blender art. The decoded database is still a local research artifact, not a complete playable item library.

## Nano database and data-driven casting (2026-09-08)

`decode_client_items.py --record-type 1040005` reads the nano resource type as well as the default item type 1000020. All **10,815 nano resource records** in the local 18.8.50_EP1 snapshot have decoded stat tables and complete structural parses; no unparsed suffixes were reported. This count is resource records, not a claim of 10,815 distinct player-available programs. `Artifacts/client-nano-decoding.json` includes the two currently reviewed buff records and source payload hashes.

`Tools/import_reviewed_nano_casts.py` corroborates the current two nano casting definitions against the decoded client: Expertise costs 40, has 2100ms base cast/500ms recharge and PM/SI 61 requirements; TMS Mk I costs 126, has 250ms base cast/500ms recharge and TS 47/MC 59 plus Soldier requirements. Both use four NCU; durations remain 1800/20 seconds. The compiler rejects differences from the already reviewed web values. Time-unit conversions are the existing local conventions and still need original-client timing fixtures.

Unity now reads these cast fields from `self-effects.json` rather than hardcoded branches. Requirements are rechecked at cast completion. Source VisualProfession 368 is mapped to the playable profession check only; Agent disguise/VisualProfession behavior remains unimplemented. TMS's extra Heal Reactivity and resistance functions remain explicitly unimplemented, and this step does not import other nanos or arbitrary expressions. Each imported definition retains the client version and payload hash alongside its existing web source.

```powershell
python Tools/decode_client_items.py Research/ClientReference/18.8.50/app --record-type 1040005 --output Research/ClientReference/nanos-18.8.50.jsonl --report Artifacts/client-nano-decoding.json
python Tools/import_reviewed_nano_casts.py
```

## Reviewed nanoline replacement (2026-09-08)

Added Assault Rifle Proficiency 26354 from the explicitly identified 18.8.50_EP1 client: +10 Assault Rifle, 2 NCU, 1800s duration, 25 nano, 2000ms cast, 500ms recharge, PM/SI 31. This third definition has its own older source version; it is not presented as a verified 18.8.62 web record. Stat 75 is NanoStrain and stat 551 is StackingOrder in the pinned AOSharp enum. Proficiency and Expertise share line 27 with orders 2 and 3; TMS is line 2/order 300.

The runtime now replaces an existing reviewed same-line buff only when its priority is not higher. Capacity is evaluated after deducting the replaced buff's NCU. A failed capacity or lower-priority attempt leaves active effects, remaining duration and nano resource untouched. Different lines coexist. Same-ID refresh continues to work. Equal-priority distinct programs and composite/multi-line exceptions are not validated by the current three definitions.

The [AO Universe buff guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/buffing-guide) supports same-line exclusion; the [AOWiki buff explanation](https://wiki.aodb.us/wiki/Who_buffs_what) describes stacking-order priority. Actual-client tie and replacement-capacity boundary fixtures remain outstanding. These sources and local tests do not prove all original server behavior.

The nano library provides a Cast on self button for the three supported definitions. This is still the existing prototype access model: acquisition/learning and casting on other entities remain incomplete. Active-effect display and cancellation now enumerate the supported effects so Proficiency is visible. Earlier per-definition unimplemented notes about general nanoline replacement refer to the remaining broader scope; the reviewed Proficiency/Expertise pair is now implemented.

## All item suffixes resolved (2026-09-08)

The latest run structurally consumes **all 120,569 item records and all 10,815 nano records**, with no unparsed suffixes or stat-table failures. This supersedes the earlier twenty-exception count. It remains structural decoding, not evidence of complete gameplay behavior or semantic understanding of opaque fields.

Five older CellAO function layouts were corrected against actual client bytes and the independent [anarchydevs/aodb data model](https://github.com/anarchydevs/aodb), pinned at `91dbd554a622ada0e336ba7a6152cbb52745cdd8`:

- InputBoxSpell 53115 has a string followed by two integers.
- TextSpell 53134 has two strings followed by one full integer, not three trailing bytes.
- NpcMovementActionSpell 53191 has one action integer (the reviewed sit/stand records contain 30/37).
- CreateCityGuestKeySpell 53235 has three integer words, including full-width flags.
- SendMailSpell 53252 has three strings, a hash and three trailing integers.

The corresponding files are under `Research/aodb/AODb.Data/Spells`. These field-layout facts corroborate the bytes; their descriptive comments are not treated as proof of all runtime semantics. Empty strings consume only the zero length word. **The earlier note claiming that zero-length strings consume an additional null byte was incorrect** and is superseded. Nonempty strings retain length/terminator validation.

Fourteen decoder regression checks pass, including real records for these layouts, an empty first string followed by a nonempty second string, and malformed input. The key test initially used a flags value from older parser commentary; direct inspection of the 18.8.50 payload establishes bytes `8f 02 17 c0` (unsigned `0xC017028F`), which the test now checks. All 66 reviewed item-field comparisons remain passing. Unity files and the tested player were not changed this turn.

## General single-skill buff execution (2026-09-08)

`Tools/import_general_skill_buffs.py` compiles **97 general Proficiency/Expertise programs** from the 18.8.50_EP1 nano records. Together with partial TMS this yields **98 self-effect definitions**. Every accepted record must have explicit timing/cost/NCU/duration/line/priority fields, the reviewed flags, one unconditional single-tick Modify effect, and a supported two-skill conjunction. Values are preserved per record rather than normalized. First Aid and Treatment correctly require BioMet/PsyMod, unlike the more common PsyMod/SenseImp pair.

Eleven candidates remain unresolved in `Artifacts/general-skill-buff-import.json`: eight composites, Improved Psychology Expertise's different flags, BioMet Proficiency's absent attack-time stat 294 (it has stat 72 instead), and Deflect Expertise's missing explicit recharge field. No zero/default is guessed for absent fields. Composite cross-line replacement and acquisition remain part of the full goal.

The Unity caster now carries the actual nano ID and accepts any compiled definition, replacing its previous three-action switch. The library's self-cast button works for compiled records. Active effects use a scrollable display with cancellation. Other-target casts, nano acquisition/learning and wider modifiers/actions remain incomplete; the prototype still permits attempting supported library records without a learned-nano inventory.

Each general buff is explicitly labeled 18.8.50_EP1 with its payload hash. The older compiler for the three initial definitions skips unrelated records so it cannot delete the broader catalog. `import_general_skill_buffs.py` rejects mismatches against any existing reviewed numeric field before updating it. This remains a mixed snapshot project; current-client conformance is not established.

## Nano crystal upload and learned-program persistence (2026-09-08)

`Tools/import_nano_crystals.py` compiles 98 ordinary one-charge crystals whose single unconditional UploadNano function targets a supported program. It uses each crystal's own Use requirements; it does not assume upload and cast requirements are identical. The item catalog now has 105 exact-QL definitions (seven equipment records plus 98 crystals). Each crystal has a client payload hash and explicit 18.8.50_EP1 source. Damaged/random crystals and unrelated upload functions are not accepted by this importer.

Character saves now contain `learnedNanos`. Casting checks membership both before starting and at completion, so browsing a database record no longer grants access to it. Upload requires an owned unequipped supported crystal and satisfied requirements. Successful upload learns the program and removes the one-charge item; failed requirements or duplicate learning leave the crystal intact. Upload is rejected while casting, equipping or dead. Equipment rows expose Upload for crystals; supported-but-unlearned library entries show Not learned.

The [nano crystal bug report](https://forums.funcom.com/t/nano-crystal-summon-urolok-the-rotten-is-borked/84710) documents that crystal upload requirements can differ from cast requirements. The client function ID 53019 is UploadNano in the pinned CellAO enum, and its upload handler persists a program ID. The [nano guide](https://anarchyonline.fandom.com/wiki/Nanos) describes using an inventory crystal to upload. One-charge consumption follows the reviewed ordinary item shape; full item-use timing, server transaction behavior and damaged-crystal behavior still need conformance fixtures.

Existing schema-2/3 saves without the new field load with an empty learned list; no unsupported historical grant is invented. New starter crystals/programs have not yet been sourced or granted. Consequently fresh characters and old prototype saves need real acquisition paths before they can obtain these buffs in normal play. Shops, drops, starter inventory and profession-specific starting nano rules remain unfinished. QA explicitly seeds learned programs only inside its isolated fixture and separately tests a real crystal upload; it does not save those grants to a user character.

The selected item comparison remains scoped to seven equipment records; adding crystals does not extend that 66-field gate. Crystal upload has separate integration tests for ownership, missing skills, successful consumption, duplicate rejection and save round-trip.

## Soldier startup nano: Body Boost (2026-09-08)

The actual startup crystal is **29092, Soldier: Startup Crystal - Body Boost**, uploading **29091 Body Boost**. It requires Soldier (VisualProfession 368 equals 1), BioMet 5 and Matter Creation 5. Client data provides +20 MaxHealth (stat 1), 1 NCU, 14400 seconds duration, 11 nano, 3000ms base cast and 4000ms recharge, line 151/order 1. `Tools/import_body_boost.py` validates these fields and imports the program and startup crystal, retaining their payload hashes. The catalog now contains 99 effects and 106 executable item definitions.

The [AO Galaxy Soldier health-buff listing](https://www.aogalaxy.com/nanos?aoProfID=6) corroborates the buff's cost/timing/requirements and identifies its location as Arete Nano Container. Client item **248265 Soldier Nanoprogram Container** uses spawn hashes `7K1M` and conditionally `STVK`, plus function 53229/hash `X5BU`. These container functions and acquisition conditions have not been resolved; merely finding a startup crystal in the database does not prove that modern Arete grants it directly. No new normal-play inventory grant was fabricated. `Artifacts/soldier-startup-evidence.json` preserves the three relevant records without original descriptions.

MaxHealth now includes the stat-1 equipment/effect modifiers. The local current-HP rule preserves current health on a maximum increase and clamps it when the maximum decreases; this avoids HP exceeding its capacity. This transition still needs an original-client behavioral fixture and is not a verified healing mechanic. Body Boost is currently self-cast only.

Validation includes uploading at a Soldier's base skills, rejecting the crystal for a different profession without consuming it, +20 actual MaxHealth, expiry, runtime cast cost and cancellation/clamping. The crystal is provided only inside the QA fixture until the actual starter/container acquisition path is completed.

## Soldier container acquisition and unresolved references (2026-09-08)

The [current Arete guide](https://www.ao-universe.com/guides/classic-ao/encounter-guides-3/low-level-encounter/arete-landing), section 6, identifies **Marco Spida**, waypoint **3407.7, 831.0**, as the vendor. The player selects their profession's container and confirms the purchase. Opening it advances the associated mission and awards Composite Attribute Boost; the guide lists 1120 credits and up to 2229 XP. These are source observations, not implemented rewards. Exact purchase price and level-dependent reward formula remain unresolved. The container's client Value 1500 is not assumed to equal the shop price.

The [nostalgic ICC Shuttleport guide](https://www.ao-universe.com/guides/nostalgia/guides/nostalgic-guide---icc-shuttleport-island) lists a historical Soldier package with Ego Taunt, Partial Deflection Shield, Minor Combat Barrier and Damage Multiplier. That older list is not used as a verified current Arete payload. The current Body Boost listing identifies Arete Nano Container; the version/content discrepancy still needs a stronger fixture.

`Tools/trace_container_hashes.py` performs a repeatable literal byte and direct identity search across the extracted database for 7K1M, STVK and X5BU. The report is `Artifacts/soldier-container-hash-trace.json`. There are no direct index identity matches. The 7K1M bytes occur once, in the container; STVK occurs in the container and as an uninterpreted occurrence inside resource type 1010004; X5BU occurs in fourteen profession containers. These matches do not resolve spawn tables or quest records. The scan does not decode compressed/encrypted payloads or match across segment boundaries, and it does not prove mappings are absent from all client data or server data.

A critical unresolved condition is operator 36 with operand **70320 Partial Deflection Shield**. Both pinned AOSharp-adjacent aodb and CellAO enum sources name it HasNotFormula. However, CellAO RequirementLambdaCreator.cs actually calls Character.HasNano without negation, and that method checks uploaded programs. A web-rendered interpretation referring to a running nano also does not establish the correct meaning. **Earlier commentary describing operator 36 as definitively not learned was too strong: its polarity remains unverified.** Do not substitute active-buff state for learned-program state, or ship either polarity without resolving this contradiction.

These findings change the acquisition plan: implement the documented vendor/quest route once the spawn mappings, predicate and prices are verified; do not add a fictitious automatic Body Boost grant or simply copy the historical four-nano package. Full implementation remains required. No Unity/player code changed during this investigation.
# Official EP1 patch acquisition — 2026-09-08

The official HTTP page http://launcher.anarchy-online.com/exepatches/index_html is reachable, although HTTPS on that host failed. Its EP1 list advertises twelve sequential patches from 18.8.50 through 18.8.62 and a final 18.8.62 to 18.8.62.0 hotfix. This is an advertised archive chain, not proof of the currently running server version.

`Tools/acquire_client_patches.py` downloads only URLs present in that page, upgrading the download host to HTTPS, extracts CAB archives with 7-Zip without executing the patch programs, and checks embedded CHK MD5s. `Artifacts/client-patch-acquisition.json` records source URLs, SHA-256 file fingerprints and verification results. Twelve archives were acquired; all 24 embedded checksums matched. MD5 checks establish package consistency, not independent authenticity. The first extracted AnarchyPatcher.exe additionally had a Valid Authenticode signature from FUNCOM OSLO AS.

The final advertised 18.8.62.0 EP1 archive returns HTTP 404 over both HTTPS and HTTP. The script deliberately exits nonzero and preserves this failure in its report instead of claiming complete acquisition. Re-running verified existing archives reproduces the same results.

RTD files identify RTPatch binary differences and list required DLL/executable changes with missing files treated as errors. The existing database-only extraction is therefore not a complete patch input. No patches have been applied, no installer or patch executable has been run, and all current decoded data remains explicitly 18.8.50_EP1. Next: prepare an isolated complete reference extraction and determine the patch program's supported invocation and database-update procedure, then verify the resulting version and database before reimporting.

## RES records and complete reference extraction — 2026-09-08

The installer was extracted without execution to `Research/ClientReference/PatchWorking/app`: 7,098 files, 3,133,054,176 bytes, version.id still 18.8.50_EP1. The original database-only snapshot was preserved. No patch executable was invoked.

`Tools/inspect_resource_patches.py` now inspects the twelve acquired RES files after checking their acquisition SHA-256 fingerprints. The observed envelope starts with four big-endian words (988536, count, zero, repeated count). Each entry starts with type/id/length. A following RTPatch signature 4b2a0902 identifies an opaque delta of that length; otherwise a compressed-byte length and zlib stream supply a complete record of the declared expanded length. Complete records contain little-endian type/id plus a third word before the existing payload. This is an empirically checked layout for these packages, not a universal format specification or proof of deletion semantics for other headers.

All 788 envelopes reach exact end of file; complete-record identities and expanded lengths match. Twelve opaque deltas affect types 1000001, 1000009, 1000010 and 1000026. All 532 item/nano entries fully decode with the existing parser, producing 482 unique latest item/nano records; 423 IDs are absent from the corresponding base catalogue. This is not 423 newly executable items. Local original descriptions remain in ignored Research JSONL; the tracked report contains identities, hashes and changed-field names.

`Artifacts/resource-patch-inspection.json` records per-patch counts and the base comparison. None of these item/nano IDs overlaps the 106 executable items or 99 self effects presently implemented. This supports no data changes for those definitions in the inspected RES chain; it does not verify engine behavior, other resources, the missing hotfix, or the live server. No Unity catalogues were silently relabeled to a newer version.

Eight parser tests pass, including every truncation point of an actual record, header/count corruption, identity mismatch, expanded-length mismatch, appended data, an extra compressed stream and a real mixed delta/complete-record file. Next: integrate the complete-record overlay into a clearly versioned reference catalogue and verify an actual patched client independently before claiming full version equivalence. Full gameplay, acquisition routes and Blender world fidelity remain incomplete.

## Versioned item/nano reference projection — 2026-09-08

`python Tools/build_client_catalog.py` builds `Research/ClientReference/Catalog-through-18.8.62/items.jsonl` and `nanos.jsonl` from the original decoded base plus the complete records in the verified, contiguous twelve-patch chain. It reads the RES files directly, verifies acquisition hashes, refuses opaque item/nano deltas, duplicate identities and incomplete payloads, and replaces records by (resource type, ID). Later patches take precedence. Base records retain a base-client origin; patch records retain version, archive URL and RES hash. The original payload hash stays attached in both cases.

The projection contains 120,842 item records (313 latest patch-origin records) and 10,965 nano records (169 latest patch-origin records). Counts include resource variants, not just player-available items or programs. Output fingerprints and the exact input chain are recorded in `Artifacts/client-catalog-projection.json`; the missing .62.0 hotfix stays explicit. No Unity executable data or old Nadybot index was silently replaced or relabeled.

Six tests pass. Beyond merge edge cases and tampered/missing source rejection, the full projection is compared field-for-field with every unaffected base record and every latest record from the separate previous RES inspection. These tests establish projection integrity; they do not establish equivalence to a running 18.8.62 client, the server, unsupported record types or executable gameplay. The original full client remains extracted at 18.8.50 and has not been patched. Next: independently verify the official patch application and use the projection to expand executable item/nano coverage with actual semantics and acquisition paths.

## Composite Nano Expertise execution — 2026-09-08

Imported program 223380 from the versioned projection; its actual origin remains 18.8.50_EP1. The complete record explicitly gives +20 to 127/128/129/122/131/130, 4 NCU, 28,800 seconds, nano cost 1, cast/recharge 1 second each, PM/SI >=61, primary line 91 and additional lines 35/108/116/87/89, priority 10. Numeric import evidence and payload hash: `Artifacts/composite-nano-import.json`.

AOSharp Stat.cs identifies 546–550 as StackingLine2–6. The runtime now matches intersections of positive primary/additional lines, checks all overlapping programs' priorities, subtracts each replaced program's NCU once, and replaces whole programs atomically. This extends the existing replacement interpretation to explicit additional lines; independent original-client proof of cross-line behavior is still pending. AO-Universe's buffing guide (https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/buffing-guide) corroborates non-stacking within a nanoline but does not establish all composite replacement edge cases.

Crystal 223381 has an additional operator-36 requirement and an action-5 False condition. The ordinary crystal importer rejects this unsupported shape; no acquisition/upload behavior was invented. The nano runs for known-program characters, including the isolated runtime test's seeded knowledge. No starter grant was added. Other-target casting and world acquisition remain unimplemented.

## Seven composite skill groups — 2026-09-08

Tools/import_composite_skill_buffs.py supersedes the single-program importer for the reviewed +20 general groups. Exact IDs: Physical Special 215264, Ranged 223348, Melee 223360, Ranged Special 223364, Nano 223380, Tradeskill 287040 and Utility 287046. All retain their base-client 18.8.50_EP1 provenance despite being read through the .62 projection. Each has explicit cost 1, 4 NCU, 28,800-second duration and 1-second cast/recharge. PM/SI minima are respectively 61 for the first five, 20 for Tradeskill and 4 for Utility. Each record's six stacking lines and all modifiers are recorded in Artifacts/composite-skill-import.json; no missing line is inferred from modified skills.

The compiler requires the reviewed exact modifiers, timings, flags, requirement expressions and stacking lines. Composite Martial Prowess 302158 is excluded because it also changes combat modifiers outside this skill-only scope. The ordinary crystal importer continues to reject seven composite crystals with unsupported action/requirement shapes. No automatic learning or acquisition was added.

Runtime effect validation covers every modifier and expiry for all seven groups, simultaneous non-overlapping groups, and distinct Utility/Tradeskill requirements. These are implementation checks against decoded data; cross-line precedence and live behavior still require independent original-client verification. The general single-skill import report is an earlier snapshot and its composite exclusion messages are superseded by this compiler, not proof that these programs remain unavailable to the caster.

## Solar-Powered Assault Rifle visual reference — 2026-09-08

The original 121569 record explicitly maps stat 79 (Icon) to 13313 and stat 209 (WeaponMesh) to 15839. AOSharp's Stat.cs confirms those stat identities. The database index contains icon 13313 as type 1010008: a native 48x48 PNG, 1,965 bytes. Type 1010001 / 15839 is a 15,724-byte serialized resource containing RTriMesh_t, FAFTriMeshData_t, SimpleMesh, TriList and vertex/material descriptors. Type 1000046 also has ID 15839, but that separate 16-byte record was not interpreted as geometry.

Tools/extract_solar_rifle_reference.py verifies the item mapping, extracts the icon and opaque mesh locally without modifying them and records hashes in Artifacts/solar-rifle-visual-reference.json. Original resource files remain in ignored Research, outside shipped Unity art. No mesh-format decoder or exact geometry conformance is claimed.

The Blender rifle now interprets the icon's elongated silhouette with distinct stock, receiver, grip, ribbed forestock, barrel and sights. These are original authored shapes, not a reconstruction of the opaque mesh's vertices. Parts are joined into the existing Rifle and Barrel skinned objects so inventory hiding and hand binding remain intact. Detailed proportions, materials, weapon-ready grips and exact appearance still need stronger original-client visual evidence.

## Original rifle mesh decoding and reference reconstruction — 2026-09-08

The earlier opaque-mesh status is superseded for this particular descriptor. Tools/decode_client_mesh.py reads the 3/0/0/1 serialized envelope, 65 paired symbols, one root wrapper and 21 object records with typed length-delimited fields. References index the object records after the wrapper. It retains raw fields and only decodes the observed (16,65536,274,count) vertex descriptor: 3 position floats, 3 normal floats and 2 UV floats. Three SimpleMesh parts contain 29,29,215 vertices and 45,45,352 triangles respectively: 273 vertices and 442 triangles total. All triangle indices are in range, normals have unit length within tolerance, and all 18 min/max bound components exactly match the separate stored BVolume records.

The material graph references medium_blaster texture resource 1010004/17222, a 14,830-byte JPEG. The extractor now saves that locally too. Tools/render_original_rifle_reference.py renders a local reference from the decoded mesh and original texture, with inferred row-matrix transpose, hierarchical composition, coordinate conversion and V flip. These transform/UV conventions remain provisional until comparison with original-client rendering; they are not validated by bounds checks. Original geometry, texture and render remain outside shipped Unity art in Research/ClientReference.

The reference reveals a vented extended barrel shroud, narrow stock rod and lower support, contradicting the earlier conventional-stock icon interpretation. The authored Blender weapon has been corrected toward that silhouette. Exact proportions, surface materials and grips remain unfinished. Seven decoder tests cover the real fixture, truncation, trailing bytes, header mismatch, changed bounds, invalid indices and unknown vertex layout. Tracked evidence: Artifacts/solar-rifle-mesh-decoding.json.

## Weapon mesh coverage and multiple geometry parts — 2026-09-08

Tools/audit_weapon_meshes.py enumerates all 740 unique positive, non-sentinel WeaponMesh IDs in the base 18.8.50 item catalogue and resolves them against resource type 1010001. The first audit decoded 243, rejected 496 and found one missing resource. This is structural coverage, not proof of correct rendering or implemented weapons.

Original resource 30234 contains FAFTriMeshData_t object 5 with num_meshes=2, a type-17/unit-4 mesh array referencing SimpleMesh objects 6 and 12, and bounds object 18. Those parts contain 48/64 vertices and 52/72 triangles. The union of both parts matches all six stored bounding components exactly. Artifacts/multi-mesh-reference.json records the original payload SHA-256 and identities. The tests load this fixture directly from the local database, without requiring an extracted fixture file.

The decoder now validates the reference-array count and target classes, then checks bounds across all referenced parts. All 98 formerly rejected multi-part resources pass that new layout stage: 63 now decode completely, while 35 fail the unchanged exact bounds check. Updated totals: 306 decoded; 239 bounds mismatches; 179 unsupported vertex descriptors; 13 trailing-data cases; one unsupported header; one unexpected normal layout; one missing resource. These categories reconcile to 740. Artifacts/weapon-mesh-audit.json retains individual hashes and outcomes.

A diagnostic inspection of the 239 bounds cases found differences ranging from the smallest positive normalized float32 (1.1754943508222875e-38) to about 15.7865 model units. Their cause remains unresolved; no broad tolerance or ignored bounds check was introduced. Eleven decoder tests pass, including original single/multiple-part geometry, count mismatch, wrong target class and out-of-table references. No original geometry was imported into Unity and no new gameplay fidelity is claimed.

## Alternate vertex descriptor flags — 2026-09-08

Inspection of all 179 weapon resources previously rejected for vertex descriptors found the same first/third words (16 and 274) and the same 32 bytes per vertex, with second-word variants 0, 2048 and 67584 in addition to the previously accepted 65536. These values are preserved as opaque flags; no material, rendering or GPU usage semantics are inferred. The decoder now accepts only these four observed values while retaining vertex-count, finite-value, normal, triangle and exact-bound checks. The complete descriptor is retained in each decoded mesh and in successful weapon audit entries.

Two additional original resources pass every check: 165052 has descriptor (16,67584,274,612), 282 triangles and six exact bounds; 203233 has (16,67584,274,55), 82 triangles and six exact bounds. Original fixtures test both. The other 177 resources now reach later validation: 175 fail stored bounds and two fail normal validation. In particular, no resource with flags 0 or 2048 has yet passed the complete decoder. Observed row size alone is not proof of those flags' rendering semantics.

Updated full audit: 308 decoded, 414 bounds mismatches, three normal-layout failures, 13 trailing-data failures, one header failure and one missing resource, totaling 740. Thirteen mesh tests pass, including rejection of an unobserved flag value. The rifle geometry report was regenerated to reflect descriptor retention; original geometry counts and bounds remain unchanged.

The 13 trailing-data resources were also inspected without accepting them: tails range from two to fifteen bytes, including both zero and nonzero sequences. Resource 136588 ends with nine bytes spelling `ungleplan`; others contain binary values. Their purpose remains unknown. No padding-stripping heuristic was added. No Unity assets or runtime behavior changed.

## Conservative float32 maximum bounds — 2026-09-08

The former strict tight-bound requirement rejected original resources whose maximum on a non-positive axis is stored as exactly 2^-126 (1.1754943508222875e-38), the smallest positive normal float32 value. Examples include resource 7826 (all Y positions negative), 21182 (maximum Z exactly zero), and 21191 (two affected axes). Such a maximum still contains the geometry. Its exact equality to this float32 constant is observed evidence; the exporter/client operation that produced it is not established. Diagnostic attempts to explain other minima via lexical comparison or vector-reset algorithms did not explain the broad failure set and were not implemented.

The decoder now accepts this specific conservative maximum only when the computed maximum is non-positive. It does not change vertices, use a general tolerance, accept arbitrary padding, or accept this constant as a mismatching minimum. Exact matches remain counted in verifiedBoundsComponents; the new sentinelExpandedBoundsComponents field counts the separate accepted cases. The audit explicitly separates resources with exact bounds only from those requiring this observed conservative case.

Full audit: 348 decoded, comprising 308 with exact bounds only and 40 with the conservative case (41 components); 374 rejected for other bounds differences, three for normal layout, 13 for trailing data, one for header, and one resource missing. The total remains 740. Seventeen decoder tests pass, including real negative/planar/two-axis fixtures, arbitrary maximum-padding rejection, positive vertices exceeding the sentinel, and a mismatching minimum. Rifle 15839 retains all 18 exact bounds and zero sentinel cases.

This supersedes earlier statements that every accepted resource has exclusively tight exact bounds. It improves geometry-reference coverage, not rendering validation, material decoding, authored art, or playable content.

## Weapon material texture graph resolution — 2026-09-08

Tools/decode_mesh_materials.py follows each decoded SimpleMesh material reference through FAFMaterial_t.delta_state, RDeltaState texture-channel arrays, FAFTexture_t.creator and AnarchyTexCreator_t type/inst. It checks expected classes, reference types and ranges, channel-array lengths and ambiguous fields. Render states, lighting, opacity, environment textures and shader semantics are not interpreted. A resolved channel identity is not proof of equivalent rendering.

Tools/audit_mesh_materials.py resolves this observed graph for all 348 geometrically accepted weapon resources. They reference 256 unique texture resources, all present in the original 18.8.50 database. Artifacts/weapon-material-audit.json records per-model material/channel/object identities and original mesh/texture hashes. Models rejected by the mesh decoder are outside this audit.

Tools/extract_solar_rifle_reference.py now obtains texture identity from the actual original material graph instead of hardcoding 17222. The reviewed rifle requires exactly one channel, channel 0, of resource type 1010004. Its original graph resolves objects 6 -> 7 -> 8 -> 9 to resource 1010004/17222, with the same original texture checksum as before. The extraction report now includes these links. Seven tests cover that original chain, dynamic identity extraction, wrong class, invalid reference, channel-count mismatch, duplicate field and wrong reference type.

The existing rifle reference render remains a provisional reconstruction with simplified shading. This change supplies validated texture identities for future multi-material reference rendering; it does not import original assets into Unity or improve the authored art by itself.

## 2026-09-08 — Multi-material Blender reference scenes

Tools/render_weapon_reference.py now reads original geometry and material graphs directly from the local database and produces packed reference.blend files and reference.png renders under ignored Research/ClientReference/WeaponStudies/<mesh-id>. Run Blender in background with --python-exit-code 1 --python Tools/render_weapon_reference.py -- --mesh-id <id>. Original resources remain outside Unity art.

Validated/rendered and visually inspected: 156747 (four mesh parts/four material assignments/two texture resources), 15839 (three parts/one material/one texture), and 30234 (two parts/two materials/two textures). The latter exercises a RRefFrame_t root, a non-rendering collision leaf and a multi-SimpleMesh data object. Unknown scene classes reject; traversal detects cycles and verifies that no decoded mesh was omitted. Each mesh uses its linked material, source normals and provisional V-flipped UVs. Camera framing follows the transformed bounds. Reports Artifacts/weapon-study-<id>.json record original hashes, part/material bindings and render/packed-scene hashes.

The initial image.has_data check hit Blender lazy loading and was corrected to validate image dimensions and pixel availability. The first 30234 run rejected its unreviewed group root; inspection established its matrix/children fields before adding traversal. Both failures were resolved and the successful renders were inspected.

Transforms still use the provisional transposed anim_matrix hierarchy and coordinate conversion. Lighting and roughness are study settings, not original shader behavior; collisions/attractors are omitted from visible geometry. These are original reference reconstructions, not newly authored game assets or verified original-client frames.

## 2026-09-08 — Burst input coverage and unresolved defaults

Tools/extract_burst_reference.py projects original Burst-capable item inputs from the local catalogue through inspected patch 18.8.62. The Can flag is bit 11 (2048), confirmed independently by the local CellAO CanFlags enum and AOSharp WeaponItem.GetSpecialAttacks; Burst skill ID is 148 and BurstRecharge is 374. It preserves raw attack/recharge, clip-size, damage and cycle values and explicit field presence. No missing cycle becomes zero.

Results: 2,282 Burst-flagged records, all with attack/recharge fields; 2,238 have an explicit cycle value, 44 omit it, none explicitly store zero. Starter 121569 is one of the missing-cycle cases, retaining base 18.8.50 provenance, Can=3077, raw attack=100, recharge=150, clip=4294967295 and damage 3–24. Output Research/ClientReference/Catalog-through-18.8.62/burst-reference.jsonl is local reference data; Artifacts/burst-reference-audit.json records coverage and hashes. Four tests distinguish absent/zero, preserve raw values and reject non-Burst items.

Published formula evidence: https://www.ao-universe.com/guides/classic-ao/profession-guides/tepaminas-soldier-guide-33 (Tepamina, updated 2012) describes recharge as weapon recharge seconds times 20, plus cycle/100, minus skill/25 and states a nine-second cap. https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/attack-rating-weapon-damage-and-special-attacks (updated 2013) gives the same unbounded expression and identifies delay as BurstRecharge/100, while noting special-specific caps. These are historical authored guides, not verified live-client rules for all speeds/versions. Their formulas do not establish the absent-value default of item 121569.

The local CellAO character-stat default for BurstRecharge is the sentinel 1234567890, so it was not imported as an item default. AOSharp delegates actual special execution to the game client and supplies no independent local recharge implementation. Burst execution remains unfinished: missing/default cycle semantics, caps/rounding, hit resolution, ammunition and interaction with ordinary attacks need further evidence. No guessed Burst combat action or cooldown was added; gameplay and the Windows build are unchanged.

## 2026-09-08 — Original-client Burst code candidates

The follow-up default trace found that CellAO Item.GetAttribute delegates absent attributes to ItemTemplate.getItemAttribute, which calls StatNamesDefaults.GetDefault. That table has no explicit 374 default and returns sentinel 1234567890. It does not establish zero. Nadybot SkillsController.burstCommand adds attack time and floors recharge, unlike the previously reviewed historical expression; its capBurst rounds attack+8. These differences require original evidence rather than selecting a convenient formula.

Added Tools/inspect_client_special_exports.py for static analysis of the original unexecuted Gamecode.dll (SHA-256 8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320). Local analysis dependencies are pefile distribution 2024.8.26 and capstone distribution 5.0.9 (module reports 5.0.7), installed under ignored Research/ReferenceTools. The script verifies x86 PE32, resolves three named exports, and records bounded instruction windows without loading/executing the DLL. Artifacts/client-special-export-audit.json contains identity/RVA/hash summaries; full disassembly stays in Research/ClientReference/special-export-windows.json. Windows are not claimed as complete recovered functions.

Named paths: GetShopItemStat RVA 0x17aa0, IsSecondarySpecialAttackAvailable 0x27140, SecondarySpecialAttack 0x2810c. The shop wrapper has virtual dispatch and a zero fallback on missing object paths; that is not proof of an absent-stat default. The special-availability wrapper leads to internal VA 0x10063e1d.

A static executable-section search found two push-immediate 374 candidates. A reviewed instruction-aligned window beginning RVA 0x9ac2e reads stat 210, multiplies by double 20.0, reads stat 374, adds it, and subtracts four times a stat lookup via an owner-related pointer. Later comparisons reference 50.0 and 800.0. Full caller selection, units, branch interpretation, final timing and skill identity need tracing before this is an executable rule.

A second window beginning 0x9bbfb sets EDI=1000 and later passes it with stat 374 to a virtual slot at +0x44, with adjacent similar stat assignments. It is a candidate initializer, not proof that all missing BurstRecharge values default to 1000: object type, callers and applicability to item 121569 remain unresolved. The hardcoded candidate RVAs are guarded by the exact reviewed binary hash. No guessed default or combat code was added.

## 2026-09-08 — WeaponItem RTTI and Burst branch identification

The original initializer at RVA 0x9bb4f has a direct-call candidate at 0x9c38b, verified in the surrounding decoded constructor sequence. That sequence installs primary vtable VA 0x101675ec before invoking the initializer. The MSVC complete-object-locator/type-descriptor chain names .?AVWeaponItem_t@@. The initializer's stat-374 assignment with EDI=1000 therefore belongs to WeaponItem_t initialization, rather than an unidentified object. Its +0x3c getter and +0x44 setter virtual slots resolve to RVAs 0x3d90/0x3da8, which forward through an embedded stat object at this+0x34. Template application and lookup precedence still need tracing before treating 1000 as the effective default for every absent item field.

The arithmetic routine's instruction-aligned entry at 0x9aadb chooses a pointer from this+0x14, falling back to this+0x10, and requires a non-null first argument. It compares the second argument against 148 and branches to the reviewed Burst arithmetic at 0x9ac2e. That branch reads stat 210 with detail 2, multiplies by 20, adds stat 374 with detail 2, then subtracts four times stat 148 obtained from the first argument's embedded object at +0xe8 with detail 0. The later comparisons enforce at least 50 and subsequently at least 800 in the raw result. It then passes the floating result through helper VA 0x1013ef30. Caller semantics, final timing units and helper rounding have not yet been proved. The helper begins with executable instructions, not a simple imported-function jump. No attack-time addition is present in this inspected Burst branch.

Tools/inspect_client_special_exports.py now captures the constructor/initializer, dispatch, forwarding and result-conversion windows and verifies the WeaponItem RTTI under the pinned original binary hash. Artifacts/client-special-export-audit.json includes the type evidence. These observations refine the previous candidate-only status without claiming full recharge behavior or adding speculative gameplay code.

## 2026-09-08 — Differential Burst arithmetic verification

Inspected integer conversion at RVA 0x13ef30: the SSE path stores the x87 result as a double and uses CVTTSD2SI; a separate x87 fallback performs an integer conversion and adjustment. Both original paths are now exercised by Tools/verify_burst_arithmetic.py using Unicorn 2.1.4 installed under ignored Research/ReferenceTools. This is bounded emulation of original arithmetic instructions, not a launched client, connected game, or invocation of its OS services.

The verifier checks the exact original Gamecode.dll SHA-256, maps it only into emulated memory, starts at the instruction-aligned Burst branch 0x9ac2e and stops after the real conversion helper returns at 0x9ad38. It substitutes only the three stat getter calls, verifying their stat/detail argument pairs (210,2), (374,2), (148,0). Instruction count and emulation time are bounded. Actual x87 arithmetic, comparisons, branches and both SSE/fallback conversion paths execute in the emulator.

The recovered raw calculation matches float32(20*rawRecharge + rawCycle - 4*BurstSkill), limited to a minimum 800, then converted to an integer. The earlier minimum-50 operation is dominated by the subsequent minimum 800. This describes the inspected branch only: it does not establish seconds conversion, final timer additions, template default precedence or special-attack behavior.

All 3,582 comparisons pass: 1,791 unique input triples through both conversion paths. Cases cover all 445 unique explicit recharge/cycle pairs from the projected catalogue, zero skill and the skill values immediately below/at/above the raw floor threshold, plus selected zero/large/intermediate stress cases. The 44 absent-cycle records are not assigned defaults. The report Artifacts/burst-arithmetic-emulation.json includes original binary hash, catalogue hash, inputs and individual emulated/recovered outcomes.

The large-value case also exercises float32 rounding before integer conversion. This is evidence for the recovered arithmetic over the tested domain, not arbitrary int overflow inputs or full live-client equivalence. The helper disassembly window was added to the static export report. Unity/gameplay was not changed.

## 2026-09-08 — Exported special-action lock path

Tracing the exported SecondarySpecialAttack path establishes a separate list-based lock gate. At RVA 0x2819e it calls the reader 0x63e1d for the requested stat. This reader scans 16-byte entries, compares stat at offset +4 and returns true for membership. If present, the action calls 0x63e42, which returns the first matching entry's signed word at +12, or zero when absent. Positive values are passed to the feedback path 0x6453a; zero/negative values skip that feedback but still reach rejection at 0x282e1 (AL=0). Membership and remaining value are therefore distinct.

The exported IsSecondarySpecialAttackAvailable wrapper delegates to this membership reader. The local AOSharp SpecialAttack.IsAvailable explicitly negates this export, corroborating its misleading name. No synthetic half-second AOSharp wrapper delay is treated as an original gameplay rule. The feedback function divides its input by 3600 and 60 and refers to UnableToPerformActionSkill; this supports a seconds-like display value but does not connect it to the recovered raw Burst arithmetic.

Tools/verify_special_lock_lookup.py emulates both original reader functions without substituting their instructions. Sixteen checks pass on eight synthetic lists: empty/missing, first/middle/last entries, zero/negative remaining values and duplicate stats selecting the first entry. Artifacts/special-lock-lookup-emulation.json records original binary hash and results. Fixtures do not prove the list's server source, update/expiry behavior or actual timer creation.

The static report now includes the gate/readers/rejection/display windows and a reference search for the recovered arithmetic entry 0x9aadb. This DLL contains no matching direct E8/E9 target or absolute pointer pattern for that entry. This does not prove dead code, but its invocation by gameplay remains unestablished. Earlier arithmetic emulation demonstrates what the isolated code computes, not that it is the active final cooldown implementation. Further work must trace lock insertion/update and template precedence rather than directly install the isolated formula. Unity/gameplay remains unchanged.

## 2026-09-08 — Special lock decrement and expiry

Located original updater RVA 0x646ce. It walks the same 16-byte list used by the availability gate, decrements signed remaining value at +12, keeps entries whose new value is positive and erases the others. Erasure calls 0x51cc5, whose copy helper 0x128648 moves all four words of each subsequent entry and updates the vector end. The updater resumes at the returned iterator, so consecutive expired entries are not skipped.

Tools/verify_special_lock_lookup.py now exercises that original updater, erase routine and copy loop in the emulator without substituting their instructions. The final report contains 16 prior reader comparisons plus 12 expiry comparisons: empty list, positive/one/zero/negative values, adjacent removals at the beginning/middle/end, duplicate skill entries, multiple ticks, zero ticks and a large signed value. Distinct marker words in each fixture verify payload preservation during compaction. All 28 comparisons pass. This proves the tested per-call behavior; extreme signed underflow and invalid lists are outside the tested domain.

A direct updater call at RVA 0x5b488 is guarded by byte +0x18 of the singleton obtained from 0xb1bf. RTTI identifies its installed vtable 0x10156edc as GameTime_t. The reviewed time update resets that byte, compares an accumulated value with a previous marker plus 1.0 and conditionally sets it at 0xb371. Full input-clock units, drift/catch-up handling and upstream scheduling remain unverified, so no new wall-clock behavior has been installed in Unity.

The static inspector now records updater/erase/copy windows, the caller, timing flag and RTTI evidence. Artifact special-lock-lookup-emulation.json supersedes its prior 16-check report and explicitly distinguishes reader and expiry coverage. Lock insertion, network updates, template precedence and linkage to the recovered Burst arithmetic remain unfinished.

## 2026-09-08 — SpecialAttackInfo result-message path

The message name alone did not identify a cooldown source. Local AOSharp's SpecialAttackInfoMessage (N3 type 0x754f1115) declares equip slot, amount, ammo count, target identity, skill and a final unknown integer. Its StreamReader uses network-to-host ordering for 32-bit values. SpecialAttackWeapon is a different message containing a list; CellAO's login fixture uses it for dummy martial-arts/Dimach/Brawl weapon entries, so that fixture was not interpreted as a Burst lock list.

Original Gamecode RTTI names .?AVSpecialAttackInfoIIR_t@@ at vtable RVA 0x167cfc. The serializer/deserializer at 0xa1b67/0xa1bbb confirms three initial scalar fields (object offsets +0x20/+0x24/+0x28), target identity (+0x18), and two final scalars (+0x2c/+0x30). The dispatch slot at 0xa1c02 resolves an actor via message identity +4 and forwards the fields to its weapon-holder handler 0x6ac00 through actor+0x1d4. Missing actors skip that call.

The reviewed receiver tail subtracts the amount from target stat 27 and forwards equip slot/ammo to 0x6855b. The final unknown word conditionally reaches another handler; it was not relabeled as cooldown or assigned speculative semantics. No connection to lock insertion has been proved for this message.

Tools/verify_special_result_dispatch.py now emulates the original dispatch instructions, intercepting only actor lookup and the downstream handler. Six comparisons cover three distinct decoded-message fixtures with/without an actor, including signed sentinel values and different skills/targets. All pass. These fixtures are not captured packets and do not execute damage, network framing or the downstream handler. Artifacts/special-result-dispatch-emulation.json records inputs and actual forwarded words; the static report includes RTTI, field offsets and code windows.

No Unity gameplay changes. The next lock investigation must follow the list insertion/update source separately from this result-display path.


## 2026-09-08 — Original special-lock insertion and extension

The hash-pinned 18.8.50_EP1 Gamecode.dll contains insertion at RVA 0x64ca7, extension at 0x6580c and duration adjustment at 0x63ad5. Insertion leaves an existing skill entry untouched; otherwise it appends four words (0, stat, adjusted duration, adjusted duration). Extension inserts when absent, or adds the adjusted duration to both duration fields of the first matching entry. Burst (stat 148) bypasses the SkillLockModifier lookup in this adjustment routine. This does not establish the upstream Burst duration or its time unit.

`Tools/verify_special_lock_lookup.py` now passes 52 comparisons: 16 reader, 12 expiry and 24 insertion/extension fixtures. The new fixtures use synthetic Burst inputs 0, 1 and 40, run the original list and adjustment instructions, and substitute only visual notification calls. They are not captured server messages. The static inspector records the three routines and a caller at 0x5e598, which passes a stat/duration pair from an upstream argument. That upstream input remains unidentified; the recovered Burst arithmetic has not been linked to this lock path.


## 2026-09-08 — CharacterAction special-lock input

The original CharacterActionIIR_t RTTI identifies vtable RVA 0x161e00, with handler 0x7264c in slot +8. The handler calls actor dispatcher 0x5d310 at 0x72688, forwarding action from object+0x18 and a pointer to the two words at +0x28/+0x2c as its fourth argument. The local AOSharp CharacterActionMessage schema names those words Parameter1 and Parameter2; its enum names action 0xAA SpecialUsed.

The dispatcher subtracts one from the action, checks the range through 262, translates through byte table 0x5f217 and jumps through table 0x5f07b. Action 170 selects insertion branch 0x5e588 (table index 60); action 20 selects extension branch 0x5e505 (index 2). Both pass the first pair word as skill and the second as duration to actor+0x1bc. The extension also passes a zero third argument. No semantic name is assigned to action 20 here.

Tools/verify_special_lock_dispatch.py checks RTTI/table mappings and emulates the actual switch and argument-forwarding instructions for six synthetic Burst fixtures; all pass. This verifies the distinct CharacterAction input for special locks, not a captured packet or upstream server cooldown calculation. The handler forwarding was inspected statically, not emulated. Timing cadence and the link to isolated WeaponItem arithmetic remain open; Unity Burst remains unimplemented.


## 2026-09-08 — Original lock interval gate and engine delta

N3.dll SHA256 1cc4ea47f8896f71f55013d7c4948885e8628ff90ceb7ef0568f905b18c989cd exports GetDeltaTime@n3Engine_t at RVA 0x159f: it loads the float at object+0x68. RunEngine@n3Engine_t at 0x66ad writes its float argument to that field, increments frame count +0x6c, and adds the same argument to total time +0x70. Gamecode.dll's imported singleton at IAT 0x154f44 is N3.dll's n3Engine_t::m_pcInstance. These observations identify the previously opaque input field as engine delta time, but do not alone establish platform-clock units.

Tools/verify_special_lock_clock.py runs the original Gamecode interval gate 0xb32e through 0xb3ad with synthetic engine pointers and frame deltas. It resets the isolated x87 stack between calls; it does not replace gate instructions. All 199 updates across eight sequences pass, including exact boundaries, 30/60-step inputs and long frames. For the tested finite nonnegative inputs, accumulated time reaching previous+1 sets the tick flag once and advances previous to accumulated time. A 3.5-unit update followed by zero updates yields a single tick, not backlog catch-up ticks. Fractional overshoot is not retained by this path in these comparisons. This corrects the initial unverified interpretation that its x87 adjustment might preserve overshoot.

The known actor call tests this flag before decrementing its lock list. Full actor scheduling, platform-clock conversion into RunEngine and the server calculation of the CharacterAction duration remain unverified. This is not a running-client comparison or proof of the full Burst cooldown. No Unity gameplay change was made.


## 2026-09-08 — Platform timer conversion and engine forwarding

DeltaTimer.dll SHA256 22f1e2566a2f563b56ee07898ac7fb6d71362e87f75d176084bdaf538c37b60b imports timeGetTime at IAT RVA 0x2084. Routine 0x114b subtracts its stored origin with 32-bit arithmetic, corrects unsigned interpretation and divides by the double 1000 at 0x20a8 before storing a float. Tools/verify_original_timer_units.py emulates this routine with substituted OS readings and a pre-established origin. All six cases pass, including milliseconds 16/1000, a large unsigned value and wraparound from 0xfffffff0 to 16. Initialization and multiple-wrap elapsed intervals are outside its scope.

The Gamecode n3EngineClientAnarchy_t::RunEngine export at 0x1814e forwards its float argument unchanged via IAT 0x155014 to N3 n3EngineClient_t::RunEngine (0x7a01). The latter forwards it unchanged at 0x7a3d..0x7a46 to n3Engine_t::RunEngine (0x66ad). These bounded forwarding paths were inspected statically; no full engine was executed.

The remaining gap is the higher-level scheduling/Timer_t path: Interfaces.dll imports the Anarchy engine method through thunk 0x12246; a direct-call byte search did not find its caller (this does not exclude virtual calls). AFCM Timer_t::GetDeltaTime loads through object+0x14; its update calls DeltaTimer::GetDeltaTime at 0x674c and assigns +0x14 at 0x6774. Its smoothing/update behavior remains untraced. Therefore the verified milliseconds-to-seconds conversion is not yet claimed as an end-to-end timing proof for the special lock.


## 2026-09-08 — AFCM high-resolution timer path

AFCM.dll SHA256 d04a6eef227feb0bd6fed7589acfb7e74cf329bfef8da92938608d2ede414381 exports Timer_t::FrameProcess at 0x65d3. Its primary path calls QueryPerformanceFrequency (IAT 0xa014) and QueryPerformanceCounter (0xa018), subtracts the preceding 64-bit counter, divides by frequency, and adds residual time at 0x17088. It has a framerate-limiting Sleep path and conversion through the original helper at 0x7f86. DeltaTimer processing at 0x673c is a fallback, not proof of the primary clock's full behavior.

Tools/probe_original_frame_timer.py executes this original routine with synthetic successful OS counter calls, a 2000 FPS fixture limit to avoid rate limiting, initial residual zero, and explicit x87 control word 0x37f. It records six observations in Artifacts/original-frame-timer-probe.json. One-second and 3.5-second inputs yielded corresponding float deltas. Other observations include 16000 microseconds producing approximately .015 plus .001 residual, and 16667 producing approximately .016 with .000667 residual but a tick delta of 15. These boundary-sensitive outputs have not been independently confirmed on native x86; the report deliberately does not label them passed differential comparisons or a verified recovered formula.

The path writes regular and smooth delta fields +0x10/+0x20 and a tick delta +0x28, with residual retained globally. Full rate limiting, fallback selection, boundary rounding and virtual engine scheduling remain open. No Unity timing behavior was changed on the basis of this probe.


## 2026-09-08 — Native x87 precision comparison

Tools/native_timer_precision.c is an independently authored native 32-bit arithmetic probe, compiled with MSVC 14.51.36231 using vcvars32.bat and cl /W4 /Od. It does not load any AO DLL. It computes the timer's integer-counter/frequency ratio, multiplies by 1000 on the x87 stack, then truncates to integer under explicit control words. Artifacts/native-timer-precision.csv contains 18 observations (six inputs under three precision settings).

At control word 0x037f (64-bit significand), native inputs 1000 and 16000 microseconds produce truncated integers 0 and 15, matching the surprising original-routine emulation's first conversion. At 0x007f and 0x027f (24/53-bit significands), these instead yield 1 and 16. Other sampled inputs 16667, 33333, 1000000 and 3500000 produce 16, 33, 1000 and 3500 across these settings. Thus a native arithmetic comparison reproduces the boundary effect under the same precision assumption; it does not prove the original runtime uses that assumption.

The probe is not execution of the original conversion helper or full timer, nor a claim about the live client's FPU state. Actual startup/render-device FPU configuration remains unresolved. Interfaces.dll calls _control87 with (0,0) at 0x11ede, which reads rather than sets the control word. Anarchy.exe imports _controlfp_s through thunk RVA 0x4a204; its calling path remains to be inspected. Earlier timer probes must retain explicit precision assumptions. No Unity timing change is justified yet.


## 2026-09-08 — Precision setter and native conversion cross-check

Anarchy.exe SHA256 4243068cd935402cdee41a5609bcde2fc048a2b317641900e47d98165ae2ef48 contains a _controlfp_s call at RVA 0x4a0f8 through thunk 0x4a204 (IAT 0x57224). It passes null output, new value 0x10000 and mask 0x30000. Local Windows SDK ucrt/float.h defines these as _PC_53 and _MCW_PC. The enclosing routine starts at 0x4a0e8 and is called at 0x49baa. This establishes a 53-bit precision setter in the original executable; full startup reachability and later renderer changes are not established here.

The original FrameProcess probe now samples all three precision control words 0x007f/0x027f/0x037f. At instruction 0x66e9 it records the first original helper's integer conversion and compares it with independently authored native arithmetic from native-timer-precision.csv. All 18 sampled first conversions match. This narrows the earlier uncertainty: the first conversion's observed precision dependence is independently corroborated. Full timer output, carry across multiple frames, rate limiting, fallback scheduling and live FPU configuration still need separate evidence. The report captures the executable hash and setter bytes in addition to timer identity. No Unity gameplay change.
