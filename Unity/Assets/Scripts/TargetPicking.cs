using System.Collections.Generic;
using UnityEngine;

namespace Reborn
{
    public static class TargetPicking
    {
        public static DroneEnemy Cycle(IEnumerable<DroneEnemy> enemies,DroneEnemy current,Vector3 player,float range,bool backwards)
        {
            var live=new List<DroneEnemy>();
            foreach(var enemy in enemies)
                if(enemy&&!enemy.dead&&enemy.gameObject.activeInHierarchy&&(enemy.transform.position-player).sqrMagnitude<range*range)live.Add(enemy);
            // Nearest first; retain input order for equal distances.
            for(int i=1;i<live.Count;i++)
            {
                var candidate=live[i];float distance=(candidate.transform.position-player).sqrMagnitude;int j=i-1;
                while(j>=0&&(live[j].transform.position-player).sqrMagnitude>distance){live[j+1]=live[j];j--;}
                live[j+1]=candidate;
            }
            if(live.Count==0)return null;
            int at=live.IndexOf(current);
            if(at<0)return live[0];
            return live[(at+(backwards?-1:1)+live.Count)%live.Count];
        }
        // Visual selection bounds only; these do not change weapon hit/damage rules.
        public static DroneEnemy Pick(IEnumerable<DroneEnemy> enemies,Ray ray,Vector3 player,float range)
        {
            DroneEnemy selected=null;float nearest=float.PositiveInfinity;
            foreach(var enemy in enemies)
            {
                if(!enemy||enemy.dead||!enemy.gameObject.activeInHierarchy||Vector3.Distance(player,enemy.transform.position)>=range)continue;
                foreach(var renderer in enemy.GetComponentsInChildren<Renderer>())
                {
                    if(!renderer.enabled||!renderer.bounds.IntersectRay(ray,out float distance)||distance>=nearest)continue;
                    if(Physics.Raycast(ray,out var obstacle,distance,1<<0,QueryTriggerInteraction.Ignore)
                        &&obstacle.collider.GetComponentInParent<DroneEnemy>()!=enemy)continue;
                    selected=enemy;nearest=distance;
                }
            }
            return selected;
        }
    }
}
