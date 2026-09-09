using System;
using System.Collections.Generic;
using UnityEngine;
namespace Reborn
{
    public static class AoReference
    {
        static AoRules rules;static AoItemIndex items;static AoNanoIndex nanos;static Dictionary<int,AoSkillDefinition> skills;
        public static AoRules Rules { get { Ensure();return rules; } }
        public static AoItemIndex Items => items??(items=Read<AoItemIndex>("items"));
        public static AoNanoIndex Nanos => nanos??(nanos=Read<AoNanoIndex>("nanos"));
        static T Read<T>(string name) { var asset=Resources.Load<TextAsset>("AO/"+name);if(!asset)throw new InvalidOperationException("Missing AO reference: "+name);return JsonUtility.FromJson<T>(asset.text); }
        static void Ensure() { if(rules!=null)return;rules=Read<AoRules>("rules");skills=new Dictionary<int,AoSkillDefinition>();foreach(var s in rules.skills)skills.Add(s.id,s); }
        public static AoSkillDefinition Skill(int id) { Ensure();return skills.TryGetValue(id,out var s)?s:null; }
        public static AoBreed Breed(int id) => Array.Find(Rules.breeds,b=>b.id==id);
        public static string Profession(int id) => Array.Find(Rules.professions,p=>p.id==id)?.name??"Unknown";
        public static int NextXp(int level) => Array.Find(Rules.levels,l=>l.level==level)?.xp??0;
    }
}
