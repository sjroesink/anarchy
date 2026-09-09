using System;
using UnityEngine;

namespace Reborn
{
    public static class HotbarValidation
    {
        public static void Run(DistrictGame game,Action<bool,string> check)
        {
            var drag=new HotbarDrag();var bar=new AoHotbar();
            drag.Begin(bar,0,Vector2.zero);drag.Move(Vector2.right*20);
            check(drag.Release(bar,8)==-1&&bar.Get(8)==-1&&bar.Get(0)==0,"drag moves to empty slot without producing an activation");
            drag.Begin(bar,8,Vector2.zero);drag.Move(Vector2.right*20);drag.Release(bar,1);
            check(bar.Get(1)==-1&&bar.Get(8)==-2,"occupied shortcut destinations swap without losing either action");
            drag.Begin(bar,1,Vector2.zero);check(drag.Release(bar,1)==1,"stationary press and release remains a normal shortcut click");
            drag.Begin(bar,1,Vector2.zero);drag.Move(Vector2.right*20);drag.Release(bar,-1);
            check(bar.Get(1)==-1&&!drag.Active,"release outside bar cancels without deleting the shortcut");
            drag.Begin(bar,1,Vector2.zero);drag.Move(Vector2.right*20);drag.Cancel();
            check(bar.Get(1)==-1&&!drag.Active,"cancel leaves assignments unchanged");
            drag.Begin(bar,1,Vector2.zero);drag.Move(Vector2.right*20);bar.layer=9;drag.Release(bar,9);
            check(bar.Get(9)==-1&&bar.slots[1]==0,"drag can move a shortcut between layers");
            drag.Begin(bar,9,Vector2.zero);drag.Move(Vector2.right*20);bar.Assign(9,-7);drag.Release(bar,8);
            check(bar.Get(9)==-7&&bar.Get(8)==0,"stale drag cannot overwrite a changed source assignment");
            var fixture=new CharacterBuild();fixture.hotbar.layer=9;fixture.hotbar.Assign(9,26370);
            check(fixture.hotbar.Get(9)==26370&&fixture.hotbar.slots[0]==-1,"tenth layer and tenth slot are independent of first layer");
            string json=JsonUtility.ToJson(fixture),originalBarJson=JsonUtility.ToJson(fixture.hotbar);
            var restored=CharacterBuild.ParseSave(json);
            check(restored!=null&&restored.hotbar.layer==9&&restored.hotbar.Get(9)==26370,"hotbar assignments survive character save roundtrip");
            string ownedId=fixture.inventory.items[0].instanceId;
            fixture.hotbar.AssignItem(8,ownedId);
            var itemRestored=CharacterBuild.ParseSave(JsonUtility.ToJson(fixture));
            check(itemRestored!=null&&itemRestored.hotbar.Item(8)==ownedId,"item shortcut persists exact owned instance identity");
            drag.Begin(fixture.hotbar,8,Vector2.zero);drag.Move(Vector2.right*20);drag.Release(fixture.hotbar,9);
            check(fixture.hotbar.Item(9)==ownedId&&fixture.hotbar.Get(8)==26370&&string.IsNullOrEmpty(fixture.hotbar.Item(8)),"item and nano shortcuts swap together without stale references");
            fixture.hotbar.Assign(9,-1);check(string.IsNullOrEmpty(fixture.hotbar.Item(9)),"assigning action clears previous item reference");
            string legacy=json.Replace(",\"hotbar\":"+originalBarJson,"");
            var migrated=CharacterBuild.ParseSave(legacy);
            check(migrated!=null&&migrated.hotbar.Get(0)==-1,"older save without hotbar receives default shortcuts");
            fixture.hotbar.slots=new int[99];check(!fixture.Valid(),"malformed hotbar slot count is rejected");
            var oldBar=game.Build.hotbar;bool oldWalking=game.Walking;
            try
            {
                game.Build.hotbar=new AoHotbar();game.Build.hotbar.layer=1;
                game.ActivateHotbar(9);check(game.Walking==oldWalking,"empty shortcut does not execute default first-layer action");
                game.Build.hotbar.Assign(9,-7);game.Build.hotbar.visible=false;
                game.ActivateHotbar(9);check(game.Walking!=oldWalking,"hidden shortcut bar still executes assigned action");
                game.Build.hotbar.Assign(9,0);game.ActivateHotbar(9);
                check(game.Walking!=oldWalking,"cleared shortcut no longer executes its previous action");
                game.Build.hotbar.Assign(9,int.MaxValue);float nano=game.Nano;
                game.ActivateHotbar(9);check(game.Nano==nano&&!game.Casting,"unknown saved nano action cannot bypass reviewed nano execution");
                game.Build.hotbar.AssignItem(9,"missing-owned-instance");game.ActivateHotbar(9);
                check(!game.Equipping&&game.Nano==nano,"missing item shortcut cannot create an item or begin equip");
            }
            finally{game.Build.hotbar=oldBar;game.Walking=oldWalking;}
        }
    }
}
