"""Decode AO client item stats; retain unparsed data instead of inventing semantics.

Record layout adapted from CellAO Extractor.cs, NewParser.cs and
Structs/HLFlat3F1Counter.cs at ca77f375a7dabe3be769da94e6c2a3d093344c2c.
Copyright (c) 2005-2013, CellAO Team. BSD terms: licenses/CellAO.txt.
"""
import argparse
from collections import Counter
from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import struct

from audit_client_reference import read_index

# CellAO table, same source commit and BSD attribution as the parser layout.
FUNCTION_LAYOUTS = dict(line.strip().split('=', 1) for line in
    Path(__file__).with_name('client-function-layouts.cfg').read_text().splitlines() if '=' in line)


class Reader:
    def __init__(self, data):
        self.data, self.offset = data, 0

    def take(self, size):
        if size < 0 or self.offset + size > len(self.data):
            raise ValueError('Truncated item payload')
        result = self.data[self.offset:self.offset + size]
        self.offset += size
        return result

    def u32(self):
        return struct.unpack('<I', self.take(4))[0]

    def count(self):
        encoded = self.u32()
        if not encoded or encoded % 1009:
            raise ValueError('Invalid 3F1 count')
        return encoded // 1009 - 1

    def requirements(self, count):
        return [{'statId': self.u32(), 'value': self.u32(), 'operator': self.u32()}
                for _ in range(count)]


def decode(instance, data):
    r = Reader(data)
    header = [r.u32() for _ in range(4)]
    stats = {}
    for _ in range(r.count()):
        key, value = r.u32(), r.u32()
        if str(key) in stats:
            raise ValueError('Duplicate stat')
        stats[str(key)] = value  # Unsigned wire values, no silent signed conversion.
    text_header = [r.u32(), r.u32()]
    name_size, description_size = struct.unpack('<HH', r.take(4))
    name = r.take(name_size).decode('cp1252')
    description = r.take(description_size).decode('cp1252')
    result = {'id': instance, 'name': name, 'description': description,
              'header': header, 'textHeader': text_header, 'stats': stats,
              'payloadSha256': hashlib.sha256(data).hexdigest(),
              'actions': [], 'events': [], 'attackDefence': [], 'animationSound': []}
    while r.offset < len(data):
        start = r.offset
        try:
            kind = r.u32()
            if kind == 22:
                if r.u32() != 36:
                    raise ValueError('Unknown action block header')
                for _ in range(r.count()):
                    action = r.u32()
                    result['actions'].append({'action': action, 'requirements': r.requirements(r.count())})
            elif kind == 2:
                event = r.u32()
                functions = []
                for _ in range(r.count()):
                    function = r.u32()
                    layout = FUNCTION_LAYOUTS.get(str(function))
                    if layout is None:
                        raise ValueError(f'Unimplemented function {function}')
                    prefix = [r.u32(), r.u32()]
                    requirements = r.requirements(r.u32())
                    ticks, interval, target, marker = [r.u32() for _ in range(4)]
                    entry = {'function': function, 'prefix': prefix,
                                      'requirements': requirements, 'ticks': ticks,
                                      'interval': interval, 'target': target, 'marker': marker,
                                      'arguments': [], 'argumentLayout': layout, 'opaqueSegments': []}
                    for segment in layout.split(','):
                        amount, code = int(segment[:-1]), segment[-1]
                        if code == 'n':
                            entry['arguments'].extend(r.u32() for _ in range(amount))
                        elif code == 'x':
                            entry['opaqueSegments'].append({'afterArgument': len(entry['arguments']),
                                                           'hex': r.take(amount).hex()})
                        elif code == 'h':
                            for _ in range(amount):
                                raw = r.take(4)
                                entry['arguments'].append({'type': 'hash', 'bytesHex': raw.hex(),
                                    'reversedAscii': raw[::-1].decode('ascii', errors='backslashreplace')})
                        elif code == 's':
                            for _ in range(amount):
                                length = r.u32()
                                # The length includes a terminator only when nonzero.
                                # Empty strings occupy just their length word in actual records.
                                raw = r.take(length)
                                if length and (raw[-1:] != b'\0' or b'\0' in raw[:-1]):
                                    raise ValueError('String length/terminator mismatch')
                                entry['arguments'].append({'type': 'string', 'bytesHex': raw.hex(),
                                    'declaredLength': length,
                                    'text': raw[:-1].decode('utf-8', errors='backslashreplace')})
                        else:
                            raise ValueError(f'Unimplemented argument layout {function}: {layout}')
                    functions.append(entry)
                result['events'].append({'event': event, 'functions': functions})
            elif kind == 4:
                result['attackDefenceMarker'] = r.u32()
                for _ in range(r.count()):
                    group = r.u32()
                    values = [{'statId': r.u32(), 'weight': r.u32()} for _ in range(r.count())]
                    result['attackDefence'].append({'group': group, 'values': values})
            elif kind in (14, 20):
                marker = r.u32()
                values = []
                for _ in range(r.count()):
                    action = r.u32()
                    values.append({'action': action, 'values': [r.u32() for _ in range(r.count())]})
                result['animationSound'].append({'kind': kind, 'marker': marker, 'values': values})
            elif kind == 6:
                marker = r.u32()
                result.setdefault('unknown6', []).append({'marker': marker,
                    'pairs': [[r.u32(), r.u32()] for _ in range(r.count())]})
            elif kind == 23:
                event = r.u32()
                entries = []
                for _ in range(r.count()):
                    raw_hash = r.take(4)
                    low, high = r.take(2)
                    extended = low == high == 0
                    if extended:
                        low, high = struct.unpack('<HH', r.take(4))
                    entries.append({'hashBytesHex': raw_hash.hex(), 'low': low, 'high': high,
                                    'extended': extended, 'opaqueTailHex': r.take(11).hex()})
                result.setdefault('shopHashes', []).append({'event': event, 'entries': entries})
            else:
                raise ValueError(f'Unimplemented block {kind}')
        except ValueError as error:
            result['unparsed'] = {'offset': start, 'bytes': len(data) - start,
                                  'reason': str(error), 'sha256': hashlib.sha256(data[start:]).hexdigest()}
            break
    result['fullyParsed'] = 'unparsed' not in result
    return result


