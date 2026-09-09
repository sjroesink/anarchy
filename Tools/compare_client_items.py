"""Compare reviewed Unity item fields with independently decoded client records.

This gate covers only listed fields and exact QLs, across different patch snapshots.
Opaque effects and full runtime semantics are explicitly outside this comparison.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def compare():
    client = json.loads((ROOT / 'Artifacts/client-item-decoding.json').read_text())
    unity = json.loads((ROOT / 'Unity/Assets/Resources/AO/executable-items.json').read_text())
    lookup = {item['id']: item for item in client['reviewedItems']}
    checks = []

    def equal(item_id, field, expected, actual):
        checks.append({'id': item_id, 'field': field, 'matches': expected == actual,
                       'client': expected, 'unity': actual})

    for item in unity['items']:
        item_id = item['id']
        if item_id not in lookup:continue  # This gate covers only its seven reviewed equipment samples.
        source = lookup[item_id]
        if not source['fullyParsed']:
            raise ValueError(f'Unparsed reviewed item {item_id}')
        stats = source['stats']
        for field, value in [('name', source['name']), ('ql', stats['54']),
                             ('canFlags', stats['30']), ('equipDelayMs', stats['211'] * 10)]:
            equal(item_id, field, value, item[field])
        base = 16 if stats['76'] == 2 else 0
        slots = [bit + base for bit in range(16) if stats['298'] & (1 << bit)]
        equal(item_id, 'slots', slots, item['slots'])
        requirements = []
        for action in source['actions']:
            if action['action'] not in (6, 8):
                raise ValueError(f'Unreviewed action {action}')
            for req in action['requirements']:
                if req == {'statId': 0, 'value': 0, 'operator': 4}:
                    continue  # Conjunction in this reviewed expression, not a general evaluator.
                if req['operator'] != 2:
                    raise ValueError(f'Unreviewed requirement {req}')
                requirements.append((req['statId'], 'AtLeast', req['value'] + 1))
        equal(item_id, 'requirements', sorted(requirements),
              sorted((r['statId'], r['comparison'], r['amount']) for r in item['requirements']))
        modifiers, appearance = [], []
        for event in source['events']:
            if event['event'] != 14:
                continue  # Weapon visual/action effects are preserved, not treated as modifiers.
            for function in event['functions']:
                if function['requirements'] or (function['ticks'], function['interval'], function['target']) != (1, 0, 2):
                    raise ValueError('Unreviewed conditional/repeating modifier')
                if function['function'] == 53045:
                    modifiers.append(tuple(function['arguments']))
                elif function['function'] == 53039:
                    appearance.append((53039, *function['arguments']))
                else:
                    raise ValueError('Unreviewed On Wear function')
        equal(item_id, 'modifiers', sorted(modifiers), sorted((m['statId'], m['amount']) for m in item['modifiers']))
        equal(item_id, 'appearanceData', appearance,
              [(a['functionId'], a['textureId'], a['layer']) for a in item.get('appearanceActions', [])])
        if 'weapon' in item:
            for field, stat, factor in [('minDamage', 286, 1), ('maxDamage', 285, 1),
                ('criticalBonus', 284, 1), ('damageTypeStatId', 436, 1), ('range', 287, 1),
                ('initiativeStatId', 440, 1), ('attackMs', 294, 10), ('rechargeMs', 210, 10)]:
                equal(item_id, field, stats[str(stat)] * factor, item['weapon'][field])
            groups = {group['group']: group['values'] for group in source['attackDefence']}
            for field, group in [('attackSkills', 12), ('defenceSkills', 13)]:
                equal(item_id, field, sorted((s['statId'], s['weight']) for s in groups[group]),
                      sorted((s['statId'], s['amount']) for s in item['weapon'][field]))
    return {'clientVersion': client['clientVersion'], 'unitySourceVersion': unity['sourceVersion'],
            'scope': 'Exact reviewed fields only; matching values do not prove patch or gameplay parity.',
            'checks': checks, 'passed': sum(check['matches'] for check in checks), 'total': len(checks)}


if __name__ == '__main__':
    report = compare()
    (ROOT / 'Artifacts/client-item-comparison.json').write_text(json.dumps(report, indent=2) + '\n')
    print(f"CLIENT_ITEM_COMPARISON {report['passed']}/{report['total']}")
    raise SystemExit(0 if report['passed'] == report['total'] else 1)
