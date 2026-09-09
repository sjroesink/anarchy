"""Emulate original special-lock list behavior with synthetic Burst inputs; tick frequency remains unverified."""
import hashlib,json,struct,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'Research/ReferenceTools'))
import pefile,unicorn
from unicorn.x86_const import *

raw=(root/'Research/ClientReference/PatchWorking/app/Gamecode.dll').read_bytes()
digest=hashlib.sha256(raw).hexdigest()
if digest!='8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320':raise ValueError('Unreviewed binary')
pe=pefile.PE(data=raw);base=pe.OPTIONAL_HEADER.ImageBase

def machine(entries):
    uc=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32)
    uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095)
    uc.mem_write(base,pe.get_memory_mapped_image());uc.mem_map(0x200000,0x10000)
    uc.mem_write(0x202000,struct.pack('<I',0x202100))
    uc.mem_write(0x202100,struct.pack('<3I',0x203000,0x203000+16*len(entries),0x204000))
    for index,(stat,value) in enumerate(entries):
        uc.mem_write(0x203000+16*index,struct.pack('<4i',123+index,stat,456+index,value))
    return uc

def run(rva,entries,skill):
    uc=machine(entries)
    uc.mem_write(0x207000,struct.pack('<2I',0x208000,skill))
    uc.reg_write(UC_X86_REG_ECX,0x202000);uc.reg_write(UC_X86_REG_ESP,0x207000)
    uc.emu_start(base+rva,0x208000,timeout=1000000,count=2000)
    if uc.reg_read(UC_X86_REG_EIP)!=0x208000:raise ValueError('Lookup failed to return')
    value=uc.reg_read(UC_X86_REG_EAX)
    return value&255 if rva==0x63e1d else struct.unpack('<i',struct.pack('<I',value))[0]

def tick(entries,ticks):
    uc=machine(entries)
    for _ in range(ticks):
        uc.mem_write(0x207000,struct.pack('<I',0x208000))
        uc.reg_write(UC_X86_REG_ECX,0x202000);uc.reg_write(UC_X86_REG_ESP,0x207000)
        uc.emu_start(base+0x646ce,0x208000,timeout=1000000,count=10000)
        if uc.reg_read(UC_X86_REG_EIP)!=0x208000:raise ValueError('Tick failed to return')
    start,end=struct.unpack('<2I',uc.mem_read(0x202100,8))
    if start!=0x203000 or end<start or end>start+len(entries)*16 or (end-start)%16:
        raise ValueError('Unexpected vector bounds after expiry')
    return list(struct.iter_unpack('<4i',uc.mem_read(start,end-start))) if end>start else []

def apply_lock(entries,duration,extend=False):
    uc=machine(entries);skill=148;notifications=[]
    uc.mem_write(0x202018,struct.pack('<I',0x205000))
    uc.mem_write(base+0x2eb44c,struct.pack('<I',1))
    uc.reg_write(UC_X86_REG_FPCW,0x37f)
    uc.mem_write(0x207000,struct.pack('<4I',0x208000,skill,duration,0))
    uc.reg_write(UC_X86_REG_ECX,0x202000);uc.reg_write(UC_X86_REG_ESP,0x207000)
    def visual_only(machine,address,size,user):
        if address==base+0x64ce9:
            if machine.reg_read(UC_X86_REG_ECX)!=0x205000:raise ValueError('Unexpected visual owner')
            machine.reg_write(UC_X86_REG_EAX,0x205800);machine.reg_write(UC_X86_REG_EIP,address+size)
        elif address==base+0x64cf0:
            esp=machine.reg_read(UC_X86_REG_ESP)
            notifications.append(struct.unpack('<I',machine.mem_read(esp,4))[0])
            machine.reg_write(UC_X86_REG_ESP,esp+4);machine.reg_write(UC_X86_REG_EIP,address+size)
    uc.hook_add(unicorn.UC_HOOK_CODE,visual_only)
    uc.emu_start(base+(0x6580c if extend else 0x64ca7),0x208000,timeout=1000000,count=10000)
    if uc.reg_read(UC_X86_REG_EIP)!=0x208000:raise ValueError('Lock application failed to return')
    start,end=struct.unpack('<2I',uc.mem_read(0x202100,8))
    if start!=0x203000 or end<start or end>start+(len(entries)+1)*16 or (end-start)%16:
        raise ValueError('Unexpected inserted vector bounds')
    return list(struct.iter_unpack('<4i',uc.mem_read(start,end-start))) if end>start else [],notifications

