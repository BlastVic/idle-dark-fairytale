using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Ads;
using Assets.Scripts.Services.Ads.MediationProxy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookChestValidation
{
    public const string Out="output/chest-ui/";static bool started;static double deadline;
    static StorybookChestValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("CHEST_UI_QA",false)){SessionState.SetBool("CHEST_UI_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);started=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","Actual Unity chest detail validation; isolated project; save persistence disabled.\n");EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");SessionState.SetBool("CHEST_UI_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);started=false;deadline=EditorApplication.timeSinceStartup+300;StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;}
        if(EditorApplication.isPlaying&&SessionState.GetBool("CHEST_UI_QA",false)&&!started&&GameManager.single&&GameplayCanvas.single){started=true;deadline=EditorApplication.timeSinceStartup+300;GameManager.single.StartCoroutine(new ChestDetailProbe().Run());}
        if(EditorApplication.isPlaying&&SessionState.GetBool("CHEST_UI_QA",false)&&deadline>0&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog");EditorApplication.isPlaying=false;}}
    public static void Log(string s)=>File.AppendAllText(Out+"validation.txt",s+"\n");
}
public sealed class ChestDetailProbe
{
    int failures,errors;GameManager gm;Chest data;ChestPackage owner;StorybookChestUI ui;
    void Check(bool v,string n){StorybookChestValidation.Log((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
    void Error(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;StorybookChestValidation.Log("ERROR "+m+"\n"+s);}}
    void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookChestValidation.Out+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    void Bounds(RectTransform r,string n){var c=new Vector3[4];r.GetWorldCorners(c);Check(c.All(v=>v.x>=-1&&v.y>=-1&&v.x<=Screen.width+1&&v.y<=Screen.height+1),n);}
    void Hit(Button b,string n){var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,n);}
    Chest Fixture(double seconds){var c=new Chest{uniqueKey="ChestUIQA-"+Guid.NewGuid(),chestMapName="来自探索",chestGrade=ChestGrade.NORMAL,rewardTime=DateTime.Now.AddSeconds(seconds).ToBinary().ToString(),crystalsMin=25,crystalsMax=25,possibleRewards=new List<Item>(),possibleGoldRewards=new List<Item>(),possibleDiamondRewards=new List<Item>()};InventoryManager.single.chestsOwned.Clear();InventoryManager.single.chestsOwned.Add(c);return c;}
    IEnumerator Open(double seconds){data=Fixture(seconds);GameplayCanvas.single.homeUI.Refresh();GameplayCanvas.single.homeUI.chestButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(2);owner=ChestPackage.single;ui=owner?owner.view:null;Check(owner&&ui&&gm.gameState==GameStateType.CHEST_PACKAGE,"home chest click opens replacement prefab");}
    void TimeTo(double seconds){data.rewardTime=DateTime.Now.AddSeconds(seconds).ToBinary().ToString();owner.SetMe(data);ui.Refresh();}
    IEnumerator Return(){owner.CloseButton();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&!ChestPackage.single,"back returns home without orphan overlay");}
    public IEnumerator Run()
    {
        Application.logMessageReceived+=Error;gm=GameManager.single;var gc=GameplayCanvas.single;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        gm.premiumCurrency=160;yield return Open(1476);if(!ui){Finish();yield break;}
        int nodes=ui.GetComponentsInChildren<Transform>(true).Length;
        Check(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"no missing scripts");
        int[] w={941,768,720},h={1672,1024,1440};string[] tags={"9x16","3x4","1x2"};
        for(int i=0;i<3;i++){
            StorybookEquipmentValidation.Resolution(w[i],h[i]);yield return new WaitForSecondsRealtime(.4f);ui.GetComponent<StorybookFusionLayout>().Apply();TimeTo(1476);yield return new WaitForEndOfFrame();Shot(tags[i]+"-waiting");
            Bounds(ui.GetComponent<StorybookFusionLayout>().page,tags[i]+" authored page within screen");foreach(var b in new[]{ui.back,ui.pay,ui.advert}){Bounds((RectTransform)b.transform,tags[i]+" "+b.name+" fits");Hit(b,tags[i]+" "+b.name+" pointer");}
            Check(ui.waiting.activeSelf&&ui.paidActions.activeSelf&&!ui.freeActions.activeSelf,tags[i]+" waiting action visibility");
            var feedback=ui.pay.GetComponent<ShelterButtonFeedback>();var scale=ui.pay.transform.localScale;feedback.OnPointerDown(new PointerEventData(EventSystem.current));Check(ui.pay.transform.localScale.x<scale.x,tags[i]+" press feedback");feedback.OnPointerUp(new PointerEventData(EventSystem.current));Check(ui.pay.transform.localScale==scale,tags[i]+" release restores");
            TimeTo(-1);yield return new WaitForEndOfFrame();Shot(tags[i]+"-ready");Check(ui.ready.activeSelf&&ui.freeActions.activeSelf&&!ui.paidActions.activeSelf,tags[i]+" ready action visibility");Hit(ui.free,tags[i]+" free pointer");
        }
        var layout=ui.GetComponent<StorybookFusionLayout>();layout.SetValidationSafeArea(new Rect(0,65,Screen.width,Screen.height-125));yield return new WaitForEndOfFrame();Shot("1x2-safe-area");var c=new Vector3[4];layout.page.GetWorldCorners(c);Check(c.All(v=>v.y>=64&&v.y<=Screen.height-59),"safe area preserved");layout.SetValidationSafeArea(null);
        StorybookEquipmentValidation.Resolution(941,1672);yield return new WaitForSecondsRealtime(.3f);TimeTo(1);yield return new WaitForSecondsRealtime(1.2f);Check(owner.Ready&&ui.free.gameObject.activeInHierarchy,"timer expires into ready automatically");
        TimeTo(100);gm.premiumCurrency=1;ui.Refresh();float before=gm.crystals;owner.TapOpen();owner.PayCherriesButton();Check(!owner.Opening&&owner.Owned&&gm.crystals==before&&gm.premiumCurrency==1,"early free and unaffordable pay cannot consume or grant");yield return new WaitForEndOfFrame();Shot("insufficient-blood-moon");
        Check(!ui.pay.interactable&&ui.advert.interactable,"only unaffordable action disabled");gm.premiumCurrency=160;ui.Refresh();Check(ui.pay.interactable,"balance replenishment restores action");
        owner.WatchVidButton();Check(!owner.AwaitingAd&&!owner.Opening&&owner.Owned&&gm.crystals==before,"unavailable advertisement restores controls without reward");
        foreach(var grade in new[]{ChestGrade.GOLD,ChestGrade.DIAMOND}){data.chestGrade=grade;ui.Refresh();Check(ui.title.text==(grade==ChestGrade.GOLD?"黄金宝箱":"钻石宝箱"),"grade label "+grade);}data.chestGrade=ChestGrade.NORMAL;
        Check(ui.GetComponentsInChildren<Transform>(true).Length==nodes,"no runtime generated UI nodes");yield return Return();Check(InventoryManager.single.chestsOwned.Count==1,"back preserves unopened chest");
        yield return Open(100);owner.cherryCost=3;gm.premiumCurrency=10;before=gm.crystals;owner.PayCherriesButton();owner.PayCherriesButton();owner.TapOpen();Check(owner.Opening&&gm.premiumCurrency==7&&gm.crystals==before+25&&InventoryManager.single.chestsOwned.Count==0,"paid transaction charges configured amount and grants once");owner.CloseButton();Check(!owner.Leaving,"back cannot interrupt reward transaction");yield return new WaitForSecondsRealtime(1.5f);yield return new WaitForEndOfFrame();Shot("opening-reward");yield return new WaitForSecondsRealtime(3.5f);owner.view.opening.collect.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&gm.crystals==before+25,"paid reward returns home and count-up never grants twice");
        yield return Open(-1);before=gm.crystals;float currency=gm.premiumCurrency;owner.TapOpen();owner.TapOpen();Check(owner.Opening&&gm.crystals==before+25&&gm.premiumCurrency==currency,"free transaction grants once without premium cost");yield return new WaitForSecondsRealtime(5);owner.view.opening.collect.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        yield return Open(100);before=gm.crystals;currency=gm.premiumCurrency;
        var ad=AdManager.Instance;var field=typeof(AdManager).GetField("_mediationProxy",BindingFlags.Instance|BindingFlags.NonPublic);var previous=field.GetValue(ad);var fake=new FakeChestAd();field.SetValue(ad,fake);
        fake.RewardedVideoFinished+=success=>typeof(AdManager).GetMethod("OnRewardedVideoFinished",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ad,new object[]{success});
        try{owner.WatchVidButton();Check(owner.AwaitingAd&&!ui.back.interactable&&!ui.pay.interactable,"advertisement pending locks duplicate actions");fake.Finish(false);Check(!owner.AwaitingAd&&!owner.Opening&&owner.Owned&&gm.crystals==before,"cancelled advertisement grants nothing and recovers");owner.WatchVidButton();fake.Finish(true);fake.Finish(true);Check(owner.Opening&&gm.crystals==before+25&&gm.premiumCurrency==currency,"completed advertisement grants exactly once without premium cost");}finally{field.SetValue(ad,previous);}
        yield return new WaitForSecondsRealtime(5);owner.view.opening.collect.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&!ChestPackage.single,"advertisement reward returns home");Check(errors==0,"no unexpected runtime errors");Finish();
    }
    void Finish(){Application.logMessageReceived-=Error;StorybookChestValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
    sealed class FakeChestAd:IAdMediationProxy{public event Action<bool> RewardedVideoFinished;public void Finish(bool v)=>RewardedVideoFinished?.Invoke(v);public void Initialize(){}public bool IsInterstitialAvailable()=>false;public bool IsRewardedVideoAvailable()=>true;public void ShowBanner(){}public void ShowInterstitial(){}public void ShowRewardedVideo(){}public void ShowTestSuit(){}}
}
