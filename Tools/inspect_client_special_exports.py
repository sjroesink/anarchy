"""Static PE export/code-window inspection. Never loads or executes the client."""
import hashlib,json,sys,struct
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Research/ReferenceTools'))
import pefile,capstone

source=root/'Research/ClientReference/PatchWorking/app/Gamecode.dll'
raw=source.read_bytes();pe=pefile.PE(data=raw)
if pe.FILE_HEADER.Machine!=0x14c or pe.OPTIONAL_HEADER.Magic!=0x10b:
    raise ValueError('Unreviewed client architecture')
exports=sorted(pe.DIRECTORY_ENTRY_EXPORT.symbols,key=lambda s:s.address)
decoder=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
needles=['?N3Msg_GetShopItemStat@','?N3Msg_IsSecondarySpecialAttackAvailable@','?N3Msg_SecondarySpecialAttack@']
rows=[];windows=[]
for needle in needles:
    matches=[e for e in exports if e.name and e.name.decode().startswith(needle)]
    if len(matches)!=1:raise ValueError('Missing or ambiguous client export '+needle)
    export=matches[0]
    if export.forwarder:raise ValueError('Forwarded export requires separate inspection')
    later=[e.address for e in exports if e.address>export.address]
    size=min(256,min(later)-export.address) if later else 256
    code=pe.get_data(export.address,size)
    decoded=[{'rva':i.address-pe.OPTIONAL_HEADER.ImageBase,'mnemonic':i.mnemonic,'operands':i.op_str}
             for i in decoder.disasm(code,pe.OPTIONAL_HEADER.ImageBase+export.address)]
    row={'export':export.name.decode(),'rva':export.address,'windowBytes':len(code),
         'windowSha256':hashlib.sha256(code).hexdigest(),'instructionCount':len(decoded),
         'controlTransfers':[i for i in decoded if i['mnemonic'] in ('call','jmp')]}
    rows.append(row);windows.append({**row,'instructions':decoded})
report={'scope':__doc__,'sourceVersion':'18.8.50_EP1','sourceSha256':hashlib.sha256(raw).hexdigest(),
        'imageBase':pe.OPTIONAL_HEADER.ImageBase,'exportCount':len(exports),
        'tools':{'pefile':pefile.__version__,'capstone':capstone.__version__},'windows':rows,
        'limits':['Windows are bounded byte samples, not recovered complete functions',
                  'Virtual calls, internal targets, stat defaults and recharge behavior remain unresolved']}
if report['sourceSha256']!='8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320':
    raise ValueError('Manual code-window RVAs require the reviewed original binary')
report['candidateWindows']=[]
for label,rva,size in [('Burst stat arithmetic',0x9ac2e,0x8c),('WeaponItem initialization assignment',0x9bbfb,0x69),
                       ('Special skill dispatch',0x9aadb,0x3b),('WeaponItem initializer entry',0x9bb4f,0x20),
                       ('WeaponItem constructor vtables and initializer call',0x9c310,0x80),
                       ('Stat getter and setter forwarding',0x3d90,0x24),('Arithmetic result conversion',0x9ad30,0xe),
                       ('SSE and x87 integer conversion helpers',0x13ef30,0xab),
                       ('Special lock membership and value lookup',0x63e1d,0x4b),
                       ('Special lock duration adjustment',0x63ad5,0xa6),
                       ('Special lock insertion',0x64ca7,0x54),
                       ('Special lock extension',0x6580c,0x3d),
                       ('Special lock insertion caller',0x5e588,0x15),
                       ('CharacterAction message lock forwarding',0x7264c,0x45),
                       ('CharacterAction dispatcher switch',0x5d3a0,0x1f),
                       ('CharacterAction lock extension arguments',0x5e505,0x16),
                       ('Special action lock gate',0x2819e,0x3b),
                       ('Special action rejection return',0x282e1,4),
                       ('Remaining-time display decomposition',0x64549,0x3e),
                       ('Special lock decrement and expiry',0x646ce,0x33),
                       ('Lock vector erase',0x51cc5,0x2d),('Lock entry copy loop',0x128648,0x29),
                       ('GameTime-gated lock updater call',0x5b46b,0x22),
                       ('GameTime constructor identity',0xb03f,9),
                       ('GameTime interval flag',0xb338,0x75),
                       ('SpecialAttackInfo deserialization and serialization',0xa1b67,0x9b),
                       ('SpecialAttackInfo dispatch',0xa1c02,0x33),
                       ('Special result target value and ammunition update',0x6addd,0x50)]:
    code=pe.get_data(rva,size)
    instructions=[{'rva':i.address-pe.OPTIONAL_HEADER.ImageBase,'mnemonic':i.mnemonic,'operands':i.op_str}
                  for i in decoder.disasm(code,pe.OPTIONAL_HEADER.ImageBase+rva)]
    summary={'label':label,'rva':rva,'bytes':len(code),'sha256':hashlib.sha256(code).hexdigest()}
    report['candidateWindows'].append(summary);windows.append({**summary,'instructions':instructions})
