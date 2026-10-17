using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assets.Scripts.Game.Enum;

// Explicit integration review. Uses the existing no-save QA switch.
[InitializeOnLoad]
public static class RedHoodVictoryValidation {
    const string Out="output/red-hood-motion/victory/";
    static bool running;
    static double deadline;
    static RedHoodVictoryValidation() {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RED_VICTORY_QA",false)) {
                SessionState.SetBool("RED_VICTORY_QA",false);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;
            }
        };
    }
    static void Tick() {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(SessionState.GetBool("RED_VICTORY_QA",false) && deadline==0)deadline=EditorApplication.timeSinceStartup+150;
        if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Out+"flow.request")) {
            File.Delete(Out+"flow.request");File.WriteAllText(Out+"flow-validation.txt","");
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes.First(s=>s.enabled).path);
            SessionState.SetBool("RED_VICTORY_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
            deadline=EditorApplication.timeSinceStartup+150;EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying && SessionState.GetBool("RED_VICTORY_QA",false) && !running && GameManager.single && GameplayCanvas.single) {
            running=true;GameManager.single.StartCoroutine(Run());
        }
        if(SessionState.GetBool("RED_VICTORY_QA",false) && EditorApplication.timeSinceStartup>deadline) {Check(false,"timeout");EditorApplication.isPlaying=false;}
    }
    static void Check(bool ok,string text) {File.AppendAllText(Out+"flow-validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    static void Shot(string name) {var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run() {
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();
        while(intro && intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var gm=GameManager.single;var gc=GameplayCanvas.single;var router=Router.single;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        gc.dailyCherriesPop.SetActive(false);
        var daily=gc.GetComponent<StorybookDailyReward>();if(daily)daily.enabled=false;
        SkillsController.Instance.ResetSkills();
        gm.lastBattleKey="Black Forest Mushroom Test";router.LastBattle();
        yield return new WaitForSecondsRealtime(1.5f);
        var player=Player.single;
        player.StopAllCoroutines();WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();
        gm.EnemyCleanup();player.ab.isDying=false;
        player.skeletonAnimation.AnimationState.SetAnimation(0,"Attack2",false).TrackTime=.9f;
        router.newChest=new Chest{crystalsMin=0,crystalsMax=0};
        var routine=gm.StartCoroutine(router.GoToVictory());
        yield return new WaitForSeconds(.8f);
        Check(player.IsVictoryRelaxing && player.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name=="Victory","real route starts Victory and stops attacks");
        Check(!gc.victoryPanel.activeSelf,"results wait for relief motion");
        yield return new WaitForSeconds(1f);yield return new WaitForEndOfFrame();Shot("battle-closed-eyes");
        Check(player.skeletonAnimation.Skeleton.FindSlot("head_rest").A>.9f,"real route closes eyes during exhale");
        yield return new WaitForSeconds(1.25f);
        Check(player.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name=="VictoryIdle","real route holds lowered weapon in VictoryIdle");
        yield return new WaitForEndOfFrame();Shot("battle-relaxed-idle");
        yield return routine;
        Check(gc.victoryPanel.activeSelf && gm.gameState==GameStateType.VICTORY,"victory settlement still opens");
        yield return router.GoToCampFromBattle();yield return new WaitForSeconds(1);
        Check(gc.homeUI.gameObject.activeInHierarchy,"victory returns to camp");
        router.LastBattle();yield return new WaitForSeconds(1.5f);
        float readyDeadline=Time.realtimeSinceStartup+10;
        while(Time.realtimeSinceStartup<readyDeadline && (!Player.single || Player.single.isCampPlayer || Player.single.skeletonAnimation.AnimationState.GetCurrent(0)==null))yield return null;
        Check(!Player.single.IsVictoryRelaxing && Player.single.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name!="VictoryIdle","replay clears victory state");
        Player.single.StopAllCoroutines();gm.EnemyCleanup();WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();
        // Use the actual Player and battle camera at each required portrait ratio.
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}) {
            typeof(MonsterPreviewValidation).GetMethod("SetResolution",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{size.x,size.y});yield return new WaitForSecondsRealtime(.4f);
            Check(Screen.width==size.x && Screen.height==size.y,"render size "+size.x+"x"+size.y);
            var actor=Player.single.skeletonAnimation;actor.AnimationState.ClearTracks();actor.Skeleton.SetToSetupPose();
            actor.Update(0);actor.LateUpdate();
            var assets=LevelController.Instance.AssetManager;
            if(assets.battleStyle)assets.battleStyle.PlacePlayer(Player.single,assets.battleCampCamera.GetComponent<Camera>());
            var entry=actor.AnimationState.SetAnimation(0,"Victory",false);entry.TrackTime=2.9f;
            actor.timeScale=0;actor.Update(0);actor.LateUpdate();
            yield return new WaitForEndOfFrame();Shot("battle-rest-"+size.x+"x"+size.y);
        }
        Player.single.skeletonAnimation.timeScale=1;
        yield return router.GoToDefeat();
        Check(gc.defeatPanel.activeSelf,"defeat settlement unaffected");
        yield return router.GoToCampFromBattle();yield return new WaitForSeconds(1);
        Check(gc.homeUI.gameObject.activeInHierarchy,"defeat returns to camp");
        File.AppendAllText(Out+"flow-validation.txt","COMPLETE\n");EditorApplication.isPlaying=false;
    }
}
