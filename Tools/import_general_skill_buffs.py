"""Compile client general skill buffs whose complete execution shape is supported.

Preserves values per record; missing timings and composite programs are reported,
never assigned guessed defaults. Does not provide nano learning or other targets.
"""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'Unity/Assets/Resources/AO/self-effects.json'
catalog=json.loads(path.read_text())
existing={item['id']:item for item in catalog['effects']}
compiled=[]; skipped=[]
for line in (ROOT/'Research/ClientReference/nanos-18.8.50.jsonl').open(encoding='utf-8'):
    item=json.loads(line)
    if not item['name'].endswith((' Expertise',' Proficiency')):continue
    reason=None; stats=item['stats']; events=item['events']; actions=item['actions']
    if item['name'].startswith('Composite '):reason='Composite replacement rules are not implemented'
    elif not item['fullyParsed']:reason='Incomplete record'
    elif any(str(k) not in stats for k in (8,54,75,210,294,407,551)):reason='Missing explicit cast fields'
    elif stats['0']!=2147549184:reason='Unreviewed flags'
    elif len(events)!=1 or events[0]['event']!=0 or len(events[0]['functions'])!=1:reason='Unsupported event shape'
    if reason is None:
        f=events[0]['functions'][0]
        if f['function']!=53045 or f['target']!=3 or f['requirements'] or f['ticks']!=1 or f['interval']!=0 or len(f['arguments'])!=2 or f['arguments'][1] not in (10,20):reason='Unsupported modifier shape'
    if reason is None:
        if len(actions)!=1 or actions[0]['action']!=3:reason='Unsupported cast action'
        else:
            req=actions[0]['requirements']
            if len(req)!=3 or req[-1]!={'statId':0,'value':0,'operator':4} or any(r['operator']!=2 or r['statId'] not in (122,128,129) for r in req[:2]):reason='Unsupported requirement expression'
    if reason:
        skipped.append({'id':item['id'],'name':item['name'],'reason':reason});continue
    result={'id':item['id'],'name':item['name'],'ncu':stats['54'],'duration':stats['8']/100,
        'sourceVersion':'18.8.50_EP1','sourceUrl':'https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe',
        'nanoCost':stats['407'],'castMs':stats['294']*10,'rechargeMs':stats['210']*10,'requiredProfession':0,
        'nanoLine':stats['75'],'stackingOrder':stats['551'],
        'castRequirements':[{'statId':r['statId'],'amount':r['value']+1} for r in req[:2]],
        'modifiers':[{'statId':f['arguments'][0],'amount':f['arguments'][1]}],
        'clientEvidenceVersion':'18.8.50_EP1','clientPayloadSha256':item['payloadSha256'],
        'unimplemented':['Other-target casting','Nano acquisition and learning','Composite replacement interactions']}
    if item['id'] in existing:
        old=existing[item['id']]
        for key in ('ncu','duration','nanoCost','castMs','rechargeMs','modifiers'):
            if old[key]!=result[key]:raise ValueError(f'Existing definition mismatch {item["id"]}: {key}')
    existing[item['id']]=result;compiled.append(item['id'])
catalog['effects']=list(existing.values())
catalog['scope']='Reviewed general single-skill self buffs and partial TMS; full nano execution incomplete.'
path.write_text(json.dumps(catalog,indent=2)+'\n')
report={'sourceVersion':'18.8.50_EP1','compiled':compiled,'skipped':skipped,'catalogCount':len(existing)}
(ROOT/'Artifacts/general-skill-buff-import.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'Compiled {len(compiled)} general buffs; catalog {len(existing)}; unresolved {len(skipped)}')
