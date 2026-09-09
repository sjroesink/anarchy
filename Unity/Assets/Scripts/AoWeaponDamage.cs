using System;

namespace Reborn
{
    // Published PvM normal-hit formula, AR 0..1000. This is not hit chance,
    // critical, PvP, OE or profession-specific post-1000 AR conformance.
    public static class AoWeaponDamage
    {
        public static float AfterArmor(float scaledRoll,float scaledMinimum,int armor,int addDamage=0)
            =>Math.Max(0,Math.Max(scaledMinimum,scaledRoll-Math.Max(0,armor)/10f)+addDamage);
        public static int AddDamageStat(int armorStat)
        {
            switch(armorStat)
            {
                case 90:return 278;case 91:return 279;case 92:return 280;
                case 93:return 281;case 94:return 282;case 95:return 311;
                case 96:return 317;case 97:return 316;
                default:return -1;
            }
        }
        public static bool TryNormal(AoWeaponDefinition weapon,int attackRating,int baseRoll,int armor,int addDamage,out float damage)
        {
            damage=0;
            if(weapon==null || weapon.minDamage<0 || weapon.maxDamage<weapon.minDamage || attackRating<0 || attackRating>1000 || AddDamageStat(weapon.damageTypeStatId)<0)return false;
            if(baseRoll<weapon.minDamage || baseRoll>weapon.maxDamage)return false;
            float scale=1+attackRating/400f;
            damage=AfterArmor(baseRoll*scale,weapon.minDamage*scale,armor,addDamage);
            return true;
        }
    }
}
