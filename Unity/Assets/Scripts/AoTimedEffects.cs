using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reborn
{
    [Serializable] public sealed class AoSelfEffectDefinition
    {
        public int id,ncu;public float duration;public string name,sourceUrl;
        public int nanoCost,castMs,rechargeMs,requiredProfession,nanoLine,stackingOrder;
        public int[] additionalNanoLines;
        public bool SharesLine(AoSelfEffectDefinition other)
        {
            var mine=new[]{nanoLine}.Concat(additionalNanoLines??Array.Empty<int>()).Where(n=>n>0);
            var theirs=new[]{other.nanoLine}.Concat(other.additionalNanoLines??Array.Empty<int>()).Where(n=>n>0);
            return mine.Intersect(theirs).Any();
        }
        public AoRequirement[] castRequirements;
        public string clientEvidenceVersion,clientPayloadSha256;
        public AoStatModifier[] modifiers;public string[] unimplemented;
        public bool MeetsCastRequirements(CharacterBuild build)
        {
            // Current playable professions have no disguise/VisualProfession mechanic yet.
            return castRequirements!=null && (requiredProfession==0 || build.profession==requiredProfession)
                && castRequirements.All(r=>build.Value(r.statId)>=r.amount);
        }
    }
    [Serializable] public sealed class AoSelfEffectCatalog
    {
        public int schema;public string sourceVersion,scope;public AoSelfEffectDefinition[] effects;
    }
    public static class AoSelfEffects
    {
        static AoSelfEffectCatalog data;
        public static AoSelfEffectCatalog Catalog=>data??(data=JsonUtility.FromJson<AoSelfEffectCatalog>(Resources.Load<TextAsset>("AO/self-effects").text));
        public static AoSelfEffectDefinition Get(int id)=>Array.Find(Catalog.effects,e=>e.id==id);
    }
    // Runtime state, intentionally excluded from permanent character saves.
    // Shared primary/additional lines replace as whole programs. Original-client
    // cross-line replacement conformance and hostile effects remain incomplete.
    public sealed class AoTimedEffects
    {
        sealed class Active { public AoSelfEffectDefinition definition;public float remaining; }
        readonly List<Active> active=new List<Active>();
        public int UsedNcu=>active.Sum(e=>e.definition.ncu);
        public int Modifier(int statId)=>active.Sum(e=>e.definition.modifiers.Where(m=>m.statId==statId).Sum(m=>m.amount));
        public float Remaining(int id)=>active.Find(e=>e.definition.id==id)?.remaining??0;
        public bool CanApply(int id,int capacity)
        {
            var def=AoSelfEffects.Get(id);if(def==null)return false;
            var previous=active.Where(e=>e.definition.id==id || def.SharesLine(e.definition)).ToArray();
            if(previous.Any(e=>e.definition.stackingOrder>def.stackingOrder))return false;
            return UsedNcu-previous.Sum(e=>e.definition.ncu)+def.ncu<=capacity;
        }
        public bool Apply(int id,int capacity)
        {
            if(!CanApply(id,capacity))return false;
            var def=AoSelfEffects.Get(id);
            active.RemoveAll(e=>e.definition.id==id || def.SharesLine(e.definition));
            active.Add(new Active{definition=def,remaining=def.duration});
            return true;
        }
        public bool Cancel(int id)=>active.RemoveAll(e=>e.definition.id==id)>0;
        public void Tick(float seconds)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
            foreach(var effect in active)effect.remaining=Mathf.Max(0,effect.remaining-seconds);
            active.RemoveAll(e=>e.remaining<=0);
        }
    }
}
