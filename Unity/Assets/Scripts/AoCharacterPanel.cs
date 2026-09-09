using System;
using System.Linq;
using UnityEngine;
namespace Reborn
{
    public sealed class AoCharacterPanel
    {
        int tab,page;string category="Abilities",query="",lastQuery="";
        AoItemRecord selectedItem;AoNanoRecord selectedNano;
        DistrictGame activeGame;
        AoItemRecord[] itemMatches;AoNanoRecord[] nanoMatches;
        string itemQuery,nanoQuery;
        readonly System.Collections.Generic.Dictionary<string,int> equipmentSlots=new System.Collections.Generic.Dictionary<string,int>();
        GUIStyle label,heading,dim,button;
        readonly Color cyan=new Color(.4f,.85f,.95f);
        public void SelectTab(int value){tab=value;page=0;query="";}
        public void SelectNano(int id)
        {
            var found=Array.Find(AoReference.Nanos.nanos,n=>n.id==id);
            if(found==null)return;
            SelectTab(3);selectedNano=found;query=found.name;
        }
        void Init()
        {
            if(label!=null)return;label=new GUIStyle(GUI.skin.label){fontSize=16};label.normal.textColor=Color.white;
            heading=new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold};dim=new GUIStyle(label){fontSize=13};dim.normal.textColor=new Color(.7f,.78f,.82f);
            button=new GUIStyle(GUI.skin.button){fontSize=14};button.normal.background=null;button.hover.background=null;button.active.background=null;
        }
        void Text(float x,float y,string s,GUIStyle style=null,float width=700)=>GUI.Label(new Rect(x,y,width,30),s,style??label);
        void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        bool Btn(float x,float y,float w,string s){Fill(new Rect(x,y,w,30),new Color(.07f,.13f,.17f));Fill(new Rect(x,y,w,1),new Color(.24f,.46f,.53f));return GUI.Button(new Rect(x,y,w,30),s,button);}
        public void Draw(DistrictGame game)
        {
            Init();activeGame=game;game.TextEntryFocused=false;var b=game.Build;
            Fill(new Rect(350,180,900,565),new Color(.018f,.034f,.046f,.99f));Fill(new Rect(350,180,900,2),cyan);
            Text(375,196,AoReference.Profession(b.profession).ToUpper()+"  /  "+AoReference.Breed(b.breed).name.ToUpper(),heading);
            Text(375,230,$"Level {b.level} · Title level {b.Title} · {b.ip:N0} improvement points · {b.xp:N0} / {AoReference.NextXp(b.level):N0} XP",dim);
            if(Btn(1202,194,30,"×"))game.characterOpen=false;
            string[] tabs={"SKILLS","EQUIPMENT","ITEM DATABASE","NANO LIBRARY"};
            for(int i=0;i<tabs.Length;i++)if(Btn(375+i*208,269,199,tabs[i])){tab=i;page=0;query="";}
            if(tab==0)Skills(game);else if(tab==1)Equipment(game);else if(tab==2)Items();else Nanos(game);
        }
        void Skills(DistrictGame game)
        {
            var b=game.Build;var cats=AoReference.Rules.skills.Select(s=>s.category).Distinct().ToArray();
            for(int i=0;i<cats.Length;i++)if(Btn(375,314+i*32,177,cats[i]))category=cats[i];
            Text(576,313,category,heading);Text(839,321,"BASE",dim);Text(900,321,"ABILITY",dim);Text(974,321,"TOTAL",dim);Text(1051,321,"RAISE",dim);
            var skills=AoReference.Rules.skills.Where(s=>s.category==category).ToArray();
            for(int i=0;i<skills.Length;i++)
            {
                var s=skills[i];float y=357+i*31;
                if(i%2==0)Fill(new Rect(570,y-2,656,30),new Color(.06f,.1f,.13f));
                Text(579,y,s.name,label,247);Text(845,y,b.Base(s.id).ToString());Text(916,y,"+"+b.Trickle(s.id));Text(983,y,b.Value(s.id).ToString());
                int price=b.Cost(s.id);GUI.enabled=!s.historical && b.ip>=price && b.Base(s.id)<b.Cap(s.id);
                if(Btn(1053,y-2,164,price<0?"Historical":"+1   /   "+price+" IP")){game.TrainSkill(s.id);}
                GUI.enabled=true;
            }
            Text(575,686,"Totals include ability trickle-down, equipment and active nanos.",dim,650);
            Text(575,713,"U closes this window. Full live-rule conformance is still in progress.",dim,650);
        }
        void Equipment(DistrictGame game)
        {
            var inventory=game.Build.inventory;var held=inventory.At(6);var weapon=held?.Definition.weapon;
            Text(380,317,"EQUIPMENT / INVENTORY",heading);
            Text(380,355,$"NCU {game.Build.UsedNcu} / {game.Build.MaxNcu}    ·    Belt decks {game.Build.Value(45)}",dim);
            int pages=Math.Max(1,(inventory.items.Count+6)/7);page=Math.Min(page,pages-1);
            Text(978,355,$"{page+1} / {pages}",dim,90);
            if(Btn(1082,350,60,"<"))page=Math.Max(0,page-1);
            if(Btn(1152,350,60,">"))page=Math.Min(pages-1,page+1);
            for(int i=0;i<Math.Min(7,inventory.items.Count-page*7);i++)
            {
                var item=inventory.items[page*7+i];float y=398+i*36;
                int efficiency=item.slot==0?100:AoOverEquipping.ArmorEfficiency(item.Definition,game.Build.Value);
                if(efficiency<100)Fill(new Rect(375,y-2,600,32),new Color(.3f,.07f,.045f));
                Text(380,y,item.Definition.name,label,400);Text(782,y,$"QL {item.ql} · {AoItems.SlotName(item.slot)}"+(efficiency<100?$" · OE {efficiency}%":""),dim,230);
                if(item.Definition.uploadNanoId!=0)
                {
                    GUI.enabled=!game.Equipping&&!game.Casting;
                    if(Btn(1082,y,130,"Upload")){game.UploadCrystal(item.instanceId);GUI.enabled=true;break;}
                    GUI.enabled=true;continue;
                }
                GUI.enabled=!game.Equipping&&item.Definition.equipDelayKnown;
                if(!equipmentSlots.TryGetValue(item.instanceId,out int slotIndex))slotIndex=0;
                int[] slots=item.Definition.slots;
                if(item.slot==0&&slots.Length>1)
                {
                    if(Btn(985,y,92,AoItems.SlotName(slots[slotIndex])+" >"))slotIndex=(slotIndex+1)%slots.Length;
                    equipmentSlots[item.instanceId]=slotIndex;
                }
                if(Btn(1082,y,130,item.slot==0?"Equip":"Unequip"))game.RequestEquipment(item.instanceId,item.slot==0?slots[slotIndex]:0);
                GUI.enabled=true;
            }
            Text(380,645,$"AC   Projectile {game.Build.Value(90)} · Melee {game.Build.Value(91)} · Energy {game.Build.Value(92)} · Chemical {game.Build.Value(93)} · Radiation {game.Build.Value(94)} · Cold {game.Build.Value(95)} · Poison {game.Build.Value(96)} · Fire {game.Build.Value(97)}",dim,840);
            if(weapon!=null)Text(380,674,$"Right hand: {weapon.minDamage}–{weapon.maxDamage} ({weapon.criticalBonus}) · {weapon.attackMs/1000f:0.00}s / {weapon.rechargeMs/1000f:0.00}s · {weapon.range}m",dim);
            else Text(380,674,"No ranged weapon equipped.",dim);
            Text(380,707,game.Equipping?$"Changing equipment… {Mathf.Max(0,game.equipRemaining):0.0}s":"Only owned items can be equipped. Item database entries are not inventory.",dim);
        }
        void Search()
        {
            Text(378,314,"SEARCH",dim);GUI.SetNextControlName("AoCatalogSearch");query=GUI.TextField(new Rect(451,311,584,29),query,100);
            activeGame.TextEntryFocused=GUI.GetNameOfFocusedControl()=="AoCatalogSearch";
            if(query!=lastQuery){page=0;lastQuery=query;}
            if(Btn(1050,311,78,"<"))page=Math.Max(0,page-1);if(Btn(1136,311,78,">"))page++;
        }
        void Items()
        {
            Search();if(itemMatches==null || itemQuery!=query){itemQuery=query;itemMatches=AoReference.Items.items.Where(i=>i.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();}var matches=itemMatches;
            page=Math.Min(page,Math.Max(0,(matches.Length-1)/8));
            for(int i=0;i<8 && page*8+i<matches.Length;i++)
            {
                var item=matches[page*8+i];float y=353+i*35;
                if(Btn(377,y,641,item.name))selectedItem=item;
                Text(1031,y+3,$"QL {item.lowQl}–{item.highQl}",dim,200);
            }
            Text(378,643,$"{matches.Length:N0} entries · Page {page+1} · Reference index {AoReference.Items.sourceVersion}",dim);
            if(selectedItem!=null){Text(378,677,$"IDs {selectedItem.lowId} → {selectedItem.highId} · QL {selectedItem.lowQl} → {selectedItem.highQl} · {(selectedItem.inGame?"In source game":"Source marks unavailable")}",dim);}
            Text(378,711,"Index records preserve identities and QL ranges; most are not yet usable items.",dim);
        }
        void Nanos(DistrictGame game)
        {
            Search();if(nanoMatches==null || nanoQuery!=query){nanoQuery=query;nanoMatches=AoReference.Nanos.nanos.Where(n=>n.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();}var matches=nanoMatches;
            page=Math.Min(page,Math.Max(0,(matches.Length-1)/7));
            for(int i=0;i<7&&page*7+i<matches.Length;i++)
            {
                var n=matches[page*7+i];var execution=AoSelfEffects.Get(n.id);
                float y=353+i*35;if(Btn(377,y,641,n.name))selectedNano=n;
                Text(1031,y+3,$"QL {n.ql} · {execution?.nanoCost??n.nanoCost} NP",dim,200);
            }
            Text(378,610,$"{matches.Length:N0} nano records · Page {page+1}",dim);
            if(selectedNano!=null)
            {
                var execution=AoSelfEffects.Get(selectedNano.id);
                Text(378,638,$"AO ID {selectedNano.id} · {selectedNano.professions} · {selectedNano.line}",dim);
                var requirements=execution?.castRequirements??selectedNano.requirements;
                Text(378,661,string.Join("   /   ",requirements.Select(r=>(AoReference.Skill(r.statId)?.name??(r.statId==54?"Level":"Stat "+r.statId))+" ≥ "+r.amount)),dim,840);
                if(execution!=null)
                {
                    Text(378,684,$"{execution.ncu} NCU · {execution.nanoCost} NP · Base cast {execution.castMs/1000f:0.##}s · Recharge {execution.rechargeMs/1000f:0.##}s · Duration {TimeSpan.FromSeconds(execution.duration):g}",dim,840);
                    GUI.enabled=game.Build.KnowsNano(selectedNano.id);
                    if(Btn(1090,705,125,GUI.enabled?"Cast on self":"Not learned"))game.CastReviewedNano(selectedNano.id);
                    GUI.enabled=true;
                }
                else Text(378,684,"Reference entry; execution is not implemented.",dim,840);
            }
            Text(378,711,"Upload an owned nano crystal in Equipment to learn its program.",dim,690);
        }
    }
}
