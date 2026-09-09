using UnityEngine;

namespace Reborn
{
    // Authored cursor art; red targeting feedback follows the Funcom game guide.
    public sealed class TargetCursor : MonoBehaviour
    {
        DistrictGame game;
        Texture2D reticle;
        bool showing;
        void Awake()
        {
            game=GetComponent<DistrictGame>();
            reticle=new Texture2D(32,32,TextureFormat.RGBA32,false);
            reticle.name="Target reticle";reticle.filterMode=FilterMode.Point;
            var pixels=new Color32[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float dx=x-15.5f,dy=y-15.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                bool ring=r>=7.5f&&r<=9.5f;
                bool tick=(Mathf.Abs(dx)<=.6f&&Mathf.Abs(dy)>=10&&Mathf.Abs(dy)<=14)
                    ||(Mathf.Abs(dy)<=.6f&&Mathf.Abs(dx)>=10&&Mathf.Abs(dx)<=14);
                bool edge=(r>=6.5f&&r<=10.5f)
                    ||(Mathf.Abs(dx)<=1.6f&&Mathf.Abs(dy)>=9&&Mathf.Abs(dy)<=15)
                    ||(Mathf.Abs(dy)<=1.6f&&Mathf.Abs(dx)>=9&&Mathf.Abs(dx)<=15);
                pixels[y*32+x]=ring||tick?new Color32(255,58,48,255):edge?new Color32(12,16,20,240):new Color32(0,0,0,0);
            }
            reticle.SetPixels32(pixels);reticle.Apply();
        }
        void LateUpdate()
        {
            bool active=Application.isFocused&&!Input.GetMouseButton(1)&&game&&game.PointerTargetAtScreen(Input.mousePosition);
            if(active==showing)return;
            showing=active;Cursor.SetCursor(active?reticle:null,new Vector2(16,16),CursorMode.Auto);
        }
        void OnDisable(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);showing=false;}
        void OnApplicationFocus(bool focused){if(!focused)OnDisable();}
        void OnDestroy(){if(reticle)Destroy(reticle);}
    }
}
