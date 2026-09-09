using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Reborn
{
    public sealed class DistrictGame : MonoBehaviour
    {
        public static DistrictGame Instance;
        public Transform player, terminal, visual;
        public Camera view;
        public GameObject dronePrefab;
        public Transform traffic;
        public CharacterBuild Build = new CharacterBuild();
        public float Health = 240, Nano = 100;
        public bool Paused, characterOpen, missionOpen;
        public bool Walking;
        public bool TextEntryFocused;
        public int difficulty = 1, seed = 7319, kills, required;
        public bool missionActive, missionComplete, autoAttack;
        public DroneEnemy target;
        public DroneEnemy inspectedTarget;
        public readonly List<DroneEnemy> enemies = new List<DroneEnemy>();
        public readonly List<string> messages = new List<string>();
        public readonly AoSkillLocks SpecialLocks = new AoSkillLocks();
        public readonly CombatNumbers DamageNumbers=new CombatNumbers();
        public float burstCooldown => SpecialLocks.Remaining(148);
        public float BurstLockFraction => SpecialLocks.Duration(148)>0 ? Mathf.Clamp01(burstCooldown/SpecialLocks.Duration(148)) : 0;
        public float healCooldown;
        public float aggDef { get=>Build.aggression;set=>Build.aggression=Mathf.Clamp01(value); }
        public float buffTime=>Build.Effects.Remaining(26370);
        public float reflectTime=>Build.Effects.Remaining(70308);
        public float castRemaining,nanoRecharge;
        int pendingNano;
        Vector3 castOrigin;
        float castDuration;
        public bool Casting=>pendingNano!=0;
        public int CastingNanoId=>pendingNano;
        public string CastingName=>AoSelfEffects.Get(pendingNano)?.name??"";
        public float CastProgress=>castDuration>0?Mathf.Clamp01(1-castRemaining/castDuration):0;
        string pendingEquip;
        int pendingEquipSlot;
        public float equipRemaining;
        public bool Equipping=>pendingEquip!=null;
        public string PendingEquipmentId=>pendingEquip;
        public bool RemovingEquipment=>Equipping&&pendingEquipSlot==0;
        public float EquipmentProgress=>!Equipping?0:Mathf.Clamp01(1-equipRemaining/Mathf.Max(.001f,equipDuration));
        float equipDuration;
        public bool Buff => buffTime > 0;
        public bool Reflect => reflectTime > 0;
        public float TerminalDistance => Vector3.Distance(player.position, terminal.position);
        public string SavePath => Path.Combine(Application.persistentDataPath, "soldier-v3.json");
        CharacterController motor;
        float yaw, pitch = 3, distance = 7, vertical, weaponTimer;
        float weaponCycleDuration;
        bool weaponPreparing;
        DroneEnemy preparingTarget;
        public string WeaponPhase=>weaponPreparing?"Attack":"Recharge";
        public bool RangedMoving=>motor&&motor.velocity.sqrMagnitude>.01f;
        public float WeaponCycleRemaining=>weaponCycleDuration>0?Mathf.Max(0,weaponTimer):0;
        public float WeaponCycleProgress=>weaponCycleDuration>0?Mathf.Clamp01(weaponPreparing?1-weaponTimer/weaponCycleDuration:weaponTimer/weaponCycleDuration):0;
        Material beamMaterial;
        Transform ring;
        bool photo;
        bool qaArtCamera;
        bool orbitDragging;
        bool terminalPress;
        Vector2 terminalPressPoint;
        bool cameraInitialized;Vector3 cameraFocus,cameraFocusVelocity;float cameraDistance=7;
        public bool FirstPerson { get; private set; }
        public void Log(string text) { messages.Add(text); if (messages.Count > 5) messages.RemoveAt(0); }
        void Awake()
        {
            Instance = this; motor = player.GetComponent<CharacterController>();
            beamMaterial = new Material(Shader.Find("Sprites/Default"));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-server")>=0)
                gameObject.AddComponent<ServerTrainingClient>();
            else Load();
            Health=Build.MaxHealth;Nano=Build.MaxNano;
            RefreshWeaponVisual();
            gameObject.AddComponent<SoldierCombatPose>().game=this;
            Log("Uplink online. Find the mission terminal [E].");
            ring = new GameObject("Target reticle").transform;
            var line = ring.gameObject.AddComponent<LineRenderer>(); line.sharedMaterial = beamMaterial;
            line.startColor = line.endColor = new Color(1,.45f,.16f); line.startWidth = line.endWidth = .055f;
            line.loop = true; line.positionCount = 64; line.useWorldSpace = false;
            for (int i=0;i<64;i++) { float a=i*Mathf.PI/32; line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.1f,0,Mathf.Sin(a)*1.1f)); }
            ring.gameObject.SetActive(false);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-qaCapture") >= 0) StartCoroutine(CaptureRun());
        }
        string QaArtifactDirectory
        {
            get
            {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-captureDirectory");
                string path=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.dataPath,"../../Artifacts");
                path=Path.GetFullPath(path);Directory.CreateDirectory(path);return path;
            }
        }
        IEnumerator CaptureRun()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            Screen.SetResolution(1600,900,FullScreenMode.Windowed);
            Build = new CharacterBuild();
            // Isolated QA fixture; live characters must upload their crystals.
            foreach(var definition in AoSelfEffects.Catalog.effects)Build.learnedNanos.Add(definition.id);
            aggDef=.65f;
            Health=Build.MaxHealth;Nano=Build.MaxNano;RefreshWeaponVisual();
            yield return new WaitForSeconds(3);
            Debug.Log($"QA_CAPTURE_RESOLUTION: {Screen.width}x{Screen.height}");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qaSceneArt")>=0)
            {
                string artDirectory=Path.Combine(QaArtifactDirectory,"SceneArt");
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"district.png"));
                view.transform.position=terminal.position+new Vector3(1.3f,1.9f,-4.4f);
                view.transform.LookAt(terminal.position+Vector3.up*1.65f);
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"mission-terminal.png"));
                var grid=GameObject.Find("GridTerminal").transform;
                view.transform.position=grid.position+new Vector3(1.2f,1.7f,-4.2f);
                view.transform.LookAt(grid.position+new Vector3(-.35f,1.4f,0));
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"grid-terminal.png"));
                AcceptMission();target=enemies[0];autoAttack=true;weaponTimer=100;
                motor.enabled=false;player.position=new Vector3(0,.1f,7);motor.enabled=true;
                yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();
                qaArtCamera=true;view.transform.position=player.position+visual.rotation*new Vector3(2.4f,1.75f,2.7f);
                view.transform.LookAt(player.position+Vector3.up*1.2f);
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"soldier.png"));
                view.transform.position=player.position+visual.rotation*new Vector3(.85f,1.45f,1.2f);
                view.transform.LookAt(player.position+visual.rotation*new Vector3(.10f,1.28f,.28f));
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"weapon-grip.png"));
                view.transform.position=player.position+visual.rotation*new Vector3(-.85f,1.45f,1.2f);
                view.transform.LookAt(player.position+visual.rotation*new Vector3(.10f,1.28f,.28f));
                SceneArtCapture.Save(view,Path.Combine(artDirectory,"trigger-grip.png"));
                Debug.Log("SUPPORT_WRIST_DISTANCE: "+GetComponent<SoldierCombatPose>().SupportWristDistance());
                Application.Quit();yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qaPose")>=0)
            {
                AcceptMission();target=enemies[0];autoAttack=true;weaponTimer=100;
                motor.enabled=false;player.position=new Vector3(0,.1f,7);motor.enabled=true;
                yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();
                var combatPose=GetComponent<SoldierCombatPose>();
                Debug.Log("POSE_MUZZLE_DISTANCE: "+combatPose.MuzzleSurfaceDistance());
                qaArtCamera=true;view.transform.position=player.position+visual.rotation*new Vector3(2.4f,1.75f,2.7f);
                view.transform.LookAt(player.position+Vector3.up*1.2f);
                yield return new WaitForSeconds(.2f);
                ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"combat-stance.png"));
                yield return new WaitForSeconds(.3f);Application.Quit();yield break;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"district-gameplay.png"));
            yield return new WaitForSeconds(2);
            characterOpen = true;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"character-planner.png"));
            yield return new WaitForSeconds(2);
            GetComponent<DistrictHud>().ShowCharacterTab(2);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"item-database.png"));
            yield return new WaitForSeconds(1);
            GetComponent<DistrictHud>().ShowCharacterTab(3);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"nano-library.png"));
            yield return new WaitForSeconds(1);
            GetComponent<DistrictHud>().ShowNanoDetails(287046);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"nano-library-details.png"));
            yield return new WaitForSeconds(.2f);
            characterOpen = false;
            int checks=0;
            Action<bool,string> check=(ok,msg)=>{checks++;if(!ok)throw new Exception("RUNTIME CHECK FAILED: "+msg);};
            var soldierAnimator=visual.GetComponent<Animator>();
            // Capture fixtures also run in hidden windows; evaluate bone transforms
            // even when renderer visibility would otherwise cull animation updates.
            soldierAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            soldierAnimator.enabled=false;
            var forearm=Array.Find(visual.GetComponentsInChildren<Transform>(),t=>t.name=="Rig_ForearmR");
            var handSkin=Array.Find(visual.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.name=="Hand FabricR");
            check(forearm!=null&&handSkin!=null,"Blender forearm and skinned hand imported");
            var poseMesh=new Mesh();handSkin.BakeMesh(poseMesh);var handBefore=poseMesh.bounds.center;
            var restForearm=forearm.localRotation;forearm.localRotation=restForearm*Quaternion.Euler(-65,0,0);
            yield return null;handSkin.BakeMesh(poseMesh);
            check(Vector3.Distance(handBefore,poseMesh.bounds.center)>.03f,"rotating imported forearm deforms the hand mesh");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"soldier-rig-runtime.png"));
            yield return new WaitForSeconds(.2f);forearm.localRotation=restForearm;
            yield return null;handSkin.BakeMesh(poseMesh);
            check(Vector3.Distance(handBefore,poseMesh.bounds.center)<.001f,"restoring imported bone restores the rest mesh");
            Destroy(poseMesh);
            soldierAnimator.enabled=true;
            var movementOrigin=player.position;
            var thighBone=Array.Find(visual.GetComponentsInChildren<Transform>(),t=>t.name=="Rig_ThighR");
            var restThigh=thighBone.localRotation;bool capturedWalk=false;
            // Observe a full 32-frame / 24-fps walk cycle plus its transition.
            float moveUntil=Time.time+1.8f;float maximumWalkAngle=0;int walkFrames=0;
            while(Time.time<moveUntil)
            {
                motor.Move(Vector3.forward*2*Time.deltaTime);yield return null;
                walkFrames++;maximumWalkAngle=Mathf.Max(maximumWalkAngle,Quaternion.Angle(restThigh,thighBone.localRotation));
                if(!capturedWalk&&Quaternion.Angle(restThigh,thighBone.localRotation)>15)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"soldier-walk-runtime.png"));
                    capturedWalk=true;
                }
            }
            Debug.Log($"WALK_QA frames={walkFrames} maxAngle={maximumWalkAngle} mode={soldierAnimator.GetInteger("Locomotion")} walkState={soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Walk")} distance={Vector3.Distance(movementOrigin,player.position)}");
            check(capturedWalk,"runtime walk animation rotates the thigh through a visible stride");
            check(Vector3.Distance(movementOrigin,player.position)>3,"walk fixture covers distance instead of losing small per-frame movements");
            check(soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"actual movement selects Blender walk clip");
            yield return new WaitForSeconds(.35f);
            check(soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"stopping movement restores Blender idle clip");
            var idleOrigin=player.position;yield return new WaitForSeconds(.2f);
            check(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(idleOrigin.x,idleOrigin.z))<.001f,"animation does not move character root");
            var shinBone=Array.Find(visual.GetComponentsInChildren<Transform>(),t=>t.name=="Rig_ShinR");
            var restShin=shinBone.localRotation;bool capturedRun=false;float runUntil=Time.time+.9f;
            while(Time.time<runUntil)
            {
                motor.Move(Vector3.forward*6*Time.deltaTime);yield return null;
                if(!capturedRun&&Quaternion.Angle(restShin,shinBone.localRotation)>45)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"soldier-run-runtime.png"));
                    capturedRun=true;
                }
            }
            check(capturedRun&&soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Run"),"fast movement selects run with a distinct deeper knee bend");
            float slowUntil=Time.time+.4f;
            while(Time.time<slowUntil){motor.Move(Vector3.forward*2*Time.deltaTime);yield return null;}
            check(soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"slowing down transitions from run to walk");
            yield return new WaitForSeconds(.35f);
            check(soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"stopping after run restores idle");
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.back})
            {
                string state=direction.x<0?"StrafeLeft":direction.x>0?"StrafeRight":"Backward";
                float directionUntil=Time.time+.6f;
                while(Time.time<directionUntil){motor.Move(visual.TransformDirection(direction)*2*Time.deltaTime);yield return null;}
                check(soldierAnimator.GetCurrentAnimatorStateInfo(0).IsName(state),"actual movement selects Blender "+state+" clip");
                if(state=="StrafeLeft")ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"soldier-strafe.png"));
                if(state=="Backward")ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"soldier-backward.png"));
                yield return new WaitForSeconds(.3f);
            }
            motor.enabled=false;player.position=movementOrigin;motor.enabled=true;
            foreach(bool characterWindow in new[]{true,false})
            {
                motor.enabled=false;player.position=movementOrigin+Vector3.up*2;motor.enabled=true;vertical=0;
                characterOpen=characterWindow;missionOpen=!characterWindow;
                var airborne=player.position;
                yield return new WaitForSeconds(.2f);
                check(player.position.y<airborne.y-.1f,"open window preserves gravity: "+(characterWindow?"character":"mission"));
                check(new Vector2(player.position.x-airborne.x,player.position.z-airborne.z).magnitude<.001f,"open window does not introduce horizontal movement");
                yield return new WaitForSeconds(.65f);
                check(motor.isGrounded&&!RangedMoving,"landing with a window open clears ranged movement restriction");
                characterOpen=missionOpen=false;
            }
            var starter=Build.inventory.At(6);
            var equipmentTarget=new GameObject("Equipment QA target").AddComponent<DroneEnemy>();
            equipmentTarget.enabled=false;equipmentTarget.transform.position=player.position+Vector3.up*2;
            target=equipmentTarget;
            check(CanHit(),"equipment fixture starts with reachable target");
            check(TryNormalDamage(24,out float unarmored)&&Mathf.Abs(unarmored-24.36f)<.001f,"runtime normal damage uses shared attack rating");
            equipmentTarget.armorClasses=new[]{new AoStatModifier{statId=90,amount=10000}};
            check(TryNormalDamage(24,out float armored)&&Mathf.Abs(armored-3.045f)<.001f,"runtime target projectile AC reaches scaled minimum damage");
            autoAttack=true;yield return new WaitForSeconds(.05f);
            check(weaponPreparing,"equipment fixture begins a real attack preparation");
            check(!RequestEquipment("missing-equipment-instance",0)&&weaponPreparing&&autoAttack,"rejected equipment request preserves the prepared attack");
            check(RequestEquipment(starter.instanceId,0)&&Equipping&&Build.Weapon!=null,"unequip waits for sourced delay");
            check(!weaponPreparing&&preparingTarget==null&&!autoAttack,"accepted equipment request immediately cancels attack preparation");
            check(!RequestEquipment(starter.instanceId,0),"duplicate pending equip rejected");
            yield return new WaitForSeconds(1.2f);
            check(Build.Weapon==null&&starter.slot==0&&!Equipping,"unequip commits after delay");
            check(!CanHit(),"cannot shoot while weapon is in inventory");
            int weaponParts=0;
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name=="Rifle"||t.name=="Barrel"){weaponParts++;check(!t.gameObject.activeSelf,"unequipped weapon mesh hidden");}
            check(weaponParts==2,"both Blender weapon parts located");
            check(RequestEquipment(starter.instanceId,6)&&Build.Weapon==null,"reequip does not grant weapon early");
            yield return new WaitForSeconds(1.2f);
            check(Build.Weapon!=null&&starter.slot==6,"reequip commits owned weapon");
            check(CanHit(),"same target reachable again after reequip");
            target=null;Destroy(equipmentTarget.gameObject);
            var restored=CharacterBuild.ParseSave(JsonUtility.ToJson(Build));
            check(restored!=null&&restored.inventory.At(6).instanceId==starter.instanceId,"runtime save preserves item identity");
            characterOpen=true;GetComponent<DistrictHud>().ShowCharacterTab(1);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"equipment-inventory.png"));
            yield return new WaitForSeconds(1);characterOpen=false;
            // Owned equipment fixtures only: acquisition is not yet implemented.
            var beltFixture=new AoItemInstance(36783,1);var memoryFixture=new AoItemInstance(36779,1);
            Build.inventory.items.Add(beltFixture);Build.inventory.items.Add(memoryFixture);
            check(!RequestEquipment(memoryFixture.instanceId,9),"memory cannot equip before belt exists");
            check(RequestEquipment(beltFixture.instanceId,7)&&Build.Value(45)==0,"belt starts ten-second equip without granting deck early");
            yield return new WaitForSeconds(1.2f);
            check(Equipping&&beltFixture.slot==0&&Build.Value(45)==0,"belt remains pending beyond memory equip duration");
            yield return new WaitForSeconds(9);
            check(!Equipping&&beltFixture.slot==7&&Build.Value(45)==1,"belt commits after ten seconds");
            check(RequestEquipment(memoryFixture.instanceId,9)&&Build.MaxNcu==8,"memory starts one-second equip without granting NCU early");
            yield return new WaitForSeconds(1.2f);
            check(memoryFixture.slot==9&&Build.MaxNcu==10,"memory adds two NCU after delay");
            check(!RequestEquipment(beltFixture.instanceId,0),"installed memory prevents belt removal in runtime");
            var largeMemoryFixture=new AoItemInstance(95520,200);Build.inventory.items.Add(largeMemoryFixture);
            characterOpen=true;GetComponent<DistrictHud>().ShowCharacterTab(1);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"belt-memory-equipment.png"));
            yield return new WaitForSeconds(1);characterOpen=false;
            Build.inventory.items.Remove(largeMemoryFixture);
            check(RequestEquipment(memoryFixture.instanceId,0),"memory removal starts");
            yield return new WaitForSeconds(1.2f);
            check(Build.MaxNcu==8&&memoryFixture.slot==0,"memory removal returns NCU baseline");
            // Restore the isolated fixture for the existing nano checks.
            Build.inventory.items.Remove(memoryFixture);Build.inventory.items.Remove(beltFixture);
            var armorFixture=new AoItemInstance(85697,1);Build.inventory.items.Add(armorFixture);
            check(!RequestEquipment(armorFixture.instanceId,21),"runtime armor checks abilities");
            Build.investments[17]=2;Build.investments[20]=2;
            check(RequestEquipment(armorFixture.instanceId,21)&&Build.Value(90)==0,"armor protection waits for equip delay");
            yield return new WaitForSeconds(.2f);
            check(armorFixture.slot==21&&Build.Value(90)==5&&Mathf.Abs(IncomingProjectile(3,1,out _)-2.5f)<.001f,"worn armor reduces incoming projectile damage");
            Build.Effects.Apply(70308,Build.MaxNcu);
            check(Mathf.Abs(IncomingProjectile(3,1,out float returnDamage)-.625f)<.001f&&Mathf.Abs(returnDamage-1.875f)<.001f,"reflect applies after armor mitigation");
            Build.Effects.Cancel(70308);
            Build.investments[17]=0;
            check(Build.Value(90)==3&&Mathf.Abs(IncomingProjectile(3,1,out _)-2.7f)<.001f,"ability loss reduces equipped armor protection at runtime");
            characterOpen=true;GetComponent<DistrictHud>().ShowCharacterTab(1);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"armor-equipment.png"));
            yield return new WaitForSeconds(1);characterOpen=false;
            Build.investments[17]=2;check(Build.Value(90)==5,"ability restoration recovers armor protection");
            check(RequestEquipment(armorFixture.instanceId,0),"armor removal begins");
            yield return new WaitForSeconds(.2f);
            check(Build.Value(90)==0&&IncomingProjectile(3,1,out _)==3,"armor removal restores incoming damage");
            Build.inventory.items.Remove(armorFixture);Build.investments[17]=0;Build.investments[20]=0;
            Expertise();check(!Buff && pendingNano==0,"reject missing expertise skills");
            // Isolated nano fixtures exceed level-one training caps; do not save them.
            Build.investments[129]=55;Build.investments[122]=55;Build.investments[132]=56;Nano=200;
            var castingTarget=new GameObject("Casting QA target").AddComponent<DroneEnemy>();castingTarget.enabled=false;
            castingTarget.transform.position=player.position+Vector3.up*2;target=castingTarget;
            check(CanHit(),"cast fixture starts with reachable weapon target");
            Expertise();check(Casting&&!CanHit(),"normal attacks blocked during nano execution");
            check(Mathf.Abs(castRemaining-1.77f)<.001f,"runtime NanoCInit and slider change expertise cast time");
            check(!RequestEquipment(starter.instanceId,0)&&Casting&&!Equipping,"equipment changes cannot run during nano execution");
            yaw+=10;
            yield return new WaitForSeconds(.25f);
            check(Casting&&CastProgress>0,"camera rotation does not interrupt stationary cast");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"nano-casting.png"));
            yield return new WaitForSeconds(.5f);
            motor.Move(Vector3.forward*.3f);
            yield return new WaitForSeconds(.1f);
            check(!Casting&&!Buff&&Nano==200&&Build.UsedNcu==0&&castRemaining==0,"movement interrupts without granting effect or consuming resources");
            check(CanHit(),"weapon action available again after interruption");
            Expertise();check(Casting,"final-frame interruption fixture begins casting");castRemaining=.001f;
            motor.Move(Vector3.forward*.3f);
            yield return new WaitForSeconds(.1f);
            check(!Casting&&!Buff&&Nano==200,"displacement wins over cast completion in same update");
            yaw-=10;target=null;Destroy(castingTarget.gameObject);
            Expertise();check(pendingNano==26370 && !Buff,"expertise has cast time");
            yield return new WaitForSeconds(2.3f);
            check(Buff && Nano==160 && buffTime>1790,"sourced expertise cost and 30 minute duration");
            check(Build.Value(116)==26&&Build.AttackRating()==26&&Build.UsedNcu==4,"buff reaches skill, attack rating and NCU without doubling");
            yield return new WaitForSeconds(.6f);
            Build.investments[131]=41;Build.investments[130]=53;
            Reflection();yield return new WaitForSeconds(.4f);
            check(Reflect&&Build.UsedNcu==8&&Build.Value(205)==75&&Nano==34,"TMS shares effect state with expertise");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"active-nanos.png"));
            yield return new WaitForSeconds(1);
            Nano=100;Expertise();check(Buff&&pendingNano==26370,"casting active expertise starts refresh rather than cancellation");
            yield return new WaitForSeconds(2.3f);
            check(Buff&&Reflect&&Build.UsedNcu==8&&Build.Value(116)==26&&Nano==60,"refresh at full NCU replaces same effect once");
            CancelNano(70308);Build.investments[131]=0;Build.investments[130]=0;
            CancelNano(26370);check(!Buff,"cancel buff");
            yield return new WaitForSeconds(.6f);
            Nano=100;Expertise();check(pendingNano==26370,"resource-loss fixture starts executing");Nano=39;
            yield return new WaitForSeconds(2.3f);
            check(!Buff&&Build.UsedNcu==0&&Nano==39&&pendingNano==0,"resource loss during cast cannot create buff or negative nano");
            Build.investments[149]=394;Nano=100;
            Expertise();check(Buff&&!Casting&&Nano==60&&Mathf.Abs(nanoRecharge-.5f)<.001f,"instant nano applies once and keeps fixed recharge");
            CancelNano(26370);Build.investments[149]=0;nanoRecharge=0;
            Reflection();check(pendingNano==0 && !Reflect,"TMS requirements enforced");
            Build.investments[149]=394;Nano=100;
            Build.learnedNanos.Remove(26354);CastReviewedNano(26354);
            check(!Casting&&Nano==100&&Build.Effects.Remaining(26354)==0,"unlearned nano cannot cast");
            var crystal=new AoItemInstance(26481,4);Build.inventory.items.Add(crystal);
            UploadCrystal(crystal.instanceId);check(Build.KnowsNano(26354)&&Build.inventory.Find(crystal.instanceId)==null,"owned crystal uploads and is consumed");
            CastReviewedNano(26354);check(Build.Effects.Remaining(26354)>0&&Build.UsedNcu==2&&Nano==75,"Proficiency casts through catalog");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"nano-proficiency.png"));
            yield return new WaitForSeconds(.2f);nanoRecharge=0;
            Expertise();check(Buff&&Build.Effects.Remaining(26354)==0&&Build.UsedNcu==4&&Nano==35,"Expertise replaces Proficiency through cast path");
            nanoRecharge=0;CastReviewedNano(26354);check(Buff&&Build.Effects.Remaining(26354)==0&&Nano==35&&!Casting,"weaker replacement consumes no nano");
            CancelNano(26370);Build.investments[149]=0;
            Build.investments[149]=394;Build.investments[128]=0;Nano=100;nanoRecharge=0;
            CastReviewedNano(27175);check(!Casting&&Nano==100&&Build.Effects.Remaining(27175)==0,"Treatment requires Biological Metamorphosis");
            Build.investments[128]=61;int treatmentBefore=Build.Value(124);
            CastReviewedNano(27175);check(Build.Value(124)==treatmentBefore+20&&Nano==60,"generic caster applies Treatment Expertise");
            CancelNano(27175);Build.investments[128]=0;Build.investments[149]=0;nanoRecharge=0;
            Build.learnedNanos.Remove(29091);var bodyCrystal=new AoItemInstance(29092,1);Build.inventory.items.Add(bodyCrystal);
            UploadCrystal(bodyCrystal.instanceId);check(Build.KnowsNano(29091),"Body Boost crystal uploads at base Soldier skills");
            Nano=32;int hpBeforeBody=Build.MaxHealth;CastReviewedNano(29091);
            yield return new WaitForSeconds(3.2f);
            check(Build.MaxHealth==hpBeforeBody+20&&Nano==21&&Build.UsedNcu==1,"Body Boost cast increases maximum HP and charges sourced nano");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"body-boost.png"));
            yield return new WaitForSeconds(.2f);Health=Build.MaxHealth;CancelNano(29091);
            yield return null;
            check(Health<=Build.MaxHealth&&Build.MaxHealth==hpBeforeBody,"removing Body Boost clamps current HP to reduced maximum");
            nanoRecharge=0;Nano=32;Build.investments[129]=61;Build.investments[122]=61;
            int[] compositeStats={127,128,129,122,131,130};
            int[] compositeBefore=Array.ConvertAll(compositeStats,s=>Build.Value(s));
            CastReviewedNano(223380);yield return new WaitForSeconds(1.3f);
            check(Build.Effects.Remaining(223380)>28798&&Build.UsedNcu==4&&Nano==31,"Composite Nano Expertise uses catalog cost and duration through cast path");
            for(int i=0;i<compositeStats.Length;i++)check(Build.Value(compositeStats[i])==compositeBefore[i]+20,"composite cast modifies nano skill "+compositeStats[i]);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"composite-nano.png"));
            yield return new WaitForSeconds(.2f);CancelNano(223380);
            nanoRecharge=0;Build.investments[129]=0;Build.investments[122]=0;
            int utilityTreatment=Build.Value(124),utilityPsychology=Build.Value(162);Nano=32;
            CastReviewedNano(287046);yield return new WaitForSeconds(1.3f);
            check(Build.Value(124)==utilityTreatment+20&&Build.Value(162)==utilityPsychology+20&&Nano==31&&Build.UsedNcu==4,"Utility composite casts with base Soldier requirements and modifies Treatment and Psychology");
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"composite-utility.png"));
            yield return new WaitForSeconds(.2f);CancelNano(287046);
            check(Build.Value(124)==utilityTreatment&&Build.Value(162)==utilityPsychology,"Utility cancellation restores skills through runtime path");
            nanoRecharge=0;
            float oldNano=Nano;float oldHp=Health;Heal();check(Health==oldHp&&Nano==oldNano,"fictional heal removed");
            TargetPickingValidation.Run(this,check);
            TerminalInteractionValidation.Run(this,check);
            CameraObstructionValidation.Run(check);
            CameraViewValidation.Run(this,check);
            check(ToggleCameraView(),"runtime first-person capture enters view");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"first-person.png"));
            yield return null;
            check(ToggleCameraView(),"runtime third-person capture restores view");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"third-person.png"));
            yield return null;
            VitalModifierValidation.Run(this,check);
            var returningObject=Instantiate(dronePrefab,new Vector3(1010,5,1000),Quaternion.identity);
            var returningDrone=returningObject.AddComponent<DroneEnemy>();returningDrone.home=new Vector3(1000,5,1000);
            float returnHealth=returningDrone.health;
            yield return new WaitForSeconds(.1f);
            float returnDistance=Vector2.Distance(new Vector2(returningObject.transform.position.x,returningObject.transform.position.z),new Vector2(1000,1000));
            check(returnDistance>5&&returnDistance<10,"disengaged drone flies toward home instead of teleporting there");
            check(returningDrone.health==returnHealth,"return movement does not invent a healing reset");
            returningObject.SetActive(false);Destroy(returningObject);
            HotbarValidation.Run(this,check);
            var shortcutWeapon=Build.inventory.At(6);var beforeItemBar=Build.hotbar;Build.hotbar=new AoHotbar();
            Build.hotbar.AssignItem(9,shortcutWeapon.instanceId);ActivateHotbar(9);
            check(Equipping&&shortcutWeapon.slot==6,"item shortcut respects delayed unequip rather than immediate mutation");
            yield return new WaitForSeconds(equipRemaining+.1f);
            check(shortcutWeapon.slot==0,"item shortcut unequips the owned weapon after its delay");
            ActivateHotbar(9);yield return new WaitForSeconds(equipRemaining+.1f);
            check(shortcutWeapon.slot==6&&Build.inventory.At(6).instanceId==shortcutWeapon.instanceId,"same shortcut reequips the same owned weapon instance");
            Build.hotbar=beforeItemBar;
            var movementPosition=player.position;var movementRotation=visual.rotation;
            float movementYaw=yaw,movementVertical=vertical;bool movementWalk=Walking,movementAttack=autoAttack;
            motor.enabled=false;player.position=new Vector3(0,10,-15);motor.enabled=true;
            yaw=0;autoAttack=false;Walking=false;
            var turnStart=player.position;MoveCharacter(0,1,0,false,1f/60);
            check(new Vector2(player.position.x-turnStart.x,player.position.z-turnStart.z).magnitude<.001f&&yaw>0,"turn input rotates without lateral movement");
            yaw=0;var strafeStart=player.position;MoveCharacter(0,0,1,false,1f/60);
            check(player.position.x>strafeStart.x&&Mathf.Abs(player.position.z-strafeStart.z)<.001f&&yaw==0,"strafe input moves sideways without changing heading");
            Walking=true;var walkStart=player.position;MoveCharacter(1,0,0,false,1f/60);
            float walkDistance=player.position.z-walkStart.z;
            Walking=false;var runStart=player.position;MoveCharacter(1,0,0,false,1f/60);
            check(walkDistance>0&&player.position.z-runStart.z>walkDistance,"walk mode is slower than run through character motor");
            motor.enabled=false;player.position=movementPosition;motor.enabled=true;
            visual.rotation=movementRotation;yaw=movementYaw;vertical=movementVertical;Walking=movementWalk;autoAttack=movementAttack;
            var numberFixture=new CombatNumbers();
            numberFixture.Add(Vector3.zero,0,false);numberFixture.Add(Vector3.zero,float.NaN,false);
            check(numberFixture.Count==0,"damage labels reject empty and invalid damage");
            for(int i=0;i<40;i++)numberFixture.Add(Vector3.zero,1,false);
            check(numberFixture.Count==32,"damage label buffer remains bounded during bursts");
            numberFixture.Tick(.5f);check(numberFixture.Count==32,"damage labels remain during their visible lifetime");
            numberFixture.Tick(1);check(numberFixture.Count==0,"expired damage labels release buffer entries");
            AcceptMission();check(enemies.Count==3 && missionActive,"mission spawns objective count");
            AcceptMission();check(enemies.Count==3,"duplicate accept rejected");
            CycleTarget();check(target!=null,"tab target selects live enemy");
            check(!CanHit(),"starter weapon range is 20m");
            Burst();check(burstCooldown==0,"unimplemented Burst does not consume a fabricated cooldown");
            Vector3 beforeMove=player.position;motor.Move(Vector3.forward);check(player.position.z>beforeMove.z+.9f,"character controller movement");
            motor.enabled=false;player.position=new Vector3(0,.1f,7);motor.enabled=true;
            yield return new WaitForSeconds(.1f);
            check(CanHit(),"target in range");autoAttack=true;weaponTimer=100;
            yield return new WaitForSeconds(.3f);
            yield return new WaitForEndOfFrame();
            check(GetComponent<SoldierCombatPose>().AimWeight>.99f,"combat pose reaches aimed stance");
            check(GetComponent<SoldierCombatPose>().MuzzleSurfaceDistance()<.075f,"shot origin remains on posed rifle barrel");
            check(GetComponent<SoldierCombatPose>().SupportWristDistance()<.01f,"supporting hand reaches authored rifle marker");
            GetComponent<SoldierCombatPose>().Shot();
            yield return null;yield return new WaitForEndOfFrame();
            check(GetComponent<SoldierCombatPose>().SupportWristDistance()<.01f,"supporting hand follows rifle recoil");
            weaponTimer=0;
            float movementShotHealth=target.health;
            MoveCharacter(0,0,1,false,.02f);
            check(RangedMoving&&!CanHit(),"actual strafing blocks normal ranged fire");
            Fire(1);check(target.health==movementShotHealth,"attempted ranged fire during movement applies no damage");
            MoveCharacter(0,1,0,false,.02f);
            check(!RangedMoving&&CanHit(),"turning in place does not block normal ranged fire");
            float beforePreparationHealth=target.health;
            yield return new WaitForSeconds(.1f);
            check(weaponPreparing&&WeaponCycleRemaining>0&&target.health==beforePreparationHealth,"first automatic shot waits through attack preparation");
            autoAttack=false;yield return new WaitForSeconds(.05f);
            check(!weaponPreparing&&target.health==beforePreparationHealth,"stopping during preparation cancels the pending shot");
            autoAttack=true;yield return new WaitForSeconds(.05f);
            var preparationTarget=target;target=enemies[1];yield return new WaitForSeconds(.05f);
            check(weaponPreparing&&preparingTarget==target,"switching target restarts preparation for the new target");
            target=preparationTarget;yield return new WaitForSeconds(.05f);
            var lateWall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Vector3 wallFrom=player.position+Vector3.up*1.4f;
            lateWall.transform.position=(wallFrom+target.transform.position)*.5f;
            lateWall.transform.rotation=Quaternion.LookRotation(target.transform.position-wallFrom);
            lateWall.transform.localScale=new Vector3(8,5,.3f);Physics.SyncTransforms();
            float wallTargetHealth=target.health;
            yield return new WaitForSeconds(WeaponCycleRemaining+.1f);
            check(!weaponPreparing&&target.health==wallTargetHealth,"wall appearing during preparation prevents damage at shot release");
            check(WeaponBlockReason()=="Line of sight blocked.","late obstruction reports the same sight failure used by firing");
            lateWall.SetActive(false);Destroy(lateWall);Physics.SyncTransforms();
            yield return new WaitForSeconds(WeaponCycleRemaining+.1f);
            check(weaponPreparing&&preparingTarget==target,"automatic preparation resumes after obstruction is removed");
            yield return new WaitForSeconds(WeaponCycleRemaining+.1f);
            check(!weaponPreparing&&WeaponCycleRemaining>0&&WeaponCycleProgress>0&&WeaponCycleProgress<1,"automatic shot transitions from attack preparation to recharge");
            float rechargeBeforeStop=WeaponCycleRemaining;autoAttack=false;yield return new WaitForSeconds(.05f);
            check(!weaponPreparing&&WeaponCycleRemaining>0&&WeaponCycleRemaining<rechargeBeforeStop,"stopping attack preserves the running recharge timer");
            autoAttack=true;
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"combat.png"));
            yield return new WaitForSeconds(2);
            InspectTarget();
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"target-information.png"));
            yield return new WaitForSeconds(.2f);
            inspectedTarget=null;
            qaArtCamera=true;
            view.transform.position=player.position+visual.rotation*new Vector3(2.4f,1.75f,2.7f);
            view.transform.LookAt(player.position+Vector3.up*1.2f);
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"combat-stance.png"));
            yield return new WaitForSeconds(.2f);
            autoAttack=false;
            var presentationRandom=UnityEngine.Random.state;
            float expectedRandom=UnityEngine.Random.value;
            UnityEngine.Random.state=presentationRandom;
            float impactHealth=target.health;
            var missFixture=new CombatNumbers();missFixture.Miss(target.transform.position);
            check(missFixture.Count==1&&target.health==impactHealth,"miss presentation does not change target health");
            missFixture.Tick(1.2f);check(missFixture.Count==0,"miss presentation expires with combat labels");
            target.Hit(1);
            check(UnityEngine.Random.value==expectedRandom,"impact visuals preserve gameplay random sequence");
            check(target.health==impactHealth-1,"impact visuals do not add damage");
            view.transform.position=target.transform.position+new Vector3(2,1,-3);
            view.transform.LookAt(target.transform.position);
            yield return new WaitForSeconds(.06f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"projectile-impact.png"));
            yield return new WaitForSeconds(.7f);
            // Suspend fixture enemies so their shots do not create new effects during cleanup QA.
            foreach(var enemy in enemies)enemy.enabled=false;
            yield return new WaitForSeconds(.6f);
            var savedMissRandom=UnityEngine.Random.state;
            var missBounds=new Bounds(Vector3.zero,new Vector3(3,2,2));
            foreach(var missOrigin in new[]{new Vector3(0,0,-10),new Vector3(0,10,0),new Vector3(3,0,0)})
            {
                bool clear=CombatVisuals.MissEndpoint(missOrigin,missBounds,out var endpoint);
                var missRay=new Ray(missOrigin,(endpoint-missOrigin).normalized);
                check(clear&&!missBounds.IntersectRay(missRay),"miss tracer clears target bounds from horizontal, vertical or close origin");
            }
            check(!CombatVisuals.MissEndpoint(Vector3.zero,missBounds,out _),"miss tracer is suppressed when muzzle lies inside enclosing target volume");
            bool foundMiss=false;
            for(int missSeed=0;missSeed<10000;missSeed++)
            {
                UnityEngine.Random.InitState(missSeed);
                var candidateState=UnityEngine.Random.state;
                UnityEngine.Random.Range(Build.Weapon.minDamage,Build.Weapon.maxDamage+1);
                if(UnityEngine.Random.value>=Mathf.Clamp(.78f+Build.AttackRating()*.001f,.78f,.97f))
                {UnityEngine.Random.state=candidateState;foundMiss=true;break;}
            }
            check(foundMiss,"deterministic fixture finds a miss under the existing hit chance");
            float beforeMissHealth=target.health;int beforeMissLabels=DamageNumbers.Count;
            Fire(1);
            UnityEngine.Random.state=savedMissRandom;
            check(target.health==beforeMissHealth&&DamageNumbers.Count==beforeMissLabels+1,"resolved missed shot emits feedback without applying damage");
            yield return new WaitForSeconds(.08f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"combat-miss.png"));
            yield return new WaitForSeconds(1.2f);
            check(FindObjectsByType<CombatVisuals>(FindObjectsSortMode.None).Length==0,"completed combat effects release their scene objects");
            qaArtCamera=false;autoAttack=false;
            target=enemies[0];autoAttack=true;
            enemies[1].Hit(999);
            check(autoAttack&&target==enemies[0]&&!target.dead,"death of another enemy preserves the current attack");
            enemies[0].Hit(999);
            check(!autoAttack&&target.dead,"death of the current target stops automatic attack");
            foreach(var enemy in enemies.ToArray()) { enemy.Hit(999);enemy.Hit(999); }
            check(kills==required && missionComplete,"kills count once and complete objective");
            Claim();check(Build.credits==0,"remote reward claim rejected");
            motor.enabled=false;player.position=terminal.position+Vector3.forward*2;motor.enabled=true;
            // In QA, exercise the claim while keeping the user's local save untouched.
            Claim();check(Build.credits==250 && !missionActive,"reward commit");Claim();check(Build.credits==250,"duplicate reward rejected");
            check(Build.xp==150,"claimed training XP matches persistent character progress");
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"training-reward.png"));
            yield return new WaitForSeconds(.2f);
            AcceptMission();check(enemies.Count==3 && kills==0,"next mission resets objectives");
            target=enemies[0];preparingTarget=target;autoAttack=weaponPreparing=true;weaponTimer=weaponCycleDuration=5;
            Respawn();
            check(!autoAttack&&!weaponPreparing&&preparingTarget==null&&target==null&&WeaponCycleRemaining==0,"reclaim immediately clears prepared attack and combat target");
            check(missionActive&&enemies.Count==3&&kills==0,"reclaim preserves the active training objective");
            weaponTimer=weaponCycleDuration=3;Respawn();
            check(WeaponCycleRemaining==3,"reclaim does not fabricate a weapon recharge reset");
            Abandon();check(!missionActive && enemies.Count==0,"abandon removes enemies");
            File.WriteAllText(Path.Combine(QaArtifactDirectory,"runtime-validation.txt"),"PASS: "+checks+" runtime checks. Equipment ownership, delay, visuals, save identity, unarmed rejection, nano resources/timing and training mission invariants. Not full AO conformance.\n");
            photo=true;qaArtCamera=true;
            view.transform.position=new Vector3(7.7f,2.3f,-12.7f);
            view.transform.LookAt(new Vector3(4.3f,1.45f,-8));
            yield return new WaitForSeconds(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"grid-terminal-detail.png"));
            yield return new WaitForSeconds(1);
            view.transform.position=new Vector3(3,2.4f,-17);
            view.transform.LookAt(new Vector3(-9,.9f,4));
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(QaArtifactDirectory,"street-detail.png"));
            yield return new WaitForSeconds(.5f);
            Debug.Log("RUNTIME_CAPTURE_OK"); Application.Quit();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if(GetComponent<DistrictHud>().CancelShortcutInteraction()){}else if (characterOpen || missionOpen) { characterOpen=false; missionOpen=false; } else if(inspectedTarget)inspectedTarget=null;else Paused = !Paused; }
            if (Input.GetKeyDown(KeyCode.F2)) photo = !photo;
            if (Paused) {orbitDragging=terminalPress=false;return;}
            float dt=Time.deltaTime;
            DamageNumbers.Tick(dt);
            if(Equipping){equipRemaining-=dt;if(equipRemaining<=0){Build.inventory.Equip(pendingEquip,pendingEquipSlot,Build.Value,out var equipMessage);Log(equipMessage);pendingEquip=null;RefreshWeaponVisual();}}
            Build.Effects.Tick(dt);
            ClampResources();
            healCooldown = Mathf.Max(0,healCooldown-dt);
            nanoRecharge=Mathf.Max(0,nanoRecharge-dt);
            if(!characterOpen)TextEntryFocused=false;
            if(!TextEntryFocused){
            if (Input.GetKeyDown(KeyCode.U)) { characterOpen=!characterOpen; missionOpen=false; }
            if(Input.GetKeyDown(KeyCode.F8)&&!Input.GetKey(KeyCode.LeftControl)&&!Input.GetKey(KeyCode.RightControl))ToggleCameraView();
            if (Input.GetKeyDown(KeyCode.E) && TerminalDistance<4) { missionOpen=!missionOpen; characterOpen=false; }
            if (Input.GetKeyDown(KeyCode.F5)) Save();
            if (Input.GetKeyDown(KeyCode.Tab)&&!Input.GetKey(KeyCode.LeftControl)&&!Input.GetKey(KeyCode.RightControl)) CycleTarget(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
            if (Input.GetKeyDown(KeyCode.T)) InspectTarget();
            if(Input.GetKeyDown(KeyCode.Q))ToggleAttack();
            if(Input.GetKeyDown(KeyCode.Y))Build.hotbar.visible=!Build.hotbar.visible;
            for(int slot=0;slot<10;slot++)
            {
                KeyCode key=slot==9?KeyCode.Alpha0:(KeyCode)((int)KeyCode.Alpha1+slot);
                if(!Input.GetKeyDown(key))continue;
                if(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))Build.hotbar.layer=slot;
                else ActivateHotbar(slot);
            }
            }
            TerminalPointerAtScreen(Input.mousePosition,Input.GetMouseButtonDown(1),Input.GetMouseButtonUp(1));
            OrbitCameraAtScreen(Input.mousePosition,Input.GetMouseButtonDown(1),Input.GetMouseButton(1),new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y")));
            if (!characterOpen && !missionOpen)
            {
                if(Input.GetMouseButtonDown(0)&&!Input.GetMouseButton(1))TrySelectAtScreen(Input.mousePosition,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
                ZoomCameraAtScreen(Input.mousePosition,Input.mouseScrollDelta.y);
                if(Input.GetKeyDown(KeyCode.Backspace))Walking=!Walking;
                float strafe=(Input.GetKey(KeyCode.C)?1:0)-(Input.GetKey(KeyCode.Z)?1:0);
                MoveCharacter(Input.GetAxisRaw("Vertical"),Input.GetAxisRaw("Horizontal"),strafe,Input.GetKeyDown(KeyCode.Space),dt);
            }
            else MoveCharacter(0,0,0,false,dt);
            if(player.position.y < -10) Respawn();
            // Evaluate displacement before completion, including the final cast frame.
            if(Casting)
            {
                if(Vector3.Distance(player.position,castOrigin)>.02f)InterruptNano("Movement interrupted nano execution.");
                else {castRemaining-=dt;if(castRemaining<=0)FinishNano();}
            }
            if(weaponPreparing&&(!autoAttack||!target||target.dead||target!=preparingTarget||Equipping||Casting||Build.Weapon==null))
            CancelWeaponPreparation();
            weaponTimer-=dt;
            if (autoAttack && target && !target.dead && weaponTimer<=0 && !Equipping && !Casting && !RangedMoving && Build.Weapon!=null)
            {
                var weapon=Build.Weapon;int initiative=Build.Value(weapon.initiativeStatId);
                if(weaponPreparing)
                {
                    weaponPreparing=false;preparingTarget=null;
                    weaponTimer=weaponCycleDuration=AoInitiative.WeaponRecharge(weapon.rechargeMs/1000f,initiative,aggDef);
                    Fire(1);
                }
                else if(WeaponBlockReason()==null)
                {
                    weaponPreparing=true;preparingTarget=target;
                    weaponTimer=weaponCycleDuration=AoInitiative.WeaponAttack(weapon.attackMs/1000f,initiative,aggDef);
                }
            }
            ring.gameObject.SetActive(target && !target.dead);
            if (target) ring.position=new Vector3(target.transform.position.x,.08f,target.transform.position.z);
            if(traffic) traffic.position = new Vector3(Mathf.Sin(Time.time*.022f)*120,38,85+Mathf.Cos(Time.time*.022f)*10);
        }
        void LateUpdate()
        {
            if(qaArtCamera){cameraInitialized=false;return;}
            UpdateCameraView();
        }
        internal bool ToggleCameraView()
        {
            if(Paused||characterOpen||missionOpen||TextEntryFocused)return false;
            FirstPerson=!FirstPerson;cameraInitialized=false;cameraDistance=distance;
            return true;
        }
        internal void UpdateCameraView()
        {
            if(FirstPerson)
            {
                view.transform.SetPositionAndRotation(player.position+Vector3.up*1.8f,Quaternion.Euler(pitch,yaw,0));
                SetLocalPlayerVisible(false);return;
            }
            var pose=GetComponent<SoldierCombatPose>();
            Vector3 desiredFocus=player.position+Vector3.up*1.65f+Quaternion.Euler(0,yaw,0)*Vector3.right*(pose?pose.AimWeight*.35f:0);
            if(!cameraInitialized||Vector3.Distance(cameraFocus,desiredFocus)>10){cameraFocus=desiredFocus;cameraFocusVelocity=Vector3.zero;cameraInitialized=true;}
            cameraFocus=Vector3.SmoothDamp(cameraFocus,desiredFocus,ref cameraFocusVelocity,.065f);
            Vector3 focus=cameraFocus;
            Vector3 offset=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);
            float d=CameraObstruction.Distance(focus,offset);
            cameraDistance=d<cameraDistance?d:Mathf.Lerp(cameraDistance,d,1-Mathf.Exp(-Time.deltaTime*9));
            view.transform.position=focus+offset.normalized*cameraDistance;
            view.transform.LookAt(focus+Vector3.up*.2f);
            SetLocalPlayerVisible(Vector3.Distance(view.transform.position,player.position+Vector3.up*1.65f)>=.9f);
        }
        void SetLocalPlayerVisible(bool visible)
        {
            // Only this camera hides the local player's layer. Keep renderers and
            // equipment activation intact so switching view cannot equip a weapon.
            int mask=1<<player.gameObject.layer;
            view.cullingMask=visible?view.cullingMask|mask:view.cullingMask&~mask;
        }
        public void CycleTarget(bool backwards=false)
        {
            target=TargetPicking.Cycle(enemies,target,player.position,45,backwards);
        }
        internal float RequestedCameraDistance=>distance;
        void OnApplicationFocus(bool focused){if(!focused)orbitDragging=terminalPress=false;}
        internal bool PointerTerminalAtScreen(Vector2 point)
        {
            if(Paused||characterOpen||missionOpen||TextEntryFocused||!terminal||!terminal.gameObject.activeInHierarchy||TerminalDistance>=4)return false;
            if(point.x<0||point.y<0||point.x>=Screen.width||point.y>=Screen.height)return false;
            if(GetComponent<DistrictHud>().BlocksWorldPointer(point))return false;
            return Physics.Raycast(view.ScreenPointToRay(point),out var hit,view.farClipPlane,1<<0,QueryTriggerInteraction.Ignore)
                &&hit.transform.IsChildOf(terminal);
        }
        internal bool TerminalPointerAtScreen(Vector2 point,bool pressed,bool released)
        {
            if(Paused||characterOpen||missionOpen||TextEntryFocused){terminalPress=false;return false;}
            if(pressed){terminalPress=PointerTerminalAtScreen(point);terminalPressPoint=point;}
            // A camera drag stays a drag even if the pointer returns to its origin.
            if(terminalPress&&(point-terminalPressPoint).sqrMagnitude>25)terminalPress=false;
            if(!released)return false;
            bool use=terminalPress&&PointerTerminalAtScreen(point);terminalPress=false;
            if(!use)return false;
            missionOpen=true;characterOpen=false;orbitDragging=false;return true;
        }
        internal bool OrbitCameraAtScreen(Vector2 point,bool pressed,bool held,Vector2 delta)
        {
            if(!held||Paused||characterOpen||missionOpen||TextEntryFocused){orbitDragging=false;return false;}
            if(pressed)
                orbitDragging=point.x>=0&&point.y>=0&&point.x<Screen.width&&point.y<Screen.height&&!GetComponent<DistrictHud>().BlocksWorldPointer(point);
            if(!orbitDragging)return false;
            yaw+=delta.x*3;pitch=Mathf.Clamp(pitch-delta.y*2,-15,65);return true;
        }
        internal bool ZoomCameraAtScreen(Vector2 point,float scroll)
        {
            if(FirstPerson||Paused||characterOpen||missionOpen||TextEntryFocused||scroll==0||float.IsNaN(scroll)||float.IsInfinity(scroll))return false;
            if(point.x<0||point.y<0||point.x>=Screen.width||point.y>=Screen.height)return false;
            if(GetComponent<DistrictHud>().BlocksWorldPointer(point))return false;
            distance=Mathf.Clamp(distance-scroll,4,14);return true;
        }
        void MoveCharacter(float forward,float turn,float strafe,bool jump,float dt)
        {
            yaw+=turn*120*dt;
            Quaternion heading=Quaternion.Euler(0,yaw,0);
            Vector3 direction=heading*Vector3.ClampMagnitude(new Vector3(strafe,0,forward),1);
            Vector3 facing=Vector3.zero;
            if(direction.sqrMagnitude>.01f||Mathf.Abs(turn)>.01f)facing=heading*Vector3.forward;
            if(autoAttack&&target&&!target.dead&&Build.Weapon!=null){facing=target.transform.position-player.position;facing.y=0;}
            if(facing.sqrMagnitude>.01f)visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(facing),dt*12);
            vertical=motor.isGrounded?-2:vertical-22*dt;
            if(jump&&motor.isGrounded)vertical=7;
            // Authored prototype speeds; AO RunSpeed-derived movement remains to be implemented.
            motor.Move((direction*(Walking?2:5)+Vector3.up*vertical)*dt);
        }
        public bool InspectTarget()
        {
            if(Paused||characterOpen||missionOpen||TextEntryFocused||!target||target.dead||!target.gameObject.activeInHierarchy)return false;
            inspectedTarget=target;return true;
        }
        public DroneEnemy PointerTargetAtScreen(Vector2 screenPoint)
        {
            if(Paused||characterOpen||missionOpen||TextEntryFocused)return null;
            if(screenPoint.x<0||screenPoint.y<0||screenPoint.x>=Screen.width||screenPoint.y>=Screen.height)return null;
            var hud=GetComponent<DistrictHud>();
            if(hud&&hud.BlocksWorldPointer(screenPoint))return null;
            return TargetPicking.Pick(enemies,view.ScreenPointToRay(screenPoint),player.position,45);
        }
        public bool TrySelectAtScreen(Vector2 screenPoint,bool inspect=false)
        {
            var selected=PointerTargetAtScreen(screenPoint);
            if(!selected)return false;
            target=selected;if(inspect)InspectTarget();return true;
        }
        public void ToggleAttack()
        {
            if(autoAttack){autoAttack=false;return;}
            if(Equipping||Build.Weapon==null){Log("Equip a ranged weapon first.");return;}
            if(!target||target.dead||!target.gameObject.activeInHierarchy)CycleTarget();
            if(target)autoAttack=true;else Log("No hostile signal. Accept a contract at the terminal.");
        }
        public void ActivateHotbar(int slot)
        {
            if(Paused||TextEntryFocused||GetComponent<DistrictHud>().ShortcutInteractionActive)return;
            string itemId=Build.hotbar.Item(slot);
            if(!string.IsNullOrEmpty(itemId)){UseShortcutItem(itemId);return;}
            int action=Build.hotbar.Get(slot);
            switch(action)
            {
                case -1:ToggleAttack();break;
                case -2:Burst();break;
                case -3:Heal();break;
                case -4:Expertise();break;
                case -5:Reflection();break;
                case -6:characterOpen=!characterOpen;missionOpen=false;break;
                case -7:Walking=!Walking;break;
                default:if(action>0)CastReviewedNano(action);break;
            }
        }
        public void UseShortcutItem(string instanceId)
        {
            var item=Build.inventory.Find(instanceId);
            if(item==null){Log("Shortcut item is no longer in your inventory.");return;}
            if(item.Definition.uploadNanoId>0){UploadCrystal(instanceId);return;}
            if(item.slot!=0){RequestEquipment(instanceId,0);return;}
            var slots=item.Definition.slots;
            if(slots==null||slots.Length==0){Log("This item's use action is not implemented yet.");return;}
            int destination=slots[0];
            foreach(int candidate in slots)if(Build.inventory.At(candidate)==null){destination=candidate;break;}
            RequestEquipment(instanceId,destination);
        }
        public string WeaponBlockReason()
        {
            if(RangedMoving)return "Stand still to fire a ranged weapon.";
            if(Casting)return "Nano execution in progress.";
            if(Equipping || Build.Weapon==null)return "No ready ranged weapon equipped.";
            if(!target || target.dead)return "Select a target with TAB.";
            if(Vector3.Distance(player.position,target.transform.position)>Build.Weapon.range)return $"Target beyond weapon range ({Build.Weapon.range} m).";
            if(Physics.Linecast(player.position+Vector3.up*1.4f,target.transform.position,1<<0))return "Line of sight blocked.";
            return null;
        }
        bool CanHit()
        {
            string reason=WeaponBlockReason();if(reason!=null){Log(reason);return false;}
            return true;
        }
        void Fire(float multiplier)
        {
            if(!CanHit()) return;
            var weapon=Build.Weapon;
            int roll=UnityEngine.Random.Range(weapon.minDamage,weapon.maxDamage+1);
            if(!TryNormalDamage(roll,out float damage))
            {autoAttack=false;Log("Weapon damage rules are not verified for this attack rating or damage type.");return;}
            damage*=multiplier;
            bool hit=UnityEngine.Random.value < Mathf.Clamp(.78f+Build.AttackRating()*.001f,.78f,.97f);
            var pose=GetComponent<SoldierCombatPose>();if(pose)pose.Shot();
            Vector3 origin=pose?pose.MuzzlePosition:player.position+Vector3.up*1.3f;
            Vector3 impact=target.transform.position;
            bool drawTracer=true;
            if(!hit)
            {
                var bounds=new Bounds(impact,Vector3.zero);
                foreach(var renderer in target.GetComponentsInChildren<Renderer>())
                    if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
                drawTracer=CombatVisuals.MissEndpoint(origin,bounds,out impact);
            }
            if(drawTracer)Beam(origin,impact,new Color(.2f,.85f,1));
            if(hit) { target.Hit(damage); Log($"Rifle impact · {Mathf.RoundToInt(damage)} damage"); }
            else { DamageNumbers.Miss(target.transform.position);Log("Target evaded."); }
        }
        bool TryNormalDamage(int roll,out float damage)
        {
            damage=0;var weapon=Build.Weapon;if(weapon==null||!target)return false;
            return AoWeaponDamage.TryNormal(weapon,Build.AttackRating(),roll,target.ArmorClass(weapon.damageTypeStatId),Build.Value(AoWeaponDamage.AddDamageStat(weapon.damageTypeStatId)),out damage);
        }
        public void TrainSkill(int id)
        {
            var connection=GetComponent<ServerTrainingClient>();
            if(connection){connection.Train(id);return;}
            Build.TrainStat(id,out string reason);Log(reason);
        }
        public void Burst()
        {
            if(SpecialLocks.Contains(148)) { Log("Burst skill is locked.");return; }
            Log("Burst is listed for the solar rifle; execution and recharge are not implemented yet.");
        }
        public void Heal() { Log("No healing nano uploaded or first-aid item equipped."); }
        public bool RequestEquipment(string instanceId,int slot)
        {
            if(Casting){Log("Finish nano execution before changing equipment.");return false;}
            if(Equipping){Log("Equipment change already in progress.");return false;}
            var item=Build.inventory.Find(instanceId);
            if(item==null || !item.Definition.equipDelayKnown){Log("Equipment timing is not yet verified for this item.");return false;}
            if(!Build.inventory.CanEquip(instanceId,slot,Build.Value,out var reason)){Log(reason);return false;}
            pendingEquip=instanceId;pendingEquipSlot=slot;equipRemaining=equipDuration=item.Definition.equipDelayMs/1000f;autoAttack=false;CancelWeaponPreparation();
            Log("Changing equipment: "+item.Definition.name);return true;
        }
        void RefreshWeaponVisual()
        {
            if(!visual)return;
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name=="Rifle"||t.name=="Barrel")t.gameObject.SetActive(Build.Weapon!=null);
        }
        public void Expertise()=>StartNano(26370);
        public void Reflection()=>StartNano(70308);
        public void CastReviewedNano(int id)
        {
            StartNano(id);
        }
        public void UploadCrystal(string instanceId)
        {
            if(Casting||Equipping||Health<=0){Log("Finish the current action before uploading a nano.");return;}
            Build.UploadNanoCrystal(instanceId,out string reason);Log(reason);
        }
        public void CancelNano(int id)
        {
            if(Build.Effects.Cancel(id))Log(AoSelfEffects.Get(id).name+" cancelled.");
            ClampResources();
        }
        public void ClampResources()
        {
            Health=Mathf.Min(Health,Build.MaxHealth);
            Nano=Mathf.Min(Nano,Build.MaxNano);
        }
        bool NanoRequirements(int nano)
        {
            return Build.KnowsNano(nano)&&(AoSelfEffects.Get(nano)?.MeetsCastRequirements(Build)??false);
        }
        void StartNano(int nano)
        {
            int id=nano;var definition=AoSelfEffects.Get(id);if(definition==null)return;int cost=definition.nanoCost;
            if(!Build.KnowsNano(nano)){Log("Upload this nano program before casting it.");return;}
            if(!NanoRequirements(nano)){Log("Nano skill or profession requirements are not met.");return;}
            if(Equipping || pendingNano!=0 || nanoRecharge>0 || !Build.Effects.CanApply(id,Build.MaxNcu) || Nano<cost){Log("Nano busy, insufficient NCU or nano energy.");return;}
            pendingNano=nano;castRemaining=castDuration=AoInitiative.NanoCast(definition.castMs/1000f,Build.Value(149),aggDef);castOrigin=player.position;Log("Executing "+definition.name+"...");
            if(castRemaining<=0)FinishNano();
        }
        void InterruptNano(string message)
        {
            if(!Casting)return;
            pendingNano=0;castRemaining=castDuration=0;Log(message);
        }
        void FinishNano()
        {
            int nano=pendingNano,id=nano;var definition=AoSelfEffects.Get(id);if(definition==null)return;int cost=definition.nanoCost;
            pendingNano=0;castRemaining=0;
            if(!NanoRequirements(nano) || Nano<cost || !Build.Effects.Apply(id,Build.MaxNcu)){Log("Nano failed: requirements, resources or NCU changed during execution.");return;}
            Nano-=cost;nanoRecharge=definition.rechargeMs/1000f;
        }
        public void EnemyShot(DroneEnemy enemy)
        {
            if(Physics.Linecast(enemy.transform.position,player.position+Vector3.up,1<<0)) return;
            Beam(enemy.transform.position,player.position+Vector3.up,new Color(1,.3f,.08f));
            if(UnityEngine.Random.value < Mathf.Clamp(.92f-Build.evades*.003f+aggDef*.1f,.3f,.95f))
            {
                float damage=IncomingProjectile(2+difficulty,1,out float reflected);
                Health-=damage;DamageNumbers.Add(player.position+Vector3.up*1.5f,damage,true);
                if(reflected>0)enemy.Hit(reflected);
            }
            if(Health<=0) Respawn();
        }
        float IncomingProjectile(float raw,float minimum,out float returnedDamage)
        {
            float armored=AoWeaponDamage.AfterArmor(raw,minimum,Build.Value(90));
            float reflected=armored*Mathf.Clamp01(Build.Value(205)/100f);
            returnedDamage=Mathf.Min(Mathf.Max(0,Build.Value(475)),reflected);
            return armored-reflected;
        }
        void Beam(Vector3 from,Vector3 to,Color c)
        {
            CombatVisuals.Tracer(beamMaterial,from,to,c);
        }
        public void DroneImpact(Vector3 position,bool destroyed)=>CombatVisuals.Impact(beamMaterial,position,destroyed);
        public void AcceptMission()
        {
            if(GetComponent<ServerTrainingClient>()){Log("Server missions are not available yet.");return;}
            if(missionActive) return;
            foreach(var e in enemies) if(e) Destroy(e.gameObject); enemies.Clear();
            kills=0;required=2+difficulty;missionActive=true;missionComplete=false;seed++;
            var random=new System.Random(seed);
            for(int i=0;i<required;i++)
            {
                var pos=new Vector3((float)random.NextDouble()*12-6,1.9f,17+i*7);
                var go=Instantiate(dronePrefab,pos,Quaternion.identity);go.name="Rogue survey drone "+(i+1);
                var e=go.AddComponent<DroneEnemy>(); e.index=i;e.home=pos;e.maxHealth=e.health=80+difficulty*25;enemies.Add(e);
            }
            missionOpen=false;Log($"Contract {seed} accepted. Disable {required} rogue drones in the north concourse.");
        }
        public void Killed(DroneEnemy enemy) { kills++;if(enemy==target)autoAttack=false;if(kills>=required) { missionComplete=true;Log("Contract complete. Return to the terminal to claim your reward."); } }
        public void Claim()
        {
            if(GetComponent<ServerTrainingClient>())return;
            if(!missionActive || !missionComplete || TerminalDistance>=4) return;
            int previousCredits=Build.credits,previousLevel=Build.level;
            int awardedXp=Build.Reward(difficulty);missionActive=false;missionComplete=false;Health=Build.MaxHealth;Nano=Build.MaxNano;
            Log($"Training reward · +{awardedXp:N0} XP · +{Build.credits-previousCredits:N0} credits");
            if(Build.level>previousLevel)Log($"Level {Build.level} reached · {Build.ip:N0} IP available");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qaCapture")<0)Save();
        }
        public void Abandon() { foreach(var e in enemies) if(e) Destroy(e.gameObject);enemies.Clear();target=null;autoAttack=false;missionActive=false;missionComplete=false;kills=0;Log("Contract abandoned."); }
        void CancelWeaponPreparation()
        {
            if(!weaponPreparing)return;
            weaponPreparing=false;preparingTarget=null;weaponTimer=weaponCycleDuration=0;
        }
        void Respawn() { InterruptNano("Death interrupted nano execution.");CancelWeaponPreparation();autoAttack=false;target=null;motor.enabled=false;player.position=new Vector3(0,.1f,-15);motor.enabled=true;Health=Build.MaxHealth;Nano=Build.MaxNano;Log("Reclaim transfer complete. Contract remains active."); }
        public void Save()
        {
            if(GetComponent<ServerTrainingClient>())return;
            try { Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(SavePath+".tmp",JsonUtility.ToJson(Build,true));if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,null);else File.Move(SavePath+".tmp",SavePath);Log("Character saved locally."); }
            catch(Exception e) { Log("Save failed: "+e.Message); }
        }
        void Load()
        {
            try {
                string path=File.Exists(SavePath)?SavePath:Path.Combine(Application.persistentDataPath,"soldier-v2.json");
                if(File.Exists(path)){var b=CharacterBuild.ParseSave(File.ReadAllText(path));if(b!=null)Build=b;else Log("Invalid save ignored.");}
            }
            catch(Exception) { Log("Save unavailable; using a fresh Soldier."); }
        }
        void OnApplicationQuit() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qaCapture")<0) Save(); }
        public bool HideHud => photo;
    }
}
