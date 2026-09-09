using UnityEngine;

namespace Reborn
{
    public static class CameraObstruction
    {
        public static float Distance(Vector3 focus,Vector3 offset)
        {
            float length=offset.magnitude;
            if(length<=.001f)return 0;
            // The collision radius also protects the camera near plane.
            // Never enforce a minimum distance beyond an intervening obstacle.
            if(Physics.CheckSphere(focus,.25f,1<<0,QueryTriggerInteraction.Ignore))return 0;
            return Physics.SphereCast(focus,.25f,offset/length,out var hit,length,1<<0,QueryTriggerInteraction.Ignore)
                ?Mathf.Clamp(hit.distance-.2f,0,length):length;
        }
    }
}
