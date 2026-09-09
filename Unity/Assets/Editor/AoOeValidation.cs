using System;
using System.IO;
using Reborn;

public static class AoOeValidation
{
    public static void Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{count++;if(!ok)throw new Exception("OE: "+msg);};
        int[] values={100,80,79,60,59,40,39,20,19,0};int[] expected={100,100,75,75,50,50,25,25,0,0};
        for(int i=0;i<values.Length;i++)check(AoOverEquipping.Efficiency(values[i],100)==expected[i],"threshold "+values[i]);
        var b=new CharacterBuild();b.investments[17]=2;b.investments[20]=2;
        var armor=new AoItemInstance(85697,1);b.inventory.items.Add(armor);b.inventory.Equip(armor.instanceId,21,b.Value,out _);
        check(b.Value(90)==5,"full protection at equip requirements");
        b.investments[17]=0;
        check(AoOverEquipping.ArmorEfficiency(armor.Definition,b.Value)==75&&b.Value(90)==3,"single failing ability reduces armor AC");
        check(b.Value(17)==6&&b.Value(20)==8,"OE does not lower the abilities used for its own evaluation");
        b.investments[17]=2;check(b.Value(90)==5,"restoring requirement restores armor without re-equip");
        b=new CharacterBuild();b.investments[161]=744;
        var belt=new AoItemInstance(36787,200);var memory=new AoItemInstance(95520,200);
        b.inventory.items.Add(belt);b.inventory.items.Add(memory);b.inventory.Equip(belt.instanceId,7,b.Value,out _);b.inventory.Equip(memory.instanceId,9,b.Value,out _);
        b.investments[161]=0;
        check(b.MaxNcu==72&&b.Value(45)==6,"NCU and belt capacity persist below equipment requirement");
        File.WriteAllText("../Artifacts/oe-validation.txt",$"PASS: {count} armor OE and NCU exception checks.\nThreshold inclusivity and integer rounding need original-client fixtures; full weapon/pet/special-stat OE remains incomplete.\n");
    }
}
