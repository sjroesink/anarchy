using UnityEngine;

namespace Reborn
{
    // Authored presentation, independent of combat RNG, damage and cooldowns.
    public sealed class CombatVisuals : MonoBehaviour
    {
        LineRenderer[] lines;
        Vector3[] origins, velocities;
        float age, duration;
        Color color;
        bool burst;
        public static bool MissEndpoint(Vector3 origin,Bounds target,out Vector3 endpoint)
        {
            endpoint=origin;
            Vector3 delta=target.center-origin;float distance=delta.magnitude;
            // Enclose all renderer bounds and the tracer width in a conservative sphere.
            float radius=target.extents.magnitude+.12f;
            if(distance<=radius)return false; // No clear outgoing ray from inside this volume.
            Vector3 forward=delta/distance;
            Vector3 side=Vector3.Cross(Vector3.up,forward);
            if(side.sqrMagnitude<.001f)side=Vector3.Cross(Vector3.right,forward);
            side.Normalize();
            float tangent=radius/Mathf.Sqrt(distance*distance-radius*radius);
            Vector3 direction=(forward+side*tangent*1.05f).normalized;
            endpoint=origin+direction*(distance+radius+2);
            return true;
        }

        public static void Tracer(Material material,Vector3 from,Vector3 to,Color tint)
        {
            var effect=new GameObject("Weapon tracer").AddComponent<CombatVisuals>();
            effect.duration=.12f;effect.color=tint;
            effect.lines=new[]{effect.MakeLine(material,from,to,.045f)};
        }

        public static void Impact(Material material,Vector3 at,bool destroyed)
        {
            var effect=new GameObject(destroyed?"Drone discharge":"Projectile impact").AddComponent<CombatVisuals>();
            effect.duration=destroyed?.55f:.24f;effect.burst=true;
            effect.color=destroyed?new Color(.35f,.85f,1):new Color(1,.7f,.28f);
            int count=destroyed?22:9;
            effect.lines=new LineRenderer[count];effect.origins=new Vector3[count];effect.velocities=new Vector3[count];
            // A spherical distribution avoids touching Unity's shared gameplay random state.
            for(int i=0;i<count;i++)
            {
                float y=1-2*(i+.5f)/count,angle=i*2.399963f;
                float radius=Mathf.Sqrt(1-y*y);
                var direction=new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
                effect.origins[i]=at;
                effect.velocities[i]=direction*(destroyed?3.5f:2.2f);
                effect.lines[i]=effect.MakeLine(material,at,at+direction*.1f,destroyed?.045f:.025f);
            }
        }

        LineRenderer MakeLine(Material material,Vector3 from,Vector3 to,float width)
        {
            var child=new GameObject("Streak");child.transform.SetParent(transform);
            var line=child.AddComponent<LineRenderer>();line.sharedMaterial=material;
            line.positionCount=2;line.SetPosition(0,from);line.SetPosition(1,to);
            line.startWidth=width;line.endWidth=width*.25f;
            line.startColor=line.endColor=color;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;
            return line;
        }

        void Update()
        {
            if(DistrictGame.Instance&&DistrictGame.Instance.Paused)return;
            age+=Time.deltaTime;
            if(age>=duration){Destroy(gameObject);return;}
            float fade=1-age/duration;
            var tint=color;tint.a=fade;
            for(int i=0;i<lines.Length;i++)
            {
                lines[i].startColor=lines[i].endColor=tint;
                if(!burst)continue;
                Vector3 head=origins[i]+velocities[i]*age+Vector3.down*(2*age*age);
                lines[i].SetPosition(0,head-velocities[i]*Mathf.Min(age,.035f));
                lines[i].SetPosition(1,head);
            }
        }
    }
}