cases=[([],148),([(147,9)],148),([(148,40)],148),([(147,9),(148,40),(150,80)],148),
       ([(147,9),(150,80),(148,1)],148),([(148,0)],148),([(148,-1)],148),
       ([(148,9),(148,40)],148)]
results=[]
for entries,skill in cases:
    first=next((value for stat,value in entries if stat==skill),None)
    member=run(0x63e1d,entries,skill);remaining=run(0x63e42,entries,skill)
    if member!=int(first is not None) or remaining!=(first if first is not None else 0):
        raise ValueError('Original special-lock lookup differs from recovered layout')
    results.append({'entries':entries,'skill':skill,'membership':member,'rawRemaining':remaining})
tick_cases=[([],1),([(148,2)],1),([(148,1)],1),([(148,0)],1),([(148,-1)],1),
            ([(147,1),(148,1),(150,5)],1),([(147,5),(148,1),(150,5)],1),
            ([(147,5),(148,1),(150,1)],1),([(148,1),(148,3)],1),
            ([(147,1),(148,2),(150,3)],3),([(148,2)],0),([(148,2147483647)],1)]
tick_results=[]
for entries,ticks in tick_cases:
    expected=[(123+i,stat,456+i,value) for i,(stat,value) in enumerate(entries)]
    for _ in range(ticks):expected=[(a,b,c,d-1) for a,b,c,d in expected if d-1>0]
    actual=tick(entries,ticks)
    if actual!=expected:raise ValueError(f'Original expiry differs: {entries}, {ticks}: {actual} != {expected}')
    tick_results.append({'entries':entries,'tickCalls':ticks,'result':actual})
application_results=[]
for entries in [[],[(147,9)],[(148,9)],[(147,9),(148,5)]]:
    for duration in [0,1,40]:
        for extend in [False,True]:
            expected=[(123+i,stat,456+i,value) for i,(stat,value) in enumerate(entries)]
            index=next((i for i,row in enumerate(expected) if row[1]==148),None)
            if index is None:expected.append((0,148,duration,duration))
            elif extend:
                a,b,c,d=expected[index];expected[index]=(a,b,c+duration,d+duration)
            actual,notifications=apply_lock(entries,duration,extend)
            if actual!=expected or notifications!=([148] if index is None else []):
                raise ValueError(f'Lock insertion/extension mismatch {entries}, {duration}, {extend}: {actual}')
            application_results.append({'entries':entries,'durationInput':duration,'extend':extend,'result':actual,'notifications':notifications})
report={'scope':__doc__,'sourceVersion':'18.8.50_EP1','sourceSha256':digest,'unicornVersion':unicorn.__version__,
        'passed':len(cases)*2+len(tick_cases)+len(application_results),'lookupComparisons':len(cases)*2,'tickComparisons':len(tick_cases),
        'applicationComparisons':len(application_results),'results':results,'tickResults':tick_results,'applicationResults':application_results,
        'limits':['Synthetic list fixtures, not captured server messages','Entry presence is separate from its remaining value',
                  'Visual notification calls substituted; original duration adjustment/list insertion/extension instructions execute',
                  'Upstream duration source, tick cadence and link to Burst arithmetic remain unverified']}
(root/'Artifacts/special-lock-lookup-emulation.json').write_text(json.dumps(report,indent=2)+'\n')
print('PASS:',report['passed'],'original special-lock reader/application/expiry comparisons')
