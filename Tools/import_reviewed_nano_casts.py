"""Import reviewed casting fields only, corroborated by the older official client."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
report = json.loads((ROOT / 'Artifacts/client-nano-decoding.json').read_text())
path = ROOT / 'Unity/Assets/Resources/AO/self-effects.json'
catalog = json.loads(path.read_text())
source = {item['id']: item for item in report['reviewedItems']}
expected = {26370: (40, 2100, 500, 0, [(122, 61), (129, 61)]),
            26354: (25, 2000, 500, 0, [(122, 31), (129, 31)]),
            70308: (126, 250, 500, 1, [(130, 59), (131, 47)])}
if not any(e['id']==26354 for e in catalog['effects']):
    item=source[26354]
    functions=[f for e in item['events'] for f in e['functions']]
    if len(functions)!=1 or functions[0]['function']!=53045 or functions[0]['arguments']!=[116,10]:
        raise ValueError('Proficiency effect changed')
    catalog['effects'].append({'id':26354,'name':item['name'],'ncu':2,'duration':1800,
        'sourceUrl':'https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe',
        'sourceVersion':'18.8.50_EP1','modifiers':[{'statId':116,'amount':10}],
        'unimplemented':['Other-target casting','Nano acquisition and learning']})
catalog['scope']='Three reviewed self-effect paths; mixed explicit source snapshots, not full nano execution.'
for effect in catalog['effects']:
    if effect['id'] not in expected:continue
    item = source[effect['id']]
    if not item['fullyParsed']:
        raise ValueError('Reviewed nano must be structurally parsed')
    stats, requirements, profession = item['stats'], [], 0
    for action in item['actions']:
        if action['action'] != 3:
            raise ValueError('Unreviewed nano action')
        for req in action['requirements']:
            if req == {'statId': 0, 'value': 0, 'operator': 4}:
                continue
            if req['statId'] == 368 and req['operator'] == 0:
                profession = req['value']
            elif req['operator'] == 2 and req['statId'] in (122, 129, 130, 131):
                requirements.append((req['statId'], req['value'] + 1))
            else:
                raise ValueError(f'Unreviewed requirement: {req}')
    fields = (stats['407'], stats['294'] * 10, stats['210'] * 10, profession, sorted(requirements))
    if fields != expected[effect['id']]:
        raise ValueError('Client/web reviewed casting values disagree')
    if stats['54'] != effect['ncu'] or stats['8'] / 100 != effect['duration']:
        raise ValueError('Client effect duration/NCU differs')
    effect.update(nanoCost=fields[0], castMs=fields[1], rechargeMs=fields[2], requiredProfession=profession,
                  nanoLine=stats['75'], stackingOrder=stats['551'],
                  castRequirements=[{'statId': stat, 'amount': amount} for stat, amount in sorted(requirements)],
                  clientEvidenceVersion=report['clientVersion'], clientPayloadSha256=item['payloadSha256'])
path.write_text(json.dumps(catalog, indent=2) + '\n')
print('PASS: three reviewed casting definitions and nanoline priorities imported')
