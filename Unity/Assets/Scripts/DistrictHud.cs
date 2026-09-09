using UnityEngine;

namespace Reborn
{
    public sealed class DistrictHud : MonoBehaviour
    {
        DistrictGame g;
        Vector2 effectScroll;
        readonly HotbarView hotbarView=new HotbarView();
        public bool ShortcutInteractionActive=>hotbarView.Busy;
        public bool CancelShortcutInteraction()=>hotbarView.Cancel();
        readonly Color cyan=new Color(.32f,.82f,.93f), muted=new Color(.68f,.76f,.8f), white=new Color(.88f,.94f,.95f), amber=new Color(1,.57f,.27f);
        GUIStyle text, small, title, button;
        void Init()
        {
            text=new GUIStyle(GUI.skin.label){fontSize=17};text.normal.textColor=white;
            small=new GUIStyle(text){fontSize=13};
            title=new GUIStyle(text){fontSize=30,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){fontSize=15,alignment=TextAnchor.MiddleCenter};button.normal.textColor=white;
            button.normal.background=null;button.hover.background=null;button.active.background=null;
        }
        void Fill(Rect r,Color c) { GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
        void Label(float x,float y,string s,Color? c=null,GUIStyle style=null,float w=600) { var st=style??text;st.normal.textColor=c??white;GUI.Label(new Rect(x,y,w,40),s,st); }
        void Panel(float x,float y,float w,float h) { Fill(new Rect(x,y,w,h),new Color(.018f,.034f,.045f,.94f));Fill(new Rect(x,y,w,1),new Color(.25f,.46f,.53f,.8f)); }
        bool Button(float x,float y,float w,string s) { var r=new Rect(x,y,w,36);Fill(r,r.Contains(Event.current.mousePosition)?new Color(.13f,.29f,.35f):new Color(.07f,.13f,.17f));Fill(new Rect(x,y,w,1),new Color(.24f,.46f,.53f));return GUI.Button(r,s,button); }
        void Bar(float x,float y,float w,float pct,Color c) { Fill(new Rect(x,y,w,5),new Color(.1f,.18f,.22f));Fill(new Rect(x,y,w*Mathf.Clamp01(pct),5),c); }
        static string EffectDuration(float remaining)
        {
            int seconds=Mathf.CeilToInt(Mathf.Max(0,remaining));
            return seconds>=3600?$"{seconds/3600}:{seconds/60%60:00}:{seconds%60:00}":$"{seconds/60}:{seconds%60:00}";
        }
        public bool BlocksWorldPointer(Vector2 screenPoint)
        {
            var game=DistrictGame.Instance;
            if(!game)return true;
            if(game.Paused||game.characterOpen||game.missionOpen)return true;
            if(game.HideHud)return false;
            if(hotbarView.Busy&&game.Build.hotbar.visible)return true;
            Vector2 point=new Vector2(screenPoint.x*1600/Screen.width,(Screen.height-screenPoint.y)*900/Screen.height);
            if(game.Build.hotbar.visible&&new Rect(453,763,694,93).Contains(point))return true;
            foreach(var rect in new[]{new Rect(24,24,360,166),new Rect(1260,24,316,94),new Rect(1260,139,316,202),new Rect(1260,355,316,82),new Rect(24,686,370,145)})
                if(rect.Contains(point))return true;
            if(game.target&&!game.target.dead&&new Rect(605,101,390,75).Contains(point))return true;
            if(game.inspectedTarget&&new Rect(1260,454,316,220).Contains(point))return true;
            if(game.Casting&&new Rect(453,697,694,54).Contains(point))return true;
            if(game.TerminalDistance<4&&!game.Casting&&new Rect(622,681,356,49).Contains(point))return true;
            if(game.Build.UsedNcu>0)
            {
                int count=0;foreach(var effect in AoSelfEffects.Catalog.effects)if(game.Build.Effects.Remaining(effect.id)>0)count++;
                if(new Rect(24,202,360,Mathf.Min(count,6)*39+38).Contains(point))return true;
            }
            return false;
        }
        void OnGUI()
        {
            g=DistrictGame.Instance;if(!g || g.HideHud) return;if(text==null) Init();
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));
            if(!g.characterOpen&&!g.missionOpen&&!g.Paused)g.DamageNumbers.Draw(g.view);
            Panel(24,24,360,125);Label(44,38,"A N A R C H Y  /  R E B O R N",cyan,small);
            Label(44,61,"SOLDIER",null,title);Label(245,72,"LVL  "+g.Build.level,muted,small);
            Bar(44,110,155,g.Health/g.Build.MaxHealth,new Color(.92f,.24f,.22f));Bar(217,110,145,g.Nano/g.Build.MaxNano,cyan);
            Label(44,119,$"{Mathf.CeilToInt(g.Health)} / {g.Build.MaxHealth}  HEALTH",muted,small);Label(217,119,$"{Mathf.CeilToInt(g.Nano)} / {g.Build.MaxNano}  NANO",muted,small);
            Panel(24,150,360,40);
            int nextXp=AoReference.NextXp(g.Build.level);
            if(g.Build.level<200&&nextXp>0)
            {
                Label(44,153,$"{g.Build.xp:N0} / {nextXp:N0} XP",white,small,230);
                Label(294,153,$"{100f*g.Build.xp/nextXp:0.#}%",muted,small,70);
                Bar(44,179,318,(float)g.Build.xp/nextXp,new Color(.96f,.76f,.28f));
            }
            else Label(44,156,$"LEVEL {g.Build.level}",muted,small,300);
            Label(625,29,"B O R E A L I S   /   D I S T R I C T   0 7",white,small);
            Label(680,51,"NORTHERN TRANSIT CONCOURSE",muted,small);
            Panel(1260,24,316,94);Label(1280,38,"RUBI-KA  /  LOCAL PROTOTYPE",cyan,small);
            Label(1280,64,"07:42   ·   NEUTRAL TERRITORY",null,text);Label(1280,91,"SUPPRESSION GAS   75%  ·  ART STUDY",muted,small);
            Panel(1260,139,316,202);Label(1280,156,"FIELD OPERATIONS",cyan,small);
            Label(1280,181,g.missionActive?"Signal interference":"A city of possibilities",null,text);
            Label(1280,216,g.missionActive?$"{g.kills:00} / {g.required:00}   Rogue drones disabled":"Find the cyan mission terminal.",white,small);
            Label(1280,244,g.missionComplete?"Return to terminal · claim reward":g.missionActive?"North concourse · TAB to target":"[E]  Connect and choose a contract",g.missionComplete?amber:muted,small);
            Bar(1280,279,272,g.missionActive?(float)g.kills/g.required:0,cyan);
            Label(1280,295,g.missionActive?$"CONTRACT #{g.seed}   /   THREAT {g.difficulty}":"REWARDS   Credits · XP · Improvement points",muted,small);
            if(g.target && !g.target.dead)
            {
                Panel(605,101,390,75);Label(625,113,"ROGUE SURVEY DRONE",amber,small);Label(878,113,$"{Vector3.Distance(g.player.position,g.target.transform.position):0} m",muted,small);
                Bar(625,147,350,g.target.health/g.target.maxHealth,amber);
                Vector3 anchor=g.target.transform.position+Vector3.up*.95f;
                Vector3 projected=g.view.WorldToScreenPoint(anchor);
                if(projected.z>0&&!Physics.Linecast(g.view.transform.position,anchor,1<<0,QueryTriggerInteraction.Ignore))
                {
                    float x=projected.x*1600/Screen.width-110,y=(Screen.height-projected.y)*900/Screen.height-36;
                    if(x>=0&&x<=1380&&y>=180&&y<640)
                    {
                        Panel(x,y,220,36);Label(x+10,y+2,"Rogue survey drone",amber,small,200);
                        Bar(x+10,y+27,200,g.target.health/g.target.maxHealth,amber);
                    }
                }
            }
            Panel(1260,355,316,82);
            Label(1280,365,"DEF",muted,small,60);Label(1505,365,"AGG",muted,small,65);
            Label(1380,365,$"{g.aggDef*100:0}%",cyan,small,75);
            g.aggDef=GUI.HorizontalSlider(new Rect(1280,406,272,20),g.aggDef,0,1);
            if(g.Build.UsedNcu>0)
            {
                int activeCount=0;foreach(var effect in AoSelfEffects.Catalog.effects)if(g.Build.Effects.Remaining(effect.id)>0)activeCount++;
                float height=Mathf.Min(activeCount,6)*39;
                Panel(24,202,360,height+38);
                Label(42,212,$"NCU  {g.Build.UsedNcu} / {g.Build.MaxNcu}",cyan,small);
                effectScroll=GUI.BeginScrollView(new Rect(24,234,356,height),effectScroll,new Rect(0,0,335,activeCount*39));
                int row=0;
                foreach(var effect in AoSelfEffects.Catalog.effects)
                {
                    float remaining=g.Build.Effects.Remaining(effect.id);if(remaining<=0)continue;
                    var effectLabel=new GUIStyle(small){fontSize=12,wordWrap=false,clipping=TextClipping.Clip};
                    effectLabel.normal.textColor=white;
                    GUI.Label(new Rect(18,row*39,245,19),effect.name,effectLabel);
                    effectLabel.normal.textColor=remaining<=10?amber:muted;
                    GUI.Label(new Rect(18,16+row*39,245,18),$"{EffectDuration(remaining)} remaining · {effect.ncu} NCU",effectLabel);
                    Bar(18,33+row*39,237,effect.duration>0?remaining/effect.duration:0,remaining<=10?amber:cyan);
                    if(Button(267,row*39,65,"Cancel"))g.CancelNano(effect.id);row++;
                }
                GUI.EndScrollView();
            }
            Panel(24,686,370,145);Label(42,698,"SYSTEM  /  COMBAT",cyan,small);
            for(int i=0;i<g.messages.Count;i++) Label(42,723+i*19,g.messages[i],i==g.messages.Count-1?white:muted,new GUIStyle(small){fontSize=11,wordWrap=false,clipping=TextClipping.Clip},345);
            if(g.Build.hotbar.visible&&!g.characterOpen&&!g.missionOpen&&!g.Paused)Panel(453,763,694,93);
            if(g.Casting)
            {
                Panel(453,697,694,54);Label(468,704,g.CastingName,cyan,small);
                Label(1043,704,$"{g.castRemaining:0.0}s",muted,small,90);
                Bar(468,737,664,g.CastProgress,amber);
            }
            hotbarView.Draw(g);
            hotbarView.DrawEditor(g);
            Label(453,868,$"W/S  Move  A/D  Turn  Z/C  Strafe  Backspace  {(g.Walking?"Walk":"Run")}  RMB  Orbit  TAB  Target",muted,small,820);
            Label(25,868,"F2  Photo mode    F5  Save    T  Target info",muted,small,420);
            Label(1290,859,$"{g.Build.credits:N0} CR    /    {g.Build.ip} IP",cyan,text);
            if(g.TerminalDistance<4 && !g.missionOpen && !g.characterOpen && !g.Casting) { Panel(622,681,356,49);Label(648,691,"RIGHT CLICK / E  ·  MISSIONS",cyan,text); }
            if(g.characterOpen) Character();
            if(g.missionOpen) Mission();
            if(g.inspectedTarget&&!g.characterOpen&&!g.missionOpen) TargetInformation();
            if(g.Paused)
            {
                Fill(new Rect(0,0,1600,900),new Color(0,0,0,.65f));Panel(570,280,460,300);Label(615,309,"UPLINK PAUSED",cyan,title);
                if(Button(615,376,370,"Resume"))g.Paused=false;
                if(Button(615,427,370,"Save character"))g.Save();
                if(Button(615,478,370,"Save and quit")){g.Save();Application.Quit();}
            }
        }
        readonly AoCharacterPanel aoPanel = new AoCharacterPanel();
        void TargetInformation()
        {
            var inspected=g.inspectedTarget;
            Panel(1260,454,316,220);
            Label(1278,464,"TARGET INFORMATION",cyan,small,250);
            Label(1278,494,"Rogue survey drone",white,text,278);
            Label(1278,522,"TRAINING CONSTRUCT",amber,small,278);
            var body=new GUIStyle(small){wordWrap=true};
            GUI.Label(new Rect(1278,551,278,50),"A survey unit interfering with the transit grid. Disable it to restore the terminal uplink.",body);
            Label(1278,602,inspected.dead?"Disabled":$"Active · {Vector3.Distance(g.player.position,inspected.transform.position):0} m",muted,small,160);
            if(Button(1454,622,104,"Close"))g.inspectedTarget=null;
        }
        public void ShowCharacterTab(int tab){aoPanel.SelectTab(tab);}
        public void ShowNanoDetails(int id){aoPanel.SelectNano(id);}
        void Character() { aoPanel.Draw(g); }
        void Mission()
        {
            Panel(510,225,580,472);Label(543,250,"MISSION UPLINK",null,title);Label(543,299,"BOREALIS TERMINAL  /  SECURE CONNECTION",cyan,small);
            Label(543,346,"Signal interference",white,title);Label(543,393,"Rogue survey drones are disrupting the transit grid.",muted,text);
            Label(543,420,"Disable the signals, then return here for payment.",muted,text);
            if(!g.missionActive)
            {
                Label(543,466,$"THREAT LEVEL   {g.difficulty}    /    {g.difficulty+2} DRONES",amber,small);
                g.difficulty=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(543,508,505,20),g.difficulty,1,3));
                Label(543,546,$"{150+g.difficulty*100} credits     {100+g.difficulty*50} XP     Seed {g.seed+1}",cyan,text);
                if(Button(543,608,505,"ACCEPT CONTRACT"))g.AcceptMission();
            }
            else
            {
                Label(543,482,$"OBJECTIVE  {g.kills} / {g.required} DRONES DISABLED",amber,text);
                if(g.missionComplete && Button(543,550,505,"CLAIM REWARD"))g.Claim();
                if(Button(543,604,244,"Close uplink"))g.missionOpen=false;
                if(Button(801,604,247,"Abandon contract"))g.Abandon();
            }
        }
    }
}
