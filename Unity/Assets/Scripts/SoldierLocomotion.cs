using UnityEngine;

namespace Reborn
{
    // Visual-only controller: movement/collision and combat timing stay in DistrictGame.
    public sealed class SoldierLocomotion : MonoBehaviour
    {
        public Animator animator;
        Vector3 previousPosition;
        void OnEnable(){previousPosition=transform.position;}
        void LateUpdate()
        {
            var delta=transform.position-previousPosition;previousPosition=transform.position;
            delta.y=0;
            if(!animator||Time.deltaTime<=0)return;
            float speed=delta.magnitude>3?0:delta.magnitude/Time.deltaTime;
            int mode=speed>=4?2:speed>.1f?1:0;
            if(speed>.1f)
            {
                Vector3 local=animator.transform.InverseTransformDirection(delta);
                if(Mathf.Abs(local.x)>Mathf.Abs(local.z)*1.2f)mode=local.x<0?3:4;
                else if(local.z<0)mode=5;
            }
            animator.SetInteger("Locomotion",mode);
            animator.speed=mode==2?Mathf.Clamp(speed/6f,.7f,1.6f):mode>0?Mathf.Clamp(speed/2f,.5f,2.5f):1;
        }
    }
}
