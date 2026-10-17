using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, one-time integration. Never runs at import or overwrites an existing prefab.
public static class MobGloamwebBuilder
{
    public const string Dir = "Assets/DarkFairytale/Monsters/BlackForestSpider/MobGloamweb";
    public const string Evidence = "output/monster-animation/MobGloamweb";
    public const string TestKey = "Black Forest Spider Test";
    static readonly string[] Basics = { "Idle1", "Attack1", "Hit", "Death1" };

    [MenuItem("Tools/Dark Fairytale/Monsters/Install MobGloamweb (Once)")]
    public static void BuildAndValidate()
    {
        Directory.CreateDirectory(Evidence);
        try
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(Dir + "/MobGloamweb.png");
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/MobGloamweb.png");
            var mat = GetOrCreate(Dir + "/MobGloamweb_Material.mat", () => new Material(Shader.Find("Spine/Skeleton")));
            mat.mainTexture = tex;
            mat.SetFloat("_StraightAlphaInput", 1);
            mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            EditorUtility.SetDirty(mat);
            var white = GetOrCreate(Dir + "/MobGloamweb_White.mat", () => new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodWhite.mat")));
            white.mainTexture = tex;
            EditorUtility.SetDirty(white);
            var atlas = GetOrCreate(Dir + "/MobGloamweb_Atlas.asset", () => ScriptableObject.CreateInstance<SpineAtlasAsset>());
            atlas.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Dir + "/MobGloamweb.atlas.txt");
            atlas.materials = new[] { mat };
            atlas.Clear();
            EditorUtility.SetDirty(atlas);
            var data = GetOrCreate(Dir + "/MobGloamweb_SkeletonData.asset", () => ScriptableObject.CreateInstance<SkeletonDataAsset>());
            data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(Dir + "/MobGloamweb.json");
            data.atlasAssets = new AtlasAssetBase[] { atlas };
            data.scale = .006f;
            data.defaultMix = .06f;
            data.Clear();
            EditorUtility.SetDirty(data);
            if (data.GetSkeletonData(false) == null) throw new Exception("MobGloamweb skeleton did not load.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/MobGloamweb.prefab") == null)
                MakePrefab(data, mat, white);
            Register();
            UpdateTestScene(data);
            AddTestWave();
            AssetDatabase.SaveAssets();
            Validate();
            File.WriteAllText(Evidence + "/build-result.txt", "PASS: MobGloamweb installed, registered and validated.\n");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Evidence + "/build-error.txt", ex.ToString());
            Debug.LogException(ex);
            throw;
        }
    }

    static string OriginalJsonPath()
    {
        foreach (var path in new[] { "../Spine/monster2/动画/C032_S01B/C032_S01B.json", "../Spine/monster2/C032_S01B/C032_S01B.json" })
            if (File.Exists(path)) return path;
        throw new FileNotFoundException("C032_S01B source JSON missing in sibling Spine directory.");
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Prepare MobGloamweb Acceptance")]
    public static void PrepareAcceptance()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(Dir + "/MobGloamweb.png");
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/MobGloamweb.png");
        var normal = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/MobGloamweb_Material.mat");
        normal.SetFloat("_StraightAlphaInput", 1);
        normal.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
        EditorUtility.SetDirty(normal);
        var white = GetOrCreate(Dir + "/MobGloamweb_White.mat", () => new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodWhite.mat")));
        white.mainTexture = texture;
        EditorUtility.SetDirty(white);
        var root = PrefabUtility.LoadPrefabContents(Dir + "/MobGloamweb.prefab");
        try
        {
            var display = root.GetComponentInChildren<SkeletonAnimation>(true);
            display.transform.localScale = new Vector3(-1, 1, 1);
            var swapper = display.GetComponent<MaterialSwapper>();
            swapper.whiteMats = new[] { white };
            PrefabUtility.SaveAsPrefabAsset(root, Dir + "/MobGloamweb.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Register();
        UpdateTestScene(AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Dir + "/MobGloamweb_SkeletonData.asset"));
        AssetDatabase.SaveAssets();
        Validate();
    }

    static T GetOrCreate<T>(string path, Func<T> factory) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = factory();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void MakePrefab(SkeletonDataAsset data, Material mat, Material white)
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/DarkFairytale/Monsters/BlackForestMushroom/MobTallowkin/MobTallowkin.prefab");
        try
        {
            root.name = "MobGloamweb";
            root.transform.localScale = Vector3.one;
            var display = root.GetComponentInChildren<SkeletonAnimation>(true);
            display.transform.localScale = new Vector3(-1, 1, 1);
            display.transform.localPosition = Vector3.zero;
            display.skeletonDataAsset = data;
            display.initialSkinName = "1";
            display.initialFlipX = display.initialFlipY = false;
            display.AnimationName = "Idle1";
            display.loop = true;
            display.Initialize(true);
            var enemy = root.GetComponent<Enemy>();
            enemy.skeletonAnimation = display;
            enemy.maxSkins = 1;
            enemy.maxAttacks = 2; // exclusive upper bound; exactly one AttackN.
            enemy.waitBetweenAttacks = 2.8f;
            enemy.expGiven = 1;
            enemy.itemsGiven = new List<Item>();
            enemy.bossName = "";
            enemy.currentAnimation = "";
            enemy.rootPos.transform.localPosition = new Vector3(0, 1.1f, 0);
            var actor = root.GetComponent<Actor_Base>();
            actor.isPlayer = actor.isBoss = false;
            actor.baseStat.hpMax = actor.baseStat.hpNow = 8;
            actor.baseStat.dmg = 1;
            actor.chanceInterruptAttack = 35;
            root.GetComponent<DropIn>().spawnInPlace = false; // no invented spawn animation.
            var swapper = display.GetComponent<MaterialSwapper>();
            swapper.skeletonAnimation = display;
            swapper.origMats = new List<Material> { mat };
            swapper.whiteMats = new[] { white };
            PrefabUtility.SaveAsPrefabAsset(root, Dir + "/MobGloamweb.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Register()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new Exception("Addressables settings missing.");
        string guid = AssetDatabase.AssetPathToGUID(Dir + "/MobGloamweb.prefab");
        settings.CreateOrMoveEntry(guid, settings.DefaultGroup).address = "MobGloamweb";
        EditorUtility.SetDirty(settings);
        var db = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/Data/LevelDataDescriptions.asset"));
        var entries = db.FindProperty("_enemyDb");
        SerializedProperty item = null;
        for (int i = 0; i < entries.arraySize; i++)
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == "MobGloamweb")
                item = entries.GetArrayElementAtIndex(i);
        if (item == null)
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            item = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        }
        item.FindPropertyRelative("key").stringValue = "MobGloamweb";
        item.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue = guid;
        db.ApplyModifiedPropertiesWithoutUndo();
    }

    static void UpdateTestScene(SkeletonDataAsset data)
    {
        var scene = EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath, OpenSceneMode.Single);
        var tester = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonsterAnimationTest>(true)).First();
        tester.discoveryFolders = tester.discoveryFolders.Concat(new[] { "Assets/DarkFairytale/Monsters/BlackForestSpider" }).Distinct().ToArray();
        tester.monsterPrefabs = tester.monsterPrefabs.Concat(new[] { AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/MobGloamweb.prefab") }).Distinct().ToArray();
        tester.RefreshCatalog();
        MonsterPreviewUiBuilder.RefreshHudCatalog(tester);
        MonsterPreviewUiBuilder.MakePortrait(data, tester, true);
        // Start the shared preview on this newly added monster, while retaining its catalog.
        tester.character.skeletonDataAsset = data;
        tester.character.initialSkinName = "1";
        tester.character.AnimationName = "Idle1";
        tester.character.transform.localScale = new Vector3(-1, 1, 1);
        EditorUtility.SetDirty(tester);
        EditorUtility.SetDirty(tester.character);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void AddTestWave()
    {
        const string path = "Assets/Scenes/MainScenes/Gameplay.unity";
        string original = File.ReadAllText(path);
        if (original.Contains("  - levelKey: " + TestKey + "\n")) return;
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var wm = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveManager>(true)).First();
            wm.waveDb.Add(new Waveset {
                levelKey = TestKey, levelName = TestKey, mapGraphicKey = "Map1",
                waves = new[] {new Wave {enemies = new[] {"MobGloamweb"}}, new Wave {enemies = new[] {"MobGloamweb", "MobGloamweb"}}, new Wave {enemies = new[] {"MobGloamweb", "MobTallowkin", "MobGloamweb"}}},
                chestItemsGiven = new List<Item>(), chestGoldItemsGiven = new List<Item>(), chestDiamondItemsGiven = new List<Item>(),
                crystalsMin = 0, crystalsMax = 0
            });
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            string serialized = File.ReadAllText(path);
            int start = serialized.IndexOf("  - levelKey: " + TestKey + "\n", StringComparison.Ordinal);
            int end = serialized.IndexOf("  highestWavesReached:", start, StringComparison.Ordinal);
            string block = string.Join("\n", serialized.Substring(start, end-start).Split('\n').Select(line => line.TrimEnd()));
            int at = original.IndexOf("  highestWavesReached:", StringComparison.Ordinal);
            if (at < 0) throw new Exception("WaveManager anchor missing.");
            File.WriteAllText(path, original.Insert(at, block));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Validate MobGloamweb")]
    public static void Validate()
    {
        Directory.CreateDirectory(Evidence);
        var report = new StringBuilder("MobGloamweb Unity 3.8 runtime validation\n");
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Dir + "/MobGloamweb_SkeletonData.asset");
        var sd = data.GetSkeletonData(false);
        var original = JObject.Parse(File.ReadAllText(OriginalJsonPath()));
        var exported = JObject.Parse(File.ReadAllText(Dir + "/MobGloamweb.json"));
        foreach (string key in new[] { "bones", "slots", "ik", "transform", "path" })
            if (!JToken.DeepEquals(original[key], exported[key])) throw new Exception(key + " changed");
        foreach (var animation in (JObject)original["animations"])
            if (!JToken.DeepEquals(animation.Value, exported["animations"][animation.Key])) throw new Exception("Original motion changed: " + animation.Key);
        if (!JToken.DeepEquals(original["skins"][0], exported["skins"][0])) throw new Exception("Original meshes changed");
        report.AppendLine("PASS: original bones, slots, constraints, mesh UV/weights and all ten timelines unchanged.");
        if (sd.FindSkin("1") == null) throw new Exception("Missing numeric skin.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/MobGloamweb.prefab");
        var prefabDisplay = prefab.GetComponentInChildren<SkeletonAnimation>(true);
        if (prefab.transform.GetChild(0).GetComponent<EnemyLifebar>() == null || prefab.transform.GetChild(1) != prefabDisplay.transform)
            throw new Exception("Invalid enemy child order.");
        if (prefab.GetComponent<Enemy>().GetMaxAttacks() != 2 || prefab.GetComponent<Enemy>().maxSkins != 1)
            throw new Exception("Invalid attack/skin configuration.");
        string guid = AssetDatabase.AssetPathToGUID(Dir + "/MobGloamweb.prefab");
        if (AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address != "MobGloamweb") throw new Exception("Address mismatch.");
        if (Scripts.Level.LevelDataDescriptions.EnemyDb.Count(e => e.key == "MobGloamweb" && e.assetRef.AssetGUID == guid) != 1)
            throw new Exception("Database entry mismatch");
        foreach (var tr in prefab.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) != 0) throw new Exception("Missing script: " + tr.name);
        var swapper = prefabDisplay.GetComponent<MaterialSwapper>();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/MobGloamweb.png");
        if (swapper.origMats.Any(m => !m || m.mainTexture != texture) || swapper.whiteMats.Any(m => !m || m.mainTexture != texture))
            throw new Exception("Wrong normal/flash texture");
        report.AppendLine("PASS: database, Addressables, prefab scripts and normal/flash textures.");
        var scene = EditorSceneManager.NewPreviewScene();
        SkeletonAnimation display = null;
        try
        {
            display = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            SceneManager.MoveGameObjectToScene(display.gameObject, scene);
            display.transform.localScale = new Vector3(-1, 1, 1);
            foreach (string skin in new[] { "default", "1" })
            {
                display.Skeleton.SetSkin(skin);
                foreach (var animation in sd.Animations)
                {
                    display.AnimationState.ClearTracks();
                    display.Skeleton.SetToSetupPose();
                    int hits=0, completes=0;
                    Spine.AnimationState.TrackEntryEventDelegate handler = (entry, ev) => {
                        if (ev.Data.Name == "OnHit") hits++;
                        if (ev.Data.Name == "OnComplete") completes++;
                    };
                    display.AnimationState.Event += handler;
                    var track = display.AnimationState.SetAnimation(0, animation.Name, false);
                    track.MixDuration = 0;
                    display.Update(0); display.LateUpdate();
                    int frames = Mathf.CeilToInt(animation.Duration*60)+2;
                    for (int frame=0;frame<frames;frame++)
                    {
                        display.Update(1f/60); display.LateUpdate();
                        foreach (var v in display.GetComponent<MeshFilter>().sharedMesh.vertices)
                            if (float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)) throw new Exception("Non-finite vertex: "+animation.Name);
                    }
                    display.AnimationState.Event -= handler;
                    if (Basics.Contains(animation.Name))
                    {
                        int wantedHit = animation.Name=="Attack1" ? 1 : 0;
                        int wantedComplete = animation.Name=="Idle1" ? 0 : 1;
                        if (hits!=wantedHit || completes!=wantedComplete) throw new Exception(animation.Name+" incorrect event counts "+hits+"/"+completes);
                    }
                    report.AppendLine(skin+" / "+animation.Name+": PASS "+frames+" frames; OnHit="+hits+" OnComplete="+completes);
                }
            }
            foreach (string next in new[] { "Hit", "Death1" })
            {
                display.AnimationState.ClearTracks(); display.Skeleton.SetToSetupPose();
                display.AnimationState.SetAnimation(0, "Attack1", false); display.Update(.2f);
                int leakedHits=0, completed=0;
                Spine.AnimationState.TrackEntryEventDelegate handler=(entry, ev)=>{if(ev.Data.Name=="OnHit")leakedHits++;if(ev.Data.Name=="OnComplete")completed++;};
                display.AnimationState.Event+=handler;
                display.AnimationState.SetAnimation(0,next,false);
                for(int f=0;f<100;f++) display.Update(1f/60);
                display.AnimationState.Event-=handler;
                if(leakedHits!=0 || completed!=1)throw new Exception("Interrupted attack events: " + next);
                report.AppendLine("PASS: Attack1 -> " + next + ": no late hit; one completion.");
            }
            foreach (string name in Basics)
            {
                var anim=sd.FindAnimation(name);
                for (int i=0;i<4;i++)
                {
                    display.AnimationState.ClearTracks(); display.Skeleton.SetSkin("1"); display.Skeleton.SetToSetupPose();
                    display.AnimationState.SetAnimation(0,name,false).MixDuration=0;
                    display.Update(anim.Duration*i/3f); display.LateUpdate();
                    Render(display,scene,Evidence+"/"+name+"-"+i+".png",768,768);
                }
            }
        }
        finally
        {
            if(display!=null) UnityEngine.Object.DestroyImmediate(display.gameObject);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        report.AppendLine("PASS: independent prefab, numeric skin, single Attack1, source schema loaded by actual Runtime, all 14 animations sampled in both skins.");
        File.WriteAllText(Evidence+"/validation.txt",report.ToString());
        Debug.Log("MOBGLOAMWEB_VALIDATION_PASSED");
    }

    public static void Render(SkeletonAnimation display,Scene scene,string path,int width,int height)
    {
        var cameraGo=new GameObject("MobGloamweb evidence camera");
        SceneManager.MoveGameObjectToScene(cameraGo,scene);
        var camera=cameraGo.AddComponent<Camera>();
        camera.scene=scene; camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.16f,.18f,.22f,1);
        var bounds=display.GetComponent<Renderer>().bounds;
        camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);
        camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x*height/width)*.62f;
        var rt=new RenderTexture(width,height,24);
        camera.targetTexture=rt; camera.Render();
        var previous=RenderTexture.active; RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=previous; camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(cameraGo);
    }
}
