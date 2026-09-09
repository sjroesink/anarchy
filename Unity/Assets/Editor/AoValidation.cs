using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Reborn;
public static class AoValidation
{
    public static void Run()
    {
        int count=0;Action<bool,string> check=(ok,name)=>{count++;if(!ok)throw new Exception("AO reference check failed: "+name);};
        var rules=AoReference.Rules;check(rules.stats.Length==626,"stat import count");check(rules.skills.Length==75,"75 identities including archived skills");
        check(rules.professions.Length==14&&rules.breeds.Length==4,"profession and breed identities");
        foreach(var skill in rules.skills)
        {
            if(skill.id>=100&&!skill.historical){check(skill.weights.Sum()==100,"ability weights "+skill.id);check(skill.costTenths.Length==16,"profession mapping "+skill.id);}
        }
        check(AoReference.Skill(127).weights.SequenceEqual(new[]{0,0,0,80,0,20}),"MM depends on INT/PSY, not the old emulator AGI typo");
        check(AoReference.Skill(163).weights.SequenceEqual(new[]{0,0,50,50,0,0}),"Chemistry INT/STA");
        check(AoReference.Skill(156).weights.SequenceEqual(new[]{20,40,40,0,0,0}),"Run speed STR/AGI/STA");
        var b=new CharacterBuild();check(b.Value(116)==6,"initial assault rifle 5 + 1 trickle");check(b.Value(113)==6,"Rifle is a separate stat");
        check(b.Cost(116)==5&&b.Cost(113)==10,"Soldier profession multipliers");
        b.profession=5;check(b.Cost(113)==6,"Agent Rifle cost floor 5*1.3");b.profession=1;
        check(b.TrainStat(116,out _)&&b.ip==1495&&b.Value(116)==7,"one point IP accounting");
        b.investments[17]=4;b.investments[20]=4;check(b.Trickle(113)==2,"Rifle 60 AGI / 40 SEN weighted quarter");
        b.ip=0;check(!b.TrainStat(116,out _),"reject IP underflow");check(!b.TrainStat(138,out _),"archived Swimming not trained as a modern skill");
        b=new CharacterBuild();check(b.MaxHealth==34&&b.MaxNano==32,"Solitus Soldier baseline fixture");
        check(AoReference.NextXp(1)==1450&&AoReference.NextXp(2)==2600,"AO first XP thresholds");
        b.xp=1300;b.Reward(1);check(b.level==2&&b.xp==0&&b.ip==5500,"reference XP crossing and low-title IP grant");
        var clone=JsonUtility.FromJson<CharacterBuild>(JsonUtility.ToJson(b));check(clone.Valid()&&clone.level==2,"schema3 stat investment save roundtrip");
        clone.investments=null;check(!clone.Valid(),"malformed save rejected");
        var solar=AoReference.Items.items.Single(i=>i.lowId==121569);check(solar.name=="Solar-Powered Assault Rifle"&&solar.lowQl==1&&solar.highQl==1,"real starter identity");
        check(!AoReference.Items.executionDataComplete,"index does not claim executable item completeness");
        var expertise=AoReference.Nanos.nanos.First(n=>n.id==26370);check(expertise.nanoCost==40&&expertise.requirements.Any(r=>r.statId==129&&r.amount==61)&&expertise.requirements.Any(r=>r.statId==122&&r.amount==61),"Expertise requirements");
        var tms=AoReference.Nanos.nanos.First(n=>n.id==70308);check(tms.nanoCost==126&&tms.requirements.Any(r=>r.statId==131&&r.amount==47),"TMS Mk I metadata");
        Directory.CreateDirectory("../Artifacts");File.WriteAllText("../Artifacts/ao-reference-validation.txt",$"PASS: {count} reference/invariant checks.\nThese test import and selected sourced fixtures, NOT full AO gameplay conformance.\n");
        Debug.Log("AO_REFERENCE_VALIDATION_OK: "+count);
    }
}
