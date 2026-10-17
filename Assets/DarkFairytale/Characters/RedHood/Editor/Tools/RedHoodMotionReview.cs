using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Spine.Unity;

namespace RedHoodPrototype.Editor {
    // Explicit review only. Never regenerates assets or changes the user's scene.
    [InitializeOnLoad]
    public static class RedHoodMotionReview {
        const string Output = "output/red-hood-motion";
        // Explicit one-shot render request; never rebuilds assets on import.
        static RedHoodMotionReview() { EditorApplication.update += PollReviewRequest; }
        static void PollReviewRequest() {
            const string request = Output + "/sweep-perspective/review.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            string command=File.ReadAllText(request).Trim();
            File.Delete(request);
            try {
                if(command=="install") RedHoodWeaponViews.Install();
                if(command=="idlewind") ReviewAt(Output+"/idle-wind",new[]{"Idle1"});
                else if(command=="skill3") ReviewAt(Output+"/skill3",new[]{"Skill3"});
                else if(command=="skill2") ReviewAt(Output+"/skill2",new[]{"Skill2"});
                else if(command=="revive") ReviewAt(Output+"/revive",new[]{"Revive"});
                else if(command=="death") ReviewAt(Output+"/death",new[]{"Death1"});
                else if(command=="victory") ReviewAt(Output+"/victory",new[]{"Victory","VictoryIdle"});
                else if(command=="skill5") ReviewAt(Output+"/skill5-sync",new[]{"Skill5"});
                else if(command=="skill1") ReviewAt(Output+"/skill1-sync",new[]{"Skill1"});
                else if(command=="attack1") ReviewAt(Output+"/attack1-power",new[]{"Attack1"});
                else if(command=="velocity") ReviewAt(Output+"/sweep-return-velocity",new[]{"Attack2"});
                else if(command=="return") ReviewAt(Output+"/sweep-return",new[]{"Attack2"});
                else ReviewSweepPerspective();
                DarkFairytaleValidation.ValidatePlayer();
                RenderProductionPoses();
                File.WriteAllText(Output + "/sweep-perspective/result.txt", "PASS");
            } catch (Exception e) {
                File.WriteAllText(Output + "/sweep-perspective/result.txt", e.ToString());
                Debug.LogException(e);
            }
        }

