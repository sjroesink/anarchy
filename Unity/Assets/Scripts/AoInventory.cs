using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Reborn
{
    [Serializable] public sealed class AoItemCondition { public int statId,amount;public string comparison; }
    [Serializable] public sealed class AoStatModifier { public int statId,amount; }
    [Serializable] public sealed class AoAppearanceAction { public int functionId,textureId,layer;public bool implemented; }
    [Serializable] public sealed class AoWeaponDefinition
    {
        public int minDamage,maxDamage,criticalBonus,damageTypeStatId,attackMs,rechargeMs,initiativeStatId;
        public float range;public string ammoType;public AoStatModifier[] attackSkills,defenceSkills;public string[] specials;
    }
    [Serializable] public sealed class AoExecutableItem
    {
        public int id,ql,equipDelayMs,canFlags,uploadNanoId;public string name,sourceUrl,additionalSourceUrl;public int[] slots;
        public bool equipDelayKnown,noDrop,armorOe;public AoItemCondition[] requirements;public AoStatModifier[] modifiers;
        public string[] unsupportedActions;public AoWeaponDefinition weapon;
        public AoAppearanceAction[] appearanceActions;
    }
    [Serializable] public sealed class AoExecutableCatalog { public int schema;public string sourceVersion,scope;public AoExecutableItem[] items; }
    public static class AoItems
    {
        static AoExecutableCatalog data;
        public static AoExecutableCatalog Catalog=>data??(data=JsonUtility.FromJson<AoExecutableCatalog>(Resources.Load<TextAsset>("AO/executable-items").text));
        public static AoExecutableItem Get(int id,int ql)=>Array.Find(Catalog.items,d=>d.id==id&&d.ql==ql);
        public static string SlotName(int slot)=>slot==0?"Inventory":slot==6?"Right hand":slot==7?"Belt":slot==21?"Body":slot>=9&&slot<=14?"NCU "+(slot-8):"Slot "+slot;
    }
    [Serializable] public sealed class AoItemInstance
    {
        public string instanceId;public int templateId,ql,slot;
        public AoExecutableItem Definition=>AoItems.Get(templateId,ql);
        public AoItemInstance(int id,int quality,int where=0){instanceId=Guid.NewGuid().ToString("N");templateId=id;ql=quality;slot=where;}
    }
    [Serializable] public sealed class AoInventory
    {
        public List<AoItemInstance> items=new List<AoItemInstance>{new AoItemInstance(121569,1,6)};
        public AoItemInstance At(int slot)=>slot==0?null:items.Find(i=>i.slot==slot);
        public AoItemInstance Find(string id)=>items.Find(i=>i.instanceId==id);
        public int Modifier(int statId,string exclude=null)=>items.Where(i=>i.slot!=0&&i.instanceId!=exclude).Sum(i=>i.Definition.modifiers.Where(m=>m.statId==statId).Sum(m=>m.amount));
        public int ArmorModifier(int statId,Func<int,int> stat)
            =>items.Where(i=>i.slot!=0).Sum(i=>i.Definition.modifiers.Where(m=>m.statId==statId).Sum(m=>m.amount)*AoOverEquipping.ArmorEfficiency(i.Definition,stat)/100);
        public bool CanEquip(string instanceId,int slot,Func<int,int> stat,out string reason)
        {
            var item=Find(instanceId);var def=item?.Definition;
            if(def==null){reason="No executable definition for this owned AOID / QL.";return false;}
            if(def.unsupportedActions.Length!=0){reason="Item contains unsupported actions.";return false;}
            if(slot==0)
            {
                if(item.slot==7 && items.Any(i=>i.slot>=9&&i.slot<=14)){reason="Remove deck items before removing the belt.";return false;}
                reason="Ready";return true;
            }
            if(Array.IndexOf(def.slots,slot)<0){reason="Item cannot be worn in this slot.";return false;}
            foreach(var condition in def.requirements)
            {
                int value=stat(condition.statId);bool ok;
                switch(condition.comparison){case "AtLeast":ok=value>=condition.amount;break;case "AtMost":ok=value<=condition.amount;break;case "Equal":ok=value==condition.amount;break;case "NotEqual":ok=value!=condition.amount;break;default:reason="Unsupported requirement operator.";return false;}
                if(!ok){reason=$"Requires {AoReference.Skill(condition.statId)?.name??condition.statId.ToString()} {condition.comparison} {condition.amount}.";return false;}
            }
            if(slot>=9&&slot<=14 && slot-8>stat(45)){reason="No corresponding belt deck slot is available.";return false;}
            if(slot==7)
            {
                if(At(7)!=item && items.Any(i=>i.slot>=9&&i.slot<=14)){reason="Remove deck items before replacing the belt.";return false;}
                int slots=def.modifiers.Where(m=>m.statId==45).Sum(m=>m.amount);
                if(items.Any(i=>i.slot>=9&&i.slot<=14&&i.slot-8>slots)){reason="The new belt cannot hold the installed deck items.";return false;}
            }
            reason="Ready";return true;
        }
        // Atomic state commit. The game must perform the sourced equip delay before calling this.
        public bool Equip(string instanceId,int slot,Func<int,int> stat,out string reason)
        {
            if(!CanEquip(instanceId,slot,stat,out reason))return false;
            var item=Find(instanceId);var replaced=At(slot);
            if(replaced!=null&&replaced!=item)replaced.slot=0;
            item.slot=slot;reason=slot==0?"Item moved to inventory.":"Item equipped.";return true;
        }
        public bool Valid()
        {
            if(items==null||items.Any(i=>i==null||string.IsNullOrWhiteSpace(i.instanceId)||i.Definition==null))return false;
            if(items.Select(i=>i.instanceId).Distinct().Count()!=items.Count)return false;
            var worn=items.Where(i=>i.slot!=0).ToArray();
            if(worn.Select(i=>i.slot).Distinct().Count()!=worn.Length)return false;
            if(worn.Any(i=>!i.Definition.slots.Contains(i.slot)))return false;
            int decks=Modifier(45);return !worn.Any(i=>i.slot>=9&&i.slot<=14&&i.slot-8>decks);
        }
    }
}
