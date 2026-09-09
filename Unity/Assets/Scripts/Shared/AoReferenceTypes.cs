#nullable disable
using System;
namespace Reborn
{
    [Serializable] public sealed class AoStatIdentity { public int id; public string key; }
    [Serializable] public sealed class AoSkillDefinition { public int id; public string key,name,category,dependencyEvidence,costEvidence; public int[] weights,costTenths; public bool historical; }
    [Serializable] public sealed class AoBreed { public int id;public string name; public int[] baseAbilities,costs; }
    [Serializable] public sealed class AoProfession { public int id; public string name; }
    [Serializable] public sealed class AoLevel { public int level,xp; }
    [Serializable] public sealed class AoRules { public int schema;public string reference;public AoStatIdentity[] stats;public AoSkillDefinition[] skills;public AoBreed[] breeds;public AoProfession[] professions;public AoLevel[] levels; }
    [Serializable] public sealed class AoItemRecord { public int lowId,highId,lowQl,highQl,iconId,slots,flags;public string name;public bool inGame,froob; }
    [Serializable] public sealed class AoItemIndex { public int schema;public string sourceVersion;public bool executionDataComplete;public AoItemRecord[] items; }
    [Serializable] public sealed class AoRequirement { public int statId,amount; }
    [Serializable] public sealed class AoNanoRecord { public int id,crystalId,ql,nanoCost,lineId; public string name,professions,line;public AoRequirement[] requirements;public bool executionDataComplete; }
    [Serializable] public sealed class AoNanoIndex { public AoNanoRecord[] nanos; }
}