def records(client, record_type=1000020):
    db = client / 'cd_image/data/db'
    index, block, size, _ = read_index((db / 'ResourceDatabase.idx').read_bytes())
    with ExitStack() as stack:
        files = [stack.enter_context(p.open('rb')) for p in sorted(db.glob('ResourceDatabase.dat*'))]
        for (kind, instance), position in index.items():
            if kind != record_type:
                continue
            part = position // size
            offset = position - (size - block) * part

            def take(count):
                nonlocal part, offset
                result = bytearray()
                while len(result) < count:
                    if part >= len(files):
                        raise ValueError('Missing database segment')
                    files[part].seek(offset)
                    chunk = files[part].read(count - len(result))
                    if not chunk:
                        raise ValueError('Truncated database segment')
                    result.extend(chunk)
                    offset += len(chunk)
                    if len(result) < count:
                        part += 1
                        offset = block
                return bytes(result)

            header = take(34)
            actual_kind, actual_id, length = struct.unpack_from('<III', header, 10)
            if (actual_kind, actual_id) != (kind, instance) or length < 12:
                raise ValueError('Record header mismatch')
            yield instance, take(length - 12)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('client', type=Path)
    parser.add_argument('--output', type=Path, required=True, help='Local JSONL reference, outside Unity')
    parser.add_argument('--report', type=Path, required=True)
    parser.add_argument('--record-type', type=int, choices=(1000020, 1040005), default=1000020)
    args = parser.parse_args()
    reviewed = ({121569, 36783, 36779, 36778, 36787, 95520, 85697}
                if args.record_type == 1000020 else {26354, 26370, 70308})
    reasons, failures, samples, unresolved = Counter(), [], [], []
    count = full = 0
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('w', encoding='utf-8') as out:
        for instance, payload in records(args.client, args.record_type):
            count += 1
            try:
                item = decode(instance, payload)
            except (ValueError, UnicodeDecodeError) as error:
                failures.append({'id': instance, 'reason': str(error)})
                continue
            full += item['fullyParsed']
            if not item['fullyParsed']:
                reasons[item['unparsed']['reason']] += 1
                unresolved.append({'id': instance, **item['unparsed']})
            out.write(json.dumps(item, ensure_ascii=False) + '\n')
            if instance in reviewed:
                # No original item descriptions in the tracked numerical evidence report.
                samples.append({k: v for k, v in item.items() if k != 'description'})
    report = {'schema': 2, 'recordType': args.record_type, 'clientVersion': (args.client / 'version.id').read_text().strip(),
              'scope': 'Decoded wire data, not executable semantics or live-version conformance.',
              'records': count, 'decodedStatTables': count - len(failures), 'fullyParsedRecords': full,
              'unparsedReasons': dict(reasons.most_common()), 'failures': failures,
              'unresolvedRecords': unresolved,
              'reviewedItems': sorted(samples, key=lambda item: item['id'])}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k not in ('reviewedItems', 'failures', 'unresolvedRecords')}))


if __name__ == '__main__':
    main()
