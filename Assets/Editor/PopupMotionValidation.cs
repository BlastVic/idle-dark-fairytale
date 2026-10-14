using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Scripts.Game.Enum;
[InitializeOnLoad] public static class PopupMotionValidation
{
 const string Out="output/popup-motion/";static bool running;static double started;
 static PopupMotionValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("POPUP_MOTION_QA",false)){SessionState.SetBool("POPUP_MOTION_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("POPUP_MOTION_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("POPUP_MOTION_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;started=EditorApplication.timeSinceStartup;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup-started>160){Check(false,"timeout");EditorApplication.isPlaying=false;}}
 static void Check(bool b,string msg)=>File.AppendAllText(Out+"validation.txt",(b?"PASS ":"FAIL ")+msg+"\n");
 static void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 static void Hit(Button b,string name){var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.position)},hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,name);}
 static StorybookDailyReward Fresh(StorybookDailyReminder r){var old=GameplayCanvas.single.GetComponent<StorybookDailyReward>();UnityEngine.Object.DestroyImmediate(old);var c=GameplayCanvas.single.gameObject.AddComponent<StorybookDailyReward>();c.reminder=r;return c;}


 static IEnumerator Exercise(GameObject popup,string label){
 StorybookPopupMotion.Hide(popup,true);StorybookPopupMotion.Show(popup);var fx=popup.GetComponent<StorybookPopupMotion>();
 Check(fx&&fx.content.alpha==0&&fx.input.blocksRaycasts,label+" starts shaded stage with window hidden");
 yield return new WaitForSecondsRealtime(.08f);Check(fx.content.alpha==0,label+" shade precedes panel");
 yield return new WaitForSecondsRealtime(.35f);Check(fx.content.alpha==1&&fx.input.interactable&&fx.motion.anchoredPosition==Vector2.zero,label+" opens at authored position");
 StorybookPopupMotion.Hide(popup);yield return new WaitForSecondsRealtime(.1f);Check(popup.activeSelf&&!fx.input.interactable&&fx.input.blocksRaycasts,label+" closing blocks click-through");
 StorybookPopupMotion.Show(popup);yield return new WaitForSecondsRealtime(.4f);Check(popup.activeSelf&&fx.content.alpha==1,label+" reopen interrupts close cleanly");
 StorybookPopupMotion.Hide(popup);yield return new WaitForSecondsRealtime(.4f);Check(!popup.activeSelf&&!fx.input.blocksRaycasts,label+" closed releases raycasts");
 }
 static IEnumerator Run(){
 var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
 var gc=GameplayCanvas.single;var gm=GameManager.single;var dev=UnityEngine.Object.FindFirstObjectByType<DeveloperOverlayUI>();if(dev)dev.gameObject.SetActive(false);TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();yield return new WaitForSecondsRealtime(.4f);
 var daily=gc.GetComponent<StorybookDailyReward>();daily.enabled=false;StorybookPopupMotion.Hide(daily.reminder.gameObject,true);
 yield return Exercise(gc.homeUI.modal,"Shelter details");
 gm.playerLevel=1;gc.homeUI.OpenFunction(2);
 foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
 StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);StorybookPopupMotion.Hide(gc.homeUI.modal,true);StorybookPopupMotion.Show(gc.homeUI.modal);yield return new WaitForSecondsRealtime(.06f);yield return new WaitForEndOfFrame();Shot("shade-"+size.x+"x"+size.y);yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();Shot("enter-"+size.x+"x"+size.y);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("open-"+size.x+"x"+size.y);
 }
 gc.homeUI.CloseModal();yield return new WaitForSecondsRealtime(.4f);
 daily.reminder.Show();yield return new WaitForSecondsRealtime(.4f);yield return Exercise(daily.reminder.gameObject,"Daily supply");
 yield return Router.single.GoToInventory();var equipment=gc.inventoryPanel.GetComponent<StorybookEquipmentUI>();yield return Exercise(equipment.modal,"Decompose confirmation");yield return Exercise(equipment.options,"Sort/filter options");
 foreach(var path in new[]{"Assets/Prefab/Addressable Prefabs/CashMallPackage.prefab","Assets/Prefab/Addressable Prefabs/RebirthPackage.prefab","Assets/DarkFairytale/UI/Developer/DeveloperOverlay.prefab","Assets/DarkFairytale/UI/Tutorial/StorybookTutorial.prefab"}){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var fx=asset.GetComponentInChildren<StorybookPopupMotion>(true);Check(fx&&fx.motion&&fx.content&&fx.input&&(fx.dimmers.Length>0||asset.name=="DeveloperOverlay"),"Persistent motion bindings "+asset.name);}
 Time.timeScale=0;yield return Exercise(equipment.modal,"Paused-time animation");Time.timeScale=1;
 StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
 }
}
