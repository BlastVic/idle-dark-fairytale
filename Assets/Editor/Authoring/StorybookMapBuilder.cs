using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit initial authoring, never automatic import/play-time rebuilding.</summary>
[InitializeOnLoad]
public static class StorybookMapBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Map";
    public const string Path="Assets/DarkFairytale/UI/Map/Prefabs/Chapter01Map.prefab";
    public const string Package="Assets/Prefab/Addressable Prefabs/Packages/MapPackageAuto.prefab";
    public const string Output="output/map-ui/";
    public static readonly string[] Keys={"Khorasan Ruins I","Khorasan Ruins II","test01","test02","Mount Zion"};
    public static readonly string[] Names={"暮色林缘","蘑菇森林","月下过桥","溪畔小屋","小屋内部"};
    static Texture2D reference,forest;static Font font,regular;static Sprite paper,red;static Material ink,colour;
    static StorybookMapBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Output+"build.request"))return;File.Delete(Output+"build.request");try{Build();File.WriteAllText(Output+"build-result.txt","PASS");}catch(Exception e){File.WriteAllText(Output+"build-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string path,Func<T> create)where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a)return a;a=create();AssetDatabase.CreateAsset(a,path);return a;}
    static Sprite Slice(string name,float x,float y,float w,float h,Vector4 border=default,Texture2D texture=null){var t=texture?texture:reference;return Asset(Root+"/Sprites/"+name+".asset",()=>{var s=ProductionUiArt.Create(t,new Rect(x,t.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);s.name=name;return s;});}
    static RectTransform Node(Transform p,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);return (RectTransform)g.transform;}
    static void Box(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Full(RectTransform r,float l=0,float t=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l,bottom);r.offsetMax=new Vector2(-right,-t);}
    static void Center(RectTransform r,float x,float y,float w,float h){Box(r,x-w*.5f,y,w,h);r.anchorMin=r.anchorMax=new Vector2(.5f,1);}
    static void Right(RectTransform r,float x,float y,float w,float h){Box(r,-x,y,w,h);r.anchorMin=r.anchorMax=new Vector2(1,1);}
    static Image Pic(Transform p,string name,Sprite sprite,float x,float y,float w,float h,Material mat=null){var r=Node(p,name);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.material=mat;im.color=Color.white;im.raycastTarget=false;return im;}
    static Text Text(Transform p,string name,string value,float x,float y,float w,float h,int size,Color? color=null){var r=Node(p,name);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color??Color.black;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string name,float x,float y,float w,float h){var im=Pic(p,name,null,x,y,w,h);im.color=new Color(1,1,1,.001f);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.96f,.88f);c.pressedColor=new Color(.72f,.62f,.48f);c.disabledColor=c.normalColor;c.fadeDuration=.08f;b.colors=c;b.navigation=new Navigation{mode=Navigation.Mode.None};b.gameObject.AddComponent<ShelterButtonFeedback>();b.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");return b;}
    static Material Surface(string name,Sprite s,int mode,float w,float h,float inner=0){var m=Asset(Root+"/Materials/"+name+".mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));var r=s.rect;var t=s.texture;m.SetFloat("_Mode",mode);m.SetFloat("_Inner",inner);m.SetVector("_SourceRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));m.SetVector("_Tiles",new Vector4(w/r.width,h/r.height,0,0));EditorUtility.SetDirty(m);return m;}
    static Image Paper(Transform p,string name,float w,float h,bool crimson=false){var s=crimson?red:paper;return Pic(p,name,s,0,0,w,h,Surface(name+"Paper",s,2,w,h));}
    static void Frame(Transform p,string name,Sprite s,float w,float h){var i=Pic(p,name,s,0,0,w,h);Full(i.rectTransform);i.type=Image.Type.Sliced;i.fillCenter=false;}
    static void Rule(Transform p,float x,float y,float w,float h){var im=Pic(p,"InkRule",null,x,y,w,h);im.color=new Color(.15f,.10f,.065f,.55f);}
    [MenuItem("Tools/Dark Fairytale/UI/Build Chapter 01 Map (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Output);
        reference=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Map/Art/ApprovedMap.png");forest=ShelterRefinedBuilder.Import(Root+"/Art/BlackForest.png");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");regular=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/BlankPaper.asset");red=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/BlankRedV2.asset");
        ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/Materials/ReferenceInk.mat");colour=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/Materials/ReferenceColour.mat");
        var root=Node(null,"Chapter01Map");
        try{
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();var ui=root.gameObject.AddComponent<StorybookMapUI>();var layout=root.gameObject.AddComponent<StorybookMapLayout>();ui.layout=layout;ui.controls=root.gameObject.AddComponent<CanvasGroup>();
            var black=Pic(root,"OutsideSupportedArea",null,0,0,941,1672);black.color=Color.black;black.raycastTarget=true;Full(black.rectTransform);
            var supported=Node(root,"SupportedViewport");supported.gameObject.AddComponent<RectMask2D>();layout.supported=supported;
            var core=Node(supported,"Core_9x16");layout.core=core;
            var viewport=Node(core,"MapViewport");layout.viewport=viewport;viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();ui.scroll=scroll;layout.scroll=scroll;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=true;scroll.decelerationRate=.08f;scroll.scrollSensitivity=35;scroll.viewport=viewport;
            var drag=viewport.gameObject.AddComponent<StorybookMapDrag>();drag.map=ui;
            var content=Node(viewport,"Chapter01Terrain");layout.content=content;scroll.content=content;
            var bg=Pic(content,"BlackForestBackground",Slice("BlackForest",0,0,forest.width,forest.height,default,forest),0,0,941,1672);bg.raycastTarget=true;
            var routes=Node(content,"RouteSegments");Full(routes);
            Vector2[][] routePoints={
                new[]{new Vector2(129,1122),new Vector2(156,1084),new Vector2(212,1032),new Vector2(183,967),new Vector2(137,911),new Vector2(122,865),new Vector2(160,820)},
                new[]{new Vector2(220,786),new Vector2(280,770),new Vector2(339,750),new Vector2(397,740),new Vector2(464,745)},
                new[]{new Vector2(548,739),new Vector2(598,719),new Vector2(642,682),new Vector2(693,646),new Vector2(728,641)},
                new[]{new Vector2(766,596),new Vector2(787,554),new Vector2(782,508),new Vector2(790,481)}
            };
            var routeImages=new Image[5][];routeImages[0]=Array.Empty<Image>();
            for(int i=0;i<4;i++){
                var group=Node(routes,"To_1-"+(i+2));Full(group);var dots=new System.Collections.Generic.List<Image>();
                for(int j=0;j<routePoints[i].Length-1;j++){
                    var a=routePoints[i][j];var b=routePoints[i][j+1];int count=Mathf.CeilToInt(Vector2.Distance(a,b)/23);
                    for(int k=0;k<count;k++){var v=Vector2.Lerp(a,b,(float)k/count);var dot=Pic(group,"Dot_"+dots.Count,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),v.x-6,v.y-4,12,8);dots.Add(dot);}
                }routeImages[i+1]=dots.ToArray();
            }
            ui.completedSeal=Slice("CompletedSeal",149,1055,82,82);ui.availableSeal=Slice("AvailableSeal",458,699,90,90);ui.lockedSeal=Slice("LockedSeal",713,594,84,84);
            ui.completedMaterial=Surface("CompletedCircle",ui.completedSeal,1,82,82);ui.availableMaterial=Surface("AvailableCircle",ui.availableSeal,1,90,90);ui.lockedMaterial=Surface("LockedCircle",ui.lockedSeal,1,84,84);
            var positions=new[]{new Vector2(190,1097),new Vector2(190,795),new Vector2(504,745),new Vector2(756,634),new Vector2(797,444)};
            var views=new[]{new Vector2(191,1065),new Vector2(125,711),new Vector2(485,744),new Vector2(744,500),new Vector2(747,431)};
            var labelRects=new[]{new Rect(145,1121,161,48),new Rect(138,825,161,50),new Rect(416,773,178,51),new Rect(726,665,153,49),new Rect(764,479,145,45)};
            var pin=Slice("OriginalLocationPin",487,658,38,53);var pinMaterial=Surface("LocationPin",pin,6,38,53);pinMaterial.shader=Shader.Find("UI/Storybook Map Cutout");
            ui.nodes=new StorybookMapNode[5];
            for(int i=0;i<5;i++){
                var b=Button(content,"Node_1-"+(i+1)+"_"+Names[i],positions[i].x-95,positions[i].y-48,220,161);var n=b.gameObject.AddComponent<StorybookMapNode>();ui.nodes[i]=n;n.levelKey=Keys[i];n.displayName=Names[i];n.button=b;
                n.seal=Pic(b.transform,"Seal",ui.completedSeal,51,4,88,88,ui.completedMaterial);
                var lr=labelRects[i];float lx=lr.x-(positions[i].x-95),ly=lr.y-(positions[i].y-48);
                var label=Paper(b.transform,"NodeLabel_"+i,lr.width,lr.height);Box(label.rectTransform,lx,ly,lr.width,lr.height);
                Frame(label.transform,"LabelFrame",Slice("NodeLabelFrameV2_"+i,lr.x,lr.y,lr.width,lr.height,new Vector4(7,7,7,7)),lr.width,lr.height);
                n.labelFrame=label.transform.Find("LabelFrame").GetComponent<Image>();
                n.label=Text(label.transform,"Label","1-"+(i+1)+"  "+Names[i],3,0,lr.width-6,lr.height,21);
                n.locator=Pic(b.transform,"SelectedMarker",pin,76,-39,38,53,pinMaterial).gameObject;
                n.progress=Text(b.transform,"WaveProgress","",lx,ly+lr.height+3,lr.width,25,17,new Color(.98f,.93f,.82f));var shade=n.progress.gameObject.AddComponent<Shadow>();shade.effectColor=new Color(0,0,0,.9f);shade.effectDistance=new Vector2(1,-1);
                if(i==4){n.customSeal=Slice("WolfSeal",753,401,87,87);n.customMaterial=Surface("WolfCircle",n.customSeal,1,87,87);var badge=Slice("WolfLock",816,424,46,47);n.lockBadge=Pic(b.transform,"LockedWolfBadge",badge,114,28,46,47,Surface("WolfLockCircle",badge,1,46,47)).gameObject;}
                n.preview=Slice("Preview_"+i,Mathf.Clamp(views[i].x-160,0,621),views[i].y-98,320,196,default,forest);n.incomingRoute=routeImages[i];
            }
            var header=Paper(core,"Header",941,151);layout.header=header.rectTransform;
            Frame(header.transform,"HeaderFrame",Slice("HeaderFrame",8,16,925,133,new Vector4(24,16,24,18)),941,151);
            var ht=Text(header.transform,"ChapterTitle","第一章 · 黑森林",0,38,640,87,53);Center(ht.rectTransform,0,38,640,87);
            Pic(header.transform,"LeftThorn",Slice("HeaderThornLeft",99,29,49,110),99,29,49,110,ink);
            var rt=Pic(header.transform,"RightThorn",Slice("HeaderThornRight",801,29,47,110),0,29,47,110,ink);Right(rt.rectTransform,140,29,47,110);
            var ls=Pic(header.transform,"LeftStar",Slice("HeaderStarLeft",235,72,29,37),235,72,29,37,ink);
            var rs=Pic(header.transform,"RightStar",Slice("HeaderStarRight",674,72,29,37),0,72,29,37,ink);Right(rs.rectTransform,267,72,29,37);
            var trees=Pic(header.transform,"LeftForestFlourish",Slice("HeaderTreesLeft",130,77,96,58),126,76,96,58,ink);
            var treesR=Pic(header.transform,"RightForestFlourish",Slice("HeaderTreesRight",698,82,99,54),0,81,99,54,ink);Right(treesR.rectTransform,246,81,99,54);
            ui.backButton=Button(header.transform,"BackToShelter",31,8,70,143);var back=Pic(ui.backButton.transform,"BookmarkBack",Slice("BackBookmark",31,8,68,147),0,0,68,147);
            ui.locateButton=Button(header.transform,"LocateSelection",0,37,89,88);Right((RectTransform)ui.locateButton.transform,112,37,89,88);Pic(ui.locateButton.transform,"LocateGlyph",Slice("LocateGlyph",844,52,65,68),12,10,65,68,ink);
            var detail=Paper(core,"SelectedNodeCard",925,184);layout.detail=detail.rectTransform;
            Frame(detail.transform,"DetailFrame",Slice("DetailFrame",8,1248,925,184,new Vector4(26,23,26,23)),925,184);
            ui.detailPreview=Pic(detail.transform,"NodePreview",ui.nodes[2].preview,43,26,218,132);ui.detailPreview.preserveAspect=false;
            var thumbFrame=Pic(detail.transform,"PreviewFrame",Slice("PreviewFrame",48,1273,222,134,new Vector4(5,5,5,5)),41,25,222,134);thumbFrame.type=Image.Type.Sliced;thumbFrame.fillCenter=false;
            Rule(detail.transform,277,27,1,131);
            ui.detailTitle=Text(detail.transform,"NodeTitle","1-3  月下过桥",296,25,330,55,35);ui.detailTitle.alignment=TextAnchor.MiddleLeft;
            ui.detailStatus=Text(detail.transform,"NodeState","当前探索",362,91,263,37,25);ui.detailStatus.alignment=TextAnchor.MiddleLeft;
            ui.detailWaves=Text(detail.transform,"WaveProgress","波次  0 / 10",365,132,266,29,20);ui.detailWaves.alignment=TextAnchor.MiddleLeft;
            Pic(detail.transform,"CrossedSwords",Slice("DetailSwords",302,1338,53,62),295,93,53,62,ink);Rule(detail.transform,296,84,319,1);
            ui.enterButton=Button(detail.transform,"EnterSelectedNode",0,58,246,68);Right((RectTransform)ui.enterButton.transform,292,58,246,68);
            var entry=ui.enterButton.GetComponent<Image>();entry.sprite=red;entry.material=Surface("EnterRed",red,2,246,68);entry.color=Color.white;
            Frame(entry.transform,"ButtonFrame",Slice("EnterFrame",635,1305,247,70,new Vector4(22,16,22,16)),246,68);
            ui.enterCaption=Text(entry.transform,"Caption","进入关卡",8,0,230,68,32,new Color(.99f,.93f,.81f));ui.enterCaption.font=regular;
            var navigation=Node(core,"Navigation");layout.navigation=navigation;var navPaper=Paper(navigation,"NavigationPaper",925,194);navPaper.rectTransform.anchorMax=new Vector2(1,1);navPaper.rectTransform.offsetMin=new Vector2(0,-194);navPaper.rectTransform.offsetMax=Vector2.zero;
            Frame(navPaper.transform,"NavigationFrame",Slice("NavigationFrame",8,1447,925,194,new Vector4(25,20,25,18)),925,194);
            var navNames=new[]{"装备","技能","庇护所","地图","商店"};var icons=new[]{Slice("Equipment",61,1479,97,91),Slice("SkillsTight",240,1481,97,84),Slice("Shelter",409,1470,120,99),Slice("Map",611,1466,110,100),Slice("Shop",782,1472,100,97)};
            ui.navigationButtons=new Button[5];
            for(int i=0;i<5;i++){
                var b=Button(navigation,"Nav_"+navNames[i],0,0,185,194);ui.navigationButtons[i]=b;var r=(RectTransform)b.transform;r.anchorMin=new Vector2(i/5f,1);r.anchorMax=new Vector2((i+1)/5f,1);r.offsetMin=new Vector2(0,-194);r.offsetMax=Vector2.zero;
                if(i==3){var book=Paper(b.transform,"SelectedMapBookmark",150,216,true);Center(book.rectTransform,0,-9,150,216);var top=Pic(book.transform,"BookmarkTop",Slice("MapBookmarkTop",572,1437,149,129),0,0,150,129);var bottom=Pic(book.transform,"BookmarkBottom",Slice("MapBookmarkBottom",572,1609,149,43),0,172,150,43);var left=Pic(book.transform,"BookmarkLeft",Slice("MapBookmarkLeft",572,1566,13,43),0,129,13,43);var right=Pic(book.transform,"BookmarkRight",Slice("MapBookmarkRight",710,1566,11,43),139,129,11,43);}
                else{var icon=Pic(b.transform,"Icon",icons[i],0,25,105,100,colour);Center(icon.rectTransform,0,25,105,100);icon.preserveAspect=true;}
                var caption=Text(b.transform,"Caption",navNames[i],0,121,180,47,i==3?32:29,i==3?new Color(.99f,.93f,.82f):Color.black);Center(caption.rectTransform,0,121,180,47);if(i==3)caption.font=regular;
                if(i<4){var line=Pic(b.transform,"Divider",Slice("MapNavDivider",183,1465,30,157),0,12,30,157,ink);Right(line.rectTransform,15,12,30,157);}
                if(i==1){var badge=Pic(b.transform,"SkillBadge",Slice("SkillBadge",318,1462,41,42),0,13,41,42);Center(badge.rectTransform,57,13,41,42);badge.material=Surface("SkillBadgeRing",badge.sprite,1,41,42,.4f);var fill=Pic(badge.transform,"RedInterior",red,4,4,33,34,Surface("SkillBadgeFill",red,3,33,34));ui.skillBadge=Text(badge.transform,"Count","0",0,0,41,42,26,new Color(.99f,.94f,.82f));}
            }
            StorybookMapInteractionUpgrade.Apply(root.gameObject);layout.Apply();ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        Attach();AssetDatabase.SaveAssets();
    }
    static void Attach()
    {
        if(!File.Exists(Output+"MapPackageAuto-before.prefab"))File.Copy(Package,Output+"MapPackageAuto-before.prefab");
        var go=PrefabUtility.LoadPrefabContents(Package);
        try{
            var package=go.GetComponent<MapPackageUI>();package.useLegacyTutorial=false;
            foreach(var behaviour in go.GetComponents<MonoBehaviour>())if(behaviour!=package)behaviour.enabled=false;
            foreach(Transform child in go.transform)if(child.name!="MapCamera")child.gameObject.SetActive(false);
            // Legacy maps/clouds remain archived in this prefab, inactive and unable to receive clicks.
            foreach(var behaviour in package.mapCam.GetComponents<MonoBehaviour>())behaviour.enabled=false;
            foreach(Transform child in package.mapCam.transform)child.gameObject.SetActive(false);
            var old=go.transform.Find("Chapter01Map");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var map=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),go.transform);map.name="Chapter01Map";map.SetActive(true);
            ProductionUiArt.SaveAsPrefabAsset(go,Package);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
}
