using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit one-shot real death / resurrection review. Save writes are disabled.
[InitializeOnLoad]
public static class RedHoodDeathValidation {
    const string Out="output/red-hood-motion/death/";
    static bool running;
    static double deadline;
    static RedHoodDeathValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{
        if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RED_DEATH_QA",false)){
            SessionState.SetBool("RED_DEATH_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;
        }
    };}
    static void Check(bool ok,string text){File.AppendAllText(Out+"flow-validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("RED_DEATH_QA",false)&&deadline==0)deadline=EditorApplication.timeSinceStartup+150;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"flow.request")){
            File.Delete(Out+"flow.request");File.WriteAllText(Out+"flow-validation.txt","");
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes.First(s=>s.enabled).path);
            SessionState.SetBool("RED_DEATH_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
            deadline=EditorApplication.timeSinceStartup+150;EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&SessionState.GetBool("RED_DEATH_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}
        if(SessionState.GetBool("RED_DEATH_QA",false)&&EditorApplication.timeSinceStartup>deadline){Check(false,"timeout");EditorApplication.isPlaying=false;}
    }
    static IEnumerator Run(){
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var gm=GameManager.single;var gc=GameplayCanvas.single;var router=Router.single;
        var delayField=typeof(Router).GetField("defeatPresentationDelay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        float originalDelay=(float)delayField.GetValue(router);delayField.SetValue(router,6f);
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        var daily=gc.GetComponent<StorybookDailyReward>();if(daily)daily.enabled=false;
        SkillsController.Instance.ResetSkills();gm.lastBattleKey="Black Forest Mushroom Test";router.LastBattle();
        yield return new WaitForSecondsRealtime(2);
        var p=Player.single;p.StopAllCoroutines();WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();gm.EnemyCleanup();
        p.skeletonAnimation.AnimationState.SetAnimation(0,"Attack2",false).TrackTime=.7f;
        p.ab.Death();yield return new WaitForSeconds(1.49f);
        Check(p.ab.isDying&&p.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name=="Death1","real death overrides attack");
        Check(!gc.defeatPanel.activeSelf,"defeat panel waits for kneeling");
        yield return new WaitForSeconds(.34f);yield return new WaitForEndOfFrame();Shot("battle-kneeling");
        Check(p.skeletonAnimation.Skeleton.FindSlot("scythe").A<.01f,"weapon fades during kneeling");
        yield return new WaitForSeconds(1.1f);yield return new WaitForEndOfFrame();Shot("battle-death-final");
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            typeof(MonsterPreviewValidation).GetMethod("SetResolution",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{size.x,size.y});
            yield return new WaitForSecondsRealtime(.3f);
            var assets=LevelController.Instance.AssetManager;assets.battleStyle.PlacePlayer(p,assets.battleCampCamera.GetComponent<Camera>());
            var track=p.skeletonAnimation.AnimationState.SetAnimation(0,"Death1",false);track.TrackTime=2.88f;p.skeletonAnimation.timeScale=0;p.skeletonAnimation.Update(0);p.skeletonAnimation.LateUpdate();
            yield return new WaitForEndOfFrame();Shot("death-"+size.x+"x"+size.y);
        }
        p.skeletonAnimation.timeScale=1;
        float until=Time.realtimeSinceStartup+15;while((!gc.defeatPanel.activeSelf || gm.gameState!=Assets.Scripts.Game.Enum.GameStateType.DEFEAT)&&Time.realtimeSinceStartup<until)yield return null;
        Check(gc.defeatPanel.activeSelf,"real death reaches defeat panel");
        delayField.SetValue(router,originalDelay);
        yield return router.GoToCampFromBattle();yield return new WaitForSeconds(1);Check(gc.homeUI.gameObject.activeInHierarchy,"defeat returns to camp");
        router.LastBattle();yield return new WaitForSeconds(2);
        p=Player.single;p.StopAllCoroutines();WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();gm.EnemyCleanup();
        p.ab.isDying=true;p.ab.ressurrectionWaiting=true;p.isAttacking=false;p.SetAnimation("Death1",false,false);
        yield return new WaitForSeconds(3.33f);
        Check(p.ab.isDying&&p.ab.ressurrectionWaiting&&p.currentAnimation=="Revive","resurrection stays locked during rising");
        yield return new WaitForSeconds(2.75f);
        Check(!p.ab.isDying&&!p.ab.ressurrectionWaiting,"death completion preserves resurrection");
        Check(p.skeletonAnimation.Skeleton.FindSlot("scythe").A>.99f&&p.skeletonAnimation.Skeleton.FindSlot("lantern").A>.99f,"weapon returns after resurrection");
        File.AppendAllText(Out+"flow-validation.txt","COMPLETE\n");EditorApplication.isPlaying=false;
    }
}
