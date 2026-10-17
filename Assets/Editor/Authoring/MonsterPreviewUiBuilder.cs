using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit authoring commands. Never rebuilds existing layouts on import or Play.</summary>
public static class MonsterPreviewUiBuilder
{
    public const string Root = "Assets/Tests/MonsterAnimation/UI";
    public const string HudPath = "Assets/Tests/MonsterAnimation/UI/Prefabs/MonsterPreviewHUD.prefab";
    static Font font;
    static readonly Color Ink = new Color(.065f,.08f,.09f,.97f);
    static readonly Color Line = new Color(.47f,.40f,.29f,1);
    static readonly Color Paper = new Color(.89f,.84f,.73f,1);
    static readonly Color Red = new Color(.43f,.10f,.11f,1);
    static Font Font => font ? font : (font = AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf"));

    [MenuItem("Tools/Dark Fairytale/Monsters/Install Preview UGUI (Once)")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MonsterTestSceneBuilder.ScenePath) throw new InvalidOperationException("Open MonsterAnimationTest first.");
        var tester = Object.FindFirstObjectByType<MonsterAnimationTest>();
        tester.RefreshCatalog();
        if (!File.Exists(HudPath)) BuildPrefab(tester);
        if (!tester.hud)
            tester.hud = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath),scene)).GetComponent<MonsterPreviewHud>();
        if (!tester.stage)
        {
            var stage = new GameObject("Preview Stage").AddComponent<MonsterPreviewStage>();
            tester.stage = stage; stage.tester = tester; stage.hud = tester.hud; stage.previewCamera = tester.previewCamera;
            var style = AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");
            var bg = new GameObject("Black Forest Backdrop",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            bg.transform.SetParent(stage.transform); bg.sprite = style.background; bg.sortingOrder = -100;
            stage.backdrop = bg;
            bg.gameObject.AddComponent<BlackForestAtmosphere>().Initialize(style);
            // Persist all ambience nodes in the scene, without rebuilding them at runtime.
            foreach (var r in bg.GetComponentsInChildren<Renderer>()) { r.sortingLayerName = "Default"; r.sortingOrder = -90; }
            bg.sortingOrder = -100;
            var hood = SkeletonAnimation.NewSkeletonAnimationGameObject(AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle_SkeletonData.asset"));
            hood.name = "Red Hood - Idle Reference"; hood.transform.SetParent(stage.transform);
            hood.AnimationName = "Idle1"; hood.loop = true; hood.AnimationState.SetAnimation(0,"Idle1",true);
            hood.Update(0); hood.LateUpdate(); hood.GetComponent<Renderer>().sortingOrder = 10; stage.redHood = hood;
            stage.battlePlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Characters/RedHood/Prefabs/Player.prefab");
            stage.battleStyle = style;
            // Default preview is the first independent monster, while preserving all catalog references.
            if (tester.character) Object.DestroyImmediate(tester.character.gameObject);
            var first = tester.monsters.FirstOrDefault(d=>d.name.StartsWith("MobTallowkin")) ?? tester.monsters.First();
            tester.character = SkeletonAnimation.NewSkeletonAnimationGameObject(first);
            tester.character.name = "Monster Preview";
            tester.character.transform.SetParent(tester.transform);
            var prefab = tester.monsterPrefabs.FirstOrDefault(p=>p && p.GetComponentInChildren<SkeletonAnimation>(true).skeletonDataAsset==first);
            if(prefab) tester.character.transform.localScale = prefab.GetComponentInChildren<SkeletonAnimation>(true).transform.localScale;
            tester.character.AnimationName = "Idle1"; tester.character.loop = true;
            tester.character.AnimationState.SetAnimation(0,"Idle1",true); tester.character.Update(0); tester.character.LateUpdate();
            tester.character.GetComponent<Renderer>().sortingOrder=10;
            tester.previewCamera.orthographic=true; tester.previewCamera.orthographicSize=6.4f;
            tester.previewCamera.transform.position=new Vector3(0,0,-20);
            tester.previewCamera.backgroundColor=new Color(.035f,.045f,.055f);
        }
        if (!Object.FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        Canvas.ForceUpdateCanvases(); tester.stage.InitializePreview(); tester.stage.Apply();
        EditorUtility.SetDirty(tester); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets(); Debug.Log("Monster preview UGUI installed. Existing prefabs are never overwritten.");
    }

    public static void RefreshHudCatalog(MonsterAnimationTest tester)
    {
        if (!File.Exists(HudPath)) return;
        var root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var hud = root.GetComponent<MonsterPreviewHud>();
            var cards = hud.cards.Where(c => c && c.data).ToList();
            foreach (var data in tester.monsters)
            {
                var card = cards.FirstOrDefault(c => c.data == data);
                if (card) continue; // Preserve existing names, portraits and hand-edited layout.
                card = ((GameObject)PrefabUtility.InstantiatePrefab(hud.cardTemplate.gameObject, hud.cardContent)).GetComponent<MonsterPreviewCard>();
                card.data = data; card.displayName = DisplayName(data.name); card.title.text = card.displayName;
                card.portrait.sprite = MakePortrait(data, tester); cards.Add(card);
            }
            hud.cards = cards.ToArray();
            ProductionUiArt.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void BuildPrefab(MonsterAnimationTest tester)
    {
        Directory.CreateDirectory(Root+"/Portraits"); AssetDatabase.Refresh();
        var go = new GameObject("MonsterPreviewHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        try
        {
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=50;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(720,1280); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var hud=go.AddComponent<MonsterPreviewHud>();
            var core=Rect(go.transform,"Core_9x16",0,0,720,1280);
            var safe=go.AddComponent<BattleHudSafeArea>(); safe.core=core; safe.referenceSize=new Vector2(720,1280); safe.Apply();
            hud.playerSlot=Rect(core,"RedHoodPreviewArea",36,685,310,300);
            hud.monsterSlot=Rect(core,"MonsterPreviewArea",388,680,294,305);
            var top=Panel(core,"Header",16,16,688,94);
            Label(top,"Title","怪物动画测试",20,10,270,37,28);
            Label(top,"Subtitle","MonsterAnimationTest",20,51,265,27,17,new Color(.62f,.59f,.53f));
            hud.redHoodToggle=Toggle(top,"RedHoodVisible","小红帽",304,20,174,true);
            Label(top,"IdleState","Idle",335,61,90,22,15,new Color(.65f,.64f,.56f));
            hud.resetButton=Button(top,"ResetView","重置视角",522,23,147,49);
            hud.catalogButton=Button(core,"CatalogSelector","",16,120,688,64);
            hud.selectedPortrait=Image(hud.catalogButton.transform,"SelectedPortrait",8,3,60,58,Color.white); hud.selectedPortrait.preserveAspect=true;
            hud.selectedTitle=Label(hud.catalogButton.transform,"SelectedName","红帽菇",84,9,415,44,24);
            hud.catalogArrow=Label(hud.catalogButton.transform,"ExpandLabel","怪物库  +",525,13,142,38,21);
            var dropdown=Panel(core,"MonsterCatalog",16,194,688,390); hud.catalogPanel=dropdown.gameObject;
            var inputBg=Image(dropdown,"Search",18,16,652,52,new Color(.045f,.055f,.065f,1)); Outline(inputBg);
            hud.searchField=inputBg.gameObject.AddComponent<InputField>();
            var inputText=Label(inputBg.transform,"Value","",16,4,622,44,22);
            var placeholder=Label(inputBg.transform,"Placeholder","搜索名称 / 编号",16,4,622,44,22,new Color(.52f,.53f,.52f));
            hud.searchField.textComponent=inputText; hud.searchField.placeholder=placeholder; hud.searchField.targetGraphic=inputBg;
            hud.catalogCount=Label(dropdown,"Count","",20,74,630,25,16,new Color(.61f,.61f,.55f));
            hud.catalogScroll=Scroll(dropdown,"CatalogScroll",18,104,652,268,out var content);
            hud.cardContent=content;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize=new Vector2(311,117); grid.spacing=new Vector2(12,12); grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=2;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var template=CreateCard(null,"",null);
            hud.cardTemplate=ProductionUiArt.SaveAsPrefabAsset(template.gameObject,"Assets/Tests/MonsterAnimation/UI/Prefabs/MonsterCard.prefab").GetComponent<MonsterPreviewCard>(); Object.DestroyImmediate(template.gameObject);
            var cards=new List<MonsterPreviewCard>();
            foreach(var data in tester.monsters)
            {
                var card=(GameObject)PrefabUtility.InstantiatePrefab(hud.cardTemplate.gameObject,content);
                var c=card.GetComponent<MonsterPreviewCard>(); c.data=data; c.displayName=DisplayName(data.name); c.title.text=c.displayName;
                c.portrait.sprite=MakePortrait(data,tester); cards.Add(c);
            }
            hud.cards=cards.ToArray();
            hud.emptyText=Label(dropdown,"NoResults","没有匹配的怪物",30,145,615,80,24); hud.emptyText.alignment=TextAnchor.MiddleCenter; hud.emptyText.gameObject.SetActive(false);
            if(hud.cards.Length>0) hud.selectedPortrait.sprite=hud.cards[0].portrait.sprite;
            var dock=Panel(core,"PlaybackDock",16,1002,688,262);
            hud.statusText=Label(dock,"AnimationStatus","动画 / Idle1",20,7,650,28,20);
            hud.animationScroll=Scroll(dock,"AnimationScroll",20,41,648,88,out var animations);
            hud.animationContent=animations;
            var ag=animations.gameObject.AddComponent<GridLayoutGroup>(); ag.cellSize=new Vector2(150,40); ag.spacing=new Vector2(10,7); ag.constraint=GridLayoutGroup.Constraint.FixedColumnCount; ag.constraintCount=4;
            animations.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var ab=Button(null,"AnimationButton","Idle1",0,0,150,40); ab.GetComponentInChildren<Text>().fontSize=18;
            hud.animationTemplate=ProductionUiArt.SaveAsPrefabAsset(ab.gameObject,"Assets/Tests/MonsterAnimation/UI/Prefabs/AnimationButton.prefab").GetComponent<Button>(); Object.DestroyImmediate(ab.gameObject);
            var buttons=new List<Button>();
            // Persist enough controls for every current animation; future entries reuse the template.
            int count=Mathf.Max(4,tester.monsters.Max(d=>d.GetSkeletonData(false).Animations.Count));
            for(int i=0;i<count;i++)
            {
                var b=((GameObject)PrefabUtility.InstantiatePrefab(hud.animationTemplate.gameObject,animations)).GetComponent<Button>();
                b.GetComponentInChildren<Text>().text=new[]{"Idle1","Attack1","Hit","Death1"}[i%4]; buttons.Add(b);
                if(i>=4)b.gameObject.SetActive(false);
            }
            hud.animationButtons=buttons.ToArray();
            hud.pauseButton=Button(dock,"Pause","暂停",20,137,146,43); hud.pauseText=hud.pauseButton.GetComponentInChildren<Text>();
            hud.replayButton=Button(dock,"Replay","重播",178,137,146,43);
            hud.loopToggle=Toggle(dock,"Loop","循环",353,137,140,true);
            hud.cycleToggle=Toggle(dock,"AutoCycle","轮播",514,137,150,false);
            Label(dock,"SpeedLabel","速度",20,188,75,30,21);
            hud.speedSlider=Slider(dock,"Speed",100,194,435,.1f,2,1);
            hud.speedText=Label(dock,"SpeedValue","1.00×",550,187,115,31,21); hud.speedText.alignment=TextAnchor.MiddleRight;
            Label(dock,"ZoomLabel","缩放",20,224,75,30,21);
            hud.zoomSlider=Slider(dock,"Zoom",100,230,435,.3f,2,1);
            hud.zoomText=Label(dock,"ZoomValue","1.00×",550,223,115,31,21); hud.zoomText.alignment=TextAnchor.MiddleRight;
            dropdown.gameObject.SetActive(false);
            ProductionUiArt.SaveAsPrefabAsset(go,HudPath);
        }
        finally { Object.DestroyImmediate(go); }
    }
    public static string DisplayName(string name) => CreatureNames.DisplayName(name);
    static MonsterPreviewCard CreateCard(Transform parent,string name,Sprite sprite)
    {
        var b=Button(parent,"MonsterCard","",0,0,311,117);
        var c=b.gameObject.AddComponent<MonsterPreviewCard>(); c.button=b; c.selection=b.GetComponent<Image>();
        c.portrait=Image(b.transform,"Portrait",6,5,106,107,Color.white); c.portrait.preserveAspect=true; c.portrait.sprite=sprite;
        c.title=Label(b.transform,"Name",name,121,9,178,99,20);
        return c;
    }
    public static Sprite MakePortrait(SkeletonDataAsset data,MonsterAnimationTest tester, bool overwrite=false)
    {
        string path=Root+"/Portraits/"+data.name+".png";
        if(overwrite || !File.Exists(path))
        {
            var scene=EditorSceneManager.NewPreviewScene(); RenderTexture rt=null; Texture2D tex=null;
            var previous=RenderTexture.active;
            try
            {
                var cam=new GameObject("Portrait camera").AddComponent<Camera>(); SceneManager.MoveGameObjectToScene(cam.gameObject,scene);
                cam.scene=scene; cam.orthographic=true; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.065f,.08f,.09f,1); cam.nearClipPlane=.01f; cam.farClipPlane=100;
                var sk=SkeletonAnimation.NewSkeletonAnimationGameObject(data); SceneManager.MoveGameObjectToScene(sk.gameObject,scene);
                var source=tester.monsterPrefabs.FirstOrDefault(p=>p && p.GetComponentInChildren<SkeletonAnimation>(true).skeletonDataAsset==data);
                if(source)sk.transform.localScale=source.GetComponentInChildren<SkeletonAnimation>(true).transform.localScale;
                var anim=data.GetSkeletonData(false).Animations.FirstOrDefault(a=>a.Name=="Idle1") ?? data.GetSkeletonData(false).Animations.FirstOrDefault(a=>a.Name.ToLower().Contains("idle"));
                if(anim!=null)sk.AnimationState.SetAnimation(0,anim.Name,true);
                sk.Update(.1f);sk.LateUpdate(); var bounds=sk.GetComponent<Renderer>().bounds;
                cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.55f; cam.transform.position=new Vector3(bounds.center.x,bounds.center.y,-20);
                rt=new RenderTexture(256,256,24); cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
                tex=new Texture2D(256,256,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;if(rt)Object.DestroyImmediate(rt);if(tex)Object.DestroyImmediate(tex);EditorSceneManager.ClosePreviewScene(scene);}
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Image Image(Transform p,string n,float x,float y,float w,float h,Color c)
    {var r=Rect(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
    static void Outline(Graphic g) { var o=g.gameObject.AddComponent<Outline>();o.effectColor=Line;o.effectDistance=new Vector2(1,-1); }
    static RectTransform Panel(Transform p,string n,float x,float y,float w,float h)
    {var im=Image(p,n,x,y,w,h,Ink);im.raycastTarget=true;Outline(im);return im.rectTransform;}
    static Text Label(Transform p,string n,string value,float x,float y,float w,float h,int size,Color? color=null)
    {var r=Rect(p,n,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=Font;t.fontSize=size;t.color=color??Paper;t.text=value;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Button Button(Transform p,string n,string text,float x,float y,float w,float h)
    {var im=Image(p,n,x,y,w,h,new Color(.10f,.12f,.14f,1));im.raycastTarget=true;Outline(im);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.highlightedColor=new Color(1.3f,1.2f,1.1f);colors.pressedColor=new Color(.7f,.65f,.6f);colors.selectedColor=Color.white;b.colors=colors;var t=Label(im.transform,"Label",text,6,0,w-12,h,23);t.alignment=TextAnchor.MiddleCenter;return b;}
    static Toggle Toggle(Transform p,string n,string label,float x,float y,float w,bool on)
    {var im=Image(p,n,x,y,w,44,Color.clear);im.raycastTarget=true;var t=im.gameObject.AddComponent<Toggle>();var box=Image(im.transform,"Box",0,8,28,28,new Color(.08f,.09f,.10f));Outline(box);var mark=Image(box.transform,"Check",5,5,18,18,Red);t.targetGraphic=box;t.graphic=mark;t.isOn=on;Label(im.transform,"Label",label,39,0,w-39,44,22);return t;}
    static Slider Slider(Transform p,string n,float x,float y,float w,float min,float max,float value)
    {
        var root=Rect(p,n,x,y-9,w,34);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        var bg=Image(root,"Track",0,11,w,12,new Color(.035f,.04f,.045f));Outline(bg);
        var fillArea=Rect(root,"FillArea",0,11,w,12);var fill=Image(fillArea,"Fill",0,0,w,12,Red);
        var handles=Rect(root,"HandleArea",8,0,w-16,34);var knob=Image(handles,"Handle",0,4,18,26,Paper);Outline(knob);knob.rectTransform.pivot=new Vector2(.5f,.5f);
        var s=root.gameObject.AddComponent<Slider>();s.fillRect=fill.rectTransform;s.handleRect=knob.rectTransform;s.targetGraphic=knob;fill.rectTransform.sizeDelta=Vector2.zero;fill.rectTransform.anchoredPosition=Vector2.zero;knob.rectTransform.sizeDelta=new Vector2(18,0);knob.rectTransform.anchoredPosition=Vector2.zero;s.minValue=min;s.maxValue=max;s.value=value;return s;
    }
    static ScrollRect Scroll(Transform p,string n,float x,float y,float w,float h,out RectTransform content)
    {
        var root=Rect(p,n,x,y,w,h);var s=root.gameObject.AddComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;s.scrollSensitivity=28;
        var viewport=Rect(root,"Viewport",0,0,w-10,h);viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        content=Rect(viewport,"Content",0,0,w-10,h);s.viewport=viewport;s.content=content;
        var track=Image(root,"Scrollbar",w-5,0,5,h,new Color(.13f,.14f,.15f));var bar=track.gameObject.AddComponent<Scrollbar>();
        var handle=Image(track.transform,"Handle",0,0,5,h,Red);bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;handle.rectTransform.sizeDelta=Vector2.zero;handle.rectTransform.anchoredPosition=Vector2.zero;s.verticalScrollbar=bar;
        return s;
    }
}
