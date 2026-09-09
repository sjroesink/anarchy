using System;
using UnityEngine;

namespace Reborn
{
    public static class TerminalInteractionValidation
    {
        public static void Run(DistrictGame game,Action<bool,string> check)
        {
            var motor=game.player.GetComponent<CharacterController>();bool motorEnabled=motor.enabled;
            Vector3 playerPosition=game.player.position,cameraPosition=game.view.transform.position;
            Quaternion cameraRotation=game.view.transform.rotation;
            bool character=game.characterOpen,mission=game.missionOpen,pause=game.Paused,text=game.TextEntryFocused;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.SetActive(false);
            try
            {
                motor.enabled=false;game.player.position=game.terminal.position+Vector3.back*2;
                game.characterOpen=game.missionOpen=game.Paused=game.TextEntryFocused=false;
                Vector3 display=game.terminal.position+Vector3.up*1.8f;
                game.view.transform.SetPositionAndRotation(display+Vector3.back*4,Quaternion.identity);
                Physics.SyncTransforms();Vector2 point=game.view.WorldToScreenPoint(display);
                check(game.PointerTerminalAtScreen(point),"visible nearby terminal resolves through its actual scene collider");
                check(!game.TerminalPointerAtScreen(point,true,false)&&!game.missionOpen,"press alone permits camera orbit without opening mission window");
                check(game.TerminalPointerAtScreen(point,false,true)&&game.missionOpen,"right click release opens nearby terminal");game.missionOpen=false;
                check(!game.TerminalPointerAtScreen(point,false,true)&&!game.missionOpen,"release cannot reuse a consumed terminal press");
                game.TerminalPointerAtScreen(point,true,false);
                game.TerminalPointerAtScreen(point+Vector2.right*20,false,false);
                check(!game.TerminalPointerAtScreen(point,false,true)&&!game.missionOpen,"camera drag returning to terminal does not activate it");
                Vector2 hud=new Vector2(Screen.width*.5f,Screen.height*.1f);
                game.TerminalPointerAtScreen(hud,true,false);
                check(!game.TerminalPointerAtScreen(point,false,true),"HUD press cannot become terminal use on release");
                game.TerminalPointerAtScreen(point,true,false);game.characterOpen=true;
                game.TerminalPointerAtScreen(point,false,false);game.characterOpen=false;
                check(!game.TerminalPointerAtScreen(point,false,true),"opening a modal cancels pending terminal use");
                game.TerminalPointerAtScreen(point,true,false);
                game.SendMessage("OnApplicationFocus",false);
                check(!game.TerminalPointerAtScreen(point,false,true),"focus loss cancels pending terminal use");
                wall.transform.position=display+Vector3.back*2;wall.transform.localScale=new Vector3(2,3,.25f);
                wall.SetActive(true);Physics.SyncTransforms();
                check(!game.PointerTerminalAtScreen(point),"wall prevents clicking terminal through scenery");
                wall.SetActive(false);Physics.SyncTransforms();
                game.TerminalPointerAtScreen(point,true,false);game.player.position=game.terminal.position+Vector3.back*8;
                check(!game.TerminalPointerAtScreen(point,false,true),"moving out of range before release prevents use");
                game.player.position=game.terminal.position+Vector3.back*2;
                game.Paused=true;check(!game.PointerTerminalAtScreen(point),"pause blocks terminal use");game.Paused=false;
                game.TextEntryFocused=true;check(!game.PointerTerminalAtScreen(point),"text entry blocks terminal use");game.TextEntryFocused=false;
                check(!game.PointerTerminalAtScreen(new Vector2(-1,100)),"pointer outside window cannot use terminal");
            }
            finally
            {
                game.TerminalPointerAtScreen(Vector2.zero,false,true);
                game.player.position=playerPosition;motor.enabled=motorEnabled;
                game.view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
                game.characterOpen=character;game.missionOpen=mission;game.Paused=pause;game.TextEntryFocused=text;
                wall.SetActive(false);UnityEngine.Object.Destroy(wall);Physics.SyncTransforms();
            }
        }
    }
}
