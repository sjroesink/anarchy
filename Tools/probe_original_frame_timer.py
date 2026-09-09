"""Probe original AFCM high-resolution FrameProcess; boundary rounding is not yet independently verified."""
import csv,hashlib,itertools,json,struct,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'Research/ReferenceTools'))
import pefile,unicorn
from unicorn.x86_const import *
raw=(root/'Research/ClientReference/PatchWorking/app/AFCM.dll').read_bytes()
digest=hashlib.sha256(raw).hexdigest()
if digest!='d04a6eef227feb0bd6fed7589acfb7e74cf329bfef8da92938608d2ede414381':raise ValueError('Unreviewed binary')
pe=pefile.PE(data=raw);base=pe.OPTIONAL_HEADER.ImageBase
results=[]
native={(int(r["controlWord"]),int(r["microseconds"])):int(r["nativeMilliseconds"]) for r in csv.DictReader((root/"Artifacts/native-timer-precision.csv").open())}
for control,microseconds in itertools.product([0x7f,0x27f,0x37f],[1000,16000,16667,33333,1000000,3500000]):
 conversions=[]
 uc=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32)
 uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);uc.mem_write(base,pe.get_memory_mapped_image())
 uc.mem_map(0x200000,0x10000);uc.reg_write(UC_X86_REG_FPCW,control)
 state=0x202000;stack=0x207000;stop=0x208000
 uc.mem_write(state+0x14,struct.pack('<I',state+0x10))
 uc.mem_write(base+0x1703c,struct.pack('<f',2000))
 uc.mem_write(base+0x17090,struct.pack('<q',10000000))
 uc.mem_write(base+0x17088,struct.pack('<d',0))
 for offset,stub in [(0xa014,0x209000),(0xa018,0x209010),(0xa01c,0x209020)]:uc.mem_write(base+offset,struct.pack('<I',stub))
 def hook(u,address,size,data):
  if address==base+0x66e9:conversions.append(u.reg_read(UC_X86_REG_EAX))
  if address==0x209020:raise ValueError('Unexpected sleep; this fixture does not cover rate limiting')
  if address not in (0x209000,0x209010):return
  esp=u.reg_read(UC_X86_REG_ESP);ret,arg=struct.unpack('<2I',u.mem_read(esp,8))
  u.mem_write(arg,struct.pack('<q',1000000 if address==0x209000 else 10000000+microseconds))
  u.reg_write(UC_X86_REG_EAX,1);u.reg_write(UC_X86_REG_ESP,esp+8);u.reg_write(UC_X86_REG_EIP,ret)
 uc.hook_add(unicorn.UC_HOOK_CODE,hook)
 uc.mem_write(stack,struct.pack('<I',stop));uc.reg_write(UC_X86_REG_ESP,stack);uc.reg_write(UC_X86_REG_ECX,state)
 uc.emu_start(base+0x65d3,stop,timeout=1000000,count=1000)
 if uc.reg_read(UC_X86_REG_EIP)!=stop:raise ValueError('Frame timer did not return')
 row={'controlWord':control,'firstConversion':conversions[0],'nativeConversion':native[(control,microseconds)],'nativeMatch':conversions[0]==native[(control,microseconds)],'microseconds':microseconds,'delta':struct.unpack('<f',uc.mem_read(state+0x10,4))[0],
      'smoothDelta':struct.unpack('<f',uc.mem_read(state+0x20,4))[0],
      'tickDelta':struct.unpack('<I',uc.mem_read(state+0x28,4))[0],
      'carry':struct.unpack('<d',uc.mem_read(base+0x17088,8))[0]}
 results.append(row)
report={'scope':__doc__,'sourceSha256':digest,'controlWords':[0x7f,0x27f,0x37f],'completedFixtures':len(results),'results':results,
 'limits':['Synthetic successful performance-counter calls; rate limiting and fallback are excluded',
 'Observed emulation output, not an independently verified recovered formula',
 'First conversion matches native arithmetic for all sampled control words; full timer and runtime control word remain unverified',
 'Virtual engine call scheduling remains unresolved']}
report['nativeFirstConversionMatches']=sum(row['nativeMatch'] for row in results)
exe_raw=(root/'Research/ClientReference/PatchWorking/app/Anarchy.exe').read_bytes()
exe_digest=hashlib.sha256(exe_raw).hexdigest()
if exe_digest!='4243068cd935402cdee41a5609bcde2fc048a2b317641900e47d98165ae2ef48':raise ValueError('Unreviewed executable')
exe=pefile.PE(data=exe_raw)
report['precisionSetterEvidence']={'sourceSha256':exe_digest,'callRva':0x4a0f8,'callerRva':0x49baa,
 'newControl':0x10000,'mask':0x30000,'meaning':'_PC_53 under _MCW_PC',
 'windowHex':exe.get_data(0x4a0e8,0x1b).hex(),
 'limit':'Setter path identified; later render-device FPU changes remain unresolved'}
(root/'Artifacts/original-frame-timer-probe.json').write_text(json.dumps(report,indent=2)+'\n')
print('Completed:',len(results),'original frame-timer probes; boundary rounding remains unverified')

