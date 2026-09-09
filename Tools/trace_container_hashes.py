"""Read-only literal reference audit for the Soldier container's three hashes.

Does not interpret compressed/encoded payloads or prove that server mappings exist
in the client. Index layout is supplied by the attributed audit_client_reference.
"""
import argparse
import bisect
import json
import mmap
import struct
from pathlib import Path
from audit_client_reference import read_index


def trace(client):
    directory=client/'cd_image/data/db'
    records,block,size,_=read_index((directory/'ResourceDatabase.idx').read_bytes())
    hashes=('7K1M','STVK','X5BU')
    result={name:{'identityMatches':[],'literalMatches':[]} for name in hashes}
    for name in hashes:
        identities={int.from_bytes(name.encode(),endian) for endian in ('little','big')}
        result[name]['identityMatches']=[{'type':kind,'id':identity} for kind,identity in records if identity in identities]
    for part,path in enumerate(sorted(directory.glob('ResourceDatabase.dat*'))):
        entries=sorted((offset-(size-block)*part,kind,identity)
                       for (kind,identity),offset in records.items() if offset//size==part)
        offsets=[entry[0] for entry in entries]
        with path.open('rb') as stream,mmap.mmap(stream.fileno(),0,access=mmap.ACCESS_READ) as data:
            for name in hashes:
                for spelling in (name,name[::-1]):
                    needle=spelling.encode();start=0
                    while (hit:=data.find(needle,start))>=0:
                        start=hit+1
                        index=bisect.bisect_right(offsets,hit)-1
                        if index<0:continue
                        offset,kind,identity=entries[index]
                        payload_length=struct.unpack_from('<I',data,offset+18)[0]-12
                        if not offset+34<=hit or hit+4>offset+34+payload_length:continue
                        result[name]['literalMatches'].append({'type':kind,'id':identity,
                            'payloadOffset':hit-offset-34,'wireSpelling':spelling})
    return {'clientVersion':(client/'version.id').read_text().strip(),
            'scope':'Literal per-segment byte search and direct index identity search only; no semantic mapping inferred.',
            'hashes':result}


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('client',type=Path);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();result=trace(args.client)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+'\n')
    print({name:len(value['literalMatches']) for name,value in result['hashes'].items()})
