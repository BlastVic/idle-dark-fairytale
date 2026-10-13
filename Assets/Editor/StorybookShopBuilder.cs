using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring only; never automatically rebuilds on import or Play.</summary>
[InitializeOnLoad]
public static class StorybookShopBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shop",Path="Assets/Prefab/Addressable Prefabs/CashMallPackage.prefab",Out="output/shop-ui/";
    static Font font;static Sprite paper,frame,red;static Texture2D source;static Material ink;
    static StorybookShopBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string p,Func<T> make) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(p);if(a)return a;a=make();AssetDatabase.CreateAsset(a,p);return a;}
    static RectTransform Node(Transform p,string n){var g=new GameObject(n,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);return (RectTransform)g.transform;}
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static void Full(RectTransform r,float l=0,float t=0,float right=0,float b=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l,b);r.offsetMax=new Vector2(-right,-t);}
    static Sprite Slice(string n,float x,float y,float w,float h,Vector4 border=default){return Asset(Root+"/Sprites/"+n+".asset",()=>Sprite.Create(source,new Rect(x,source.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border));}
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h){var r=Node(p,n);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.raycastTarget=false;return im;}
    static Text Txt(Transform p,string n,string s,float x,float y,float w,float h,int size){var r=Node(p,n);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.075f,.055f,.035f);t.text=s;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static RectTransform Panel(Transform p,string n,float x,float y,float w,float h,Sprite border=null){var im=Pic(p,n,paper,x,y,w,h);im.type=Image.Type.Tiled;var f=Pic(im.transform,"Frame",border?border:frame,0,0,w,h);Full(f.rectTransform);f.type=Image.Type.Sliced;f.fillCenter=false;return im.rectTransform;}
    static Button Button(Transform p,string n,float x,float y,float w,float h,out Text caption){var im=Pic(p,n,red,x,y,w,h);im.type=Image.Type.Tiled;im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.65f,.55f,.5f);b.colors=c;im.gameObject.AddComponent<ShelterButtonFeedback>();im.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");var f=Pic(im.transform,"Frame",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/RedButtonFrame.asset"),0,0,w,h);Full(f.rectTransform);f.type=Image.Type.Sliced;f.fillCenter=false;caption=Txt(im.transform,"Price","暂不可用",0,0,w,h,40);caption.color=new Color(.98f,.94f,.82f);return b;}
    static Sprite Extract(string name,int x,int y,int w,int h){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/ShopExtracted.png");return Asset(Root+"/Sprites/"+name+".asset",()=>Sprite.Create(tex,ShelterRefinedBuilder.Trim(tex,new RectInt(x,tex.height-y-h,w,h)),Vector2.one*.5f,100));}
    static void Vignette(Image image,bool left){var m=Asset(Root+(left?"/ForestFade.mat":"/StallFade.mat"),()=>new Material(Shader.Find("UI/Shop Vignette Fade")));var r=image.sprite.rect;var tex=image.sprite.texture;m.SetVector("_SourceRect",new Vector4(r.x/tex.width,r.y/tex.height,r.width/tex.width,r.height/tex.height));m.SetFloat("_Mode",0);m.SetFloat("_InkOnly",0);m.SetFloat("_Cutoff",1);m.SetFloat("_Feather",.001f);m.SetVector("_EdgeFade",left?new Vector4(0,.16f,.08f,0):new Vector4(.12f,0,.12f,0));EditorUtility.SetDirty(m);image.material=m;image.preserveAspect=true;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Shop (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Out);AssetDatabase.Refresh();
        source=ShelterRefinedBuilder.Import(Root+"/Art/ApprovedShop.png");ShelterRefinedBuilder.Import(Root+"/Art/ShopExtracted.png");font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset");frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/PanelFrameV3.asset");red=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/BlankRedV2.asset");
        ink=Asset(Root+"/IllustrationInk.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));ink.SetFloat("_Mode",0);ink.SetFloat("_InkOnly",0);ink.SetFloat("_Cutoff",.48f);ink.SetFloat("_Feather",.13f);EditorUtility.SetDirty(ink);
        var tex=ShelterRefinedBuilder.Import(Root+"/Art/BloodMoonCrystal.png");var icon=Asset(Root+"/Sprites/BloodMoonCrystal.asset",()=>Sprite.Create(tex,ShelterRefinedBuilder.Trim(tex,new RectInt(0,0,tex.width,tex.height)),Vector2.one*.5f,100));
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(StorybookEquipmentBuilder.Path).GetComponent<StorybookEquipmentUI>();var root=Node(null,"CashMallPackage");
        try{
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;root.gameObject.AddComponent<GraphicRaycaster>();var scale=root.gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(941,1672);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=root.gameObject.AddComponent<StorybookShopUI>();root.gameObject.AddComponent<CashMallPackage>().view=ui;var layout=root.gameObject.AddComponent<StorybookSkillsLayout>();
            var black=Pic(root,"OutsideSafeArea",null,0,0,941,1672);Full(black.rectTransform);black.color=new Color(.02f,.018f,.016f);black.raycastTarget=true;
            layout.supported=Node(root,"SupportedViewport");layout.supported.gameObject.AddComponent<RectMask2D>();layout.core=Node(layout.supported,"Core_9x16");
            layout.header=UnityEngine.Object.Instantiate(original.layout.header,layout.core);layout.header.name="Header";layout.headerContent=(RectTransform)layout.header.Find("Contents");layout.headerContent.Find("Title").GetComponent<Text>().text="商 店";ui.playerLevel=layout.headerContent.Find("PlayerLevel").GetComponent<Text>();ui.subjectNumber=layout.headerContent.Find("SubjectNumber").GetComponent<Text>();ui.back=layout.headerContent.Find("BackToShelter").GetComponent<Button>();
            var sharedNav=StorybookNavigationAuthoring.Instantiate(layout.core,root,4);
            layout.navigation=(RectTransform)sharedNav.transform;layout.navigationContent=null;ui.navigation=sharedNav.buttons;
            layout.points=Panel(layout.core,"Wallet",8,158,925,102);layout.pointsContent=Node(layout.points,"Contents");Box(layout.pointsContent,0,0,925,102);Pic(layout.pointsContent,"BloodMoonCrystal",icon,235,12,95,76).preserveAspect=true;ui.balance=Txt(layout.pointsContent,"Balance","血月结晶 240",334,7,445,88,39);
            layout.tree=Panel(layout.core,"ShopPage",8,270,925,1230);var vp=Node(layout.tree,"Viewport");Full(vp,15,15,15,15);vp.gameObject.AddComponent<RectMask2D>();var bg=Pic(vp,"ScrollSurface",null,0,0,895,1200);Full(bg.rectTransform);bg.color=new Color(1,1,1,.001f);bg.raycastTarget=true;
            var sc=vp.gameObject.AddComponent<ScrollRect>();sc.viewport=vp;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=35;var content=Node(vp,"Content");Box(content,0,0,895,1125);sc.content=content;ui.scroll=sc;layout.treeScroll=sc;
            // Two original ink vignette pieces leave a clean central area for editable labels.
            Vignette(Pic(content,"ForestLeft",Extract("ForestCutout",350,780,285,320),0,0,280,235),true);Vignette(Pic(content,"MarketRight",Extract("StallCutout",900,780,350,320),675,0,220,235),false);
            Txt(content,"ShopTitle","林间补给",260,60,380,65,46);Txt(content,"ShopSubtitle","为下一段旅途，备好补给",245,134,420,42,25);
            var cardBorder=Slice("CardFrame",78,458,538,232,new Vector4(20,20,20,20));
            Sprite pouch=Extract("PouchCutout",0,100,595,540),chest=Extract("ChestCutout",610,150,655,495);
            for(int i=0;i<2;i++){var card=Panel(content,i==0?"Pouch80":"Chest500",20,245+i*373,855,353,cardBorder);var art=Pic(card,"ApprovedIllustration",i==0?pouch:chest,20,25,380,300);art.preserveAspect=true;Txt(card,"Name",i==0?"一袋月晶":"一匣月晶",420,27,405,55,39);var rule=Pic(card,"Rule",null,440,96,345,1);rule.color=new Color(.2f,.15f,.1f,.45f);var amount=Txt(card,"Amount",i==0?"80":"500",434,112,175,90,62);amount.color=new Color(.5f,.035f,.025f);Txt(card,"Currency","血月结晶",610,145,200,48,28);var b=Button(card,"Purchase",435,233,377,90,out var caption);caption.text=i==0?"$0.99":"$4.99";if(i==0){ui.buy80=b;ui.price80=caption;}else{ui.buy500=b;ui.price500=caption;}}
            ui.status=Txt(content,"Status","价格以商店显示为准",80,1017,735,72,25);
            var reward=Node(layout.core,"RewardOverlay");Full(reward);ui.reward=reward.gameObject;ui.rewardGroup=reward.gameObject.AddComponent<CanvasGroup>();var blocker=Pic(reward,"InputBlocker",null,0,0,941,1672);Full(blocker.rectTransform);blocker.color=new Color(0,0,0,.64f);blocker.raycastTarget=true;
            var rp=Panel(reward,"RewardPaper",0,0,630,785);rp.anchorMin=rp.anchorMax=rp.pivot=Vector2.one*.5f;rp.anchoredPosition=Vector2.zero;
            var seal=Pic(rp,"ApprovedWaxSeal",Extract("WaxSealCutout",55,790,280,320),260,-40,115,123);Txt(rp,"Title","补给已送达",30,95,570,75,52);
            var rewardArt=Pic(rp,"ApprovedChest",chest,70,207,490,330);rewardArt.preserveAspect=true;ui.rewardAmount=Txt(rp,"Amount","血月结晶 +500",20,555,590,72,43);Txt(rp,"Stored","已存入背包",40,628,550,50,33);Txt(rp,"ReturnHint","即将返回庇护所",40,714,550,42,26);reward.gameObject.SetActive(false);ui.rewardGroup.blocksRaycasts=false;
            layout.Apply();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        ReplaceCurrency(icon);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static void ReplaceCurrency(Sprite icon)
    {
        string[] old={"216f6244fd4724c468fcdd6a6f35c6b4","8ba8c10128a33884a9ec3f521561f64b","e01a45e29f2994669a071a43487f24a0"};
        string guid=AssetDatabase.AssetPathToGUID(Root+"/Sprites/BloodMoonCrystal.asset");int count=0;
        foreach(var path in Directory.GetFiles("Assets","*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".prefab")||p.EndsWith(".unity"))){string s=File.ReadAllText(path),o=s;foreach(var g in old)s=System.Text.RegularExpressions.Regex.Replace(s,@"\{fileID: -?\d+, guid: "+g+@", type: \d+\}","{fileID: 21300000, guid: "+guid+", type: 2}");s=s.Replace("You got daily cherries!","每日血月结晶已送达！").Replace("Uh oh, out of cherries?  Let's visit the cash mall for the first time!","血月结晶不足，去商店补充一些吧！");s=System.Text.RegularExpressions.Regex.Replace(s,@"(?ms)^--- !u!114.*?(?=^--- !u!|\z)",m=>{string b=m.Value;if(b.Contains("m_Sprite: {fileID: 21300000, guid: "+guid)){b=System.Text.RegularExpressions.Regex.Replace(b,@"m_Material: \{[^}]*\}","m_Material: {fileID: 0}");b=b.Replace("m_PreserveAspect: 0","m_PreserveAspect: 1");}if(b.Contains("每日血月结晶")||b.Contains("血月结晶不足，去商店"))b=System.Text.RegularExpressions.Regex.Replace(b,@"m_Font: \{[^}]*\}","m_Font: {fileID: 12800000, guid: 0224f00f4c40b4055a447ab78bff7114, type: 3}");return b;});if(s!=o){File.WriteAllText(path,s);count++;}}
        File.WriteAllText(Out+"currency-replacements.txt","Updated serialized currency references in "+count+" prefabs/scenes. Legacy IAP IDs and saved balance preserved.");
    }
}
