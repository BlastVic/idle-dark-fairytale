using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookBattleResultBuilder
{
    public const string Root="Assets/DarkFairytale/UI/BattleResult";
    static Font font;
    static StorybookBattleResultBuilder(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&File.Exists("output/battle-result/build.request")){File.Delete("output/battle-result/build.request");Build();}};}
    static RectTransform Node(Transform p,string n,float x,float y,float w,float h){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h){var r=Node(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.raycastTarget=false;return im;}
    static Text Text(Transform p,string n,string value,float x,float y,float w,float h,int size,Color? color=null){var r=Node(p,n,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.verticalOverflow=VerticalWrapMode.Overflow;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.text=value;t.color=color??new Color(.06f,.045f,.03f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Sprite Load(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/"+n+".asset");
    static Button Button(Transform p,string n,string title,float x,float y,float w,float h,bool red=false,int size=26){
        var im=Pic(p,n,AssetDatabase.LoadAssetAtPath<Sprite>(Root+(red?"/CrimsonButton.asset":"/IvoryButton.asset")),x,y,w,h);im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=4;im.raycastTarget=true;
        var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.7f,.65f,.6f);c.disabledColor=c.normalColor;b.colors=c;
        b.gameObject.AddComponent<ShelterButtonFeedback>();b.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
        // Frame sprites contain their original textured centre.
        Text(b.transform,"Caption",title,6,0,w-12,h,size,red?new Color(.98f,.94f,.82f):(Color?)null);return b;
    }
    static Sprite Slice(Texture2D t,string n,Rect rect){string path=Root+"/"+n+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!s){s=Sprite.Create(t,rect,new Vector2(.5f,.5f),100);AssetDatabase.CreateAsset(s,path);}return s;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Battle Results (Explicit)")]
    public static void Build(){
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
        var bi=(TextureImporter)AssetImporter.GetAtPath(Root+"/ResultButtons.png");bi.textureType=TextureImporterType.Sprite;bi.spriteImportMode=SpriteImportMode.Single;bi.alphaIsTransparency=true;bi.maxTextureSize=2048;bi.textureCompression=TextureImporterCompression.Uncompressed;bi.mipmapEnabled=false;bi.SaveAndReimport();
        var bt=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ResultButtons.png");
        for(int k=0;k<2;k++){string path=Root+(k==0?"/CrimsonButton.asset":"/IvoryButton.asset");if(!AssetDatabase.LoadAssetAtPath<Sprite>(path)){var s=Sprite.Create(bt,new Rect(46,k==0?bt.height-387:bt.height-737,1682,234),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(96,35,96,35));AssetDatabase.CreateAsset(s,path);}}

        for(int side=0;side<2;side++){
            var go=new GameObject(side==0?"StorybookVictory":"StorybookDefeat",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            try{
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=400;
                var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(665,1182);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var backdrop=Pic(go.transform,"BlackOutsideSupportedArea",null,0,0,665,1182);backdrop.color=Color.black;backdrop.raycastTarget=true;backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;backdrop.rectTransform.offsetMin=backdrop.rectTransform.offsetMax=Vector2.zero;
                var supported=Node(go.transform,"SupportedPortrait",0,0,665,1182);supported.gameObject.AddComponent<RectMask2D>();
                var forest=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DarkFairytale/MoonlitCastleBattle.png");
                var bg=Pic(supported,"ForestExtension",Slice(forest,"ForestBackdrop",new Rect(0,0,forest.width,forest.height)),0,0,forest.width,forest.height);
                var backdropLayout=go.AddComponent<StorybookResultBackdrop>();backdropLayout.supported=supported;backdropLayout.background=bg.rectTransform;backdropLayout.artworkSize=new Vector2(forest.width,forest.height);
                var core=Node(go.transform,"Core_9x16",0,0,665,1182);var fit=go.AddComponent<BattleHudSafeArea>();fit.core=core;fit.referenceSize=new Vector2(665,1182);
                var ui=go.AddComponent<StorybookBattleResultUI>();ui.victory=side==0;ui.presentation=core.gameObject.AddComponent<CanvasGroup>();
                Pic(core,"ApprovedArtwork_NoText",null,0,0,665,1182);
                Text(core,"Chapter","第一章 · 黑森林",150,302,390,40,23);
                Text(core,"Heading",side==0?"战斗胜利":"战斗失败",140,339,410,86,58,side==1?new Color(.38f,.045f,.04f):(Color?)null);
                ui.location=Text(core,"Location","1-2 蘑菇森林",150,422,390,37,26);
                ui.rewardTitle=Text(core,"RewardTitle",side==0?"战利品宝箱":"火光暂熄，故事未完",137,768,410,44,30);
                ui.rewardHint=Text(core,"RewardHint",side==0?"立即开启 · 有机会获得 2 倍或 3 倍奖励":"强化装备与属性后，再次挑战",138,815,410,32,18);
                ui.rewardOptions=Node(core,"ImmediateOpenOptions",0,0,665,1182).gameObject;
                ui.advert=Button(ui.rewardOptions.transform,"Advert","观看广告开启",164,848,178,44,false,18);
                ui.currency=Button(ui.rewardOptions.transform,"Currency","消耗货币开启",350,848,184,44,false,17);ui.currencyCaption=ui.currency.GetComponentInChildren<Text>();
                ui.standardActions=Node(core,"StandardActions",0,0,665,1182).gameObject;
                ui.replay=Button(ui.standardActions.transform,"Replay","再次挑战",159,923,376,64,true,30);ui.home=Button(ui.standardActions.transform,"Home","返回庇护所",159,996,376,60,false,28);
                ui.nextActions=Node(core,"NextActions",0,0,665,1182).gameObject;
                ui.next=Button(ui.nextActions.transform,"NextNode","挑战下一节点",159,923,376,64,true,28);
                ui.replaySmall=Button(ui.nextActions.transform,"Replay","再次挑战",159,996,180,60,false,23);ui.homeSmall=Button(ui.nextActions.transform,"Home","返回庇护所",349,996,186,60,false,23);ui.nextActions.SetActive(false);ui.rewardOptions.SetActive(side==0);
                ui.chapterMap=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/Map/Chapter01Map.prefab").GetComponent<StorybookMapUI>();
                StorybookResultChestAuthoring.Apply(go);
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+go.name+".prefab");
            }finally{Object.DestroyImmediate(go);}
        }
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/MainScenes/Gameplay.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
        var gc=Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);
        for(int side=0;side<2;side++){
            var old=side==0?gc.victoryPanel:gc.defeatPanel;
            if(old&&old.GetComponent<StorybookBattleResultUI>()){Object.DestroyImmediate(old);}
            else if(old){old.SetActive(false);old.name=side==0?"LegacyVictoryBindings":"LegacyDefeatBindings";}
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+(side==0?"StorybookVictory":"StorybookDefeat")+".prefab"),scene);instance.SetActive(false);
            var ui=instance.GetComponent<StorybookBattleResultUI>();if(side==0){gc.victoryPanel=instance;gc.victoryMapName=ui.location;}else{gc.defeatPanel=instance;gc.defeatMapName=ui.location;}
        }
        EditorUtility.SetDirty(gc);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("output/battle-result/build.txt","Built and installed editable victory/defeat prefabs.");
    }
}
