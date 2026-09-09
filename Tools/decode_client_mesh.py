"""Inspect the observed serialized mesh layout of original resource 15839.

Restricted descriptor support; local reference output, never a Unity art import.
Transform composition and material rendering are not yet engine-verified.
"""
import hashlib,json,math,struct
from pathlib import Path

class Reader:
    def __init__(self,data):self.data=data;self.offset=0
    def take(self,n):
        if n<0 or self.offset+n>len(self.data):raise ValueError('Truncated mesh record')
        result=self.data[self.offset:self.offset+n];self.offset+=n;return result
    def u32(self):return struct.unpack('<I',self.take(4))[0]
    def string(self):
        end=self.data.find(b'\0',self.offset)
        if end<0:raise ValueError('Unterminated symbol')
        result=self.take(end-self.offset);self.take(1);return result.decode('ascii')

def decode(data):
    r=Reader(data)
    if [r.u32() for _ in range(4)]!=[3,0,0,1]:raise ValueError('Unsupported mesh header')
    symbols=[(r.string(),r.string()) for _ in range(r.u32())]
    count=r.u32();objects=[]
    if len(symbols)>256:raise ValueError('Unsupported symbol-index width')
    for index in range(count+1):
        size=r.u32();chunk=Reader(r.take(size))
        if chunk.u32()!=1:raise ValueError('Unsupported object version')
        fields=[]
        for _ in range(chunk.u32()):
            symbol=chunk.take(1)[0]
            if symbol>=len(symbols):raise ValueError('Invalid symbol index')
            kind,unit,length=chunk.u32(),chunk.u32(),chunk.u32()
            payload=chunk.take(length)
            if unit and length%unit:raise ValueError('Invalid field array size')
            fields.append({'symbol':symbol,'name':symbols[symbol][1],'type':kind,'unit':unit,'payload':payload})
        if chunk.offset!=size:raise ValueError('Trailing object bytes')
        objects.append(fields)
    if r.offset!=len(data):raise ValueError('Trailing mesh bytes')
    for fields in objects:
        for field in fields:
            if field['type']==17:
                if field['unit']!=4:raise ValueError('Unsupported reference width')
                if any(v < -1 or v>=count for (v,) in struct.iter_unpack('<i',field['payload'])):
                    raise ValueError('Invalid object reference')
    def field(fields,name):
        matches=[f for f in fields if f['name']==name]
        if len(matches)!=1:raise ValueError('Missing or ambiguous field '+name)
        return matches[0]['payload']
    def integer(fields,name):return struct.unpack('<I',field(fields,name))[0]
    def classname(fields):
        identity=integer(fields,'__class_id__')
        if identity>=len(symbols) or symbols[identity][0]!='':raise ValueError('Invalid class identity')
        return symbols[identity][1]
    def blob(fields,name):
        payload=field(fields,name)
        if struct.unpack_from('<I',payload)[0]!=len(payload)-4:raise ValueError('Invalid blob length')
        return payload[4:]
    meshes=[];nodes=[]
    for index,fields in enumerate(objects[1:]):
        cls=classname(fields)
        if cls=='SimpleMesh':
            descriptor=struct.unpack('<4I',field(fields,'vb_desc'))
            # Observed second-word variants retain the same 32-byte vertex rows.
            # Preserve this opaque flag word; its rendering semantics are unverified.
            if descriptor[0]!=16 or descriptor[1] not in (0,2048,65536,67584) or descriptor[2]!=274:
                raise ValueError('Unsupported vertex descriptor')
            vertices=list(struct.iter_unpack('<8f',blob(fields,'vertices')))
            if len(vertices)!=descriptor[3]:raise ValueError('Vertex count mismatch')
            if any(not math.isfinite(v) for row in vertices for v in row):raise ValueError('Non-finite vertex')
            if any(abs(sum(v*v for v in row[3:6])-1)>.01 for row in vertices):raise ValueError('Unexpected vertex normal layout')
            triangles_object=objects[integer(fields,'trilist')+1]
            if classname(triangles_object)!='TriList':raise ValueError('Unexpected triangle class')
            triangles=list(struct.iter_unpack('<3H',blob(triangles_object,'triangles')))
            if any(v>=len(vertices) for row in triangles for v in row):raise ValueError('Triangle outside vertex range')
            meshes.append({'objectId':index,'materialObject':integer(fields,'material'),'vertexDescriptor':descriptor,'vertices':vertices,'triangles':triangles})
        elif cls=='RTriMesh_t':
            nodes.append({'objectId':index,'matrix':struct.unpack('<16f',field(fields,'anim_matrix')),
                          'dataObject':integer(fields,'data'),
                          'children':[v[0] for v in struct.iter_unpack('<i',field(fields,'chld'))] if integer(fields,'chld_cnt') else []})
    bounds_verified=0;sentinel_bounds=0
    for fields in objects[1:]:
        if classname(fields)!='FAFTriMeshData_t':continue
        mesh_ids=[v[0] for v in struct.iter_unpack('<i',field(fields,'mesh'))]
        if len(mesh_ids)!=integer(fields,'num_meshes') or not mesh_ids:
            raise ValueError('Mesh reference count mismatch')
        mesh_by_id={m['objectId']:m for m in meshes}
        if any(identity not in mesh_by_id for identity in mesh_ids):
            raise ValueError('Unexpected mesh reference class')
        vertices=[v for identity in mesh_ids for v in mesh_by_id[identity]['vertices']]
        if not vertices:raise ValueError('Empty mesh bounds')
        bounds=objects[integer(fields,'bvol')+1]
        for name,aggregate in [('min_pos',min),('max_pos',max)]:
            expected=struct.unpack('<3f',field(bounds,name))
            actual=tuple(aggregate(v[axis] for v in vertices) for axis in range(3))
            for computed,stored in zip(actual,expected):
                if computed==stored:bounds_verified+=1
                # Observed legacy maximum bound: smallest positive normal float32.
                # For non-positive geometry this is conservative, not a tight bound.
                # Its origin is unproven; do not generalize to arbitrary padding.
                elif name=='max_pos' and computed<=0 and stored==2**-126:sentinel_bounds+=1
                else:raise ValueError('Decoded vertices differ from stored bounds')
    return {'symbols':symbols,'objects':objects,'meshes':meshes,'nodes':nodes,
            'verifiedBoundsComponents':bounds_verified,'sentinelExpandedBoundsComponents':sentinel_bounds}

