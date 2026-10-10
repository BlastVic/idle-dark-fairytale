using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reskins of mon_7080/7081/7082, retaining each source rig and animation timelines.</summary>
public static class BlackForestMushroomBuilder
{
    public const string Root = "Assets/DarkFairytale/Monsters/BlackForestMushroom";
    const string Source = "ArtDirection/Monsters/BlackForestMushroom/source";
    public static readonly string[] Ids = { "Mob001", "Mob002", "Mob003" };
    static readonly string[] Sheets = { "wine-red", "moss-green", "moon-violet" };
    static readonly string[] SourceIds = { "mon_7080", "mon_7081", "mon_7082" };
    static string SheetPath(int i) => Source + "/original-rig/" + (i == 0 ? Sheets[i] : SourceIds[i] + "/painted") + ".png";
    static readonly string[] Parts = MushroomOriginalSkinSheet.Parts;
    const string Request = "output/monster-animation/build-mushrooms.request";

    [InitializeOnLoadMethod]
    static void ResumeRequestedBuild()
    {
        if (!File.Exists(Request)) return;
        EditorApplication.delayCall += () => {
            if (EditorApplication.isPlaying) return;
            File.Delete(Request);
            try { Build(); }
            catch (Exception e) { File.WriteAllText("output/monster-animation/mushroom-build-error.txt", e.ToString()); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Build Black Forest Mushrooms")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebuilding.");
        for (int i = 0; i < Ids.Length; i++)
            if (!File.Exists(SheetPath(i))) throw new FileNotFoundException("Missing skin: " + SheetPath(i));
        if (File.Exists(Request)) File.Delete(Request);
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory("output/monster-animation");
        for (int i = 0; i < Ids.Length; i++) BuildOne(i);
        RegisterEnemies();
        AddTestWave();
        RefreshSharedScene();
        AssetDatabase.SaveAssets();
        // Refresh prefab dependencies after all three atlas postprocessors have completed.
        foreach (var id in Ids)
            AssetDatabase.ImportAsset(Root + "/" + id + "/" + id + ".prefab", ImportAssetOptions.ForceUpdate);
        Validate();
        if (File.Exists("output/monster-animation/mushroom-build-error.txt")) File.Delete("output/monster-animation/mushroom-build-error.txt");
        Debug.Log("BLACK_FOREST_MUSHROOM_BUILD_PASSED");
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Update 7081 and 7082 Skins")]
    public static void UpdateSeparateSkins()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        for (int i = 1; i < Ids.Length; i++)
        {
            if (!File.Exists(SheetPath(i))) throw new FileNotFoundException("Missing painted skin: " + SheetPath(i));
            if (!File.Exists(Source + "/" + SourceIds[i] + "-rig-template.json"))
                throw new FileNotFoundException("Run build_rig.py before updating skins.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + Ids[i] + "/" + Ids[i] + ".prefab") == null)
                throw new InvalidOperationException("Existing prefab missing; use the full build first: " + Ids[i]);
        }
        Directory.CreateDirectory("output/monster-animation");
        for (int i = 1; i < Ids.Length; i++) BuildOne(i, true);
        // Preserve the independent mushroom prefabs and their stable SkeletonData GUIDs.
        AssetDatabase.SaveAssets();
        Validate();
        File.WriteAllText("output/monster-animation/separate-rigs-build.txt", "PASS: mon_7081 -> Moss, mon_7082 -> Moon; existing gameplay stats preserved.\n");
    }

    static void BuildOne(int variant, bool preservePrefab = false)
    {
        string id = Ids[variant], dir = Root + "/" + id;
        string export = Path.GetFullPath("../Spine/export/monster/" + id);
        string editable = Source + "/" + id;
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(export);
        Directory.CreateDirectory(editable + "/images");
        string sheetPath = SheetPath(variant);
        var sheet = new Texture2D(2,2,TextureFormat.RGBA32,false);
        bool reskinned = File.Exists(sheetPath);
        if(reskinned) sheet.LoadImage(File.ReadAllBytes(sheetPath));
        var crops = new List<Texture2D>();
        var rig = JObject.Parse(File.ReadAllText(Source + "/" + (variant == 0 ? "rig-template" : SourceIds[variant] + "-rig-template") + ".json"));
        for(int p=0;p<Parts.Length;p++)
        {
            var original = new Texture2D(2,2,TextureFormat.RGBA32,false);
            original.LoadImage(File.ReadAllBytes("../Spine/monster/"+SourceIds[variant]+"/images/"+Parts[p]+".png"));
            // The complete region canvas is invariant: no alpha cropping, mesh edits or mirroring.
            var crop = original;
            if(reskinned && Parts[p] != "shadow") {
                RectInt rect=MushroomOriginalSkinSheet.PartRect(p,original);
                crop=new Texture2D(original.width*2,original.height*2,TextureFormat.RGBA32,false);
                for(int y=0;y<crop.height;y++) for(int x=0;x<crop.width;x++)
                    crop.SetPixel(x,y,sheet.GetPixelBilinear((rect.x+(x+.5f)/crop.width*rect.width)/1536f,(rect.y+(y+.5f)/crop.height*rect.height)/1536f));
                crop.Apply(); UnityEngine.Object.DestroyImmediate(original);
            }
            crops.Add(crop);
            File.WriteAllBytes(editable+"/images/"+Parts[p]+".png",crop.EncodeToPNG());
        }
        var atlasTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Rect[] packed = atlasTexture.PackTextures(crops.ToArray(), 8, 2048, false);
        var atlas = new StringBuilder("\n" + id + ".png\nsize: " + atlasTexture.width + "," + atlasTexture.height + "\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for (int p = 0; p < Parts.Length; p++)
        {
            int x = Mathf.RoundToInt(packed[p].x * atlasTexture.width), y = Mathf.RoundToInt((1-packed[p].yMax)*atlasTexture.height);
            int w = Mathf.RoundToInt(packed[p].width*atlasTexture.width), h = Mathf.RoundToInt(packed[p].height*atlasTexture.height);
            atlas.AppendLine(Parts[p] + "\n  rotate: false\n  xy: " + x + ", " + y + "\n  size: " + w + ", " + h + "\n  orig: " + w + ", " + h + "\n  offset: 0, 0\n  index: -1");
        }
        byte[] png = atlasTexture.EncodeToPNG();
        File.WriteAllBytes(dir + "/" + id + ".png", png);
        // Import the matching texture/atlas/data together. Importing a new texture
        // against the old atlas lets Spine delete a material it considers unused.
        File.WriteAllText(dir + "/" + id + ".atlas.txt", atlas.ToString());
        File.WriteAllText(dir + "/" + id + ".json", rig.ToString());
        File.WriteAllText(editable + "/" + id + ".json", rig.ToString());
        foreach (var crop in crops) UnityEngine.Object.DestroyImmediate(crop);
        UnityEngine.Object.DestroyImmediate(sheet); UnityEngine.Object.DestroyImmediate(atlasTexture);
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(dir + "/" + id + ".png");
        importer.textureType = TextureImporterType.Default; importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + id + ".png");
        var mat = GetOrCreate<Material>(dir + "/" + id + "_Material.mat", () => new Material(Shader.Find("Spine/Skeleton")));
        mat.mainTexture = texture; mat.SetFloat("_StraightAlphaInput", 1); mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT"); EditorUtility.SetDirty(mat);
        var flash = GetOrCreate<Material>(dir + "/" + id + "_White.mat", () => new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/RedHoodWhite.mat")));
        flash.mainTexture = texture; EditorUtility.SetDirty(flash);
        AssetDatabase.SaveAssets();
        foreach (string ext in new[] { ".png", ".atlas.txt", ".json" }) File.Copy(dir + "/" + id + ext, export + "/" + id + ext, true);
        var atlasAsset = GetOrCreate<SpineAtlasAsset>(dir + "/" + id + "_Atlas.asset", () => ScriptableObject.CreateInstance<SpineAtlasAsset>());
        // Spine's import postprocessor may reload materials when the atlas changes.
        mat = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + id + "_Material.mat");
        flash = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + id + "_White.mat");
        atlasAsset.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/" + id + ".atlas.txt");
        atlasAsset.materials = new[] { mat }; atlasAsset.Clear(); EditorUtility.SetDirty(atlasAsset);
        var data = GetOrCreate<SkeletonDataAsset>(dir + "/" + id + "_SkeletonData.asset", () => ScriptableObject.CreateInstance<SkeletonDataAsset>());
        data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/" + id + ".json");
        data.atlasAssets = new AtlasAssetBase[] { atlasAsset }; data.scale = .003f; data.defaultMix = .06f; data.Clear(); EditorUtility.SetDirty(data);
        if (data.GetSkeletonData(false) == null) throw new Exception("Skeleton import failed: " + id);
        if (!preservePrefab) BuildPrefab(dir, id, data, mat, flash, variant);
        else AssetDatabase.ImportAsset(dir + "/" + id + ".prefab", ImportAssetOptions.ForceUpdate);
    }

    static T GetOrCreate<T>(string path, Func<T> create) where T : UnityEngine.Object
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value != null) return value;
        value = create(); AssetDatabase.CreateAsset(value, path); return value;
    }

    static void BuildPrefab(string dir, string id, SkeletonDataAsset data, Material mat, Material flash, int variant)
    {
        var go = PrefabUtility.LoadPrefabContents("Assets/Prefab/Addressable Prefabs/Enemies/Mob1.prefab");
        try
        {
            go.name = id; go.transform.localScale = Vector3.one;
            var skeleton = go.GetComponentInChildren<SkeletonAnimation>(true);
            // Old source utility bones refer to a different skeleton; new impact anchor is a stable body-local point.
            foreach (var utility in skeleton.GetComponents<SkeletonUtility>()) UnityEngine.Object.DestroyImmediate(utility);
            for (int i = skeleton.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(skeleton.transform.GetChild(i).gameObject);
            skeleton.transform.localPosition = Vector3.zero; skeleton.transform.localRotation = Quaternion.identity; skeleton.transform.localScale = new Vector3(-1,1,1);
            skeleton.skeletonDataAsset = data; skeleton.initialSkinName = "1"; skeleton.initialFlipX = false; skeleton.initialFlipY = false;
            skeleton.AnimationName = "Idle1"; skeleton.loop = true; skeleton.Initialize(true);
            var enemy = go.GetComponent<Enemy>(); enemy.skeletonAnimation = skeleton; enemy.maxSkins = 1; enemy.maxAttacks = 2;
            enemy.bossName = ""; enemy.waitBetweenAttacks = 2.8f; enemy.expGiven = 1; enemy.itemsGiven = new List<Item>(); enemy.currentAnimation = "";
            var impact = new GameObject("ImpactPoint"); impact.transform.SetParent(skeleton.transform, false); impact.transform.localPosition = new Vector3(0, 1.7f, 0); enemy.rootPos = impact;
            var actor = go.GetComponent<Actor_Base>(); actor.isPlayer = false; actor.isBoss = false; actor.chanceInterruptAttack = 35;
            actor.baseStat.hpMax = 6; actor.baseStat.hpNow = 6; actor.baseStat.dmg = 1;
            var bar = go.transform.GetChild(0); bar.localPosition = new Vector3(0, 4.6f, 0); bar.localScale = new Vector3(-.008f,.008f,.008f);
            var swapper = skeleton.GetComponent<MaterialSwapper>(); swapper.skeletonAnimation = skeleton;
            swapper.origMats = new List<Material> { mat }; swapper.whiteMats = new[] { flash };
            enemy.ab = actor; actor.enemyLifebar = bar.GetComponent<EnemyLifebar>();
            go.GetComponent<DropIn>().spawnInPlace = true;
            PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + id + ".prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }

    static void RegisterEnemies()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new Exception("No Addressables settings.");
        var db = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/LevelDataDescriptions.asset"));
        var entries = db.FindProperty("_enemyDb");
        foreach (var id in Ids)
        {
            string guid = AssetDatabase.AssetPathToGUID(Root + "/" + id + "/" + id + ".prefab");
            settings.CreateOrMoveEntry(guid, settings.DefaultGroup).address = id;
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++) if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == id) entry = entries.GetArrayElementAtIndex(i);
            if (entry == null) { entries.InsertArrayElementAtIndex(entries.arraySize); entry = entries.GetArrayElementAtIndex(entries.arraySize-1); }
            entry.FindPropertyRelative("key").stringValue = id;
            entry.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue = guid;
        }
        db.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
    }

    static void AddTestWave()
    {
        const string path = "Assets/Scenes/MainScenes/Gameplay.unity";
        if (File.ReadAllText(path).Contains("  - levelKey: Black Forest Mushroom Test\n")) return;
        string originalScene = File.ReadAllText(path);
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty) throw new Exception("Gameplay has unsaved edits. Save them before adding the test wave.");
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var wm = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveManager>(true)).First();
            const string key = "Black Forest Mushroom Test";
            var wave = wm.waveDb.Find(w => w.levelKey == key);
            if (wave == null) { wave = new Waveset(); wm.waveDb.Add(wave); }
            wave.levelKey = key; wave.levelName = key; wave.mapGraphicKey = "Map1";
            wave.waves = new[] { new Wave { enemies = new[] { Ids[0] } }, new Wave { enemies = new[] { Ids[1] } }, new Wave { enemies = new[] { Ids[2] } } };
            wave.chestItemsGiven = new List<Item>(); wave.chestGoldItemsGiven = new List<Item>(); wave.chestDiamondItemsGiven = new List<Item>();
            wave.crystalsMin = wave.crystalsMax = 0;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            // Keep the original scene serialization; only append this waveset's YAML block.
            string serialized = File.ReadAllText(path);
            int start = serialized.IndexOf("  - levelKey: " + key + "\n", StringComparison.Ordinal);
            int end = serialized.IndexOf("  highestWavesReached:", start, StringComparison.Ordinal);
            string block = string.Join("\n", serialized.Substring(start, end-start).Split('\n').Select(line => line.TrimEnd()));
            string anchor = "  highestWavesReached:";
            int insert = originalScene.IndexOf(anchor, StringComparison.Ordinal);
            if (insert < 0) throw new Exception("Cannot locate WaveManager serialization.");
            File.WriteAllText(path,originalScene.Insert(insert,block));
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    static void RefreshSharedScene()
    {
        var scene = SceneManager.GetSceneByPath(MonsterTestSceneBuilder.ScenePath); bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            var tester = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonsterAnimationTest>(true)).First();
            tester.discoveryFolders = tester.discoveryFolders.Concat(new[] { Root }).Distinct().ToArray();
            tester.monsterPrefabs = Ids.Select(id => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + id + "/" + id + ".prefab")).ToArray();
            tester.RefreshCatalog(); EditorUtility.SetDirty(tester);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Validate Black Forest Mushrooms")]
    public static void Validate()
    {
        var report = new StringBuilder("Black Forest mushroom validation\n");
        ValidateIdentities(report);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + id + "/" + id + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var skeleton = go.GetComponentInChildren<SkeletonAnimation>(); skeleton.Initialize(true);
                if (go.transform.GetChild(0).GetComponent<EnemyLifebar>() == null || go.transform.GetChild(1) != skeleton.transform) throw new Exception(id + " child order invalid");
                if (skeleton.Skeleton.Data.FindSkin("1") == null) throw new Exception("Missing skin 1");
                if (go.GetComponent<Enemy>().GetMaxAttacks() != 2)
                    throw new Exception("Source attack_1 must not be counted as another game AttackN");
                var slots = skeleton.Skeleton.Slots;
                int back = skeleton.Skeleton.Data.FindSlotIndex("head_back"), body = skeleton.Skeleton.Data.FindSlotIndex("body_02"), front = skeleton.Skeleton.Data.FindSlotIndex("head");
                if (!(back < body && body < front) || slots.Items[back].Bone != slots.Items[front].Bone
                    || !(slots.Items[back].Attachment is Spine.MeshAttachment) || !(slots.Items[front].Attachment is Spine.MeshAttachment))
                    throw new Exception("Cap layers must bracket the head and share one bone");
                if (skeleton.transform.lossyScale.x >= 0 || skeleton.Skeleton.ScaleX <= 0) throw new Exception("Prefab must flip the right-authored artwork only at its display transform");
                foreach (string original in new[] { "idle", "attack_1", "die", "run", "spawn", "stun", "victory" })
                {
                    float duration=skeleton.Skeleton.Data.FindAnimation(original).Duration;
                    for(int f=0;f<5;f++) {
                        float t=duration*f/5f;
                        Vector3[] expected=SampleVertices(skeleton,"default",original,t);
                        Vector3[] actual=SampleVertices(skeleton,"1",original,t);
                        if(expected.Length!=actual.Length) throw new Exception("Skin changed original geometry: " + original);
                        for(int v=0;v<actual.Length;v++) if((expected[v]-actual[v]).sqrMagnitude>1e-10f)
                            throw new Exception("Skin lost original deformation: " + original);
                    }
                }
                report.AppendLine(id + ": PASS all 7 original motions preserve default/skin-1 geometry, including spawn deform.");
                foreach (string name in new[] { "Idle1", "Attack1", "Hit", "Death1" })
                {
                    int hits = 0, completes = 0; float hitTime = -1, completeTime = -1;
                    Spine.AnimationState.TrackEntryEventDelegate handler = (entry, e) => { if (e.Data.Name == "OnHit") { hits++; hitTime = e.Time; } if (e.Data.Name == "OnComplete") { completes++; completeTime = e.Time; } };
                    skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose(); skeleton.Skeleton.SetSkin("1"); skeleton.Skeleton.SetSlotsToSetupPose();
                    skeleton.AnimationState.Event += handler;
                    var animation = skeleton.Skeleton.Data.FindAnimation(name);
                    if (animation == null) throw new Exception("Missing " + name);
                    skeleton.AnimationState.SetAnimation(0, name, false);
                    for (int frame = 0; frame < Mathf.CeilToInt(animation.Duration * 60)+3; frame++)
                    {
                        skeleton.Update(1f/60); skeleton.LateUpdate();
                        foreach (var v in skeleton.GetComponent<MeshFilter>().sharedMesh.vertices)
                            if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y)) throw new Exception("Invalid vertex");
                    }
                    skeleton.AnimationState.Event -= handler;
                    if (hits != (name == "Attack1" ? 1 : 0) || completes != (name == "Idle1" ? 0 : 1)) throw new Exception(id + " invalid events: " + name);
                    if (name == "Attack1" && hitTime >= completeTime) throw new Exception("Hit must precede completion");
                    report.AppendLine(id + " / " + name + ": PASS hits=" + hits + " completes=" + completes + " duration=" + animation.Duration);
                }
                var swap = skeleton.GetComponent<MaterialSwapper>();
                if (swap.origMats.Count != 1 || swap.whiteMats.Length != 1 || swap.origMats[0].mainTexture != swap.whiteMats[0].mainTexture) throw new Exception("Invalid flash materials");
                skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose(); skeleton.Update(0);
                var punchBone = skeleton.Skeleton.FindBone("arm_left_03");
                float restX = skeleton.transform.TransformPoint(new Vector3(punchBone.WorldX,punchBone.WorldY,0)).x;
                skeleton.AnimationState.SetAnimation(0,"Attack1",false); skeleton.Update(.3667f);
                if (skeleton.transform.TransformPoint(new Vector3(punchBone.WorldX,punchBone.WorldY,0)).x >= restX) throw new Exception("Attack must travel left");
                int interruptedHits = 0, interruptedCompletes = 0;
                Spine.AnimationState.TrackEntryEventDelegate interruptHandler = (entry,e) => { if(e.Data.Name == "OnHit") interruptedHits++; if(e.Data.Name == "OnComplete") interruptedCompletes++; };
                skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose();
                skeleton.AnimationState.Event += interruptHandler;
                skeleton.AnimationState.SetAnimation(0,"Attack1",false); skeleton.Update(.12f);
                skeleton.AnimationState.SetAnimation(0,"Hit",false);
                for(int f=0;f<28;f++) skeleton.Update(1f/60);
                skeleton.AnimationState.Event -= interruptHandler;
                if(interruptedHits != 0 || interruptedCompletes != 1) throw new Exception("Interrupted attack leaked combat events");
                report.AppendLine(id + ": PASS leftward punch and attack-before-hit interruption (0 hits, 1 completion).");
                skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose(); skeleton.AnimationState.SetAnimation(0,"Idle1",true); skeleton.Update(0); skeleton.LateUpdate();
                go.transform.position = new Vector3((i-1)*4.3f,0,0);
                go.transform.GetChild(0).gameObject.SetActive(false);
            }
            var camGo = new GameObject("Mushroom Preview"); SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera = camGo.AddComponent<Camera>(); camera.scene = scene; camera.orthographic = true; camera.orthographicSize = 3.0f;
            camera.transform.position = new Vector3(0,2.1f,-10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.105f,.13f,.18f);
            Render(camera,"output/monster-animation/mushrooms-lineup.png");
            string[] poses = { "Attack1", "Hit", "Death1" };
            float[] times = { .36f, .07f, 1.05f };
            for (int p = 0; p < poses.Length; p++)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var skeleton = root.GetComponentInChildren<SkeletonAnimation>();
                    if (skeleton == null) continue;
                    skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose();
                    skeleton.AnimationState.SetAnimation(0,poses[p],false); skeleton.Update(times[p]); skeleton.LateUpdate();
                }
                camera.transform.position = new Vector3(poses[p] == "Death1" ? -1f : 0f, 2.1f, -10);
                Render(camera,"output/monster-animation/mushrooms-" + poses[p] + ".png");
            }
            report.AppendLine("PASS: 3 prefabs, 12 animation runs, skin 1, child order, right-authored rigs mirrored only by prefab display transforms, finite vertices and matching flash textures.");
            RenderVisualAudit(scene, camera);
            report.AppendLine("PASS: original head_back < body_02 < head; original meshes, bone bindings and layer order retained. Visual review sheets rendered separately from event assertions.");
            File.WriteAllText("output/monster-animation/mushroom-validation.txt",report.ToString()); Debug.Log(report.ToString());
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void ValidateIdentities(StringBuilder report)
    {
        var db = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/LevelDataDescriptions.asset"));
        var entries = db.FindProperty("_enemyDb");
        var keys = new[] { "Mob1", "Mob2", "Mob4" }.Concat(Ids).ToArray();
        var originals = new[] { "Slime", "Slime2", "Slime3" };
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            string path = i < 3 ? "Assets/Prefab/Addressable Prefabs/Enemies/" + key + ".prefab" : Root + "/" + key + "/" + key + ".prefab";
            string guid = AssetDatabase.AssetPathToGUID(path);
            int matches = 0;
            for (int j = 0; j < entries.arraySize; j++)
            {
                var entry = entries.GetArrayElementAtIndex(j);
                if (entry.FindPropertyRelative("key").stringValue != key) continue;
                matches++;
                if (entry.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue != guid)
                    throw new Exception("Wrong enemy DB GUID: " + key);
            }
            if (matches != 1 || AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid)?.address != (i < 3 ? path : key))
                throw new Exception("Missing/duplicate key or wrong address: " + key);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var skeleton = prefab.GetComponentInChildren<SkeletonAnimation>(true);
            string expected = i < 3 ? "Assets/Grfx/Enemies/" + originals[i] + "/skeleton_SkeletonData.asset" : Root + "/" + key + "/" + key + "_SkeletonData.asset";
            if (AssetDatabase.GetAssetPath(skeleton.skeletonDataAsset) != expected || skeleton.skeletonDataAsset.GetSkeletonData(false) == null)
                throw new Exception("Wrong or unreadable skeleton: " + key);
            report.AppendLine(key + ": PASS distinct DB/address key and original/new skeleton: " + expected);
        }
    }

    static Vector3[] SampleVertices(SkeletonAnimation skeleton,string skin,string animation,float time)
    {
        skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetSkin(skin); skeleton.Skeleton.SetToSetupPose();
        skeleton.AnimationState.SetAnimation(0,animation,false); skeleton.Update(time); skeleton.LateUpdate();
        return skeleton.GetComponent<MeshFilter>().sharedMesh.vertices;
    }

    static void Render(Camera camera, string path)
    {
        var old = RenderTexture.active;
        var rt = new RenderTexture(1500, 650, 24);
        var image = new Texture2D(1500, 650, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0,0,1500,650),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); }
    }

    static void RenderVisualAudit(Scene scene, Camera camera)
    {
        var actors = scene.GetRootGameObjects().Where(r => r.GetComponentInChildren<SkeletonAnimation>() != null).ToArray();
        string[] names = { "Idle1", "Attack1", "Hit", "Death1", "stun", "run", "spawn", "victory" };
        float[][] samples = { new[] { 0f,.14f,.28f,.42f,.57f,.71f,.85f,.99f },
            new[] { 0f,.11f,.22f,.3f,.36f,.46f,.65f,.82f },
            new[] { 0f,.035f,.07f,.12f,.17f,.23f,.29f,.34f },
            new[] { 0f,.0667f,.2333f,.4333f,.6667f,1f,1.5f,1.9667f },
            new[] { 0f,.14f,.28f,.42f,.57f,.71f,.85f,.99f },
            new[] { 0f,.1f,.2f,.3f,.4f,.5f,.6f,.7f },
            new[] { 0f,.1333f,.2667f,.3667f,.5f,.6333f,.7667f,.9f },
            new[] { 0f,.0667f,.1333f,.2f,.2667f,.3333f,.4f,.4667f } };
        const int w=400,h=480;
        var rt = new RenderTexture(w,h,24); var previous = RenderTexture.active;
        camera.targetTexture = rt; camera.orthographicSize = 2.5f;
        try
        {
            foreach (var actor in actors) { actor.SetActive(false); actor.transform.position = Vector3.zero; }
            for (int a=0;a<names.Length;a++)
            {
                // Fit the entire motion envelope once, so extended fists and the falling cap stay visible.
                var envelope = new Bounds(); bool hasBounds=false;
                foreach (var probe in actors) {
                    probe.SetActive(true);
                    var preview = probe.GetComponentInChildren<SkeletonAnimation>();
                    foreach(float time in samples[a]) {
                        preview.AnimationState.ClearTracks(); preview.Skeleton.SetToSetupPose();
                        preview.AnimationState.SetAnimation(0,names[a],false); preview.Update(time); preview.LateUpdate();
                        var bounds=preview.GetComponent<MeshRenderer>().bounds;
                        if(!hasBounds) { envelope=bounds; hasBounds=true; } else envelope.Encapsulate(bounds);
                    }
                    probe.SetActive(false);
                }
                camera.orthographicSize=Mathf.Max(envelope.extents.y,envelope.extents.x/(w/(float)h))*1.1f;
                camera.transform.position=new Vector3(envelope.center.x,envelope.center.y,-10);
                var sheet = new Texture2D(w*8,h*actors.Length,TextureFormat.RGB24,false);
                for (int row=0;row<actors.Length;row++)
                {
                    var actor = actors[row]; actor.SetActive(true);
                    var skeleton = actor.GetComponentInChildren<SkeletonAnimation>();
                    for(int frame=0;frame<8;frame++)
                    {
                        skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose();
                        skeleton.AnimationState.SetAnimation(0,names[a],false); skeleton.Update(samples[a][frame]); skeleton.LateUpdate();
                        camera.Render(); RenderTexture.active=rt;
                        sheet.ReadPixels(new Rect(0,0,w,h),frame*w,(actors.Length-1-row)*h);
                    }
                    actor.SetActive(false);
                }
                sheet.Apply(); File.WriteAllBytes("output/monster-animation/audit-"+names[a]+".png",sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }
        finally { camera.targetTexture=null; RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(rt); }
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Run Mushroom Battle (Play mode)")]
    public static void RunBattle()
    {
        RunBattleLevel("Black Forest Mushroom Test");
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Run First Level (Play mode)")]
    public static void RunFirstLevel()
    {
        RunBattleLevel("Khorasan Ruins I");
    }

    static void RunBattleLevel(string levelKey)
    {
        if (!EditorApplication.isPlaying || WaveManager.single == null) throw new Exception("Open Gameplay, enter Play mode and wait for initialization first.");
        var assets = LevelController.Instance.AssetManager;
        assets.HideCampPackage(); assets.HideMapPackage();
        GameplayCanvas.single.ToggleButtonGrid(false); GameplayCanvas.single.ToggleFusionBarUI(false);
        GameplayCanvas.single.victoryPanel.SetActive(false); GameplayCanvas.single.defeatPanel.SetActive(false);
        MushroomBattleProbe.Begin();
        WaveManager.single.CallStartLevel(levelKey);
    }
}
