using UnityEngine;

namespace Reborn
{
    public static class DroneMovement
    {
        public static Vector3 Step(Vector3 origin,Vector3 destination,float maximumDistance,float radius)
        {
            Vector3 delta=destination-origin;float distance=Mathf.Min(delta.magnitude,Mathf.Max(0,maximumDistance));
            if(distance<=.0001f)return origin;
            if(Physics.CheckSphere(origin,radius,1<<0,QueryTriggerInteraction.Ignore))return origin;
            if(Physics.SphereCast(origin,radius,delta.normalized,out var hit,distance,1<<0,QueryTriggerInteraction.Ignore))
                distance=Mathf.Max(0,hit.distance-.03f);
            return origin+delta.normalized*distance;
        }
    }
}
