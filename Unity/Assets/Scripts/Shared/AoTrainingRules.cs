#nullable disable
using System;
namespace Reborn
{
    [Serializable] public sealed class AoTrainingState
    {
        public int level=1, ip=1500, breed=1, profession=1;
        public int[] investments=new int[169];
        public long revision;
    }
    // Existing reference rules, not full AO conformance: SL and ability-dependent caps remain open.
    public static class AoTrainingRules
    {
        static AoSkillDefinition Skill(AoRules r,int id)=>Array.Find(r.skills,s=>s.id==id);
        static AoBreed Breed(AoRules r,int id)=>Array.Find(r.breeds,b=>b.id==id);
        public static int Base(AoRules r,int breed,int[] investments,int id)
        {
            if(id>=16&&id<=21)return Breed(r,breed).baseAbilities[id-16]+investments[id];
            return id>=100&&id<=168?5+investments[id]:0;
        }
        public static int Cost(AoRules r,int breed,int profession,int[] investments,int id)
        {
            var s=Skill(r,id);if(s==null||s.historical||!((id>=16&&id<=21)||(id>=100&&id<=168)))return -1;
            int value=Base(r,breed,investments,id);
            return id<100?value*Breed(r,breed).costs[id-16]:value*s.costTenths[profession]/10;
        }
        public static int Cap(AoRules r,int breed,int profession,int level,int id)
        {
            if(id>=16&&id<=21)return Breed(r,breed).baseAbilities[id-16]+level*3;
            var s=Skill(r,id);if(s==null||s.historical)return 5;
            int c=s.costTenths[profession],band=c<15?0:c<25?1:c<35?2:c<45?3:4;
            int title=level>=205?7:level>=190?6:level>=150?5:level>=100?4:level>=50?3:level>=15?2:1;
            int[,] caps={{60,200,360,490,550,600},{55,180,330,450,500,540},{50,160,300,410,450,480},{45,140,270,370,400,420},{40,120,250,330,350,360}};
            return Math.Min(5+level*(band==0?5:band<3?4:3),caps[band,Math.Min(title,6)-1]);
        }
        public static bool Train(AoRules r,AoTrainingState state,int id,out string reason)
        {
            int cost=Cost(r,state.breed,state.profession,state.investments,id);
            if(cost<0){reason="Historical or non-trainable stat.";return false;}
            if(state.level>200){reason="Shadowlevel training awaits verified caps.";return false;}
            if(Base(r,state.breed,state.investments,id)>=Cap(r,state.breed,state.profession,state.level,id)){reason="Level or title cap reached.";return false;}
            if(state.ip<cost){reason="Not enough improvement points.";return false;}
            state.ip-=cost;state.investments[id]++;reason=Skill(r,id).name+" +1";return true;
        }
    }
}
