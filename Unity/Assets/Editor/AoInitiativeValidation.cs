using System;
using System.IO;
using Reborn;

public static class AoInitiativeValidation
{
    public static void Run()
    {
        int count=0;Action<float,float,string> check=(actual,expected,label)=>{count++;if(Math.Abs(actual-expected)>.0001f)throw new Exception("Initiative: "+label+" actual="+actual);};
        check(AoInitiative.NanoCast(5,672,.5f),1.64f,"published nano example");
        check(AoInitiative.NanoCast(10,1200,.5f),4,"1200 breakpoint");
        check(AoInitiative.NanoCast(10,1800,.5f),3,"post1200 nano effectiveness");
        check(AoInitiative.NanoCast(10,2400,.5f),2,"second upper tier interval");
        check(AoInitiative.NanoCast(5,0,0),6,"full defence adds one second");
        check(AoInitiative.NanoCast(5,0,1),4,"full aggression subtracts one second");
        check(AoInitiative.NanoCast(5,0,.25f),5.5f,"intermediate slider position");
        check(AoInitiative.NanoCast(2.1f,400,1),0,"instant casting clamps at zero");
        check(AoInitiative.NanoCast(10,5000,1,1.25f),1.25f,"explicit special nano cap");
        check(AoInitiative.WeaponAttack(5,0,.875f),5,"weapon neutral differs from nano neutral");
        check(AoInitiative.WeaponAttack(5,0,0),6.75f,"full defence weapon modifier");
        check(AoInitiative.WeaponAttack(5,0,1),4.75f,"full aggression weapon modifier");
        check(AoInitiative.WeaponAttack(5,1800,.875f),5-1400/600f,"weapon upper tier attack");
        check(AoInitiative.WeaponRecharge(7,1800,.875f),7-1400/300f,"weapon upper tier recharge");
        check(AoInitiative.WeaponAttack(1,1200,1),1,"ordinary weapon attack cap");
        check(AoInitiative.WeaponRecharge(1.5f,1200,1),1,"ordinary weapon recharge cap");
        var b=new CharacterBuild{aggression=.25f};var json=UnityEngine.JsonUtility.ToJson(b);
        check(CharacterBuild.ParseSave(json).aggression,.25f,"slider survives character save");
        check(CharacterBuild.ParseSave(json.Replace("\"aggression\":0.25,"," ")).aggression,.65f,"older saves receive previous default slider position");
        b.aggression=1.1f;check(b.Valid()?1:0,0,"out-of-range saved slider rejected");
        File.WriteAllText("../Artifacts/initiative-validation.txt",$"PASS: {count} initiative/slider formula fixtures.\nPublished-rule implementation; original-client conformance and all special caps remain incomplete.\n");
    }
}
