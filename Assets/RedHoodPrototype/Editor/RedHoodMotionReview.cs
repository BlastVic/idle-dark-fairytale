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
    public static class RedHoodMotionReview {
        const string Output = "output/red-hood-motion";

        [MenuItem("Tools/Red Hood/Review Motion Sample")]
        public static void Review() {
            ReviewAt(Output);
        }

        [MenuItem("Tools/Red Hood/Review Idle Correction")]
        public static void ReviewIdleCorrection() {
            ReviewAt(Output + "/idle-correction");
        }

        static void ReviewAt(string output) {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new Exception("Stop Play before reviewing the motion sample.");
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var resources = new System.Collections.Generic.List<UnityEngine.Object>();
            var previousRT = RenderTexture.active;
            try {
                var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>("Assets/RedHoodPrototype/RedHood_Atlas.asset");
                SkeletonAnimation Load(string path, float x) {
                    var json = new TextAsset(File.ReadAllText(path));
                    resources.Add(json);
                    var data = SkeletonDataAsset.CreateRuntimeInstance(json, atlas, true);
                    resources.Add(data);
                    data.defaultMix = .12f;
                    var actor = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                    SceneManager.MoveGameObjectToScene(actor.gameObject, scene);
                    actor.transform.position = new Vector3(x, 0, 0);
                    actor.Initialize(true);
                    return actor;
                }
                var after = Load("Assets/DarkFairytale/RedHoodBattle.json", 3);
                var beforePath = output + "/before/RedHoodBattle.json";
                var before = File.Exists(beforePath) ? Load(beforePath, -3) : null;
                var report = new StringBuilder("Red Hood motion sample / Unity Spine runtime, 60 Hz\n");
                Validate(after, report);
                var cameraGO = new GameObject("Motion review camera");
                SceneManager.MoveGameObjectToScene(cameraGO, scene);
                var camera = cameraGO.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 3.9f;
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
                foreach (string name in new[] { "Idle1", "Attack1" }) {
                    string directory = output + "/" + name;
                    Directory.CreateDirectory(directory);
                    float duration = after.Skeleton.Data.FindAnimation(name).Duration;
                    int count = Mathf.CeilToInt(duration*60);
                    for (int i=0;i<count;i++) {
                        float t = i/60f;
                        Pose(after,name,t,name == "Idle1");
                        if (before) Pose(before,name,t,name == "Idle1");
                        camera.Render();
                        RenderTexture.active = rt;
                        frame.ReadPixels(new Rect(0,0,1080,720),0,0);
                        frame.Apply();
                        File.WriteAllBytes(directory + "/frame-" + i.ToString("D3") + ".png",frame.EncodeToPNG());
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
                int expectedCompletes = attack || animation.Name == "Death1" ? 1 : 0;
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
