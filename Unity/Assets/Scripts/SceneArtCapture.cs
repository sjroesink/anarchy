using System.IO;
using UnityEngine;

namespace Reborn
{
    // Scene-only review: independent of the desktop size, intentionally excludes IMGUI.
    public static class SceneArtCapture
    {
        public static void Save(Camera camera,string path)
        {
            var previousTarget=camera.targetTexture;
            var previousActive=RenderTexture.active;
            float previousAspect=camera.aspect;
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.aspect=1600f/900;
                camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path,pixels.EncodeToPNG());
                Debug.Log("SCENE_ART_CAPTURE: "+path+" 1600x900, scene only; HUD excluded");
            }
            finally
            {
                camera.targetTexture=previousTarget;camera.aspect=previousAspect;
                RenderTexture.active=previousActive;
                RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);
            }
        }
    }
}
