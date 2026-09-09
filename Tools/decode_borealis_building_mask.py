"""Compare bit-order interpretations against independent original door anchors."""
import hashlib,json,math
from pathlib import Path

root=Path(__file__).resolve().parents[1];out=root/'Research/Borealis/ClientLayout'
doors=[p for p in json.loads((out/'placements.json').read_text())['placements'] if p['templateName'] in ('Door','Locked Door')]
def candidate(msb):
    mask=bytearray(512*512);cells=[]
    for block in range(16):
        data=(out/f'buildingmap_compressed_data-{block:02}.bin').read_bytes()
        if len(data)!=2048:raise ValueError('Unexpected building block dimensions')
        for bit in range(128*128):
            if data[bit//8]&(1<<(7-bit%8 if msb else bit%8)):
                x=block%4*128+bit%128;z=block//4*128+bit//128
                mask[z*512+x]=1;cells.append((x*2,z*2))
    distances=[]
    for door in doors:
        x,_,z=door['positionRaw'];distance=min(math.hypot(cx-x,cz-z) for cx,cz in cells)
        distances.append(dict(identity=door['identity'],distanceToMarkedSample=distance))
    return mask,dict(msbFirst=msb,markedSamples=len(cells),doorsWithin4=sum(p['distanceToMarkedSample']<=4 for p in distances),medianDoorDistance=sorted(p['distanceToMarkedSample'] for p in distances)[len(distances)//2],doors=distances)
mask,best=candidate(True);_,alternative=candidate(False)
if best['doorsWithin4']!=len(doors) or best['doorsWithin4']<=alternative['doorsWithin4']:raise ValueError('Building-mask interpretation needs review')
(out/'building-mask-512.raw').write_bytes(mask)
report=dict(resolution=512,sampleSpacing=2,sha256=hashlib.sha256(mask).hexdigest(),selected=best,alternative=alternative,
            scope='1-bit occupancy interpretation supported by original door proximity; exact cell extents, rendered footprint boundaries, collision purpose and building heights remain unverified.')
(out/'building-mask-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'PASS: {best["markedSamples"]} marked samples; {best["doorsWithin4"]}/{len(doors)} doors within 4 units, alternative {alternative["doorsWithin4"]}/{len(doors)}')
