using UnityEngine;

namespace Reborn
{
    public sealed class HotbarDrag
    {
        public int source=-1,action;
        public string item;
        public bool moved;
        Vector2 origin;
        public bool Active=>source>=0;
        public void Begin(AoHotbar bar,int slot,Vector2 point)
        {if(slot<0||slot>=10){Cancel();return;}source=bar.layer*10+slot;action=bar.Get(slot);item=bar.Item(slot);origin=point;moved=false;}
        public void Move(Vector2 point){if(Active&&(point-origin).sqrMagnitude>64)moved=true;}
        // Returns a click slot only for a stationary release on the original slot/layer.
        public int Release(AoHotbar bar,int slot)
        {
            int click=-1;
            if(Active&&slot>=0&&slot<10)
            {
                int destination=bar.layer*10+slot;
                if(moved)bar.Swap(source,destination,action,item);
                else if(destination==source&&bar.slots[source]==action&&bar.items[source]==item)click=slot;
            }
            Cancel();return click;
        }
        public void Cancel(){source=-1;action=0;item=null;moved=false;}
    }
}
