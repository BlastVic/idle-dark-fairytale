using System;
using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class ChinesePrologueVoiceSetup {
    [Serializable] class Timeline {public CursedBookPrologue.Caption[] captions; public float[] pageRevealTimes;public float openStart,openEnd,cameraStart,cameraEnd,pageTurnDuration,pageInkDuration;}
    static ChinesePrologueVoiceSetup(){EditorApplication.update+=Tick;}
    static void Tick(){
        const string dir="output/prologue-zh-integrated/";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(dir+"install.request"))return;
        File.Delete(dir+"install.request");
        var root=PrefabUtility.LoadPrefabContents(CursedBookIntroBuilder.Prefab);
        try{
            const string audio="Assets/Audio/StorybookIntro/Prologue/Chinese/PrologueZhFull.wav";
            var importer=(AudioImporter)AssetImporter.GetAtPath(audio);var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            var t=JsonUtility.FromJson<Timeline>(File.ReadAllText(CursedBookIntroBuilder.Root+"/Prologue/VoiceTimeline.zh-CN.json"));
            var p=root.GetComponent<CursedBookPrologue>();p.fullNarration=AssetDatabase.LoadAssetAtPath<AudioClip>(audio);p.silentPreview=false;p.voiceCaptions=t.captions;p.pageRevealTimes=t.pageRevealTimes;p.pageTurnDuration=t.pageTurnDuration;p.pageInkDuration=t.pageInkDuration;p.openingBookStart=t.openStart;p.openingBookEnd=t.openEnd;p.openingCameraStart=t.cameraStart;p.openingCameraEnd=t.cameraEnd;
            p.coverOpenSound=null;
            p.openingSketch=AssetDatabase.LoadAssetAtPath<Texture2D>(CursedBookIntroBuilder.Root+"/Prologue/OpeningSketch.png");
            p.portraitRevealStart=15f;p.forestRevealStart=19.3f;p.sketchRevealDuration=1.5f;
            if(p.openingText){p.openingText.text="";p.openingText.gameObject.SetActive(false);}
            PrefabUtility.SaveAsPrefabAsset(root,CursedBookIntroBuilder.Prefab);AssetDatabase.SaveAssets();File.WriteAllText(dir+"install-result.txt","PASS");
        }catch(Exception e){File.WriteAllText(dir+"install-result.txt",e.ToString());Debug.LogException(e);}finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
