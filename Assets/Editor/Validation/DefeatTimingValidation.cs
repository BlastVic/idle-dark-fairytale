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
        yield return new WaitForSecondsRealtime(2);var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);if(intro&&!intro.Finished)intro.SkipStory();yield return new WaitForSecondsRealtime(2);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();}
        GameManager.single.lastBattleKey="Black Forest Mushroom Test";Router.single.LastBattle();
        yield return new WaitForSecondsRealtime(3);
        var player=Player.single;WaveManager.single.StopAllCoroutines();GameManager.single.EnemyCleanup();player.StopAllCoroutines();player.ab.ressurrectionWaiting=false;player.ab.isDying=false;
        IdleKnightHero.UI.SoundManager.Instance.PlayClip("PLAYER_DEATH",false,false);
        player.SetAnimation("Death1",false,false);player.ab.isDying=true;
        var death=player.skeletonAnimation.AnimationState.GetCurrent(0);
        var fade=TransitionCanvas.single.GetComponent<StorybookDeathFade>();
        float start=Time.realtimeSinceStartup;
        Router.single.StartCoroutine(Router.single.GoToDefeat());
        yield return new WaitForSecondsRealtime(.5f);
        Check(!gc.defeatPanel.activeSelf,"Defeat panel remains hidden during death");
        yield return new WaitForEndOfFrame();Shot("death-before-panel");
        while(!death.IsComplete&&Time.realtimeSinceStartup-start<10){Check(!fade.Presenting,"no fade before animation ends");yield return null;}
        float animationEnd=Time.realtimeSinceStartup;
        while(!fade.Presenting&&Time.realtimeSinceStartup-animationEnd<2)yield return null;
        float fadeStart=Time.realtimeSinceStartup;
        Check(fade.Presenting&&fadeStart-animationEnd<.15f,"fade begins immediately after death, gap="+(fadeStart-animationEnd));
        Check(fade.stillDuration==0,"no still-frame hold before colour drain");
        yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Shot("immediate-colour-drain");
        Check(fade.finalPage.material.GetFloat("_Progress")>0,"colour drain already advancing");
        while(!gc.defeatPanel.activeSelf && Time.realtimeSinceStartup-start<15)yield return null;
        float elapsed=Time.realtimeSinceStartup-start;
        Check(gc.defeatPanel.activeSelf && elapsed>fadeStart-start,"Panel follows death and fade: "+elapsed+" seconds");
        yield return new WaitForSecondsRealtime(1.2f);
        Check(!Router.single.waitingForBattleCleanup && GameManager.single.gameState==GameStateType.DEFEAT,"Cleanup completes and defeat actions unlock");
        yield return new WaitForEndOfFrame();Shot("defeat-after-delay");
        File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
