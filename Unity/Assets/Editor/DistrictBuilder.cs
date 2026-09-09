using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Reborn;

public static class DistrictBuilder
{
    static Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static RuntimeAnimatorController SoldierController()
    {
        const string modelPath="Assets/Art/Models/Soldier.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.isReadable=true;
        var clips=importer.defaultClipAnimations;
        foreach(var clip in clips){clip.loopTime=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        AnimationClip idle=null,walk=null,run=null,left=null,right=null,back=null;
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            if(asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {if(clip.name.Contains("Soldier_Idle"))idle=clip;if(clip.name.Contains("Soldier_Walk"))walk=clip;if(clip.name.Contains("Soldier_Run"))run=clip;if(clip.name.Contains("Soldier_StrafeLeft"))left=clip;if(clip.name.Contains("Soldier_StrafeRight"))right=clip;if(clip.name.Contains("Soldier_Backward"))back=clip;}
        if(!idle||!walk||!run||!left||!right||!back)throw new Exception("Missing Blender Soldier directional animation clips");
        const string path="Assets/Art/Soldier.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.parameters=new[]{new AnimatorControllerParameter{name="Locomotion",type=AnimatorControllerParameterType.Int}};
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states)machine.RemoveState(state.state);
        var resting=machine.AddState("Idle");resting.motion=idle;
        var walking=machine.AddState("Walk");walking.motion=walk;machine.defaultState=resting;
        var running=machine.AddState("Run");running.motion=run;
        var strafingLeft=machine.AddState("StrafeLeft");strafingLeft.motion=left;
        var strafingRight=machine.AddState("StrafeRight");strafingRight.motion=right;
        var retreating=machine.AddState("Backward");retreating.motion=back;
        var states=new[]{resting,walking,running,strafingLeft,strafingRight,retreating};
        for(int from=0;from<states.Length;from++)for(int to=0;to<states.Length;to++)
        {
            if(from==to)continue;
            var transition=states[from].AddTransition(states[to]);transition.hasExitTime=false;transition.duration=.12f;
            transition.AddCondition(AnimatorConditionMode.Equals,to,"Locomotion");
        }
        EditorUtility.SetDirty(controller);return controller;
    }
    static Material Mat(string name,Color color,float metal=0,float glow=0)
    {
        var path="Assets/Art/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.38f);
        if(glow>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*glow);}
        mats[name]=m;return m;
    }
    static GameObject Model(string name,Vector3 pos,Vector3 scale, float yaw=0)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/"+name+".fbx");
        if(!asset)throw new Exception("Missing Blender export: "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.name=name;go.transform.position=pos;go.transform.localScale=scale;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        foreach(var r in go.GetComponentsInChildren<Renderer>())
        {
            var mm=r.sharedMaterials;for(int i=0;i<mm.Length;i++)if(mm[i] && mats.TryGetValue(mm[i].name,out var mat))mm[i]=mat;r.sharedMaterials=mm;
        }
        return go;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 scale,string mat,bool collider=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mats[mat];
        if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;
    }
    static void Sign(string value,Vector3 position,float size,Color color,float yaw=180)
    {
        var go=new GameObject("Sign / "+value.Replace('\n',' '));go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=80;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
        // Physical signs must not show mirrored lettering through their reverse face.
        var renderer=go.GetComponent<MeshRenderer>();
        var signMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/WorldSign.mat");
        if(!signMaterial)
        {
            signMaterial=new Material(Shader.Find("Reborn/WorldSign"));
            signMaterial.mainTexture=renderer.sharedMaterial.mainTexture;
            AssetDatabase.CreateAsset(signMaterial,"Assets/Art/Materials/WorldSign.mat");
        }
        renderer.sharedMaterial=signMaterial;
    }
    [MenuItem("Reborn/Generate district and validate")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Art/Materials");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Prefabs");
        var soldierController=SoldierController();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Mat("Graphite",new Color(.08f,.12f,.16f),.7f);Mat("Steel",new Color(.26f,.34f,.4f),.65f);Mat("Concrete",new Color(.28f,.32f,.34f));
        Mat("Armor",new Color(.25f,.265f,.22f),.35f).SetFloat("_Glossiness",.30f);
        Mat("Fabric",new Color(.085f,.09f,.078f)).SetFloat("_Glossiness",.08f);
        SoldierMaterials.Apply(mats["Armor"],false);
        SoldierMaterials.Apply(mats["Fabric"],true);
        Mat("WeaponMetal",new Color(.042f,.038f,.034f),.65f).SetFloat("_Glossiness",.44f);
        Mat("WeaponEdge",new Color(.13f,.12f,.105f),.7f).SetFloat("_Glossiness",.44f);
        Mat("WeaponGrip",new Color(.105f,.073f,.039f)).SetFloat("_Glossiness",.36f);
        Mat("Dark",new Color(.018f,.031f,.045f),.3f);Mat("Cyan",new Color(.18f,.58f,.8f),.3f,.7f);Mat("Amber",new Color(1,.4f,.12f),.3f,.8f);Mat("Ivory",new Color(.66f,.73f,.75f),.5f);
        Mat("Paving",new Color(.16f,.21f,.23f),.25f);Mat("Water",new Color(.045f,.2f,.27f),.85f);Mat("Marking",new Color(.55f,.64f,.63f));
        Mat("Foliage",new Color(.16f,.22f,.10f)).SetFloat("_Glossiness",.12f);
        Mat("FoliageLight",new Color(.32f,.35f,.16f)).SetFloat("_Glossiness",.12f);
        Mat("Soil",new Color(.12f,.095f,.065f)).SetFloat("_Glossiness",.05f);
        foreach(string surface in new[]{"Paving","Concrete"})
        {
            mats[surface].shader=Shader.Find("Reborn/WeatheredSurface");
            mats[surface].SetFloat("_Weathering",surface=="Paving"?.65f:.42f);
            mats[surface].SetFloat("_Glossiness",.16f);
        }
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.29f,.41f,.49f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0055f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.42f,.57f,.7f);RenderSettings.ambientEquatorColor=new Color(.26f,.32f,.38f);RenderSettings.ambientGroundColor=new Color(.12f,.14f,.17f);
        var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sky.mat");if(!sky){sky=new Material(Shader.Find("Reborn/DistrictSky"));AssetDatabase.CreateAsset(sky,"Assets/Art/Materials/Sky.mat");}RenderSettings.skybox=sky;
        var sun=new GameObject("Late morning / directional light").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.86f,.7f);sun.intensity=1.65f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(38,-32,0);
        QualitySettings.shadowDistance=140;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.antiAliasing=4;
        Box("District foundation",new Vector3(0,-1.5f,40),new Vector3(330,2,350),"Dark");
        Box("Concourse",new Vector3(0,-.3f,20),new Vector3(29,.6f,125),"Paving");
        // Paving expansion joints and embedded wayfinding strips.
        for(int z=-40;z<83;z+=4)Box("Expansion seam",new Vector3(0,.006f,z),new Vector3(29,.01f,.05f),"Dark",false);
        for(int x=-12;x<=12;x+=4)Box("Paving seam",new Vector3(x,.008f,20),new Vector3(.035f,.01f,124),"Dark",false);
        foreach(int x in new[]{-14,14})
        {
            Box("Curb",new Vector3(x,.14f,20),new Vector3(.55f,.28f,125),"Steel");
            Box("Guide light",new Vector3(x-.32f*Mathf.Sign(x),.05f,20),new Vector3(.07f,.06f,123),"Cyan",false);
            Box("Canal",new Vector3(x*1.55f,-.38f,35),new Vector3(13,.08f,153),"Water",false);
            for(int z=-35;z<82;z+=6){Box("Railing post",new Vector3(x, .75f,z),new Vector3(.12f,1.3f,.12f),"Steel");}
            Box("Rail",new Vector3(x,1.36f,20),new Vector3(.12f,.12f,125),"Steel");
        }
        Box("South bulkhead",new Vector3(0,1.5f,-42),new Vector3(29,3,.5f),"Concrete");
        Box("North bulkhead",new Vector3(0,1.5f,82),new Vector3(29,3,.5f),"Concrete");
        for(int i=0;i<12;i++)
        {
            int side=i%2==0?-1:1;float z=-25+(i/2)*22;
            Model("Streetlight",new Vector3(side*12,0,z),Vector3.one,side<0?90:-90);
            Box("Service bay",new Vector3(side*10,.16f,z+5),new Vector3(3,.32f,5),"Concrete");
            for(int j=0;j<2;j++)Model("Cargo",new Vector3(side*10,.32f,z+4+j*1.6f),Vector3.one,20*j);
        }
        foreach(int side in new[]{-1,1})
        {
            foreach(int z in new[]{-7,24,55})
            {
                var planter=Model("GardenIsland",new Vector3(side*10,0,z),Vector3.one,90);
                var solid=planter.AddComponent<BoxCollider>();solid.center=new Vector3(0,.40f,0);solid.size=new Vector3(4.25f,.8f,1.65f);
            }
            foreach(int z in new[]{14,43})
            {
                var bench=Model("TransitBench",new Vector3(side*8,0,z),Vector3.one,side<0?90:-90);
                var solid=bench.AddComponent<BoxCollider>();solid.center=new Vector3(0,.5f,0);solid.size=new Vector3(2.75f,1.05f,.85f);
            }
        }
        var rng=new System.Random(122);
        for(int side=-1;side<=1;side+=2)
        for(int row=0;row<4;row++)
        for(int col=0;col<8;col++)
        {
            float x=side*(34+row*22+(float)rng.NextDouble()*5),z=-28+col*28+row*7;
            float h=1+(float)rng.NextDouble()*2.8f;
            if(row==0&&col%3==1)Model("UtilityHabitat",new Vector3(x,-1,z),new Vector3(1.35f,1.2f,1.35f),side<0?90:-90);
            else if(row==0)Model("Habitat",new Vector3(x,-1,z),new Vector3(1.8f,1.2f,1.8f),side<0?90:-90);
            else if(row==1&&col%3!=0)Model("UtilityHabitat",new Vector3(x,-1,z),new Vector3(1.8f,1.5f+col%2*.3f,1.8f),side<0?90:-90);
            else
            {
                // A low middle ring keeps the street habitats readable; taller
                // landmarks sit behind it, with different forms on each bank.
                int variant=(col+row+(side>0?1:0))%3;
                string model=variant==0?"Tower":variant==1?"SteppedTower":"DrumTower";
                float height=row==1?.45f+col%2*.2f:row==2?.85f+(col%3)*.22f:h*.78f;
                Model(model,new Vector3(x,-1,z),new Vector3(1+(float)rng.NextDouble()*.35f,height,1.2f),col%2*90);
            }
        }
        for(int i=0;i<9;i++)if(i!=4)Model(i%3==0?"SteppedTower":i%3==1?"Tower":"DrumTower",new Vector3(-90+i*23,-1,230),new Vector3(1.3f,.8f+(float)rng.NextDouble()*1.7f,1.5f));
        // Transit gate frames the central skyline.
        foreach(int x in new[]{-11,11})Box("Gate pylon",new Vector3(x,6,63),new Vector3(2,12,2),"Graphite");
        Box("Transit gantry",new Vector3(0,12,63),new Vector3(24,2,2),"Steel");
        Box("Gantry inset",new Vector3(0,12,61.94f),new Vector3(18,1.35f,.08f),"Dark",false);
        Sign("N O R T H   /   T R A N S I T",new Vector3(0,12,61.86f),.15f,new Color(.4f,.85f,1),0);
        Box("Hologram frame",new Vector3(-25,22,42),new Vector3(7,15,1),"Graphite",false);
        Box("Hologram",new Vector3(-25,22,41.4f),new Vector3(6.4f,14,.12f),"Dark",false);
        Sign("ICC\n\nN E U T R A L\nT E R R I T O R Y",new Vector3(-25,23,41.2f),.13f,new Color(.2f,.8f,1),0);
        Box("Sector marker",new Vector3(11,2,0),new Vector3(1.8f,4,.35f),"Graphite");
        Sign("07",new Vector3(11,2,-.2f),.22f,new Color(.9f,.5f,.2f),0);
        Sign("BOREALIS\nTRANSIT AUTHORITY",new Vector3(0,3,80),.22f,new Color(.5f,.8f,.9f),0);
        var terminal=Model("Terminal",new Vector3(-4,0,-8),Vector3.one*1.2f,180);
        var terminalBody=terminal.AddComponent<BoxCollider>();
        terminalBody.center=new Vector3(0,1.48f,0);terminalBody.size=new Vector3(.86f,1.35f,.61f);
        var terminalBase=terminal.AddComponent<BoxCollider>();
        terminalBase.center=new Vector3(0,.40f,0);terminalBase.size=new Vector3(.50f,.80f,.62f);
        var terminalSign=terminal.AddComponent<BoxCollider>();
        terminalSign.center=new Vector3(0,2.48f,0);terminalSign.size=new Vector3(.39f,.47f,.18f);
        Model("Whompah",new Vector3(-9,0,1),Vector3.one,180);
        Model("Whompah",new Vector3(9,0,1),Vector3.one,180);
        Sign("NEWLAND CITY",new Vector3(-9,4.5f,-.02f),.036f,new Color(.7f,.87f,.9f),0);
        Sign("STRET WEST BANK",new Vector3(9,4.5f,-.02f),.032f,new Color(.7f,.87f,.9f),0);
        Model("GridTerminal",new Vector3(5,0,-8),Vector3.one,180);
        Model("SubspaceDish",new Vector3(-58,0,67),Vector3.one*2.4f,0);
        Box("Terminal pad",new Vector3(-4,.015f,-8),new Vector3(3,.025f,3),"Steel",false);
        var soldier=Model("Soldier",Vector3.zero,Vector3.one,180);
        var player=new GameObject("Player / Soldier");player.layer=2;player.transform.position=new Vector3(0,.05f,-15);
        soldier.transform.SetParent(player.transform,false);soldier.transform.localRotation=Quaternion.identity;
        var animator=soldier.GetComponent<Animator>();if(!animator)animator=soldier.AddComponent<Animator>();
        animator.runtimeAnimatorController=soldierController;animator.applyRootMotion=false;
        player.AddComponent<SoldierLocomotion>().animator=animator;
        foreach(var t in soldier.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
        var controller=player.AddComponent<CharacterController>();controller.height=2;controller.radius=.32f;controller.center=new Vector3(0,1,0);controller.stepOffset=.3f;controller.minMoveDistance=0;
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=57;camera.nearClipPlane=.15f;camera.farClipPlane=700;camera.allowHDR=true;camera.gameObject.AddComponent<AudioListener>();
        camera.transform.position=new Vector3(0,4,-23);camera.transform.LookAt(player.transform.position+Vector3.up*1.8f);
        var drone=Model("Drone",Vector3.zero,Vector3.one,0);var droneAsset=PrefabUtility.SaveAsPrefabAsset(drone,"Assets/Prefabs/Drone.prefab");UnityEngine.Object.DestroyImmediate(drone);
        var shuttle=Model("Shuttle",new Vector3(15,38,85),Vector3.one*2.4f,90);
        var root=new GameObject("District / Game systems");var game=root.AddComponent<DistrictGame>();game.player=player.transform;game.visual=soldier.transform;game.terminal=terminal.transform;game.view=camera;game.dronePrefab=droneAsset;game.traffic=shuttle.transform;root.AddComponent<DistrictHud>();root.AddComponent<TargetCursor>();
        PlayerSettings.productName="Anarchy Reborn - District Prototype";PlayerSettings.companyName="RebornPrototype";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Borealis.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Borealis.unity",true)};
        AssetDatabase.SaveAssets();Validate();Debug.Log("DISTRICT_GENERATION_OK");
    }
    public static void Validate()
    {
        int assertions=0;
        Action<bool,string> check=(value,msg)=>{assertions++;if(!value)throw new Exception("VALIDATION FAILED: "+msg);};
        AoValidation.Run();
        AoSkillLockValidation.Run();
        AoInventoryValidation.Run();
        AoEffectsValidation.Run();
        AoDamageValidation.Run();
        AoInitiativeValidation.Run();
        AoOeValidation.Run();
        foreach(string name in new[]{"Tower","SteppedTower","DrumTower","Terminal","Cargo","Drone","Soldier","Streetlight","Shuttle","Whompah","GridTerminal","SubspaceDish","Habitat","UtilityHabitat","GardenIsland","TransitBench"})check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/"+name+".fbx")!=null,"asset "+name);
        var soldierAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Soldier.fbx");
        var skins=soldierAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        AnimationClip walkClip=null;
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Soldier.fbx"))
            if(asset is AnimationClip clip&&clip.name.Contains("Soldier_Walk")&&!clip.name.StartsWith("__preview__"))walkClip=clip;
        check(walkClip!=null,"Blender walk clip imported");
        var poseObject=UnityEngine.Object.Instantiate(soldierAsset);
        try
        {
            var thigh=Array.Find(poseObject.GetComponentsInChildren<Transform>(),t=>t.name=="Rig_ThighR");
            walkClip.SampleAnimation(poseObject,walkClip.length*.25f);var first=thigh.localRotation;
            walkClip.SampleAnimation(poseObject,walkClip.length*.75f);
            check(Quaternion.Angle(first,thigh.localRotation)>20,"imported walk clip moves thigh between opposing phases");
            foreach(string direction in new[]{"StrafeLeft","StrafeRight","Backward"})
            {
                AnimationClip directional=null;
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Soldier.fbx"))
                    if(asset is AnimationClip clip&&clip.name.Contains("Soldier_"+direction)&&!clip.name.StartsWith("__preview__"))directional=clip;
                check(directional!=null,"Blender directional clip imported: "+direction);
                directional.SampleAnimation(poseObject,directional.length*.25f);var phase=thigh.localRotation;
                directional.SampleAnimation(poseObject,directional.length*.75f);
                check(Quaternion.Angle(phase,thigh.localRotation)>15,"directional clip moves thigh: "+direction);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(poseObject);}
        check(skins.Length>0,"Soldier imports as skinned geometry");
        foreach(var skin in skins)
        {
            check(skin.sharedMesh!=null&&skin.sharedMesh.vertexCount>0,"Soldier skin mesh "+skin.name);
            var uv=skin.sharedMesh.uv;
            check(uv.Length==skin.sharedMesh.vertexCount&&Array.TrueForAll(uv,p=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)),"Soldier surface UVs "+skin.name);
            check(skin.bones.Length>0&&Array.TrueForAll(skin.bones,b=>b!=null),"Soldier bone bindings "+skin.name);
        }
        Directory.CreateDirectory("../Artifacts");File.WriteAllText("../Artifacts/validation.txt",$"PASS: {assertions} checks\nBlender asset import checks. AO rule tests are in ao-reference-validation.txt.\n");
        Debug.Log($"VALIDATION_OK: {assertions} checks");
    }
    public static void BuildWindows()
    {
        Generate();
        string output="../Builds/AnarchyReborn.exe";
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-buildOutput");
        if(index>=0){if(index+1>=args.Length)throw new ArgumentException("Missing build output");output=args[index+1];}
        output=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Borealis.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("WINDOWS_BUILD_OK");
    }
}
