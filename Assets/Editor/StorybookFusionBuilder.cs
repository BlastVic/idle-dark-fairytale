using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit prefab authoring. A request file is an explicit command, never an import rebuild.</summary>
[InitializeOnLoad]
public static class StorybookFusionBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Fusion", Path="Assets/Prefab/Addressable Prefabs/FusionPackage.prefab", Out="output/fusion-ui/";
    static Texture2D source, clean;
    static Font font;
    static Sprite paper, frame;
    static Material cutout, frameCutout;
    static StorybookFusionBuilder() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Out+"build.request"))return;
        File.Delete(Out+"build.request");
        try { Build(); File.WriteAllText(Out+"build.txt","PASS"); }
        catch(Exception e) { File.WriteAllText(Out+"build.txt",e.ToString()); Debug.LogException(e); }
    }
    static T Asset<T>(string path,Func<T> make) where T:UnityEngine.Object
    { var old=AssetDatabase.LoadAssetAtPath<T>(path); if(old)return old; var asset=make();AssetDatabase.CreateAsset(asset,path);return asset; }
    static Sprite Slice(string name,float x,float y,float w,float h,Vector4 border=default)
    { return Asset(Root+"/Sprites/"+name+".asset",()=>Sprite.Create(source,new Rect(x,source.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border)); }
    static Sprite CleanSlice(string name,float x,float y,float w,float h,Vector4 border=default)
    { return Asset(Root+"/Sprites/"+name+".asset",()=>Sprite.Create(clean,new Rect(x,clean.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border)); }
    static RectTransform Node(Transform parent,string name)
    {var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    static void Box(RectTransform rt,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(rt,x,y,w,h);
    static Image Pic(Transform p,string n,Sprite sprite,float x,float y,float w,float h)
    {var r=Node(p,n);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;return im;}
    static Text Txt(Transform p,string n,string value,float x,float y,float w,float h,int size)
    {var r=Node(p,n);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.065f,.052f,.038f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Image Icon(Transform p,string n,Sprite sprite,float x,float y,float w,float h)
    {var im=Pic(p,n,sprite,x,y,w,h);im.material=cutout;im.preserveAspect=true;return im;}
    static RectTransform Panel(Transform p,string n,float x,float y,float w,float h,Sprite border=null)
    {var im=Pic(p,n,paper,x,y,w,h);
        var mat=Asset(Root+"/Paper_"+w+"_"+h+".mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));
        var rect=paper.rect;mat.SetFloat("_Mode",2);mat.SetVector("_SourceRect",new Vector4(rect.x/clean.width,rect.y/clean.height,rect.width/clean.width,rect.height/clean.height));mat.SetVector("_Tiles",new Vector4(w/rect.width,h/rect.height,0,0));EditorUtility.SetDirty(mat);im.material=mat;
        var f=Pic(im.transform,"Frame",border?border:frame,0,0,w,h);f.type=Image.Type.Sliced;f.fillCenter=false;f.material=frameCutout;return im.rectTransform;}
    static Button Button(Transform parent,string name,Sprite sprite,float x,float y,float w,float h,string caption,int size,out Text label)
    {
        var im=Pic(parent,name,sprite,x,y,w,h);im.raycastTarget=true;
        var button=im.gameObject.AddComponent<Button>();button.targetGraphic=im;button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors;colors.pressedColor=new Color(.65f,.6f,.55f);button.colors=colors;
        im.gameObject.AddComponent<ShelterButtonFeedback>();im.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
        label=Txt(im.transform,"Caption",caption,0,0,w,h,size);
        return button;
    }
    [MenuItem("Tools/Dark Fairytale/UI/Build Fusion (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Out);AssetDatabase.Refresh();
        source=ShelterRefinedBuilder.Import(Root+"/Art/ApprovedFusion.png");clean=ShelterRefinedBuilder.Import(Root+"/Art/FusionBlankLabels.png");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
        paper=CleanSlice("PaperContinuous",285,579,235,48);frame=CleanSlice("ThinPanelFrameClean",80,571,532,166,new Vector4(13,13,13,13));
        cutout=Asset(Root+"/InkCutout.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));
        cutout.SetFloat("_Mode",0);cutout.SetFloat("_InkOnly",0);cutout.SetFloat("_Cutoff",.48f);cutout.SetFloat("_Feather",.09f);EditorUtility.SetDirty(cutout);
        frameCutout=Asset(Root+"/FrameCutout.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));
        frameCutout.SetFloat("_Mode",0);frameCutout.SetFloat("_InkOnly",0);frameCutout.SetFloat("_Cutoff",.7f);frameCutout.SetFloat("_Feather",.06f);EditorUtility.SetDirty(frameCutout);
        var root=Node(null,"FusionPackage");
        try
        {
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            root.gameObject.AddComponent<GraphicRaycaster>();var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(595,1056);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=root.gameObject.AddComponent<StorybookFusionUI>();root.gameObject.AddComponent<FusionPackage>().view=ui;ui.referenceInk=cutout;
            var layout=root.gameObject.AddComponent<StorybookFusionLayout>();
            var outside=Pic(root,"OutsideSafeArea",null,0,0,595,1056);outside.rectTransform.anchorMin=Vector2.zero;outside.rectTransform.anchorMax=Vector2.one;outside.rectTransform.offsetMin=outside.rectTransform.offsetMax=Vector2.zero;outside.color=new Color(.018f,.018f,.016f);outside.raycastTarget=true;
            var page=Panel(root,"Core_9x16",0,0,595,1056,CleanSlice("OuterFrameClean",48,70,596,1054,new Vector4(14,16,14,16)));layout.page=page;
            var header=Panel(page,"Header",8,8,579,107,CleanSlice("HeaderFrameClean",53,75,585,116,new Vector4(12,10,12,12)));
            Icon(header,"ForestLeft",Slice("ForestLeft",109,101,139,80),48,26,133,70);
            Icon(header,"ForestRight",Slice("ForestRight",446,102,182,78),404,24,155,72);
            Txt(header,"Title","融 合",190,15,207,53,36);
            Txt(header,"Subtitle","庇护所 · 残存遗迹",148,70,295,28,20);
            ui.back=Button(header,"Back",Slice("BackRibbon",62,77,46,108),5,-1,46,108,"",20,out _);
            var wallet=Panel(page,"Wallet",8,121,579,66,CleanSlice("WalletFrameClean",53,194,585,62,new Vector4(12,10,12,10)));
            var crystal=Slice("BlueCrystalV2",204,197,55,49);
            Icon(wallet,"Crystal",crystal,148,6,54,52);
            ui.balance=Txt(wallet,"Balance","持有水晶  <color=#194E73>1,280</color>",205,5,267,56,29);
            ui.dormantRuin=Slice("RuinDormant",62,258,569,309);ui.chargedRuin=Slice("RuinCharged",701,258,569,309);
            ui.ruin=Pic(page,"ApprovedRuin",ui.dormantRuin,14,189,567,308);ui.ruin.preserveAspect=true;
            var progressPanel=Panel(page,"Progress",31,501,533,165);
            ui.level=Txt(progressPanel,"Level","融合等级   Lv.6 → Lv.7",24,10,485,43,28);
            Txt(progressPanel,"ProgressLabel","融合进度",163,55,208,24,21);
            Pic(progressPanel,"MeterBack",Slice("MeterBack",391,666,162,23),55,94,432,23);
            ui.progressFill=Pic(progressPanel,"MeterFill",Slice("MeterFill",772,666,425,23),55,94,432,23);ui.progressFill.type=Image.Type.Filled;ui.progressFill.fillMethod=Image.FillMethod.Horizontal;ui.progressFill.fillOrigin=0;ui.progressFill.fillAmount=180f/310;
            var meterFrame=Pic(progressPanel,"MeterFrame",CleanSlice("MeterFrameClean",98,646,501,60,new Vector4(38,15,38,15)),18,75,501,60);meterFrame.type=Image.Type.Sliced;meterFrame.fillCenter=false;meterFrame.material=frameCutout;
            ui.progress=Txt(progressPanel,"ProgressValue","180 / 310",91,129,350,30,25);ui.progress.color=new Color(.08f,.24f,.35f);
            var lower=Panel(page,"RewardsAndAction",31,676,533,360);
            Txt(lower,"RewardTitle","本次提升奖励",121,8,292,35,26);
            var rule=Pic(lower,"TitleRule",null,54,29,72,1);rule.color=new Color(.2f,.16f,.1f,.4f);
            rule=Pic(lower,"TitleRuleRight",null,410,29,72,1);rule.color=new Color(.2f,.16f,.1f,.4f);
            var vp=Node(lower,"RewardsViewport");Box(vp,28,48,477,92);vp.gameObject.AddComponent<RectMask2D>();
            var hit=Pic(vp,"ScrollTarget",null,0,0,477,92);hit.color=new Color(1,1,1,.001f);hit.raycastTarget=true;
            ui.rewardScroll=vp.gameObject.AddComponent<ScrollRect>();ui.rewardScroll.viewport=vp;ui.rewardScroll.horizontal=false;ui.rewardScroll.movementType=ScrollRect.MovementType.Clamped;ui.rewardScroll.scrollSensitivity=35;
            var content=Node(vp,"Content");Box(content,0,0,477,92);ui.rewardScroll.content=content;
            ui.statIcon=Slice("StatIcon",127,803,72,74);ui.experienceIcon=Slice("ExperienceIconV2",369,800,88,78);
            ui.skillIcon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Refined/Sprites/Icons_1.asset");if(!ui.skillIcon)ui.skillIcon=ui.experienceIcon;
            ui.rewards=new StorybookFusionUI.RewardSlot[8];
            for(int i=0;i<8;i++)
            {
                var card=Panel(content,"Reward_"+i,i%2*242,i/2*98,235,92,CleanSlice("RewardCardFrameClean",112,793,229,94,new Vector4(9,9,9,9)));
                var icon=Pic(card,"Icon",i==0?ui.statIcon:ui.experienceIcon,11,10,70,72);icon.material=cutout;icon.preserveAspect=true;
                var title=Txt(card,"Name",i==0?"属性点":"经验",83,8,143,32,23);var amount=Txt(card,"Amount",i==0?"+1":"+50",82,38,145,47,33);amount.color=new Color(.08f,.24f,.35f);
                amount.resizeTextForBestFit=true;amount.resizeTextMinSize=18;amount.resizeTextMaxSize=33;
                ui.rewards[i]=new StorybookFusionUI.RewardSlot{root=card.gameObject,icon=icon,title=title,amount=amount};card.gameObject.SetActive(i<2);
            }
            ui.rewardHint=Txt(lower,"RewardScrollHint","",70,141,393,20,15);
            // The wax seal is reduced to 32 high, with 16 units clear of the action button.
            ui.readySeal=Icon(lower,"ReadySeal",Slice("ReadySealV2",846,889,59,54),111,164,31,32).gameObject;ui.readySeal.SetActive(false);
            ui.costCrystal=Icon(lower,"CostCrystal",crystal,131,163,38,36).gameObject;
            ui.cost=Txt(lower,"CostOrReady","补满消耗 <color=#194E73>130</color> 水晶",160,163,328,36,21);
            ui.action=Button(lower,"PrimaryAction",CleanSlice("BlankButton",144,943,404,75),66,212,401,72,"注入水晶",33,out ui.actionCaption);ui.action.GetComponent<Image>().preserveAspect=true;ui.action.GetComponent<Image>().material=cutout;
            ui.actionCaption.color=new Color(.99f,.94f,.83f);
            ui.hint=Txt(lower,"Hint","消耗水晶，补满融合进度",16,292,501,28,19);
            ui.returnToShelter=Button(lower,"ReturnToShelter",null,120,322,293,34,"返回庇护所",21,out _);ui.returnToShelter.GetComponent<Image>().color=new Color(1,1,1,.001f);
            layout.Apply();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Path);
        }
        finally {UnityEngine.Object.DestroyImmediate(root.gameObject);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
}
