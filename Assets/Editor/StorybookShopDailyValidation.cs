using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Services.Ads;
using Assets.Scripts.Services.Ads.MediationProxy;
[InitializeOnLoad] public static class StorybookShopDailyValidation
{
 const string Out=StorybookShopDailyUpgrade.Out;static bool running;
 static void Check(bool b,string s)=>File.AppendAllText(Out+"validation.txt",(b?"PASS ":"FAIL ")+s+"\n");
 static StorybookShopDailyValidation(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EditorBuildSettings.scenes.First(s=>s.enabled).path);File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("SHOP_DAILY_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("SHOP_DAILY_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}};EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode){running=false;SessionState.SetBool("SHOP_DAILY_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}};}
 sealed class Proxy:IAdMediationProxy{
 public event Action<bool> RewardedVideoFinished;public bool ready=true;public int shows;
 public void Initialize(){}public bool IsInterstitialAvailable()=>false;public bool IsRewardedVideoAvailable()=>ready;public void ShowBanner(){}public void ShowInterstitial(){}public void ShowRewardedVideo(){shows++;}public void ShowTestSuit(){}public void Finish(bool s)=>RewardedVideoFinished?.Invoke(s);
 }
 static IEnumerator Run(){
 var intro=UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
 var gm=GameManager.single;var gc=GameplayCanvas.single;gc.GetComponent<StorybookDailyReward>().enabled=false;gc.dailyCherriesPop.SetActive(false);gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
 foreach(var d in UnityEngine.Object.FindObjectsByType<DebugMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))d.enabled=false;var dev=UnityEngine.Object.FindAnyObjectByType<DeveloperOverlayUI>();if(dev)dev.gameObject.SetActive(false);
 gm.thingsSeenPermanent.Remove(StorybookDailyReward.ClaimKey(DateTime.Today));Router.single.CashMallButton();yield return new WaitForSecondsRealtime(3);var ui=CashMallPackage.single.view;
 var manager=AdManager.Instance;var field=typeof(AdManager).GetField("_mediationProxy",BindingFlags.NonPublic|BindingFlags.Instance);var original=(IAdMediationProxy)field.GetValue(manager);var proxy=new Proxy();proxy.RewardedVideoFinished+=(Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>),manager,typeof(AdManager).GetMethod("OnRewardedVideoFinished",BindingFlags.NonPublic|BindingFlags.Instance));field.SetValue(manager,proxy);
 foreach(var sz in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){StorybookEquipmentValidation.Resolution(sz.x,sz.y);ui.FocusDaily();yield return new WaitForSecondsRealtime(.5f);Check(ui.dailyClaim.interactable&&ui.dailyAdvert.interactable,"Both choices available "+sz);yield return new WaitForEndOfFrame();Shot("shop-"+sz.x+"x"+sz.y);}
 float before=gm.premiumCurrency;proxy.ready=false;ui.dailyAdvert.onClick.Invoke();Check(!ui.Busy&&!StorybookDailyReward.ClaimedToday&&gm.premiumCurrency==before,"Unavailable retains free claim");proxy.ready=true;
 ui.dailyAdvert.onClick.Invoke();ui.dailyAdvert.onClick.Invoke();Check(proxy.shows==1&&ui.Busy&&!ui.dailyClaim.interactable&&!ui.back.interactable,"Pending guards duplicate click and navigation");proxy.Finish(false);Check(!ui.Busy&&!StorybookDailyReward.ClaimedToday&&gm.premiumCurrency==before,"Cancellation retains eligibility");
 ui.dailyAdvert.onClick.Invoke();typeof(StorybookShopUI).GetField("dailyAdDay",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui,DateTime.Today.AddDays(-1));proxy.Finish(true);Check(!StorybookDailyReward.ClaimedToday&&gm.premiumCurrency==before,"Midnight stale callback requires retry");
 ui.dailyAdvert.onClick.Invoke();proxy.Finish(true);Check(gm.premiumCurrency==before+gm.dailyCherriesAmount*2&&StorybookDailyReward.ClaimedToday,"Success grants double");proxy.Finish(true);Check(gm.premiumCurrency==before+gm.dailyCherriesAmount*2&&!StorybookDailyReward.TryClaim(),"Duplicate and shared claim guard");Check(!ui.dailyClaim.interactable&&!ui.dailyAdvert.interactable,"Both choices disabled after claim");yield return new WaitForSecondsRealtime(5);
 Router.single.CashMallButton();yield return new WaitForSecondsRealtime(3);ui=CashMallPackage.single.view;Check(!ui.dailyClaim.interactable&&!ui.dailyAdvert.interactable,"Reopen preserves claim");yield return new WaitForEndOfFrame();Shot("shop-claimed");
 gm.thingsSeenPermanent.Remove(StorybookDailyReward.ClaimKey(DateTime.Today));ui.Refresh();before=gm.premiumCurrency;ui.dailyClaim.onClick.Invoke();Check(gm.premiumCurrency==before+gm.dailyCherriesAmount&&!ui.dailyAdvert.interactable,"Free grants base and excludes double");field.SetValue(manager,original);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
 }
 static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
}
