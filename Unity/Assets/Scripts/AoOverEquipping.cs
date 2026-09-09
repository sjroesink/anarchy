using System;

namespace Reborn
{
    public static class AoOverEquipping
    {
        // Exact integer comparisons; threshold inclusivity still needs client fixtures.
        public static int Efficiency(int current,int required)
        {
            if(required<=0)return 100;
            long value=(long)current*100;
            return value>=80L*required?100:value>=60L*required?75:value>=40L*required?50:value>=20L*required?25:0;
        }
        public static int ArmorEfficiency(AoExecutableItem definition,Func<int,int> stat)
        {
            if(!definition.armorOe)return 100;
            int result=100;
            foreach(var requirement in definition.requirements)
            {
                if(requirement.comparison!="AtLeast"||requirement.statId<16||requirement.statId>21)
                    throw new InvalidOperationException("Unreviewed armor OE requirement");
                result=Math.Min(result,Efficiency(stat(requirement.statId),requirement.amount));
            }
            return result;
        }
    }
}
