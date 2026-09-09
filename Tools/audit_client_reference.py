"""Read-only fingerprint and structural audit of an extracted AO resource database.

Does not import assets or assert compatibility with the current live version.
Index/segment layout adapted from CellAO Extractor.cs at commit
ca77f375a7dabe3be769da94e6c2a3d093344c2c; see Tools/licenses/CellAO.txt.
"""
import argparse
from collections import Counter
from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import struct


def fingerprint(path):
    with path.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    return {'name': path.name, 'bytes': path.stat().st_size, 'sha256': digest}


def read_index(data):
    def u32(offset):
        return struct.unpack_from('<I', data, offset)[0]
    block_offset, segment_size = u32(12), u32(184)
    if not 0 < block_offset < segment_size:
        raise ValueError('Unsupported segment layout')
    node = u32(u32(72))
    visited, records = set(), {}
    while node:
        if node in visited or node + 28 > len(data):
            raise ValueError('Cyclic or out-of-bounds index node')
        visited.add(node)
        following = u32(node)
        count = struct.unpack_from('<h', data, node + 8)[0]
        if count < 0 or node + 28 + 16 * count > len(data):
            raise ValueError('Invalid index entry count')
        for offset in range(node + 28, node + 28 + 16 * count, 16):
            high, low = struct.unpack_from('<II', data, offset)
            kind, instance = struct.unpack_from('>II', data, offset + 8)
            key = kind, instance
            if key in records:
                raise ValueError(f'Duplicate identity {key}')
            records[key] = (high << 32) | low
        node = following
    return records, block_offset, segment_size, len(visited)


def audit(client):
    db = client / 'cd_image/data/db'
    index = db / 'ResourceDatabase.idx'
    records, block_offset, segment_size, nodes = read_index(index.read_bytes())
    paths = sorted(db.glob('ResourceDatabase.dat*'))
    if not paths:
        raise ValueError('No database segments')
    counts, samples = Counter(), []
    selected = {121569, 36783, 36779, 36778, 36787, 95520, 85697}
    with ExitStack() as stack:
        streams = [stack.enter_context(path.open('rb')) for path in paths]

        def read_at(position, length):
            segment = position // segment_size
            offset = position - (segment_size - block_offset) * segment
            result = bytearray()
            while len(result) < length:
                if segment >= len(streams):
                    raise ValueError('Record exceeds available database segments')
                stream = streams[segment]
                stream.seek(offset)
                chunk = stream.read(length - len(result))
                if not chunk:
                    raise ValueError('Invalid or truncated database segment')
                result.extend(chunk)
                segment += 1
                offset = block_offset
            return bytes(result)

        for (kind, instance), position in records.items():
            header = read_at(position, 34)
            actual_kind, actual_instance, stored_size = struct.unpack_from('<III', header, 10)
            if (actual_kind, actual_instance) != (kind, instance) or stored_size < 12:
                raise ValueError(f'Record identity/size mismatch at {position}: {(kind, instance)}')
            # Reading the entire record checks segment bounds, including records crossing files.
            raw = read_at(position, 34 + stored_size - 12)
            counts[kind] += 1
            if kind == 0xF4254 and instance in selected:
                samples.append({'type': kind, 'id': instance, 'payloadBytes': len(raw) - 34,
                                'payloadSha256': hashlib.sha256(raw[34:]).hexdigest()})
    return {
        'schema': 1,
        'clientVersion': (client / 'version.id').read_text().strip(),
        'scope': 'Structural audit only; item semantics and live-version conformance are not verified.',
        'indexNodes': nodes, 'recordCount': len(records),
        'recordTypes': {str(k): v for k, v in sorted(counts.items())},
        'blockOffset': block_offset, 'segmentSize': segment_size,
        'files': [fingerprint(p) for p in [client / 'version.id', index, *paths]],
        'reviewedItemSamples': sorted(samples, key=lambda item: item['id']),
    }


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('client', type=Path, help='Extracted app directory containing version.id')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.client)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(f"CLIENT_AUDIT_OK {result['clientVersion']}: {result['recordCount']} record headers and payload bounds checked")
