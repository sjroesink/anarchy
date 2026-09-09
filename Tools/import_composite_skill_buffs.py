"""Compile reviewed +20 general composite skill programs from client records.

Martial Prowess has additional combat modifiers whose behavior is not covered.
This importer does not invent crystal upload or world acquisition semantics.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
EXPECTED={
    215264:([147,146,142,144,101,100],[66,120,41,564,547,85],61),
    223348:([112,116,114,115,113,111,133,109,110],[103,27,83,118,112,37],61),
    223360:([102,107,103,105,106,104],[16,23,21,25,101,61],61),
    223364:([151,167,148,150,121,134],[18,73,44,71,39,581],61),
    223380:([127,128,129,122,131,130],[91,35,108,116,87,89],61),
    287040:([125,126,157,158,159,163,160],[93,60,68,134,100,47],20),
    287046:([164,136,165,161,124,123,162],[51,949,43,50,70,125],4),
}

def compile_record(record):
    skills,lines,minimum=EXPECTED[record['id']]
    s=record['stats']
    if not record['fullyParsed'] or len(record['events'])!=1 or record['events'][0]['event']!=0:
        raise ValueError('Unsupported composite event')
    functions=record['events'][0]['functions']
    if (len(functions)!=len(skills) or [f['arguments'] for f in functions]!=[[n,20] for n in skills]
        or any(f['function']!=53045 or f['target']!=3 or f['ticks']!=1 or f['interval']!=0
               or f['requirements'] or f['opaqueSegments'] for f in functions)):
        raise ValueError('Unsupported composite modifier')
    if [s[str(n)] for n in (0,8,54,210,294,407,551)]!=[65536,2880000,4,100,100,1,10]:
        raise ValueError('Unexpected composite execution fields')
    if [s[str(n)] for n in (75,546,547,548,549,550)]!=lines:
        raise ValueError('Unexpected composite stacking lines')
    if record['actions']!=[{'action':3,'requirements':[{'statId':129,'value':minimum-1,'operator':2},
        {'statId':122,'value':minimum-1,'operator':2},{'statId':0,'value':0,'operator':4}]}]:
        raise ValueError('Unexpected composite cast requirements')
    origin=record['referenceOrigin']
    return {'id':record['id'],'name':record['name'],'ncu':s['54'],'duration':s['8']/100,
        'nanoCost':s['407'],'castMs':s['294']*10,'rechargeMs':s['210']*10,'requiredProfession':0,
        'nanoLine':lines[0],'additionalNanoLines':lines[1:],'stackingOrder':s['551'],
        'castRequirements':[{'statId':129,'amount':minimum},{'statId':122,'amount':minimum}],
        'modifiers':[{'statId':n,'amount':20} for n in skills],
        'sourceVersion':origin['version'],'clientEvidenceVersion':origin['version'],
        'sourceUrl':origin.get('url','https://update.anarchy-online.com/download/AO/18.8.50_EP1/install.exe'),
        'clientPayloadSha256':record['payloadSha256'],
        'unimplemented':['Other-target casting','World acquisition','Original-client verification of cross-line replacement']}

def main():
    source=ROOT/'Research/ClientReference/Catalog-through-18.8.62/nanos.jsonl'
    compiled=[compile_record(r) for r in map(json.loads,source.open(encoding='utf-8')) if r['id'] in EXPECTED]
    if len(compiled)!=len(EXPECTED):raise ValueError('Missing composite source record')
    path=ROOT/'Unity/Assets/Resources/AO/self-effects.json'
    catalog=json.loads(path.read_text())
    catalog['effects']=[e for e in catalog['effects'] if e['id'] not in EXPECTED]+compiled
    catalog['scope']='Reviewed single-skill and seven composite skill buffs, Body Boost; partial TMS. Full nano execution incomplete.'
    path.write_text(json.dumps(catalog,indent=2)+'\n')
    (ROOT/'Artifacts/composite-skill-import.json').write_text(json.dumps({'compiled':compiled,
        'excluded':[{'id':302158,'reason':'Martial Prowess also changes combat modifiers not covered by this skill compiler'}]},indent=2)+'\n')
    print(f'Imported {len(compiled)} composite skill programs; catalog {len(catalog["effects"])}')

if __name__=='__main__':main()
