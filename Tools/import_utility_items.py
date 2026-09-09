"""Compile reviewed static belt/NCU records from captured raw AO Index tables.

Only exact recorded QLs. Reject unknown operators, actions and placement rather
than silently dropping them. Numeric facts remain linked to the raw source.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AO = ROOT / 'Unity/Assets/Resources/AO'
REVIEWED = (36783, 36779, 36778, 36787, 95520)

def compile_item(raw):
    def rows(first):
        table = next(t for t in raw['tables'] if t['columns'][0] == first)
        return [r for r in table['rows'] if r != ['No rows.']]
    base = dict(rows('Key'))
    stats = {int(r[0]): int(r[1]) for r in rows('Stat ID')}
    slots = [i for i in range(32) if stats[298] & (1 << i)]
    if slots not in ([7], list(range(9, 15))) or stats[30] != 5:
        raise ValueError('Unreviewed item placement/capabilities')
    requirements = []
    for r in rows('Type'):
        if r[0] != 'equip' or int(r[2]) != 161 or int(r[3]) != 2:
            raise ValueError('Unreviewed requirement')
        threshold = int(r[4]) + 1
        if r[6] != '>=' or int(r[7]) != threshold:
            raise ValueError('Raw requirement and published interpretation disagree')
        requirements.append(dict(statId=161, comparison='AtLeast', amount=threshold))
    modifiers = []
    effect_table = next(t for t in raw['tables'] if 'Event ID' in t['columns'])
    for r in effect_table['rows']:
        if (r[1:4] != ['14', '53045', '2'] or r[6:8] != ['1', '0']
                or int(r[4]) not in (45, 181) or r[8] != f'{r[4]},{r[5]}'):
            raise ValueError('Unreviewed effect action')
        modifiers.append(dict(statId=int(r[4]), amount=int(r[5])))
    expected = sorted((int(r[0]), int(r[1])) for r in rows('Attribute ID'))
    if sorted((m['statId'], m['amount']) for m in modifiers) != expected:
        raise ValueError('Effect actions and modifier summary disagree')
    return dict(id=raw['aoid'], ql=stats[54], name=base['name'], slots=slots,
                sourceUrl=raw['sourceUrl'], additionalSourceUrl=f"https://www.aogalaxy.com/_items/item.php?aoid={raw['aoid']}",
                equipDelayMs=stats[211]*10, equipDelayKnown=True, canFlags=stats[30],
                noDrop=bool(stats[0] & (1 << 26)), requirements=requirements,
                modifiers=modifiers, unsupportedActions=[])

if __name__ == '__main__':
    compiled = [compile_item(json.loads((AO / f'RawItems/{i}.json').read_text())) for i in REVIEWED]
    path = AO / 'executable-items.json'
    catalog = json.loads(path.read_text())
    catalog['items'] = [i for i in catalog['items'] if i['id'] not in REVIEWED] + compiled
    catalog['scope'] = 'Reviewed exact-QL records; no QL interpolation or full database conformance'
    path.write_text(json.dumps(catalog, indent=2) + '\n', encoding='utf-8')
    print(f'Compiled {len(compiled)} reviewed belt/NCU definitions')
