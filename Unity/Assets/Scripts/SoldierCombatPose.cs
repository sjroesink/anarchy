using System.Linq;
using System.Collections;
using UnityEngine;

namespace Reborn
{
    // Presentation only: two-hand aim and recoil do not modify hit rolls or attack timers.
    [DefaultExecutionOrder(100)]
    public sealed class SoldierCombatPose:MonoBehaviour
    {
        public DistrictGame game;
        Transform rightUpper,rightElbow,rightHand,leftUpper,leftElbow,leftHand;
        Quaternion rightGrip,leftGrip;
        Transform muzzle,supportWrist;
        float weight,recoil;
        Light flash;
        SkinnedMeshRenderer barrel;
        bool ready;
        public float AimWeight=>weight;
        public Vector3 MuzzlePosition=>muzzle?muzzle.position:game.player.position+Vector3.up*1.3f;
        IEnumerator Start()
        {
            // Sample the imported animated bind pose before attaching the model-space muzzle.
            yield return new WaitForEndOfFrame();
            var bones=game.visual.GetComponentsInChildren<Transform>();
            rightUpper=bones.FirstOrDefault(t=>t.name=="Rig_UpperArmR");rightElbow=bones.FirstOrDefault(t=>t.name=="Rig_ForearmR");rightHand=bones.FirstOrDefault(t=>t.name=="Rig_HandR");
            leftUpper=bones.FirstOrDefault(t=>t.name=="Rig_UpperArmL");leftElbow=bones.FirstOrDefault(t=>t.name=="Rig_ForearmL");leftHand=bones.FirstOrDefault(t=>t.name=="Rig_HandL");
            if(!rightUpper||!rightElbow||!rightHand||!leftUpper||!leftElbow||!leftHand){enabled=false;yield break;}
            rightGrip=Quaternion.Inverse(game.visual.rotation)*rightHand.rotation;
            leftGrip=Quaternion.Inverse(game.visual.rotation)*leftHand.rotation;
            barrel=game.visual.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r=>r.name=="Barrel");
            if(!barrel){enabled=false;yield break;}
            muzzle=bones.FirstOrDefault(t=>t.name=="RifleMuzzle");
            if(!muzzle){enabled=false;yield break;}
            supportWrist=bones.FirstOrDefault(t=>t.name=="RifleSupportWrist");
            if(!supportWrist){enabled=false;yield break;}
            flash=new GameObject("Rifle muzzle light").AddComponent<Light>();flash.type=LightType.Point;
            flash.color=new Color(.35f,.85f,1);flash.range=2.5f;flash.intensity=0;flash.shadows=LightShadows.None;
            flash.transform.SetParent(transform);
            ready=true;
        }
        public void Shot(){recoil=1;}
        public float SupportWristDistance()=>supportWrist&&leftHand?Vector3.Distance(leftHand.position,supportWrist.position):float.PositiveInfinity;
        public float MuzzleSurfaceDistance()
        {
            if(!barrel)return float.PositiveInfinity;
            var mesh=barrel.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            float distance=float.PositiveInfinity;
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];Vector3 point=Vector3.zero;
                int[] indices={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};
                float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};
                for(int j=0;j<4;j++)if(amounts[j]>0)point+=barrel.bones[indices[j]].TransformPoint(bind[indices[j]].MultiplyPoint3x4(vertices[i]))*amounts[j];
                distance=Mathf.Min(distance,Vector3.Distance(point,MuzzlePosition));
            }
            return distance;
        }
        void LateUpdate()
        {
            if(!ready)return;
            bool aiming=game.autoAttack&&game.target&&!game.target.dead&&game.Build.Weapon!=null&&!game.Casting&&!game.Equipping;
            weight=Mathf.MoveTowards(weight,aiming?1:0,Time.deltaTime*5);
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*12);
            if(weight>0)
            {
                var frame=game.visual;
                Solve(rightUpper,rightElbow,rightHand,frame.TransformPoint(new Vector3(.08f,1.30f,.18f-recoil*.045f)),frame.TransformPoint(new Vector3(.48f,1.02f,.02f)),frame.rotation*rightGrip,weight);
                Solve(leftUpper,leftElbow,leftHand,supportWrist.position,frame.TransformPoint(new Vector3(-.36f,1.02f,.20f)),frame.rotation*leftGrip,weight);
            }
            flash.transform.position=MuzzlePosition;flash.intensity=recoil>.5f?(recoil-.5f)*4:0;
        }
        static void Solve(Transform upper,Transform elbow,Transform hand,Vector3 target,Vector3 hint,Quaternion grip,float blend)
        {
            Quaternion a=upper.localRotation,b=elbow.localRotation,c=hand.localRotation;
            Vector3 origin=upper.position,axis=target-origin;
            float first=Vector3.Distance(origin,elbow.position),second=Vector3.Distance(elbow.position,hand.position);
            float distance=Mathf.Clamp(axis.magnitude,Mathf.Abs(first-second)+.001f,first+second-.001f);
            axis.Normalize();target=origin+axis*distance;
            Vector3 bend=Vector3.ProjectOnPlane(hint-origin,axis).normalized;
            float along=(first*first-second*second+distance*distance)/(2*distance);
            Vector3 joint=origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            upper.rotation=Quaternion.FromToRotation(elbow.position-origin,joint-origin)*upper.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
            hand.rotation=grip;
            Quaternion aimA=upper.localRotation,aimB=elbow.localRotation,aimC=hand.localRotation;
            upper.localRotation=Quaternion.Slerp(a,aimA,blend);elbow.localRotation=Quaternion.Slerp(b,aimB,blend);hand.localRotation=Quaternion.Slerp(c,aimC,blend);
        }
    }
}
