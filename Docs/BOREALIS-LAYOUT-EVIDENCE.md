# Borealis layout evidence

The current Unity district is an authored transit concourse, not a reconstruction of Borealis. The preceding visual improvements do not establish city-layout fidelity.

`Tools/Build-BorealisSurvey.ps1` extracts seven landmarks from the local Nadybot WHEREIS dataset into `Research/Borealis/landmarks.json` and a coordinate diagram. Each point keeps its source record. The generic city position in record 84 is not a separate landmark; that record's prose supplies the Stret West Bank whompah coordinate.

The Grid record is (636,728); the Stret West Bank whompah is (682,531). Their horizontal coordinate separation is approximately 202 units. The prototype instead puts Grid and whompahs within a few metres. The satellite dish is (350,350), southwest of both, rather than alongside the same short concourse. These discrepancies require a city-layout replacement, not further decorative details alone.

Cross-check: [historical Borealis community guide](https://anarchyonline.fandom.com/de/wiki/Borealis) reports Grid (637,728), agreeing within one coordinate unit. It also places shops throughout the city and the dish on western hills. Its historical Jobe route and Subway information must not be assumed to match the target client version. [AO Universe travel guide](https://www.ao-universe.com/guides/classic-ao/gameplay-guides-6/travelling-on-rubi-ka) describes a different current Jobe network. The linked historical map image could not be retrieved through the web tool (402), so no footprints were traced from it.

Local CellAO Districts/800.xml corroborates playfield 800 and named districts including Borealis, The waterfalls and The Dish; it contains no building geometry. Local AODb MinimapMetadata documents tiled minimap texture identifiers but no relevant tiles have been extracted in this iteration.

Next reconstruction requirements: obtain/version original minimap or world geometry; establish horizontal axis orientation, scale and heights against landmarks; derive streets, walls and building footprints; place existing Blender landmarks at the corresponding coordinates. Keep historical/community coordinates labelled until corroborated against the target client. No transport destinations, collision geometry or live player positions were changed from these incomplete observations.

## Original-client placement extraction

`Tools/extract_borealis_layout.py` now reads all 72 static-object entries from resource 1000026:800 in the local 18.8.50 database. All 11,710 payload bytes are consumed, with per-entry size/playfield/finite-value checks. Raw source bytes and SHA-256 identities are retained under Research/Borealis/ClientLayout. Template names are resolved from the same client, rather than inferred from proximity alone.

Template 95350 is named Enter The Grid and is placed at X=636.4026, Y=66.8100, Z=728.8094. This corroborates the community Grid coordinate and supplies an original vertical coordinate. Nearby template 85303 is Team Mission Terminal at X=631.5435, Y=66.8002, Z=713.4652. Whom-pah area entries use template 96272, Dummy Whom-Pah noselect; these names do not yet resolve portal destinations.

This is stronger placement evidence than the community survey. It does not establish complete render geometry, building footprints, terrain topology, destinations or whether every object remains identical in later client patches. The previously expected minimap types 3086620/3086621 have zero records in this database. Resource 1000009:800 exists with 79,351 bytes, but its tilemap payload has not been decoded; do not treat its header guesses as a heightmap.

## Ground record structure

`Tools/inspect_borealis_ground.py` now parses the CHGA envelope and 34 serialized objects in 1000009:800. Named fields explicitly report map_width/map_height/map_modulo=257 and tiletexture_count=56. It fully decompresses 16 heightmap blocks of 4,225 bytes, 16 tilemap blocks of 8,192 bytes and 16 buildingmap blocks of 2,048 bytes, checking stream completion. Every block has a retained binary and hash. Heightmap_small_data and tile_type_data are preserved separately.

These are decoded containers, not a decoded landscape. 540 trailing bytes remain uninterpreted and are preserved explicitly. Height reconstruction (including signed/delta/scaling semantics), block ordering/orientation, building-map bit interpretation and texture assignments are unverified. The outer header's 250x250 values differ from the named 257x257 fields; do not silently equate them. No Unity terrain or building footprint may yet be claimed from these blocks alone.

## Height reconstruction and Unity import

`decode_borealis_heights.py` reconstructs each 65x65 byte block with the two-dimensional predictor `(residual + left + above - aboveLeft) modulo 256`. This matches all four reference corner bytes in all 16 auxiliary records. The 4x4 block arrangement shares 1,551 samples, all equal at seams. Simpler linear and row-based cumulative interpretations matched only four corner sets each in the exploratory comparison.

Using horizontal spacing 4 and height-per-byte 0.4 (the original envelope scalars), unflipped X/Z orientation produces terrain Y=66.8 at the original Grid's X/Z, while the object is Y=66.8100. Other axis flips/transposes disagree substantially at that anchor. These checks strongly support the reconstruction; they do not prove original collision triangulation or every object-ground relationship. The report retains all object/terrain comparisons, including offsets due to raised geometry or other causes.

BorealisLayoutBuilder imports the 257x257 reconstructed field as TerrainData, size 1024x102x1024, shifted by the same Grid origin as object placements. All 66,049 samples roundtrip within 1/65535 normalized height. The outer-envelope 250-versus-257 extent question remains unresolved. Capture mode renders the untextured height study for inspection; it is not the final game environment.

## Original tile distribution

`decode_borealis_tiles.py` stitches the 16 64x64 uint16 tile blocks using the heightfield's block arrangement. All 65,536 low-14-bit indices reference the 56-entry CHGA texture table. Each texture resolves to a 1010006 ground resource whose leading original name is retained with its payload hash. Top-two-bit values are stored separately; their likely orientation role is not yet verified.

The reconstruction terrain now uses an explicitly diagnostic palette grouped by these original texture names. It displays the source distribution of paved city surfaces, rock, grass and other ground types. Variants with the same name may encode transitions; they are collapsed for the survey, not reproduced faithfully. Blue denotes a texture-name category, not a recovered water plane. Palette colours are authored and are not original texture samples. The visible rectilinear city pattern is useful layout evidence but does not itself prove building footprint/collision geometry.

## Building-mask interpretation

The 16 2,048-byte buildingmap blocks support a 128x128-bit interpretation per block, yielding a stitched 512x512 grid at 2-unit sample spacing. The MSB-first interpretation marks 1,769 samples and puts all 46 original Door/Locked Door positions within 4 units of a marked sample (median 0.8031). LSB-first puts 32/46 within 4 (median 1.8385). The comparison is retained in building-mask-validation.json; it corroborates orientation/bit order but is not proof of rendered wall boundaries or original collision semantics.

The separate Unity reconstruction scene displays marked cells in a dark diagnostic terrain layer, with a city overhead capture. Cells are not extruded into guessed buildings. Exact extents, building heights, facade types and relationships to other collision/mesh records still require evidence. Source tile names are preserved under the overlay; Unity splat filtering is only a visualization.

## Separate PF static-geometry candidate index

The installer inventory lists cd_image/data/statels/800.pf. A 32,508-byte copy exists in PatchWorking; its hash/path are recorded by inspect_borealis_statels_file.py. It is distinct from database resource 1000026:800. Its first word is 1 followed by 625 increasing offsets, then 332 uninterpreted preamble bytes before the cell data. Empty cells are 12 zero bytes. A 25x25 grid at 40-unit spacing corroborates 174 first-object candidate positions; each also resolves a candidate mesh ID against the 18.8.50 mesh index. Twelve nonempty cells fail this restricted prefix interpretation and are retained as unresolved.

This likely supplies the missing static-geometry graph. It is NOT a complete decoder: one first prefix per supported cell is recorded, trailing objects/record variants are not decoded, packed orientation and auxiliary bytes are unknown, and PatchWorking provenance must be reconciled with a specific installer/patch version. Uniform six-group/22-byte parsing fails at cell 264 and must not be used. No original meshes or speculative placements were added to Unity.
