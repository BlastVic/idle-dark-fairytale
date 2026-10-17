using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class SpineEffectsTestBuilder
{
    public const string Root="Assets/Tests/SpineEffects";
    public const string ScenePath=Root+"/SpineEffectsTest.unity";
    const string Queue="output/spine-effects-test/command.request";
    static Font Font=>AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
    static SpineEffectsTestBuilder(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Queue))return;
        string command=File.ReadAllText(Queue).Trim();File.Delete(Queue);
        try {
            if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="inspect"){
                var t=UnityEngine.Object.FindFirstObjectByType<SpineEffectsTest>();
                var e=EventSystem.current;var hits=new System.Collections.Generic.List<RaycastResult>();
                e.RaycastAll(new PointerEventData(e){position=RectTransformUtility.WorldToScreenPoint(null,t.selectionButtons[1].transform.position+new Vector3(100,-20,0))},hits);
                File.WriteAllText("output/spine-effects-test/inspect.txt","selected="+t.Selected+" module="+e.currentInputModule+" input="+Input.mousePosition+" hits="+string.Join(",",hits.ConvertAll(h=>h.gameObject.name)));
            }
            else if(command=="res-motion")ResurrectionMotionAuthoring.Apply();
            else if(command=="size")CommonEffectsSizing.Apply();
            else if(command=="hit")CommonHitAuthoring.Build();
            else if(command=="cast")CommonCastAuthoring.Build();
            else if(command=="sweep")CommonSweepAuthoring.Build();
            else if(command=="lightning")CommonLightningAuthoring.Build();
            else if(command=="heal")CommonHealAuthoring.Build();
            else if(command=="stage")SpineEffectsStageAuthoring.Install();
            else if(command=="build")Build();
            else if(command=="play"){EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;}
            else if(command=="validate")SpineEffectsTestValidation.Begin();
            else throw new Exception("Unknown command "+command);
            File.WriteAllText("output/spine-effects-test/result.txt","PASS "+command);
        } catch(Exception e){File.WriteAllText("output/spine-effects-test/result.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Dark Fairytale/Effects/Open Test Scene")]
    public static void Open(){if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Tools/Dark Fairytale/Effects/Create Test Scene If Missing")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Current scene has unsaved edits; save before creating test scene.");
        Directory.CreateDirectory(Root);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Spine Effects Test");var model=root.AddComponent<SpineEffectsTest>();model.petBattleEffects=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Companions/PetCindrel/PetCindrel.prefab").GetComponent<PetCindrelEffects>();
        var cameraGo=new GameObject("Preview Camera");cameraGo.transform.SetParent(root.transform);cameraGo.transform.position=new Vector3(0,0,-20);
        var cam=cameraGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=9;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.068f,.085f);model.previewCamera=cam;
        var bg=new GameObject("Battle Background").AddComponent<SpriteRenderer>();bg.transform.SetParent(root.transform);bg.sortingLayerName="BG";bg.sortingOrder=-100;bg.sprite=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/MoonlitCastleBattleStyle.asset").background;model.backdrop=bg;
        model.effectAnchor=new GameObject("Effect Anchor").transform;model.effectAnchor.SetParent(root.transform);model.effectAnchor.localPosition=new Vector3(0,.6f,0);
        var canvasGo=new GameObject("Effects HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasGo.transform.SetParent(root.transform);
        var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=0;
        model.safeRoot=Rect(canvasGo.transform,"Safe Area",Vector2.zero,Vector2.one);
        var top=Panel(model.safeRoot,"Catalog",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-220),Vector2.zero);
        Label(top,"Heading","特效测试室",22,12,676,46,28);
        model.title=Label(top,"Selected","",22,58,676,38,21);
        string[] names={"火球","命中爆炸","爪击","扬尘","火焰爆发"};model.displayNames=names;model.effects=new PetCindrelSpineEffect[5];model.selectionButtons=new Button[5];
        for(int i=0;i<5;i++){var id=PetCindrelEffectsBuilder.Names[i];model.effects[i]=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(PetCindrelEffectsBuilder.Root+"/"+id+"/"+id+".prefab");model.selectionButtons[i]=Button(top,id,names[i],22+(i%3)*226,102+(i/3)*55,218,48);}
        var bottom=Panel(model.safeRoot,"Controls",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,350));
        model.status=Label(bottom,"Status","",22,8,676,62,20);
        model.timeSlider=Slider(bottom,"Time",22,72,676,0,2,0);
        model.replayButton=Button(bottom,"Replay","重播",22,112,126,46);
        model.pauseButton=Button(bottom,"Pause","暂停",158,112,126,46);model.pauseText=model.pauseButton.GetComponentInChildren<Text>();
        model.stepButton=Button(bottom,"Step","步进 1/30",294,112,126,46);
        model.loopButton=Button(bottom,"Loop","循环：开",430,112,126,46);model.loopText=model.loopButton.GetComponentInChildren<Text>();
        model.backgroundButton=Button(bottom,"Background","换背景",566,112,132,46);
        model.widthText=Label(bottom,"Width Label","",22,172,676,32,20);
        model.widthSlider=Slider(bottom,"Width",22,215,676,2,70,25);
        model.speedText=Label(bottom,"Speed Label","",22,258,676,32,20);
        model.speedSlider=Slider(bottom,"Speed",22,305,676,.1f,2,1);
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(root.transform);
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/SpineEffectsTestRig.prefab");UnityEngine.Object.DestroyImmediate(root);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/SpineEffectsTestRig.prefab"));
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static RectTransform Panel(Transform p,string name,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){var r=Rect(p,name,min,max);r.offsetMin=lo;r.offsetMax=hi;r.gameObject.AddComponent<Image>().color=new Color(.045f,.055f,.065f,.96f);return r;}
    static RectTransform Box(Transform p,string name,float x,float y,float w,float h){var r=Rect(p,name,new Vector2(0,1),new Vector2(0,1));r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Text Label(Transform p,string name,string text,float x,float y,float w,float h,int size){var t=Box(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=Font;t.text=text;t.fontSize=size;t.color=new Color(.9f,.85f,.73f);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string name,string text,float x,float y,float w,float h){var r=Box(p,name,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.14f,.17f,.20f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var c=b.colors;c.pressedColor=new Color(.55f,.55f,.55f);b.colors=c;var t=Label(r,"Label",text,0,0,w,h,20);t.alignment=TextAnchor.MiddleCenter;return b;}
    static Slider Slider(Transform p,string name,float x,float y,float w,float min,float max,float value){var r=Box(p,name,x,y,w,28);var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.2f,.23f,.26f);var s=r.gameObject.AddComponent<Slider>();s.minValue=min;s.maxValue=max;s.value=value;var area=Rect(r,"Handle Area",Vector2.zero,Vector2.one);area.offsetMin=new Vector2(12,0);area.offsetMax=new Vector2(-12,0);var handle=Rect(area,"Handle",Vector2.zero,Vector2.one);handle.sizeDelta=new Vector2(24,0);var image=handle.gameObject.AddComponent<Image>();image.color=new Color(.8f,.64f,.38f);s.handleRect=handle;s.targetGraphic=image;return s;}
}
