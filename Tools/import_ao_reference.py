"""Import versioned AO facts; never infer missing item execution data from names."""
import csv, hashlib, json, re, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ND=ROOT/'Research/Nadybot/src/Modules'
CELL=ROOT/'Research/CellAO/CellAO/Libraries/Source/CellAO.Stats'
OUT=ROOT/'Unity/Assets/Resources/AO'
OUT.mkdir(parents=True,exist_ok=True)
def readcsv(path):
    with path.open(encoding='utf-8-sig') as f:return list(csv.DictReader(line for line in f if not line.startswith('#')))
def write(name,obj):
    (OUT/(name+'.json')).write_text(json.dumps(obj,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
def rows(text,name):
    block=text.split(name+' =',1)[1].split('};',1)[0]
    return [[float(v.strip()) for v in s.split(',')] for s in re.findall(r'\{\s*([\d.,\s]+)\}',block)]
raw=(ROOT/'Research/aosharp/AOSharp.Common/GameData/Stat.cs').read_text()
stats=[{'id':int(v,16),'key':k} for k,v in re.findall(r'^\s*(\w+) = (0x[0-9A-Fa-f]+)',raw,re.M)]
assert len({r['id'] for r in stats})==len(stats)
keys={r['id']:r['key'] for r in stats}
names={int(r['id']):r['name'] for r in readcsv(ND/'ITEMS_MODULE/skills.csv')}
skill_update=(CELL/'SkillUpdate.cs').read_text()
costs={int(r[0]):r[1:] for r in rows(skill_update,'double[,] skillCosts')}
prof_matrix=[12,8,5,6,1,0,13,2,4,3,10,9,-1,7,11]
skills=[]
for r in readcsv(ND/'TRICKLE_MODULE/trickle.csv'):
    sid=int(r['skill_id'])
    weights=[int(round(float(r['amount'+a])*100)) for a in ['Str','Agi','Sta','Int','Sen','Psy']]
    assert sum(weights)==100,(sid,weights)
    skills.append({'id':sid,'key':keys[sid],'name':names.get(sid,r['name']),'category':r['groupName'], 'weights':weights,
                   'costTenths':[0]+[0 if c<0 else round(costs[sid][c]*10) for c in prof_matrix],
                   'historical':False,'dependencyEvidence':'Nadybot trickle.csv','costEvidence':'CellAO SkillUpdate.cs; live conformance not yet verified'})
for sid,label in [(138,'Swimming'),(140,'Map Navigation')]:
    skills.append({'id':sid,'key':keys[sid],'name':label,'category':'Historical','weights':[],'costTenths':[], 'historical':True,
                   'dependencyEvidence':'Not exposed in modern Nadybot skill table','costEvidence':'Not trainable in this live-reference UI'})
ability_rows=rows(skill_update,'double[,] attributeCost')
base_rows=rows(skill_update,'double[,] baseAttributes')[:4]
breeds=[]
for i,name in enumerate(['Solitus','Opifex','Nanomage','Atrox']):
    breeds.append({'id':i+1,'name':name,'baseAbilities':[int(v) for v in base_rows[i]],'costs':[int(r[i+1]) for r in ability_rows]})
for sid in range(16,22):
    skills.insert(sid-16,{'id':sid,'key':keys[sid],'name':keys[sid],'category':'Abilities','weights':[], 'costTenths':[], 'historical':False,'dependencyEvidence':'AO ability stat identity','costEvidence':'CellAO breed table; live conformance pending'})
profs=['Soldier','Martial Artist','Engineer','Fixer','Agent','Adventurer','Trader','Bureaucrat','Enforcer','Doctor','Nano-Technician','Meta-Physicist',None,'Keeper','Shade']
write('rules',{'schema':1,'reference':'live-target; mixed published reference snapshots (see provenance)','stats':stats,'skills':skills,'breeds':breeds,
               'professions':[{'id':i+1,'name':n} for i,n in enumerate(profs) if n],
               'levels':[{'level':int(r['level']),'xp':int(r['xpsk'])} for r in readcsv(ND/'LEVEL_MODULE/levels.csv')]})
items=[]
for r in readcsv(ND/'ITEMS_MODULE/aodb.csv'):
    items.append({'lowId':int(r['lowid']),'highId':int(r['highid']),'lowQl':int(r['lowql']),'highQl':int(r['highql']),
                  'name':r['name'],'iconId':int(r['icon']),'slots':int(r['slot']),'flags':int(r['flags']),
                  'inGame':r['in_game']=='1','froob':r['froob_friendly']=='1'})
write('items',{'schema':1,'sourceVersion':'18.08.58.01','executionDataComplete':False,'items':items})
nanos=[]
for r in readcsv(ND/'NANO_MODULE/nanos.csv'):
    nanos.append({'id':int(r['nano_id']),'crystalId':int(r['crystal_id'] or 0),'ql':int(r['ql'] or 0), 'name':r['nano_name'],
                  'nanoCost':int(r['nano_cost'] or 0),'professions':r['professions'],'line':r['strain'],'lineId':int(r['strain_id'] or 0),
                  'requirements':[{'statId':s,'amount':int(r[k])} for k,s in [('mm',127),('bm',128),('pm',129),('si',122),('ts',131),('mc',130),('min_level',54)] if r[k]],
                  'executionDataComplete':False})
write('nanos',{'schema':1,'nanos':nanos})
sources=[]
for repo in ['aosharp','CellAO','Nadybot']:
    directory=ROOT/'Research'/repo
    sources.append({'repository':subprocess.check_output(['git','-C',str(directory),'remote','get-url','origin'],text=True).strip(),
                    'commit':subprocess.check_output(['git','-C',str(directory),'rev-parse','HEAD'],text=True).strip()})
files=[ND/'TRICKLE_MODULE/trickle.csv',ND/'ITEMS_MODULE/aodb.csv',ND/'NANO_MODULE/nanos.csv',CELL/'SkillUpdate.cs']
write('provenance',{'sources':sources,'files':[{'path':str(p.relative_to(ROOT)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files],
                     'notVerified':['Exact selected live patch','Skill costs against live client','Full item requirements, effects and interpolation','Gameplay and world parity']})
notices=ROOT/'Unity/Assets/StreamingAssets/ThirdParty';notices.mkdir(parents=True,exist_ok=True)
(notices/'Nadybot-GPL-3.0.txt').write_text((ROOT/'Research/Nadybot/LICENSE').read_text(),encoding='utf-8')
(notices/'CellAO-BSD.txt').write_text(skill_update.split('#region License')[1].split('#endregion')[0].replace('// ','').replace('//',''),encoding='utf-8')
(ROOT/'Artifacts/reference-import.txt').write_text(f'Imported {len(stats)} stat IDs, {len(skills)} skill/ability entries (including 2 historical), {len(items)} item index records, {len(nanos)} nano metadata records.\nThis measures imported references, NOT full gameplay fidelity.\n',encoding='utf-8')
print((ROOT/'Artifacts/reference-import.txt').read_text())
