using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in initial authoring only. Never rebuilds existing hand-edited prefabs on import or play.</summary>
public static class CursedStorybookUiBuilder
{
    public const string Root = "Assets/DarkFairytale/UI/CursedStorybook";
    public const string HudPath = Root + "/CursedStorybookBattleHUD.prefab";
    public const string EnemyPath = Root + "/CursedStorybookEnemyBar.prefab";
    const string Request = "output/battle-ui/build.request";
    static Sprite[] sprites;
    static Font font;
    static readonly Color Ink = new Color(.075f, .065f, .055f);
    static readonly Color Ivory = new Color(.94f, .90f, .80f);

    [InitializeOnLoadMethod]
    static void Listen()
    {
        EditorApplication.update -= CheckRequest;
        EditorApplication.update += CheckRequest;
    }

    static void CheckRequest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(Request)) return;
        File.Delete(Request);
        try { CreateAndInstall(); File.WriteAllText("output/battle-ui/build-result.txt", "PASS: authored prefabs installed; validation follows.\n"); }
        catch (Exception e) { File.WriteAllText("output/battle-ui/build-result.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Dark Fairytale/UI/Create B3 Prefabs and Install (Once)")]
    public static void CreateAndInstall()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory("output/battle-ui");
        ImportArt();
        font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSerifCJKsc-Regular.otf");
        if (!font) throw new Exception("Missing packaged Chinese font.");
        // A second invocation reuses saved prefabs and preserves every manual adjustment.
        if (!File.Exists(HudPath)) CreateHud();
        if (!File.Exists(EnemyPath)) CreateEnemy();
        Install();
    }

    static void ImportArt()
    {
        AssetDatabase.ImportAsset(Root + "/CursedStorybookAtlas.png", ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/CursedStorybookAtlas.png");
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/CursedStorybookAtlas.png");
        var slices = new[] {
            Slice("Parchment", 16, 24, 1504, 300, new Vector4(250, 90, 220, 90)),
            Slice("WavePlaque", 20, 338, 610, 306, Vector4.zero),
            Slice("PaperButton", 638, 433, 370, 174, new Vector4(65, 45, 65, 45)),
            Slice("Portrait", 1015, 330, 305, 382, Vector4.zero),
            Slice("WaxBookmark", 1338, 336, 180, 443, Vector4.zero),
            Slice("MeterFrame", 20, 738, 1324, 97, new Vector4(65, 18, 65, 18)),
            Slice("HealthInk", 30, 837, 1306, 70, Vector4.zero),
            Slice("ExperienceInk", 30, 918, 1306, 67, Vector4.zero)
        };
        Directory.CreateDirectory(Root + "/Sprites");
        sprites = slices.Select(slice => {
            string path = Root + "/Sprites/" + slice.name + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) {
                sprite = Sprite.Create(texture, slice.rect, slice.pivot, 100, 0, SpriteMeshType.FullRect, slice.border);
                sprite.name = slice.name;
                AssetDatabase.CreateAsset(sprite, path);
            }
            return sprite;
        }).ToArray();
        AssetDatabase.SaveAssets();
    }

    static SpriteMetaData Slice(string name, int x, int top, int width, int height, Vector4 border)
    {
        return new SpriteMetaData { name = name, rect = new Rect(x, 1024 - top - height, width, height),
            pivot = new Vector2(.5f, .5f), alignment = (int)SpriteAlignment.Center, border = border };
    }

    static Sprite Art(string name) { return sprites.First(s => s.name == name); }

    static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    static Image Picture(string name, Transform parent, string sprite, Vector2 anchor, Vector2 position, Vector2 size, bool sliced = false, Vector2? pivot = null)
    {
        var rect = Node(name, parent, anchor, position, size, pivot);
        var image = rect.gameObject.AddComponent<Image>();
        if (sprite != null) image.sprite = Art(sprite);
        image.raycastTarget = false;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    static Text Label(string name, Transform parent, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var rect = Node(name, parent, anchor, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.text = value; text.fontSize = fontSize; text.color = color; text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(16, fontSize - 10); text.resizeTextMaxSize = fontSize;
        return text;
    }

    static Image Fill(string name, Transform parent, string art, Vector2 anchor, Vector2 position, Vector2 size, float amount)
    {
        var image = Picture(name, parent, art, anchor, position, size);
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = amount;
        return image;
    }

    static void CreateHud()
    {
        var root = Node("CursedStorybookBattleHUD", null, new Vector2(.5f,.5f), Vector2.zero, new Vector2(1080,1920));
        try
        {
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var hud = root.gameObject.AddComponent<CursedBattleHud>();
            var core = Node("Core_9x16", root, new Vector2(.5f,.5f), Vector2.zero, new Vector2(1080,1920));
            root.gameObject.AddComponent<BattleHudSafeArea>().core = core;

            var back = Picture("ReturnButton", core, "PaperButton", new Vector2(0,1), new Vector2(145,-103), new Vector2(230,108));
            back.raycastTarget = true;
            hud.backButton = back.gameObject.AddComponent<Button>();
            var colors = hud.backButton.colors; colors.highlightedColor = new Color(1,.96f,.88f); colors.pressedColor = new Color(.72f,.62f,.53f);
            colors.disabledColor = new Color(.6f,.6f,.6f,.7f); hud.backButton.colors = colors;
            hud.backButton.targetGraphic = back; hud.backButton.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Arrow", back.transform, "←", new Vector2(.5f,.5f), new Vector2(-68,0), new Vector2(58,64), 52, Ink);
            Label("Text", back.transform, "返回", new Vector2(.5f,.5f), new Vector2(29,0), new Vector2(122,58), 36, Ink);

            var wave = Picture("WavePlaque", core, "WavePlaque", new Vector2(.5f,1), new Vector2(0,-117), new Vector2(354,178));
            hud.waveText = Label("WaveText", wave.transform, "第 2 / 3 波", new Vector2(.5f,.5f), new Vector2(0,-10), new Vector2(278,56), 34, Ink);
            var progress = Node("WaveProgress", wave.transform, new Vector2(.5f,.5f), new Vector2(0,-54), new Vector2(264,24));
            hud.progressRow = progress.gameObject;
            var layout = progress.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = 15; layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            hud.waveMarkers = new Image[8];
            for (int i = 0; i < hud.waveMarkers.Length; i++)
            {
                var box = Node("Wave_" + (i+1), progress, new Vector2(.5f,.5f), Vector2.zero, new Vector2(20,20));
                var outline = Picture("InkOutline", box, null, new Vector2(.5f,.5f), Vector2.zero, new Vector2(17,17));
                outline.color = Ink; outline.transform.localEulerAngles = new Vector3(0,0,45);
                var dot = Picture("State", outline.transform, null, new Vector2(.5f,.5f), Vector2.zero, new Vector2(13,13));
                hud.waveMarkers[i] = dot;
            }

            var panel = Picture("PlayerStatus", core, "Parchment", new Vector2(.5f,0), new Vector2(0,194), new Vector2(1016,288), true);
            panel.pixelsPerUnitMultiplier = 1.45f;
            Picture("Portrait", panel.transform, "Portrait", Vector2.zero, new Vector2(124,170), new Vector2(178,223));
            var levelPaper = Picture("LevelPaper", panel.transform, "PaperButton", Vector2.zero, new Vector2(123,52), new Vector2(184,72));
            hud.levelText = Label("LevelText", levelPaper.transform, "Lv. 6", new Vector2(.5f,.5f), new Vector2(0,0), new Vector2(156,45), 34, Ink);
            Picture("WaxBookmark", panel.transform, "WaxBookmark", Vector2.zero, new Vector2(949,155), new Vector2(74,183));
            Label("HealthLabel", panel.transform, "生命", Vector2.zero, new Vector2(290,190), new Vector2(88,60), 33, Ink);
            Label("ExperienceLabel", panel.transform, "经验", Vector2.zero, new Vector2(290,106), new Vector2(88,60), 33, Ink);
            Picture("HealthFrame", panel.transform, "MeterFrame", Vector2.zero, new Vector2(620,190), new Vector2(550,44));
            hud.hpFill = Fill("HealthFill", panel.transform, "HealthInk", Vector2.zero, new Vector2(620,190), new Vector2(518,29), 11f/14);
            hud.hpText = Label("HealthText", panel.transform, "11 / 14", Vector2.zero, new Vector2(620,191), new Vector2(488,47), 31, Ivory);
            var shadow = hud.hpText.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.8f); shadow.effectDistance = new Vector2(1,-1);
            Picture("ExperienceFrame", panel.transform, "MeterFrame", Vector2.zero, new Vector2(562,106), new Vector2(430,40));
            hud.xpFill = Fill("ExperienceFill", panel.transform, "ExperienceInk", Vector2.zero, new Vector2(562,106), new Vector2(400,26), .44f);
            hud.xpText = Label("ExperienceText", panel.transform, "44%", Vector2.zero, new Vector2(819,106), new Vector2(70,51), 30, Ink);
            hud.SetWave(2,3);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, HudPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    static void CreateEnemy()
    {
        var root = Node("CursedStorybookEnemyBar", null, new Vector2(.5f,.5f), Vector2.zero, new Vector2(360,100));
        try
        {
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UI"; canvas.sortingOrder = 10;
            Picture("HealthFrame", root, "MeterFrame", new Vector2(.5f,.5f), Vector2.zero, new Vector2(360,28));
            var fill = Fill("HP FILL", root, "HealthInk", new Vector2(.5f,.5f), Vector2.zero, new Vector2(338,17), 1);
            var meter = fill.gameObject.AddComponent<MeterMover>();
            var bar = root.gameObject.AddComponent<EnemyLifebar>(); bar.hpMeter = meter;
            bar.hpText = Label("HealthText", root, "6 / 6", new Vector2(.5f,.5f), new Vector2(0,39), new Vector2(360,60), 52, Ivory);
            var outline = bar.hpText.gameObject.AddComponent<Outline>(); outline.effectDistance = new Vector2(1.3f,-1.3f); outline.effectColor = new Color(.06f,.05f,.06f);
            bar.miniBossName = Label("BossName", root, "", new Vector2(.5f,.5f), new Vector2(0,83), new Vector2(540,44), 32, Ivory);
            // Presentation is configured by the battle style after instantiation.
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, EnemyPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    [MenuItem("Tools/Dark Fairytale/UI/Install Existing B3 Prefabs")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScenes/Gameplay.unity")
            throw new InvalidOperationException("Open Gameplay first; installation never replaces your current scene.");
        var gc = UnityEngine.Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);
        if (!gc) throw new Exception("GameplayCanvas not found.");
        if (!gc.battleHud)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath), scene);
            gc.battleHud = go.GetComponent<CursedBattleHud>(); go.SetActive(false);
            EditorUtility.SetDirty(gc);
        }
        var style = AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/MoonlitCastleBattleStyle.asset");
        style.enemyHealthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath).GetComponent<EnemyLifebar>();
        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        Debug.Log("B3_UI_PREFABS_INSTALLED");
    }
}
