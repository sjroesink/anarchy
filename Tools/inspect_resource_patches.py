"""Inspect the observed EP1 RES envelopes; never apply them to the database.

Layout inferred from the twelve official 18.8.50-18.8.62 packages. Unknown
headers and incomplete streams fail closed. RTPatch deltas remain opaque.
"""
import hashlib
import json
from pathlib import Path
import struct
import zlib

from decode_client_items import decode


def unpack(data):
    offset = 0

    def take(size):
        nonlocal offset
        if size < 0 or offset + size > len(data):
            raise ValueError("Truncated RES envelope")
        value = data[offset:offset + size]
        offset += size
        return value

    magic, count, unknown, repeated_count = struct.unpack(">4I", take(16))
    if magic != 988536 or unknown != 0 or repeated_count != count:
        raise ValueError("Unsupported RES header")
    result = []
    for _ in range(count):
        kind, identity, length = struct.unpack(">3I", take(12))
        prefix = take(4)
        if prefix == bytes.fromhex("4b2a0902"):
            payload = prefix + take(length - 4)
            result.append((kind, identity, "opaqueRTPatch", payload))
            continue
        compressed = take(struct.unpack(">I", prefix)[0])
        inflater = zlib.decompressobj()
        # Bound output to the declared record length and reject trailing streams.
        payload = inflater.decompress(compressed, length + 1)
        if (len(payload) != length or not inflater.eof or inflater.unused_data
                or inflater.unconsumed_tail or length < 12):
            raise ValueError("Invalid compressed RES record")
        if struct.unpack_from("<2I", payload) != (kind, identity):
            raise ValueError("RES record identity mismatch")
        result.append((kind, identity, "completeRecord", payload))
    if offset != len(data):
        raise ValueError("Trailing RES bytes")
    return result


def main():
    root = Path("Research/ClientReference")
    acquisition = json.loads(Path("Artifacts/client-patch-acquisition.json").read_text())
    report = {"scope": "Observed RES envelopes and decoded complete records only; no patches applied",
              "baseVersion": "18.8.50_EP1", "patches": [], "decodedRecords": 0,
              "opaqueDeltas": [], "decodeFailures": [], "latestItemNanoChanges": []}
    latest = {}
    for patch in acquisition["patches"]:
        path = next((root / "Patches" / f'{patch["from"]}-{patch["to"]}').rglob("*.RES"))
        data = path.read_bytes()
        expected = next(f["sha256"] for f in patch["files"] if f["path"].endswith(path.name))
        if hashlib.sha256(data).hexdigest() != expected:
            raise ValueError(f"RES fingerprint differs from acquired archive: {path}")
        entries = unpack(data)
        counts = {}
        for kind, identity, mode, payload in entries:
            counts[str(kind)] = counts.get(str(kind), 0) + 1
            evidence = {"type": kind, "id": identity, "patchTo": patch["to"],
                        "sha256": hashlib.sha256(payload).hexdigest()}
            if mode == "opaqueRTPatch":
                report["opaqueDeltas"].append(evidence)
            elif kind in (1000020, 1040005):
                try:
                    decoded = decode(identity, payload[12:])
                    if not decoded["fullyParsed"]:
                        raise ValueError(decoded["unparsed"]["reason"])
                    latest[kind, identity] = {**decoded, "recordType": kind, "patchTo": patch["to"]}
                    report["decodedRecords"] += 1
                except (ValueError, UnicodeDecodeError) as error:
                    report["decodeFailures"].append({**evidence, "reason": str(error)})
        report["patches"].append({"from": patch["from"], "to": patch["to"],
                                  "resSha256": hashlib.sha256(data).hexdigest(),
                                  "entries": len(entries), "types": counts})
    for kind, name in [(1000020, "items"), (1040005, "nanos")]:
        needed = {identity for record_kind, identity in latest if record_kind == kind}
        base = {}
        with (root / f"{name}-18.8.50.jsonl").open(encoding="utf-8") as stream:
            for line in stream:
                record = json.loads(line)
                if record["id"] in needed:
                    base[record["id"]] = record
        for identity in sorted(needed):
            record = latest[kind, identity]
            old = base.get(identity)
            changed_fields = [key for key in ("name", "description", "stats", "actions", "events",
                                               "attackDefence", "animationSound")
                              if old is None or old.get(key) != record.get(key)]
            report["latestItemNanoChanges"].append({"type": kind, "id": identity,
                "name": record["name"], "patchTo": record["patchTo"], "newToBase": old is None,
                "changedFields": changed_fields, "payloadSha256": record["payloadSha256"]})
    with (root / "patch-item-nano-records-through-18.8.62.jsonl").open("w", encoding="utf-8") as stream:
        for key in sorted(latest):
            stream.write(json.dumps(latest[key], ensure_ascii=False) + "\n")
    report["executableCatalogOverlap"] = {}
    for catalog, field, kind in [("executable-items", "items", 1000020), ("self-effects", "effects", 1040005)]:
        content = json.loads((Path("Unity/Assets/Resources/AO") / (catalog + ".json")).read_text())
        ids = {item["id"] for item in content[field]}
        report["executableCatalogOverlap"][catalog] = {
            "catalogRecords": len(ids), "changedIds": sorted(identity for identity in ids if (kind, identity) in latest)}
    Path("Artifacts/resource-patch-inspection.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"envelopes": sum(p["entries"] for p in report["patches"]),
                      "decodedItemNanoRecords": report["decodedRecords"], "uniqueItemNanoRecords": len(latest),
                      "opaqueDeltas": len(report["opaqueDeltas"]), "decodeFailures": report["decodeFailures"]}))
    if report["decodeFailures"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
