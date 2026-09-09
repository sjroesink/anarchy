using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Reborn
{
    // Presentation of resolved damage; never feeds back into combat calculations.
    public sealed class CombatNumbers
    {
        sealed class Entry { public Vector3 position;public string text;public bool incoming;public float age; }
        readonly List<Entry> entries=new List<Entry>();
        GUIStyle style;
        public int Count=>entries.Count;
        public void Miss(Vector3 position)
        {
            if(entries.Count==32)entries.RemoveAt(0);
            entries.Add(new Entry{position=position,text="Miss",incoming=false});
        }
        public void Add(Vector3 position,float amount,bool incoming)
        {
            if(amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
            if(entries.Count==32)entries.RemoveAt(0);
            entries.Add(new Entry{position=position,text=amount.ToString("0.##",CultureInfo.InvariantCulture),incoming=incoming});
        }
        public void Tick(float seconds)
        {
            for(int i=entries.Count-1;i>=0;i--){entries[i].age+=seconds;if(entries[i].age>=1.15f)entries.RemoveAt(i);}
        }
        public void Draw(Camera camera)
        {
            if(style==null)style=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            foreach(var entry in entries)
            {
                Vector3 world=entry.position+Vector3.up*(.3f+entry.age*.85f);
                Vector3 point=camera.WorldToScreenPoint(world);
                if(point.z<=0||Physics.Linecast(camera.transform.position,world,1<<0,QueryTriggerInteraction.Ignore))continue;
                float x=point.x*1600/Screen.width,y=(Screen.height-point.y)*900/Screen.height;
                if(x<0||x>1600||y<0||y>900)continue;
                float alpha=Mathf.Clamp01((1.15f-entry.age)/.35f);
                style.normal.textColor=new Color(0,0,0,alpha*.85f);
                GUI.Label(new Rect(x-49,y-17,100,36),entry.text,style);
                style.normal.textColor=entry.incoming?new Color(1,.18f,.14f,alpha):new Color(1,1,1,alpha);
                GUI.Label(new Rect(x-50,y-18,100,36),entry.text,style);
            }
        }
    }
}