def main():
    root=Path(__file__).resolve().parents[1]
    source=root/'Research/ClientReference/solar-rifle-mesh.bin';data=source.read_bytes();result=decode(data)
    output=root/'Research/ClientReference/solar-rifle-geometry.json'
    # Keep every raw field (including matrices/materials) without inventing semantics.
    serial={**result,'objects':[[{**f,'payload':f['payload'].hex()} for f in fields] for fields in result['objects']]}
    output.write_text(json.dumps(serial,indent=2))
    report={'resourceType':1010001,'resourceId':15839,'sourceSha256':hashlib.sha256(data).hexdigest(),
        'scope':__doc__.strip(),'objects':len(result['objects'])-1,'meshes':len(result['meshes']),
        'vertices':sum(len(m['vertices']) for m in result['meshes']),
        'triangles':sum(len(m['triangles']) for m in result['meshes']),
        'verifiedBoundsComponents':result['verifiedBoundsComponents'],
        'sentinelExpandedBoundsComponents':result['sentinelExpandedBoundsComponents'],
        'outputSha256':hashlib.sha256(output.read_bytes()).hexdigest()}
    (root/'Artifacts/solar-rifle-mesh-decoding.json').write_text(json.dumps(report,indent=2)+'\n');print(report)

if __name__=='__main__':main()
