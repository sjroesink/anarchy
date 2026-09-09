using UnityEngine;

namespace Reborn
{
    public sealed class HotbarView
    {
        public int editingSlot=-1;
        int editingLayer;
        Vector2 scroll;
        public readonly HotbarDrag drag=new HotbarDrag();
        public bool Busy=>editingSlot>=0||drag.Active;
        public bool Cancel(){bool active=Busy;editingSlot=-1;drag.Cancel();return active;}
        static int SlotAt(Vector2 point)
        {for(int slot=0;slot<10;slot++)if(new Rect(466+slot*66,793,62,52).Contains(point))return slot;return -1;}
        static readonly string[] names={"Attack","Burst","First Aid","Expertise","TMS Mk I","Skills","Walk / run"};
        public static string Name(int action)=>action<0&&action>=-7?names[-action-1]:action>0?AoSelfEffects.Get(action)?.name??"Unavailable":"Empty";
        static string ItemName(DistrictGame game,string id)=>game.Build.inventory.Find(id)?.Definition.name??"Missing item";
        static int NanoId(int action)=>action==-4?26370:action==-5?70308:action>0&&AoSelfEffects.Get(action)!=null?action:0;
        public void Draw(DistrictGame game)
        {
            var bar=game.Build.hotbar;
            if(!bar.visible||game.characterOpen||game.missionOpen||game.Paused){Cancel();return;}
            var input=Event.current;int pointed=SlotAt(input.mousePosition);
            if(input.type==EventType.MouseDown&&input.button==0&&pointed>=0&&editingSlot<0)
            {drag.Begin(bar,pointed,input.mousePosition);input.Use();}
            if(input.type==EventType.MouseDrag&&drag.Active){drag.Move(input.mousePosition);input.Use();}
            if(input.type==EventType.MouseDown&&input.button==1&&drag.Active){drag.Cancel();input.Use();}
            if(input.type==EventType.MouseUp&&input.button==0&&drag.Active)
            {int clicked=drag.Release(bar,pointed);input.Use();if(clicked>=0)game.ActivateHotbar(clicked);}
            var text=new GUIStyle(GUI.skin.label){fontSize=12};text.normal.textColor=new Color(.35f,.85f,1);
            var button=new GUIStyle(GUI.skin.button){fontSize=11,wordWrap=true};
            GUI.Label(new Rect(466,767,100,22),$"LAYER {bar.layer+1} / 10",text);
            if(GUI.Button(new Rect(566,767,28,20),"<",button))bar.layer=(bar.layer+9)%10;
            if(GUI.Button(new Rect(597,767,28,20),">",button))bar.layer=(bar.layer+1)%10;
            GUI.Label(new Rect(638,767,500,22),"Shift + 1–0: layer   •   Right-click: assign   •   Y: hide",text);
            for(int slot=0;slot<10;slot++)
            {
                int action=bar.Get(slot);var rect=new Rect(466+slot*66,793,62,52);
                string key=slot==9?"0":(slot+1).ToString();string name=!string.IsNullOrEmpty(bar.Item(slot))?ItemName(game,bar.Item(slot)):action==-1&&game.autoAttack?"Attack ON":Name(action);
                if(Event.current.type==EventType.MouseDown&&Event.current.button==1&&rect.Contains(Event.current.mousePosition))
                {editingSlot=slot;editingLayer=bar.layer;scroll=Vector2.zero;Event.current.Use();}
                int nano=string.IsNullOrEmpty(bar.Item(slot))?NanoId(action):0;
                bool executing=nano!=0&&game.CastingNanoId==nano;
                float wait=nano==0?0:executing?game.castRemaining:game.nanoRecharge;
                bool attack=string.IsNullOrEmpty(bar.Item(slot))&&action==-1&&game.autoAttack;
                if(attack)wait=game.WeaponCycleRemaining;
                string caption=wait>0?$"{key}  {wait:0.0}s":key;
                GUI.Box(rect,new GUIContent(caption+"\n"+name,name),button);
                if(executing||attack&&wait>0)
                {
                    var previous=GUI.color;GUI.color=new Color(.15f,.8f,1);
                    GUI.DrawTexture(new Rect(rect.x+3,rect.yMax-5,(rect.width-6)*(executing?game.CastProgress:game.WeaponCycleProgress),3),Texture2D.whiteTexture);
                    GUI.color=previous;
                }
                if(!string.IsNullOrEmpty(bar.Item(slot))&&game.PendingEquipmentId==bar.Item(slot))
                {
                    var previous=GUI.color;GUI.color=new Color(.15f,.8f,1);
                    GUI.DrawTexture(new Rect(rect.x+3,rect.yMax-5,(rect.width-6)*game.EquipmentProgress,3),Texture2D.whiteTexture);
                    GUI.color=previous;
                }
            }
            if(pointed>=0&&!Busy)
            {
                string id=bar.Item(pointed);var item=string.IsNullOrEmpty(id)?null:game.Build.inventory.Find(id);
                string detail=string.IsNullOrEmpty(id)?Name(bar.Get(pointed)):item==null?"Missing item":$"{item.Definition.name}\nQL {item.ql} · {(item.slot==0?"In inventory":"Equipped")}";
                if(item!=null&&game.PendingEquipmentId==id)
                    detail+=$"\n{(game.RemovingEquipment?"Unequipping":"Equipping")} · {Mathf.Max(0,game.equipRemaining):0.0}s";
                int nano=string.IsNullOrEmpty(id)?NanoId(bar.Get(pointed)):0;
                if(string.IsNullOrEmpty(id)&&bar.Get(pointed)==-1)
                {
                    string blocked=game.WeaponBlockReason();
                    detail+="\n"+(blocked??(game.WeaponCycleRemaining>0?$"{game.WeaponPhase} · {game.WeaponCycleRemaining:0.0}s":"Weapon ready"));
                }
                if(nano!=0)
                {
                    if(game.CastingNanoId==nano)detail+=$"\nExecuting · {Mathf.Max(0,game.castRemaining):0.0}s";
                    else if(game.Casting)detail+="\nAnother nano is executing";
                    else if(game.nanoRecharge>0)detail+=$"\nNano recharge · {game.nanoRecharge:0.0}s";
                }
                var tooltip=new GUIStyle(GUI.skin.box){fontSize=14,wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,8,8)};
                float height=tooltip.CalcHeight(new GUIContent(detail),340);
                GUI.Box(new Rect(Mathf.Clamp(466+pointed*66,453,807),755-height,340,height),detail,tooltip);
            }
            if(drag.Active&&drag.moved)
                GUI.Box(new Rect(input.mousePosition.x+12,input.mousePosition.y-32,160,30),string.IsNullOrEmpty(drag.item)?Name(drag.action):ItemName(game,drag.item));
        }
        public void DrawEditor(DistrictGame game)
        {
            if(editingSlot<0||!game.Build.hotbar.visible||game.characterOpen||game.missionOpen||game.Paused)return;
            GUI.Box(new Rect(453,390,694,355),GUIContent.none);
            var text=new GUIStyle(GUI.skin.label){fontSize=16};text.normal.textColor=Color.white;
            var button=new GUIStyle(GUI.skin.button){fontSize=14,alignment=TextAnchor.MiddleLeft};
            GUI.Label(new Rect(470,401,550,26),$"Assign shortcut · layer {editingLayer+1}, slot {(editingSlot+1)%10}",text);
            if(GUI.Button(new Rect(1044,399,86,28),"Close")){editingSlot=-1;return;}
            int known=0;foreach(var effect in AoSelfEffects.Catalog.effects)if(game.Build.KnowsNano(effect.id))known++;
            scroll=GUI.BeginScrollView(new Rect(470,440,660,285),scroll,new Rect(0,0,635,(8+known+game.Build.inventory.items.Count)*34));
            int row=0;
            if(GUI.Button(new Rect(0,row++*34,635,30),"Clear slot",button))Assign(game,0);
            for(int i=0;i<7;i++)if(GUI.Button(new Rect(0,row++*34,635,30),names[i],button))Assign(game,-i-1);
            foreach(var effect in AoSelfEffects.Catalog.effects)
                if(game.Build.KnowsNano(effect.id)&&GUI.Button(new Rect(0,row++*34,635,30),effect.name,button))Assign(game,effect.id);
            foreach(var item in game.Build.inventory.items)
                if(GUI.Button(new Rect(0,row++*34,635,30),$"Item · {item.Definition.name} · QL {item.ql}",button)&&editingSlot>=0)
                {int currentLayer=game.Build.hotbar.layer;game.Build.hotbar.layer=editingLayer;game.Build.hotbar.AssignItem(editingSlot,item.instanceId);game.Build.hotbar.layer=currentLayer;editingSlot=-1;}
            GUI.EndScrollView();
        }
        void Assign(DistrictGame game,int action)
        {
            if(editingSlot<0)return;
            game.Build.hotbar.slots[editingLayer*10+editingSlot]=action;game.Build.hotbar.items[editingLayer*10+editingSlot]=null;editingSlot=-1;
        }
    }
}
