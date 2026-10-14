using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit migration: preserve existing art and all unrelated prefab edits.
[InitializeOnLoad]
public static class StorybookShopDailyUpgrade
{
    public const string Out="output/shop-daily-double/";
    static StorybookShopDailyUpgrade(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;
        File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());}
    };}
    [MenuItem("Tools/Dark Fairytale/UI/Upgrade Shop Daily Double Reward (Explicit)")]
    public static void Build()
    {
        var root=PrefabUtility.LoadPrefabContents(StorybookShopBuilder.Path);
        try{
            var ui=root.GetComponent<StorybookShopUI>();var claim=ui.dailyClaim;
            if(!ui.dailyAdvert){var copy=UnityEngine.Object.Instantiate(claim.gameObject,claim.transform.parent);copy.name="WatchVideoDouble";ui.dailyAdvert=copy.GetComponent<Button>();ui.dailyAdCaption=copy.GetComponentInChildren<Text>();}
            StorybookEquipmentLayout.Box((RectTransform)claim.transform,531,26,287,70);
            StorybookEquipmentLayout.Box((RectTransform)ui.dailyAdvert.transform,531,116,287,70);
            foreach(var b in new[]{claim,ui.dailyAdvert}){
                var t=b.GetComponentInChildren<Text>();t.fontSize=26;var r=t.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,4);r.offsetMax=new Vector2(-12,-4);
                if(!b.GetComponent<ShelterButtonFeedback>())b.gameObject.AddComponent<ShelterButtonFeedback>();
            }
            ui.dailyAdCaption.text="看视频领双倍";ui.dailyCaption.text="免费领取";
            ui.dailyHint=claim.transform.parent.Find("Hint").GetComponent<Text>();ui.dailyHint.fontSize=19;
            PrefabUtility.SaveAsPrefabAsset(root,StorybookShopBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
