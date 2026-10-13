using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Game.Enum;
using Scripts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class StorybookFusionValidation
{
    public const string Dir="output/fusion-ui/";
    static bool started;
    static double deadline;
    static StorybookFusionValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("FUSION_QA",false)){
                SessionState.SetBool("FUSION_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);started=false;
            }
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
            File.WriteAllText(Dir+"validation.txt","Fusion actual Unity validation. Isolated project; persistence disabled.\n");
            SessionState.SetBool("FUSION_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);started=false;
            deadline=EditorApplication.timeSinceStartup+240;StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;
        }
        if(SessionState.GetBool("FUSION_QA",false)&&EditorApplication.isPlaying&&!started&&GameManager.single&&GameplayCanvas.single){started=true;deadline=EditorApplication.timeSinceStartup+240;GameManager.single.StartCoroutine(new FusionProbe().Run());}
        if(SessionState.GetBool("FUSION_QA",false)&&EditorApplication.isPlaying&&deadline>0&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog");EditorApplication.isPlaying=false;}
    }
    public static void Log(string s)=>File.AppendAllText(Dir+"validation.txt",s+"\n");
}
public class FusionProbe
{
    int failures,errors;
    StorybookFusionUI ui;
    GameManager gm;
    void Check(bool result,string label){StorybookFusionValidation.Log((result?"PASS ":"FAIL ")+label);if(!result)failures++;}
    void Error(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!message.Contains("Ad is unavailable")){errors++;StorybookFusionValidation.Log("ERROR "+message+"\n"+stack);}}
    void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookFusionValidation.Dir+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    void Pointer(Button button,string name)
    {
        var rt=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        Check(point.x>=0&&point.y>=0&&point.x<=Screen.width&&point.y<=Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,name);
    }
    void Bounds(RectTransform rt,string name)
    {
        var corners=new Vector3[4];rt.GetWorldCorners(corners);Check(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),name);
    }
    IEnumerator Open()
    {
        Router.single.CallFusionFromCamp();float end=Time.realtimeSinceStartup+20;
        while(!FusionPackage.single&&Time.realtimeSinceStartup<end)yield return null;
        yield return new WaitForSecondsRealtime(1.3f);ui=FusionPackage.single?FusionPackage.single.view:null;
        Check(ui,"existing FusionPackage Addressable loads new authored UI");
    }
    void Seed(){gm.fusionLevel=6;gm.fxpMax=310;gm.fxpNow=180;gm.crystals=1280;}
    public IEnumerator Run()
    {
        Application.logMessageReceived+=Error;gm=GameManager.single;var gc=GameplayCanvas.single;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var tutorial in TutorialManager.single.tutorials)tutorial.SetActive(false);}
        Seed();yield return Open();if(!ui){Finish();yield break;}
        int nodeCount=ui.GetComponentsInChildren<Transform>(true).Length;
        Check(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"no missing scripts");
        Check(ui.rewards.Count(r=>r.root.activeSelf)==2&&ui.rewards[0].amount.text=="+1"&&ui.rewards[1].amount.text=="+50","reward preview matches level data");
        foreach(var size in new[]{new Vector2Int(941,1672),new Vector2Int(768,1024),new Vector2Int(720,1440)})
        {
            string tag=size.x==941?"9x16":size.x==768?"3x4":"1x2";
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
            var layout=ui.GetComponent<StorybookFusionLayout>();layout.Apply();Seed();ui.Refresh();yield return new WaitForEndOfFrame();Shot(tag+"-fusion");
            Pointer(ui.action,tag+" primary action reachable");Pointer(ui.back,tag+" back reachable");Pointer(ui.returnToShelter,tag+" return reachable");Bounds(layout.page,tag+" complete page fits");
            gm.fxpNow=310;gm.crystals=1150;ui.Refresh();yield return new WaitForEndOfFrame();Shot(tag+"-ready");
            var seal=new Vector3[4];((RectTransform)ui.readySeal.transform).GetWorldCorners(seal);var button=new Vector3[4];((RectTransform)ui.action.transform).GetWorldCorners(button);
            Check(seal[0].y-button[1].y>=10,tag+" wax seal and primary button have clear gap");
            layout.SetValidationSafeArea(new Rect(0,55,Screen.width,Screen.height-110));yield return new WaitForEndOfFrame();Bounds(layout.page,tag+" safe-area page fits");Pointer(ui.action,tag+" safe-area action");Shot(tag+"-safe-area");layout.SetValidationSafeArea(null);
        }
        StorybookEquipmentValidation.Resolution(595,1056);yield return new WaitForSecondsRealtime(.4f);
        Seed();ui.Refresh();yield return new WaitForEndOfFrame();Shot("reference-size-fusion");
        gm.fxpNow=310;gm.crystals=1150;ui.Refresh();yield return new WaitForEndOfFrame();Shot("reference-size-ready");
        StorybookEquipmentValidation.Resolution(941,1672);yield return new WaitForSecondsRealtime(.4f);
        Seed();gm.crystals=129;ui.Refresh();yield return null;yield return new WaitForEndOfFrame();Shot("insufficient");
        Check(!ui.action.interactable,"insufficient crystals disable fill");ui.Activate();Check(gm.crystals==129&&gm.fxpNow==180&&!ui.Busy,"insufficient direct invocation cannot spend");
        Check(ui.action.GetComponentsInChildren<Graphic>().All(g=>g.material.HasProperty("_DisabledUI")&&g.material.GetFloat("_DisabledUI")>0),"whole disabled button including frame and text is grayscale");
        gm.crystals=130;ui.Refresh();yield return new WaitForEndOfFrame();Check(ui.action.interactable,"exact balance permits fill");
        Check(ui.action.GetComponentsInChildren<Graphic>().All(g=>!g.material.HasProperty("_DisabledUI")||g.material.GetFloat("_DisabledUI")==0),"restored button returns original materials");
        var feedback=ui.action.GetComponent<ShelterButtonFeedback>();var original=ui.action.transform.localScale;var pointer=new PointerEventData(EventSystem.current);
        feedback.OnPointerDown(pointer);Check(ui.action.transform.localScale.x<original.x,"pressed state");feedback.OnPointerUp(pointer);Check(ui.action.transform.localScale==original,"released state");
        ui.Activate();ui.Activate();ui.Close();Check(ui.Busy&&!ui.Leaving&&!ui.back.interactable,"fill prevents repeated spend and leaving");
        yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Shot("filling");
        yield return new WaitForSecondsRealtime(1.2f);Check(gm.crystals==0&&gm.fxpNow==310&&gm.fusionLevel==6&&ui.Ready&&!ui.Busy,"fill deducts once, fills exactly, waits for explicit upgrade");
        Check(ui.action.interactable&&ui.actionCaption.text=="提升融合等级","ready state switches action");
        int stats=gm.statPoints;int lvl=gm.fusionLevel;float premium=gm.premiumCurrency;
        ui.Activate();ui.Activate();Check(gm.fusionLevel==lvl+1&&gm.statPoints>=stats+1,"upgrade executes original rewards once");Check(gm.premiumCurrency==premium,"premium blood moon crystals untouched");
        yield return new WaitForSecondsRealtime(3);Check(!FusionPackage.single&&gm.gameState==GameStateType.CAMP,"successful upgrade returns to shelter");yield return new WaitForEndOfFrame();Shot("upgrade-return");
        Seed();gm.fxpNow=180.5f;yield return Open();Check(ui.Cost==130,"fractional missing progress uses explicit whole-crystal cost");ui.Activate();yield return new WaitForSecondsRealtime(1.5f);Check(gm.crystals==1150&&gm.fxpNow==310,"fractional progress fills precisely without negative balance");
        // Every authored reward slot is reusable; larger levels scroll without spawning hierarchy.
        gm.fusionLevel=12;gm.fxpNow=0;gm.fxpMax=1300;ui.Refresh();yield return new WaitForEndOfFrame();Shot("multi-reward");
        Check(ui.rewards.Count(r=>r.root.activeSelf)>=3&&ui.rewardScroll.content.rect.height>ui.rewardScroll.viewport.rect.height,"multiple reward types use scrollable authored slots");
        ui.rewardScroll.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();Shot("multi-reward-bottom");Check(ui.rewards.Where(r=>r.root.activeSelf).All(r=>r.amount.cachedTextGenerator.lineCount<=1),"reward probability and amount labels stay on one line");Pointer(ui.action,"scrolling rewards keeps primary action reachable");
        gm.fusionLevel=LevelDataDescriptions.FusionLevelDb.Length;ui.Refresh();Check(!ui.action.interactable,"final level cannot index beyond reward database");ui.Activate();Check(!ui.Busy,"final-level invocation safely ignored");
        Seed();ui.Refresh();Check(ui.GetComponentsInChildren<Transform>(true).Length==nodeCount,"runtime creates no UI nodes");
        Check(ui.GetComponentsInChildren<Text>(true).All(t=>!new[]{"古神","邪神","祭拜","献祭","神像"}.Any(word=>t.text.Contains(word))),"player-facing authored UI avoids story spoilers");
        ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(!FusionPackage.single&&gm.gameState==GameStateType.CAMP,"back closes Addressable without orphan raycast blocker");
        if(gc.homeUI){gc.homeUI.functionButtons[0].onClick.Invoke();Check(!gc.homeUI.modal.activeSelf&&gc.homeUI.IsNavigating,"shelter fusion button navigates directly without modal");yield return new WaitForSecondsRealtime(3);Check(FusionPackage.single&&FusionPackage.single.view,"shelter function reaches new UI");if(FusionPackage.single)FusionPackage.single.view.Close();yield return new WaitForSecondsRealtime(2);}
        gm.lastBattleKey="test01";gc.LastBattleButtonClicked();yield return new WaitForSecondsRealtime(3);
        Check(gm.gameState==GameStateType.BATTLE&&!FusionPackage.single,"battle remains reachable after leaving fusion");
        Router.single.GoToCampFromBatle();yield return new WaitForSecondsRealtime(2);
        Check(gm.gameState==GameStateType.CAMP&&gc.homeUI.gameObject.activeSelf,"battle return restores shelter");
        Check(errors==0,"no unexpected runtime errors");Finish();
    }
    void Finish(){Application.logMessageReceived-=Error;StorybookFusionValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
