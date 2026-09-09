using UnityEngine;

namespace Reborn
{
    // AO Universe, Weapon Initiatives and the AggDef Slider (2025-05-20).
    // Slider uses 0=full defence, 1=full aggression. Special caps are explicit.
    public static class AoInitiative
    {
        public static float Effective(int initiative)=>Mathf.Min(initiative,1200)+Mathf.Max(0,initiative-1200)/3f;
        public static float NanoCast(float baseSeconds,int initiative,float aggression,float minimumSeconds=0)
            =>Mathf.Max(minimumSeconds,baseSeconds-Effective(initiative)/200f+1-2*Mathf.Clamp01(aggression));
        public static float WeaponAttack(float baseSeconds,int initiative,float aggression,float minimumSeconds=1)
            =>Mathf.Max(minimumSeconds,baseSeconds-Effective(initiative)/600f+1.75f-2*Mathf.Clamp01(aggression));
        public static float WeaponRecharge(float baseSeconds,int initiative,float aggression,float minimumSeconds=1)
            =>Mathf.Max(minimumSeconds,baseSeconds-Effective(initiative)/300f+1.75f-2*Mathf.Clamp01(aggression));
    }
}
