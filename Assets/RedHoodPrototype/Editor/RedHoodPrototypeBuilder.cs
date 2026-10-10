using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Spine.Unity;

namespace RedHoodPrototype.Editor {
    public static class RedHoodPrototypeBuilder {
        const string Root = "Assets/RedHoodPrototype/";

        [MenuItem("Tools/Red Hood/Validate Current Scene %#F8")]
        public static void ValidateCurrentScene() {
            if (Application.isPlaying) throw new Exception("Stop Play before frame validation.");
            var character = UnityEngine.Object.FindFirstObjectByType<SkeletonAnimation>();
            var camera = Camera.main;
            if (!character || !camera || !character.skeletonDataAsset.name.StartsWith("RedHood"))
                throw new Exception("Open RedHoodAnimationTest before validation.");
            character.Initialize(true);
            ValidateAndRender(character, camera);
            character.AnimationState.ClearTracks();
            character.Skeleton.SetToSetupPose();
            character.Update(0);
            character.LateUpdate();
            Debug.Log("RED_HOOD_MAIN_VALIDATION_PASSED");
        }

        [MenuItem("Tools/Red Hood/Rebuild Test Scene %#F9")]
        public static void Build() {
            // Ask Unity to preserve user scene changes before an explicit menu rebuild.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // The bundled Spine importer discovers pages after blank lines.
            string atlasPath = Root + "RedHood.atlas.txt";
            string atlasText = File.ReadAllText(atlasPath);
            if (!atlasText.StartsWith("\n") && !atlasText.StartsWith("\r\n")) {
                File.WriteAllText(atlasPath, "\n" + atlasText);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "RedHood.png");
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "RedHood.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "RedHood_Material.mat");
            if (!material) {
                material = new Material(Shader.Find("Spine/Skeleton"));
                AssetDatabase.CreateAsset(material, Root + "RedHood_Material.mat");
            }
            material.mainTexture = texture;
            material.SetFloat("_StraightAlphaInput", 1);
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            EditorUtility.SetDirty(material);
            var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Root + "RedHood_Atlas.asset");
            if (!atlas) {
                atlas = ScriptableObject.CreateInstance<SpineAtlasAsset>();
                AssetDatabase.CreateAsset(atlas, Root + "RedHood_Atlas.asset");
            }
            atlas.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "RedHood.atlas.txt");
            atlas.materials = new[] { material };
            atlas.Clear();
            EditorUtility.SetDirty(atlas);
            var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root + "RedHood_SkeletonData.asset");
            if (!data) {
                data = ScriptableObject.CreateInstance<SkeletonDataAsset>();
                AssetDatabase.CreateAsset(data, Root + "RedHood_SkeletonData.asset");
            }
            data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "RedHood.json");
            data.atlasAssets = new AtlasAssetBase[] { atlas };
            data.scale = 0.01f;
            data.defaultMix = 0.12f;
            data.Clear();
            EditorUtility.SetDirty(data);
            if (data.GetSkeletonData(false) == null) throw new Exception("Red Hood skeleton failed to load");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.transform.position = new Vector3(0, 2.2f, -20);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.19f, 0.23f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            var character = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            character.name = "Red Hood (Reference Side View / New Rig)";
            character.transform.localScale = Vector3.one;
            character.initialSkinName = "default";
            character.AnimationName = "Idle1";
            character.loop = true;
            character.Update(0);
            character.LateUpdate();
            PrefabUtility.SaveAsPrefabAsset(character.gameObject, Root + "RedHood.prefab");
            var controls = new GameObject("Animation Preview Controls").AddComponent<RedHoodAnimationPreview>();
            controls.character = character;
            EditorSceneManager.SaveScene(scene, Root + "RedHoodAnimationTest.unity");
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode) ValidateAndRender(character, camera);
            Debug.Log("RED_HOOD_PROTOTYPE_READY: " + Root + "RedHoodAnimationTest.unity");
        }

        static void ValidateAndRender(SkeletonAnimation character, Camera camera) {
            string output = Path.GetFullPath("output/red-hood-prototype");
            Directory.CreateDirectory(output);
            string report = "Reference side-view rig / Spine runtime animation validation\n";
            int hits = 0;
            int completes = 0;
            character.AnimationState.Event += (entry, e) => { if (e.Data.Name == "OnHit") hits++; };
            character.AnimationState.Event += (entry, e) => { if (e.Data.Name == "OnComplete") completes++; };
            foreach (var animation in character.Skeleton.Data.Animations) {
                hits = 0;
                completes = 0;
                float maxGripError = 0;
                character.AnimationState.ClearTracks();
                character.Skeleton.SetToSetupPose();
                character.AnimationState.SetAnimation(0, animation.Name, false);
                character.Update(0);
                float initialWristY = character.Skeleton.FindBone("grip").WorldY;
                float maxWristLift = 0;
                float maxBladeY = 0;
                float maxFootDrift = 0;
                float maxRearKneeCross = float.NegativeInfinity;
                float sideBladeStart = 0;
                float sideBladeContact = 0;
                float cutMinY = float.PositiveInfinity, cutMaxY = float.NegativeInfinity;
                var farFoot = character.Skeleton.FindBone("far_foot");
                var nearFoot = character.Skeleton.FindBone("near_foot");
                var initialFarFoot = new Vector2(farFoot.WorldX, farFoot.WorldY);
                var initialNearFoot = new Vector2(nearFoot.WorldX, nearFoot.WorldY);
                int steps = Mathf.CeilToInt(animation.Duration * 60) + 2;
                for (int i = 0; i < steps; i++) {
                    character.Update(1f/60f);
                    character.LateUpdate();
                    // Facing-right knees must bend forward, including while the
                    // hips compress over a fixed rear foot during anticipation.
                    var thigh = character.Skeleton.FindBone("far_thigh");
                    var shin = character.Skeleton.FindBone("far_boot");
                    float ankleX, ankleY;
                    shin.LocalToWorld(shin.Data.Length, 0, out ankleX, out ankleY);
                    float kneeCross = (shin.WorldX - thigh.WorldX) * (ankleY - shin.WorldY)
                        - (shin.WorldY - thigh.WorldY) * (ankleX - shin.WorldX);
                    maxRearKneeCross = Mathf.Max(maxRearKneeCross, kneeCross);
                    if (kneeCross > 0.0001f)
                        throw new Exception("Rear knee bent backwards: " + animation.Name + " at sample " + i);
                    var hand = character.Skeleton.FindBone("forearm");
                    var grip = character.Skeleton.FindBone("grip");
                    maxWristLift = Mathf.Max(maxWristLift, grip.WorldY - initialWristY);
                    float bladeX, bladeY;
                    var bladeTip = character.Skeleton.FindBone("blade_tip");
                    bladeX = bladeTip.WorldX;
                    bladeY = bladeTip.WorldY;
                    if (animation.Name == "Attack2" && character.Skeleton.FindBone("scythe_projection").ScaleX < .99f)
                        throw new Exception("Attack2 compressed or mirrored the rigid blade.");
                    maxBladeY = Mathf.Max(maxBladeY, bladeY);
                    if (i == 25) sideBladeStart = bladeX;
                    if (i == 33) sideBladeContact = bladeX;
                    if (i >= 25 && i <= 33) {
                        cutMinY = Mathf.Min(cutMinY, bladeY);
                        cutMaxY = Mathf.Max(cutMaxY, bladeY);
                    }
                    maxFootDrift = Mathf.Max(maxFootDrift, Vector2.Distance(initialFarFoot, new Vector2(farFoot.WorldX, farFoot.WorldY)), Vector2.Distance(initialNearFoot, new Vector2(nearFoot.WorldX, nearFoot.WorldY)));
                    float wristX, wristY;
                    hand.LocalToWorld(hand.Data.Length, 0, out wristX, out wristY);
                    float gripError = Vector2.Distance(new Vector2(wristX, wristY), new Vector2(grip.WorldX, grip.WorldY));
                    maxGripError = Mathf.Max(maxGripError, gripError);
                    if (gripError > 0.05f) throw new Exception("Hand lost weapon grip: " + animation.Name + " / " + gripError);
                    foreach (var v in character.GetComponent<MeshFilter>().sharedMesh.vertices)
                        if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y))
                            throw new Exception("Invalid vertex: " + animation.Name);
                }
                bool attack = animation.Name.StartsWith("Attack");
                if (animation.Name == "Attack1" && (maxWristLift < 0.95f || maxBladeY < 4.25f))
                    throw new Exception("Attack1 lost overhead silhouette.");
                if (attack && maxFootDrift > 0.001f)
                    throw new Exception("Attack lost planted stance: " + animation.Name);
                if (animation.Name == "Attack2" && (sideBladeContact - sideBladeStart < 1.8f || cutMaxY-cutMinY > .40f || Mathf.Abs(animation.Duration - 1.18f) > .001f))
                    throw new Exception("Attack2 lost horizontal cut: travel=" + (sideBladeContact-sideBladeStart) + ", height=" + (cutMaxY-cutMinY));
                int expectedHits = attack || animation.Name == "Skill1" ? 1 : 0;
                if (hits != expectedHits || completes != (attack || animation.Name == "Skill1" ? 1 : 0))
                    throw new Exception("Unexpected combat event count: " + animation.Name);
                report += animation.Name + ": " + steps + " frames sampled, hits=" + hits + ", completes=" + completes + ", max grip error=" + maxGripError.ToString("F5") + "\n";
                report += "  rear knee max signed cross=" + maxRearKneeCross.ToString("F5") + " (must be <= 0)\n";
                if (attack) report += "  wrist lift=" + maxWristLift.ToString("F3") + ", blade apex=" + maxBladeY.ToString("F3") + ", foot target drift=" + maxFootDrift.ToString("F5") + " Unity units\n";
                if (animation.Name == "Attack2") report += "  blade horizontal travel=" + (sideBladeContact-sideBladeStart).ToString("F3") + ", cutting height variation=" + (cutMaxY-cutMinY).ToString("F3") + " Unity units, duration=" + animation.Duration.ToString("F2") + " s\n";
            }
            // Interrupted attacks must not leave the sweep pose or far-side
            // draw order on the next animation after the normal mix completes.
            foreach (string next in new[] { "Idle1", "Attack1" }) {
                foreach (float interruptTime in new[] { .34f, .56f, .72f, .94f }) {
                    character.AnimationState.ClearTracks();
                    character.Skeleton.SetToSetupPose();
                    character.AnimationState.SetAnimation(0, "Attack2", false);
                    character.Update(interruptTime);
                    character.AnimationState.SetAnimation(0, next, false);
                    for (int i = 0; i < 18; i++) character.Update(1f/60f);
                    var projection = character.Skeleton.FindBone("scythe_projection");
                    if (Mathf.Abs(projection.ScaleX - 1) > .001f || Mathf.Abs(projection.ScaleY - 1) > .001f)
                        throw new Exception("Sweep projection leaked into " + next);
                    foreach (string slotName in new[] { "torso", "scythe" })
                        if (character.Skeleton.FindSlot(slotName).Attachment.Name != slotName)
                            throw new Exception("Turn attachment leaked into " + next);
                    for (int i = 0; i < character.Skeleton.DrawOrder.Count; i++)
                        if (character.Skeleton.DrawOrder.Items[i].Data.Index != i)
                            throw new Exception("Sweep draw order leaked into " + next);
                }
            }
            report += "Attack2 interruptions: 8 transitions to Idle1/Attack1 restored scale, attachments and draw order after mixing.\n";
            File.WriteAllText(Path.Combine(output, "validation.txt"), report);
            var rt = new RenderTexture(540,960,24);
            camera.targetTexture = rt;
            camera.aspect = 540f/960f;
            var frame = new Texture2D(540,960,TextureFormat.RGB24,false);
            int frameIndex = 0;
            foreach (string name in new[] { "Idle1", "Attack1", "Attack2", "Skill1", "Hit", "Death1", "Victory", "Rebirth" }) {
                character.AnimationState.ClearTracks();
                character.Skeleton.SetToSetupPose();
                var entry = character.AnimationState.SetAnimation(0,name,false);
                int count = name == "Idle1" ? 12 : 24;
                for (int i=0; i<count; i++) {
                    character.Update(i == 0 ? 0 : entry.Animation.Duration / (count-1));
                    character.LateUpdate();
                    camera.Render();
                    RenderTexture.active = rt;
                    frame.ReadPixels(new Rect(0,0,540,960),0,0);
                    frame.Apply();
                    File.WriteAllBytes(Path.Combine(output,"frame-" + frameIndex.ToString("D3") + ".png"),frame.EncodeToPNG());
                    frameIndex++;
                }
            }
            // Attack review at native 60 Hz: preserve the brief downstroke and
            // contact hold that the overview's 24 frames cannot resolve.
            foreach (string name in new[] { "Attack1", "Attack2" }) {
                character.AnimationState.ClearTracks();
                character.Skeleton.SetToSetupPose();
                var entry = character.AnimationState.SetAnimation(0, name, false);
                int count = Mathf.CeilToInt(entry.Animation.Duration * 60) + 1;
                for (int i = 0; i < count; i++) {
                    character.Update(i == 0 ? 0 : Mathf.Min(1f / 60, entry.Animation.Duration - (i-1)/60f));
                    character.LateUpdate();
                    camera.Render();
                    RenderTexture.active = rt;
                    frame.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                    frame.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-" + i.ToString("D3") + ".png"), frame.EncodeToPNG());
                }
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(frame);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
