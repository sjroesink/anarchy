"""Verify original SpecialAttackInfo dispatch using synthetic decoded messages.

Actor lookup and downstream handler are intercepted; no network or combat execution.
"""
import hashlib,json,struct,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'Research/ReferenceTools'))
import pefile,unicorn
from unicorn.x86_const import *
raw=(root/'Research/ClientReference/PatchWorking/app/Gamecode.dll').read_bytes()
digest=hashlib.sha256(raw).hexdigest()
if digest!='8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320':raise ValueError('Unreviewed binary')
pe=pefile.PE(data=raw);base=pe.OPTIONAL_HEADER.ImageBase

def dispatch(words,actor_exists):
    uc=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32)
    uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);uc.mem_write(base,pe.get_memory_mapped_image())
    uc.mem_map(0x200000,0x10000);message=0x202000;actor=0x203000;holder=0x204000
    # Order corroborated by original serializer and AOSharp's AoMember declarations.
    slot,amount,ammo,target_type,target_id,skill,unknown=words
    uc.mem_write(message+4,struct.pack('<2I',50000,17))
    uc.mem_write(message+0x18,struct.pack('<2i',target_type,target_id))
    uc.mem_write(message+0x20,struct.pack('<5i',slot,amount,ammo,skill,unknown))
    uc.mem_write(actor+0x1d4,struct.pack('<I',holder))
    uc.mem_write(0x207000,struct.pack('<I',0x208000))
    uc.reg_write(UC_X86_REG_ESP,0x207000);uc.reg_write(UC_X86_REG_ECX,message)
    calls=[]
    def hook(machine,address,size,user):
        esp=machine.reg_read(UC_X86_REG_ESP)
        if address==base+0xa1c09:
            pointer=struct.unpack('<I',machine.mem_read(esp,4))[0]
            if pointer!=message+4:raise ValueError('Unexpected actor lookup argument')
            machine.reg_write(UC_X86_REG_EAX,actor if actor_exists else 0)
            machine.reg_write(UC_X86_REG_EIP,address+size)
        elif address==base+0xa1c2c:
            a,b,c,target,d,e=struct.unpack('<6I',machine.mem_read(esp,24))
            if machine.reg_read(UC_X86_REG_ECX)!=holder:raise ValueError('Wrong dispatch receiver')
            t,i=struct.unpack('<2I',machine.mem_read(target,8))
            calls.append([a,b,c,t,i,d,e])
            machine.reg_write(UC_X86_REG_ESP,esp+24);machine.reg_write(UC_X86_REG_EIP,address+size)
    uc.hook_add(unicorn.UC_HOOK_CODE,hook)
    uc.emu_start(base+0xa1c02,0x208000,timeout=1000000,count=1000)
    if uc.reg_read(UC_X86_REG_EIP)!=0x208000:raise ValueError('Dispatch failed to return')
    return calls

cases=[(6,24,-1,50000,1234,148,0),(8,0,0,50000,42,142,7),(6,100,30,51035,999,150,-1)]
results=[]
for words in cases:
    for exists in [False,True]:
        actual=dispatch(words,exists);expected=[[v&0xffffffff for v in words]] if exists else []
        if actual!=expected:raise ValueError('Original message dispatch differs from field mapping')
        results.append({'messageWords':words,'actorFound':exists,'downstreamArguments':actual})
report={'scope':__doc__,'sourceVersion':'18.8.50_EP1','sourceSha256':digest,'passed':len(results),
        'handlerRva':0xa1c02,'receiverRva':0x6ac00,'results':results,
        'limits':['Decoded-message fixtures, not network captures','No proof of lock insertion or cooldown values',
                  'Final unknown field is preserved without inferred semantics']}
(root/'Artifacts/special-result-dispatch-emulation.json').write_text(json.dumps(report,indent=2)+'\n')
print('PASS:',len(results),'original special-result dispatch comparisons')
