using UnityEngine;

namespace Reborn
{
    public sealed class DroneEnemy : MonoBehaviour
    {
        public float health = 110, maxHealth = 110;
        public int index;
        public bool dead;
        public Vector3 home;
        // Explicit training fixture AC values, not claimed AO mob statistics.
        public AoStatModifier[] armorClasses=new AoStatModifier[0];
        public int ArmorClass(int statId)
        {
            int value=0;foreach(var armor in armorClasses)if(armor.statId==statId)value+=armor.amount;
            return Mathf.Max(0,value);
        }
        float shot;
        float movementRadius=.65f;
        void Start()
        {
            var bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var renderer in GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            movementRadius=Mathf.Max(.65f,bounds.extents.magnitude);
        }
        public void Hit(float amount)
        {
            if (dead) return;
            health = Mathf.Max(0, health - amount);
            DistrictGame.Instance.DamageNumbers.Add(transform.position,amount,false);
            if(amount>0)
            {
                Vector3 impact=transform.position;
                if(health>0)
                {
                    var renderers=GetComponentsInChildren<Renderer>();
                    if(renderers.Length>0)
                    {
                        Vector3 from=DistrictGame.Instance.player.position+Vector3.up*1.3f;
                        var ray=new Ray(from,(transform.position-from).normalized);
                        float nearest=float.PositiveInfinity;
                        foreach(var renderer in renderers)
                            if(renderer.bounds.IntersectRay(ray,out float distance))nearest=Mathf.Min(nearest,distance);
                        if(!float.IsPositiveInfinity(nearest))impact=ray.GetPoint(Mathf.Max(0,nearest-.025f));
                    }
                }
                DistrictGame.Instance.DroneImpact(impact,health<=0);
            }
            if (health > 0) return;
            dead = true;
            DistrictGame.Instance.Killed(this);
            gameObject.SetActive(false);
        }
        void Update()
        {
            var game = DistrictGame.Instance;
            if (!game || dead || game.Paused) return;
            float dt = Time.deltaTime;
            var delta = game.player.position - transform.position;
            if (delta.magnitude < 18 && game.Health > 0)
            {
                Vector3 to = game.player.position + Vector3.up * 1.8f;
                if (delta.magnitude > 8) transform.position = DroneMovement.Step(transform.position,to,dt*2.7f,movementRadius);
                Face(delta,dt);
                shot -= dt;
                if (shot <= 0) { shot = 2.3f; game.EnemyShot(this); }
            }
            else
            {
                Vector3 destination=home+Vector3.up*(Mathf.Sin(Time.time*1.5f+index)*.22f);
                Vector3 heading=destination-transform.position;
                transform.position=DroneMovement.Step(transform.position,destination,dt*2.7f,movementRadius);
                if(new Vector2(heading.x,heading.z).sqrMagnitude>.04f)Face(heading,dt);
            }
        }
        void Face(Vector3 direction,float seconds)
        {
            direction.y=0;
            if(direction.sqrMagnitude>.0001f)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),seconds*3);
        }
    }
}