report['arithmeticConstants']=[{'rva':rva,'format':fmt,'value':struct.unpack('<'+fmt,pe.get_data(rva,struct.calcsize(fmt)))[0]}
                              for rva,fmt in [(0x15e710,'d'),(0x156a68,'d'),(0x16016c,'f'),(0x160368,'d'),(0x160360,'f')]]
base=pe.OPTIONAL_HEADER.ImageBase
def pointer(va):return struct.unpack('<I',pe.get_data(va-base,4))[0]
vtable=base+0x1675ec;locator=pointer(vtable-4);descriptor=pointer(locator+12)
type_name=pe.get_string_at_rva(descriptor-base+8).decode('ascii')
if type_name!='.?AVWeaponItem_t@@':raise ValueError('Unexpected constructor RTTI')
report['weaponTypeEvidence']={'vtableRva':vtable-base,'locatorRva':locator-base,
    'typeDescriptorRva':descriptor-base,'decoratedName':type_name,
    'getterSlotRva':pointer(vtable+0x3c)-base,'setterSlotRva':pointer(vtable+0x44)-base,
    'initializerRva':0x9bb4f,'constructorCallRva':0x9c38b,'candidateStat374InitialValue':1000,
    'remainingQuestion':'Template override/default lookup applicability and final lock timer processing'}
time_vtable=base+0x156edc;time_locator=pointer(time_vtable-4);time_descriptor=pointer(time_locator+12)
time_name=pe.get_string_at_rva(time_descriptor-base+8).decode('ascii')
if time_name!='.?AVGameTime_t@@':raise ValueError('Unexpected timing object RTTI')
report['lockTickEvidence']={'updaterRva':0x646ce,'callerRva':0x5b488,
    'gateObject':time_name,'gateVtableRva':time_vtable-base,'gateByteOffset':24,
    'gateSetterRva':0xb371,'scope':'Per-call decrement and erase verified separately; full clock semantics unresolved'}
message_vtable=base+0x167cfc;message_locator=pointer(message_vtable-4)
message_descriptor=pointer(message_locator+12)
message_type=pe.get_string_at_rva(message_descriptor-base+8).decode('ascii')
if message_type!='.?AVSpecialAttackInfoIIR_t@@' or pointer(message_vtable+8)!=base+0xa1c02:
    raise ValueError('Unexpected special-result message RTTI or dispatch slot')
report['specialResultMessage']={'type':message_type,'vtableRva':message_vtable-base,
    'dispatchRva':0xa1c02,'weaponHolderHandlerRva':0x6ac00,
    'fieldOffsets':{'targetIdentity':24,'equipSlot':32,'amount':36,'ammoCount':40,'skill':44,'unknown':48},
    'scope':'Field ordering corroborated by local AOSharp schema; final unknown field remains opaque'}
direct=[];absolute=[]
for section in pe.sections:
    data=section.get_data()
    if section.Characteristics&0x20000000:
        for offset in range(len(data)-4):
            if data[offset] in (0xe8,0xe9) and section.VirtualAddress+offset+5+struct.unpack_from('<i',data,offset+1)[0]==0x9aadb:
                direct.append(section.VirtualAddress+offset)
    offset=0
    while True:
        offset=data.find(struct.pack('<I',base+0x9aadb),offset)
        if offset<0:break
        absolute.append(section.VirtualAddress+offset);offset+=1
report['arithmeticReferenceSearch']={'targetRva':0x9aadb,'directCallOrJumpCandidates':direct,
    'absoluteAddressCandidates':absolute,'limit':'Byte-pattern search in this DLL only; absence does not prove dead code'}
local=root/'Research/ClientReference/special-export-windows.json'
local.write_text(json.dumps(windows,indent=2)+'\n')
(root/'Artifacts/client-special-export-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
