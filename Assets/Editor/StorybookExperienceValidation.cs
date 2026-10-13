using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[InitializeOnLoad]
public static class StorybookExperienceValidation
{
    const string Out="output/experience-ui/";static bool running;static double deadline;
    static StorybookExperienceValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("XP_QA",false)){SessionState.SetBool("XP_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("XP_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("XP_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup>deadline){Log(false,"Timeout");EditorApplication.isPlaying=false;}}
    static void Log(bool ok,string message)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static void CheckButton(Button b){var r=(RectTransform)b.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);bool inside=true;foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(null,c);inside&=p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height;}var center=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);Log(inside&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,b.name+" safe and receives pointer");}
    static IEnumerator Run(){
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
        var gm=GameManager.single;var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        var ui=gc.experienceUI;var before=gm.gameState;gm.premiumCurrency=120;
        gc.homeUI.OpenFunction(1);gc.homeUI.OpenFunction(1);
        yield return new WaitForSecondsRealtime(.65f);yield return new WaitForEndOfFrame();
        Log(!ui.gameObject.activeSelf&&Router.single.ExperienceTransitioning&&TransitionCanvas.single.group.alpha>.99f,"Entry reaches black before switching page; repeated input guarded");Shot("transition-enter-black");
        yield return new WaitForSecondsRealtime(1.25f);
        Log(!Router.single.ExperienceTransitioning&&!TransitionCanvas.single.group.blocksRaycasts,"Entry fade completes and releases transition input blocker");Log(ui.gameObject.activeSelf&&gc.homeUI.gameObject.activeInHierarchy&&gm.gameState==before,"Experience shortcut opens full page and preserves return state");Log(ui.transform.Find("OutsideSafeArea").GetComponent<Image>().color.a==1&&ui.GetComponent<StorybookFusionLayout>()&&ui.paper.Find("Header/Back"),"Full screen uses opaque backdrop and fusion header/back layout");
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);gm.isBoostedXPActive=false;gm.xpBoostEndTime=DateTime.Now.AddSeconds(-1);ui.Refresh();yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Shot("idle-"+size.x+"x"+size.y);CheckButton(ui.paid);CheckButton(ui.advert);CheckButton(ui.close);
            gm.isBoostedXPActive=true;gm.xpBoostEndTime=DateTime.Now.AddSeconds(522);ui.Refresh();yield return new WaitForEndOfFrame();Shot("active-"+size.x+"x"+size.y);
        }
        StorybookEquipmentValidation.Resolution(1080,1920);gm.isBoostedXPActive=false;gm.xpBoostEndTime=DateTime.Now.AddSeconds(-1);ui.Purchase();var end=gm.xpBoostEndTime;ui.Purchase();Log(gm.premiumCurrency==70&&gm.isBoostedXPActive&&(end-DateTime.Now).TotalSeconds>598,"Payment charges 50 and grants 10 minutes; duplicate click blocked");yield return new WaitForSecondsRealtime(.6f);ui.Purchase();Log(gm.premiumCurrency==20&&Math.Abs((gm.xpBoostEndTime-end).TotalSeconds-600)<.1,"Renewal adds 10 minutes to remaining duration");ui.Refresh();yield return new WaitForEndOfFrame();Shot("insufficient");Log(!ui.paid.interactable,"Insufficient balance disables entire payment button");var unchanged=gm.xpBoostEndTime;ui.Purchase();Log(gm.premiumCurrency==20&&gm.xpBoostEndTime==unchanged,"Insufficient funds cannot buy");
        ui.WatchAd();yield return null;Log(!ui.Busy&&ui.notice.text.Contains("暂不可用"),"Unavailable editor ad unlocks controls without reward");
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=ui.GetType();type.GetField("<Busy>k__BackingField",flags).SetValue(ui,true);int token=(int)type.GetField("session",flags).GetValue(ui),id=(int)type.GetField("request",flags).GetValue(ui);var complete=type.GetMethod("CompleteAd",flags);complete.Invoke(ui,new object[]{token,id});complete.Invoke(ui,new object[]{token,id});Log(Math.Abs((gm.xpBoostEndTime-unchanged).TotalSeconds-600)<.1&&gm.premiumCurrency==20,"Simulated completed ad grants once with no currency charge");
        type.GetField("<Busy>k__BackingField",flags).SetValue(ui,true);unchanged=gm.xpBoostEndTime;type.GetMethod("AdCancelled",flags).Invoke(ui,new object[]{"ExperienceModal"});Log(!ui.Busy&&gm.xpBoostEndTime==unchanged,"Cancelled ad restores controls without granting time");
        yield return new WaitForSecondsRealtime(.6f);gm.premiumCurrency=120;gm.isBoostedXPActive=true;gm.xpBoostEndTime=DateTime.Now.AddSeconds(-1);ui.Refresh();Log(ui.state.text=="当前未开启"&&ui.duration.fillAmount==0&&ui.paid.interactable,"Expired boost returns to inactive state and payment recovers");
        ui.Close();yield return new WaitForSecondsRealtime(1.4f);Log(!ui.gameObject.activeSelf&&!ui.presentation.blocksRaycasts,"Return fade deactivates page and releases raycasts");var staleEnd=gm.xpBoostEndTime;complete.Invoke(ui,new object[]{token,id});Log(gm.xpBoostEndTime==staleEnd,"Stale ad callback cannot reward after closing");gc.homeUI.OpenFunction(1);yield return new WaitForSecondsRealtime(1.9f);Log(ui.gameObject.activeSelf&&!ui.Busy,"Reopen works");ui.Close();yield return new WaitForSecondsRealtime(1.4f);gm.rebirthLevel=0;gm.xpNow=0;gm.xpMax=100000;gm.isBoostedXPActive=true;gm.xpBoostEndTime=DateTime.Now.AddMinutes(1);gm.IncXp(100);Log(Mathf.Abs(gm.xpNow-133)<.01f,"Active boost gives 33 percent extra XP with page closed");gm.xpBoostEndTime=DateTime.Now.AddSeconds(-1);gm.IncXp(100);Log(Mathf.Abs(gm.xpNow-233)<.01f&&!gm.isBoostedXPActive,"Expired boost stops granting bonus with page closed");File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
