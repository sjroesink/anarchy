using System;

namespace Reborn
{
    [Serializable] public sealed class AoHotbar
    {
        public int layer;
        public bool visible=true;
        public int[] slots=Defaults();
        public string[] items=new string[100];
        static int[] Defaults(){var result=new int[100];for(int i=0;i<7;i++)result[i]=-i-1;return result;}
        public bool Valid()=>layer>=0&&layer<10&&slots!=null&&slots.Length==100&&Array.TrueForAll(slots,id=>id>=-7)&&items!=null&&items.Length==100&&Array.TrueForAll(items,id=>id==null||id.Length<=128);
        public int Get(int slot)=>slot>=0&&slot<10?slots[layer*10+slot]:0;
        public string Item(int slot)=>slot>=0&&slot<10?items[layer*10+slot]:null;
        public void Assign(int slot,int action){if(slot>=0&&slot<10&&action>=-7){slots[layer*10+slot]=action;items[layer*10+slot]=null;}}
        public void AssignItem(int slot,string id){if(slot>=0&&slot<10&&id!=null&&id.Length<=128){slots[layer*10+slot]=0;items[layer*10+slot]=id;}}
        public bool Swap(int from,int to,int expected,string expectedItem=null)
        {
            if(from<0||from>=100||to<0||to>=100||(expected==0&&string.IsNullOrEmpty(expectedItem))||slots[from]!=expected||items[from]!=expectedItem)return false;
            int other=slots[to];slots[to]=slots[from];slots[from]=other;
            string otherItem=items[to];items[to]=items[from];items[from]=otherItem;return true;
        }
    }
}
