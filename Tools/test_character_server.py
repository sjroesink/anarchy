"""Exercise the separate server over HTTP, including hostile and concurrent requests."""
import argparse,concurrent.futures,json,os,socket,subprocess,time,urllib.request,urllib.error,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--unity-client',type=Path);args=parser.parse_args()
run=root/'Artifacts'/('server-test-'+uuid.uuid4().hex)
run.mkdir()
with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
env=dict(os.environ,AO_SERVER_PORT=str(port),AO_SERVER_DATA=str(run/'characters'))
url=f'http://127.0.0.1:{port}'
log=(run/'server.log').open('w')
def launch():return subprocess.Popen(['dotnet',str(root/'Server/bin/Debug/net10.0/Anarchy.Server.dll')],env=env,stdout=log,stderr=log,creationflags=0x08000000)
def request(path,body=None,token=None):
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 data=None if body is None else json.dumps(body).encode()
 req=urllib.request.Request(url+path,data=data,headers=headers)
 try:
  with urllib.request.urlopen(req,timeout=10) as r:return r.status,json.loads(r.read() or b'{}')
 except urllib.error.HTTPError as e:
  raw=e.read()
  try:value=json.loads(raw or b'{}')
  except json.JSONDecodeError:value={}
  return e.code,value
def ready(process):
 for _ in range(100):
  if process.poll() is not None:raise RuntimeError('Server exited; see '+str(run))
  try:
   if request('/health')[0]==200:return
  except (OSError,urllib.error.URLError):pass
  time.sleep(.05)
 raise RuntimeError('Server did not become ready')
checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name)
process=launch()
try:
 ready(process)
 check(request('/character')[0]==401,'anonymous character access rejected')
 status,session=request('/sessions',{});token=session['token']
 check(status==200 and session['character']['ip']==1500,'server creates its own character')
 check(request('/character',token='0'*64)[0]==401,'unknown session rejected')
 status,r=request('/character/train',{'statId':116,'revision':0,'ip':999999},token)
 check(status==400,'client supplied IP rejected')
 check(request('/character',token=token)[1]['ip']==1500,'rejected payload did not mutate state')
 status,r=request('/character/train',{'statId':116,'revision':0},token)
 check(status==200 and r['character']['ip']==1495 and r['character']['investments'][116]==1,'server calculates training cost')
 check(request('/character/train',{'statId':116,'revision':0},token)[0]==409,'replayed revision rejected')
 with concurrent.futures.ThreadPoolExecutor(2) as pool:
  outcomes=list(pool.map(lambda _:request('/character/train',{'statId':116,'revision':1},token)[0],range(2)))
 check(sorted(outcomes)==[200,409],'concurrent revision applies at most once')
 state=request('/character',token=token)[1]
 check(state['ip']==1489 and state['revision']==2,'concurrent accounting correct')
 for stat in [-1,2147483647,138]:
  check(request('/character/train',{'statId':stat,'revision':2},token)[0]==400,'nontrainable stat rejected '+str(stat))
 check(request('/character/train',{'statId':116,'revision':2,'padding':'x'*5000},token)[0]==413,'oversized body rejected')
 other=request('/sessions',{})[1]
 check(request('/character',token=other['token'])[1]['ip']==1500,'sessions have isolated character state')
 process.terminate();process.wait(timeout=10)
 process=launch();ready(process)
 check(request('/character',token=token)[1]==state,'server character persists across process restart')
 if args.unity_client:
  capture=run/'unity-server.log'
  client=subprocess.Popen([str(args.unity_client.resolve()),'-server',url,'-qaServer','-screen-fullscreen','0','-logFile',str(capture)],creationflags=0x08000000)
  try:
   client.wait(timeout=45)
   check(client.returncode==0 and 'SERVER_CLIENT_QA_OK' in capture.read_text(),'Unity player trains through the separate server')
  finally:
   if client.poll() is None:client.terminate();client.wait(timeout=10)
 report={'passed':len(checks),'checks':checks,'scope':'Development skill server only; not complete multiplayer or anti-cheat'}
 (root/'Artifacts/character-server-validation.json').write_text(json.dumps(report,indent=2)+'\n')
 print('PASS:',len(checks),'separate character-server checks')
finally:
 if process.poll() is None:process.terminate();process.wait(timeout=10)
 log.close()
