"""Compile ordinary one-charge UploadNano crystals for supported programs."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'Unity/Assets/Resources/AO/executable-items.json'
catalog=json.loads(path.read_text()); existing={i['id']:i for i in catalog['items']}
supported={i['id'] for i in json.loads((ROOT/'Unity/Assets/Resources/AO/self-effects.json').read_text())['effects']}
added=[];skipped=[]
for line in (ROOT/'Research/ClientReference/items-18.8.50.jsonl').open(encoding='utf-8'):
    i=json.loads(line)
    if not i['name'].startswith('Nano Crystal ('):continue
    fs=[f for e in i['events'] for f in e['functions']]
    if len(fs)!=1 or fs[0]['function']!=53019 or fs[0]['arguments'][0] not in supported:continue
    f=fs[0];s=i['stats'];req=[]
    if not i['fullyParsed'] or s.get('26')!=1 or s.get('298')!=0 or f['requirements'] or f['ticks']!=1 or f['interval']!=0 or f['target']!=2 or len(i['events'])!=1 or i['events'][0]['event']!=0:skipped.append(i['id']);continue
    valid=True
    for a in i['actions']:
        if a['action']!=3:valid=False;break
        for r in a['requirements']:
            if r=={'statId':0,'value':0,'operator':4}:continue
            if r['operator'] not in (0,2):valid=False;break
            req.append({'statId':r['statId'],'comparison':'Equal' if r['operator']==0 else 'AtLeast','amount':r['value']+(r['operator']==2)})
    if not valid:skipped.append(i['id']);continue
    existing[i['id']]={'id':i['id'],'ql':s['54'],'name':i['name'],'slots':[],
        'sourceUrl':'https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe',
        'sourceVersion':'18.8.50_EP1','clientPayloadSha256':i['payloadSha256'],
        'equipDelayKnown':False,'equipDelayMs':0,'canFlags':s['30'],'noDrop':bool(s['0']&64),
        'requirements':req,'modifiers':[],'unsupportedActions':[],'uploadNanoId':f['arguments'][0]}
    added.append(i['id'])
catalog['items']=list(existing.values());catalog['scope']='Reviewed exact-QL equipment and ordinary upload crystals; full item behavior incomplete.'
path.write_text(json.dumps(catalog,indent=2)+'\n')
(ROOT/'Artifacts/nano-crystal-import.json').write_text(json.dumps({'compiled':added,'skipped':skipped,'catalogCount':len(existing)},indent=2)+'\n')
print(f'Compiled {len(added)} crystals; skipped {len(skipped)}; item catalog {len(existing)}')
