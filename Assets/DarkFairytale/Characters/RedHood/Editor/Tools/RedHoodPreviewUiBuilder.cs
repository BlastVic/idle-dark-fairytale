using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Spine.Unity;
using Object=UnityEngine.Object;
namespace RedHoodPrototype.Editor {
 [InitializeOnLoad] public static class RedHoodPreviewUiBuilder {
  public const string ScenePath="Assets/DarkFairytale/Characters/RedHood/Tests/RedHoodAnimationTest.unity";
  public const string HudPath="Assets/DarkFairytale/Characters/RedHood/Tests/UI/Prefabs/RedHoodPreviewHUD.prefab";
  public const string Out="output/red-hood-preview-ui/";
  static RedHoodPreviewUiBuilder(){EditorApplication.update+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!File.Exists(Out+"install.request"))return;File.Delete(Out+"install.request");try{Install();File.WriteAllText(Out+"install.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"install.txt",e.ToString());Debug.LogException(e);}};}
  [MenuItem("Tools/Red Hood/Install Editable Preview UI")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
   var scene=EditorSceneManager.OpenScene(ScenePath);
   var model=Object.FindFirstObjectByType<RedHoodAnimationPreview>();
   if(!model)throw new Exception("Missing preview model");
   var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle_SkeletonData.asset");
   model.character.skeletonDataAsset=data;model.character.Initialize(true);
   if(!File.Exists(HudPath))BuildHud(data);
   // Targeted migration only; preserve all authored positions and styling.
   var prefab=PrefabUtility.LoadPrefabContents(HudPath);
   try{var ph=prefab.GetComponent<RedHoodPreviewHud>();if(!ph.catalogGroup){ph.catalogGroup=ph.animationPanel.AddComponent<CanvasGroup>();ph.catalogGroup.alpha=0;ph.catalogGroup.blocksRaycasts=false;ph.catalogGroup.interactable=false;PrefabUtility.SaveAsPrefabAsset(prefab,HudPath);}}finally{PrefabUtility.UnloadPrefabContents(prefab);}
   if(!model.hud)model.hud=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath),scene)).GetComponent<RedHoodPreviewHud>();
   model.autoAttack=false;model.paused=false;model.speed=model.zoom=1;model.returnToIdle=true;
   model.previewCamera=Camera.main;if(!model.previewCamera.GetComponent<AudioListener>())model.previewCamera.gameObject.AddComponent<AudioListener>();model.previewCamera.orthographic=true;model.previewCamera.orthographicSize=6.4f;model.previewCamera.transform.position=new Vector3(0,0,-20);
   model.character.transform.localScale=Vector3.one;model.character.transform.position=Vector3.zero;
   bool measured=false;Bounds bounds=new Bounds();
   foreach(var clip in model.character.Skeleton.Data.Animations){
    for(int i=0;i<=30;i++){
     model.character.AnimationState.ClearTracks();model.character.Skeleton.SetToSetupPose();
     var entry=model.character.AnimationState.SetAnimation(0,clip.Name,false);entry.TrackTime=clip.Duration*i/30f;entry.AnimationLast=entry.TrackTime;
     model.character.Update(0);model.character.LateUpdate();var b=model.character.GetComponent<MeshFilter>().sharedMesh.bounds;
     if(!measured){bounds=b;measured=true;}else bounds.Encapsulate(b);
    }
   }
   model.motionBounds=bounds;model.character.AnimationState.ClearTracks();model.character.Skeleton.SetToSetupPose();model.character.AnimationState.SetAnimation(0,"Idle1",true);model.character.Update(0);model.character.LateUpdate();
   var style=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");
   if(!model.backdrop){var bg=new GameObject("Black Forest Preview Backdrop").AddComponent<SpriteRenderer>();bg.sprite=style.background;bg.sortingOrder=-100;model.backdrop=bg;}
   model.character.GetComponent<Renderer>().sortingOrder=10;
   if(!Object.FindFirstObjectByType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
   Canvas.ForceUpdateCanvases();EditorUtility.SetDirty(model);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }
  static void BuildHud(SkeletonDataAsset data){
   var go=new GameObject("RedHoodPreviewHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
   try{
    go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=50;
    var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
    var h=go.AddComponent<RedHoodPreviewHud>();var core=Rect(go.transform,"Core_9x16",0,0,720,1280);
    var safe=go.AddComponent<BattleHudSafeArea>();safe.core=core;safe.referenceSize=new Vector2(720,1280);safe.Apply();
    h.previewArea=Rect(core,"CharacterPreviewArea",40,200,640,680);
    var header=Panel(core,"Header",16,16,688,86);h.title=Label(header,"Title","小红帽动画测试",20,8,430,36,28);
    Label(header,"Subtitle","角色预览 · 正式骨架",20,48,440,26,17,new Color(.62f,.59f,.53f));h.reset=Button(header,"ResetView","重置视角",510,19,158,48);
    h.selector=Button(core,"AnimationSelector","",16,112,688,60);h.selectorLabel=Label(h.selector.transform,"SelectedAnimation","Idle1 / 全部动画 +",20,6,648,48,23);
    var dock=Panel(core,"PlaybackDock",16,900,688,364);
    h.status=Label(dock,"Status","正在播放 Idle1",20,9,648,32,19);
    h.timeLabel=Label(dock,"Time","0.00 / 0.00 s",20,45,648,28,18);h.timeLabel.alignment=TextAnchor.MiddleRight;
    Label(dock,"TimelineLabel","时间轴 · 拖动暂停定位",20,45,400,28,18);
    h.timeSlider=Slider(dock,"Timeline",20,84,648,0,1,0);
    h.pause=Button(dock,"Pause","暂停",20,116,150,44);h.pauseLabel=h.pause.GetComponentInChildren<Text>();
    h.replay=Button(dock,"Replay","重播",186,116,150,44);
    h.previousFrame=Button(dock,"PreviousFrame","上一帧",352,116,150,44);h.nextFrame=Button(dock,"NextFrame","下一帧",518,116,150,44);
    h.loopToggle=Toggle(dock,"Loop","循环",20,170,145,false);h.cycleToggle=Toggle(dock,"Cycle","轮播",186,170,145,false);
    h.attackToggle=Toggle(dock,"Combo","连击",352,170,145,false);h.returnToggle=Toggle(dock,"ReturnIdle","接回待机",518,170,150,true);
    Label(dock,"SpeedLabel","速度",20,223,72,30,21);h.speedSlider=Slider(dock,"Speed",100,231,350,.1f,2,1);h.speedLabel=Label(dock,"SpeedValue","1.00×",460,221,208,32,17);h.speedLabel.alignment=TextAnchor.MiddleRight;
    Label(dock,"ZoomLabel","缩放",20,272,72,30,21);h.zoomSlider=Slider(dock,"Zoom",100,280,440,.5f,1.8f,1);h.zoomLabel=Label(dock,"ZoomValue","1.00×",550,270,118,32,21);h.zoomLabel.alignment=TextAnchor.MiddleRight;
    Label(dock,"Hint","60 fps 逐帧 · 失败按正式战斗 1.2× 播放",20,319,648,28,16,new Color(.65f,.64f,.56f));
    // Catalog last in sibling order: overlays the stage only when expanded.
    var panel=Panel(core,"AnimationCatalog",16,182,688,370);h.animationPanel=panel.gameObject;h.catalogGroup=panel.gameObject.AddComponent<CanvasGroup>();h.catalogGroup.alpha=0;h.catalogGroup.blocksRaycasts=false;h.catalogGroup.interactable=false;
    var input=Image(panel,"Search",18,14,652,48,new Color(.045f,.055f,.065f));Outline(input);h.search=input.gameObject.AddComponent<InputField>();
    h.search.textComponent=Label(input.transform,"Value","",14,2,624,44,21);h.search.placeholder=Label(input.transform,"Placeholder","搜索动画名称，例如 Skill / Death",14,2,624,44,21,new Color(.52f,.53f,.52f));h.search.targetGraphic=input;
    var scroll=Scroll(panel,"Animations",18,76,652,272,out var content);
    var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(207,48);grid.spacing=new Vector2(10,8);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;
    content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
    h.animationButtons=data.GetSkeletonData(false).Animations.Select(a=>Button(content,a.Name,a.Name,0,0,207,48)).ToArray();
    h.empty=Label(panel,"NoResults","没有匹配的动画",30,95,620,50,23);h.empty.alignment=TextAnchor.MiddleCenter;h.empty.gameObject.SetActive(false);panel.gameObject.SetActive(false);
    PrefabUtility.SaveAsPrefabAsset(go,HudPath);
   }finally{Object.DestroyImmediate(go);}
  }
    static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
    static readonly Color Ink=new Color(.065f,.08f,.09f,.97f),Line=new Color(.47f,.40f,.29f,1),Paper=new Color(.89f,.84f,.73f,1),Red=new Color(.43f,.10f,.11f,1);
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
    {var im=Image(p,n,x,y,w,h,new Color(.10f,.12f,.14f,1));im.raycastTarget=true;Outline(im);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;im.gameObject.AddComponent<RedHoodPreviewButtonState>();var colors=b.colors;colors.highlightedColor=new Color(1.3f,1.2f,1.1f);colors.pressedColor=new Color(.7f,.65f,.6f);colors.selectedColor=Color.white;b.colors=colors;var t=Label(im.transform,"Label",text,6,0,w-12,h,23);t.alignment=TextAnchor.MiddleCenter;return b;}
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
}
