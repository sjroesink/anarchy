"""Audit mesh decoding across every weapon-mesh ID in the base item catalogue."""
from collections import Counter
import hashlib,json
from pathlib import Path
from decode_client_items import records
from decode_client_mesh import decode

root=Path(__file__).resolve().parents[1];reference=root/'Research/ClientReference'
references={}
with (reference/'items-18.8.50.jsonl').open(encoding='utf-8') as stream:
    for line in stream:
        item=json.loads(line);identity=item['stats'].get('209',0)
        if identity not in (0,4294967295):references.setdefault(identity,[]).append(item['id'])
report={'sourceVersion':'18.8.50_EP1','scope':'Structural mesh audit only; no rendering or executable item conformance',
        'referencedMeshIds':len(references),'results':[],'missing':[]}
seen=set();reasons=Counter()
for identity,payload in records(reference/'18.8.50/app',1010001):
    if identity not in references:continue
    seen.add(identity)
    result={'meshId':identity,'itemReferences':len(references[identity]),'sha256':hashlib.sha256(payload).hexdigest()}
    try:
        parsed=decode(payload)
        if not parsed['meshes']:raise ValueError('No decoded geometry')
        result.update(status='decoded',meshes=len(parsed['meshes']),
                      vertexDescriptors=[m['vertexDescriptor'] for m in parsed['meshes']],
                      vertices=sum(len(m['vertices']) for m in parsed['meshes']),
                      triangles=sum(len(m['triangles']) for m in parsed['meshes']),
                      verifiedBoundsComponents=parsed['verifiedBoundsComponents'],
                      sentinelExpandedBoundsComponents=parsed['sentinelExpandedBoundsComponents'])
    except Exception as error:
        # Audit records unexpected exceptions too; these are decoder gaps, not valid meshes.
        reason=type(error).__name__+': '+str(error);reasons[reason]+=1
        result.update(status='unresolved',reason=reason)
    report['results'].append(result)
report['missing']=sorted(set(references)-seen)
report['summary']={'decoded':sum(r['status']=='decoded' for r in report['results']),
                   'decodedWithExactBoundsOnly':sum(r['status']=='decoded' and r['sentinelExpandedBoundsComponents']==0 for r in report['results']),
                   'decodedWithSentinelBounds':sum(r['status']=='decoded' and r['sentinelExpandedBoundsComponents']>0 for r in report['results']),
                   'unresolved':dict(reasons),'missing':len(report['missing'])}
report['results'].sort(key=lambda r:r['meshId'])
(root/'Artifacts/weapon-mesh-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'referencedMeshIds':len(references),**report['summary']},indent=2))
