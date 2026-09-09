"""Check original DeltaTimer millisecond conversion; engine scheduling linkage remains incomplete."""
import hashlib, json, struct, sys
from pathlib import Path
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'Research/ReferenceTools'))
import pefile, unicorn
from unicorn.x86_const import UC_X86_REG_ECX, UC_X86_REG_ESP, UC_X86_REG_EAX, UC_X86_REG_EIP
raw = (root / 'Research/ClientReference/PatchWorking/app/DeltaTimer.dll').read_bytes()
digest = hashlib.sha256(raw).hexdigest()
if digest != '22f1e2566a2f563b56ee07898ac7fb6d71362e87f75d176084bdaf538c37b60b':
    raise ValueError('Unreviewed timer binary')
pe = pefile.PE(data=raw)
base = pe.OPTIONAL_HEADER.ImageBase
imports = {i.address: i.name for lib in pe.DIRECTORY_ENTRY_IMPORT for i in lib.imports}
if imports.get(base + 0x2084) != b'timeGetTime': raise ValueError('Unexpected clock import')
if struct.unpack('<d', pe.get_data(0x20a8, 8))[0] != 1000: raise ValueError('Unexpected divisor')
results = []
for origin, current in [(0, 0), (0, 16), (0, 1000), (1000, 61000), (0, 0x80000010), (0xfffffff0, 16)]:
    uc = unicorn.Uc(unicorn.UC_ARCH_X86, unicorn.UC_MODE_32)
    uc.mem_map(base, (pe.OPTIONAL_HEADER.SizeOfImage + 4095) & ~4095)
    uc.mem_write(base, pe.get_memory_mapped_image())
    uc.mem_map(0x200000, 0x10000)
    uc.mem_write(base + 0x3000, b'\x00')
    uc.mem_write(base + 0x3028, struct.pack('<I', origin))
    uc.mem_write(base + 0x2084, struct.pack('<I', 0x209000))
    # Substitute the OS reading only; execute subtraction, unsigned correction and division.
    uc.mem_write(0x209000, b'\xb8' + struct.pack('<I', current) + b'\xc3')
    uc.mem_write(0x207000, struct.pack('<I', 0x208000))
    uc.reg_write(UC_X86_REG_ECX, 0x202000)
    uc.reg_write(UC_X86_REG_ESP, 0x207000)
    uc.emu_start(base + 0x114b, 0x208000, timeout=1000000, count=100)
    if uc.reg_read(UC_X86_REG_EIP) != 0x208000: raise ValueError('Timer did not return')
    actual = struct.unpack('<f', uc.mem_read(0x202000, 4))[0]
    expected = struct.unpack('<f', struct.pack('<f', ((current - origin) & 0xffffffff) / 1000))[0]
    if actual != expected: raise ValueError('Timer conversion mismatch')
    results.append({'originMilliseconds': origin, 'currentMilliseconds': current, 'secondsFloat32': actual})
report = {'scope': __doc__, 'sourceSha256': digest, 'passed': len(results), 'results': results,
          'window': {'rva': 0x114b, 'hex': pe.get_data(0x114b, 0x47).hex()},
          'limits': ['OS timeGetTime return substituted with synthetic values',
                     'Initialization branch not covered; existing timer origin is supplied',
                     'AFCM smoothing and linkage to engine RunEngine calls are not established by this test']}
(root / 'Artifacts/original-timer-units-emulation.json').write_text(json.dumps(report, indent=2) + '\n')
print(f'PASS: {len(results)} original timer unit conversions')
