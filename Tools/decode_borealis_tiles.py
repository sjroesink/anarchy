"""Reconstruct tile-index layout and original texture-name provenance."""
import collections,hashlib,json,struct
from pathlib import Path
from decode_client_items import records

root=Path(__file__).resolve().parents[1];out=root/'Research/Borealis/ClientLayout'
ids=json.loads((out/'ground-structure.json').read_text())['textureIds']
textures={}
for identity,payload in records(root/'Research/ClientReference/18.8.50/app',1010006):
    if identity not in ids:continue
    name=payload.split(b'\0',1)[0].decode('ascii')
    textures[identity]=dict(resourceType=1010006,id=identity,name=name,sha256=hashlib.sha256(payload).hexdigest())
if len(textures)!=len(ids):raise ValueError('Unresolved texture reference')
grid=[None]*(256*256);flag_grid=[0]*(256*256);flags=collections.Counter()
for block in range(16):
    data=(out/f'tilemap_compressed_data-{block:02}.bin').read_bytes()
    if len(data)!=8192:raise ValueError('Unexpected tile block size')
    for i,(word,) in enumerate(struct.iter_unpack('<H',data)):
        index=word&0x3fff;flag=word>>14
        if index>=len(ids):raise ValueError('Tile index outside referenced texture table')
        x=block%4*64+i%64;z=block//4*64+i//64
        grid[z*256+x]=index;flag_grid[z*256+x]=flag;flags[flag]+=1
if any(v is None for v in grid):raise ValueError('Incomplete tile coverage')
(out/'tile-indices-256.raw').write_bytes(bytes(grid))
(out/'tile-flags-256.raw').write_bytes(bytes(flag_grid))
catalog=[dict(index=index,**textures[identity]) for index,identity in enumerate(ids)]
report=dict(resolution=256,tileSpacing=4,catalog=catalog,flagCounts=dict(flags),
            scope='Low 14 bits resolve every tile to a referenced original texture. Top 2 bits preserved as flags; rotation meaning unverified. Block arrangement follows independently checked heightfield. Names do not specify blend masks or road boundaries.')
(out/'tile-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'PASS: {len(grid)} tile indices, {len(catalog)} original texture names; high-bit flags {dict(flags)}')
