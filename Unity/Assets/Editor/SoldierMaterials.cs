using System.IO;
using UnityEditor;
using UnityEngine;

// Authored prototype surface detail; does not represent a particular AO armor item.
public static class SoldierMaterials
{
    public static void Apply(Material material,bool fabric)
    {
        const int size=256;
        string path="Assets/Art/Materials/"+(fabric?"FabricWeave":"ArmorFinish")+".png";
        var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            float mottling=Mathf.PerlinNoise(u*9+13.7f,v*9+42.1f);
            float grain=Mathf.PerlinNoise(u*83+4.3f,v*83+7.9f);
            float value;
            if(fabric)
            {
                float weave=((x/2+y/2)%2==0?.93f:.72f);
                value=weave*(.88f+.12f*grain);
            }
            else value=.73f+.20f*mottling+.07f*grain;
            pixels[y*size+x]=new Color(value,value,value,1);
        }
        texture.SetPixels(pixels);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;
        importer.anisoLevel=4;importer.SaveAndReimport();
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        material.mainTextureScale=Vector2.one*(fabric?6:2);
        EditorUtility.SetDirty(material);
    }
}
