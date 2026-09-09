using System;
using UnityEngine;

namespace Reborn
{
    public static class TargetPickingValidation
    {
        public static void Run(DistrictGame game,Action<bool,string> check)
        {
            var first=UnityEngine.Object.Instantiate(game.dronePrefab,new Vector3(0,5,0),Quaternion.identity).AddComponent<DroneEnemy>();
            var second=UnityEngine.Object.Instantiate(game.dronePrefab,new Vector3(0,5,4),Quaternion.identity).AddComponent<DroneEnemy>();
            var third=UnityEngine.Object.Instantiate(game.dronePrefab,new Vector3(0,5,8),Quaternion.identity).AddComponent<DroneEnemy>();
            first.enabled=second.enabled=third.enabled=false;
            var candidates=new[]{third,second,first};var ray=new Ray(new Vector3(0,5,-10),Vector3.forward);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,5,-3);wall.transform.localScale=new Vector3(4,4,1);
            wall.SetActive(false);
            Vector3 cameraPosition=game.view.transform.position;Quaternion cameraRotation=game.view.transform.rotation;
            var oldTarget=game.target;var oldInspected=game.inspectedTarget;bool oldAttack=game.autoAttack,oldCharacter=game.characterOpen,oldMission=game.missionOpen,oldPause=game.Paused,oldText=game.TextEntryFocused;
            try
            {
                check(TargetPicking.Cycle(candidates,null,Vector3.zero,45,false)==first,"Tab initially chooses nearest rather than spawn order");
                check(TargetPicking.Cycle(candidates,first,Vector3.zero,45,false)==second,"Tab advances to next eligible target");
                check(TargetPicking.Cycle(candidates,second,Vector3.zero,45,false)==third,"Tab forward differs from backwards in a three enemy list");
                check(TargetPicking.Cycle(candidates,third,Vector3.zero,45,false)==first,"Tab wraps around target list");
                check(TargetPicking.Cycle(candidates,second,Vector3.zero,45,true)==first,"Shift Tab moves to previous target");
                check(TargetPicking.Cycle(candidates,first,Vector3.zero,45,true)==third,"Shift Tab wraps backwards");
                first.dead=true;
                check(TargetPicking.Cycle(candidates,first,Vector3.zero,45,false)==second,"Tab replaces a dead selection with live target");first.dead=false;
                first.gameObject.SetActive(false);
                check(TargetPicking.Cycle(candidates,null,Vector3.zero,45,false)==second,"Tab ignores inactive enemies");first.gameObject.SetActive(true);
                check(TargetPicking.Cycle(candidates,first,new Vector3(100,0,0),45,false)==null,"Tab clears selection when no enemy is in range");
                check(TargetPicking.Pick(candidates,ray,Vector3.zero,45)==first,"pointer chooses frontmost visible enemy independent of list order");
                first.dead=true;
                check(TargetPicking.Pick(candidates,ray,Vector3.zero,45)==second,"pointer skips dead targets");first.dead=false;
                first.gameObject.SetActive(false);
                check(TargetPicking.Pick(candidates,ray,Vector3.zero,45)==second,"pointer skips inactive targets");first.gameObject.SetActive(true);
                check(TargetPicking.Pick(candidates,ray,new Vector3(100,5,0),45)==null,"pointer obeys current target selection distance");
                check(TargetPicking.Pick(candidates,new Ray(ray.origin,Vector3.back),Vector3.zero,45)==null,"pointer ignores enemies behind its ray");
                wall.SetActive(true);Physics.SyncTransforms();
                check(TargetPicking.Pick(candidates,ray,Vector3.zero,45)==null,"solid scenery prevents selection through a wall");wall.SetActive(false);Physics.SyncTransforms();
                game.characterOpen=game.missionOpen=game.Paused=game.TextEntryFocused=false;
                var hud=game.GetComponent<DistrictHud>();
                Vector2 hotbar=new Vector2(800f/1600*Screen.width,(900-810f)/900*Screen.height);
                check(hud.BlocksWorldPointer(hotbar),"scaled hotbar blocks world picking");
                float cameraDistance=game.RequestedCameraDistance;
                check(!game.ZoomCameraAtScreen(hotbar,1)&&game.RequestedCameraDistance==cameraDistance,"scrolling over HUD does not zoom camera");
                check(!game.ZoomCameraAtScreen(new Vector2(-1,100),1)&&game.RequestedCameraDistance==cameraDistance,"scrolling outside game window does not zoom camera");
                game.view.transform.SetPositionAndRotation(ray.origin,Quaternion.LookRotation(ray.direction));
                game.enemies.Add(first);game.enemies.Add(second);game.target=null;
                Vector2 point=game.view.WorldToScreenPoint(first.transform.position);
                check(game.ZoomCameraAtScreen(point,.25f)&&Mathf.Abs(game.RequestedCameraDistance-(cameraDistance-.25f))<.001f,"scrolling over the world zooms camera");
                game.ZoomCameraAtScreen(point,-.25f);
                check(!game.OrbitCameraAtScreen(hotbar,true,true,Vector2.zero),"right click on HUD does not start camera orbit");
                check(!game.OrbitCameraAtScreen(point,false,true,Vector2.zero),"drag from HUD into world cannot acquire camera orbit");
                check(game.OrbitCameraAtScreen(point,true,true,Vector2.zero),"world right click starts camera orbit");
                check(game.OrbitCameraAtScreen(hotbar,false,true,Vector2.zero),"world camera drag continues when crossing HUD");
                check(!game.OrbitCameraAtScreen(point,false,false,Vector2.zero),"right button release ends camera orbit");
                game.OrbitCameraAtScreen(point,true,true,Vector2.zero);game.characterOpen=true;
                check(!game.OrbitCameraAtScreen(point,false,true,Vector2.zero),"opening character window cancels camera drag");game.characterOpen=false;
                check(!game.OrbitCameraAtScreen(point,false,true,Vector2.zero),"closing window requires a new camera drag press");
                bool attack=game.autoAttack;
                check(game.PointerTargetAtScreen(point)==first&&game.target==null&&game.autoAttack==attack,"hover resolves visible enemy without selecting or attacking");
                check(game.PointerTargetAtScreen(hotbar)==null,"hover over hotbar does not expose a world target");
                check(game.PointerTargetAtScreen(new Vector2(-1,100))==null,"pointer outside window has no target");
                check(game.TrySelectAtScreen(point)&&game.target==first,"screen-space click selects rendered enemy");
                check(game.autoAttack==attack,"selection does not toggle auto attack");
                check(game.InspectTarget()&&game.inspectedTarget==first,"inspect opens information for selected target");
                game.target=null;
                check(!game.InspectTarget()&&game.inspectedTarget==first,"missing target does not replace open information");
                game.inspectedTarget=null;
                check(game.TrySelectAtScreen(point,true)&&game.inspectedTarget==first,"shift click selects and inspects enemy");
                Vector2 infoPoint=new Vector2(1400f/1600*Screen.width,(900-560f)/900*Screen.height);
                check(hud.BlocksWorldPointer(infoPoint)&&!game.TrySelectAtScreen(infoPoint),"target information panel blocks world clicks");
                game.target=second;
                check(game.inspectedTarget==first,"changing selection preserves the inspected subject");
                second.dead=true;
                check(!game.InspectTarget()&&game.inspectedTarget==first,"dead target cannot open new information");second.dead=false;
                check(!game.TrySelectAtScreen(hotbar)&&game.target==second,"HUD click preserves target");
                game.characterOpen=true;
                check(!game.InspectTarget(),"character window blocks inspect shortcut");
                check(!game.TrySelectAtScreen(point)&&game.target==second,"character window blocks world selection");game.characterOpen=false;
                game.missionOpen=true;
                check(!game.TrySelectAtScreen(point)&&game.target==second,"mission window blocks world selection");game.missionOpen=false;
                game.autoAttack=true;game.target=first;first.dead=true;
                game.ToggleAttack();
                check(!game.autoAttack&&game.target==first,"stop combat does not acquire a new enemy after target death");first.dead=false;
                game.autoAttack=true;game.target=null;game.ToggleAttack();
                check(!game.autoAttack&&game.target==null,"stop combat succeeds without a target");
                game.target=second;
                game.Paused=true;
                check(!game.TrySelectAtScreen(point)&&game.target==second,"pause blocks world selection");
            }
            finally
            {
                game.enemies.Remove(first);game.enemies.Remove(second);game.target=oldTarget;
                game.inspectedTarget=oldInspected;
                game.autoAttack=oldAttack;
                game.characterOpen=oldCharacter;game.missionOpen=oldMission;game.Paused=oldPause;game.TextEntryFocused=oldText;
                game.view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
                first.gameObject.SetActive(false);second.gameObject.SetActive(false);
                third.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(first.gameObject);UnityEngine.Object.Destroy(second.gameObject);UnityEngine.Object.Destroy(third.gameObject);UnityEngine.Object.Destroy(wall);
            }
        }
    }
}
