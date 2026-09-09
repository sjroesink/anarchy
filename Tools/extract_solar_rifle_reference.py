"""Extract local original-client visual references; never ship them as new art."""
import hashlib,json
from pathlib import Path
from decode_client_items import records,decode
from decode_client_mesh import decode as decode_mesh
from decode_mesh_materials import resolve_materials
root=Path(__file__).resolve().parents[1]
reference=root/'Research/ClientReference'
item=decode(121569,next(payload for identity,payload in records(reference/'18.8.50/app') if identity==121569))
if item['stats']['79']!=13313 or item['stats']['209']!=15839:raise ValueError('Unexpected visual identity mapping')
mesh_payload=next(payload for identity,payload in records(reference/'18.8.50/app',1010001) if identity==item['stats']['209'])
materials=resolve_materials(decode_mesh(mesh_payload))
channels=[channel for material in materials for channel in material['textureChannels']]
if len(channels)!=1 or channels[0]['channel']!=0 or channels[0]['resourceType']!=1010004:
    raise ValueError('Unreviewed rifle texture layout')
texture_identity=channels[0]['resourceId']
report={'itemId':121569,'clientVersion':'18.8.50_EP1','iconStat':79,'weaponMeshStat':209,
        'scope':'Original visual identities and resolved texture graph; rendering semantics unverified',
        'materials':materials,
        'itemPayloadSha256':item['payloadSha256'],'resources':[]}
for kind,identity,name in [(1010008,13313,'solar-rifle-icon.png'),(1010001,15839,'solar-rifle-mesh.bin'),
                           (1010004,texture_identity,'solar-rifle-texture.bin')]:
    payload=next(payload for item_id,payload in records(reference/'18.8.50/app',kind) if item_id==identity)
    if name.endswith('.png') and not payload.startswith(b'\x89PNG\r\n\x1a\n'):raise ValueError('Unexpected icon format')
    (reference/name).write_bytes(payload)
    report['resources'].append({'type':kind,'id':identity,'bytes':len(payload),'sha256':hashlib.sha256(payload).hexdigest()})
(root/'Artifacts/solar-rifle-visual-reference.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
