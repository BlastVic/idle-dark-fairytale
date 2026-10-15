using System;
using System.IO;
using System.Linq;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Only explicit menu/file commands author assets. Existing prefab layout is preserved.</summary>
[InitializeOnLoad]
public static class CursedBookIntroBuilder
{
    public const string Root = "Assets/DarkFairytale/UI/StorybookIntro";
    public const string Prefab = Root + "/CursedBookIntro.prefab";
    public const string Output = "output/storybook-intro/";
    static CursedBookIntroBuilder() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var centered = Output + "center-cover.request";
        if(File.Exists(centered)) {
            File.Delete(centered);
            try { CenterCover(); File.WriteAllText(Output+"center-cover-result.txt","PASS"); }
            catch(Exception e){File.WriteAllText(Output+"center-cover-result.txt",e.ToString());Debug.LogException(e);}
            return;
        }
        var sceneOnly = Output + "scene-only.request";
        if (File.Exists(sceneOnly)) {
            File.Delete(sceneOnly);
            try {
                var home = PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
                try {
                    var layout = home.GetComponent<ShelterResponsiveLayout>();
                    layout.presentationControls = layout.core.GetComponent<CanvasGroup>();
                    if (!layout.presentationControls) layout.presentationControls = layout.core.gameObject.AddComponent<CanvasGroup>();
                    PrefabUtility.SaveAsPrefabAsset(home, ShelterHomeBuilder.Path);
                } finally { PrefabUtility.UnloadPrefabContents(home); }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Output + "scene-only-result.txt", "PASS");
            } catch (Exception e) { File.WriteAllText(Output + "scene-only-result.txt", e.ToString()); }
            return;
        }
        var ink = Output + "ink-reveal.request";
        if (File.Exists(ink)) {
            File.Delete(ink);
            try { InstallInkReveal(); File.WriteAllText(Output + "ink-reveal-result.txt", "PASS"); }
            catch (Exception e) { File.WriteAllText(Output + "ink-reveal-result.txt", e.ToString()); Debug.LogException(e); }
            return;
        }
        var upgrade = Output + "page-entry.request";
        if (File.Exists(upgrade)) {
            File.Delete(upgrade);
            try { UpgradePageEntry(); File.WriteAllText(Output + "page-entry-result.txt", "PASS"); }
            catch (Exception e) { File.WriteAllText(Output + "page-entry-result.txt", e.ToString()); Debug.LogException(e); }
            return;
        }
        var command = Output + "build.request";
        if (!File.Exists(command)) return;
        File.Delete(command);
        try { Build(); File.WriteAllText(Output + "build-result.txt", "PASS"); }
        catch (Exception e) { File.WriteAllText(Output + "build-result.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Dark Fairytale/Storybook/Center Initial Cover (Explicit)")]
    public static void CenterCover()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try {
            var intro=root.GetComponent<CursedBookIntro>();
            intro.cameraRig.anchoredPosition=-Vector2.Scale(intro.homePage.rectTransform.anchoredPosition,intro.cameraRig.localScale);
            intro.book.timeScale=0;
            var existing=root.transform.Find("IntroBackdropCamera");
            var camera=existing?existing.GetComponent<Camera>():null;
            if(!camera){var go=new GameObject("IntroBackdropCamera",typeof(Camera));go.transform.SetParent(root.transform,false);camera=go.GetComponent<Camera>();}
            camera.enabled=true;camera.cullingMask=0;camera.depth=-101;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=root.transform.Find("Backdrop").GetComponent<Image>().color;
            camera.orthographic=true;camera.allowHDR=false;camera.allowMSAA=false;
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    static T Asset<T>(string p, Func<T> create) where T : UnityEngine.Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(p);
        if (!a) { a = create(); AssetDatabase.CreateAsset(a, p); }
        return a;
    }
    [MenuItem("Tools/Dark Fairytale/Storybook/Install Opening Prefab (Once)")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode first.");
        Directory.CreateDirectory(Output);
        var names = new[] { "cover", "page" };
        var textures = names.Select(n => {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes("ArtDirection/Storybook/SpineSource/images/" + n + ".png")); return t;
        }).ToArray();
        var atlas = new Texture2D(4096, 4096, TextureFormat.RGBA32, false);
        var rects = atlas.PackTextures(textures, 8, 4096, false);
        File.WriteAllBytes(Root + "/CursedBook.png", atlas.EncodeToPNG());
        var txt = new StringBuilder("CursedBook.png\nsize: " + atlas.width + "," + atlas.height + "\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for (int i = 0; i < names.Length; i++) {
            var r = rects[i]; int w = Mathf.RoundToInt(r.width * atlas.width), h = Mathf.RoundToInt(r.height * atlas.height);
            txt.Append(names[i] + "\n  rotate: false\n  xy: " + Mathf.RoundToInt(r.x * atlas.width) + ", " + Mathf.RoundToInt((1-r.yMax)*atlas.height) + "\n  size: " + w + ", " + h + "\n  orig: " + w + ", " + h + "\n  offset: 0, 0\n  index: -1\n");
        }
        File.WriteAllText(Root + "/CursedBook.atlas.txt", txt.ToString());
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/CursedBook.png");
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var mat = Asset(Root + "/CursedBook_UI.mat", () => new Material(Shader.Find("Spine/SkeletonGraphic")));
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/CursedBook.png");
        mat.SetFloat("_StraightAlphaInput", 1); mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT"); EditorUtility.SetDirty(mat);
        var aa = Asset(Root + "/CursedBook_Atlas.asset", () => ScriptableObject.CreateInstance<SpineAtlasAsset>());
        aa.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/CursedBook.atlas.txt"); aa.materials = new[] { mat }; aa.Clear(); EditorUtility.SetDirty(aa);
        var data = Asset(Root + "/CursedBook_SkeletonData.asset", () => ScriptableObject.CreateInstance<SkeletonDataAsset>());
        data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/CursedBook.json"); data.atlasAssets = new AtlasAssetBase[] { aa }; data.scale = .01f; data.Clear(); EditorUtility.SetDirty(data);
        if (data.GetSkeletonData(false)?.FindAnimation("Open") == null) throw new Exception("Spine 3.8 Open animation failed to load.");
        if (!File.Exists(Prefab)) Create(data, mat);
        UpgradePageEntry();
        CenterCover();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScenes/Gameplay.unity") {
            if (scene.isDirty) throw new Exception("Save the current scene before installation.");
            scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
        }
        if (!UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include)) {
            var old = UnityEngine.Object.FindFirstObjectByType<IntroScript>(FindObjectsInactive.Include);
            if (!old) throw new Exception("Original IntroScript not found; refusing to replace startup flow.");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
            var bridge = go.GetComponent<IntroScript>();
            bridge.objectsToTurnOnAfter = old.objectsToTurnOnAfter;
            PrefabUtility.RecordPrefabInstancePropertyModifications(bridge);
            old.parentObj.SetActive(false); // Preserve the complete original intro for reference/rollback.
            EditorUtility.SetDirty(old.parentObj);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        foreach (var t in textures) UnityEngine.Object.DestroyImmediate(t);
        UnityEngine.Object.DestroyImmediate(atlas);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
    }
    static RectTransform Node(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(parent, false); r.sizeDelta = size; r.anchoredPosition = pos; return r;
    }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero; }
    [MenuItem("Tools/Dark Fairytale/Storybook/Install Hidden Ink Reveal")]
    public static void InstallInkReveal()
    {
        var shader = Shader.Find("DarkFairytale/UI/HiddenInk");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Hidden ink shader failed to compile.");
        var material = Asset(Root + "/HiddenInk.mat", () => new Material(shader));
        material.SetFloat("_EdgeWidth", .0475f); EditorUtility.SetDirty(material);
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try {
            root.GetComponent<CursedBookIntro>().homePage.material = material;
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root + "/CursedBook_SkeletonData.asset").Clear();
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Dark Fairytale/Storybook/Upgrade to Live Page Entry (Once)")]
    public static void UpgradePageEntry()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode first.");
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try {
            var intro = root.GetComponent<CursedBookIntro>();
            if (intro.cameraRig && intro.homePage && intro.homeCamera) return;
            var core = root.transform.Find("Core_9x16");
            var rig = Node("CameraRig", core, new Vector2(1080,1920), Vector2.zero);
            var actor = intro.book.rectTransform;
            var position = actor.anchoredPosition; var scale = actor.localScale;
            actor.SetParent(rig, false); actor.anchoredPosition = position; actor.localScale = scale;
            var page = Node("LiveHomePage", rig, new Vector2(303.75f,540), new Vector2(225.5f,120));
            var raw = page.gameObject.AddComponent<RawImage>(); raw.color = new Color(1,1,1,0); raw.raycastTarget = false;
            var camObj = new GameObject("HomeCaptureCamera", typeof(Camera)); camObj.transform.SetParent(root.transform, false);
            var cam = camObj.GetComponent<Camera>(); cam.enabled = false; cam.orthographic = true; cam.orthographicSize = 1;
            cam.nearClipPlane = .1f; cam.farClipPlane = 10; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black; cam.cullingMask = 1 << 31; cam.depth = -100; cam.allowHDR = false; cam.allowMSAA = false;
            intro.cameraRig = rig; intro.homePage = raw; intro.homeCamera = cam;
            intro.completion.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    static void Create(SkeletonDataAsset data, Material mat)
    {
        var root = new GameObject("CursedBookIntro", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        try {
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var bg = Node("Backdrop", root.transform, Vector2.zero, Vector2.zero); Stretch(bg);
            var image = bg.gameObject.AddComponent<Image>(); image.color = new Color(.022f,.025f,.034f,1); image.raycastTarget = true;
            var core = Node("Core_9x16", root.transform, new Vector2(1080,1920), Vector2.zero);
            var layout = root.AddComponent<CursedBookIntroLayout>(); layout.core = core;
            var actor = Node("Book_Spine", core, new Vector2(1000,720), new Vector2(0,120)); actor.localScale = Vector3.one*1.1f;
            var sg = actor.gameObject.AddComponent<SkeletonGraphic>(); sg.skeletonDataAsset = data; sg.material = mat;
            sg.startingAnimation = "Open"; sg.startingLoop = false; sg.unscaledTime = true; sg.raycastTarget = false; sg.Initialize(true);
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
            var label = Node("LoadingCaption", core, new Vector2(840,70), new Vector2(0,-390)).gameObject.AddComponent<Text>();
            label.font=font;label.fontSize=30;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.60f,.57f,.52f);label.text="故事，即将翻开";label.raycastTarget=false;
            var version = Node("Version",core,new Vector2(350,55),new Vector2(300,-855)).gameObject.AddComponent<Text>();
            version.font=font;version.fontSize=26;version.alignment=TextAnchor.MiddleRight;version.color=new Color(.45f,.44f,.42f);version.text="v"+Application.version;version.raycastTarget=false;
            var bridge=root.AddComponent<IntroScript>();bridge.parentObj=root;bridge.objectsToTurnOnAfter=new GameObject[0];
            var intro=root.AddComponent<CursedBookIntro>();intro.book=sg;intro.group=root.GetComponent<CanvasGroup>();intro.completion=bridge;intro.version=version;intro.status=label;
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
