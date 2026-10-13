using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit migration of the existing editable prefab; no runtime reconstruction.</summary>
[InitializeOnLoad]
public static class StorybookExperienceFullscreen
{
    const string Root=StorybookExperienceBuilder.Root;
    static StorybookExperienceFullscreen(){EditorApplication.update+=()=>{
        const string request="output/experience-ui/fullscreen.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        File.Delete(request);Patch();
    };}
    [MenuItem("Tools/Dark Fairytale/UI/Make Experience Fullscreen (Explicit)")]
    public static void Patch(){
        var root=PrefabUtility.LoadPrefabContents(Root+"/StorybookExperience.prefab");
        try{Apply(root);PrefabUtility.SaveAsPrefabAsset(root,Root+"/StorybookExperience.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();File.WriteAllText("output/experience-ui/fullscreen.txt","PASS");
    }
    static void Box(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    public static void Apply(GameObject root){
        var ui=root.GetComponent<StorybookExperienceUI>();var page=ui.paper;
        var fusion=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Addressable Prefabs/FusionPackage.prefab");var template=fusion.transform.Find("Core_9x16");
        var scaler=root.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(595,1056);
        var outside=root.transform.Find("DimAndBlockBackground");if(!outside)outside=root.transform.Find("OutsideSafeArea");outside.name="OutsideSafeArea";outside.GetComponent<Image>().color=new Color(.018f,.018f,.016f,1);
        page.SetParent(root.transform,false);page.name="Core_9x16";page.localScale=Vector3.one;
        var oldCore=root.transform.Find("SafeCore");if(oldCore)Object.DestroyImmediate(oldCore.gameObject);
        var fit=root.GetComponent<BattleHudSafeArea>();if(fit)Object.DestroyImmediate(fit);
        var layout=root.GetComponent<StorybookFusionLayout>();if(!layout)layout=root.AddComponent<StorybookFusionLayout>();layout.page=page;layout.designSize=new Vector2(595,1056);
        var background=page.GetComponent<Image>();var original=template.GetComponent<Image>();background.sprite=original.sprite;background.material=original.material;background.type=original.type;
        foreach(var name in new[]{"Frame","Header","Title","TitleDivider","Close"}){var old=page.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
        var frame=Object.Instantiate(template.Find("Frame").gameObject,page,false);frame.name="Frame";frame.transform.SetAsFirstSibling();
        var header=Object.Instantiate(template.Find("Header").gameObject,page,false);header.name="Header";
        header.transform.Find("Title").GetComponent<Text>().text="经验加成";
        header.transform.Find("Subtitle").GetComponent<Text>().text="庇护所 · 烛光研读";
        ui.close=header.transform.Find("Back").GetComponent<Button>();
        Box(page.Find("Book"),31,134,533,316);ui.illustration.preserveAspect=true;
        Box(page.Find("Benefit"),31,461,533,65);var benefit=page.Find("Benefit").GetComponent<Text>();benefit.fontSize=33;benefit.text="经验获取 <color=#83180F><size=54>+33%</size></color>";
        Box(page.Find("TimerFrame"),31,540,533,202);
        var timerFrame=page.Find("TimerFrame").GetComponent<Image>();var progressFrame=template.Find("Progress/Frame").GetComponent<Image>();timerFrame.sprite=progressFrame.sprite;timerFrame.material=progressFrame.material;timerFrame.fillCenter=false;
        Box(ui.state.transform,65,553,465,36);ui.state.fontSize=27;
        Box(ui.timer.transform,65,595,465,45);ui.timer.fontSize=35;
        Box(page.Find("DurationTrack"),61,651,473,42);Box(ui.duration.transform,77,661,438,21);
        Box(page.Find("DurationRule"),45,701,505,30);page.Find("DurationRule").GetComponent<Text>().fontSize=23;
        Box(ui.advert.transform,65,764,465,79);Box(ui.paid.transform,65,856,465,79);
        Box(ui.adCaption.transform,111,0,342,79);ui.adCaption.fontSize=29;
        Box(ui.paidCaption.transform,82,0,365,79);ui.paidCaption.fontSize=29;
        Box(ui.advert.transform.Find("Film"),62,18,40,43);Box(ui.paid.transform.Find("Crystal"),29,11,54,58);
        Box(ui.balance.transform,202,956,300,38);ui.balance.fontSize=25;
        Box(page.Find("WalletIcon"),173,956,29,38);
        Box(ui.notice.transform,35,1006,525,28);ui.notice.fontSize=19;
        layout.Apply();
    }
}
