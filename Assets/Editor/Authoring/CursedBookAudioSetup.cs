using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit opt-in installer; never rebuilds UI or runs on import without a request.</summary>
[InitializeOnLoad]
public static class CursedBookAudioSetup
{
    const string Request="output/storybook-audio/install.request";
    static CursedBookAudioSetup(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        File.Delete(Request);Install();
    }
    [MenuItem("Tools/Dark Fairytale/Storybook/Install Intro Audio")]
    public static void Install(){
        const string path="Assets/DarkFairytale/UI/StorybookIntro/Prefabs/CursedBookIntro.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var a=root.GetComponent<CursedBookIntroAudio>();
            if(!a)a=root.AddComponent<CursedBookIntroAudio>();
            a.intro=root.GetComponent<CursedBookIntro>();
            if(!a.foley)a.foley=Source(root,"Book Foley",false);
            if(!a.atmosphere)a.atmosphere=Source(root,"Book Atmosphere",true);
            a.foley.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/StorybookIntro/Clips/BookIntroFoley.wav");
            a.atmosphere.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/StorybookIntro/Clips/BookIntroAtmosphere.wav");
            if(!a.foley.clip||!a.atmosphere.clip)throw new System.Exception("Missing intro audio assets");
            PrefabUtility.SaveAsPrefabAsset(root,path);
            File.WriteAllText("output/storybook-audio/import-validation.txt","PASS: persistent prefab AudioSources; foley="+a.foley.clip.length+"s; ambience="+a.atmosphere.clip.length+"s\n");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static AudioSource Source(GameObject root,string name,bool loop){
        var go=new GameObject(name);go.transform.SetParent(root.transform,false);
        var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;return s;
    }
}
