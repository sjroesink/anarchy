using System;
using System.IO;
using Reborn;
using UnityEngine;

public static class AoEffectsValidation
{
    public static void Run()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{count++;if(!ok)throw new Exception("AO effects: "+message);};
        var b=new CharacterBuild();int skill=b.Value(116),cost=b.Cost(116),baseValue=b.Base(116);
        var starter=new CharacterBuild();var starterCrystal=new AoItemInstance(29092,1);starter.inventory.items.Add(starterCrystal);
        check(starter.UploadNanoCrystal(starterCrystal.instanceId,out _)&&starter.KnowsNano(29091),"Soldier base skills can upload Body Boost");
        int baselineHealth=starter.MaxHealth;starter.Effects.Apply(29091,1);
        check(starter.MaxHealth==baselineHealth+20&&starter.UsedNcu==1,"Body Boost modifies real maximum HP");
        starter.Effects.Tick(14400);check(starter.MaxHealth==baselineHealth&&starter.UsedNcu==0,"Body Boost expiration restores maximum HP");
        var otherProfession=new CharacterBuild{profession=10};var wrongCrystal=new AoItemInstance(29092,1);otherProfession.inventory.items.Add(wrongCrystal);
        check(!otherProfession.UploadNanoCrystal(wrongCrystal.instanceId,out _)&&otherProfession.inventory.Find(wrongCrystal.instanceId)!=null,"Soldier starter crystal checks profession");
        var learner=new CharacterBuild();var crystal=new AoItemInstance(26481,4);learner.inventory.items.Add(crystal);
        check(!learner.KnowsNano(26354),"fresh character has no invented learned program");
        check(!learner.UploadNanoCrystal("unowned",out _),"unowned crystal rejected");
        check(!learner.UploadNanoCrystal(crystal.instanceId,out _)&&learner.inventory.Find(crystal.instanceId)!=null,"insufficient upload skills retain crystal");
        learner.investments[129]=25;learner.investments[122]=25;
        check(learner.UploadNanoCrystal(crystal.instanceId,out _)&&learner.KnowsNano(26354)&&learner.inventory.Find(crystal.instanceId)==null,"successful upload learns and consumes crystal");
        var duplicate=new AoItemInstance(26481,4);learner.inventory.items.Add(duplicate);
        check(!learner.UploadNanoCrystal(duplicate.instanceId,out _)&&learner.inventory.Find(duplicate.instanceId)!=null,"duplicate upload retains unused crystal");
        var restored=CharacterBuild.ParseSave(JsonUtility.ToJson(learner));
        check(restored!=null&&restored.KnowsNano(26354)&&restored.inventory.Find(duplicate.instanceId)!=null,"learned programs and remaining crystals survive save");
        foreach(var definition in AoSelfEffects.Catalog.effects)
        {
            if(definition.id==70308)continue;
            var candidate=new CharacterBuild();int stat=definition.modifiers[0].statId,before=candidate.Value(stat);
            check(candidate.Effects.Apply(definition.id,definition.ncu)&&candidate.Value(stat)==before+definition.modifiers[0].amount,"general skill effect "+definition.id);
            candidate.Effects.Cancel(definition.id);check(candidate.Value(stat)==before,"general skill cancellation "+definition.id);
        }
        var stacking=new CharacterBuild();
        int[] composites={215264,223348,223360,223364,223380,287040,287046};
        var combinedComposites=new CharacterBuild();
        foreach(int id in composites)
        {
            var definition=AoSelfEffects.Get(id);var candidate=new CharacterBuild();
            int[] initial=Array.ConvertAll(definition.modifiers,m=>candidate.Value(m.statId));
            check(candidate.Effects.Apply(id,4),"composite skill effect applies "+id);
            for(int i=0;i<initial.Length;i++)check(candidate.Value(definition.modifiers[i].statId)==initial[i]+20,"complete composite modifier "+id+":"+definition.modifiers[i].statId);
            candidate.Effects.Tick(28800);
            for(int i=0;i<initial.Length;i++)check(candidate.Value(definition.modifiers[i].statId)==initial[i],"complete composite expiry "+id+":"+definition.modifiers[i].statId);
            check(combinedComposites.Effects.Apply(id,28),"different composite groups coexist "+id);
        }
        check(combinedComposites.UsedNcu==28,"seven non-overlapping composite groups reserve 28 NCU");
        check(AoSelfEffects.Get(287046).MeetsCastRequirements(new CharacterBuild()),"Utility uses its sourced PM/SI four requirement");
        var tradeskillCaster=new CharacterBuild();
        check(!AoSelfEffects.Get(287040).MeetsCastRequirements(tradeskillCaster),"Tradeskill does not inherit Utility's lower requirement");
        tradeskillCaster.investments[129]=14;tradeskillCaster.investments[122]=14;
        check(AoSelfEffects.Get(287040).MeetsCastRequirements(tradeskillCaster),"Tradeskill accepts exact PM/SI twenty boundary");
        var composite=new CharacterBuild();
        int[] nanoSkills={127,128,129,122,131,130};
        int[] beforeComposite=Array.ConvertAll(nanoSkills,s=>composite.Value(s));
        foreach(int stat in nanoSkills)
        {
            var single=Array.Find(AoSelfEffects.Catalog.effects,e=>e.id!=223380&&e.modifiers.Length==1&&e.modifiers[0].statId==stat&&e.modifiers[0].amount==20);
            check(single!=null&&composite.Effects.Apply(single.id,24),"individual nano expertise before composite "+stat);
        }
        check(composite.UsedNcu==24,"six individual nano expertises reserve 24 NCU");
        check(!composite.Effects.Apply(223380,3)&&composite.UsedNcu==24,"failed composite replacement leaves all six buffs intact");
        check(composite.Effects.Apply(223380,4)&&composite.UsedNcu==4,"composite atomically replaces all six lines and reuses NCU");
        for(int i=0;i<nanoSkills.Length;i++)check(composite.Value(nanoSkills[i])==beforeComposite[i]+20,"composite does not double nano skill "+nanoSkills[i]);
        var lower=Array.Find(AoSelfEffects.Catalog.effects,e=>e.id!=223380&&e.modifiers.Length==1&&e.modifiers[0].statId==128&&e.modifiers[0].amount==20);
        check(!composite.Effects.Apply(lower.id,100)&&composite.UsedNcu==4,"single expertise cannot overwrite a higher-order composite through a secondary line");
        composite.Effects.Tick(10);check(composite.Effects.Apply(223380,4)&&composite.Effects.Remaining(223380)==28800,"composite refresh keeps one NCU reservation");
        composite.Effects.Cancel(223380);
        for(int i=0;i<nanoSkills.Length;i++)check(composite.Value(nanoSkills[i])==beforeComposite[i],"composite cancellation removes every nano skill modifier "+nanoSkills[i]);
        check(stacking.Effects.Apply(26354,2)&&stacking.Value(116)==stacking.Base(116)+stacking.Trickle(116)+10,"Proficiency adds ten skill");
        check(!stacking.Effects.Apply(26370,3)&&stacking.Effects.Remaining(26354)==1800,"failed replacement retains original buff");
        check(stacking.Effects.Apply(26370,4)&&stacking.Effects.Remaining(26354)==0&&stacking.UsedNcu==4,"upgrade reuses replaced NCU");
        stacking.Effects.Tick(5);
        check(!stacking.Effects.Apply(26354,100)&&stacking.Effects.Remaining(26370)==1795,"weaker buff cannot replace or refresh stronger");
        check(stacking.Effects.Apply(70308,8)&&stacking.UsedNcu==8,"different lines coexist after upgrade");
        check(!b.Effects.Apply(26370,3)&&b.UsedNcu==0&&b.Value(116)==skill,"insufficient NCU is atomic");
        check(!b.Effects.Apply(999999,100),"unknown effects cannot execute");
        check(b.Effects.Apply(26370,b.MaxNcu)&&b.Value(116)==skill+20,"Expertise modifies actual Assault Rifle stat");
        check(b.AttackRating()==skill+20,"attack rating counts shared skill modifier exactly once");
        check(b.Base(116)==baseValue&&b.Cost(116)==cost,"temporary buff does not change IP investment or price");
        check(b.UsedNcu==4&&b.Effects.Remaining(26370)==1800,"sourced expertise NCU and duration");
        b.Effects.Tick(30);
        check(b.Effects.Apply(26370,4)&&b.UsedNcu==4&&b.Value(116)==skill+20&&b.Effects.Remaining(26370)==1800,"same program refresh neither doubles modifier nor NCU");
        check(!b.Effects.Apply(70308,7)&&b.UsedNcu==4,"second effect must fit remaining NCU");
        check(b.Effects.Apply(70308,8)&&b.UsedNcu==8,"two supported effects coexist");
        int[] reflected={205,206,207,208,216,217,219,225};
        int[] maxima={475,476,477,478,479,480,482,483};
        for(int i=0;i<reflected.Length;i++)check(b.Value(reflected[i])==75&&b.Value(maxima[i])==7,"TMS damage type "+i);
        check(b.Value(218)==0&&b.Value(481)==0,"TMS does not invent reflect nano damage");
        var clone=CharacterBuild.ParseSave(JsonUtility.ToJson(b));
        check(clone!=null&&clone.UsedNcu==0&&clone.Value(116)==skill,"transient nano effects do not leak into permanent save");
        b.Effects.Tick(19.99f);check(b.Value(205)==75,"TMS remains active before expiry");
        b.Effects.Tick(.02f);check(b.Value(205)==0&&b.Value(475)==0&&b.UsedNcu==4,"expiry removes only TMS modifiers and NCU");
        check(b.Effects.Cancel(26370)&&b.Value(116)==skill&&b.UsedNcu==0,"explicit cancellation restores base state");
        check(!b.Effects.Cancel(26370)&&b.UsedNcu==0,"repeated cancellation cannot underflow NCU");
        b.Effects.Apply(26370,8);b.Effects.Tick(1800);
        check(b.Value(116)==skill&&b.UsedNcu==0,"exact expertise expiry boundary");
        File.WriteAllText("../Artifacts/effects-validation.txt",$"PASS: {count} timed-effect and stat integration checks.\n{AoSelfEffects.Catalog.effects.Length} self-effect definitions; reviewed line replacement only, full nano and original-client conformance remain incomplete.\n");
    }
}
