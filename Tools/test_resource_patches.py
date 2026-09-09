"""Verify observed RES framing and rejection of corrupt/truncated envelopes."""
import struct
import unittest
import zlib
from pathlib import Path

from inspect_resource_patches import unpack


class ResourcePatchTests(unittest.TestCase):
    def setUp(self):
        self.path = next(Path("Research/ClientReference/Patches/18.8.61-18.8.62").rglob("*.RES"))
        self.data = self.path.read_bytes()

    def test_real_record(self):
        records = unpack(self.data)
        self.assertEqual(len(records), 1)
        kind, identity, mode, payload = records[0]
        self.assertEqual((kind, identity, mode, len(payload)), (1000020, 294016, "completeRecord", 319))
        self.assertIn(b"Michizure's T-shirt Spawner", payload)

    def test_truncated_at_every_byte(self):
        for length in range(len(self.data)):
            with self.subTest(length=length), self.assertRaises((ValueError, zlib.error)):
                unpack(self.data[:length])

    def test_invalid_count(self):
        data = bytearray(self.data)
        struct.pack_into(">I", data, 4, 2)
        with self.assertRaisesRegex(ValueError, "header"):
            unpack(data)

    def test_identity_mismatch(self):
        data = bytearray(self.data)
        struct.pack_into(">I", data, 20, 294017)
        with self.assertRaisesRegex(ValueError, "identity"):
            unpack(data)

    def test_wrong_expanded_length(self):
        for length in (318, 320):
            data = bytearray(self.data)
            struct.pack_into(">I", data, 24, length)
            with self.assertRaisesRegex(ValueError, "compressed"):
                unpack(data)

    def test_trailing_bytes(self):
        with self.assertRaisesRegex(ValueError, "Trailing"):
            unpack(self.data + b"\x00")

    def test_extra_zlib_stream(self):
        data = bytearray(self.data + zlib.compress(b"unexpected"))
        struct.pack_into(">I", data, 28, len(data) - 32)
        with self.assertRaisesRegex(ValueError, "compressed"):
            unpack(data)

    def test_real_delta_patch(self):
        path = next(Path("Research/ClientReference/Patches/18.8.52-18.8.53").rglob("*.RES"))
        records = unpack(path.read_bytes())
        self.assertEqual(len(records), 25)
        self.assertEqual(sum(mode == "opaqueRTPatch" for _, _, mode, _ in records), 4)


if __name__ == "__main__":
    unittest.main()
