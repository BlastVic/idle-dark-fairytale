using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Scripts.Game.Enum;
[InitializeOnLoad] public static class DailySupplyValidation
{
 const string Out="output/daily-supply/";static bool running;static double started;
 static DailySupplyValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("DAILY_SUPPLY_QA",false)){SessionState.SetBool("DAILY_SUPPLY_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("DAILY_SUPPLY_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("DAILY_SUPPLY_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;started=EditorApplication.timeSinceStartup;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup-started>160){Check(false,"timeout");EditorApplication.isPlaying=false;}}
 static void Check(bool b,string msg)=>File.AppendAllText(Out+"validation.txt",(b?"PASS ":"FAIL ")+msg+"\n");
 static void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 static void Hit(Button b,string name){var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.position)},hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,name);}
 static StorybookDailyReward Fresh(StorybookDailyReminder r){var old=GameplayCanvas.single.GetComponent<StorybookDailyReward>();UnityEngine.Object.DestroyImmediate(old);var c=GameplayCanvas.single.gameObject.AddComponent<StorybookDailyReward>();c.reminder=r;return c;}

 sealed class AdStub : Assets.Scripts.Services.Ads.MediationProxy.IAdMediationProxy {
 public event Action<bool> RewardedVideoFinished; public void Finish(bool success)=>RewardedVideoFinished?.Invoke(success);
 public void Initialize(){} public bool IsInterstitialAvailable()=>false;public bool IsRewardedVideoAvailable()=>true;public void ShowBanner(){}public void ShowInterstitial(){}public void ShowRewardedVideo(){}public void ShowTestSuit(){}
 }
 static IEnumerator Run(){
 var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
 var gc=GameplayCanvas.single;var gm=GameManager.single;var r=gc.GetComponent<StorybookDailyReward>().reminder;
 var dev=UnityEngine.Object.FindFirstObjectByType<DeveloperOverlayUI>();if(dev)dev.gameObject.SetActive(false);
 TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();for(int i=0;i<20;i++)if(!gm.thingsSeenPermanent.Contains("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
 gm.thingsSeenPermanent.Remove(StorybookDailyReward.ClaimKey(DateTime.Today));gm.thingsSeenPermanent.Remove(StorybookDailyReward.DismissKey(DateTime.Today));
 r.Show();float before=gm.premiumCurrency;
 foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);Hit(r.open,"Direct button "+size);Hit(r.advert,"Ad button "+size);Hit(r.close,"Back button "+size);yield return new WaitForEndOfFrame();Shot("reminder-"+size.x+"x"+size.y);}
 r.WatchAd();Check(!r.Busy&&gm.premiumCurrency==before&&r.open.interactable,"Unavailable ad grants nothing; direct claim remains usable");
 r.open.onClick.Invoke();r.Claim();Check(gm.premiumCurrency==before+gm.dailyCherriesAmount&&StorybookDailyReward.ClaimedToday,"Direct claim exactly once");Check(r.gameObject.activeSelf&&gc.homeUI.gameObject.activeSelf&&!CashMallPackage.single&&!r.open.interactable&&!r.advert.interactable,"Claim stays here; both claim buttons disabled");yield return new WaitForEndOfFrame();Shot("claimed");
 gm.thingsSeenPermanent.Remove(StorybookDailyReward.ClaimKey(DateTime.Today));r.Show();r.close.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(StorybookDailyReward.DismissedToday&&!r.gameObject.activeSelf&&!r.group.blocksRaycasts&&!StorybookDailyReward.ClaimedToday,"Dismiss is dated and leaves reward unclaimed");var c=Fresh(r);yield return new WaitForSecondsRealtime(2.3f);Check(!r.gameObject.activeSelf&&c.ReminderHandled,"New controller respects same-day dismissal");
 gm.thingsSeenPermanent.Remove(StorybookDailyReward.DismissKey(DateTime.Today));gm.thingsSeenPermanent.Add(StorybookDailyReward.DismissKey(DateTime.Today.AddDays(-1)));c=Fresh(r);yield return new WaitForSecondsRealtime(2.4f);Check(r.gameObject.activeSelf,"Yesterday dismissal allows reminder today");
 var ad=Assets.Scripts.Services.Ads.AdManager.Instance;var field=ad.GetType().GetField("_mediationProxy",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var original=field.GetValue(ad);var stub=new AdStub();var finish=ad.GetType().GetMethod("OnRewardedVideoFinished",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);stub.RewardedVideoFinished+=(ok)=>finish.Invoke(ad,new object[]{ok});field.SetValue(ad,stub);
 before=gm.premiumCurrency;r.WatchAd();Check(r.Busy&&!r.open.interactable&&!r.close.interactable,"Ad in progress blocks duplicate actions");stub.Finish(false);Check(!r.Busy&&gm.premiumCurrency==before&&r.open.interactable,"Cancelled ad does not grant");r.WatchAd();stub.Finish(true);stub.Finish(true);Check(gm.premiumCurrency==before+2*gm.dailyCherriesAmount&&StorybookDailyReward.ClaimedToday,"Completed ad grants exactly double once");Check(!r.Busy&&r.gameObject.activeSelf&&!CashMallPackage.single,"Double claim remains in reminder");field.SetValue(ad,original);
 yield return new WaitForEndOfFrame();Shot("double-claimed");r.Hide();yield return new WaitForSecondsRealtime(.5f);Check(!r.group.blocksRaycasts,"Hidden popup releases rays");StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
 }
}
