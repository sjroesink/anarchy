"""Preserve original Burst inputs, including absent cycle stats; no inferred defaults."""
import hashlib,json
from collections import Counter
from pathlib import Path

def project(item):
    stats=item['stats']
    if not stats.get('30',0)&2048:return None
    return {'id':item['id'],'name':item.get('name',''),'payloadSha256':item['payloadSha256'],
            'referenceOrigin':item.get('referenceOrigin'),
            'canFlags':stats['30'],'burstSkillStatId':148,
            'burstCyclePresent':'374' in stats,'burstCycleRaw':stats.get('374'),
            'attackTimeRaw':stats.get('294'),'rechargeTimeRaw':stats.get('210'),
            'clipSizeRaw':stats.get('212'),
            'minimumDamageRaw':stats.get('286'),'maximumDamageRaw':stats.get('285')}

def main():
    root=Path(__file__).resolve().parents[1]
    source=root/'Research/ClientReference/Catalog-through-18.8.62/items.jsonl'
    output=source.with_name('burst-reference.jsonl');count=0;present=0;zero=0;missing_timing=0;samples=[]
    cycles=Counter()
    with source.open(encoding='utf-8') as stream,output.open('w',encoding='utf-8') as target:
        for line in stream:
            result=project(json.loads(line))
            if result is None:continue
            target.write(json.dumps(result,ensure_ascii=False)+'\n');count+=1
            if result['burstCyclePresent']:
                present+=1;cycles[result['burstCycleRaw']]+=1
                if result['burstCycleRaw']==0:zero+=1
            if result['attackTimeRaw'] is None or result['rechargeTimeRaw'] is None:missing_timing+=1
            if result['id']==121569:samples.append(result)
    report={'scope':__doc__,'catalogue':'Base 18.8.50 plus inspected item patches through 18.8.62; not a fully patched client',
            'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
            'outputSha256':hashlib.sha256(output.read_bytes()).hexdigest(),
            'burstFlaggedRecords':count,'explicitCycleRecords':present,'explicitZeroCycleRecords':zero,
            'absentCycleRecords':count-present,'missingAttackOrRechargeRecords':missing_timing,
            'cycleValueCounts':dict(sorted(cycles.items())),'starter':samples,
            'openQuestions':['Absent BurstRecharge default','Recharge cap and rounding across weapon speeds',
                             'Special hit resolution, ammunition and concurrent attack behavior']}
    (root/'Artifacts/burst-reference-audit.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({key:report[key] for key in ['burstFlaggedRecords','explicitCycleRecords','explicitZeroCycleRecords','absentCycleRecords','missingAttackOrRechargeRecords','starter']},indent=2))

if __name__=='__main__':main()
