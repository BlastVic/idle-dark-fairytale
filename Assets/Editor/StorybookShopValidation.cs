using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Purchasing.Purchased;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Purchasing;

[InitializeOnLoad] public static class StorybookShopValidation
{
 public const string Dir="output/shop-ui/";static bool started;static double deadline;
 static StorybookShopValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SHOP_QA",false)){SessionState.SetBool("SHOP_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);started=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){File.Delete(Dir+"validate.request");EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");File.WriteAllText(Dir+"validation.txt","Shop actual Unity validation, isolated project, persistence disabled. No payment initiated.\n");SessionState.SetBool("SHOP_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);deadline=EditorApplication.timeSinceStartup+240;started=false;StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;}
 if(SessionState.GetBool("SHOP_QA",false)&&EditorApplication.isPlaying&&!started&&GameManager.single&&GameplayCanvas.single){started=true;deadline=EditorApplication.timeSinceStartup+240;GameManager.single.StartCoroutine(new ShopProbe().Run());}
 if(EditorApplication.isPlaying&&SessionState.GetBool("SHOP_QA",false)&&deadline>0&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog");EditorApplication.isPlaying=false;}}
 public static void Log(string s)=>File.AppendAllText(Dir+"validation.txt",s+"\n");
}
public class ShopProbe
{
 int failures,errors;StorybookShopUI ui;
 void Check(bool b,string s){StorybookShopValidation.Log((b?"PASS ":"FAIL ")+s);if(!b)failures++;}
 void Error(string m,string stack,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;StorybookShopValidation.Log("ERROR "+m+"\n"+stack);}}
 void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookShopValidation.Dir+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 void Pointer(Button b,string n){var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Check(p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,n);}
 IEnumerator Open(){Router.single.CashMallButton();float end=Time.realtimeSinceStartup+20;while(!CashMallPackage.single&&Time.realtimeSinceStartup<end)yield return null;yield return new WaitForSecondsRealtime(2);ui=CashMallPackage.single?CashMallPackage.single.view:null;Check(ui,"existing Addressable shop route opens new prefab");if(ui){ui.validationPrices=true;ui.Refresh();}}
 public IEnumerator Run(){Application.logMessageReceived+=Error;var gm=GameManager.single;var gc=GameplayCanvas.single;if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
 gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);gc.nextCashMallIsDailyCherries=false;for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
 gm.premiumCurrency=240;gm.playerLevel=20;if(gc.homeUI)gc.homeUI.subjectNumber="017";yield return Open();if(!ui){Finish();yield break;}
 Check(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"no missing scripts");
 int[] widths={941,768,720},heights={1672,1024,1440};string[] names={"9x16","3x4","1x2"};
 for(int i=0;i<3;i++){StorybookEquipmentValidation.Resolution(widths[i],heights[i]);yield return new WaitForSecondsRealtime(.5f);ui.GetComponent<StorybookSkillsLayout>().Apply();ui.scroll.verticalNormalizedPosition=1;yield return new WaitForEndOfFrame();Shot(names[i]+"-shop");foreach(var b in ui.navigation)Pointer(b,names[i]+" navigation "+b.name);Pointer(ui.back,names[i]+" back");Pointer(ui.buy80,names[i]+" buy80");ui.scroll.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();Pointer(ui.buy500,names[i]+" buy500 reachable");Shot(names[i]+"-shop-bottom");gm.premiumCurrency=740;ui.ShowReward(500);yield return new WaitForEndOfFrame();Shot(names[i]+"-reward");Check(!ui.buy80.interactable&&!ui.back.interactable,names[i]+" reward disables underlying actions");var corners=new Vector3[4];((RectTransform)ui.reward.transform.Find("RewardPaper")).GetWorldCorners(corners);Check(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),names[i]+" reward not cropped");ui.HideReward();Check(!ui.reward.activeSelf&&!ui.rewardGroup.blocksRaycasts,names[i]+" hidden reward releases rays");gm.premiumCurrency=240;}
 var layout=ui.GetComponent<StorybookSkillsLayout>();layout.SetValidationSafeArea(new Rect(0,60,Screen.width,Screen.height-110));yield return new WaitForEndOfFrame();Shot("1x2-safe-area");Pointer(ui.back,"safe-area back");Pointer(ui.navigation[2],"safe-area navigation");layout.SetValidationSafeArea(null);
 ui.SetBusy(true,"正在等待商店确认…");yield return null;yield return new WaitForEndOfFrame();Shot("purchase-pending");Check(!ui.buy80.interactable&&!ui.buy500.interactable&&!ui.back.interactable,"purchase in progress prevents duplicate clicks and navigation");Check(ui.buy80.GetComponentsInChildren<Graphic>().All(g=>g.material.HasProperty("_DisabledUI")&&g.material.GetFloat("_DisabledUI")>0),"entire disabled purchase button is grayscale");
 typeof(CashMallPackage).GetMethod("Failed",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(CashMallPackage.single,new object[]{new PurchasedProduct(PurchaserManager.cherries_consumable_80,ProductType.Consumable)});yield return null;Check(ui.buy80.interactable&&ui.buy500.interactable&&!ui.Busy,"failure restores purchasing");Check(gm.premiumCurrency==240,"failure does not credit currency");
 var feedback=ui.buy80.GetComponent<ShelterButtonFeedback>();var before=ui.buy80.transform.localScale;var ev=new PointerEventData(EventSystem.current);feedback.OnPointerDown(ev);Check(ui.buy80.transform.localScale.x<before.x,"button pressed state");feedback.OnPointerUp(ev);Check(ui.buy80.transform.localScale==before,"button restores after release");
 ui.validationPrices=false;ui.Refresh();Check(!string.IsNullOrWhiteSpace(ui.price80.text)&&ui.price80.text!="0"&&ui.price500.text!="0","editor shows metadata or nonzero fallback prices");ui.validationPrices=true;
 foreach(int amount in new[]{80,500}){if(!CashMallPackage.single)yield return Open();float balance=gm.premiumCurrency;string id=amount==80?PurchaserManager.cherries_consumable_80:PurchaserManager.cherries_consumable_500;typeof(CashMallPackage).GetMethod("Succeeded",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(CashMallPackage.single,new object[]{new PurchasedProduct(id,ProductType.Consumable),"Shop QA"});Check(gm.premiumCurrency==balance+amount,"confirmed "+amount+" credits actual existing balance");Check(ui.reward.activeSelf,"purchase opens reward feedback");Check(!GameObject.FindGameObjectWithTag("CherryChest"),"no old cherry chest spawned");yield return new WaitForSecondsRealtime(6);Check(gm.gameState==GameStateType.CAMP&&!CashMallPackage.single,"reward returns to shelter");}
 yield return Open();ui.navigation[1].onClick.Invoke();yield return new WaitForSecondsRealtime(5);Check(gm.gameState==GameStateType.SKILLS&&!CashMallPackage.single,"shop to skills navigation cleans up shop");yield return Open();ui.navigation[0].onClick.Invoke();yield return new WaitForSecondsRealtime(5);Check(gm.gameState==GameStateType.INVENTORY&&!CashMallPackage.single,"shop to equipment navigation");yield return Open();ui.navigation[3].onClick.Invoke();yield return new WaitForSecondsRealtime(6);Check(LevelController.Instance.AssetManager.mapPackageObj.activeSelf&&!CashMallPackage.single,"shop to map navigation");yield return Open();ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(3);Check(gm.gameState==GameStateType.CAMP,"back button preserved");yield return new WaitForEndOfFrame();Shot("shelter-new-currency");
 Check(errors==0,"no unexpected runtime errors");Finish();}
 void Finish(){Application.logMessageReceived-=Error;StorybookShopValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
