"""Compile the reviewed Soldier Body Boost program and its startup crystal."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def find(kind,identity):
    for line in (ROOT/f'Research/ClientReference/{kind}-18.8.50.jsonl').open(encoding='utf-8'):
        value=json.loads(line)
        if value['id']==identity:return value
    raise ValueError('Missing source record')
nano=find('nanos',29091);crystal=find('items',29092);stats=nano['stats']
assert nano['fullyParsed'] and crystal['fullyParsed']
assert nano['events'][0]['functions'][0]['arguments']==[1,20]
assert crystal['events'][0]['functions'][0]['arguments']==[29091]
assert (stats['8'],stats['54'],stats['294'],stats['210'],stats['407'])==(1440000,1,300,400,11)
expected=[{'statId':128,'value':4,'operator':2},{'statId':130,'value':4,'operator':2},
          {'statId':0,'value':0,'operator':4},{'statId':368,'value':1,'operator':0},{'statId':0,'value':0,'operator':4}]
assert nano['actions']==crystal['actions']==[{'action':3,'requirements':expected}]
url='https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe'
effect={'id':29091,'name':nano['name'],'ncu':stats['54'],'duration':stats['8']/100,
        'nanoCost':stats['407'],'castMs':stats['294']*10,'rechargeMs':stats['210']*10,
        'nanoLine':stats['75'],'stackingOrder':stats['551'],'requiredProfession':1,
        'castRequirements':[{'statId':128,'amount':5},{'statId':130,'amount':5}],
        'modifiers':[{'statId':1,'amount':20}], 'sourceUrl':url,'sourceVersion':'18.8.50_EP1',
        'clientEvidenceVersion':'18.8.50_EP1','clientPayloadSha256':nano['payloadSha256'],
        'unimplemented':['Other-target casting','Full Arete acquisition','Original-client current-HP transition fixtures']}
item={'id':29092,'ql':1,'name':crystal['name'],'slots':[],'uploadNanoId':29091,
      'equipDelayKnown':False,'equipDelayMs':0,'canFlags':41,'noDrop':bool(crystal['stats']['0']&64),
      'sourceUrl':url,'sourceVersion':'18.8.50_EP1','clientPayloadSha256':crystal['payloadSha256'],
      'requirements':[{'statId':128,'comparison':'AtLeast','amount':5},{'statId':130,'comparison':'AtLeast','amount':5},
                      {'statId':368,'comparison':'Equal','amount':1}], 'modifiers':[],'unsupportedActions':[]}
for name,key,record in [('self-effects','effects',effect),('executable-items','items',item)]:
    p=ROOT/f'Unity/Assets/Resources/AO/{name}.json';data=json.loads(p.read_text())
    data[key]=[v for v in data[key] if v['id']!=record['id']]+[record]
    p.write_text(json.dumps(data,indent=2)+'\n')
print('PASS: Body Boost 29091 and startup crystal 29092 compiled; no acquisition grant invented')
