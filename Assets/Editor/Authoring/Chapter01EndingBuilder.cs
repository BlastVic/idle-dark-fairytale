using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit authoring command only: never regenerates saved UI on import or during gameplay.
[InitializeOnLoad]
public static class Chapter01EndingBuilder
{
    public const string Root = "Assets/DarkFairytale/UI/Chapter01Ending/";
    const string Request = "output/chapter01-ending/build.request";
    [Serializable] class Timing { public float duration; public CursedBookPrologue.Caption[] sentences; }
    static Chapter01EndingBuilder() { EditorApplication.update += Tick; }
    static void Tick() {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Build(); File.WriteAllText("output/chapter01-ending/build.txt", "SUCCESS"); }
        catch(Exception e) { File.WriteAllText("output/chapter01-ending/build.txt",e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Dark Fairytale/Build Chapter 1 Ending")]
    public static void Build() {
        AssetDatabase.Refresh();
        var story = AssetDatabase.LoadAssetAtPath<StorySequence>("Assets/DarkFairytale/UI/Chapter01Ending/Data/Chapter01Ending.asset");
        if (!story) { story=ScriptableObject.CreateInstance<StorySequence>(); AssetDatabase.CreateAsset(story,"Assets/DarkFairytale/UI/Chapter01Ending/Data/Chapter01Ending.asset"); }
        story.storyKey="Chapter01Ending"; story.displayName="第一章 · 猎人小屋";
        story.completionKey=CursedBookIntro.ChapterEndingSeenKey;
        story.pages=new CursedBookPrologue.Page[5];
        string[] titles={"狼终于倒下","留下的地图","穿过村庄","今晚留在这里","是你的话，一定可以"};
        for(int i=0;i<5;i++) {
            string n=(i+1).ToString("00");
            if(i<4) {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"Art/Page"+n+".png");
                ti.textureType=TextureImporterType.Default;ti.maxTextureSize=2048;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            }
            var ai=(AudioImporter)AssetImporter.GetAtPath(Root+"Audio/Page"+n+".wav");
            var settings=ai.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;ai.defaultSampleSettings=settings;ai.SaveAndReimport();
            var timing=JsonUtility.FromJson<Timing>(File.ReadAllText("ArtDirection/Audio/Chapter01EndingZh/v5/page"+n+"-timings.json"));
            story.pages[i]=new CursedBookPrologue.Page {title=titles[i],focalPoint=new Vector2(.5f,(i==0||i==3)?.625f:.5f),illustration=i<4?AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Art/Page"+n+".png"):null,
                narrationAndAmbience=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Page"+n+".wav"),captions=timing.sentences,silentDuration=timing.duration};
        }
        EditorUtility.SetDirty(story);
        var go=PrefabUtility.LoadPrefabContents("Assets/DarkFairytale/UI/StorybookIntro/Prefabs/CursedBookIntro.prefab");
        try {
            go.name="Chapter01Ending";
            go.GetComponent<Canvas>().sortingOrder=29999;
            var intro=go.GetComponent<CursedBookIntro>();intro.chapterEnding=story;
            intro.homePage.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Chapter01Ending/Materials/HomePrintedInk.mat");
            var p=intro.prologue;p.pages=new CursedBookPrologue.Page[4];Array.Copy(story.pages,p.pages,4);
            p.fullNarration=null;p.silentPreview=false;p.previewInEditor=false;p.openingCaptions=Array.Empty<CursedBookPrologue.Caption>();p.openingSketch=null;
            p.beforeNarrationHold=.8f;p.afterNarrationHold=.4f;p.inkDuration=1.2f;p.pageTurnDuration=1.6f;
            go.GetComponent<CursedBookIntroAudio>().enabled=false;
            PrefabUtility.SaveAsPrefabAsset(go,"Assets/DarkFairytale/UI/Chapter01Ending/Prefabs/Chapter01Ending.prefab");
        } finally { PrefabUtility.UnloadPrefabContents(go); }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Gameplay.unity") throw new InvalidOperationException("Open Gameplay scene before wiring ending.");
        var router=UnityEngine.Object.FindFirstObjectByType<Router>(FindObjectsInactive.Include);
        Undo.RecordObject(router,"Wire chapter ending");router.chapter01EndingPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/Chapter01Ending/Prefabs/Chapter01Ending.prefab").GetComponent<CursedBookIntro>();
        EditorUtility.SetDirty(router);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
