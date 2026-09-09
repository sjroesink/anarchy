using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reborn
{
    // AO stat identities and published tables. Conformance gaps are tracked in Docs/FIDELITY.md.
    [Serializable] public sealed class CharacterBuild
    {
        public int schema=3,level=1,xp,credits,ip=1500,breed=1,profession=1;
        public float aggression=.65f;
        public int[] investments=new int[169];
        public AoInventory inventory=new AoInventory();
        public List<int> learnedNanos=new List<int>();
        public AoHotbar hotbar=new AoHotbar();
        public bool KnowsNano(int id)=>learnedNanos.Contains(id);
        public bool UploadNanoCrystal(string instanceId,out string reason)
        {
            var item=inventory.Find(instanceId);var definition=item?.Definition;
            if(definition==null || item.slot!=0 || definition.uploadNanoId==0 || definition.unsupportedActions.Length!=0)
            {reason="An owned supported nano crystal is required.";return false;}
            int id=definition.uploadNanoId;
            if(AoSelfEffects.Get(id)==null){reason="This nano is not implemented yet.";return false;}
            if(KnowsNano(id)){reason="Nano program already learned.";return false;}
            foreach(var requirement in definition.requirements)
            {
                int value=requirement.statId==368?profession:Value(requirement.statId);
                bool valid=requirement.comparison=="AtLeast"?value>=requirement.amount:requirement.comparison=="Equal"&&value==requirement.amount;
                if(!valid){reason="Nano crystal upload requirements are not met.";return false;}
            }
            learnedNanos.Add(id);inventory.items.Remove(item);
            reason=AoSelfEffects.Get(id).name+" learned.";return true;
        }
        [NonSerialized] AoTimedEffects effects;
        public AoTimedEffects Effects=>effects??(effects=new AoTimedEffects());
        public int weaponId=>inventory.At(6)?.templateId??0;
        public AoWeaponDefinition Weapon=>inventory.At(6)?.Definition.weapon;
        public int Base(int id)
        {
            if(id>=16 && id<=21)return AoReference.Breed(breed).baseAbilities[id-16]+investments[id];
            if(id>=100 && id<=168)return 5+investments[id];
            if(id==54)return level;if(id==60)return profession;if(id==4)return breed;return 0;
        }
        public int Trickle(int id)
        {
            var s=AoReference.Skill(id);if(s==null || s.weights==null || s.weights.Length!=6)return 0;
            int weighted=0;for(int a=0;a<6;a++)weighted+=Value(16+a)*s.weights[a];return weighted/400;
        }
        public int Value(int id)=>Base(id)+Trickle(id)+(id>=90&&id<=97?inventory.ArmorModifier(id,Value):inventory.Modifier(id))+Effects.Modifier(id);
        public int Cost(int id)=>AoTrainingRules.Cost(AoReference.Rules,breed,profession,investments,id);
        public int Title=>level>=205?7:level>=190?6:level>=150?5:level>=100?4:level>=50?3:level>=15?2:1;
        public int Cap(int id)
        {
            var skill=AoReference.Skill(id);
            if(skill==null||skill.historical)return Base(id);
            return AoTrainingRules.Cap(AoReference.Rules,breed,profession,level,id);
        }
        public bool TrainStat(int id,out string reason)
        {
            var state=new AoTrainingState{level=level,ip=ip,breed=breed,profession=profession,investments=investments};
            bool changed=AoTrainingRules.Train(AoReference.Rules,state,id,out reason);
            if(changed)ip=state.ip;
            return changed;
        }
        public int evades=>Value(154);
        public int AttackRating()=>Weapon==null?0:System.Linq.Enumerable.Sum(Weapon.attackSkills,s=>Value(s.statId)*s.amount)/100;
        public int UsedNcu=>Effects.UsedNcu;
        public int MaxNcu=>8+inventory.Modifier(181);
        // Soldier baseline from CellAO's HP/NP tables; other professions require further conformance work.
        public int MaxHealth=>new[]{10,15,10,25}[breed-1]+level*(new[]{6,7,8,9,10,11,12}[Title-1]+new[]{0,-1,-1,0}[breed-1])+Value(152)*new[]{3,3,2,4}[breed-1]+inventory.Modifier(1)+Effects.Modifier(1);
        public int MaxNano=>new[]{10,10,15,8}[breed-1]+level*((Title==7?5:4)+new[]{0,-1,1,-2}[breed-1])+Value(132)*new[]{3,3,4,2}[breed-1]+inventory.Modifier(221)+Effects.Modifier(221);
        public int Reward(int difficulty)
        {
            // Training mission amounts are still fixtures; XP thresholds use the reference table.
            int awardedXp=100+difficulty*50;
            credits+=150+difficulty*100;xp+=awardedXp;
            while(level<200 && xp>=AoReference.NextXp(level))
            {
                xp-=AoReference.NextXp(level);level++;
                ip+=level<15?4000:level<50?10000:level<100?20000:level<150?40000:level<190?80000:150000;
            }
            return awardedXp;
        }
        public bool Valid()
        {
            if(hotbar==null||!hotbar.Valid())return false;
            if(float.IsNaN(aggression)||float.IsInfinity(aggression)||aggression<0||aggression>1)return false;
            if(learnedNanos==null || learnedNanos.Any(id=>id<=0) || learnedNanos.Distinct().Count()!=learnedNanos.Count)return false;
            if(schema!=3 || level<1 || level>220 || ip<0 || xp<0 || credits<0 || investments==null || investments.Length!=169 || inventory==null || !inventory.Valid() || AoReference.Breed(breed)==null || AoReference.Profession(profession)=="Unknown")return false;
            for(int i=0;i<investments.Length;i++)if(investments[i]<0 || investments[i]>3000)return false;
            return true;
        }
        public static CharacterBuild ParseSave(string json)
        {
            var header=JsonUtility.FromJson<SaveHeader>(json);
            if(header==null || (header.schema!=2&&header.schema!=3))return null;
            if(header.schema==2 && header.weaponId!=121569)return null;
            var result=JsonUtility.FromJson<CharacterBuild>(json);
            if(result.hotbar==null)result.hotbar=new AoHotbar();
            if(result.hotbar.items==null)result.hotbar.items=new string[100];
            if(result.learnedNanos==null)result.learnedNanos=new List<int>();
            if(header.schema==2){result.schema=3;result.inventory=new AoInventory();}
            return result.Valid()?result:null;
        }
        [Serializable] sealed class SaveHeader { public int schema,weaponId; }
    }
}
