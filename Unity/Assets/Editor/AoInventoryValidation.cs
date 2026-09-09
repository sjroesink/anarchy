using System;
using System.IO;
using UnityEngine;
using Reborn;
public static class AoInventoryValidation
{
    public static void Run()
    {
        int count=0;Action<bool,string> check=(ok,name)=>{count++;if(!ok)throw new Exception("Inventory check failed: "+name);};
        var b=new CharacterBuild();var solar=b.inventory.At(6);
        check(b.inventory.Valid()&&solar.templateId==121569&&solar.ql==1,"owned starter in AO right-hand slot 6");
        check(b.Weapon.minDamage==3&&b.Weapon.maxDamage==24&&b.Weapon.attackMs==1000&&b.Weapon.rechargeMs==1500&&b.Weapon.range==20,"executable weapon fixture");
        check(AoItems.Get(36779,2)==null,"no guessed QL interpolation");
        check(!b.inventory.Equip("foreign-instance",6,b.Value,out _),"cannot equip an unowned catalogue item");
        check(!b.inventory.Equip(solar.instanceId,7,b.Value,out _)&&solar.slot==6,"wrong slot rejected without mutation");
        check(b.inventory.Equip(solar.instanceId,0,b.Value,out _)&&b.Weapon==null&&b.weaponId==0,"unequipping removes weapon from combat");
        check(b.inventory.Equip(solar.instanceId,6,b.Value,out _)&&b.weaponId==121569,"reequip owned instance");
        var belt=new AoItemInstance(36783,1);var memory=new AoItemInstance(36779,1);b.inventory.items.Add(belt);b.inventory.items.Add(memory);
        check(belt.Definition.equipDelayKnown&&belt.Definition.equipDelayMs==10000,"raw belt EquipDelay 1000 centiseconds");
        check(memory.Definition.equipDelayKnown&&memory.Definition.equipDelayMs==1000,"raw memory EquipDelay 100 centiseconds");
        check(!b.inventory.Equip(memory.instanceId,9,b.Value,out _),"NCU requires belt deck");
        check(!b.inventory.Equip(belt.instanceId,7,id=>5,out _)&&belt.slot==0,"computer literacy requirement enforced");
        check(b.inventory.Equip(belt.instanceId,7,b.Value,out _)&&b.Value(45)==1,"belt grants one deck");
        check(b.inventory.Equip(memory.instanceId,9,b.Value,out _)&&b.MaxNcu==10,"NCU module adds two to baseline eight");
        check(!b.inventory.Equip(memory.instanceId,10,b.Value,out _)&&memory.slot==9,"locked deck slot rejected atomically");
        check(!b.inventory.Equip(belt.instanceId,0,b.Value,out _)&&b.Value(45)==1,"belt cannot be removed with module installed");
        check(b.inventory.Valid(),"consistent equipped state");
        string json=JsonUtility.ToJson(b);var restored=CharacterBuild.ParseSave(json);
        check(restored!=null&&restored.MaxNcu==10&&restored.inventory.Find(memory.instanceId).slot==9,"instance identities and modifiers survive save");
        check(b.inventory.Equip(memory.instanceId,0,b.Value,out _)&&b.MaxNcu==8,"unequip removes modifier exactly once");
        check(b.inventory.Equip(memory.instanceId,0,b.Value,out _)&&b.MaxNcu==8,"repeat unequip cannot subtract twice");
        check(b.inventory.Equip(belt.instanceId,0,b.Value,out _)&&b.Value(45)==0,"empty belt removal");
        var duplicate=new AoItemInstance(121569,1){instanceId=solar.instanceId};b.inventory.items.Add(duplicate);
        check(!b.Valid(),"duplicate instance identifiers rejected on load");b.inventory.items.Remove(duplicate);
        var legacy="{\"schema\":2,\"weaponId\":121569,\"level\":1,\"breed\":1,\"profession\":1,\"ip\":1500,\"investments\":"+JsonUtility.ToJson(new ArrayWrapper{values=new int[169]}).Split(':')[1].TrimEnd('}')+"}";
        var migrated=CharacterBuild.ParseSave(legacy);check(migrated!=null&&migrated.schema==3&&migrated.inventory.items.Count==1,"schema2 migration preserves starter without granting extras");
        check(CharacterBuild.ParseSave(legacy.Replace("121569","999999"))==null,"invalid legacy weapon rejected");
        b=new CharacterBuild();var sixBelt=new AoItemInstance(36787,200);b.inventory.items.Add(sixBelt);
        check(!b.inventory.Equip(sixBelt.instanceId,7,id=>400,out _),"QL200 belt rejects CL400");
        check(b.inventory.Equip(sixBelt.instanceId,7,id=>401,out _)&&b.Value(45)==6,"QL200 belt accepts CL401 and grants six slots");
        var largeMemory=new AoItemInstance(95520,200);b.inventory.items.Add(largeMemory);
        check(!b.inventory.Equip(largeMemory.instanceId,14,id=>id==161?749:6,out _),"QL200 memory rejects CL749");
        b.investments[161]=744;
        check(b.inventory.Equip(largeMemory.instanceId,14,b.Value,out _)&&b.MaxNcu==72,"QL200 memory equips into deck six at CL750");
        for(int slot=9;slot<14;slot++){var module=new AoItemInstance(95520,200);b.inventory.items.Add(module);check(b.inventory.Equip(module.instanceId,slot,b.Value,out _),"independent NCU slot "+slot);}
        check(b.MaxNcu==392&&b.inventory.Valid(),"six 64 NCU modules contribute once each");
        restored=CharacterBuild.ParseSave(JsonUtility.ToJson(b));check(restored!=null&&restored.MaxNcu==392&&restored.inventory.At(14).instanceId==largeMemory.instanceId,"six-slot state persists");
        check(AoItems.Get(95520,199)==null&&AoItems.Get(36778,19)==null,"source endpoints do not authorize unsupported QLs");
        var medium=AoItems.Get(36778,20);check(medium.requirements[0].amount==35&&medium.modifiers[0].amount==4,"QL20 memory source values");
        b=new CharacterBuild();var leather=new AoItemInstance(85697,1);b.inventory.items.Add(leather);
        check(!b.inventory.Equip(leather.instanceId,21,b.Value,out _),"leather rejects insufficient abilities");
        b.investments[17]=2;check(!b.inventory.Equip(leather.instanceId,21,b.Value,out _),"both leather requirements must pass");
        b.investments[20]=2;check(b.inventory.Equip(leather.instanceId,21,b.Value,out _),"leather equips in clothing body slot");
        check(b.Value(90)==5&&b.Value(91)==5&&b.Value(94)==5&&b.Value(95)==5,"primary leather armor classes");
        check(b.Value(92)==1&&b.Value(93)==1&&b.Value(96)==1&&b.Value(97)==1,"weak leather armor classes");
        check(leather.Definition.appearanceActions[0].textureId==8732&&!leather.Definition.appearanceActions[0].implemented,"unimplemented source appearance action preserved");
        check(!b.inventory.Equip(leather.instanceId,6,b.Value,out _)&&leather.slot==21,"armor cannot replace right-hand weapon");
        check(b.inventory.Equip(leather.instanceId,0,b.Value,out _)&&b.Value(90)==0,"removal removes armor modifier");
        File.WriteAllText("../Artifacts/inventory-validation.txt",$"PASS: {count} inventory / executable-item checks.\nExact recorded QL fixtures only; full AO inventory and item-effect parity are not established.\n");
        Debug.Log("INVENTORY_VALIDATION_OK: "+count);
    }
    [Serializable] sealed class ArrayWrapper { public int[] values; }
}
