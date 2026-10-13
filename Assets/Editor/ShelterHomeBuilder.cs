using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit one-time authoring. Existing prefabs are never regenerated or overwritten.</summary>
public static class ShelterHomeBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shelter";
    public const string Path=Root+"/ShelterHome.prefab";
    const string Old="Assets/DarkFairytale/UI/CursedStorybook";
    static Font font;
    static Sprite[] icons;
    static Color ink=new Color(.12f,.10f,.09f), red=new Color(.36f,.055f,.055f), ivory=new Color(.94f,.89f,.78f);
    static Sprite Art(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>(Old+"/Sprites/"+n+".asset");
    [InitializeOnLoadMethod] static void Watch() { EditorApplication.update-=Poll;EditorApplication.update+=Poll; }
    static void Poll() {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(File.Exists("output/home-ui/polish.request")) {
            File.Delete("output/home-ui/polish.request");
            try { Polish();File.WriteAllText("output/home-ui/polish-result.txt","PASS"); } catch(Exception e){File.WriteAllText("output/home-ui/polish-result.txt",e.ToString());} return;
        }
        string request="output/home-ui/build.request";
        if(!File.Exists(request))return;
        File.Delete(request);
        try { Build();File.WriteAllText("output/home-ui/build-result.txt","PASS"); }
        catch(Exception e){File.WriteAllText("output/home-ui/build-result.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Dark Fairytale/UI/Create and Install Shelter Prefab (Once)")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        Directory.CreateDirectory(Root+"/Sprites");
        font=AssetDatabase.LoadAssetAtPath<Font>(Old+"/Fonts/NotoSerifCJKsc-Regular.otf");
        if(!File.Exists(Path)) {
            var scene=Import("ShelterScene.png"); var sheet=Import("ShelterIcons.png");
            icons=new Sprite[16];
            for(int i=0;i<16;i++) icons[i]=Slice(sheet,"Icon_"+i,new Rect(i%4*sheet.width/4,(3-i/4)*sheet.height/4,sheet.width/4,sheet.height/4));
            var background=Slice(scene,"ShelterScene",new Rect(0,0,scene.width,scene.height));
            Create(background);
            Polish();
        }
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.isDirty) EditorSceneManager.SaveScene(active);
        if(active.path!="Assets/Scenes/MainScenes/Gameplay.unity") active=EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
        var gc=UnityEngine.Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);
        if(!gc.homeUI) {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),active);
            gc.homeUI=go.GetComponent<ShelterHomeUI>();go.SetActive(false);EditorUtility.SetDirty(gc);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(active);EditorSceneManager.SaveScene(active);
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(Path);
    }
    static void Polish() {
        var texture=Import("ShelterPaper.png");
        string[] names={"CleanPaper","RedRibbon","RoundPaper","CleanButton"};
        Rect[] rects={new Rect(52,1024-440,680,370),new Rect(794,1024-354,712,200),new Rect(142,1024-986,487,477),new Rect(826,1024-866,637,227)};
        Vector4[] borders={new Vector4(65,55,65,55),new Vector4(85,42,85,42),Vector4.zero,new Vector4(45,38,45,38)};
        var assets=new Sprite[4];
        for(int i=0;i<4;i++) {
            string path=Root+"/Sprites/"+names[i]+".asset";
            assets[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!assets[i]) {assets[i]=Sprite.Create(texture,rects[i],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,borders[i]);AssetDatabase.CreateAsset(assets[i],path);}
        }
        var go=PrefabUtility.LoadPrefabContents(Path);
        try {
            var ui=go.GetComponent<ShelterHomeUI>();
            foreach(var t in go.GetComponentsInChildren<Text>(true)) {t.fontStyle=FontStyle.Bold;}
            foreach(var img in go.GetComponentsInChildren<Image>(true)) {
                if(img.sprite==Art("Parchment")){img.sprite=assets[0];img.pixelsPerUnitMultiplier=1.5f;}
                else if(img.sprite==Art("PaperButton")){img.sprite=assets[3];img.pixelsPerUnitMultiplier=1.5f;}
            }
            foreach(var b in ui.functionButtons) {var img=(Image)b.targetGraphic;img.sprite=assets[2];img.type=Image.Type.Simple;}
            ((Image)ui.battleButton.targetGraphic).sprite=assets[1];ui.battleButton.targetGraphic.color=Color.white;
            ui.battleButton.GetComponent<Image>().pixelsPerUnitMultiplier=1;
            var illustration=(RectTransform)go.transform.Find("Core_9x16/ShelterIllustration");
            illustration.anchoredPosition=new Vector2(0,-70);illustration.sizeDelta=new Vector2(1080,1440);
            // Keep a continuous dark environment behind the bottom navigation too.
            var extra=go.transform.Find("Core_9x16/LowerBackdrop");
            if(!extra) {var img=Pic("LowerBackdrop",illustration.parent,illustration.GetComponent<Image>().sprite,0,1430,1080,1440);img.color=new Color(.28f,.25f,.23f);img.transform.SetSiblingIndex(0);}
            var core=go.transform.Find("Core_9x16");if(!core.GetComponent<RectMask2D>())core.gameObject.AddComponent<RectMask2D>();
            var floor=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/ShelterFloor.asset");
            if(!floor) {var textureScene=illustration.GetComponent<Image>().sprite.texture;floor=Sprite.Create(textureScene,new Rect(0,0,textureScene.width,400),new Vector2(.5f,.5f),100);AssetDatabase.CreateAsset(floor,Root+"/Sprites/ShelterFloor.asset");}
            var lower=(RectTransform)core.Find("LowerBackdrop");lower.GetComponent<Image>().sprite=floor;lower.GetComponent<Image>().color=new Color(.65f,.61f,.56f);lower.anchoredPosition=new Vector2(0,-1490);lower.sizeDelta=new Vector2(1080,1080f*400/floor.texture.width);
            foreach(var slider in new[]{ui.musicSlider,ui.soundSlider}) {
                slider.fillRect.sizeDelta=new Vector2(0,-32);slider.fillRect.pivot=new Vector2(0,.5f);slider.fillRect.anchoredPosition=new Vector2(0,0);
                slider.handleRect.sizeDelta=new Vector2(36,0);slider.handleRect.pivot=new Vector2(.5f,.5f);slider.handleRect.anchoredPosition=Vector2.zero;
            }
            var portrait=(RectTransform)go.transform.Find("Core_9x16/TopStatus/Portrait");portrait.sizeDelta=new Vector2(108,113);portrait.anchoredPosition=new Vector2(24,-8);
            var closeImage=(Image)ui.modalClose.targetGraphic;closeImage.sprite=null;closeImage.color=new Color(.78f,.72f,.61f);ui.modalClose.GetComponentInChildren<Text>().fontSize=40;ui.modalClose.GetComponentInChildren<Text>().resizeTextMaxSize=40;
            ui.numberText.rectTransform.anchoredPosition=new Vector2(16,-128);ui.numberText.fontSize=22;ui.numberText.resizeTextMaxSize=22;
            ui.collapseButton.targetGraphic.GetComponent<Image>().sprite=null;ui.collapseButton.targetGraphic.color=new Color(1,1,1,.08f);ui.settingsButton.targetGraphic.GetComponent<Image>().sprite=null;ui.settingsButton.targetGraphic.color=new Color(1,1,1,.08f);ui.levelText.fontSize=52;ui.levelText.resizeTextMaxSize=52;
            foreach(var b in ui.statButtons) { var img=(Image)b.targetGraphic; img.sprite=null;img.color=new Color(.78f,.72f,.61f); if(!b.GetComponent<Outline>()) {var outline=b.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.2f,.14f,.1f);outline.effectDistance=new Vector2(1,-1);} var txt=b.GetComponentInChildren<Text>();txt.fontSize=40;txt.resizeTextMaxSize=40;}
            PrefabUtility.SaveAsPrefabAsset(go,Path);
        } finally {PrefabUtility.UnloadPrefabContents(go);}
        AssetDatabase.SaveAssets();
    }
    static Texture2D Import(string name) {
        string p=Root+"/"+name;AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=TextureImporterType.Default;i.alphaIsTransparency=true;
        i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=2048;i.wrapMode=TextureWrapMode.Clamp;i.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    static Sprite Slice(Texture2D t,string name,Rect r) {
        var s=Sprite.Create(t,r,new Vector2(.5f,.5f),100);s.name=name;AssetDatabase.CreateAsset(s,Root+"/Sprites/"+name+".asset");return s;
    }
    // Coordinates use the parent's top-left, y increases down. All positions remain serialized.
    static RectTransform Node(string name,Transform parent,float x,float y,float w,float h) {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Image Pic(string name,Transform parent,Sprite sprite,float x,float y,float w,float h,bool sliced=false) {
        var img=Node(name,parent,x,y,w,h).gameObject.AddComponent<Image>();img.sprite=sprite;img.raycastTarget=false;
        img.type=sliced?Image.Type.Sliced:Image.Type.Simple; if(sliced)img.pixelsPerUnitMultiplier=2.8f;
        return img;
    }
    static Text Txt(string name,Transform parent,string value,float x,float y,float w,float h,int size,Color? color=null) {
        var t=Node(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color??ink;
        t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(16,size-8);t.resizeTextMaxSize=size;
        return t;
    }
    static Button Btn(string name,Transform parent,string text,float x,float y,float w,float h,Sprite sprite=null) {
        var img=Pic(name,parent,sprite?sprite:Art("PaperButton"),x,y,w,h,true);img.raycastTarget=true;
        var b=img.gameObject.AddComponent<Button>();b.targetGraphic=img;b.navigation=new Navigation{mode=Navigation.Mode.None};
        var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.93f,.78f);c.pressedColor=new Color(.62f,.49f,.39f);c.disabledColor=c.normalColor;c.fadeDuration=.08f;b.colors=c;
        b.gameObject.AddComponent<ShelterButtonFeedback>();b.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
        if(!string.IsNullOrEmpty(text))Txt("Label",b.transform,text,4,2,w-8,h-4,30);
        return b;
    }
    static void Icon(Transform p,int index,float x,float y,float size) { var i=Pic("Icon",p,icons[index],x,y,size,size);i.preserveAspect=true; }
    static void Line(Transform p,float x,float y,float w,float h) { var i=Pic("InkRule",p,null,x,y,w,h);i.color=new Color(.18f,.12f,.08f,.28f); }
    static Image Meter(Transform p,string name,float x,float y,float w) {
        var bg=Pic(name+"Frame",p,Art("MeterFrame"),x,y,w,40,true);
        var f=Pic(name+"Fill",p,Art(name=="Health"?"HealthInk":"ExperienceInk"),x+10,y+8,w-20,24);
        f.type=Image.Type.Filled;f.fillMethod=Image.FillMethod.Horizontal;return f;
    }
    static Slider Slider(Transform p,string name,float y) {
        var r=Node(name,p,170,y,470,46);var s=r.gameObject.AddComponent<Slider>();
        var bg=Pic("Track",r,null,0,15,470,14);bg.color=new Color(.20f,.15f,.12f,.5f);
        var fill=Pic("Fill",r,null,0,15,470,14);fill.color=red;s.fillRect=fill.rectTransform;
        var handle=Pic("Handle",r,Art("PaperButton"),0,0,42,46);handle.raycastTarget=true;s.handleRect=handle.rectTransform;s.targetGraphic=handle;
        s.navigation=new Navigation{mode=Navigation.Mode.None};s.minValue=0;s.maxValue=1;return s;
    }
    static void Create(Sprite background) {
        var root=Node("ShelterHome",null,0,0,1080,1920);
        try {
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();var ui=root.gameObject.AddComponent<ShelterHomeUI>();
            var backdrop=Pic("FullScreenBackdrop",root,null,0,0,1080,1920);backdrop.color=new Color(.045f,.039f,.035f);backdrop.raycastTarget=true;
            backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;backdrop.rectTransform.offsetMin=backdrop.rectTransform.offsetMax=Vector2.zero;
            var core=Node("Core_9x16",root,0,0,1080,1920);root.gameObject.AddComponent<BattleHudSafeArea>().core=core;
            var scene=Pic("ShelterIllustration",core,background,0,170,1080,1440); // Original 3:4 ratio, no stretching.
            var top=Pic("TopStatus",core,Art("Parchment"),18,18,1044,188,true);
            Pic("Portrait",top.transform,Art("Portrait"),24,13,108,135);
            ui.numberText=Txt("SubjectNumber",top.transform,"No. 001",16,145,127,32,23,new Color(.3f,.27f,.23f));
            ui.levelText=Txt("Level",top.transform,"Lv. 1",142,38,192,92,48);
            ui.hpFill=Meter(top.transform,"Health",344,41,310);ui.hpText=Txt("HealthText",top.transform,"10 / 10",354,40,290,42,28,ivory);
            ui.xpFill=Meter(top.transform,"Experience",344,99,310);ui.xpText=Txt("ExperienceText",top.transform,"0%",354,98,290,42,28,ivory);
            Icon(top.transform,3,673,48,44);ui.crystalsText=Txt("Crystals",top.transform,"0",717,46,94,55,28);
            Icon(top.transform,4,815,48,44);ui.cherriesText=Txt("Cherries",top.transform,"0",856,46,83,55,28);
            ui.settingsButton=Btn("Settings",top.transform,"",945,48,76,76);Icon(ui.settingsButton.transform,5,9,9,58);
            var chest=Pic("ChestRail",core,Art("Parchment"),18,205,1044,151,true);
            ui.chestButtons=new Button[4];ui.chestImages=new Image[4];ui.chestLabels=new Text[4];ui.chestReady=new GameObject[4];ui.chestSprites=new[]{icons[0],icons[1],icons[2]};
            for(int i=0;i<4;i++) {
                var b=Btn("ChestSlot_"+i,chest.transform,"",18+i*257,8,245,134);b.targetGraphic.color=new Color(1,1,1,.55f);ui.chestButtons[i]=b;
                ui.chestImages[i]=Pic("Chest",b.transform,icons[Mathf.Min(i,2)],70,0,105,91);ui.chestImages[i].preserveAspect=true;
                ui.chestLabels[i]=Txt("Status",b.transform,"空位",15,92,215,34,26);
                var ready=Pic("ReadyWaxSeal",b.transform,icons[15],175,12,34,34);ui.chestReady[i]=ready.gameObject;
            }
            ui.functionButtons=new Button[3];string[] funcs={"融合","经验","转生"};int[] functionIcons={6,7,8};
            for(int i=0;i<3;i++) {var b=Btn("Function_"+funcs[i],core,"",913,400+i*150,132,132);ui.functionButtons[i]=b;Icon(b.transform,functionIcons[i],32,10,68);Txt("Caption",b.transform,funcs[i],8,87,116,32,25);}
            ui.rebirthLock=Txt("Locked",ui.functionButtons[2].transform,"锁",99,5,28,28,20).gameObject;
            var attr=Pic("Attributes",core,Art("Parchment"),18,1180,1044,284,true);ui.attributePanel=attr.rectTransform;ui.attributePanel.pivot=new Vector2(0,0);ui.attributePanel.anchoredPosition=new Vector2(18,-1464);
            Txt("Title",attr.transform,"属性",20,8,133,51,34);ui.pointsText=Txt("Points",attr.transform,"可用属性点 0",430,8,450,51,30);
            ui.collapseButton=Btn("Fold",attr.transform,"",925,4,87,53);ui.collapseText=Txt("Arrow",ui.collapseButton.transform,"▾",0,0,87,53,32);
            var body=Node("StatRows",attr.transform,14,65,1016,200);ui.attributeBody=body.gameObject.AddComponent<CanvasGroup>();body.gameObject.AddComponent<RectMask2D>();
            ui.statButtons=new Button[6];ui.statValues=new Text[6];string[] names={"生命","攻击","防御","攻速","暴击","暴伤"};
            for(int i=0;i<6;i++) {
                float x=(i%3)*338,y=(i/3)*100;
                Txt("StatLabel_"+i,body,names[i],x+8,y+13,103,70,31);
                ui.statValues[i]=Txt("StatValue_"+i,body,"0",x+115,y+13,130,70,31);
                ui.statButtons[i]=Btn("Add_"+names[i],body,"+",x+257,y+20,64,62);
                if(i%3!=2)Line(body,x+332,y+10,1,79);
                if(i<3)Line(body,x+6,99,321,1);
            }
            ui.battleButton=Btn("ContinueBattle",core,"",70,1490,940,142);ui.battleButton.targetGraphic.color=red;
            Icon(ui.battleButton.transform,14,106,19,92);Txt("Caption",ui.battleButton.transform,"继续战斗",220,22,530,92,56,ivory);Icon(ui.battleButton.transform,15,813,10,107);
            var nav=Pic("Navigation",core,Art("Parchment"),18,1670,1044,220,true);ui.navigationButtons=new Button[5];string[] tabs={"装备","技能","庇护所","地图","商店"};int[] navIcons={9,10,11,12,13};
            for(int i=0;i<5;i++) {
                var b=Btn("Nav_"+tabs[i],nav.transform,"",9+i*205,9,201,198);ui.navigationButtons[i]=b;
                if(i==2)b.targetGraphic.color=red;
                Icon(b.transform,navIcons[i],50,13,102);Txt("Caption",b.transform,tabs[i],8,137,185,44,31,i==2?ivory:ink);
                if(i==1) {var badge=Pic("Badge",b.transform,icons[15],149,3,42,42);ui.skillBadge=Txt("Count",badge.transform,"0",0,0,42,42,22,ivory);}
            }
            // Modal hierarchy exists in the prefab; runtime only switches state/content.
            var modal=Node("DetailsModal",core,0,0,1080,1920);ui.modal=modal.gameObject;ui.modalGroup=modal.gameObject.AddComponent<CanvasGroup>();
            var dim=Pic("InputBlocker",modal,null,0,0,1080,1920);dim.color=new Color(0,0,0,.72f);dim.raycastTarget=true;
            var paper=Pic("DetailsPaper",modal,Art("Parchment"),100,560,880,740,true);
            ui.modalTitle=Txt("Title",paper.transform,"详情",85,42,710,70,44);
            ui.modalBody=Txt("Content",paper.transform,"",75,165,730,300,31);
            ui.modalClose=Btn("Close",paper.transform,"×",767,38,68,66);
            ui.modalAction=Btn("Action",paper.transform,"",190,565,500,92);ui.modalActionText=Txt("Caption",ui.modalAction.transform,"查看",10,5,480,80,34);
            var audio=Node("AudioControls",paper.transform,48,180,784,310);ui.audioControls=audio.gameObject;
            Txt("MusicLabel",audio,"音乐",0,0,135,60,31);Txt("SoundLabel",audio,"音效",0,130,135,60,31);
            ui.musicSlider=Slider(audio,"Music",12);ui.soundSlider=Slider(audio,"Sound",142);
            ui.musicValue=Txt("MusicValue",audio,"100%",653,0,110,60,28);ui.soundValue=Txt("SoundValue",audio,"100%",653,130,110,60,28);
            ui.modal.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Path);
        } finally {UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
}
