using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookExperienceBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Experience",Out="output/experience-ui/";
    static Font font;static Texture2D source;
    static StorybookExperienceBuilder(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&File.Exists(Out+"build.request")){File.Delete(Out+"build.request");Build();}};}
    static Sprite Import(string name){string path=Root+"/"+name+".png";var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static Sprite Slice(string name,int x,int y,int w,int h,Vector4 border=default){string path=Root+"/"+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!s){s=Sprite.Create(source,new Rect(x,source.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);AssetDatabase.CreateAsset(s,path);}return s;}
    static RectTransform Node(Transform parent,string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h){var im=Node(p,n,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=s;im.raycastTarget=false;return im;}
    static void Full(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Text Text(Transform p,string n,string value,float x,float y,float w,float h,int size){var t=Node(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.color=new Color(.07f,.045f,.025f);t.text=value;t.verticalOverflow=VerticalWrapMode.Overflow;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string name,Sprite frame,Sprite fill,float y,bool red,out Text label){var im=Pic(p,name,Import(red?"RedButton":"IvoryButton"),36,y,368,70);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};b.gameObject.AddComponent<ShelterButtonFeedback>();b.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");label=Text(im.transform,"Caption","",69,0,287,70,25);if(red)label.color=new Color(.97f,.92f,.8f);return b;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Experience Page (Explicit)")]
    public static void Build(){
        Directory.CreateDirectory(Out);AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
        var paper=Import("Paper");var idle=Import("BookIdle");var active=Import("BookActive");Import("ApprovedPopup");source=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ApprovedPopup.png");
        var red=Slice("RedFrame",35,652,375,82,new Vector4(22,12,22,12));var ivory=Slice("IvoryFrame",35,573,375,70,new Vector4(22,12,22,12));
        var paperFill=Slice("PaperFill",300,554,20,10);var redFill=Slice("RedFill",170,660,20,10);
        var go=new GameObject("StorybookExperience",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        try{
            go.layer=5;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(470,836);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=go.AddComponent<StorybookExperienceUI>();ui.presentation=go.AddComponent<CanvasGroup>();
            var dim=Pic(go.transform,"DimAndBlockBackground",null,0,0,470,836);Full(dim.rectTransform);dim.color=new Color(0,0,0,.72f);dim.raycastTarget=true;
            var core=Node(go.transform,"SafeCore",0,0,470,836);var fit=go.AddComponent<BattleHudSafeArea>();fit.core=core;fit.referenceSize=new Vector2(470,836);
            // Design retains a 440x830 editable coordinate system, scaled uniformly to fit a floating window.
            var holder=Node(core,"ModalPlacement",15,78,440,680);holder.anchorMin=holder.anchorMax=holder.pivot=Vector2.one*.5f;holder.anchoredPosition=Vector2.zero;holder.localScale=Vector3.one*.79f;holder.sizeDelta=new Vector2(440,830);
            var panel=Pic(holder,"Paper",paper,0,0,440,830);ui.paper=panel.rectTransform;
            Text(panel.transform,"Title","经验加成",65,17,310,49,32);
            var close=Pic(panel.transform,"Close",null,379,13,44,44);close.color=Color.clear;close.raycastTarget=true;ui.close=close.gameObject.AddComponent<Button>();ui.close.targetGraphic=close;ui.close.transition=Selectable.Transition.None;close.gameObject.AddComponent<ShelterButtonFeedback>();close.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");var cross=Text(close.transform,"Cross","×",0,0,44,44,32);cross.color=new Color(.5f,.06f,.05f);
            Pic(panel.transform,"TitleDivider",Slice("Divider",112,61,219,16),112,61,219,16).material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/ReferenceInk.mat");
            ui.illustration=Pic(panel.transform,"Book",idle,34,88,374,222);ui.idleArt=idle;ui.activeArt=active;
            Text(panel.transform,"Benefit","经验获取 <color=#83180F><size=49>+33%</size></color>",30,315,380,62,29);
            var timerFrame=Pic(panel.transform,"TimerFrame",Slice("TimerFrame",38,388,371,169,new Vector4(20,12,20,12)),38,388,371,169);timerFrame.type=Image.Type.Sliced;timerFrame.fillCenter=false;timerFrame.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/ReferenceInk.mat");
            ui.state=Text(panel.transform,"State","当前未开启",66,397,307,31,23);ui.timer=Text(panel.transform,"Timer","持续 10 分钟",47,432,350,40,30);
            Pic(panel.transform,"DurationTrack",Import("DurationTrack"),56,480,340,37);
            ui.duration=Pic(panel.transform,"DurationFill",Import("DurationGold"),68,489,315,18);ui.duration.color=Color.white;ui.duration.type=Image.Type.Filled;ui.duration.fillMethod=Image.FillMethod.Horizontal;ui.duration.fillAmount=0;
            Text(panel.transform,"DurationRule","每次开启增加 10 分钟，可叠加时长",43,521,355,28,19);
            ui.advert=Button(panel.transform,"WatchAdvert",ivory,paperFill,573,false,out ui.adCaption);Pic(ui.advert.transform,"Film",Slice("Film",107,587,44,46),54,13,38,40).material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/ReferenceInk.mat");
            ui.paid=Button(panel.transform,"SpendBloodMoon",red,redFill,652,true,out ui.paidCaption);var crystal=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shop/Sprites/BloodMoonCrystal.asset");ui.adCaption.rectTransform.anchoredPosition=new Vector2(98,0);ui.adCaption.rectTransform.sizeDelta=new Vector2(260,70);var icon=Pic(ui.paid.transform,"Crystal",crystal,25,8,48,52);icon.preserveAspect=true;
            ui.balance=Text(panel.transform,"Balance","",65,735,315,33,21);Pic(panel.transform,"WalletIcon",crystal,107,734,25,33).preserveAspect=true;
            ui.notice=Text(panel.transform,"Feedback","",25,776,390,25,15);ui.notice.color=new Color(.5f,.06f,.05f);
            StorybookExperienceFullscreen.Apply(go);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/StorybookExperience.prefab");
        }finally{Object.DestroyImmediate(go);}
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/MainScenes/Gameplay.unity")throw new System.InvalidOperationException("Open Gameplay to install.");
        var gc=Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);if(gc.experienceUI)Object.DestroyImmediate(gc.experienceUI.gameObject);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/StorybookExperience.prefab"),scene);instance.SetActive(false);gc.experienceUI=instance.GetComponent<StorybookExperienceUI>();EditorUtility.SetDirty(gc);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Out+"build.txt","PASS");
    }
}
