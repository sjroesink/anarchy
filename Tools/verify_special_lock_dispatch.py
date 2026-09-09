"""Verify original CharacterAction lock routing with synthetic decoded fields, not captured packets."""
import hashlib, json, struct, sys
from pathlib import Path
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'Research/ReferenceTools'))
import pefile, unicorn
from unicorn.x86_const import UC_X86_REG_EBP, UC_X86_REG_EBX, UC_X86_REG_EDI, UC_X86_REG_ESP, UC_X86_REG_EIP, UC_X86_REG_ECX

raw = (root / 'Research/ClientReference/PatchWorking/app/Gamecode.dll').read_bytes()
digest = hashlib.sha256(raw).hexdigest()
if digest != '8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320':
    raise ValueError('Unreviewed binary')
pe = pefile.PE(data=raw)
base = pe.OPTIONAL_HEADER.ImageBase
def pointer(rva): return struct.unpack('<I', pe.get_data(rva, 4))[0] - base
vtable = 0x161e00
descriptor = pointer(pointer(vtable - 4) + 12)
name = pe.get_string_at_rva(descriptor + 8).decode()
if name != '.?AVCharacterActionIIR_t@@' or pointer(vtable + 8) != 0x7264c:
    raise ValueError('Unexpected message type/handler')

results = []
for action, target, branch in [(20, 0x6580c, 0x5e505), (170, 0x64ca7, 0x5e588)]:
    index = pe.get_data(0x5f217 + action - 1, 1)[0]
    if pointer(0x5f07b + index * 4) != branch:
        raise ValueError('Unexpected original switch mapping')
    for duration in [0, 1, 40]:
        uc = unicorn.Uc(unicorn.UC_ARCH_X86, unicorn.UC_MODE_32)
        uc.mem_map(base, (pe.OPTIONAL_HEADER.SizeOfImage + 4095) & ~4095)
        uc.mem_write(base, pe.get_memory_mapped_image())
        uc.mem_map(0x200000, 0x10000)
        frame, actor, pair, manager, stack = 0x201000, 0x202000, 0x203000, 0x204000, 0x207000
        uc.mem_write(frame + 8, struct.pack('<I', action))
        uc.mem_write(frame + 0x14, struct.pack('<I', pair))
        uc.mem_write(pair, struct.pack('<2i', 148, duration))
        uc.mem_write(actor + 0x1bc, struct.pack('<I', manager))
        for reg, value in [(UC_X86_REG_EBP, frame), (UC_X86_REG_EBX, actor), (UC_X86_REG_EDI, 0), (UC_X86_REG_ESP, stack)]:
            uc.reg_write(reg, value)
        uc.emu_start(base + 0x5d3a0, base + target, timeout=1000000, count=100)
        if uc.reg_read(UC_X86_REG_EIP) != base + target or uc.reg_read(UC_X86_REG_ECX) != manager:
            raise ValueError('Wrong lock destination')
        count = 3 if action == 20 else 2
        args = struct.unpack('<' + 'i' * count, uc.mem_read(uc.reg_read(UC_X86_REG_ESP) + 4, count * 4))
        expected = (148, duration, 0) if action == 20 else (148, duration)
        if args != expected: raise ValueError('Lock arguments differ')
        results.append({'action': action, 'duration': duration, 'skill': 148, 'calleeRva': target, 'arguments': args})
report = {'scope': __doc__, 'sourceSha256': digest, 'messageType': name, 'vtableRva': vtable,
          'messageHandlerRva': 0x7264c, 'actorDispatcherRva': 0x5d310, 'passed': len(results), 'results': results,
          'limits': ['Emulation begins after dispatcher setup, with decoded synthetic arguments',
                     'Message handler field forwarding inspected statically; network deserialization not emulated',
                     'Upstream duration calculation and clock cadence remain unverified']}
(root / 'Artifacts/special-lock-dispatch-emulation.json').write_text(json.dumps(report, indent=2) + '\n')
print(f'PASS: {len(results)} original lock-dispatch comparisons')
