# Ranged attacks and movement — evidence gap

Checked 2026-09-08 following the attack/recharge phase implementation.

## Established direction

The player-maintained Ranged Weapons page explicitly distinguishes ranged weapons by their inability to launch attacks while moving:
https://wiki.aodb.us/wiki/SMG (redirects to Ranged Weapons).

Its Game Terminology page describes kiting using instacast nanos, melee or pets:
https://wiki.aodb.us/wiki/Body_pull (redirects to Game Terminology).

These are community descriptions, not an observed 18.8.50 timing trace. They support removing unrestricted mobile ranged attacks but do not establish the exact scheduler transition.

## Current implementation

DistrictGame.MoveCharacter and the automatic weapon phase loop run independently. WeaponBlockReason checks casting, equipment, target, distance and sight, but no movement state. Consequently the current ranged Soldier can prepare and resolve an ordinary shot while moving. This is an outstanding gameplay mismatch, not a completed AO movement implementation.

## Required verification before choosing a timing rule

Distinguish starting an attack while moving from moving after preparation begins. Determine whether preparation resets, pauses or finishes pending a stationary release; whether recharge continues while moving; whether turning in place counts; and whether specials differ from ordinary shots. Avoid applying a generic restriction to melee/pets when those become playable. Use actual displacement and grounded state appropriately once the rule is established; input keys alone cannot distinguish blocked movement from travel.

## Checks performed

Searched original forum archive, Funcom forum, manual excerpts and local client DLL ASCII strings. The manuals found describe standing still for nanos; they are not proof of ranged weapon scheduling. Generic Funcom search results included other games and were excluded. No matching movement error string was found in the scanned top-level client DLLs. This does not prove absence: localized resources and UTF-16 strings were not comprehensively searched. The GameFAQs guide fetch was unavailable. No movement restriction was added on the strength of nano-only instructions.

Next useful work: locate the relevant original client action/movement gate or a controlled gameplay recording covering the transitions above. Meanwhile other gameplay and art tasks can proceed; this evidence gap does not block the whole remake.

## Implementation update

The later ranged-movement build now blocks preparation start and shot release when CharacterController velocity exceeds 0.1m/s, and CanHit rejects direct Fire calls under the same condition. This supersedes the unrestricted current-implementation paragraph above. Uses actual movement rather than pressed keys; turning in place is tested separately. The numeric jitter threshold is authored, not recovered AO data. Existing preparation time continues elapsing during travel but cannot release until stationary; recharge also continues. The preparation resumption behavior is a provisional implementation choice, not verified original behavior. This implements the supported no-moving-shot rule while leaving the precise interruption/reset semantics explicitly incomplete. It applies only to the current ranged Soldier path; no melee or special rule is implied.
