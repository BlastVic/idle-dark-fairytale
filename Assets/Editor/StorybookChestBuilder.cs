using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring only; importing or playing never rebuilds the prefab.</summary>
[InitializeOnLoad]
public static class StorybookChestBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Chest",Path="Assets/Prefab/Addressable Prefabs/ChestPackage.prefab",Out="output/chest-ui/";
    static Texture2D art,clean;static Font font;static Material ink,frameInk,paperMaterial;static Sprite paper;
    static StorybookChestBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string path,Func<T> make) where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old)return old;var a=make();AssetDatabase.CreateAsset(a,path);return a;}
    static Sprite Slice(string name,float x,float y,float w,float h,bool original=false,Vector4 border=default)
    {var texture=original?art:clean;return Asset(Root+"/Sprites/"+name+".asset",()=>ProductionUiArt.Create(texture,new Rect(x,texture.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border));}
    static Sprite Polygon(string name,float x,float y,float w,float h,Vector2[] outline)
    {
        if(name=="CrystalRewardMesh"||name=="EquipmentRewardMesh"){
            var center=new Vector2(x+w*.5f,y+h*.5f);outline=Array.ConvertAll(outline,v=>{v=center+(v-center)*1.07f;return new Vector2(Mathf.Clamp(v.x,x,x+w),Mathf.Clamp(v.y,y,y+h));});
        }
        var sprite=Slice(name,x,y,w,h,true);
        var mat=Asset(Root+"/"+name+".mat",()=>new Material(Shader.Find("UI/Chest Authored Cutout")));
        mat.SetFloat("_Mode",6);mat.SetFloat("_PolygonCount",outline.Length);
        mat.SetVector("_SourceRect",new Vector4(x/art.width,(art.height-y-h)/art.height,w/art.width,h/art.height));
        for(int i=0;i<outline.Length;i++)mat.SetVector("_P"+i,new Vector4((outline[i].x-x)/w,1-(outline[i].y-y)/h,0,0));
        EditorUtility.SetDirty(mat);return sprite;
    }
    static Material Engraving(string name,Sprite sprite)
    {
        var mat=Asset(Root+"/"+name+".mat",()=>new Material(Shader.Find("UI/Chest Authored Cutout")));
        var r=sprite.rect;mat.SetFloat("_Mode",7);mat.SetFloat("_Cutoff",.62f);mat.SetFloat("_Feather",.15f);
        mat.SetVector("_SourceRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));EditorUtility.SetDirty(mat);return mat;
    }
    static RectTransform Node(Transform p,string n){var go=new GameObject(n,typeof(RectTransform));go.layer=5;go.transform.SetParent(p,false);return (RectTransform)go.transform;}
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h,Material m=null){var r=Node(p,n);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.material=m?m:(s?AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+s.name+".mat"):null);im.raycastTarget=false;return im;}
    static Text Txt(Transform p,string n,string text,float x,float y,float w,float h,int size){var r=Node(p,n);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.075f,.055f,.035f);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Button Btn(Transform p,string n,Sprite s,float x,float y,float w,float h,string caption,int size,out Text label)
    {var im=Pic(p,n,s,x,y,w,h,ink);im.raycastTarget=true;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=2;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.68f,.58f,.46f);c.disabledColor=Color.white;b.colors=c;im.gameObject.AddComponent<ShelterButtonFeedback>();im.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");label=Txt(im.transform,"Caption",caption,12,0,w-24,h,size);return b;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Chest Detail (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Out);AssetDatabase.Refresh();
        art=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Chest/Art/ApprovedChest.png");clean=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Chest/Art/ChestBlankLabels.png");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
        ink=Asset(Root+"/ColourCutout.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));ink.SetFloat("_Mode",0);ink.SetFloat("_InkOnly",0);ink.SetFloat("_Cutoff",.47f);ink.SetFloat("_Feather",.12f);EditorUtility.SetDirty(ink);
        frameInk=Asset(Root+"/FrameCutout.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));frameInk.SetFloat("_Mode",0);frameInk.SetFloat("_InkOnly",0);frameInk.SetFloat("_Cutoff",.42f);frameInk.SetFloat("_Feather",.14f);EditorUtility.SetDirty(frameInk);
        paper=Slice("BlankPaper",180,1090,160,130);
        paperMaterial=Asset(Root+"/Paper.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));paperMaterial.SetFloat("_Mode",2);paperMaterial.SetVector("_SourceRect",new Vector4(180f/941,(1672f-1090-130)/1672,160f/941,130f/1672));paperMaterial.SetVector("_Tiles",new Vector4(540f/160,960f/130,0,0));EditorUtility.SetDirty(paperMaterial);
        var root=Node(null,"ChestPackage");
        try{
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;root.gameObject.AddComponent<GraphicRaycaster>();var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=root.gameObject.AddComponent<StorybookChestUI>();var controller=root.gameObject.AddComponent<ChestPackage>();controller.view=ui;
            var layout=root.gameObject.AddComponent<StorybookFusionLayout>();layout.designSize=new Vector2(540,960);layout.margin=0;
            var outside=Pic(root,"SafeAreaBackdrop",null,0,0,540,960);outside.color=new Color(.018f,.018f,.016f);outside.raycastTarget=true;outside.rectTransform.anchorMin=Vector2.zero;outside.rectTransform.anchorMax=Vector2.one;outside.rectTransform.offsetMin=outside.rectTransform.offsetMax=Vector2.zero;
            var page=Pic(root,"ChestPage_9x16",paper,0,0,540,960,paperMaterial);layout.page=page.rectTransform;page.raycastTarget=true;
            var forest=Pic(page.transform,"ForestEngraving",Slice("ForestEngraving",18,150,425,237),12,88,516,288,frameInk);forest.preserveAspect=true;forest.material=Engraving("ForestSoft",forest.sprite);forest.color=new Color(1,1,1,.55f);
            var bottom=Pic(page.transform,"ForestFooter",Slice("ForestFooter",18,1490,425,142),18,772,504,169,frameInk);bottom.preserveAspect=true;bottom.material=Engraving("FooterSoft",bottom.sprite);bottom.color=new Color(1,1,1,.3f);
            var frame=Pic(page.transform,"ThornFrame",Slice("PageFrameV2",5,9,455,1648,false,new Vector4(17,24,12,15)),0,0,540,960);frame.type=Image.Type.Sliced;frame.fillCenter=false;
            ui.back=Btn(page.transform,"Back",Slice("BackBookmark",27,26,85,126,true),25,15,47,70,"",20,out _);ui.back.GetComponent<Image>().type=Image.Type.Simple;ui.back.GetComponent<Image>().preserveAspect=true;
            Txt(page.transform,"PageTitle","宝箱",165,22,210,45,32);
            Pic(page.transform,"WalletCrystal",Slice("BloodMoonCrystal",337,65,51,47,true),397,33,33,31,ink).preserveAspect=true;
            ui.balance=Txt(page.transform,"Balance","160",432,33,67,33,24);
            ui.title=Txt(page.transform,"ChestGrade","青铜宝箱",75,92,390,53,39);
            ui.source=Txt(page.transform,"Source","来自探索",78,147,384,29,21);ui.source.resizeTextForBestFit=true;ui.source.resizeTextMinSize=16;ui.source.resizeTextMaxSize=21;
            var outline=new[]{new Vector2(57,474),new Vector2(72,442),new Vector2(94,420),new Vector2(128,406),new Vector2(309,403),new Vector2(350,410),new Vector2(380,428),new Vector2(397,459),new Vector2(411,487),new Vector2(408,528),new Vector2(402,536),new Vector2(404,643),new Vector2(384,662),new Vector2(231,700),new Vector2(64,655),new Vector2(61,530),new Vector2(55,522)};
            ui.waitingChest=Polygon("ClosedChestMesh",24,376,420,360,outline);
            var readyOutline=Array.ConvertAll(outline,v=>v+new Vector2(475,0));
            ui.readyChest=Polygon("ReadyChestMesh",499,376,420,360,readyOutline);
            var ground=Pic(page.transform,"InkChestGround",Slice("ChestGroundInk",24,376,420,360,true),76,175,388,332,frameInk);ground.material=Engraving("GroundSoft",ground.sprite);
            ui.chest=Pic(page.transform,"ChestIllustration",ui.waitingChest,76,175,388,332);ui.chest.useSpriteMesh=false;ui.chest.preserveAspect=true;ui.waitingChestMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/ClosedChestMesh.mat");ui.readyChestMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/ReadyChestMesh.mat");
            var rewards=Pic(page.transform,"RewardPreviewFrame",Slice("RewardPreviewFrame",31,750,406,307,false,new Vector4(17,17,17,17)),38,500,464,188,frameInk);rewards.type=Image.Type.Sliced;rewards.fillCenter=false;
            Txt(rewards.transform,"PreviewTitle","开启可获得",35,8,394,33,25);
            var blue=Polygon("CrystalRewardMesh",71,844,118,139,new[]{new Vector2(73,944),new Vector2(86,909),new Vector2(93,922),new Vector2(110,873),new Vector2(151,848),new Vector2(168,886),new Vector2(161,917),new Vector2(180,928),new Vector2(179,951),new Vector2(165,969),new Vector2(150,978),new Vector2(106,974),new Vector2(85,962)});
            var blueImage=Pic(rewards.transform,"CrystalIllustration",blue,88,50,78,92);blueImage.useSpriteMesh=false;blueImage.preserveAspect=true;
            var glove=Polygon("EquipmentRewardMesh",263,842,130,141,new[]{new Vector2(267,927),new Vector2(294,899),new Vector2(303,875),new Vector2(326,850),new Vector2(344,844),new Vector2(374,852),new Vector2(389,870),new Vector2(389,884),new Vector2(365,910),new Vector2(354,943),new Vector2(336,974),new Vector2(314,981),new Vector2(280,965),new Vector2(265,945)});
            var gloveImage=Pic(rewards.transform,"EquipmentIllustration",glove,287,48,87,94);gloveImage.useSpriteMesh=false;gloveImage.preserveAspect=true;
            ui.rewardCaption=Txt(rewards.transform,"CrystalCaption","水晶",46,145,165,32,22);
            Txt(rewards.transform,"EquipmentCaption","随机装备",251,145,164,32,22);
            var divider=Pic(rewards.transform,"InkDivider",Slice("RewardDivider",229,848,12,152,true),230,53,3,87,frameInk);
            var waiting=Node(page.transform,"WaitingStatus");Box(waiting,35,708,470,92);ui.waiting=waiting.gameObject;
            Txt(waiting,"TimerLabel","距离免费开启",45,0,380,28,22);ui.timer=Txt(waiting,"Countdown","00:24:36",55,32,360,54,43);ui.timer.color=new Color(.46f,.055f,.035f);
            var ready=Node(page.transform,"ReadyStatus");Box(ready,70,736,400,55);ui.ready=ready.gameObject;
            Pic(ready,"ReadySeal",Slice("ReadySeal",572,1115,84,83,true),31,2,47,47,ink).preserveAspect=true;
            ui.readyCaption=Txt(ready,"ReadyCaption","已可免费开启",88,0,286,51,28);
            var paid=Node(page.transform,"PaidActions");Box(paid,35,810,470,132);ui.paidActions=paid.gameObject;
            var primary=Slice("PrimaryButton",501,1235,416,114,false,new Vector4(35,28,35,28));
            ui.pay=Btn(paid,"PayBloodMoon",primary,0,0,470,62,"2 血月结晶 · 立即开启",24,out ui.payCaption);ui.payCaption.color=new Color(.99f,.94f,.82f);Box(ui.payCaption.rectTransform,69,0,387,62);
            Pic(ui.pay.transform,"CostCrystal",Slice("CostCrystal",65,1268,57,50,true),29,15,35,32,ink).preserveAspect=true;
            var secondary=Slice("AdvertisementButton",32,1364,408,105,false,new Vector4(29,27,29,27));
            ui.advert=Btn(paid,"WatchAdvertisement",secondary,0,77,470,55,"观看广告开启",24,out var adLabel);Box(adLabel.rectTransform,142,0,242,55);ui.advert.GetComponent<Image>().fillCenter=false;
            // The source contains the play glyph; cover only that interior with matching paper before placing a separate icon.
            var adPaper=Pic(ui.advert.transform,"EditablePaperInterior",paper,22,12,426,31,paperMaterial);adPaper.transform.SetAsFirstSibling();
            Pic(ui.advert.transform,"PlayGlyph",Slice("PlayGlyph",128,1393,43,49,true),102,16,24,27,ink);adLabel.transform.SetAsLastSibling();
            var free=Node(page.transform,"FreeActions");Box(free,35,810,470,132);ui.freeActions=free.gameObject;
            ui.free=Btn(free,"FreeOpen",primary,0,0,470,62,"免费开启",31,out var freeCaption);freeCaption.color=new Color(.99f,.94f,.82f);
            ui.hint=Txt(free,"Hint","点击开启，领取战利品",10,81,450,30,21);
            ui.notice=Txt(page.transform,"Notice","",44,690,452,20,16);ui.notice.color=new Color(.46f,.055f,.035f);
            ready.gameObject.SetActive(false);free.gameObject.SetActive(false);
            layout.Apply();ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
}
