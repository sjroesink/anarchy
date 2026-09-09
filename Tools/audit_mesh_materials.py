"""Audit observed material graphs on geometrically accepted weapon references."""
import hashlib,json
from collections import Counter
from pathlib import Path
from decode_client_items import records
from decode_client_mesh import decode
from decode_mesh_materials import resolve_materials

root=Path(__file__).resolve().parents[1]
client=root/'Research/ClientReference/18.8.50/app'
audit=json.loads((root/'Artifacts/weapon-mesh-audit.json').read_text())
wanted={r['meshId'] for r in audit['results'] if r['status']=='decoded'}
results=[];errors=Counter()
for identity,payload in records(client,1010001):
    if identity not in wanted:continue
    row={'meshId':identity,'sha256':hashlib.sha256(payload).hexdigest()}
    try:
        row.update(status='resolved',materials=resolve_materials(decode(payload)))
    except Exception as error:
        reason=type(error).__name__+': '+str(error);errors[reason]+=1
        row.update(status='unresolved',reason=reason)
    results.append(row)
references={(c['resourceType'],c['resourceId']) for row in results for m in row.get('materials',[]) for c in m['textureChannels']}
found={}
for kind in sorted({kind for kind,identity in references}):
    for identity,payload in records(client,kind):
        if (kind,identity) in references:
            found[(kind,identity)]={'type':kind,'id':identity,'bytes':len(payload),'sha256':hashlib.sha256(payload).hexdigest()}
report={'sourceVersion':'18.8.50_EP1','scope':'Texture reference resolution and resource existence, not shader or visual fidelity',
        'summary':{'meshReferences':len(wanted),'resolvedGraphs':sum(r['status']=='resolved' for r in results),
                   'unresolved':dict(errors),'uniqueTextureReferences':len(references),'missingTextureResources':len(references-set(found))},
        'results':sorted(results,key=lambda r:r['meshId']),'textures':[found[key] for key in sorted(found)],
        'missingTextures':sorted(references-set(found))}
(root/'Artifacts/weapon-material-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report['summary'],indent=2))
