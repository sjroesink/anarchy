"""Read local reference placements; no original assets are added to Unity.

Statel prefix layout adapted from CellAO PlayfieldParser/StatelDataExtractor/
PFCoordHeading. Copyright (c) 2005-2013 CellAO Team; BSD: licenses/CellAO.txt.
Unparsed event payloads are preserved, not assigned guessed meanings.
"""
import hashlib,json,math,struct
from pathlib import Path
from decode_client_items import records,decode

root=Path(__file__).resolve().parents[1]
client=root/'Research/ClientReference/18.8.50/app'
out=root/'Research/Borealis/ClientLayout';out.mkdir(parents=True,exist_ok=True)
payload=next(data for identity,data in records(client,1000026) if identity==800)
(out/'1000026-800.bin').write_bytes(payload)
count=struct.unpack_from('<I',payload)[0];offset=4;placements=[]
for index in range(count):
    size=struct.unpack_from('<I',payload,offset)[0];offset+=4
    data=payload[offset:offset+size];offset+=size
    if len(data)!=size or size<64:raise ValueError('Truncated statel')
    kind,identity,unknown,*_=struct.unpack_from('<3I',data)
    playfield=struct.unpack_from('<I',data,20)[0]
    position=struct.unpack_from('<3f',data,24)
    heading=struct.unpack_from('<4f',data,36)
    template,event_size=struct.unpack_from('<II',data,56)
    if playfield!=800 or not all(math.isfinite(v) for v in position+heading):raise ValueError('Unexpected coordinate prefix')
    if 64+event_size!=size:raise ValueError('Unreviewed statel suffix')
    placements.append(dict(identityType=kind,identity=identity,templateId=template,
                           positionRaw=list(position),headingRaw=list(heading),
                           unparsedEventBytes=event_size,payloadSha256=hashlib.sha256(data).hexdigest()))
if offset!=len(payload):raise ValueError('Unconsumed playfield bytes')
templates={p['templateId'] for p in placements}
names={identity:decode(identity,data)['name'] for identity,data in records(client) if identity in templates}
for placement in placements:placement['templateName']=names.get(placement['templateId'])
report=dict(clientVersion='18.8.50',resourceType=1000026,playfieldId=800,
            sha256=hashlib.sha256(payload).hexdigest(),count=count,
            positionOrder='X,Y,Z; X/Z match community horizontal coordinate pairs',headingOrder='W,Z,Y,X per CellAO FlatHeading; transform convention unverified',
            scope='Static object prefixes and original template names; rotation convention and event semantics not decoded',
            placements=placements)
(out/'placements.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'Decoded {count} static placements; consumed {offset} bytes exactly')
for name,x,z in [('Grid',636,728),('Whompah',682,531)]:
    nearest=sorted(placements,key=lambda p:math.hypot(p['positionRaw'][0]-x,p['positionRaw'][2]-z))[:2]
    print(name,[(p['identity'],p['templateId'],p['positionRaw']) for p in nearest])
