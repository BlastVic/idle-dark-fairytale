using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookSettingsBuilder
{
    public const string Path="Assets/DarkFairytale/UI/Settings/StorybookSettings.prefab",Out="output/settings-ui/";
    static Font font;static Sprite paper,frame,buttonFrame;static Material paperMat,ink;
    static StorybookSettingsBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}}
    static RectTransform Node(Transform p,string n){var g=new GameObject(n,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);return(RectTransform)g.transform;}
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static void Full(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h,Material m=null){var r=Node(p,n);Box(r,x,y,w,h);var a=r.gameObject.AddComponent<Image>();a.sprite=s;a.material=m;a.raycastTarget=false;return a;}
    static Text Txt(Transform p,string n,string text,float x,float y,float w,float h,int size,TextAnchor align=TextAnchor.MiddleLeft){var r=Node(p,n);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.alignment=align;t.color=new Color(.12f,.085f,.055f);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Button Btn(Transform p,string n,float x,float y,float w,float h){var im=Pic(p,n,null,x,y,w,h);im.color=new Color(1,1,1,.001f);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.7f,.65f,.55f);b.colors=c;im.gameObject.AddComponent<ShelterButtonFeedback>();im.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");return b;}
    static RectTransform Row(Transform p,string n,float height){var r=Node(p,n);r.gameObject.AddComponent<LayoutElement>().preferredHeight=height;var border=Pic(r,"FineFrame",frame,0,0,488,height,ink);Full(border.rectTransform);border.type=Image.Type.Sliced;border.fillCenter=false;border.pixelsPerUnitMultiplier=1.4f;return r;}
    static void Heading(Transform p,string title){var r=Node(p,title);r.gameObject.AddComponent<LayoutElement>().preferredHeight=50;Txt(r,"Heading",title,12,8,410,38,25);var rule=Pic(r,"Rule",null,12,48,464,1);rule.color=new Color(.23f,.15f,.08f,.3f);}
    static Slider Volume(Transform content,string name,string label,out Text value)
    {
        var row=Row(content,name,142);Txt(row,"Name",label,22,12,270,36,24);value=Txt(row,"Value","100%",345,12,116,36,23,TextAnchor.MiddleRight);
        var r=Node(row,"VolumeSlider");Box(r,26,65,432,54);var slider=r.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.navigation=new Navigation{mode=Navigation.Mode.None};
        var hit=Pic(r,"HitArea",null,0,0,432,54);hit.color=new Color(1,1,1,.001f);hit.raycastTarget=true;
        var track=Pic(r,"Track",paper,0,19,432,15,paperMat);var edge=Pic(track.transform,"Frame",buttonFrame,0,0,432,15,ink);Full(edge.rectTransform);edge.type=Image.Type.Sliced;edge.fillCenter=false;edge.pixelsPerUnitMultiplier=3;
        var fillArea=Node(r,"FillArea");Box(fillArea,4,22,424,9);var fill=Pic(fillArea,"RedFill",null,0,0,424,9);Full(fill.rectTransform);fill.color=new Color(.46f,.075f,.045f);slider.fillRect=fill.rectTransform;
        var handleArea=Node(r,"HandleArea");Box(handleArea,17,0,398,54);var handle=Pic(handleArea,"WaxHandle",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Chest/Sprites/ReadySeal.asset"),0,7,35,35,ink);handle.rectTransform.pivot=Vector2.one*.5f;handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=Vector2.one*.5f;handle.rectTransform.anchoredPosition=Vector2.zero;handle.preserveAspect=true;handle.raycastTarget=true;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=1;
        var colors=slider.colors;colors.pressedColor=new Color(.7f,.6f,.5f);slider.colors=colors;return slider;
    }
    [MenuItem("Tools/Dark Fairytale/UI/Build Settings (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));Directory.CreateDirectory(Out);AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Chest/Sprites/BlankPaper.asset");frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Chest/Sprites/RewardPreviewFrame.asset");buttonFrame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/PaperButtonFrame.asset");paperMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Chest/Paper.mat");ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Chest/ColourCutout.mat");
        var root=Node(null,"FullScreenSettings");
        try{
            root.localScale=Vector3.one;root.pivot=Vector2.one*.5f;root.sizeDelta=new Vector2(540,960);
            var ui=root.gameObject.AddComponent<StorybookSettingsUI>();ui.group=root.gameObject.AddComponent<CanvasGroup>();
            var bg=Pic(root,"FullScreenBackdrop",paper,0,0,540,960,paperMat);Full(bg.rectTransform);bg.raycastTarget=true;
            var layout=root.gameObject.AddComponent<StorybookSettingsLayout>();var page=Node(root,"SafePage");Box(page,0,0,540,960);layout.page=page;
            var border=Pic(page,"ThornFrame",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Chest/Sprites/PageFrameV2.asset"),0,0,540,960);Full(border.rectTransform);border.type=Image.Type.Sliced;border.fillCenter=false;
            var header=Node(page,"Header");Box(header,8,8,524,99);
            var headerFrame=Pic(header,"Frame",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Fusion/Sprites/HeaderFrameClean.asset"),0,0,524,99,AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Fusion/FrameCutout.mat"));headerFrame.type=Image.Type.Sliced;headerFrame.fillCenter=false;
            var forestInk=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Fusion/InkCutout.mat");
            Pic(header,"ForestLeft",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Fusion/Sprites/ForestLeft.asset"),44,24,121,64,forestInk).preserveAspect=true;
            Pic(header,"ForestRight",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Fusion/Sprites/ForestRight.asset"),367,22,141,66,forestInk).preserveAspect=true;
            ui.back=Btn(header,"Back",5,-1,42,98);Pic(ui.back.transform,"Bookmark",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Fusion/Sprites/BackRibbon.asset"),0,0,42,98,forestInk).preserveAspect=true;
            Txt(header,"Title","设 置",172,14,188,48,33,TextAnchor.MiddleCenter);Txt(header,"Subtitle","庇护所 · 旅途偏好",134,64,268,25,18,TextAnchor.MiddleCenter);
            var viewport=Node(page,"SettingsViewport");Box(viewport,26,124,488,782);layout.viewport=viewport;viewport.gameObject.AddComponent<RectMask2D>();var target=Pic(viewport,"ScrollTarget",null,0,0,488,782);Full(target.rectTransform);target.color=new Color(1,1,1,.001f);target.raycastTarget=true;
            ui.scroll=viewport.gameObject.AddComponent<ScrollRect>();ui.scroll.viewport=viewport;ui.scroll.horizontal=false;ui.scroll.vertical=true;ui.scroll.movementType=ScrollRect.MovementType.Clamped;ui.scroll.scrollSensitivity=35;
            var content=Node(viewport,"SettingsSections");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,600);ui.scroll.content=content;
            var vertical=content.gameObject.AddComponent<VerticalLayoutGroup>();vertical.padding=new RectOffset(0,0,4,22);vertical.spacing=16;vertical.childControlWidth=true;vertical.childForceExpandWidth=true;vertical.childControlHeight=true;vertical.childForceExpandHeight=false;var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Heading(content,"声音");ui.music=Volume(content,"Music","音乐音量",out ui.musicValue);ui.sound=Volume(content,"Sound","音效音量",out ui.soundValue);
            Heading(content,"游戏体验");var experience=Row(content,"StartupPreferences",184);Txt(experience,"Title","跳过加载动画",22,17,310,38,25);
            var description=Txt(experience,"Description","开启后，下次启动加载完成即进入主界面，\n不播放翻书入场动画。",22,62,435,63,19);description.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
            ui.skip=Btn(experience,"SkipLoadingToggle",13,7,462,54);UnityEngine.Object.DestroyImmediate(ui.skip.GetComponent<ShelterButtonFeedback>());
            var seal=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Chest/Sprites/ReadySeal.asset");
            const string offPath="Assets/DarkFairytale/UI/Settings/UncheckedWax.mat";
            var offMat=AssetDatabase.LoadAssetAtPath<Material>(offPath);if(!offMat){offMat=new Material(Shader.Find("UI/Settings Wax Outline"));AssetDatabase.CreateAsset(offMat,offPath);}
            offMat.CopyPropertiesFromMaterial(ink);offMat.SetFloat("_Inner",.31f);offMat.SetFloat("_DisabledUI",1);var sr=seal.rect;offMat.SetVector("_SourceRect",new Vector4(sr.x/seal.texture.width,sr.y/seal.texture.height,sr.width/seal.texture.width,sr.height/seal.texture.height));EditorUtility.SetDirty(offMat);
            ui.skipOff=Pic(ui.skip.transform,"UncheckedWax",seal,398,5,43,43,offMat);ui.skipOff.preserveAspect=true;
            ui.skipMark=Pic(ui.skip.transform,"CheckedSeal",seal,398,5,43,43,ink);ui.skipMark.preserveAspect=true;ui.skipMark.enabled=false;
            ui.skip.targetGraphic=ui.skipOff;var sealColors=ui.skip.colors;sealColors.pressedColor=new Color(.65f,.65f,.65f,1);sealColors.highlightedColor=Color.white;sealColors.fadeDuration=.08f;ui.skip.colors=sealColors;
            ui.skipValue=Txt(experience,"State","已关闭",22,136,420,30,19);
            var footer=Node(page,"Footer");Box(footer,26,906,488,54);layout.footer=footer;Txt(footer,"SavedHint","修改后自动保存",10,-4,300,36,18);ui.version=Txt(footer,"Version","版本 "+Application.version,300,-4,178,36,17,TextAnchor.MiddleRight);
            layout.Apply();ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
        try{var h=home.GetComponent<ShelterHomeUI>();if(h.settingsUI)UnityEngine.Object.DestroyImmediate(h.settingsUI.gameObject);var child=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),home.transform);Full((RectTransform)child.transform);child.SetActive(false);h.settingsUI=child.GetComponent<StorybookSettingsUI>();ProductionUiArt.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(home);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
}
