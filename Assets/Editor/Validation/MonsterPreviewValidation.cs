using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Explicit local requests and menu-driven live validation; never overwrites authored UI.</summary>
[InitializeOnLoad]
public static class MonsterPreviewValidation
{
    public const string Dir="output/monster-preview-ui/";
    static string Target => SessionState.GetString("MONSTER_QA_TARGET", "MobTallowkin");
    static string Evidence => Target != "MobTallowkin" ? "output/monster-animation/" + Target + "/preview/" : Dir;
    const string Key="MONSTER_PREVIEW_VALIDATION";
    static UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> prefabLoad;
    static int step, ratio, errors;
    static double until, started;
    static readonly int[] W={720,768,720}; static readonly int[] H={1280,1024,1440};
    static readonly string[] Names={"9x16","3x4","1x2"};
    static MonsterPreviewValidation()
    {
        step=SessionState.GetInt(Key,0);
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=OnLog;
    }
    static void OnLog(string message,string trace,LogType type)
    {
        if(step>0 && (type==LogType.Error||type==LogType.Exception||type==LogType.Assert)) {errors++;Log("ERROR: "+message);}
    }
    [MenuItem("Tools/Dark Fairytale/Monsters/Validate Preview UGUI")]
    public static void Begin() => BeginFor("MobTallowkin");
    [MenuItem("Tools/Dark Fairytale/Monsters/Validate MobGloamweb In Test Scene")]
    public static void BeginMobGloamweb() => BeginFor("MobGloamweb");
    public static void BeginFor(string target)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetString("MONSTER_QA_TARGET", target);
        Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence+"validation.txt","MonsterAnimationTest UGUI live validation\n");
        var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
        Check(t && t.hud && t.stage,"Scene references complete");
        Check(PrefabUtility.IsPartOfPrefabInstance(t.hud),"Editable HUD prefab connection retained");
        Check(!typeof(MonsterAnimationTest).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).Any(m=>m.Name=="OnGUI"),"No runtime OnGUI");
        foreach(var tr in t.hud.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)==0,"Missing scripts: "+tr.name,false);
        Check(t.hud.cards.All(c=>c.data && c.portrait.sprite && c.title && c.button),"All persisted catalog cards have real portraits, names and buttons");
        SessionState.SetInt("MONSTER_OLD_SIZE",CurrentSize());
        SetResolution(720,1280);ratio=0;errors=0;started=EditorApplication.timeSinceStartup;Next(1,2);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(step==0)
            {
                if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir+"install.request"))
                {File.Delete(Dir+"install.request");MonsterPreviewUiBuilder.Install();File.WriteAllText(Dir+"installed.txt","OK");}
                if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir+"validate.request"))
                {File.Delete(Dir+"validate.request");Begin();}
                return;
            }
            if(EditorApplication.timeSinceStartup-started>150)throw new Exception("Timeout at step "+step);
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
            var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>(); if(!t || !t.hud)return;
            var hud=t.hud;
            if(step==1)
            {
                Check(t.character && t.character.valid,"Default monster loads");
                if(Target!="MobTallowkin")
                {
                    var entry=Scripts.Level.LevelDataDescriptions.EnemyDb.Single(e=>e.key==Target);
                    prefabLoad=UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(entry.assetRef.RuntimeKey);
                }
                t.gameObject.AddComponent<MonsterPreviewRuntimeProbe>().hud=hud;
                Check(!hud.catalogPanel.activeSelf,"Catalog collapsed by default");
                Check(t.stage.redHood.AnimationState.GetCurrent(0).Animation.Name=="Idle1","Red Hood stays on Idle1");
                hud.redHoodToggle.isOn=false;Check(!t.stage.redHood.gameObject.activeSelf,"Hide Red Hood");
                hud.redHoodToggle.isOn=true;Check(t.stage.redHood.gameObject.activeSelf,"Show Red Hood");
                hud.pauseButton.onClick.Invoke();Check(t.Paused,"Pause button");
                hud.speedSlider.value=.5f;Check(Mathf.Abs(t.speed-.5f)<.001f && t.stage.redHood.timeScale==1,"Monster speed independent of Red Hood");
                hud.pauseButton.onClick.Invoke();Check(!t.Paused,"Resume button");
                hud.speedSlider.value=1;
                hud.zoomSlider.value=1.4f;Check(Mathf.Abs(t.Zoom-1.4f)<.001f,"Zoom slider");
                hud.resetButton.onClick.Invoke();Check(t.Zoom==1,"Reset fits the preview");
                hud.catalogButton.onClick.Invoke();Check(hud.catalogPanel.activeSelf,"Expand catalog");
                hud.searchField.text="符文狼";Check(hud.cards.Count(c=>c.gameObject.activeSelf)==1,"Search Chinese name");
                hud.searchField.text="mobdrowseed";Check(hud.cards.Count(c=>c.gameObject.activeSelf)==1,"Search case-insensitive ID");
                hud.searchField.text="no-such-monster";Check(hud.emptyText.gameObject.activeSelf,"Empty search result feedback");
                hud.searchField.text="";
                Canvas.ForceUpdateCanvases();
                hud.catalogScroll.verticalNormalizedPosition=0;
                Canvas.ForceUpdateCanvases();
                var lastCard=(RectTransform)hud.cards.Last().transform;
                var lastCorners=new Vector3[4]; lastCard.GetWorldCorners(lastCorners);
                var viewportCorners=new Vector3[4];hud.catalogScroll.viewport.GetWorldCorners(viewportCorners);
                Check(lastCorners[0].y>=viewportCorners[0].y-1 && lastCorners[1].y<=viewportCorners[1].y+1,"Last monster reachable by scrolling");
                hud.catalogScroll.verticalNormalizedPosition=1;
                foreach(var card in hud.cards)
                {
                    card.button.onClick.Invoke();Check(t.character && t.character.valid && t.Error==null,"Select "+card.displayName.Replace('\n',' '));
                    foreach(var name in t.AnimationNames) {t.Play(Array.IndexOf(t.AnimationNames,name));t.character.Update(.05f);t.character.LateUpdate();}
                }
                var selected=hud.cards.First(c=>c.data.name == Target + "_SkeletonData");selected.button.onClick.Invoke();
                hud.animationButtons[1].onClick.Invoke();Check(t.AnimationNames[t.SelectedAnimation]=="Attack1","Animation button binds to real track");
                hud.replayButton.onClick.Invoke();Check(t.character.AnimationState.GetCurrent(0).TrackTime<.01f,"Replay resets track time");
                hud.loopToggle.isOn=false;Check(!t.character.AnimationState.GetCurrent(0).Loop,"Loop toggle");hud.loopToggle.isOn=true;
                hud.cycleToggle.isOn=true;Next(2,3);
            }
            else if(step==2)
            {
                if(Target!="MobTallowkin")
                {
                    if(!prefabLoad.IsDone) {Next(2,.1);return;}
                    Check(prefabLoad.Status==UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && prefabLoad.Result.name==Target, "Runtime database / Addressables resolves target prefab");
                    Check(prefabLoad.Result.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true).transform.localScale.x<0,"Prefab faces the left-side player");
                    UnityEngine.AddressableAssets.Addressables.Release(prefabLoad);
                }
                Check(t.SelectedAnimation!=1,"Automatic animation cycling advances");hud.cycleToggle.isOn=false;t.Play(0);t.FitCamera();SetResolution(720,1280);Next(3,1);
            }
            else if(step==3)
            {
                Check(t.previewCamera.pixelWidth==W[ratio]&&t.previewCamera.pixelHeight==H[ratio],"Actual Game View resolution "+Names[ratio]+" ("+t.previewCamera.pixelWidth+"x"+t.previewCamera.pixelHeight+")");
                Layout(t,Names[ratio]);
                ScreenCapture.CaptureScreenshot(Evidence+"collapsed-"+Names[ratio]+".png");Next(4,.5);
            }
            else if(step==4)
            {
                hud.catalogButton.onClick.Invoke();Canvas.ForceUpdateCanvases();
                Check(hud.catalogPanel.activeSelf,"Expanded at "+Names[ratio]);
                ScreenCapture.CaptureScreenshot(Evidence+"expanded-"+Names[ratio]+".png");Next(5,.5);
            }
            else if(step==5)
            {
                hud.SetCatalogOpen(false);ratio++;
                if(ratio<3){SetResolution(W[ratio],H[ratio]);Next(3,1);}
                else
                {
                    var safe=hud.GetComponent<BattleHudSafeArea>();safe.enabled=false;
                    BattleHudSafeArea.FitCore(safe.core,((RectTransform)safe.transform).rect.size,new Rect(0,.04f,1,.9f),safe.referenceSize);
                    Canvas.ForceUpdateCanvases();
                    Check(safe.core.localScale.x==safe.core.localScale.y,"Safe area uses uniform scale");
                    var rootSize=((RectTransform)safe.transform).rect.size;
                    float halfHeight=safe.core.rect.height*safe.core.localScale.y*.5f;
                    Check(safe.core.anchoredPosition.y-halfHeight>=-rootSize.y*.5f+rootSize.y*.04f-.1f &&
                        safe.core.anchoredPosition.y+halfHeight<=-rootSize.y*.5f+rootSize.y*.94f+.1f,"Simulated safe-area top and bottom insets respected");
                    safe.enabled=true;safe.Apply();
                    if(Target!="MobTallowkin") {SetResolution(720,1280);Next(6,1);return;}
                    Log(errors==0?"PASS: runtime errors=0, all interactions and ratios validated.":"FAIL: runtime errors="+errors);
                    step=0;SessionState.SetInt(Key,0);EditorApplication.isPlaying=false;
                    SetSize(SessionState.GetInt("MONSTER_OLD_SIZE",0));
                }
            }
            else if(step>=6 && step<=21)
            {
                int pose=step-6;
                t.SetAutoCycle(false);t.SetPaused(true);t.loop=false;
                t.Play(pose/4);
                t.character.timeScale=1;
                t.character.Update(t.character.Skeleton.Data.FindAnimation(t.AnimationNames[pose/4]).Duration*(pose%4)/3f);
                t.character.LateUpdate();t.character.timeScale=0;
                Layout(t,t.AnimationNames[pose/4]+" keyframe "+(pose%4));
                ScreenCapture.CaptureScreenshot(Evidence+t.AnimationNames[pose/4]+"-"+(pose%4)+".png");
                Next(step+1,.3);
            }
            else if(step==22)
            {
                Check(t.character.Skeleton.Skin.Name=="1", "Preview uses prefab numeric skin 1");
                Log(errors==0?"PASS: runtime errors=0, all interactions, ratios and target pose screenshots complete.":"FAIL: runtime errors="+errors);
                step=0;SessionState.SetInt(Key,0);EditorApplication.isPlaying=false;
                SetSize(SessionState.GetInt("MONSTER_OLD_SIZE",0));
            }
        }
        catch(Exception e)
        {Log("FAIL: "+e);step=0;SessionState.SetInt(Key,0);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;Debug.LogException(e);}
    }
    static void Layout(MonsterAnimationTest t,string label)
    {
        var hud=t.hud;
        foreach(var control in hud.GetComponentsInChildren<Selectable>())
        {
            // Scroll contents may intentionally extend beyond their clipped viewport.
            if(control.transform.IsChildOf(hud.animationContent)||control.transform.IsChildOf(hud.cardContent))continue;
            var corners=new Vector3[4];((RectTransform)control.transform).GetWorldCorners(corners);
            Check(corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=t.previewCamera.pixelWidth+1&&c.y<=t.previewCamera.pixelHeight+1),label+" control inside screen: "+control.name,false);
        }
        foreach(var actor in new[]{t.character,t.stage.redHood})
        {
            var b=actor.GetComponent<Renderer>().bounds;var lo=t.previewCamera.WorldToScreenPoint(b.min);var hi=t.previewCamera.WorldToScreenPoint(b.max);
            var dock=hud.pauseButton.transform.parent as RectTransform;var corners=new Vector3[4];dock.GetWorldCorners(corners);
            Check(lo.x>=0&&hi.x<=t.previewCamera.pixelWidth&&lo.y>corners[1].y&&hi.y<t.previewCamera.pixelHeight,label+" full actor visible above controls: "+actor.name);
        }
        var probe=t.GetComponent<MonsterPreviewRuntimeProbe>();
        Check(probe && probe.width==t.previewCamera.pixelWidth && probe.height==t.previewCamera.pixelHeight && probe.selectorHit,label+" selector receives pointer raycast in runtime frame");
        Check(Mathf.Abs(hud.GetComponent<BattleHudSafeArea>().core.localScale.x-hud.GetComponent<BattleHudSafeArea>().core.localScale.y)<.0001f,label+" UI preserves aspect");
    }
    static void Next(int s,double delay){step=s;SessionState.SetInt(Key,s);until=EditorApplication.timeSinceStartup+delay;}
    static void Check(bool ok,string msg,bool verbose=true){if(!ok)throw new Exception(msg);if(verbose)Log("PASS: "+msg);}
    static void Log(string msg){Directory.CreateDirectory(Evidence);File.AppendAllText(Evidence+"validation.txt",msg+"\n");}
    static EditorWindow View()=>EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
    static PropertyInfo SizeProperty()=>View().GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    static int CurrentSize()=>(int)SizeProperty().GetValue(View());
    static void SetSize(int i){SizeProperty().SetValue(View(),i);View().Repaint();View().Focus();}
    static void SetResolution(int w,int h)
    {
        var a=typeof(Editor).Assembly;var type=a.GetType("UnityEditor.GameViewSizes");var instance=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var group=type.GetMethod("GetGroup").Invoke(instance,new[]{type.GetProperty("currentGroupType",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance)});
        var size=Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),w,h,"Monster QA "+w+"x"+h},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});SetSize((int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);
    }
}
