using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Reborn;

public static class BorealisLayoutBuilder
{
    [Serializable] public class Placement
    {
        public uint identity;
        public int templateId;
        public string templateName;
        public float[] positionRaw,headingRaw;
    }
    [Serializable] public class Layout
    {
        public int playfieldId,count;
        public string clientVersion;
        public Placement[] placements;
    }
    [Serializable] public class TileEntry {public int index;public string name;}
    [Serializable] public class Tiles {public TileEntry[] catalog;}
    static void ApplySurveySurfaces(TerrainData terrain)
    {
        var tiles=JsonUtility.FromJson<Tiles>(File.ReadAllText("../Research/Borealis/ClientLayout/tile-validation.json"));
        var indices=File.ReadAllBytes("../Research/Borealis/ClientLayout/tile-indices-256.raw");
        if(indices.Length!=256*256)throw new Exception("Incomplete tile map");
        var names=tiles.catalog.Select(t=>t.name).Distinct().Concat(new[]{"BuildingMask"}).ToArray();
        var buildingMask=File.ReadAllBytes("../Research/Borealis/ClientLayout/building-mask-512.raw");
        if(buildingMask.Length!=512*512)throw new Exception("Incomplete building mask");
        Color[] palette={new Color(.32f,.43f,.23f),new Color(.17f,.28f,.16f),new Color(.49f,.47f,.42f),new Color(.40f,.34f,.27f),new Color(.28f,.32f,.20f),new Color(.25f,.43f,.47f),new Color(.64f,.63f,.59f),new Color(.42f,.46f,.48f),new Color(.39f,.38f,.35f),new Color(.72f,.71f,.66f),new Color(.28f,.30f,.31f),new Color(.08f,.10f,.12f)};
        if(names.Length!=palette.Length)throw new Exception("Survey palette needs review");
        const string folder="Assets/Scenes/BorealisSurveyMaterials";Directory.CreateDirectory(folder);
        var layers=new TerrainLayer[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            string texturePath=folder+"/"+names[i]+".asset",layerPath=folder+"/"+names[i]+".terrainlayer";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture){texture=new Texture2D(2,2);AssetDatabase.CreateAsset(texture,texturePath);}
            texture.SetPixels(new[]{palette[i],palette[i],palette[i],palette[i]});texture.Apply();EditorUtility.SetDirty(texture);
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if(!layer){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,layerPath);}
            layer.diffuseTexture=texture;layer.tileSize=new Vector2(4,4);layer.smoothness=0;EditorUtility.SetDirty(layer);layers[i]=layer;
        }
        terrain.terrainLayers=layers;terrain.alphamapResolution=512;
        var weights=new float[512,512,names.Length];
        for(int z=0;z<512;z++)for(int x=0;x<512;x++)
        {
            int index=indices[(z/2)*256+x/2];
            if(index>=tiles.catalog.Length||tiles.catalog[index].index!=index)throw new Exception("Invalid tile reference");
            if(buildingMask[z*512+x]>1)throw new Exception("Non-binary building mask");
            weights[z,x,buildingMask[z*512+x]==1?names.Length-1:Array.IndexOf(names,tiles.catalog[index].name)]=1;
        }
        terrain.SetAlphamaps(0,0,weights);
    }
    [MenuItem("Reborn/Generate original Borealis placement study")]
    public static void Generate()
    {
        var data=JsonUtility.FromJson<Layout>(File.ReadAllText("../Research/Borealis/ClientLayout/placements.json"));
        if(data.playfieldId!=800||data.placements==null||data.count!=data.placements.Length)throw new Exception("Invalid Borealis survey");
        var grid=Array.Find(data.placements,p=>p.templateId==95350);
        if(grid==null)throw new Exception("Grid origin missing");
        Vector3 origin=Position(grid);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Borealis source placements / 18.8.50");
        var heightBytes=File.ReadAllBytes("../Research/Borealis/ClientLayout/heights-257.raw");
        if(heightBytes.Length!=257*257)throw new Exception("Invalid reconstructed height field");
        const string terrainPath="Assets/Scenes/BorealisSourceTerrain.asset";
        var terrainData=AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
        if(!terrainData){terrainData=new TerrainData();AssetDatabase.CreateAsset(terrainData,terrainPath);}
        terrainData.heightmapResolution=257;terrainData.size=new Vector3(1024,102,1024);
        var heights=new float[257,257];
        for(int z=0;z<257;z++)for(int x=0;x<257;x++)heights[z,x]=heightBytes[z*257+x]/255f;
        terrainData.SetHeights(0,0,heights);EditorUtility.SetDirty(terrainData);
        ApplySurveySurfaces(terrainData);
        var importedHeights=terrainData.GetHeights(0,0,257,257);
        for(int z=0;z<257;z++)for(int x=0;x<257;x++)
            if(Mathf.Abs(importedHeights[z,x]-heights[z,x])>1f/65535)throw new Exception("Terrain sample changed during import");
        var terrainObject=Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name="Reconstructed source heights / outer extent under review";
        terrainObject.transform.position=-origin;
        Bounds bounds=new Bounds(Vector3.zero,Vector3.zero);
        int checks=0;
        foreach(var entry in data.placements)
        {
            if(entry.headingRaw==null||entry.headingRaw.Length!=4)throw new Exception("Invalid heading");
            var node=new GameObject(entry.templateId+" / "+entry.templateName+" / "+entry.identity);
            node.transform.SetParent(root.transform);node.transform.position=Position(entry)-origin;
            bounds.Encapsulate(node.transform.position);
            var source=node.AddComponent<SourcePlacement>();
            source.clientVersion=data.clientVersion;source.sourceIdentity=entry.identity.ToString();source.templateId=entry.templateId;source.templateName=entry.templateName;
            source.sourcePosition=Position(entry);source.coordinateOrigin=origin;
            source.rawHeadingWZYX=new Vector4(entry.headingRaw[0],entry.headingRaw[1],entry.headingRaw[2],entry.headingRaw[3]);
            // Known-purpose Blender stand-ins only. Unknown and door anchors stay
            // empty; they must not be confused with inferred building geometry.
            string model=entry.templateId==95350?"GridTerminal":entry.templateId==85302||entry.templateId==85303?"Terminal":null;
            if(model!=null)
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/"+model+".fbx");
                if(!asset)throw new Exception("Missing authored model "+model);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset);
                visual.name="Authored visual / orientation unverified";visual.transform.SetParent(node.transform,false);
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        var replacement=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/"+materials[i].name+".mat");
                        if(replacement)materials[i]=replacement;
                    }
                    renderer.sharedMaterials=materials;
                }
            }
            if(Vector3.Distance(node.transform.position+origin,Position(entry))>.0001f)throw new Exception("Coordinate roundtrip failed");
            checks++;
        }
        var camera=new GameObject("Survey camera").AddComponent<Camera>();camera.orthographic=true;
        camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.6f+10;
        camera.transform.position=bounds.center+Vector3.up*500;camera.transform.rotation=Quaternion.Euler(90,0,0);camera.farClipPlane=1000;
        var light=new GameObject("Survey light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/BorealisReconstruction.unity");
        AssetDatabase.SaveAssets();
        File.WriteAllText("../Artifacts/borealis-placement-validation.txt",$"PASS: {checks} source coordinate roundtrips. Original grid origin: {origin}. Reconstructed byte heights imported; no roads, building footprints or rotation validation.\n");
        Debug.Log("BOREALIS_PLACEMENT_STUDY_OK: "+checks);
    }
    static Vector3 Position(Placement p)
    {
        if(p.positionRaw==null||p.positionRaw.Length!=3||Array.Exists(p.positionRaw,v=>float.IsNaN(v)||float.IsInfinity(v)))throw new Exception("Invalid source position");
        return new Vector3(p.positionRaw[0],p.positionRaw[1],p.positionRaw[2]);
    }
    public static void Capture()
    {
        Generate();
        var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
        var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
        Vector3 centre=terrain.transform.position+new Vector3(512,40,512);
        camera.orthographicSize=600;camera.farClipPlane=2500;
        camera.transform.position=centre+new Vector3(550,750,-550);camera.transform.LookAt(centre);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.21f);
        RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
        SceneArtCapture.Save(camera,Path.GetFullPath("../Artifacts/SceneArt/borealis-source-terrain.png"));
        camera.orthographicSize=185;
        camera.transform.position=terrain.transform.position+new Vector3(675,450,600);
        camera.transform.rotation=Quaternion.Euler(90,0,0);
        SceneArtCapture.Save(camera,Path.GetFullPath("../Artifacts/SceneArt/borealis-city-mask.png"));
    }
}
