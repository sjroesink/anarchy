"""Import the reviewed six-skill Composite Nano Expertise complete record."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Research/ClientReference/Catalog-through-18.8.62/nanos.jsonl'
record=next(r for r in map(json.loads,source.open(encoding='utf-8')) if r['id']==223380)
s=record['stats']; functions=record['events'][0]['functions']
assert record['fullyParsed'] and len(record['events'])==1 and record['events'][0]['event']==0
assert s['0']==65536
assert len(functions)==6
assert {tuple(f['arguments']) for f in functions}=={(n,20) for n in (127,128,129,122,131,130)}
assert all(f['function']==53045 and f['target']==3 and f['ticks']==1 and f['interval']==0 and not f['requirements'] for f in functions)
assert record['actions']==[{'action':3,'requirements':[{'statId':129,'value':60,'operator':2},
    {'statId':122,'value':60,'operator':2},{'statId':0,'value':0,'operator':4}]}]
assert [s[str(n)] for n in (8,54,210,294,407,75,546,547,548,549,550,551)]==[2880000,4,100,100,1,91,35,108,116,87,89,10]
origin=record['referenceOrigin']
effect={'id':record['id'],'name':record['name'],'ncu':s['54'],'duration':s['8']/100,
    'nanoCost':s['407'],'castMs':s['294']*10,'rechargeMs':s['210']*10,'requiredProfession':0,
    'nanoLine':s['75'],'additionalNanoLines':[s[str(n)] for n in range(546,551)],'stackingOrder':s['551'],
    'castRequirements':[{'statId':129,'amount':61},{'statId':122,'amount':61}],
    'modifiers':[{'statId':f['arguments'][0],'amount':f['arguments'][1]} for f in functions],
    'sourceVersion':origin['version'],'clientEvidenceVersion':origin['version'],
    'sourceUrl':origin.get('url','https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe'),
    'clientPayloadSha256':record['payloadSha256'],
    'unimplemented':['Other-target casting','World acquisition','Original-client verification of cross-line replacement']}
path=ROOT/'Unity/Assets/Resources/AO/self-effects.json'
catalog=json.loads(path.read_text());catalog['effects']=[e for e in catalog['effects'] if e['id']!=effect['id']]+[effect]
catalog['scope']='Reviewed single-skill buffs, Composite Nano Expertise and Body Boost; partial TMS. Full nano execution incomplete.'
path.write_text(json.dumps(catalog,indent=2)+'\n')
(ROOT/'Artifacts/composite-nano-import.json').write_text(json.dumps(effect,indent=2)+'\n')
print('Imported Composite Nano Expertise: six skills, six lines, 4 NCU, 28800 seconds')
