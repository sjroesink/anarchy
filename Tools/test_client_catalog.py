import json
import unittest

from build_client_catalog import ROOT, REFERENCE, merge_records, patch_records, read_jsonl


class CatalogTests(unittest.TestCase):
    def test_type_collision_and_replacement(self):
        replacements = {(1000020, 7): {"id": 7, "name": "new item"},
                        (1040005, 7): {"id": 7, "name": "different nano"}}
        result = list(merge_records([{"id": 7, "name": "old item"}], replacements, 1000020))
        self.assertEqual(result, [{"id": 7, "name": "new item"}])
        self.assertEqual(len(replacements), 2)

    def test_additions_preserve_unmodified_base(self):
        original = {"id": 9, "stats": {"1": 20}}
        result = list(merge_records([original], {(1000020, 10): {"id": 10}}, 1000020))
        self.assertEqual([r["id"] for r in result], [9, 10])
        self.assertEqual(result[0]["stats"], original["stats"])
        self.assertEqual(result[0]["referenceOrigin"]["version"], "18.8.50_EP1")
        self.assertNotIn("referenceOrigin", original)

    def test_duplicate_base_rejected(self):
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            list(merge_records([{"id": 1}, {"id": 1}], {}, 1000020))

    def test_missing_patch_rejected(self):
        acquisition = json.loads((ROOT / "Artifacts/client-patch-acquisition.json").read_text())
        acquisition["patches"].pop(1)
        with self.assertRaisesRegex(ValueError, "contiguous"):
            patch_records(acquisition, REFERENCE / "Patches")

    def test_modified_source_hash_rejected(self):
        acquisition = json.loads((ROOT / "Artifacts/client-patch-acquisition.json").read_text())
        for entry in acquisition["patches"][0]["files"]:
            if entry["path"].endswith(".RES"):
                entry["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "fingerprint"):
            patch_records(acquisition, REFERENCE / "Patches")

    def test_whole_catalog_against_separate_inspection(self):
        # Compare every projected patch record with the earlier RES inspection,
        # and every unmodified record with the original decoded base catalogue.
        inspected = {(r["recordType"], r["id"]): r for r in read_jsonl(
            REFERENCE / "patch-item-nano-records-through-18.8.62.jsonl")}
        for kind, name in [(1000020, "items"), (1040005, "nanos")]:
            actual = {r["id"]: r for r in read_jsonl(REFERENCE / "Catalog-through-18.8.62" / f"{name}.jsonl")}
            expected_ids = set()
            for base in read_jsonl(REFERENCE / f"{name}-18.8.50.jsonl"):
                identity = base["id"]
                expected_ids.add(identity)
                if (kind, identity) not in inspected:
                    projected = actual[identity]
                    self.assertEqual({k: v for k, v in projected.items()
                                      if k not in ("recordType", "referenceOrigin")}, base)
            for (record_kind, identity), record in inspected.items():
                if record_kind != kind:
                    continue
                expected_ids.add(identity)
                projected = actual[identity]
                self.assertEqual(projected["referenceOrigin"]["version"], record["patchTo"] + "_EP1")
                self.assertEqual({k: v for k, v in projected.items() if k != "referenceOrigin"},
                                 {k: v for k, v in record.items() if k != "patchTo"})
            self.assertEqual(set(actual), expected_ids)


if __name__ == "__main__":
    unittest.main()
