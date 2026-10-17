using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StorybookBossHUDValidation
{
    const string Out="output/boss-hud/";static bool running;static double start;
    static StorybookBossHUDValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("BOSS_HUD_QA",false)){SessionState.SetBool("BOSS_HUD_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");SessionState.SetBool("BOSS_HUD_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("BOSS_HUD_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;start=EditorApplication.timeSinceStartup;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup-start>150){Check(false,"timeout");EditorApplication.isPlaying=false;}}
    static void Check(bool ok,string text){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static IEnumerator Run(){
        yield return new WaitForSecondsRealtime(2);var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)intro.SkipStory();yield return new WaitForSecondsRealtime(2);
        var gm=GameManager.single;var gc=GameplayCanvas.single;TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        gm.lastBattleKey="Black Forest Mushroom Test";Router.single.LastBattle();yield return new WaitForSecondsRealtime(2);
        Player.single.StopAllCoroutines();Player.single.ab.trainingTarget=true;WaveManager.single.StopAllCoroutines();gm.EnemyCleanup();yield return null;
        var boss=gc.battleHud.bossHud;Check(boss&&boss.header,"authored nested Boss HUD and header binding");
        foreach(string key in new[]{"MobRivenfang","MobStillmere"}){
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
                StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.2f);
                var wave=new Wave{enemies=new[]{key}};WaveManager.single.currentWaveset=new Waveset{waves=new[]{wave},levelName="Boss HUD QA"};WaveManager.single.waveNumber=0;
                WaveManager.single.CallNextWave();Check(boss.Presenting&&boss.fill.fillAmount==0,"boss wave starts charge immediately "+key+size);
                yield return new WaitForSecondsRealtime(.45f);Check(boss.fill.fillAmount>0&&boss.fill.fillAmount<1,"left-to-right charge underway "+key+size);Check(!boss.Target&&GameObject.FindGameObjectsWithTag("Enemy").Length==0,"no boss actor during charge "+key+size);
                yield return new WaitForEndOfFrame();Shot(key+"-charge-"+size.x+"x"+size.y);
                float deadline=Time.unscaledTime+5;while(!boss.Target&&Time.unscaledTime<deadline)yield return null;
                Check(boss.Target,"spawned actor bound "+key);Check(boss.EntranceFinished,"boss appears only after full entrance animation "+key);if(!boss.Target)break;
                var actor=boss.Target;actor.trainingTarget=true;actor.GetComponent<Enemy>().StopAttacking();actor.currentStat.hpMax=actor.currentStat.hpNow=1200;
                Check(!actor.enemyLifebar.GetComponent<Canvas>().enabled,"body health bar disabled in prefab "+key);
                yield return new WaitForSecondsRealtime(1.6f);Check(Mathf.Abs(boss.fill.fillAmount-1)<.01f,"charge reaches full "+key);
                var item=new Item{itemGrade=ItemGrade.UI_ONLY,m_ItemFinalTitle="获得水晶 × 25"};gc.itemToast.Present(item);yield return new WaitForSecondsRealtime(.3f);
                boss.Place();gc.itemToast.Place();var headerCorners=new Vector3[4];boss.header.GetWorldCorners(headerCorners);var corners=new Vector3[4];boss.panel.GetWorldCorners(corners);
                Check(corners[1].y<headerCorners[0].y,"below wave header "+size);
                var toastCorners=new Vector3[4];gc.itemToast.card.GetWorldCorners(toastCorners);Check(toastCorners[1].y<corners[0].y,"above item toast "+size);
                Check(corners[0].x>=0&&corners[2].x<=Screen.width,"within screen width "+size);
                yield return new WaitForEndOfFrame();Shot(key+"-full-"+size.x+"x"+size.y);
                actor.currentStat.hpNow=600;yield return null;Check(Mathf.Abs(boss.fill.fillAmount-.5f)<.01f,"live damage updates health "+key);
                yield return new WaitForEndOfFrame();Shot(key+"-damaged-"+size.x+"x"+size.y);
                actor.isDying=true;yield return null;Check(!boss.Presenting,"death clears banner "+key);gc.itemToast.gameObject.SetActive(false);UnityEngine.Object.Destroy(actor.gameObject);yield return null;
            }
        }
        boss.BeginWave(new Wave{enemies=new[]{"MobRivenfang"}});Time.timeScale=0;yield return new WaitForSecondsRealtime(1.5f);Check(boss.fill.fillAmount==1,"charge uses unscaled time");Time.timeScale=1;
        boss.BeginWave(new Wave{enemies=new[]{"MobTallowkin"}});Check(!boss.Presenting,"normal wave hides banner");
        boss.BeginWave(new Wave{enemies=new[]{"MobRivenfang"}});gc.ToggleBattleUi(false);Check(!boss.Presenting,"leaving battle clears banner");
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
