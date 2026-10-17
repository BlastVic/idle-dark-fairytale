using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StoryMapNodeValidation
{
    const string Dir="output/story-map/";
    static int stage,ratio,run,lastPage,errors;static double due,deadline;static string lastBattle;static Vector2 mapPosition;static bool captured;static bool captureOnly;
    static int[] W={720,768,720},H={1280,1024,1440};static string[] R={"9x16","3x4","1x2"};
    static StoryMapNodeValidation(){captureOnly=SessionState.GetBool("STORY_NODE_CAPTURE",false);stage=SessionState.GetInt("STORY_NODE_QA",0);deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(stage>0&&(t==LogType.Error||t==LogType.Exception)){errors++;Log("ERROR "+m);}};}
    static void Log(string s)=>File.AppendAllText(Dir+"validation.txt",s+"\n");
    static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
    static void Next(int s,double wait=.5){stage=s;SessionState.SetInt("STORY_NODE_QA",s);due=EditorApplication.timeSinceStartup+wait;}
    static void Size(){typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{W[ratio],H[ratio]});}
    static void Finish(string s){Log(s);Next(0);EditorApplication.isPlaying=false;SessionState.SetBool("STORY_NODE_CLEANUP",true);}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try {
            if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("STORY_NODE_CLEANUP",false)){SessionState.SetBool("STORY_NODE_CLEANUP",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
            if(stage==0){
                if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"validate.request"))return;
                captureOnly=File.ReadAllText(Dir+"validate.request").Trim()=="capture";SessionState.SetBool("STORY_NODE_CAPTURE",captureOnly);File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Actual saved map story node integration; narration advanced in QA after beginning playback; no player save writes\n");
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("PROLOGUE_QA_FORCE",false);
                ratio=run=errors=0;Next(1,2);deadline=EditorApplication.timeSinceStartup+600;EditorApplication.isPlaying=true;return;
            }
            if(!EditorApplication.isPlaying)return;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout stage "+stage);
            if(EditorApplication.timeSinceStartup<due||!GameManager.single||!GameplayCanvas.single||!WaveManager.single)return;
            var gm=GameManager.single;var gc=GameplayCanvas.single;var assets=LevelController.Instance.AssetManager;
            if(stage==5){
                if(gm.gameState!=GameStateType.BATTLE||WaveManager.single.currentWaveset==null)return;
                Check(WaveManager.single.currentWaveset.levelKey==StorybookMapBuilder.Keys[0],"First battle remains accessible after replay");
                gc.battleHud.backButton.onClick.Invoke();Next(6,3);return;
            }
            if(stage==6){if(gm.gameState!=GameStateType.CAMP)return;Check(gc.homeUI.gameObject.activeInHierarchy,"Battle return flow preserved");Finish(errors==0?(captureOnly?"PASS final three-ratio map captures and battle regression":"PASS all story-node checks, three ratios, two replays each"):"FAIL unexpected errors="+errors);return;}
            if(stage==1){
                if(gm.gameState!=GameStateType.CAMP||!gc.homeUI)return;
                var intro=UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;
                gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
                for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
                if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
                Size();gc.homeUI.navigationButtons[3].onClick.Invoke();Next(2,2);return;
            }
            var ui=assets.mapPackageObj?assets.mapPackageObj.GetComponentInChildren<StorybookMapUI>():null;
            if(!ui||gm.gameState!=GameStateType.MAP)return;
            if(stage==2){
                Check(ui.nodes.Length==6&&ui.nodes[0].IsStory,"Saved story node precedes five battles");
                Check(ui.nodes.Skip(1).Select(n=>n.levelKey).SequenceEqual(StorybookMapBuilder.Keys),"Battle order and level keys unchanged");
                ui.SelectNode(0);ui.LocateSelection();ui.Refresh();lastBattle=gm.lastBattleKey;
                Check(ui.nodes[1].label.text.StartsWith("1-1"),"Battle numbering unchanged");
                Check(ui.nodes[0].customSeal!=ui.availableSeal,"Story uses unique book icon");
                Check(ui.storyDetailGlyph.activeSelf&&!ui.battleDetailGlyph.activeSelf,"Story card uses book instead of swords");
                Check(ui.enterCaption.text=="播放剧情"&&ui.enterButton.interactable,"Replay entry available regardless of seen flag");
                Next(3,1);return;
            }
            if(stage==3){
                ScreenCapture.CaptureScreenshot(Dir+R[ratio]+"-map.png");Next(captureOnly?12:10,.6);return;
            }
            if(stage==12){run=1;Next(11);return;}
            if(stage==10){
                mapPosition=ui.scroll.content.anchoredPosition;
                ui.enterButton.onClick.Invoke();ui.EnterSelected();
                Check(ui.IsNavigating&&ui.ActiveStory,"Story starts once; repeat click blocked");lastPage=-1;captured=false;Next(4,.5);return;
            }
            if(stage==4){
                var intro=ui.ActiveStory;
                if(intro){
                    CheckQuiet(!gc.homeUI.gameObject.activeInHierarchy,"Home appeared during replay");
                    CheckQuiet(intro.HomeTexture==null,"Replay preloaded home");
                    var p=intro.prologue;
                    if(p.Reading&&p.narration.time>.3f){
                        if(lastPage!=p.PageIndex){lastPage=p.PageIndex;Log("PASS "+R[ratio]+" run="+run+" page="+(lastPage+1));}
                        if(!captured){
                            var corners=new Vector3[4];intro.homePage.rectTransform.GetWorldCorners(corners);
                            float fraction=(corners[1].y-corners[0].y)/Screen.height;
                            Log("FRAME "+R[ratio]+" fraction="+fraction+" screen="+Screen.width+"x"+Screen.height+" root="+((RectTransform)intro.transform).rect+" scale="+intro.transform.lossyScale+" core="+intro.cameraRig.parent.localScale);
                            ScreenCapture.CaptureScreenshot(Dir+R[ratio]+"-story.png");captured=true;}
                        if(p.PageIndex==p.pages.Length-1)CheckQuiet(p.TurnsCompleted==p.pages.Length-1,"Wrong inter-page turn count");
                        p.narration.Stop();
                    }
                    return;
                }
                Check(!ui.IsNavigating&&ui.controls.interactable&&ui.controls.blocksRaycasts&&ui.controls.alpha==1,"Map input restored");
                Check(gm.lastBattleKey==lastBattle,"Story does not replace last battle key");
                Check(Vector2.Distance(ui.scroll.content.anchoredPosition,mapPosition)<1,"Map scroll restored");
                Check(gm.HasSeenThis(CursedBookPrologue.SeenKey),"Completion recorded; replay still available");
                Check(!gc.homeUI.gameObject.activeInHierarchy&&GameObject.FindGameObjectsWithTag("MapGraphics").Length==0,"Returns directly to map without home or combat");
                ScreenCapture.CaptureScreenshot(Dir+R[ratio]+"-returned.png");
                Next(11,.6);return;
            }
            if(stage==11){
                if(run==0){run++;Next(3);return;}
                run=0;ratio++;if(ratio<3){Size();Next(2,1);return;}
                ui.SelectNode(1);ui.enterButton.onClick.Invoke();Next(5,3);return;
            }
        }catch(Exception e){Finish("FAIL "+e);}
    }
    static void CheckQuiet(bool b,string message){if(!b)throw new Exception(message);}
}