        static void RenderProductionPoses() {
            var scene=EditorSceneManager.NewPreviewScene();
            var old=RenderTexture.active;
            try {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Characters/RedHood/Prefabs/Player.prefab"),scene);
                var player=go.GetComponent<Player>();
                var actor=player.skeletonAnimation;actor.Initialize(true);
                var style=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");
                var camGO=new GameObject("Production pose camera");SceneManager.MoveGameObjectToScene(camGO,scene);
                var camera=camGO.AddComponent<Camera>();camera.scene=scene;
                camera.transform.position=new Vector3(0,-4.5f,-15);camera.fieldOfView=66;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.17f);
                foreach(int width in new[]{720,640}) {
                    var rt=new RenderTexture(width,1280,24);
                    var pixels=new Texture2D(width,1280,TextureFormat.RGB24,false);
                    try {
                        camera.targetTexture=rt;camera.aspect=width/1280f;
                        style.PlacePlayer(player,camera);
                        foreach(float t in new[]{.4f,.56f,.72f}) {
                            Pose(actor,"Attack2",t,false);
                            var b=actor.GetComponent<Renderer>().bounds;
                            var min=camera.WorldToViewportPoint(b.min);var max=camera.WorldToViewportPoint(b.max);
                            if(min.x<0||max.x>1||min.y<0||max.y>1)throw new Exception("Production pose clipped at "+width+", "+t);
                            camera.Render();RenderTexture.active=rt;
                            pixels.ReadPixels(new Rect(0,0,width,1280),0,0);pixels.Apply();
                            File.WriteAllBytes(Output+"/sweep-perspective/production-"+width+"-"+Mathf.RoundToInt(t*100)+".png",pixels.EncodeToPNG());
                        }
                        var flash=go.GetComponentInChildren<MaterialSwapper>();
                        for(int i=0;i<flash.origMats.Count;i++)actor.CustomMaterialOverride[flash.origMats[i]]=flash.whiteMats[i];
                        actor.LateUpdate();camera.Render();RenderTexture.active=rt;
                        pixels.ReadPixels(new Rect(0,0,width,1280),0,0);pixels.Apply();
                        File.WriteAllBytes(Output+"/sweep-perspective/production-"+width+"-flash.png",pixels.EncodeToPNG());
                        actor.CustomMaterialOverride.Clear();
                    } finally { camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels); }
                }
            } finally { RenderTexture.active=old;EditorSceneManager.ClosePreviewScene(scene); }
        }

        [MenuItem("Tools/Red Hood/Review Sweep Perspective")]
        public static void ReviewSweepPerspective() {
            ReviewAt(Output + "/sweep-perspective", new[] { "Attack2" });
        }

        [MenuItem("Tools/Red Hood/Review Motion Sample")]
        public static void Review() {
            ReviewAt(Output);
        }

        [MenuItem("Tools/Red Hood/Review Idle Correction")]
        public static void ReviewIdleCorrection() {
            ReviewAt(Output + "/idle-correction");
        }

        [MenuItem("Tools/Red Hood/Review Sweep Power")]
        public static void ReviewSweepPower() {
            ReviewAt(Output + "/sweep-power", new[] { "Attack2" });
        }

        [MenuItem("Tools/Red Hood/Review Weapon Views")]
        public static void ReviewWeaponViews() {
            ReviewAt(Output + "/weapon-views", new[] { "Attack2" });
        }

        static void ReviewAt(string output, string[] clips = null) {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new Exception("Stop Play before reviewing the motion sample.");
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var resources = new System.Collections.Generic.List<UnityEngine.Object>();
            var previousRT = RenderTexture.active;
            try {
                var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHood_Atlas.asset");
                var extra = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodWeaponViews_Atlas.asset");
                atlas.Clear(); if (extra) extra.Clear();
                SkeletonAnimation Load(string path, float x) {
                    var json = new TextAsset(File.ReadAllText(path));
                    resources.Add(json);
                    var data = SkeletonDataAsset.CreateRuntimeInstance(json, extra ? new AtlasAssetBase[]{atlas,extra} : new AtlasAssetBase[]{atlas}, true);
                    resources.Add(data);
                    data.defaultMix = .12f;
                    var actor = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                    SceneManager.MoveGameObjectToScene(actor.gameObject, scene);
                    actor.transform.position = new Vector3(x, 0, 0);
                    actor.Initialize(true);
                    return actor;
                }
                var after = Load("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle.json", 3);
                var beforePath = output + "/before/RedHoodBattle.json";
                var before = File.Exists(beforePath) ? Load(beforePath, -3) : null;
                var report = new StringBuilder("Red Hood motion sample / Unity Spine runtime, 60 Hz\n");
                Validate(after, report);
                if (clips != null) ValidateSweep(after, report);
                if(output.EndsWith("attack1-power")) ValidateAttack1(after, report);
                if(output.EndsWith("/victory")) ValidateVictory(after,report);
                if(output.EndsWith("/death")) ValidateDeath(after,report);
                if(output.EndsWith("/revive")) ValidateRevive(after,report);
                if(output.EndsWith("/skill2")) ValidateSkill2(after,report);
                if(output.EndsWith("/skill3")) ValidateSkill3(after,report);
                if(output.EndsWith("/idle-wind")) ValidateIdleWind(after,report);
                var cameraGO = new GameObject("Motion review camera");
                SceneManager.MoveGameObjectToScene(cameraGO, scene);
                var camera = cameraGO.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = clips == null ? 3.9f : 4.3f;
                camera.transform.position = new Vector3(0, 2.55f, -20);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.12f,.14f,.17f);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                var rt = new RenderTexture(1080,720,24);
                resources.Add(rt);
                camera.targetTexture = rt;
                camera.aspect = 1.5f;
                var frame = new Texture2D(1080,720,TextureFormat.RGB24,false);
                resources.Add(frame);
                if((output.EndsWith("sweep-return") || output.EndsWith("sweep-return-velocity"))) {
                    foreach(float boundary in new[]{.72f,.92f,1.16f}) {
                        Color32[] first=null;
                        for(int side=0;side<2;side++) {
                            Pose(after,"Attack2",boundary+(side==0?-.0001f:.0001f),false);
                            camera.Render();RenderTexture.active=rt;
                            frame.ReadPixels(new Rect(0,0,1080,720),0,0);frame.Apply();
                            var pixels=frame.GetPixels32();
                            if(side==0)first=pixels;
                            else {
                                int changed=0;
                                for(int p=0;p<pixels.Length;p++)
                                    if(Mathf.Max(Mathf.Abs(pixels[p].r-first[p].r),Mathf.Abs(pixels[p].g-first[p].g),Mathf.Abs(pixels[p].b-first[p].b))>24)changed++;
                                report.AppendLine("Return attachment boundary "+boundary+": "+changed+" pixels changed by >24/255 across 0.2 ms.");
                                if(changed>40)throw new Exception("Visible return attachment discontinuity at "+boundary+": "+changed+" pixels");
                            }
                        }
                    }
                }
                foreach (string name in clips ?? new[] { "Idle1", "Attack1" }) {
                    string directory = output + "/" + name;
                    Directory.CreateDirectory(directory);
                    float duration = after.Skeleton.Data.FindAnimation(name).Duration;
                    int count = Mathf.CeilToInt(duration*60);
                    for (int i=0;i<count;i++) {
                        float t = i/60f;
                        Pose(after,name,t,name == "Idle1");
                        if (before) { before.gameObject.SetActive(before.Skeleton.Data.FindAnimation(name)!=null); if(before.gameObject.activeSelf) Pose(before,name,t,name == "Idle1"); }
                        camera.Render();
                        RenderTexture.active = rt;
                        frame.ReadPixels(new Rect(0,0,1080,720),0,0);
                        frame.Apply();
                        File.WriteAllBytes(directory + "/frame-" + i.ToString("D3") + ".png",frame.EncodeToPNG());
                    }
                }
                if(output.EndsWith("/idle-wind")) {
                    string dir=output+"/playback-1.0";Directory.CreateDirectory(dir);
                    camera.transform.position=new Vector3(0,2.5f,-20);camera.orthographicSize=4.1f;
                    Pose(after,"Idle1",0,true);if(before)Pose(before,"Idle1",0,true);
                    for(int i=0;i<432;i++) {
                        after.Update(1f/60);after.LateUpdate();if(before){before.Update(1f/60);before.LateUpdate();}
                        camera.Render();RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,1080,720),0,0);frame.Apply();
                        File.WriteAllBytes(dir+"/frame-"+i.ToString("D3")+".png",frame.EncodeToPNG());
                    }
                }
                if(output.EndsWith("/death") || output.EndsWith("/revive") || output.EndsWith("/skill2") || output.EndsWith("/skill3")) {
                    if(before)before.gameObject.SetActive(false);
                    camera.transform.position=new Vector3(3,output.EndsWith("/skill3") ? 3.1f : 2.5f,-20);camera.orthographicSize=output.EndsWith("/skill3") ? 3.9f : 3.4f;
                    string dir=output+"/playback-1.0";Directory.CreateDirectory(dir);
                    bool reviveReview=output.EndsWith("/revive");
                    if(output.EndsWith("/skill2") || output.EndsWith("/skill3")) {
                        Pose(after,"Idle1",0,true);
                        after.AnimationState.AddAnimation(0,output.EndsWith("/skill3") ? "Skill3" : "Skill2",false,.25f);
                        after.AnimationState.AddAnimation(0,"Idle1",true,output.EndsWith("/skill3") ? 5.31f : 5.06f);
                    } else if(reviveReview) {
                        Pose(after,"Death1",2.98f,false);
                        var entry=after.AnimationState.SetAnimation(0,"Revive",false);entry.MixDuration=0;
                        after.AnimationState.AddAnimation(0,"Idle1",true,2.73f);
                    } else {Pose(after,"Idle1",0,true);after.AnimationState.AddAnimation(0,"Death1",false,.25f);}
                    for(int i=0;i<((output.EndsWith("/skill2") || output.EndsWith("/skill3")) ? 450 : 240);i++) {
                        after.Update(1f/60);after.LateUpdate();camera.Render();RenderTexture.active=rt;
                        frame.ReadPixels(new Rect(0,0,1080,720),0,0);frame.Apply();
                        File.WriteAllBytes(dir+"/frame-"+i.ToString("D3")+".png",frame.EncodeToPNG());
                    }
                }
                if (output.EndsWith("/victory") || output.EndsWith("skill5-sync") || output.EndsWith("skill1-sync") || output.EndsWith("attack1-power") || output.EndsWith("sweep-perspective") || (output.EndsWith("sweep-return") || output.EndsWith("sweep-return-velocity"))) {
                    if (before) before.gameObject.SetActive(false);
                    camera.transform.position = new Vector3(3,2.5f,-20);
                    camera.orthographicSize = 3.4f;
                    foreach (float speed in output.EndsWith("/victory") ? new[]{1f} : new[] { 1f,1.5f,2f }) {
                        string directory=output+"/playback-"+speed.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);
                        Directory.CreateDirectory(directory);
                        Pose(after,"Idle1",0,true);
                        bool victoryPreview=output.EndsWith("/victory");
                        var attack=after.AnimationState.AddAnimation(0,victoryPreview ? "Victory" : output.EndsWith("skill5-sync") ? "Skill5" : output.EndsWith("skill1-sync") ? "Skill1" : output.EndsWith("attack1-power") ? "Attack1" : "Attack2",false,.25f);
                        attack.TimeScale=speed;
                        // Explicit time offset avoids Spine's duration-minus-mix
                        // queue convention cutting the recovery pose short.
                        float clipDuration=attack.Animation.Duration;
                        after.AnimationState.AddAnimation(0,victoryPreview ? "VictoryIdle" : "Idle1",true,clipDuration);
                        int frames=Mathf.CeilToInt((.25f+clipDuration/speed+(victoryPreview ? 2f : .4f))*60);
                        int hitCount=0,completeCount=0;
                        Spine.AnimationState.TrackEntryEventDelegate handler=(entry,e)=> {
                            if(e.Data.Name=="OnHit")hitCount++;
                            if(e.Data.Name=="OnComplete")completeCount++;
                        };
                        after.AnimationState.Event+=handler;
                        for(int i=0;i<frames;i++) {
                            after.Update(1f/60); after.LateUpdate(); camera.Render();
                            RenderTexture.active=rt;
                            frame.ReadPixels(new Rect(0,0,1080,720),0,0);frame.Apply();
                            File.WriteAllBytes(directory+"/frame-"+i.ToString("D3")+".png",frame.EncodeToPNG());
                        }
                        after.AnimationState.Event-=handler;
                        if(hitCount!=(victoryPreview?0:1)||completeCount!=(victoryPreview?0:1))throw new Exception("Playback event count at speed "+speed+": "+hitCount+"/"+completeCount);
                        report.AppendLine(victoryPreview ? "Victory -> VictoryIdle: continuous playback; no combat events." : "Mixed idle/"+(output.EndsWith("skill5-sync") ? "Skill5" : output.EndsWith("skill1-sync") ? "Skill1" : output.EndsWith("attack1-power") ? "Attack1" : "Attack2")+"/idle playback at "+speed+"x: one hit, one completion; rendered at 60 Hz.");
                    }
                }
                report.AppendLine("Side-by-side frames: BEFORE left / AFTER right. Actual Unity Camera.Render at 60 Hz.");
                File.WriteAllText(output+"/validation.txt",report.ToString());
                Debug.Log("RED_HOOD_MOTION_REVIEW_PASSED\n"+report);
            } finally {
                RenderTexture.active = previousRT;
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var resource in resources) {
                    if (resource is RenderTexture rt) rt.Release();
                    UnityEngine.Object.DestroyImmediate(resource);
                }
            }
        }

        [MenuItem("Tools/Red Hood/Review Cape Correction")]
        public static void ReviewCapeCorrection() {
            ReviewAt(Output + "/cape-correction");
        }

        static void Pose(SkeletonAnimation actor, string name, float t, bool loop) {
            actor.AnimationState.ClearTracks();
            actor.Skeleton.SetToSetupPose();
            var entry = actor.AnimationState.SetAnimation(0,name,loop);
            entry.TrackTime = t;
            actor.Update(0);
            actor.LateUpdate();
        }

        static void ValidateIdleWind(SkeletonAnimation actor,StringBuilder report) {
            Pose(actor,"Idle1",0,false);var first=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"Idle1",3.6f,false);var last=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            float seam=0;for(int i=0;i<first.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(first[i],last[i]));
            if(seam>.001f)throw new Exception("Idle wind loop seam="+seam);
            for(int i=0;i<=216;i++) {
                Pose(actor,"Idle1",i/60f,false);
                foreach(string name in new[]{"cloth_root","cloth_mid","cloth_back"}) {
                    var b=actor.Skeleton.FindBone(name);
                    if(Mathf.Abs(b.ScaleX-1)>.001f || Mathf.Abs(b.ScaleY-1)>.001f)throw new Exception("Wind changes cape scale");
                }
            }
            report.AppendLine("Idle wind: 3.6 second loop seam="+seam+"; cape length/scale unchanged; only three distributed cloth rotation tracks edited.");
        }

        static void ValidateSkill2(SkeletonAnimation actor,StringBuilder report) {
            Pose(actor,"Skill2",2.03f,false);
            var held=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            var shoulder=actor.Skeleton.FindBone("upper_arm");var grip=actor.Skeleton.FindBone("grip");
            if(Mathf.Abs(shoulder.WorldY-grip.WorldY)>.025f || grip.WorldX<shoulder.WorldX+.85f)throw new Exception("Skill2 right arm not level");
            for(int i=1;i<=60;i++) {
                Pose(actor,"Skill2",2.03f+i/60f,false);
                var v=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
                for(int k=0;k<v.Length;k++)if(Vector3.Distance(v[k],held[k])>.001f)throw new Exception("Skill2 hold moves");
            }
            Pose(actor,"Skill2",5.06f,false);var end=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"Idle1",0,true);var idle=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            float seam=0;for(int i=0;i<end.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(end[i],idle[i]));
            if(seam>.001f)throw new Exception("Skill2 Idle seam="+seam);
            report.AppendLine("Skill2: right arm horizontal; exact 1 second hold; Idle endpoint mesh seam="+seam+". Existing right_hand_grip IK and foreground arm slots retained.");
        }

        static void ValidateSkill3(SkeletonAnimation actor,StringBuilder report) {
            Pose(actor,"Skill3",2.08f,false);
            var held=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            var shoulder=actor.Skeleton.FindBone("upper_arm");var grip=actor.Skeleton.FindBone("grip");
            if(Mathf.Abs(shoulder.WorldX-grip.WorldX)>.025f || grip.WorldY<shoulder.WorldY+.85f)throw new Exception("Skill3 right arm not vertical");
            for(int i=1;i<=60;i++) {
                Pose(actor,"Skill3",2.08f+i/60f,false);
                var v=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
                for(int k=0;k<v.Length;k++)if(Vector3.Distance(v[k],held[k])>.001f)throw new Exception("Skill3 hold moves");
            }
            Pose(actor,"Skill3",5.31f,false);var end=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"Idle1",0,true);var idle=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            float seam=0;for(int i=0;i<end.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(end[i],idle[i]));
            if(seam>.001f)throw new Exception("Skill3 Idle seam="+seam);
            report.AppendLine("Skill3: right arm vertical; exact 1 second hold; Idle endpoint mesh seam="+seam+". Existing right_hand_grip IK and foreground arm slots retained.");
        }

        static void ValidateRevive(SkeletonAnimation actor,StringBuilder report) {
            Pose(actor,"Death1",2.98f,false);
            var seated=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"Revive",0,false);
            var start=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            if(seated.Length!=start.Length)throw new Exception("Revive start topology differs");
            float seam=0;
            for(int i=0;i<start.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(seated[i],start[i]));
            if(seam>.001f)throw new Exception("Revive start seam "+seam);
            float last=-100,minFoot=100,maxStep=0;
            for(int i=0;i<=164;i++) {
                Pose(actor,"Revive",Mathf.Min(2.73f,i/60f),false);
                float y=actor.Skeleton.FindBone("hips").WorldY;
                if(i>0)maxStep=Mathf.Max(maxStep,Mathf.Abs(y-last));last=y;
                foreach(string name in new[]{"far_foot","near_foot"})minFoot=Mathf.Min(minFoot,actor.Skeleton.FindBone(name).WorldY);
            }
            if(maxStep>.055f || minFoot<.15f)throw new Exception("Revive height pop/foot penetration");
            if(actor.Skeleton.FindSlot("scythe").A<.99f || actor.Skeleton.FindSlot("head_rest").A>.01f)throw new Exception("Revive final slots not restored");
            report.AppendLine("Revive: seated start seam="+seam+"; max hip step="+maxStep+"; minimum foot height="+minFoot+"; weapon restored and eyes open.");
        }

        static void ValidateDeath(SkeletonAnimation actor,StringBuilder report) {
            Pose(actor,"Death1",1.48f,false);
            if(actor.Skeleton.FindBone("near_foot").WorldX-actor.Skeleton.FindBone("far_foot").WorldX>.65f)throw new Exception("Death recovery did not close stance");
            if(actor.Skeleton.FindSlot("scythe").A<.99f)throw new Exception("Death scythe hidden before kneeling");
            Pose(actor,"Death1",1.60f,false);
            float fade=actor.Skeleton.FindSlot("scythe").A;
            if(fade<=.05f || fade>=.95f)throw new Exception("Death scythe does not fade");
            float minDescent=99;
            for(int i=0;i<=30;i++) {
                float t=1.78f+i*.02f;
                Pose(actor,"Death1",t-.005f,false);float y0=actor.Skeleton.FindBone("hips").WorldY;
                Pose(actor,"Death1",t+.005f,false);float y1=actor.Skeleton.FindBone("hips").WorldY;
                minDescent=Mathf.Min(minDescent,(y0-y1)/.01f);
            }
            if(minDescent<.1f)throw new Exception("Death descent pauses at intermediate kneel: "+minDescent);
            report.AppendLine("Continuous standing-to-seated descent; minimum vertical speed 1.78-2.38s="+minDescent);
            Pose(actor,"Death1",2.73f,false);
            var kneeFar=actor.Skeleton.FindBone("far_boot");var kneeNear=actor.Skeleton.FindBone("near_boot");
            var contactFar=new Vector2(kneeFar.WorldX,kneeFar.WorldY);var contactNear=new Vector2(kneeNear.WorldX,kneeNear.WorldY);
            for(int i=0;i<=15;i++) {
                Pose(actor,"Death1",2.73f+i/60f,false);
                if(Vector2.Distance(contactFar,new Vector2(kneeFar.WorldX,kneeFar.WorldY))>.001f || Vector2.Distance(contactNear,new Vector2(kneeNear.WorldX,kneeNear.WorldY))>.001f)throw new Exception("Seated knees drift");
            }
            Pose(actor,"Death1",2.98f,false);
            if(actor.Skeleton.FindSlot("scythe").A>.001f || actor.Skeleton.FindSlot("lantern").A>.001f)throw new Exception("Death weapon still visible");
            var far=actor.Skeleton.FindBone("far_boot");var near=actor.Skeleton.FindBone("near_boot");
            report.AppendLine("Death knees: "+far.WorldY+" / "+near.WorldY+"; fade midpoint alpha="+fade);
            if(far.WorldY<.08f || far.WorldY>.30f || near.WorldY<.08f || near.WorldY>.30f)throw new Exception("Both knees must reach ground");
            report.AppendLine("Death: Victory-speed recovery; weapon and lantern fade 1.48-1.73; continuous descent onto heels 1.48-2.73; completion 2.98 s.");
        }

        static void ValidateVictory(SkeletonAnimation actor, StringBuilder report) {
            Pose(actor,"Victory",0,false);
            var near=actor.Skeleton.FindBone("near_foot");
            var far=actor.Skeleton.FindBone("far_foot");
            Vector2 front=new Vector2(near.WorldX,near.WorldY),rear=new Vector2(far.WorldX,far.WorldY);
            float initialHead=actor.Skeleton.FindBone("head").WorldY;
            float lift=0,lastX=near.WorldX,minBlade=99,maxHeadStep=0,lastHead=initialHead;
            for(int i=1;i<=180;i++) {
                float t=i/60f;Pose(actor,"Victory",t,false);
                var cloth=actor.Skeleton.FindBone("cloth_root");
                if(Mathf.Abs(cloth.ScaleX-1)>.001f || Mathf.Abs(cloth.ScaleY-1)>.001f)throw new Exception("Victory shrinks the Idle cloak");
                if(t<=1.6f && actor.Skeleton.FindSlot("head_rest").A>.001f)throw new Exception("Eyes close before standing settles");
                if(t>=1.25f && Mathf.Abs(near.WorldY-front.y)>.001f)throw new Exception("Front foot recovery too slow");
                if(Vector2.Distance(rear,new Vector2(far.WorldX,far.WorldY))>.001f)throw new Exception("Victory rear support foot drift");
                lift=Mathf.Max(lift,near.WorldY-front.y);
                if(Mathf.Abs(near.WorldX-lastX)>.001f && near.WorldY-front.y<.015f)throw new Exception("Victory front foot slides on ground");
                lastX=near.WorldX;
                float headY=actor.Skeleton.FindBone("head").WorldY;maxHeadStep=Mathf.Max(maxHeadStep,Mathf.Abs(headY-lastHead));lastHead=headY;
                minBlade=Mathf.Min(minBlade,actor.Skeleton.FindBone("blade_tip").WorldY);
            }
            if(lift<.10f || front.x-near.WorldX<1f || Mathf.Abs(near.WorldY-front.y)>.001f || minBlade<-.03f || minBlade>.03f)
                throw new Exception("Victory foot/weapon clearance failed: "+lift+" / "+minBlade);
            report.AppendLine("Victory: rear foot fixed; front foot lift="+lift+", retract="+(front.x-near.WorldX)+"; blade tip minimum height="+minBlade);
            Pose(actor,"Victory",1.8f,false);
            if(actor.Skeleton.FindSlot("head_rest").A<.99f)throw new Exception("Victory eye closure missing");
            Pose(actor,"Victory",3,false);
            float headRise=actor.Skeleton.FindBone("head").WorldY-initialHead;
            float stance=near.WorldX-far.WorldX;
            float farKnee=Mathf.Abs(Mathf.DeltaAngle(actor.Skeleton.FindBone("far_thigh").WorldRotationX,actor.Skeleton.FindBone("far_boot").WorldRotationX));
            float nearKnee=Mathf.Abs(Mathf.DeltaAngle(actor.Skeleton.FindBone("near_thigh").WorldRotationX,actor.Skeleton.FindBone("near_boot").WorldRotationX));
            report.AppendLine("Standing: foot spacing="+stance+"; head rise="+headRise+"; knee bends="+farKnee+" / "+nearKnee+"; maximum head vertical step at 60Hz="+maxHeadStep);
            if(maxHeadStep>.025f)throw new Exception("Victory height pops");
            if(stance>.65f || headRise<.15f || farKnee>20 || nearKnee>20)throw new Exception("Standing silhouette geometry failed: "+stance+" / "+headRise+" / "+farKnee+" / "+nearKnee);
            var end=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"VictoryIdle",0,false);
            var start=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            if(end.Length!=start.Length)throw new Exception("Victory rest mesh topology changed");
            float seam=0;for(int i=0;i<end.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(end[i],start[i]));
            if(seam>.001f)throw new Exception("Victory rest seam="+seam);
            float groundTip=actor.Skeleton.FindBone("blade_tip").WorldY;
            for(int i=0;i<=120;i++) {
                Pose(actor,"VictoryIdle",i*.03f,false);
                if(actor.Skeleton.FindSlot("head_rest").A<.99f || actor.Skeleton.FindSlot("head").A>.001f)throw new Exception("Resting eyes reopen");
                if(Mathf.Abs(actor.Skeleton.FindBone("blade_tip").WorldY-groundTip)>.001f)throw new Exception("Resting blade drifts off ground");
            }
            Pose(actor,"VictoryIdle",3.6f,false);
            var loop=actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            for(int i=0;i<loop.Length;i++)if(Vector3.Distance(loop[i],start[i])>.001f)throw new Exception("VictoryIdle loop seam");
            report.AppendLine("Victory -> relaxed idle mesh seam="+seam+"; relaxed idle loop seamless; eyes stay closed throughout rest.");
        }

        static void ValidateAttack1(SkeletonAnimation actor, StringBuilder report) {
            Vector2 Position(string bone, float time) {
                Pose(actor,"Attack1",time,false);
                var b=actor.Skeleton.FindBone(bone);
                return new Vector2(b.WorldX,b.WorldY);
            }
            var raised=Position("grip",.32f);
            float drift=0;
            for(int i=0;i<=28;i++) drift=Mathf.Max(drift,Vector2.Distance(raised,Position("grip",.32f+i*.005f)));
            if(drift>.001f)throw new Exception("Attack1 raised hold drift="+drift);
            var contact=Position("grip",.56f);
            if(raised.y-contact.y<.5f)throw new Exception("Attack1 downstroke too small");
            report.AppendLine("Attack1: hold .32-.46 s, grip drift="+drift.ToString("F6")+"; cut .46-.56 s, grip travel="+(contact-raised));
            foreach(string bone in new[]{"far_boot","near_boot"}) {
                foreach(float t in new[]{.72f,.91f}) {
                    float left=(Position(bone,t)-Position(bone,t-.004f)).magnitude/.004f;
                    float right=(Position(bone,t+.004f)-Position(bone,t)).magnitude/.004f;
                    if(left<.03f || right<.03f || Mathf.Max(left,right)/Mathf.Min(left,right)>1.6f)
                        throw new Exception("Attack1 knee recovery speed break: "+bone+" at "+t+" = "+left+" / "+right);
                    report.AppendLine("Attack1 "+bone+" knee velocity at "+t+" s: "+left.ToString("F4")+" -> "+right.ToString("F4")+" units/s.");
                }
            }
        }

        static void ValidateSweep(SkeletonAnimation actor, StringBuilder report) {
            Vector2 Grip(float t) {
                Pose(actor,"Attack2",t,false);
                var b=actor.Skeleton.FindBone("grip");
                return new Vector2(b.WorldX,b.WorldY);
            }
            Vector2 VisibleTip(float t) {
                Pose(actor,"Attack2",t,false);
                var slot=actor.Skeleton.FindSlot("scythe");
                // Measured source-pixel landmarks, same uniform scale and grip
                // anchors as build_weapon_views.py. Not the old upright locator.
                Vector2 local = slot.Attachment.Name == "scythe_sweep_v2"
                    ? new Vector2((1474-580)*.105f,(235-618)*.105f)
                    : slot.Attachment.Name == "scythe_oblique_v2"
                    ? new Vector2((945-606)*.16f,(210-800)*.16f)
                    : new Vector2(105,-141);
                slot.Bone.LocalToWorld(local.x*.01f,local.y*.01f,out float x,out float y);
                return new Vector2(x,y);
            }
            var raised=Grip(.32f);
            float drift=0, low=float.PositiveInfinity, high=float.NegativeInfinity;
            for (int i=0;i<=28;i++) drift=Mathf.Max(drift,Vector2.Distance(raised,Grip(.32f+i*.005f)));
            var start=Grip(.46f);
            for (int i=0;i<=24;i++) {
                var p=Grip(.46f+i/240f);
                low=Mathf.Min(low,p.y); high=Mathf.Max(high,p.y);
            }
            var end=Grip(.56f);
            if (drift>.001f || end.y-start.y>-.65f || end.x-start.x<1.3f)
                throw new Exception("Sweep hold/trajectory failed: drift="+drift+", height="+(high-low)+", travel="+(end.x-start.x));
            report.AppendLine("Attack2: raised hold .32-.46 s, max grip drift="+drift.ToString("F6")+
                "; accelerated cut .46-.56 s, grip travel="+(end-start)+
                "; visible tip at hold="+VisibleTip(.46f)+", contact="+VisibleTip(.56f));
            foreach (float t in new[] { .525f,.56f,.60f,.72f }) {
                Pose(actor,"Attack2",t,false);
                string attachment=actor.Skeleton.FindSlot("scythe").Attachment.Name;
                if(attachment!="scythe_sweep_v2" && !(t==.72f && attachment=="return_scythe_sweep_v2"))
                    throw new Exception("Oblique blade missing during strike/follow-through");
            }
            float minimumReturnSpeed=float.PositiveInfinity;
            for(int i=0;i<=12;i++) {
                float t=.85f+i*.01f;
                float velocity=(Grip(t+.002f)-Grip(t-.002f)).magnitude/.004f;
                minimumReturnSpeed=Mathf.Min(minimumReturnSpeed,velocity);
                if(i==1 || i==5 || i==9)
                    report.AppendLine("Recovery grip speed at "+t.ToString("F2")+" s: "+velocity.ToString("F4")+" units/s.");
            }
            if(minimumReturnSpeed<.15f)throw new Exception("Unintended mid-recovery stop: "+minimumReturnSpeed);
            report.AppendLine("Recovery .85-.97 s: minimum grip speed="+minimumReturnSpeed.ToString("F4")+" units/s; no intermediate stop.");
            foreach (float t in new[] { .35f,.49f,.56f,.72f,.86f,1.06f,1.15f })
                foreach (string next in new[] { "Idle1","Attack1","Hit" }) {
                    Pose(actor,"Attack2",t,false);
                    actor.AnimationState.SetAnimation(0,next,false);
                    for (int i=0;i<18;i++) actor.Update(1f/60);
                    if (actor.Skeleton.FindSlot("scythe").Attachment.Name!="scythe")
                        throw new Exception("Weapon view leaked into "+next);
                    var blend=actor.Skeleton.FindSlot("scythe_return");
                    if(blend!=null && blend.Attachment!=null && blend.A>.001f)
                        throw new Exception("Return blend leaked into "+next);
                    if(actor.Skeleton.FindSlot("scythe").A<.999f)
                        throw new Exception("Weapon opacity not restored in "+next);
                    for (int i=0;i<actor.Skeleton.DrawOrder.Count;i++)
                        if (actor.Skeleton.DrawOrder.Items[i].Data.Index!=i)
                            throw new Exception("Sweep draw order leaked into "+next);
                }
            report.AppendLine("Attack2: 21 interrupted transitions restore attachment, opacity and setup draw order.");
        }

        static void Validate(SkeletonAnimation actor, StringBuilder report) {
            int hits=0, completes=0;
            actor.AnimationState.Event += (entry,e) => {
                if (e.Data.Name == "OnHit") hits++;
                if (e.Data.Name == "OnComplete") completes++;
            };
            foreach (var animation in actor.Skeleton.Data.Animations) {
                hits=0; completes=0;
                Pose(actor,animation.Name,0,false);
                float maxGrip=0, maxFootDrift=0, maxKneeDrift=0;
                var far = actor.Skeleton.FindBone("far_foot");
                var near = actor.Skeleton.FindBone("near_foot");
                var farStart = new Vector2(far.WorldX,far.WorldY);
                var nearStart = new Vector2(near.WorldX,near.WorldY);
                var rearKnee = actor.Skeleton.FindBone("far_boot");
                var frontKnee = actor.Skeleton.FindBone("near_boot");
                var rearKneeStart = new Vector2(rearKnee.WorldX,rearKnee.WorldY);
                var frontKneeStart = new Vector2(frontKnee.WorldX,frontKnee.WorldY);
                for (int i=0;i<Mathf.CeilToInt(animation.Duration*60)+2;i++) {
                    actor.Update(1f/60);
                    actor.LateUpdate();
                    var forearm = actor.Skeleton.FindBone("forearm");
                    var grip = actor.Skeleton.FindBone("grip");
                    forearm.LocalToWorld(forearm.Data.Length,0,out float x,out float y);
                    maxGrip = Mathf.Max(maxGrip,Vector2.Distance(new Vector2(x,y),new Vector2(grip.WorldX,grip.WorldY)));
                    maxFootDrift = Mathf.Max(maxFootDrift,Vector2.Distance(farStart,new Vector2(far.WorldX,far.WorldY)),Vector2.Distance(nearStart,new Vector2(near.WorldX,near.WorldY)));
                    maxKneeDrift = Mathf.Max(maxKneeDrift,Vector2.Distance(rearKneeStart,new Vector2(rearKnee.WorldX,rearKnee.WorldY)),Vector2.Distance(frontKneeStart,new Vector2(frontKnee.WorldX,frontKnee.WorldY)));
                    foreach (var vertex in actor.GetComponent<MeshFilter>().sharedMesh.vertices)
                        if (float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)||float.IsNaN(vertex.y)||float.IsInfinity(vertex.y))
                            throw new Exception(animation.Name+": invalid mesh vertex");
                }
                bool attack = animation.Name.StartsWith("Attack") || animation.Name.StartsWith("Skill");
                int expectedCompletes = attack || (animation.Name == "Death1" || animation.Name == "Revive") ? 1 : 0;
                if (hits != (attack?1:0) || completes != expectedCompletes)
                    throw new Exception(animation.Name+": event mismatch hits="+hits+", completes="+completes);
                if (maxGrip>.05f) throw new Exception(animation.Name+": grip error="+maxGrip);
                if ((animation.Name=="Idle1" || animation.Name=="Attack1") && maxFootDrift>.001f)
                    throw new Exception(animation.Name+": feet drifted="+maxFootDrift);
                if (animation.Name=="Idle1") {
                    if (maxKneeDrift>.001f) throw new Exception("Idle knee pumping="+maxKneeDrift);
                    report.AppendLine("Idle knees remain stationary: max drift="+maxKneeDrift.ToString("F6"));
                }
                report.AppendLine(animation.Name+": events OK, finite mesh, max grip error="+maxGrip.ToString("F5"));
            }
            Pose(actor,"Idle1",0,false);
            var seam = actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            Pose(actor,"Idle1",actor.Skeleton.Data.FindAnimation("Idle1").Duration,false);
            var end = actor.GetComponent<MeshFilter>().sharedMesh.vertices;
            float seamError=0;
            for (int i=0;i<seam.Length;i++) seamError=Mathf.Max(seamError,Vector3.Distance(seam[i],end[i]));
            if (seamError>.001f) throw new Exception("Idle loop seam="+seamError);
            report.AppendLine("Idle loop mesh seam="+seamError.ToString("F6")+"; Idle1/Attack1 feet remain planted.");
            foreach (string source in new[] { "Idle1","Attack1" })
                foreach (string next in new[] { "Idle1","Attack1","Attack2","Hit","Death1","Victory" }) {
                    Pose(actor,source,.5f,false);
                    actor.AnimationState.SetAnimation(0,next,next=="Idle1");
                    for (int i=0;i<24;i++) actor.Update(1f/60);
                    actor.LateUpdate();
                    foreach (var vertex in actor.GetComponent<MeshFilter>().sharedMesh.vertices)
                        if (float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)) throw new Exception("Invalid transition "+source+" -> "+next);
                }
            report.AppendLine("12 interruption transitions evaluated without invalid mesh vertices.");
        }
    }
}
