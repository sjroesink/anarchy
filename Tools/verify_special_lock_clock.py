"""Emulate the original lock interval gate using synthetic engine delta-time inputs."""
import hashlib, json, struct, sys
from pathlib import Path
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'Research/ReferenceTools'))
import pefile, unicorn
from unicorn.x86_const import UC_X86_REG_ESI, UC_X86_REG_EDX, UC_X86_REG_EIP

def load(name, expected):
    raw = (root / 'Research/ClientReference/PatchWorking/app' / name).read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    if digest != expected: raise ValueError('Unreviewed binary: ' + name)
    return pefile.PE(data=raw), digest

pe, digest = load('Gamecode.dll', '8b96f9b319ac43d14b721767f57b07d0e38a6a793032e7b1096dc40559959320')
n3, n3digest = load('N3.dll', '1cc4ea47f8896f71f55013d7c4948885e8628ff90ceb7ef0568f905b18c989cd')
exports = {e.name.decode(): e.address for e in n3.DIRECTORY_ENTRY_EXPORT.symbols if e.name}
if exports.get('?GetDeltaTime@n3Engine_t@@QBEMXZ') != 0x159f or n3.get_data(0x159f, 4) != bytes.fromhex('d94168c3'):
    raise ValueError('Engine delta-time getter changed')
if exports.get('?RunEngine@n3Engine_t@@UAEXM@Z') != 0x66ad:
    raise ValueError('Engine update export changed')
base = pe.OPTIONAL_HEADER.ImageBase
sequences = [[0], [.5, .5, .5, .5], [1, 1, 1], [1/60] * 120,
             [1/30] * 60, [.999, .001, .001], [3.5, 0, 0, .5], [1.5, .5, .5, .5]]
results = []
for deltas in sequences:
    uc = unicorn.Uc(unicorn.UC_ARCH_X86, unicorn.UC_MODE_32)
    uc.mem_map(base, (pe.OPTIONAL_HEADER.SizeOfImage + 4095) & ~4095)
    uc.mem_write(base, pe.get_memory_mapped_image())
    uc.mem_map(0x200000, 0x10000)
    state, engine, slot, start = 0x202000, 0x204000, 0x203000, 0x208000
    uc.mem_write(base + 0x154f44, struct.pack('<I', slot))
    uc.mem_write(slot, struct.pack('<I', engine))
    # Reset the x87 stack between isolated gate calls; no gate instruction is replaced.
    uc.mem_write(start, bytes.fromhex('dbe3e9') + struct.pack('<i', base + 0xb32e - (start + 7)))
    elapsed = previous = 0.0
    steps = []
    for delta in deltas:
        delta = struct.unpack('<f', struct.pack('<f', delta))[0]
        uc.mem_write(engine + 0x68, struct.pack('<f', delta))
        uc.reg_write(UC_X86_REG_ESI, state)
        uc.reg_write(UC_X86_REG_EDX, 0)
        uc.emu_start(start, base + 0xb3ad, timeout=1000000, count=100)
        if uc.reg_read(UC_X86_REG_EIP) != base + 0xb3ad: raise ValueError('Gate did not finish')
        elapsed += delta
        expected_flag = elapsed >= previous + 1
        if expected_flag: previous = elapsed
        actual_elapsed, actual_previous = struct.unpack('<2d', uc.mem_read(state + 0x28, 16))
        flag = uc.mem_read(state + 0x18, 1)[0]
        if flag != int(expected_flag) or actual_elapsed != elapsed or actual_previous != previous:
            raise ValueError(f'Clock mismatch: {delta}, {flag}, {actual_elapsed}, {actual_previous}; expected {expected_flag}, {elapsed}, {previous}')
        steps.append({'delta': delta, 'tick': bool(flag), 'elapsed': actual_elapsed, 'previous': actual_previous})
    results.append({'steps': steps, 'tickCount': sum(s['tick'] for s in steps)})
report = {'scope': __doc__, 'sourceSha256': digest, 'n3Sha256': n3digest,
          'passed': sum(len(r['steps']) for r in results), 'sequenceCount': len(results), 'results': results,
          'engineEvidence': {'deltaGetterRva': 0x159f, 'deltaOffset': 0x68, 'runEngineRva': 0x66ad,
                             'windows': [{'rva': rva, 'bytes': size, 'sha256': hashlib.sha256(n3.get_data(rva, size)).hexdigest(),
                                          'hex': n3.get_data(rva, size).hex()} for rva, size in [(0x159f, 4), (0x66ad, 0x46)]]},
          'limits': ['Synthetic nonnegative finite frame deltas, not a running client capture',
                     'Only interval-gate instructions execute; engine and actor update scheduling are outside this test',
                     'Platform-clock conversion into RunEngine input and final Burst duration calculation remain unresolved']}
(root / 'Artifacts/special-lock-clock-emulation.json').write_text(json.dumps(report, indent=2) + '\n')
print(f"PASS: {report['passed']} original clock-gate updates across {len(results)} sequences")
