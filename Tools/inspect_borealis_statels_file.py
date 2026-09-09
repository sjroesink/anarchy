"""Conservative PF offset-table audit. Candidate prefixes, not a full PF parser."""
import collections,hashlib,json,math,struct
from pathlib import Path
from audit_client_reference import read_index

root=Path(__file__).resolve().parents[1]
source=root/'Research/ClientReference/PatchWorking/app/cd_image/data/statels/800.pf'
data=source.read_bytes();out=root/'Research/Borealis/ClientLayout'
if struct.unpack_from('<I',data)[0]!=1:raise ValueError('Unreviewed PF version')
offsets=list(struct.unpack_from('<625I',data,4))+[len(data)]
if offsets[0]<2504 or any(a>=b for a,b in zip(offsets,offsets[1:])):raise ValueError('Invalid PF table')
index,*_=read_index((root/'Research/ClientReference/18.8.50/app/cd_image/data/db/ResourceDatabase.idx').read_bytes())
cells=[];candidates=[];rejected=[]
for cell in range(625):
    start,end=offsets[cell:cell+2];chunk=data[start:end]
    info=dict(cell=cell,xCell=cell%25,zCell=cell//25,offset=start,bytes=len(chunk),sha256=hashlib.sha256(chunk).hexdigest())
    cells.append(info)
    if len(chunk)==12 and not any(chunk):continue
    at=0
    while at+2<=len(chunk) and struct.unpack_from('<H',chunk,at)[0]==0:at+=2
    if at+24>len(chunk):rejected.append(cell);continue
    count=struct.unpack_from('<H',chunk,at)[0]
    position=struct.unpack_from('<3f',chunk,at+2);mesh=struct.unpack_from('<I',chunk,at+18)[0]
    x,y,z=position
    inside=all(math.isfinite(v) for v in position) and cell%25*40<=x<(cell%25+1)*40 and cell//25*40<=z<(cell//25+1)*40
    if not inside or (1010001,mesh) not in index:rejected.append(cell);continue
    candidates.append(dict(cell=cell,prefixOffset=start+at+2,leadingZeroWords=at//2,rawCount=count,
                           positionCandidate=list(position),meshTypeCandidate=1010001,meshIdCandidate=mesh,
                           orientationBytes=chunk[at+14:at+18].hex(),suffixBytes=chunk[at+22:].hex(),
                           status='First prefix only: mesh existence and 40-unit cell bounds agree. Full record size, orientation, variants and version reconciliation unverified.'))
report=dict(source=str(source.relative_to(root)),sha256=hashlib.sha256(data).hexdigest(),bytes=len(data),
            scope='625-entry spatial index with first-prefix candidates; not a complete geometry placement decoder',
            inferredCellGrid=[25,25],inferredCellSpacing=40,unparsedPreambleBytes=offsets[0]-2504,
            cellCount=625,candidateCount=len(candidates),rejectedCells=rejected,cells=cells,candidates=candidates)
(out/'statels-file-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(f'PF audit: {len(cells)} bounded cells, {len(candidates)} corroborated first-prefix candidates, {len(rejected)} unresolved nonempty cells')
