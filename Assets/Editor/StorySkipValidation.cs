using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class StorySkipValidation {
 const string D="output/story-skip/";static bool running;
 static StorySkipValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SKIP_QA",false)){SessionState.SetBool("SKIP_QA",false);SessionState.SetBool("PROLOGUE_QA_FORCE",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){if(!running&&SessionState.GetBool("SKIP_QA",false)&&GameManager.single){running=true;GameManager.single.StartCoroutine(Run());}return;}if(!File.Exists(D+"validate.request"))return;File.Delete(D+"validate.request");File.WriteAllText(D+"validation.txt","");SessionState.SetBool("SKIP_QA",true);SessionState.SetBool("PROLOGUE_QA_FORCE",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);Size(720,1280);EditorApplication.isPlaying=true;}
 static void Size(int w,int h){typeof(CursedBookIntroValidation).GetMethod("Resolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});}
 static void Check(bool value,string name){File.AppendAllText(D+"validation.txt",(value?"PASS ":"FAIL ")+name+"\n");}
 static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(D+name+".png",t.EncodeToPNG());Object.Destroy(t);}
 static IEnumerator Ready(CursedBookIntro i){float limit=Time.realtimeSinceStartup+30;while(i&&!i.prologue.narration.isPlaying&&Time.realtimeSinceStartup<limit)yield return null;}
 static IEnumerator Run(){
 yield return null;var gm=GameManager.single;for(int n=0;n<20;n++)if(!gm.HasSeenThis("Tutorial-"+n))gm.thingsSeenPermanent.Add("Tutorial-"+n);
 var intro=Object.FindAnyObjectByType<CursedBookIntro>();yield return Ready(intro);yield return new WaitForSecondsRealtime(.5f);
 var ui=intro.GetComponent<StorySkipUI>();Check(ui&&ui.skip.gameObject.activeInHierarchy,"skip visible in prologue");
 Check(!ui.IsReplay&&((RectTransform)ui.skip.transform).anchorMin.y==0,"normal story skip at bottom right");
 ui.skip.onClick.Invoke();float time=intro.prologue.narration.time;yield return new WaitForSecondsRealtime(.5f);Check(intro.prologue.Paused&&Mathf.Abs(time-intro.prologue.narration.time)<.05f&&!intro.Finished,"confirmation pauses without finishing story");
 int[] w={720,768,720},h={1280,1024,1440};string[] ratio={"9x16","3x4","1x2"};
 for(int k=0;k<3;k++){
 Size(w[k],h[k]);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Shot("confirmation-"+ratio[k]);
 var corners=new Vector3[4];ui.panel.GetWorldCorners(corners);Check(corners.All(c=>c.x>=0&&c.y>=0&&c.x<=Screen.width&&c.y<=Screen.height),"confirmation safe bounds "+ratio[k]);
 ui.cancel.onClick.Invoke();yield return new WaitForEndOfFrame();Shot("skip-button-"+ratio[k]);ui.skip.onClick.Invoke();
 }
 time=intro.prologue.narration.time;
 ui.confirm.interactable=false;yield return null;yield return new WaitForEndOfFrame();Shot("confirmation-disabled");
 Check(ui.confirm.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>g.material.HasProperty("_DisabledUI")&&g.material.GetFloat("_DisabledUI")>0),"whole button disabled state includes label");
 ui.confirm.interactable=true;yield return null;yield return new WaitForEndOfFrame();
 Check(ui.confirm.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.material.HasProperty("_DisabledUI")||g.material.GetFloat("_DisabledUI")==0),"button colors restore after enabling");
 Size(720,1280);ui.cancel.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Check(intro.prologue.narration.isPlaying&&intro.prologue.narration.time>time,"cancel resumes narration");
 ui.skip.onClick.Invoke();ui.confirm.onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);Check(intro.Finished&&GameplayCanvas.single.homeUI.isActiveAndEnabled,"skip initial prologue enters shelter");
 SessionState.SetBool("PROLOGUE_QA_FORCE",false);
 gm.thingsSeenPermanent.Remove(CursedBookIntro.ChapterEndingSeenKey);var waves=WaveManager.single;waves.currentWaveset=waves.GetWavesetByKey("Mount Zion");waves.SetWaveseReachedByKey("Mount Zion",waves.currentWaveset.waves.Length);gm.SetGameState(GameStateType.VICTORY);GameplayCanvas.single.victoryPanel.SetActive(true);GameplayCanvas.single.GoToCampFromVictoryOrDefeat();
 float until=Time.realtimeSinceStartup+15;while(!Router.single.ActiveChapterEnding&&Time.realtimeSinceStartup<until)yield return null;intro=Router.single.ActiveChapterEnding;yield return Ready(intro);yield return new WaitForSecondsRealtime(.3f);ui=intro.GetComponent<StorySkipUI>();yield return new WaitForEndOfFrame();Shot("ending-skip-button");ui.skip.onClick.Invoke();ui.confirm.onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);Check(!Router.single.ActiveChapterEnding&&GameplayCanvas.single.homeUI.GetComponent<ShelterResponsiveLayout>().presentationControls.alpha>.99f,"chapter ending skip restores home controls");
 GameplayCanvas.single.homeUI.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(2);
 var map=Object.FindAnyObjectByType<StorybookMapUI>();Check(map,"map opens after skip");
 foreach(int index in Enumerable.Range(0,map.nodes.Length).Where(n=>map.nodes[n].IsStory)){
  map.SelectNode(index);map.Refresh();Check(map.IsUnlocked(index),"story replay remains unlocked "+index);map.EnterSelected();yield return new WaitForSecondsRealtime(2);intro=map.ActiveStory;Check(intro,"replay player opens "+index);if(!intro)continue;yield return Ready(intro);ui=intro.GetComponent<StorySkipUI>();
  for(int k=0;k<3;k++){Size(w[k],h[k]);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Shot("replay-"+index+"-"+ratio[k]);Check(ui.IsReplay&&((RectTransform)ui.skip.transform).anchorMin.y==1,"replay skip at top right "+ratio[k]);}
  ui.skip.onClick.Invoke();Check(intro.Finished&&!ui.IsConfirming,"replay skips immediately without confirmation");yield return new WaitForSecondsRealtime(.5f);Check(!map.ActiveStory&&map.controls.blocksRaycasts&&map.controls.interactable,"skipping replay returns to map "+index);
 }
 File.AppendAllText(D+"validation.txt","FINISHED\n");EditorApplication.isPlaying=false;
 }
}
