using System;
using UnityEngine;

namespace Reborn
{
    public static class CameraObstructionValidation
    {
        public static void Run(Action<bool,string> check)
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var focus=new Vector3(1000,1000,1000);var offset=Vector3.back*7;
            wall.transform.position=focus+Vector3.back*.8f;
            wall.transform.localScale=new Vector3(4,4,.2f);
            try
            {
                Physics.SyncTransforms();
                float near=CameraObstruction.Distance(focus,offset);
                var droneStep=DroneMovement.Step(focus,focus+offset,7,.25f);
                check(Vector3.Distance(focus,droneStep)>.3f&&Vector3.Distance(focus,droneStep)<.5f,"drone pursuit stops before a solid wall even on a large movement step");
                check(near>.1f&&near<.3f,"close camera obstruction stays in front of wall instead of enforcing one metre");
                wall.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();
                check(Mathf.Abs(CameraObstruction.Distance(focus,offset)-7)<.001f,"trigger volumes do not shorten camera boom");
                wall.GetComponent<Collider>().isTrigger=false;
                wall.transform.position=focus;Physics.SyncTransforms();
                check(CameraObstruction.Distance(focus,offset)==0,"camera boom does not extend through an overlapping anchor obstruction");
                wall.SetActive(false);Physics.SyncTransforms();
                check(Vector3.Distance(DroneMovement.Step(focus,focus+offset,2,.25f),focus+Vector3.back*2)<.001f,"clear drone pursuit preserves its movement speed");
                check(Mathf.Abs(CameraObstruction.Distance(focus,offset)-7)<.001f,"clear view restores requested camera distance");
            }
            finally{wall.SetActive(false);UnityEngine.Object.Destroy(wall);}
        }
    }
}
