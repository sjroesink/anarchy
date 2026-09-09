"""Project original-instruction results into Unity-readable comparison fixtures."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=root/'Artifacts/special-lock-lookup-emulation.json'
raw=source.read_bytes();report=json.loads(raw)
if report['sourceSha256']!='8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320':raise ValueError('Unreviewed reference')
def entry(row):return dict(zip(['opaque','skill','duration','remaining'],row))
def initial(rows):return [entry([123+i,skill,456+i,remaining]) for i,(skill,remaining) in enumerate(rows)]
fixtures=[]
for r in report['results']:
 fixtures.append(dict(operation='read',initial=initial(r['entries']),skill=r['skill'],membership=r['membership']!=0,remaining=r['rawRemaining']))
for r in report['tickResults']:
 fixtures.append(dict(operation='tick',initial=initial(r['entries']),ticks=r['tickCalls'],expected=[entry(e) for e in r['result']]))
for r in report['applicationResults']:
 fixtures.append(dict(operation='extend' if r['extend'] else 'insert',initial=initial(r['entries']),skill=148,duration=r['durationInput'],inserted=bool(r['notifications']),expected=[entry(e) for e in r['result']]))
(root/'Artifacts/unity-skill-lock-fixtures.json').write_text(json.dumps(dict(sourceReportSha256=hashlib.sha256(raw).hexdigest(),fixtures=fixtures),indent=2)+'\n')
print('Exported',len(fixtures),'original-result fixtures')
