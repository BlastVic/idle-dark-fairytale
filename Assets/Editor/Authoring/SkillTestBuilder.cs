using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SkillTestBuilder
{
    public const string Root="Assets/Tests/Skills", ScenePath=Root+"/SkillTest.unity";
    static readonly string Dir="output/skill-test/";
    static Font Font=>AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
    static SkillTestBuilder(){EditorApplication.update+=Tick;}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"command.request"))return;var cmd=File.ReadAllText(Dir+"command.request").Trim();File.Delete(Dir+"command.request");try{if(cmd=="open")Open();else if(cmd=="build")Build();else if(cmd=="standalone")CreateStandalone();else if(cmd=="polish")Polish();else if(cmd=="play"){EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;}else if(cmd=="stop")EditorApplication.isPlaying=false;else if(cmd=="validate")SkillTestValidation.Begin();else if(cmd=="visual")SkillTestValidation.Begin(true);else throw new Exception("Unknown command: "+cmd);File.WriteAllText(Dir+"result.txt","PASS "+cmd);}catch(Exception e){File.WriteAllText(Dir+"result.txt",e.ToString());}}
    [MenuItem("Tools/Dark Fairytale/Skills/Open Skill Test Scene")]
    public static void Open(){if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Tools/Dark Fairytale/Skills/Create Skill Test If Missing")]
    public static void Build(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene first");
        Directory.CreateDirectory(Root);
        var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Skill Test HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=0;
        var test=root.AddComponent<SkillTest>();test.safeRoot=Rect(root.transform,"Safe Area",Vector2.zero,Vector2.one);
        var catalog=Panel(test.safeRoot,"Skill Catalog",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-286),Vector2.zero);
        Text(catalog,"Title","技能测试室 · 3 个无限靶怪",12,4,696,34,25);
        Text(catalog,"Hint","点击选择技能 · 可累计启用 · 测试不保存进度",12,38,696,28,17);
        var definitions=AssetDatabase.LoadAssetAtPath<GameObject>(StorybookSkillsBuilder.Path).GetComponentsInChildren<StorybookSkillNode>(true);
        test.definitions=Definitions(definitions);test.skillButtons=new Button[15];
        for(int i=0;i<15;i++){var node=definitions.Single(n=>(int)n.key==i+1);test.skillButtons[i]=Button(catalog,"Skill_"+node.key,node.displayName.Replace("\n",""),12+(i%3)*234,72+(i/3)*41,228,36);}
        var controls=Panel(test.safeRoot,"Controls",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,260));
        test.heading=Text(controls,"Selected","选择技能",12,5,480,30,23);
        test.levelDown=Button(controls,"Level Down","−",504,5,45,32);test.levelText=Text(controls,"Level","Lv. 1",554,5,96,32,20);test.levelUp=Button(controls,"Level Up","+",660,5,45,32);
        test.detail=Text(controls,"Description","",12,42,696,70,17);
        test.castButton=Button(controls,"Cast","测试 / 释放",12,116,168,40);
        test.loopButton=Button(controls,"Loop","循环：关",190,116,168,40);test.loopText=test.loopButton.GetComponentInChildren<Text>();
        test.attackButton=Button(controls,"Attack","普通攻击",368,116,162,40);test.resetButton=Button(controls,"Reset","重置场地",540,116,168,40);
        test.status=Text(controls,"Status","正在加载…",12,163,696,90,17);
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/SkillTestHUD.prefab");UnityEngine.Object.DestroyImmediate(root);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/SkillTestHUD.prefab"));
        AssetDatabase.SaveAssets();Polish();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);CreateStandalone();
    }
    static SkillTest.Definition[] Definitions(StorybookSkillNode[] nodes)=>nodes.OrderBy(n=>(int)n.key).Select(n=>new SkillTest.Definition{key=n.key,displayName=n.displayName,effectLabels=n.effectLabels}).ToArray();
    [MenuItem("Tools/Dark Fairytale/Skills/Create Standalone Skill Scene")]
    public static void CreateStandalone(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene edits first");
        // Read sound references in the editor only. No Gameplay scene is referenced or loaded by the test at runtime.
        var source=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Gameplay.unity");
        bool openedSource=!source.IsValid()||!source.isLoaded;
        if(openedSource)source=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity",OpenSceneMode.Additive);
        var sourceSound=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IdleKnightHero.UI.SoundManager>(true)).First();
        var clips=sourceSound.clips.ToArray();
        var sourceCamera=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.name=="Main Camera - Battle - Camp - (KEEP OFF)");
        var cameraPosition=sourceCamera.transform.position;var cameraRotation=sourceCamera.transform.rotation;
        var cameraFov=sourceCamera.fieldOfView;var cameraOrthographic=sourceCamera.orthographic;var cameraSize=sourceCamera.orthographicSize;
        if(openedSource)EditorSceneManager.CloseScene(source,true);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var services=new GameObject("Combat Services — no campaign or save");
        var gm=services.AddComponent<GameManager>();gm.standaloneCombat=true;gm.gameState=Assets.Scripts.Game.Enum.GameStateType.BATTLE;
        gm.basePlayerStat=new Stat{hpMax=1000,hpNow=600,dmg=100,atkSpd=100,critDmg=150};
        services.AddComponent<InventoryManager>();services.AddComponent<SkillsController>();
        services.AddComponent<Assets.Scripts.Services.Vibrations.VibrationsManager>();
        var assets=services.AddComponent<AssetManager>();
        var level=services.AddComponent<LevelController>();var so=new SerializedObject(level);so.FindProperty("_assetManager").objectReferenceValue=assets;so.ApplyModifiedPropertiesWithoutUndo();
        var sound=services.AddComponent<IdleKnightHero.UI.SoundManager>();sound.clips=clips;sound.soundLevel=.7f;sound.audioSource=services.AddComponent<AudioSource>();sound.audioSource_Music=services.AddComponent<AudioSource>();sound.audioSource.playOnAwake=sound.audioSource_Music.playOnAwake=false;
        assets.popUpText=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/CombatFloat/StorybookDamage.prefab");assets.notifierText=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/CombatFloat/StorybookDoubleExperience.prefab");
        var style=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");
        assets.battleStyle=style;
        var stage=new GameObject("Skill Test Stage").AddComponent<SkillTestStage>();stage.assets=assets;
        var cam=new GameObject("Battle Camera",typeof(Camera),typeof(AudioListener),typeof(CameraEffects));cam.tag="MainCamera";cam.transform.position=cameraPosition;cam.transform.rotation=cameraRotation;stage.battleCamera=cam.GetComponent<Camera>();stage.battleCamera.orthographic=cameraOrthographic;stage.battleCamera.fieldOfView=cameraFov;stage.battleCamera.orthographicSize=cameraSize;stage.battleCamera.clearFlags=CameraClearFlags.SolidColor;stage.battleCamera.backgroundColor=new Color(.035f,.045f,.055f);assets.battleCampCamera=cam;
        var bg=new GameObject("Preview Backdrop",typeof(SpriteRenderer),typeof(DarkFairytaleBackdrop));bg.GetComponent<SpriteRenderer>().sprite=style.background;bg.GetComponent<SpriteRenderer>().sortingLayerName="BG";bg.GetComponent<SpriteRenderer>().sortingOrder=-100;stage.backdrop=bg.GetComponent<DarkFairytaleBackdrop>();stage.backdrop.Initialize(stage.battleCamera);
        assets.playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Characters/RedHood/Prefabs/Player.prefab");
        stage.player=((GameObject)PrefabUtility.InstantiatePrefab(assets.playerPrefab,scene)).GetComponent<Player>();assets.playerObj=stage.player.gameObject;stage.player.ab=stage.player.GetComponent<Actor_Base>();stage.player.isCampPlayer=false;stage.player.skeletonAnimation.Initialize(true);
        string[] keys={"MobTallowkin","MobBramblet","MobMurmlet"};stage.targets=new Enemy[3];
        for(int i=0;i<keys.Length;i++){
            var enemy=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Monsters/"+keys[i]+"/"+keys[i]+".prefab"),scene)).GetComponent<Enemy>();
            stage.targets[i]=enemy;enemy.ab=enemy.GetComponent<Actor_Base>();enemy.skeletonAnimation=enemy.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();enemy.skeletonAnimation.Initialize(true);
            var drop=enemy.GetComponent<DropIn>();if(drop)UnityEngine.Object.DestroyImmediate(drop);
            enemy.ab.enemyLifebar=enemy.GetComponentInChildren<EnemyLifebar>(true);enemy.ab.enemyLifebar.gameObject.SetActive(false);
        }
        stage.Place();stage.player.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
        foreach(var enemy in stage.targets)enemy.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
        var hudPrefab=PrefabUtility.LoadPrefabContents(Root+"/SkillTestHUD.prefab");
        try{hudPrefab.GetComponent<SkillTest>().definitions=Definitions(AssetDatabase.LoadAssetAtPath<GameObject>(StorybookSkillsBuilder.Path).GetComponentsInChildren<StorybookSkillNode>(true));PrefabUtility.SaveAsPrefabAsset(hudPrefab,Root+"/SkillTestHUD.prefab");}finally{PrefabUtility.UnloadPrefabContents(hudPrefab);}
        var hud=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/SkillTestHUD.prefab"),scene)).GetComponent<SkillTest>();hud.stage=stage;PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    // Explicit migration to the existing Monster/Red Hood preview HUD palette and controls.
    public static void Polish(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var root=PrefabUtility.LoadPrefabContents(Root+"/SkillTestHUD.prefab");
        try {
            var test=root.GetComponent<SkillTest>();
            var catalog=(RectTransform)test.skillButtons[0].transform.parent;
            if(!test.catalogButton){
                var header=Panel(test.safeRoot,"Header",new Vector2(0,1),new Vector2(1,1),new Vector2(16,-102),new Vector2(-16,-16));
                var title=catalog.Find("Title").GetComponent<Text>();title.transform.SetParent(header,false);Place(title.rectTransform,20,8,648,36);title.text="技能测试室";title.fontSize=28;
                var hint=catalog.Find("Hint").GetComponent<Text>();hint.transform.SetParent(header,false);Place(hint.rectTransform,20,48,648,26);hint.text="3 个无限靶怪 · 不写入存档";hint.fontSize=17;
                test.catalogButton=Button(test.safeRoot,"SkillSelector","",16,112,688,60);test.catalogLabel=test.catalogButton.GetComponentInChildren<Text>();test.catalogLabel.alignment=TextAnchor.MiddleLeft;Place(test.catalogLabel.rectTransform,20,6,648,48);test.catalogLabel.fontSize=23;
                var controls=(RectTransform)test.castButton.transform.parent;
                controls.offsetMin=new Vector2(16,16);controls.offsetMax=new Vector2(-16,276);
                foreach(RectTransform child in controls){if(child.name=="Border")continue;child.anchoredPosition=new Vector2(child.anchoredPosition.x*688/720,child.anchoredPosition.y);child.sizeDelta=new Vector2(child.sizeDelta.x*688/720,child.sizeDelta.y);}
                test.catalogGroup=catalog.gameObject.AddComponent<CanvasGroup>();
            }
            catalog.anchorMin=new Vector2(0,1);catalog.anchorMax=new Vector2(1,1);catalog.offsetMin=new Vector2(16,-482);catalog.offsetMax=new Vector2(-16,-182);catalog.SetAsLastSibling();
            test.catalogGroup.alpha=0;test.catalogGroup.blocksRaycasts=false;test.catalogGroup.interactable=false;catalog.gameObject.SetActive(false);
            for(int i=0;i<test.skillButtons.Length;i++){Place((RectTransform)test.skillButtons[i].transform,18+i%3*217,14+i/3*56,207,48);Place(test.skillButtons[i].GetComponentInChildren<Text>().rectTransform,6,0,195,48);}
            foreach(var image in root.GetComponentsInChildren<Image>(true)){
                if(image.name=="Border"){UnityEngine.Object.DestroyImmediate(image.gameObject);continue;}
                image.sprite=null;image.type=Image.Type.Simple;image.color=image.GetComponent<Button>()?new Color(.10f,.12f,.14f):new Color(.065f,.08f,.09f,.97f);
                var outline=image.GetComponent<Outline>()??image.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.47f,.40f,.29f);outline.effectDistance=new Vector2(1,-1);
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true)){
                text.verticalOverflow=VerticalWrapMode.Overflow;text.fontStyle=FontStyle.Normal;text.color=new Color(.89f,.84f,.73f);
                if(text.name=="Hint")text.color=new Color(.62f,.59f,.53f);
            }
            foreach(var button in root.GetComponentsInChildren<Button>(true)){
                var old=button.GetComponent<StorybookEquipmentButtonState>();if(old)UnityEngine.Object.DestroyImmediate(old);
                if(!button.GetComponent<RedHoodPrototype.RedHoodPreviewButtonState>())button.gameObject.AddComponent<RedHoodPrototype.RedHoodPreviewButtonState>();
                var colors=button.colors;colors.highlightedColor=new Color(1.3f,1.2f,1.1f);colors.pressedColor=new Color(.7f,.65f,.6f);colors.selectedColor=colors.normalColor=Color.white;colors.disabledColor=Color.white;button.colors=colors;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/SkillTestHUD.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static RectTransform Box(Transform parent,string name,float x,float y,float w,float h){var r=Rect(parent,name,new Vector2(0,1),new Vector2(0,1));r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static void PreviewPanel(RectTransform r){var im=r.gameObject.AddComponent<Image>();im.color=new Color(.065f,.08f,.09f,.97f);var line=r.gameObject.AddComponent<Outline>();line.effectColor=new Color(.47f,.40f,.29f);line.effectDistance=new Vector2(1,-1);}
    static RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){var r=Rect(parent,name,min,max);r.offsetMin=lo;r.offsetMax=hi;PreviewPanel(r);return r;}
    static Text Text(Transform parent,string name,string content,float x,float y,float w,float h,int size){var t=Box(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=Font;t.text=content;t.fontSize=size;t.color=new Color(.16f,.11f,.09f);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
    static Button Button(Transform parent,string name,string label,float x,float y,float w,float h){var r=Box(parent,name,x,y,w,h);PreviewPanel(r);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.navigation=new Navigation{mode=Navigation.Mode.None};r.gameObject.AddComponent<ShelterButtonFeedback>();var state=r.gameObject.AddComponent<StorybookEquipmentButtonState>();state.grayscaleShader=Shader.Find("UI/Storybook Disabled");var t=Text(r,"Label",label,4,0,w-8,h,19);t.alignment=TextAnchor.MiddleCenter;return b;}
}
