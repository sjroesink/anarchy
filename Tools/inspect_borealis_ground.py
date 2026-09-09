"""Structural inspection of local CHGA ground data; no terrain interpretation."""
import hashlib,json,struct,zlib
from pathlib import Path
from decode_client_items import records
from decode_client_mesh import Reader

root=Path(__file__).resolve().parents[1]
out=root/'Research/Borealis/ClientLayout';out.mkdir(parents=True,exist_ok=True)
data=next(b for i,b in records(root/'Research/ClientReference/18.8.50/app',1000009) if i==800)
(out/'1000009-800.bin').write_bytes(data)
r=Reader(data)
if r.take(4)!=b'CHGA' or r.u32()!=len(data):raise ValueError('Invalid CHGA envelope')
version=r.u32();dimensions=struct.unpack('<HH',r.take(4));scales=struct.unpack('<ff',r.take(8))
texture_count=struct.unpack('<H',r.take(2))[0]
texture_ids=struct.unpack('<'+'H'*texture_count,r.take(texture_count*2))
if r.u32()!=1:raise ValueError('Unsupported object stream version')
symbols=[(r.string(),r.string()) for _ in range(r.u32())]
count=r.u32();objects=[];blocks=[]
for index in range(count+1):
    size=r.u32();chunk=Reader(r.take(size))
    if chunk.u32()!=1:raise ValueError('Unsupported object version')
    fields=[]
    for _ in range(chunk.u32()):
        symbol=chunk.take(1)[0];kind,unit,length=chunk.u32(),chunk.u32(),chunk.u32()
        payload=chunk.take(length);name=symbols[symbol][1]
        field=dict(name=name,type=kind,unit=unit,bytes=length,sha256=hashlib.sha256(payload).hexdigest())
        if kind==3 and length==4:field['uint32']=struct.unpack('<I',payload)[0]
        if name in ('heightmap_small_data','tile_type_data'):
            filename=name+'.bin';(out/filename).write_bytes(payload);field['file']=filename
        if name.endswith('compressed_data'):
            packed=Reader(payload);sizes=[]
            while packed.offset<len(payload):
                compressed=packed.take(packed.u32());decoder=zlib.decompressobj()
                raw=decoder.decompress(compressed,4_000_000)
                if not decoder.eof or decoder.unused_data or decoder.unconsumed_tail:raise ValueError('Incomplete compressed block')
                filename=f'{name}-{len(sizes):02}.bin';(out/filename).write_bytes(raw)
                sizes.append(dict(file=filename,bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
            field['blocks']=sizes;blocks.append((name,len(sizes),sorted(set(s['bytes'] for s in sizes))))
        fields.append(field)
    if chunk.offset!=size:raise ValueError('Object tail unparsed')
    objects.append(dict(index=index,fields=fields))
tail=data[r.offset:];(out/'ground-unparsed-tail.bin').write_bytes(tail)
report=dict(resourceType=1000009,playfieldId=800,sha256=hashlib.sha256(data).hexdigest(),version=version,
            rawEnvelopeDimensions=dimensions,rawEnvelopeScalars=scales,textureIds=texture_ids,
            scope='Named object fields and decompression only; height reconstruction, tile orientation and building encoding unknown',
            objectCount=len(objects),parsedBytes=r.offset,totalBytes=len(data),unparsedTailBytes=len(tail),
            unparsedTailSha256=hashlib.sha256(tail).hexdigest(),objects=objects)
(out/'ground-structure.json').write_text(json.dumps(report,indent=2)+'\n')
print('Objects',len(objects),'parsed',r.offset,'of',len(data),'block groups',blocks)
