using System;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit one-time authoring only. Import/reload never recreates the user's UI.</summary>
[InitializeOnLoad]
public static class CursedBookPrologueSetup
{
    public const string Output = "output/prologue-integration/";
    const string Art = CursedBookIntroBuilder.Root + "/Prologue/";
    const string Audio = "Assets/Audio/StorybookIntro/Prologue/";
    [Serializable] sealed class CaptionFile { public CursedBookPrologue.Page[] pages; }
    static CursedBookPrologueSetup() { EditorApplication.update += Tick; }
    static void Tick() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(Output + "revise.request")) {
            File.Delete(Output + "revise.request");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var root = PrefabUtility.LoadPrefabContents(CursedBookIntroBuilder.Prefab);
            try {
                var p = root.GetComponent<CursedBookPrologue>();
                ConfigurePrint(p);
                PrefabUtility.SaveAsPrefabAsset(root, CursedBookIntroBuilder.Prefab); AssetDatabase.SaveAssets();
                File.WriteAllText(Output + "revise-result.txt", "PASS");
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            return;
        }
        if (File.Exists(Output + "widen.request")) {
            File.Delete(Output + "widen.request");
            var root = PrefabUtility.LoadPrefabContents(CursedBookIntroBuilder.Prefab);
            try {
                var p = root.GetComponent<CursedBookPrologue>();
                var rt = p.illustration.rectTransform;
                rt.sizeDelta = new Vector2(rt.sizeDelta.y * .75f, rt.sizeDelta.y);
                foreach (var page in p.pages) page.focalPoint = new Vector2(.5f, .5f);
                PrefabUtility.SaveAsPrefabAsset(root, CursedBookIntroBuilder.Prefab);
                AssetDatabase.SaveAssets();
                File.WriteAllText(Output + "widen-result.txt", "PASS");
            } finally { PrefabUtility.UnloadPrefabContents(root); }
            return;
        }
        if (!File.Exists(Output + "install.request")) return;
        File.Delete(Output + "install.request");
        try { Install(); File.WriteAllText(Output + "install-result.txt", "PASS"); }
        catch (Exception e) { File.WriteAllText(Output + "install-result.txt", e.ToString()); Debug.LogException(e); }
    }
    static void ConfigurePrint(CursedBookPrologue p) {
                var shadow=p.subtitle.GetComponent<Shadow>();
                if(!shadow)shadow=p.subtitle.gameObject.AddComponent<Shadow>();
                shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(2,-2);
                var surface = p.illustration.GetComponent<ProloguePageSurface>();
                if (!surface) surface = p.illustration.gameObject.AddComponent<ProloguePageSurface>();
                p.pageSurface = surface;
                string materialPath = Art + "PrintedInk.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material) { material = new Material(Shader.Find("DarkFairytale/UI/HiddenInk")); AssetDatabase.CreateAsset(material, materialPath); }
                material.SetTexture("_PaperTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Paper.png"));
                material.SetFloat("_PaperBlend", .55f); material.SetFloat("_EdgeWidth", .095f);
                material.SetVector("_SourceUV",new Vector4(0,0,1,1));
                EditorUtility.SetDirty(material); p.illustration.material = material;
    }
    [MenuItem("Tools/Dark Fairytale/Storybook/Install Prologue Pages (Explicit)")]
    public static void Install() {
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (int i=1;i<=6;i++) {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + $"Page{i:00}.png");
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        }
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(CursedBookIntroBuilder.Root + "/CursedBook_SkeletonData.asset");
        data.Clear();
        if (data.GetSkeletonData(false).FindAnimation("TurnPage") == null) throw new Exception("Missing single-page Spine animation.");
        var root = PrefabUtility.LoadPrefabContents(CursedBookIntroBuilder.Prefab);
        try {
            var intro = root.GetComponent<CursedBookIntro>();
            var prologue = root.GetComponent<CursedBookPrologue>();
            if (!prologue) prologue = root.AddComponent<CursedBookPrologue>();
            intro.prologue = prologue; prologue.intro = intro;
            if (!prologue.illustration) {
                var rt = Node("PrologueIllustration", intro.cameraRig, new Vector2(405,540), intro.homePage.rectTransform.anchoredPosition);
                prologue.illustration = rt.gameObject.AddComponent<RawImage>();
                prologue.illustration.material = intro.homePage.material;
                prologue.illustration.raycastTarget = false;
            }
            if (!prologue.subtitle) {
                var core = root.transform.Find("Core_9x16");
                var rt = Node("PrologueSubtitle", core, new Vector2(960,150), new Vector2(0,-770));
                prologue.subtitle = rt.gameObject.AddComponent<Text>();
                prologue.subtitle.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/Tinos-Regular.ttf");
                if (!prologue.subtitle.font) prologue.subtitle.font = intro.status.font;
                prologue.subtitle.fontSize = 34; prologue.subtitle.alignment = TextAnchor.MiddleCenter;
                prologue.subtitle.color = new Color(.9f,.87f,.8f); prologue.subtitle.raycastTarget = false;
                prologue.subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                prologue.subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            }
            root.GetComponent<CursedBookIntroLayout>().prologueSubtitle = prologue.subtitle.rectTransform;
            if (!prologue.narration) prologue.narration = Source(root, "Prologue Narration");
            if (!prologue.pageFoley) prologue.pageFoley = Source(root, "Prologue Single Page Foley");
            prologue.pageTurnSound = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "SinglePageTurn.wav");
            var file = JsonUtility.FromJson<CaptionFile>(File.ReadAllText(Art + "Captions.json"));
            prologue.pages = file.pages;
            for (int i=0;i<6;i++) {
                prologue.pages[i].illustration = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + $"Page{i+1:00}.png");
                prologue.pages[i].narrationAndAmbience = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + $"Page{i+1:00}.wav");
                if (!prologue.pages[i].illustration || !prologue.pages[i].narrationAndAmbience) throw new Exception("Missing page " + i);
            }
            ConfigurePrint(prologue);
            prologue.illustration.gameObject.SetActive(false); prologue.subtitle.text = "";
            PrefabUtility.SaveAsPrefabAsset(root, CursedBookIntroBuilder.Prefab);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    static RectTransform Node(string name, Transform parent, Vector2 size, Vector2 position) {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent,false); rt.sizeDelta = size; rt.anchoredPosition = position; return rt;
    }
    static AudioSource Source(GameObject root, string name) {
        var go = new GameObject(name); go.transform.SetParent(root.transform,false);
        var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; return source;
    }
}
