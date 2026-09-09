"""Differential check of a bounded original Burst code path in an x86 emulator.

Only stat getter calls are substituted. No client process, network or OS services.
This does not verify template defaults, final timer units, hit resolution or combat.
"""
import hashlib,json,struct,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Research/ReferenceTools'))
import pefile,unicorn
from unicorn.x86_const import *

SOURCE_HASH='8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320'

def recovered_raw(recharge,cycle,skill):
    # Mirrors this reviewed branch's float32 intermediate, not a seconds formula.
    value=struct.unpack('<f',struct.pack('<f',20.0*recharge+cycle-4*skill))[0]
    return int(max(800.0,value))

def emulate(pe,recharge,cycle,skill,sse):
    uc=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32)
    base=pe.OPTIONAL_HEADER.ImageBase;size=(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095
    uc.mem_map(base,size);uc.mem_write(base,pe.get_memory_mapped_image())
    uc.mem_map(0x200000,0x10000)
    uc.reg_write(UC_X86_REG_ESP,0x207000);uc.reg_write(UC_X86_REG_EBP,0x208000)
    uc.reg_write(UC_X86_REG_ESI,0x202000);uc.reg_write(UC_X86_REG_EDI,0x203000)
    uc.reg_write(UC_X86_REG_EBX,148);uc.reg_write(UC_X86_REG_FPCW,0x37f)
    uc.mem_write(base+0x2eb44c,struct.pack('<I',int(sse)))
    getters={base+0x9ac39:(210,2,recharge),base+0x9ac56:(374,2,cycle),base+0x9ac70:(148,0,skill)}
    visited=[]
    def hook(machine,address,size,user):
        if address not in getters:return
        stat,detail,value=getters[address];esp=machine.reg_read(UC_X86_REG_ESP)
        if struct.unpack('<2I',machine.mem_read(esp,8))!=(stat,detail):
            raise ValueError('Original stat-call arguments changed')
        visited.append(stat)
        machine.reg_write(UC_X86_REG_EAX,value)
        machine.reg_write(UC_X86_REG_ESP,esp+8)
        machine.reg_write(UC_X86_REG_EIP,address+size)
    uc.hook_add(unicorn.UC_HOOK_CODE,hook)
    uc.emu_start(base+0x9ac2e,base+0x9ad38,timeout=1000000,count=2000)
    if uc.reg_read(UC_X86_REG_EIP)!=base+0x9ad38 or visited!=[210,374,148]:
        raise ValueError('Original branch did not complete as expected')
    return uc.reg_read(UC_X86_REG_EAX)

def main():
    raw=(root/'Research/ClientReference/PatchWorking/app/Gamecode.dll').read_bytes()
    if hashlib.sha256(raw).hexdigest()!=SOURCE_HASH:raise ValueError('Unreviewed original binary')
    pe=pefile.PE(data=raw)
    cases=[(150,1000,6),(150,1000,799),(150,1000,800),(150,1000,801),
           (150,0,0),(150,0,2000),(0,0,0),(100,3000,100),(500,6000,2000),
           (40,1,0),(40,0,0),(40,0,1),(40,5,1),(999999,1000,0)]
    catalogue=root/'Research/ClientReference/Catalog-through-18.8.62/burst-reference.jsonl'
    rows=[json.loads(line) for line in catalogue.read_text().splitlines()]
    pairs={(r['rechargeTimeRaw'],r['burstCycleRaw']) for r in rows if r['burstCyclePresent']}
    for recharge,cycle in sorted(pairs):
        threshold=max(0,(20*recharge+cycle-800+3)//4)
        cases.extend((recharge,cycle,skill) for skill in {0,max(0,threshold-1),threshold,threshold+1})
    cases=sorted(set(cases))
    results=[]
    for values in cases:
        expected=recovered_raw(*values)
        for sse in [False,True]:
            actual=emulate(pe,*values,sse)
            if actual!=expected:raise ValueError(f'Differential mismatch {values} SSE={sse}: {actual} != {expected}')
            results.append({'rechargeRaw':values[0],'cycleRaw':values[1],'skill':values[2],
                            'sseConversion':sse,'emulatedRaw':actual,'recoveredRaw':expected})
    report={'scope':__doc__,'sourceVersion':'18.8.50_EP1','sourceSha256':SOURCE_HASH,
            'catalogueSha256':hashlib.sha256(catalogue.read_bytes()).hexdigest(),
            'explicitInputPairs':len(pairs),'catalogueScope':'Explicit-cycle item pairs through inspected patch 18.8.62; no missing defaults inferred',
            'unicornVersion':unicorn.__version__,'passed':len(results),'results':results}
    (root/'Artifacts/burst-arithmetic-emulation.json').write_text(json.dumps(report,indent=2)+'\n')
    print('PASS:',len(results),'original-code Burst arithmetic comparisons')

if __name__=='__main__':main()
