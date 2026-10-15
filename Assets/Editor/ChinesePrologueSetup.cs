using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class ChinesePrologueSetup {
    [Serializable] class Data { public CursedBookPrologue.Page[] pages; }
    static ChinesePrologueSetup(){EditorApplication.update+=Tick;}
    static void Tick(){
        const string dir="output/prologue-cn/";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(dir+"install.request"))return;
        File.Delete(dir+"install.request");
        var root=PrefabUtility.LoadPrefabContents(CursedBookIntroBuilder.Prefab);
        try{
            var p=root.GetComponent<CursedBookPrologue>();
            var data=JsonUtility.FromJson<Data>(File.ReadAllText(CursedBookIntroBuilder.Root+"/Prologue/Captions.zh-CN.json"));
            p.silentPreview=true;p.openingCaptions=data.pages[0].captions;p.openingDuration=data.pages[0].silentDuration;
            for(int i=0;i<6;i++){p.pages[i].title=data.pages[i+1].title;p.pages[i].captions=data.pages[i+1].captions;p.pages[i].silentDuration=data.pages[i+1].silentDuration;}
            var replay=AssetDatabase.LoadAssetAtPath<StorySequence>("Assets/DarkFairytale/UI/Map/PrologueStory.asset");
            if(replay){replay.pages=p.pages;EditorUtility.SetDirty(replay);}
            p.subtitle.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
            p.subtitle.fontSize=36;p.subtitle.lineSpacing=1.2f;
            if(!p.openingText){
                var node=new GameObject("ChineseOpeningText",typeof(RectTransform),typeof(Text));
                var rt=(RectTransform)node.transform;rt.SetParent(p.illustration.transform.parent,false);
                rt.anchoredPosition=p.illustration.rectTransform.anchoredPosition;rt.sizeDelta=new Vector2(330,180);
                p.openingText=node.GetComponent<Text>();
            }
            p.openingText.font=p.subtitle.font;p.openingText.fontSize=22;p.openingText.lineSpacing=1.5f;
            p.openingText.color=new Color(.16f,.12f,.1f);p.openingText.alignment=TextAnchor.MiddleCenter;p.openingText.raycastTarget=false;
            p.openingText.text="";p.openingText.gameObject.SetActive(false);
            p.previewInEditor=false;
            PrefabUtility.SaveAsPrefabAsset(root,CursedBookIntroBuilder.Prefab);AssetDatabase.SaveAssets();
            File.WriteAllText(dir+"install-result.txt","PASS");
        }catch(Exception e){File.WriteAllText(dir+"install-result.txt",e.ToString());Debug.LogException(e);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
