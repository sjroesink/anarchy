"""Build a provenance-preserving item/nano projection from base + RES patches.

This is NOT a fully patched client database. Non-item/nano resources, RTPatch
engine updates and the unavailable .62.0 hotfix remain outside this projection.
"""
import hashlib
import json
from pathlib import Path

from decode_client_items import decode
from inspect_resource_patches import unpack

ROOT = Path(__file__).resolve().parents[1]
REFERENCE = ROOT / "Research/ClientReference"


def merge_records(base, replacements, kind):
    """Record identity includes type; replace only matching base records."""
    remaining = {identity: record for (record_kind, identity), record in replacements.items()
                 if record_kind == kind}
    seen = set()
    for record in base:
        identity = record["id"]
        if identity in seen:
            raise ValueError(f"Duplicate base identity {kind}:{identity}")
        seen.add(identity)
        if identity in remaining:
            yield remaining.pop(identity)
        else:
            yield {**record, "recordType": kind,
                   "referenceOrigin": {"version": "18.8.50_EP1", "kind": "baseClient"}}
    for identity in sorted(remaining):
        yield remaining[identity]


def patch_records(acquisition, root):
    latest, evidence = {}, []
    current = "18.8.50"
    for patch in acquisition["patches"]:
        if patch["from"] != current:
            raise ValueError(f"Non-contiguous patch chain after {current}")
        expected_target = f"18.8.{int(current.split('.')[-1]) + 1}"
        if patch["to"] != expected_target:
            raise ValueError("Unsupported patch transition")
        directory = root / f'{patch["from"]}-{patch["to"]}'
        paths = list(directory.rglob("*.RES"))
        if len(paths) != 1:
            raise ValueError(f"Expected exactly one RES in {directory}")
        path = paths[0]
        expected = next(f["sha256"] for f in patch["files"] if f["path"].endswith(path.name))
        content = path.read_bytes()
        if hashlib.sha256(content).hexdigest() != expected:
            raise ValueError(f"Source fingerprint mismatch: {path}")
        seen = set()
        for kind, identity, mode, payload in unpack(content):
            if kind not in (1000020, 1040005):
                continue
            if (kind, identity) in seen:
                raise ValueError("Duplicate item/nano identity inside patch")
            seen.add((kind, identity))
            if mode != "completeRecord":
                raise ValueError("Cannot project an opaque item/nano delta")
            record = decode(identity, payload[12:])
            if not record["fullyParsed"]:
                raise ValueError(f"Incomplete item/nano payload: {kind}:{identity}")
            latest[kind, identity] = {**record, "recordType": kind,
                "referenceOrigin": {"kind": "completeResRecord", "version": patch["to"] + "_EP1",
                                    "url": patch["url"], "resSha256": expected}}
        evidence.append({"from": patch["from"], "to": patch["to"], "resSha256": expected})
        current = patch["to"]
    if current != "18.8.62":
        raise ValueError(f"Projection requires complete .50 to .62 chain, found {current}")
    return latest, evidence


def read_jsonl(path):
    with path.open(encoding="utf-8") as stream:
        for line in stream:
            yield json.loads(line)


def main():
    acquisition = json.loads((ROOT / "Artifacts/client-patch-acquisition.json").read_text())
    latest, evidence = patch_records(acquisition, REFERENCE / "Patches")
    destination = REFERENCE / "Catalog-through-18.8.62"
    destination.mkdir(exist_ok=True)
    report = {"schema": 1, "scope": __doc__.strip(), "patches": evidence,
              "missingHotfix": acquisition.get("unavailable", []), "catalogs": []}
    for kind, name in [(1000020, "items"), (1040005, "nanos")]:
        source = REFERENCE / f"{name}-18.8.50.jsonl"
        output = destination / f"{name}.jsonl"
        partial = output.with_suffix(".partial")
        count = patched = 0
        with partial.open("w", encoding="utf-8", newline="\n") as stream:
            for record in merge_records(read_jsonl(source), latest, kind):
                count += 1
                patched += record["referenceOrigin"]["kind"] == "completeResRecord"
                stream.write(json.dumps(record, ensure_ascii=False, separators=(",", ":")) + "\n")
        partial.replace(output)
        report["catalogs"].append({"recordType": kind, "records": count, "patchOriginRecords": patched,
            "baseJsonlSha256": hashlib.sha256(source.read_bytes()).hexdigest(),
            "outputSha256": hashlib.sha256(output.read_bytes()).hexdigest(),
            "path": str(output.relative_to(ROOT))})
    (ROOT / "Artifacts/client-catalog-projection.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report["catalogs"], indent=2))


if __name__ == "__main__":
    main()
