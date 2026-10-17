using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookMapValidation
{
    const string Dir="output/map-ui/";static int stage,ratio,node,nav,errors;static double due,deadline;static bool captureOnly;
    static readonly int[] W={941,768,720},H={1672,1024,1440};static readonly string[] Ratios={"9x16","3x4","1x2"};
    static StorybookMapValidation(){stage=SessionState.GetInt("STORYBOOK_MAP_STAGE",0);ratio=SessionState.GetInt("STORYBOOK_MAP_RATIO",0);node=SessionState.GetInt("STORYBOOK_MAP_NODE",0);nav=SessionState.GetInt("STORYBOOK_MAP_NAV",0);captureOnly=SessionState.GetBool("STORYBOOK_MAP_CAPTURE",false);if(stage>0){due=EditorApplication.timeSinceStartup+2;deadline=due+300;}if(SessionState.GetBool("STORYBOOK_MAP_QA",false)){SessionState.SetBool("STORYBOOK_MAP_QA",false);stage=1;due=EditorApplication.timeSinceStartup+3;deadline=due+300;captureOnly=SessionState.GetBool("STORYBOOK_MAP_CAPTURE",false);}EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode){stage=0;SessionState.SetInt("STORYBOOK_MAP_STAGE",0);}};EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(stage>0&&(t==LogType.Error||t==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;Log("ERROR "+m);}};}
    static void Log(string s)=>File.AppendAllText(Dir+"validation.txt",s+"\n");
    static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
    static void Next(int n,double delay=1){stage=n;SessionState.SetInt("STORYBOOK_MAP_STAGE",n);SessionState.SetInt("STORYBOOK_MAP_RATIO",ratio);SessionState.SetInt("STORYBOOK_MAP_NODE",node);SessionState.SetInt("STORYBOOK_MAP_NAV",nav);due=EditorApplication.timeSinceStartup+delay;Log("STAGE "+n+" ratio="+ratio+" node="+node);}
    static void Resolution(int w,int h)=>typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});
    static void Finish(string s){Log(s);stage=0;SessionState.SetInt("STORYBOOK_MAP_STAGE",0);Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
    static StorybookMapUI Map(){var go=LevelController.Instance.AssetManager.mapPackageObj;return go?go.GetComponentInChildren<StorybookMapUI>():null;}
    static void OpenMap(){GameplayCanvas.single.homeUI.navigationButtons[3].onClick.Invoke();}
    static int Battle(StorybookMapUI ui,int i)=>Array.IndexOf(ui.nodes,ui.nodes.Where(n=>!n.IsStory).ElementAt(i));
    static void Fixture(StorybookMapUI ui){WaveManager.single.highestWavesReached.Clear();for(int i=0;i<2;i++){int index=Battle(ui,i);WaveManager.single.SetWaveseReachedByKey(ui.nodes[index].levelKey,ui.Data(index).waves.Length);}ui.SelectNode(Battle(ui,2));ui.Refresh();}
    static IEnumerator PointerProbe(StorybookMapUI ui){
        yield return new WaitForEndOfFrame();
        foreach(var b in ui.GetComponentsInChildren<Button>()){
            var r=(RectTransform)b.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);
            var screen=ui.GetComponent<Canvas>().pixelRect;bool inside=corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=screen.width+1&&c.y<=screen.height+1);
            var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));if(b.GetComponent<StorybookMapNode>()&&!RectTransformUtility.RectangleContainsScreenPoint(ui.scroll.viewport,point,null))continue;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            bool pass=inside&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b;
            if(!pass)errors++;Log((pass?"PASS":"FAIL")+" rendered pointer "+b.name+" => "+(hits.Count==0?"none":hits[0].gameObject.name));
        }
    }
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        // Navigation QA must not race an unrelated death presentation while entering higher-level maps.
        if(stage>0&&EditorApplication.isPlaying&&GameManager.single&&GameManager.single.gameState==GameStateType.BATTLE&&Player.single&&Player.single.ab&&Player.single.ab.currentStat!=null){Player.single.ab.currentStat.hpMax=1000000;Player.single.ab.currentStat.hpNow=1000000;}
        try{
        if(stage==0&&!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode){
            string previous=SessionState.GetString("MAP_QA_RESTORE_SCENE","");
            if(!string.IsNullOrEmpty(previous)){
                SessionState.EraseString("MAP_QA_RESTORE_SCENE");
                var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(current.path=="Assets/Scenes/Gameplay.unity"&&!current.isDirty&&File.Exists(previous))UnityEditor.SceneManagement.EditorSceneManager.OpenScene(previous);
            }
        }
        if(File.Exists(Dir+"restart.request")){bool full=File.ReadAllText(Dir+"restart.request").Trim()=="full";File.Delete(Dir+"restart.request");stage=0;SessionState.SetInt("STORYBOOK_MAP_STAGE",0);EditorApplication.isPlaying=false;File.WriteAllText(Dir+(full?"validate.request":"capture.request"),"");return;}
        if(stage==0){bool cap=File.Exists(Dir+"capture.request");if(!cap&&!File.Exists(Dir+"validate.request"))return;if(EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Dir+"capture.request");File.Delete(Dir+"validate.request");Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"validation.txt","Actual Gameplay map validation; save persistence disabled\n");
            var sourceScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(sourceScene.path!="Assets/Scenes/Gameplay.unity"){
                if(sourceScene.isDirty)throw new Exception("Save the current scene before running map validation");
                SessionState.SetString("MAP_QA_RESTORE_SCENE",sourceScene.path);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            }
            errors=ratio=node=nav=0;captureOnly=cap;SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("STORYBOOK_MAP_QA",true);SessionState.SetBool("STORYBOOK_MAP_CAPTURE",cap);Resolution(W[0],H[0]);deadline=EditorApplication.timeSinceStartup+300;EditorApplication.isPlaying=true;Next(1,3);return;}
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Map QA timeout stage="+stage);
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due||!GameManager.single||!GameplayCanvas.single||!WaveManager.single)return;
        var gm=GameManager.single;var gc=GameplayCanvas.single;var assets=LevelController.Instance.AssetManager;
        if(stage==1){
            if(gm.gameState!=GameStateType.CAMP||!gc.homeUI)return;var intro=UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;
            gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
            for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
            if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
            OpenMap();Next(2,2);return;
        }
        var ui=Map();
        if(stage==2){if(!ui||gm.gameState!=GameStateType.MAP)return;Check(ui.nodes.Length==7&&ui.nodes.Count(n=>n.IsStory)==2,"Five battles and two authored story nodes");Check(ui.nodes.Where(n=>!n.IsStory).Select(n=>n.levelKey).SequenceEqual(StorybookMapBuilder.Keys),"Correct five existing battle keys");Check(assets.mapPackageObj.GetComponentsInChildren<MapNode>(true).Length==0,"Legacy/future map nodes removed");Check(!assets.mapPackageObj.transform.Find("Canvas")&&!assets.mapPackageObj.transform.Find("Clouds"),"Legacy background and cloud layers removed");Fixture(ui);Next(3);return;}
        if(stage==3){ui.layout.Apply();ui.scroll.content.anchoredPosition=new Vector2(0,(ratio==1?400:149)*ui.scroll.content.localScale.y);ui.Refresh();
            foreach(var n in ui.nodes){var label=(RectTransform)n.labelRoot.transform;var seal=n.seal.rectTransform;if(!n.IsStory)Check(Mathf.Abs(label.TransformPoint(label.rect.center).x-seal.TransformPoint(seal.rect.center).x)<.1f,"Centered battle label "+n.levelKey);int i=Array.IndexOf(ui.nodes,n);Check(n.labelRoot.activeSelf==ui.IsUnlocked(i)&&n.progress.gameObject.activeSelf==ui.IsUnlocked(i),"Locked labels hidden "+i);}Canvas.ForceUpdateCanvases();ui.StartCoroutine(PointerProbe(ui));Next(4,.5);return;}
        if(stage==4){ScreenCapture.CaptureScreenshot(Dir+"map-"+Ratios[ratio]+".png");Log("CAPTURE actual map "+Ratios[ratio]);Next(5,.7);return;}
        if(stage==5){ratio++;if(ratio<3){Resolution(W[ratio],H[ratio]);Next(3,1);return;}if(captureOnly){Finish(errors==0?"PASS capture / five nodes / three ratios":"FAIL pointer errors="+errors);return;}Resolution(941,1672);Next(6,1);return;}
        if(stage==6){
            ui.layout.Apply();WaveManager.single.highestWavesReached.Clear();ui.SelectNode(Battle(ui,4));ui.Refresh();Check(!ui.enterButton.interactable,"Locked node disables entry");ui.EnterSelected();ui.ClickNode(Battle(ui,4));Check(gm.gameState==GameStateType.MAP&&!ui.IsNavigating,"Locked repeat click cannot start battle");
            ui.SelectNode(Battle(ui,0));Check(ui.enterButton.interactable,"First node available for fresh progress");
            var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(400,600)};ui.scroll.OnInitializePotentialDrag(p);ExecuteEvents.Execute(ui.scroll.gameObject,p,ExecuteEvents.beginDragHandler);p.position=new Vector2(400,800);p.delta=new Vector2(0,200);ExecuteEvents.Execute(ui.scroll.gameObject,p,ExecuteEvents.dragHandler);ui.SelectNode(Battle(ui,1));ui.EnterSelected();ui.ClickNode(Battle(ui,0));Check(ui.IsDragging&&ui.selectedIndex==Battle(ui,0)&&!ui.IsNavigating,"Drag cannot select or enter a node");ExecuteEvents.Execute(ui.scroll.gameObject,p,ExecuteEvents.endDragHandler);Next(7,.3);return;
        }
        if(stage==7){
            ui.SelectNode(Battle(ui,2));ui.LocateSelection();Canvas.ForceUpdateCanvases();Check(ui.scroll.velocity.sqrMagnitude<.01f,"Locate stops scroll inertia");ScreenCapture.CaptureScreenshot(Dir+"locked-node.png");
            foreach(var n in ui.nodes.Where(n=>!n.IsStory)){var w=WaveManager.single.GetWavesetByKey(n.levelKey);WaveManager.single.SetWaveseReachedByKey(w.levelKey,w.waves.Length);if(!string.IsNullOrEmpty(w.precondition)){var pre=WaveManager.single.GetWavesetByKey(w.precondition);WaveManager.single.SetWaveseReachedByKey(pre.levelKey,pre.waves.Length);}}
            ui.Refresh();Check(ui.nodes.Select((n,i)=>ui.IsUnlocked(i)).All(b=>b),"All five nodes respect completed prerequisites");node=0;Next(8,.3);return;
        }
        if(stage==8){
            if(node>0)Check(Mathf.Abs(ui.scroll.content.anchoredPosition.y/ui.scroll.content.localScale.y-260)<1,"Scroll position restored after battle "+node);
            ui.scroll.StopMovement();ui.scroll.content.anchoredPosition=new Vector2(0,260*ui.scroll.content.localScale.y);
            ui.SelectNode(Battle(ui,(node+1)%5));ui.nodes[Battle(ui,node)].button.onClick.Invoke();
            Check(ui.selectedIndex==Battle(ui,node)&&!ui.IsNavigating,"First click selects different node "+node);
            Check((ui.detailMotion.fullCardRise?ui.detailMotion.group.alpha==1:ui.detailMotion.group.alpha<.99f)&&ui.detailMotion.content.anchoredPosition.y<0,"Card entrance animation starts "+node);
            ui.nodes[Battle(ui,node)].button.onClick.Invoke();Check(ui.IsNavigating,"Second click enters and prevents repeated navigation "+node);ui.EnterSelected();Next(9,2);return;}
        if(stage==9){
            if(gm.gameState!=GameStateType.BATTLE||WaveManager.single.currentWaveset.levelKey!=StorybookMapBuilder.Keys[node]||GameObject.FindGameObjectsWithTag("MapGraphics").Length!=1)return;
            Check(!assets.mapPackageObj.activeSelf&&gc.battleHud.gameObject.activeSelf,"Node "+(node+1)+" enters existing battle flow");
            var catalog=Resources.Load<ChapterEnvironmentCatalog>("Chapter01Environments");var expected=catalog.entries[node].prefab.GetComponent<SpriteRenderer>().sprite;
            Check(GameObject.FindGameObjectWithTag("MapGraphics").GetComponent<SpriteRenderer>().sprite==expected,"Node "+(node+1)+" matches actual chapter environment");
            Check(gc.battleHud.backButton.gameObject.activeInHierarchy&&gc.battleHud.backButton.interactable,"Visible battle return control before navigation");gc.battleHud.backButton.onClick.Invoke();Next(10,2);return;
        }
        if(stage==10){if(gm.gameState!=GameStateType.CAMP)return;Check(gc.homeUI.gameObject.activeSelf,"Battle returns to shelter "+node);node++;OpenMap();Next(node<5?8:11,2);return;}
        if(stage==11){if(!ui||gm.gameState!=GameStateType.MAP)return;Check(Mathf.Abs(ui.scroll.content.anchoredPosition.y/ui.scroll.content.localScale.y-260)<1,"Map position survives reopen "+nav);Check(Mathf.Abs(ui.detailMotion.group.alpha-1)<.01f&&ui.detailMotion.content.anchoredPosition==Vector2.zero,"Card animation restores settled state");ui.navigationButtons[nav==0?0:nav==1?1:4].onClick.Invoke();Next(12,2);return;}
        if(stage==12){var expected=nav==0?GameStateType.INVENTORY:nav==1?GameStateType.SKILLS:GameStateType.CASH_MALL;Check(gm.gameState==expected&&!assets.mapPackageObj.activeSelf,"Map navigation opens "+expected);if(nav==2)Router.single.CallCampFromCashMall();else Router.single.CampButton();Next(13,2);return;}
        if(stage==13){if(gm.gameState!=GameStateType.CAMP)return;nav++;OpenMap();Next(nav<3?11:14,2);return;}
        if(stage==14){if(!ui||gm.gameState!=GameStateType.MAP)return;ui.backButton.onClick.Invoke();Next(15,2);return;}
        if(stage==15){Check(gm.gameState==GameStateType.CAMP&&gc.homeUI.gameObject.activeSelf,"Back returns to shelter");Finish(errors==0?"PASS all map checks; unexpected runtime errors=0":"FAIL runtime/pointer errors="+errors);}
    }catch(Exception e){Finish("FAIL "+e);}}
}
