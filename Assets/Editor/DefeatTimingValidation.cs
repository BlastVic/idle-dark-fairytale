using Assets.Scripts.Game.Enum;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit local QA only; never rebuilds authored UI or writes player saves.
[InitializeOnLoad]
public static class DefeatTimingValidation
{
    const string Out="output/defeat-timing/";
    static bool running;
    static DefeatTimingValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("DEFEAT_TIMING_QA",false)){SessionState.SetBool("DEFEAT_TIMING_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("DEFEAT_TIMING_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("DEFEAT_TIMING_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
    static void Check(bool ok,string message){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Object.Destroy(t);}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        gc.LastBattleButtonClicked();
        yield return new WaitForSecondsRealtime(3);
        var player=Player.single;player.StopAllCoroutines();player.ab.ressurrectionWaiting=false;player.ab.isDying=false;
        IdleKnightHero.UI.SoundManager.Instance.PlayClip("PLAYER_DEATH",false,false);
        player.SetAnimation("Death1",false,false);player.ab.isDying=true;
        float start=Time.realtimeSinceStartup;
        Router.single.StartCoroutine(Router.single.GoToDefeat());
        yield return new WaitForSecondsRealtime(.5f);
        Check(!gc.defeatPanel.activeSelf,"Defeat panel remains hidden during death");
        yield return new WaitForEndOfFrame();Shot("death-before-panel");
        while(!gc.defeatPanel.activeSelf && Time.realtimeSinceStartup-start<15)yield return null;
        float elapsed=Time.realtimeSinceStartup-start;
        Check(gc.defeatPanel.activeSelf && elapsed>=2.7f,"Panel delayed until death presentation finishes: "+elapsed+" seconds");
        yield return new WaitForSecondsRealtime(.7f);
        Check(!Router.single.waitingForBattleCleanup && GameManager.single.gameState==GameStateType.DEFEAT,"Cleanup completes and defeat actions unlock");
        yield return new WaitForEndOfFrame();Shot("defeat-after-delay");
        File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
