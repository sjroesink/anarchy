using System;
using System.IO;
using Reborn;

public static class AoDamageValidation
{
    public static void Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception("Damage validation: "+message);};
        var solar=AoItems.Get(121569,1);
        check((solar.canFlags&(1<<11))!=0&&Array.IndexOf(solar.weapon.specials,"Burst")>=0,"raw Can 3077 preserves Burst flag");
        var w=solar.weapon;
        check(AoWeaponDamage.TryNormal(w,0,24,0,0,out var d)&&d==24,"unmodified maximum");
        check(AoWeaponDamage.TryNormal(w,400,24,100,0,out d)&&d==38,"100 AC subtracts 10 after AR scaling");
        check(AoWeaponDamage.TryNormal(w,400,24,10000,0,out d)&&d==6,"armor cannot pass scaled weapon minimum");
        check(AoWeaponDamage.TryNormal(w,400,24,10000,20,out d)&&d==26,"add damage is applied after AC without AR scaling");
        check(AoWeaponDamage.TryNormal(w,1000,3,10000,0,out d)&&d==10.5f,"1000 AR boundary");
        check(!AoWeaponDamage.TryNormal(w,1001,24,0,0,out d)&&d==0,"unknown profession multiplier above 1000 not extrapolated");
        check(!AoWeaponDamage.TryNormal(w,0,25,0,0,out d),"out-of-range random roll rejected");
        check(!AoWeaponDamage.TryNormal(w,-1,24,0,0,out d),"negative AR unsupported");
        check(AoWeaponDamage.TryNormal(w,0,3,0,-10,out d)&&d==0,"negative add damage does not heal target");
        var published=new AoWeaponDefinition{minDamage=175,maxDamage=425,damageTypeStatId=92};
        check(AoWeaponDamage.TryNormal(published,900,175,0,20,out d)&&d==588.75f,"published AR900 minimum before integer rounding");
        check(AoWeaponDamage.TryNormal(published,900,425,0,20,out d)&&d==1401.25f,"published AR900 maximum before integer rounding");
        int[] expected={278,279,280,281,282,311,317,316};
        for(int i=0;i<expected.Length;i++)check(AoWeaponDamage.AddDamageStat(90+i)==expected[i],"AO damage stat mapping "+(90+i));
        check(AoWeaponDamage.AddDamageStat(98)==-1,"StateAction is not an armor class");
        File.WriteAllText("../Artifacts/damage-validation.txt",$"PASS: {checks} normal PvM damage and source-flag checks.\nAR 0..1000 only. Hit chance, crits, MBS, OE, PvP and integer-rounding conformance remain unverified.\n");
    }
}
