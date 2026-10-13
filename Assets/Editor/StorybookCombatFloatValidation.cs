using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
[InitializeOnLoad] public static class StorybookCombatFloatValidation{
 const string Out="output/combat-float/";static bool running;static double started;
 static StorybookCombatFloatValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("COMBAT_FLOAT_QA",false)){SessionState.SetBool("COMBAT_FLOAT_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");SessionState.SetBool("COMBAT_FLOAT_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("COMBAT_FLOAT_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;started=EditorApplication.timeSinceStartup;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup-started>180){Check(false,"timeout");EditorApplication.isPlaying=false;}}
 static void Error(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)Check(false,message);}
 static void Check(bool ok,string s)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+s+"\n");
 static IEnumerator Run(){Application.logMessageReceived+=Error;var intro=Object.FindAnyObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();var ui=gc.homeUI.settingsUI;
 Router.single.LastBattle();yield return new WaitForSecondsRealtime(3);var am=LevelController.Instance.AssetManager;var camera=am.battleCampCamera.GetComponent<Camera>();
 foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
 for(int i=0;i<5;i++){var pos=camera.ViewportToWorldPoint(new Vector3(.2f+i*.15f,.48f+(i%2)*.10f,10));am.SpawnPopUpText(pos,i==1,i==2?128:36,i==2,i==3,i==4);}
 am.SpawnNotifierText(camera.ViewportToWorldPoint(new Vector3(.5f,.7f,10)));yield return new WaitForSecondsRealtime(.18f);var floats=Object.FindObjectsByType<StorybookCombatFloat>(FindObjectsSortMode.None);Check(floats.Length>=6,"New prefabs spawned through combat API "+size);foreach(var f in floats){Check(!f.group.blocksRaycasts&&!f.GetComponent<GraphicRaycaster>(),"Float does not intercept input");var corners=new Vector3[4];f.card.GetWorldCorners(corners);Check(corners[0].x>=-1&&corners[2].x<=Screen.width+1,"Float remains within horizontal viewport");}yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"battle-"+size.x+"x"+size.y+".png",image.EncodeToPNG());Object.Destroy(image);yield return new WaitForSecondsRealtime(1.5f);}
 yield return Router.single.GoToCampFromBattle();yield return new WaitForSecondsRealtime(1.5f);Check(Object.FindObjectsByType<StorybookCombatFloat>(FindObjectsSortMode.None).Length==0,"Floats expire without lingering objects");StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");Application.logMessageReceived-=Error;EditorApplication.isPlaying=false;}
}
