using System.Collections;
using System.IO;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VictoryEndingButtonValidation
{
 const string Out="output/victory-ending/";static bool running;
 static VictoryEndingButtonValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("ENDING_BUTTON_QA",false)){SessionState.SetBool("ENDING_BUTTON_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("ENDING_BUTTON_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&!running&&SessionState.GetBool("ENDING_BUTTON_QA",false)&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
 static void Check(bool ok,string s)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+s+"\n");
 static IEnumerator Run(){
 yield return new WaitForSecondsRealtime(2);var intro=Object.FindAnyObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)intro.SkipStory();yield return new WaitForSecondsRealtime(2);
 var gm=GameManager.single;var gc=GameplayCanvas.single;var router=Router.single;var wm=WaveManager.single;TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
 gm.thingsSeenPermanent.Remove(CursedBookIntro.ChapterEndingSeenKey);wm.currentWaveset=wm.GetWavesetByKey("Mount Zion");wm.SetWaveseReachedByKey("Mount Zion",wm.currentWaveset.waves.Length);router.waitingForBattleCleanup=true;router.newChest=new Chest{chestGrade=ChestGrade.NORMAL,crystalsMin=10,crystalsMax=20};gc.ToggleCampUi(false);gc.victoryPanel.SetActive(true);
 var ui=gc.victoryPanel.GetComponent<StorybookBattleResultUI>();ui.Refresh();Check(ui.watchEnding.gameObject.activeInHierarchy&&!ui.standardActions.activeSelf&&!ui.nextActions.activeSelf&&!ui.watchEnding.interactable,"ending action selected before cleanup without flashing normal actions");
 gm.SetGameState(GameStateType.VICTORY);router.waitingForBattleCleanup=false;ui.Refresh();ui.replay.onClick.Invoke();Check(!ui.IsBusy,"hidden replay cannot bypass chapter ending");
 foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
 typeof(StorySkipValidation).GetMethod("Size",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{size.x,size.y});yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+".png",t.EncodeToPNG());Object.Destroy(t);
 var c=new Vector3[4];((RectTransform)ui.watchEnding.transform).GetWorldCorners(c);Check(c[0].x>=0&&c[0].y>=0&&c[2].x<=Screen.width&&c[2].y<=Screen.height,"button visible "+size);
 }
 gm.thingsSeenPermanent.Add(CursedBookIntro.ChapterEndingSeenKey);ui.Refresh();Check(!ui.watchEnding.gameObject.activeSelf&&ui.standardActions.activeSelf,"watched story restores normal actions");gm.thingsSeenPermanent.Remove(CursedBookIntro.ChapterEndingSeenKey);
 var final=wm.currentWaveset;wm.currentWaveset=wm.GetWavesetByKey("Khorasan Ruins II");ui.Refresh();Check(!ui.watchEnding.gameObject.activeSelf,"other nodes unchanged");wm.currentWaveset=final;ui.Refresh();
 ui.watchEnding.onClick.Invoke();yield return new WaitForSecondsRealtime(3);Check(router.ActiveChapterEnding&&!gc.victoryPanel.activeSelf,"watch button enters chapter ending");if(router.ActiveChapterEnding)router.ActiveChapterEnding.SkipStory();yield return new WaitForSecondsRealtime(1);Check(gm.HasSeenThis(CursedBookIntro.ChapterEndingSeenKey)&&!router.ActiveChapterEnding,"ending completes to shelter and persists seen flag");
 File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
 }
}
