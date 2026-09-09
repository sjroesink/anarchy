"""Corruption checks against actual locally extracted client record bytes."""
from pathlib import Path
import struct
import unittest

from decode_client_items import decode, records


class ClientDecoderTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        client = Path(__file__).resolve().parents[1] / 'Research/ClientReference/18.8.50/app'
        wanted = {36783, 25820, 25885, 42705, 120827, 257705, 281588, 283566, 283580, 297053}
        cls.payloads = {}
        for instance, data in records(client):
            if instance in wanted:
                cls.payloads[instance] = data
            if len(cls.payloads) == len(wanted):
                break
        cls.payload = cls.payloads[36783]

    def function(self, item_id, function_id):
        record = decode(item_id, self.payloads[item_id])
        self.assertTrue(record['fullyParsed'], record.get('unparsed'))
        return next(f for event in record['events'] for f in event['functions'] if f['function']==function_id)

    def test_empty_text_does_not_consume_next_string_length(self):
        args=self.function(120827,53134)['arguments']
        self.assertEqual(args[0]['declaredLength'],0)
        self.assertEqual(args[0]['bytesHex'],'')
        self.assertEqual(args[1]['text'],'You have been given a new name.')
        self.assertEqual(args[2],10)

    def test_interface_preserves_two_integer_arguments(self):
        args=self.function(257705,53115)['arguments']
        self.assertEqual(args[0]['text'],'')
        self.assertEqual(args[1:],[0,1])

    def test_key_flags_preserve_full_word(self):
        # 18.8.50 wire tail is 8f 02 17 c0; older parser comments show another value.
        self.assertEqual(self.function(281588,53235)['arguments'],[100001,71546,0xC017028F])

    def test_npc_movement_argument(self):
        self.assertEqual(self.function(283566,53191)['arguments'],[30])
        self.assertEqual(self.function(283580,53191)['arguments'],[37])

    def test_mail_trailing_integer_arguments(self):
        self.assertEqual(self.function(297053,53252)['arguments'][-3:],[1,1,0])

    def test_client_string_argument(self):
        record = decode(25820, self.payloads[25820])
        self.assertTrue(record['fullyParsed'])
        strings = [a for e in record['events'] for f in e['functions'] for a in f['arguments']
                   if isinstance(a, dict) and a['type'] == 'string']
        self.assertEqual(strings[0]['text'], 'Your body tingles with energy.')
        self.assertEqual(strings[0]['declaredLength'], 31)

    def test_client_hash_order(self):
        record = decode(42705, self.payloads[42705])
        self.assertTrue(record['fullyParsed'])
        hashes = [a for e in record['events'] for f in e['functions'] for a in f['arguments']
                  if isinstance(a, dict) and a['type'] == 'hash']
        self.assertEqual(hashes[0], {'type': 'hash', 'bytesHex': '4e494e47', 'reversedAscii': 'GNIN'})

    def test_client_shop_entries(self):
        record = decode(25885, self.payloads[25885])
        self.assertTrue(record['fullyParsed'])
        entries = record['shopHashes'][0]['entries']
        self.assertEqual(len(entries), 4)
        self.assertEqual([e['hashBytesHex'] for e in entries], ['4f4d4d41', '43454a45', '56574d41', '4e4b4d41'])

    def test_wrong_string_terminator_retained_as_unparsed(self):
        data = bytearray(self.payloads[25820])
        text = b'Your body tingles with energy.'
        offset = data.index(text) + len(text)
        self.assertEqual(data[offset], 0)
        data[offset] = 1
        record = decode(25820, data)
        self.assertFalse(record['fullyParsed'])
        self.assertEqual(record['unparsed']['reason'], 'String length/terminator mismatch')

    def test_actual_belt_record(self):
        record = decode(36783, self.payload)
        self.assertTrue(record['fullyParsed'])
        self.assertEqual(record['stats']['45'], 1)
        self.assertEqual(record['actions'][0]['requirements'], [{'statId': 161, 'value': 5, 'operator': 2}])

    def test_bad_stat_counter_rejected(self):
        data = bytearray(self.payload)
        struct.pack_into('<I', data, 16, 123)
        with self.assertRaisesRegex(ValueError, '3F1'):
            decode(36783, data)

    def test_truncated_stat_table_rejected(self):
        with self.assertRaisesRegex(ValueError, 'Truncated'):
            decode(36783, self.payload[:30])

    def test_truncated_requirement_cannot_be_complete(self):
        record = decode(36783, self.payload[:-1])
        self.assertFalse(record['fullyParsed'])
        self.assertIn('Truncated', record['unparsed']['reason'])

    def test_unknown_suffix_preserved(self):
        record = decode(36783, self.payload + struct.pack('<I', 999999))
        self.assertFalse(record['fullyParsed'])
        self.assertEqual(record['unparsed']['bytes'], 4)
        self.assertEqual(record['unparsed']['offset'], len(self.payload))


if __name__ == '__main__':
    unittest.main()
