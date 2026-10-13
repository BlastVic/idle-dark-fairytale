using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Explicit authoring only; never rebuilds on import or play.</summary>
[InitializeOnLoad]
public static class StorybookRebirthBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Rebirth",Path="Assets/Prefab/Addressable Prefabs/RebirthPackage.prefab",Out="output/rebirth-ui/";
    static Texture2D art,blank,plate;static Font font;static Material cutout;
    static StorybookRebirthBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}}
    static RectTransform Node(Transform parent,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(parent,false);return(RectTransform)g.transform;}
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static Sprite Slice(Texture2D source,string name,float x,float y,float w,float h,Vector4 border=default){var p=Root+"/Sprites/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(p);if(old)return old;var s=Sprite.Create(source,new Rect(x,source.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);s.name=name;AssetDatabase.CreateAsset(s,p);return s;}
    static Image Pic(Transform parent,string name,Sprite sprite,float x,float y,float w,float h,Material material=null){var r=Node(parent,name);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.material=material;im.raycastTarget=false;return im;}
    static Text Txt(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor align=TextAnchor.MiddleCenter){var r=Node(parent,name);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.035f,.025f,.014f);t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string name,Sprite sprite,float x,float y,float w,float h,string caption,int size,out Text text){var im=Pic(p,name,sprite,x,y,w,h,sprite?cutout:null);im.raycastTarget=true;if(!sprite)im.color=new Color(1,1,1,.001f);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var colors=b.colors;colors.pressedColor=new Color(.68f,.58f,.48f);colors.disabledColor=Color.white;b.colors=colors;im.gameObject.AddComponent<ShelterButtonFeedback>();im.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");text=Txt(im.transform,"Caption",caption,10,0,w-20,h,size);if(sprite)text.color=new Color(.99f,.94f,.82f);return b;}
    static RectTransform Band(Transform page,string name,int y,int height,bool original=false){return Pic(page,name,Slice(original?art:plate,name,0,y,941,height),0,y,941,height).rectTransform;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Rebirth (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Out);AssetDatabase.Refresh();
        art=ShelterRefinedBuilder.Import(Root+"/Art/ApprovedRebirth.png");blank=ShelterRefinedBuilder.Import(Root+"/Art/RebirthBlankLabels.png");plate=ShelterRefinedBuilder.Import(Root+"/Art/RebirthBasePlate.png");font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
        cutout=AssetDatabase.LoadAssetAtPath<Material>(Root+"/AuthoredCutout.mat");if(!cutout){cutout=new Material(Shader.Find("UI/Chest Authored Cutout"));AssetDatabase.CreateAsset(cutout,Root+"/AuthoredCutout.mat");}cutout.SetFloat("_Mode",0);cutout.SetFloat("_InkOnly",0);cutout.SetFloat("_Cutoff",.62f);cutout.SetFloat("_Feather",.07f);EditorUtility.SetDirty(cutout);
        var package=PrefabUtility.LoadPrefabContents(Path);
        try{
            var owner=package.GetComponent<RebirthPackage>();if(owner.view)UnityEngine.Object.DestroyImmediate(owner.view.gameObject);else{owner.rebirthCanvas.SetActive(false);owner.rebirthCanvas.name="LegacyRebirthCanvas (inactive reference)";}
            foreach(var e in package.GetComponentsInChildren<EventSystem>(true))e.gameObject.SetActive(false);
            var root=Node(package.transform,"StorybookRebirth");var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;root.gameObject.AddComponent<GraphicRaycaster>();var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var bg=Pic(root,"SafeAreaBackground",null,0,0,941,1672);bg.color=new Color(.018f,.018f,.016f);bg.raycastTarget=true;bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.offsetMin=bg.rectTransform.offsetMax=Vector2.zero;
            var ui=root.gameObject.AddComponent<StorybookRebirthUI>();var layout=root.gameObject.AddComponent<StorybookFusionLayout>();layout.designSize=new Vector2(941,1672);layout.margin=0;var page=Node(root,"Core_9x16");Box(page,0,0,941,1672);layout.page=page;
            var header=Band(page,"Header",0,194);Txt(header,"Title","转 生",332,50,278,87,62);Txt(header,"Subtitle","庇护所 · 再启旅途",301,132,350,50,34);
            ui.back=Button(header,"BackBookmark",Slice(art,"BackBookmark",27,26,80,171),27,26,80,171,"",20,out _);
            Band(page,"ForestDoorway",194,436,true);
            var status=Band(page,"Status",630,100);ui.readySeal=Pic(status,"ReadyWaxSeal",Slice(art,"ReadyWaxSeal",145,646,70,71),145,16,70,71,cutout);ui.number=Txt(status,"RebirthNumber","第 1 次转生",228,16,210,66,35);ui.number.resizeTextForBestFit=true;ui.number.resizeTextMinSize=27;ui.number.resizeTextMaxSize=35;ui.requirement=Txt(status,"Requirement","Lv. 60 · 已满足条件",493,16,359,66,33);
            var rewards=Band(page,"RewardComparison",730,487);Txt(rewards,"Title","转生收益",356,22,228,68,47);ui.currentHeading=Txt(rewards,"CurrentHeading","现在转生",348,99,243,57,32);ui.nextHeading=Txt(rewards,"NextHeading","升至 Lv. 61",613,99,255,57,32);
            string[] names={"属性点","技能点","经验加成","起始水晶"};string[] defaults={"+10","+3","+50%","10,000"};ui.currentValues=new Text[4];ui.nextValues=new Text[4];var blue=new Color(.035f,.19f,.29f);
            for(int i=0;i<4;i++){float y=152+80*i;float iconY=i==3?1123:891+80*i;float iconHeight=i==3?80:76;Pic(rewards,"RewardIcon"+i,Slice(art,"RewardIconV2_"+i,88,iconY,82,iconHeight),88,iconY-730,82,iconHeight,cutout).preserveAspect=true;Txt(rewards,"RewardLabel"+i,names[i],198,y,133,79,30,TextAnchor.MiddleLeft);ui.currentValues[i]=Txt(rewards,"CurrentValue"+i,defaults[i],348,y,243,79,42);ui.nextValues[i]=Txt(rewards,"NextValue"+i,i==2?"+51%":i==3?"10,015":defaults[i],613,y,255,79,42);ui.currentValues[i].color=ui.nextValues[i].color=blue;foreach(var valueText in new[]{ui.currentValues[i],ui.nextValues[i]}){valueText.resizeTextForBestFit=true;valueText.resizeTextMinSize=27;valueText.resizeTextMaxSize=42;}}
            var reset=Band(page,"ResetSummary",1217,190);Txt(reset,"Title","转生后重置",345,14,254,60,38);Txt(reset,"ResetLevel","等级回到 Lv. 1",193,78,555,41,27);Txt(reset,"ResetItems","装备、技能、属性、融合与关卡进度重置",173,120,599,43,27);
            var footer=Band(page,"Actions",1407,265);var primary=Slice(blank,"PrimaryButton",164,1415,612,115,new Vector4(40,28,40,28));ui.rebirth=Button(footer,"Rebirth",primary,164,8,612,115,"转 生",59,out ui.rebirthCaption);ui.adventure=Button(footer,"ContinueAdventure",null,364,132,211,66,"继续冒险",35,out _);
            var overlay=Pic(page,"Confirmation",null,0,0,941,1672);overlay.color=new Color(0,0,0,.72f);overlay.raycastTarget=true;ui.confirmation=overlay.gameObject;
            var paperSprite=Slice(plate,"DialogPaperTile",800,1430,50,100);var paperMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/DialogPaper.mat");if(!paperMat){paperMat=new Material(Shader.Find("UI/Chest Authored Cutout"));AssetDatabase.CreateAsset(paperMat,Root+"/DialogPaper.mat");}paperMat.SetFloat("_Mode",2);paperMat.SetVector("_SourceRect",new Vector4(800f/941,(1672f-1530)/1672,50f/941,100f/1672));paperMat.SetVector("_Tiles",new Vector4(799f/50,605f/100,0,0));EditorUtility.SetDirty(paperMat);
            var panel=Pic(overlay.transform,"DialogPaper",paperSprite,71,533,799,605,paperMat);var dialogFrame=Pic(panel.transform,"ThornFrame",Slice(plate,"DialogFrameV2",49,737,844,880,new Vector4(20,18,20,18)),0,0,799,605);dialogFrame.type=Image.Type.Sliced;dialogFrame.fillCenter=false;
            Txt(panel.transform,"Title","确认转生",170,27,459,64,42);ui.confirmBody=Txt(panel.transform,"ResetAndRewardDetails","",46,117,707,273,28);
            ui.cancel=Button(panel.transform,"Cancel",primary,50,421,320,83,"继续冒险",31,out _);ui.confirm=Button(panel.transform,"ConfirmRebirth",primary,429,421,320,83,"确认转生",31,out _);ui.cancel.GetComponent<Image>().type=ui.confirm.GetComponent<Image>().type=Image.Type.Sliced;
            Txt(panel.transform,"Hint","确认后将开始新的旅途",110,522,579,42,24);ui.confirmation.SetActive(false);
            owner.view=ui;owner.rebirthCanvas=root.gameObject;owner.rebirthButton=ui.rebirth.gameObject;owner.closeButton=ui.back.gameObject;owner.AreYouSurePanel=ui.confirmation;owner.cutsceneObj.SetActive(false);
            layout.Apply();PrefabUtility.SaveAsPrefabAsset(package,Path);
        }finally{PrefabUtility.UnloadPrefabContents(package);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
}
