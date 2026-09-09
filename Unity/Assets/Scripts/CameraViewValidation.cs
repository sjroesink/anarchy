using System;
using UnityEngine;

namespace Reborn
{
    public static class CameraViewValidation
    {
        public static void Run(DistrictGame game,Action<bool,string> check)
        {
            Vector3 position=game.player.position;
            var motor=game.player.GetComponent<CharacterController>();bool enabled=motor.enabled;
            bool paused=game.Paused,character=game.characterOpen,mission=game.missionOpen,text=game.TextEntryFocused;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.SetActive(false);
            var part=new GameObject("Inactive camera QA equipment");part.transform.SetParent(game.visual,false);part.SetActive(false);
            float distance=game.RequestedCameraDistance;var build=game.Build;
            try
            {
                game.Paused=game.characterOpen=game.missionOpen=game.TextEntryFocused=false;
                motor.enabled=false;game.player.position=new Vector3(1000,1000,1000);
                check(!game.FirstPerson&&game.ToggleCameraView()&&game.FirstPerson,"F8 action enters first-person view");
                game.UpdateCameraView();int playerMask=1<<game.player.gameObject.layer;
                check(Vector3.Distance(game.view.transform.position,game.player.position+Vector3.up*1.8f)<.001f,"first-person camera follows eye anchor without boom");
                check((game.view.cullingMask&playerMask)==0&&game.visual.gameObject.activeSelf,"first-person hides local layer without deactivating character");
                check(!part.activeSelf&&game.Build==build,"view change preserves hidden equipment and character data");
                check(!game.ZoomCameraAtScreen(new Vector2(Screen.width*.5f,Screen.height*.5f),1)&&game.RequestedCameraDistance==distance,"first-person wheel preserves remembered third-person zoom");
                game.player.position+=Vector3.forward;game.UpdateCameraView();
                check(Vector3.Distance(game.view.transform.position,game.player.position+Vector3.up*1.8f)<.001f,"eye camera follows player displacement immediately");
                Vector3 boom=-game.view.transform.forward;
                check(game.ToggleCameraView()&&!game.FirstPerson,"F8 returns to third-person view");game.UpdateCameraView();
                check((game.view.cullingMask&playerMask)!=0&&!part.activeSelf&&game.RequestedCameraDistance==distance,"third-person restores visibility and zoom without restoring unequipped objects");
                wall.transform.position=game.player.position+Vector3.up*1.65f+boom*.8f;
                wall.transform.rotation=Quaternion.LookRotation(boom);wall.transform.localScale=new Vector3(4,4,.2f);
                wall.SetActive(true);Physics.SyncTransforms();game.UpdateCameraView();
                check(!game.FirstPerson&&(game.view.cullingMask&playerMask)==0,"close obstruction hides local body without changing chosen camera mode");
                wall.SetActive(false);Physics.SyncTransforms();game.ToggleCameraView();game.ToggleCameraView();game.UpdateCameraView();
                check((game.view.cullingMask&playerMask)!=0,"clear camera restores local body rendering");
                game.Paused=true;check(!game.ToggleCameraView()&&!game.FirstPerson,"pause blocks F8");game.Paused=false;
                game.characterOpen=true;check(!game.ToggleCameraView(),"character window blocks F8");game.characterOpen=false;
                game.missionOpen=true;check(!game.ToggleCameraView(),"mission window blocks F8");game.missionOpen=false;
                game.TextEntryFocused=true;check(!game.ToggleCameraView(),"text entry blocks F8");game.TextEntryFocused=false;
            }
            finally
            {
                game.Paused=game.characterOpen=game.missionOpen=game.TextEntryFocused=false;
                if(game.FirstPerson)game.ToggleCameraView();
                wall.SetActive(false);Physics.SyncTransforms();game.player.position=position;motor.enabled=enabled;
                game.UpdateCameraView();game.Paused=paused;game.characterOpen=character;game.missionOpen=mission;game.TextEntryFocused=text;
                UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(part);
            }
        }
    }
}
