"""Evidence-tested 2D predictive byte reconstruction of Borealis heights."""
import hashlib,json,math,struct
from pathlib import Path

root=Path(__file__).resolve().parents[1];out=root/'Research/Borealis/ClientLayout'
small=(out/'heightmap_small_data.bin').read_bytes()
grid=[[None]*257 for _ in range(257)];shared=0
for block in range(16):
    data=(out/f'heightmap_compressed_data-{block:02}.bin').read_bytes()
    if len(data)!=65*65 or struct.unpack_from('<I',small,block*8)[0]!=4:raise ValueError('Unreviewed block dimensions')
    values=[]
    for i,delta in enumerate(data):
        x,y=i%65,i//65
        value=(delta+(values[i-1] if x else 0)+(values[i-65] if y else 0)-(values[i-66] if x and y else 0))%256
        values.append(value);gx=block%4*64+x;gz=block//4*64+y
        if grid[gz][gx] is not None:
            if grid[gz][gx]!=value:raise ValueError('Shared edge disagreement')
            shared+=1
        grid[gz][gx]=value
    if bytes(values[i] for i in [0,64,4160,4224])!=small[block*8+4:block*8+8]:raise ValueError('Corner reference disagreement')
raw=bytes(v for row in grid for v in row);(out/'heights-257.raw').write_bytes(raw)
placements=json.loads((out/'placements.json').read_text())['placements']
def sample(x,z):
    x/=4;z/=4;i,j=int(x),int(z);u,v=x-i,z-j
    return ((1-u)*(1-v)*grid[j][i]+u*(1-v)*grid[j][i+1]+(1-u)*v*grid[j+1][i]+u*v*grid[j+1][i+1])*.4
comparisons=[]
for p in placements:
    x,y,z=p['positionRaw']
    if 0<=x<1024 and 0<=z<1024:
        h=sample(x,z);comparisons.append(dict(identity=p['identity'],name=p['templateName'],objectY=y,terrainY=h,offset=y-h))
grid_check=next(p for p in comparisons if p['name']=='Enter The Grid')
if abs(grid_check['offset'])>.02:raise ValueError('Grid height anchor mismatch')
report=dict(resolution=257,horizontalSpacing=4,heightPerByte=.4,cornerSetsMatched=16,sharedSamplesMatched=shared,
            sha256=hashlib.sha256(raw).hexdigest(),source='1000009:800 client 18.8.50',
            scope='Predictor confirmed by all corner bytes and seams; scale/orientation corroborated by Grid height. Bilinear sampling is a comparison tool, not verified client collision interpolation. Outer 250x250 extent versus 257 samples remains unresolved.',
            comparisons=comparisons)
(out/'height-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'PASS: 16 corner sets, {shared} shared samples; Grid terrain {grid_check["terrainY"]:.3f}, object {grid_check["objectY"]:.3f}')
