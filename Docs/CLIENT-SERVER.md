# Separate client and authoritative server

The requested architecture is a Unity presentation/input client and a separate authoritative server. Original AO skill, item and combat semantics remain the target. Sharing rule code does not make client calculations authoritative: only server-owned state and accepted server commands determine durable gameplay results.

## Implemented vertical slice

`Server/Anarchy.Server.csproj` is a standalone .NET 10 process. Its first command is skill training. The server creates the character, loads its own reference data, calculates IP cost and caps, changes investments and persists the result. The HTTP command accepts only `statId` and `revision`. It never imports a client save or accepts supplied IP, level, profession, investment arrays or training cost.

`Unity/Assets/Scripts/Shared/AoTrainingRules.cs` holds the existing rules used by both the server and the local development prototype. Their documented AO conformance gaps remain: this extraction does not validate missing ability-dependent, breed or Shadowlevel caps.

Unity's `-server` mode connects through `ServerTrainingClient`. The skills button sends a command and applies the returned profile. Failure does not fall back to local training. This mode starts with a server-created character and does not load or write the offline save. Training missions and their local rewards are unavailable in this mode. Movement, equipment and other local visual/gameplay systems have not yet been migrated and do not represent authoritative multiplayer.

## Run locally

From the repository root, start the server in a terminal:

```powershell
dotnet run --project Server/Anarchy.Server.csproj --no-launch-profile
```

Then start the separately built preview:

```powershell
& ./Builds/ServerPreview/AnarchyReborn.exe -server http://127.0.0.1:5077
```

Open C and raise Assault Rifle: the first investment costs five IP on the server. The original `Builds/AnarchyReborn.exe` remains the offline build.

The current connection creates a fresh guest session on each client launch. The server persists profiles and tokens remain valid across server restart, but account login, session expiry and a client reconnect/profile-selection flow are not implemented. This is a development listener restricted to loopback. `AO_SERVER_PORT` and `AO_SERVER_DATA` configure its port and persistent storage directory. Default storage is under the server output directory, independent of Unity saves.

## Anti-cheat boundary already enforced for training

- Cryptographically random 256-bit bearer tokens identify server-created sessions. Token hashes identify on-disk profiles; tokens are not written to the profile or application logs by this code.
- A client cannot select another character through the training payload. Unknown sessions receive 401; unknown JSON fields are rejected.
- A server revision and serialized mutation prevent a replay or two concurrent commands with the same revision from purchasing multiple investments. A stale command receives 409 and the current state.
- IP, caps and stat eligibility are checked on the server. Invalid commands leave persistent state unchanged.
- Request bodies are limited to 4 KiB and the development listener has a global request limit. Profiles are replaced through a temporary file and rename, before success is returned.

These are enforced controls for this command, not a complete anti-cheat system. The development request limiter is not production DDoS protection. Anonymous session issuance is not an account system. Public hosting must remain unavailable until authenticated accounts, TLS, bounded session lifecycle and production operational controls exist.

## Remaining authoritative migration

1. Account login and session lifecycle, durable character ownership and reconnect. Never turn a supplied client save into authoritative state.
2. Server-owned movement simulation, time and collision checks, with client prediction/reconciliation. Test speed, teleport, time manipulation and impossible movement against legitimate latency and corrections.
3. Server inventory, item identity, acquisition, equipment requirements and trades. Verify ownership and atomic transactions; test duplicated and replayed item commands.
4. Server combat targeting, range/line of sight, random rolls, damage, cooldowns and nanos. The client requests an action; it never chooses damage, successful hits or cooldown completion.
5. Server XP, rewards, economy and persistence. Remove every client-side route to durable progress, including local mission rewards.
6. Structured security telemetry and detection of impossible sequences or sustained abuse. Detection needs evidence, latency-aware thresholds and reviewable outcomes; an anomalous packet alone must not imply an automatic permanent ban.

Client-side checks may improve feedback but are never the security boundary. Obfuscation or a kernel driver does not replace authoritative validation. No kernel anti-cheat component is currently implemented.

## Evidence

`Tools/test_character_server.py` exercises a real separate server over HTTP: unknown/absent sessions, supplied IP injection, invalid skills, replay, concurrent requests, independent sessions, oversized payloads and persistence over a process restart. With `--unity-client Builds/ServerPreview/AnarchyReborn.exe`, it additionally launches the real Unity player and verifies a server-backed skill purchase. Current result: 16 passing checks in `Artifacts/character-server-validation.json`.

Unity compiled the shared/client code and passed 157 existing reference/invariant checks plus the build's other existing validators. Those results cover the implemented slice, not full AO conformance or complete multiplayer security.
