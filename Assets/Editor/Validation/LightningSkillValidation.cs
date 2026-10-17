using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class LightningSkillValidation
{
    const string Dir="output/lightning-skill/";
    static bool running,holdCombat,freezeAnimation,automatic;
    static string mode;
    static double deadline;
    static readonly int[] Width={720,768,720}, Height={1280,1024,1440};
    static LightningSkillValidation(){EditorApplication.update+=Tick;}
    static void Log(string message)=>File.AppendAllText(Dir+mode+".txt",message+"\n");
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var request=Dir+"command.request";
        if(File.Exists(request)){
            var cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{
                if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop first");
                if(cmd!="validate")throw new Exception("Unknown command");
                SessionState.SetString("LIGHTNING_SKILL_QA",cmd);
                SessionState.SetString("LIGHTNING_SKILL_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("LIGHTNING_SKILL_RETURN",false)){
            SessionState.SetBool("LIGHTNING_SKILL_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            var scene=SessionState.GetString("LIGHTNING_SKILL_SCENE","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);return;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetString("LIGHTNING_SKILL_QA","")!=""&&GameManager.single&&GameplayCanvas.single){
            mode=SessionState.GetString("LIGHTNING_SKILL_QA","");SessionState.SetString("LIGHTNING_SKILL_QA","");running=true;holdCombat=false;
            deadline=EditorApplication.timeSinceStartup+240;File.WriteAllText(Dir+mode+".txt","Actual Unity QA, save writes disabled\n");
            GameManager.single.StartCoroutine(Guard(Run()));
        }
        if(running&&holdCombat&&Player.single&&!Player.single.isCampPlayer&&!Player.single.ab.isDying){
            if(!automatic)Player.single.StopAllCoroutines();if(freezeAnimation)Player.single.skeletonAnimation.timeScale=0;
            Player.single.ab.currentStat.hpNow=100000;Player.single.ab.currentStat.dmg=100;Player.single.ab.currentStat.crit=0;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.ab&&!enemy.ab.isDying){
                enemy.StopAttacking();enemy.ab.currentStat.def=0;enemy.ab.currentStat.hpMax=100000;if(enemy.ab.currentStat.hpNow<1000)enemy.ab.currentStat.hpNow=100000;
            }
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static IEnumerator Guard(IEnumerator routine){while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){Log(result);running=false;holdCombat=false;SessionState.SetBool("LIGHTNING_SKILL_RETURN",true);EditorApplication.isPlaying=false;}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static void Request()=>Player.single.QueueSkill(SkillsTypeEnum.Lightning_Demi_God);
    static void Clear()=>typeof(Player).GetMethod("ResetCombatActions",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Player.single,null);
    static int Bolts()=>UnityEngine.Object.FindObjectsByType<CommonSpineBattleEffect>(FindObjectsSortMode.None).Count(x=>x.skeleton&&x.skeleton.name.Contains("zhangjiao"));
    static void Attack(int previousActions)
    {
        var p=Player.single;p.StopAllCoroutines();p.isAttacking=false;p.currentAnimation="";p.attackIndexer=previousActions;p.skeletonAnimation.timeScale=1;
        typeof(Player).GetMethod("Attack",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,null);
    }
    static IEnumerator Run()
    {
        var gm=GameManager.single;var gc=GameplayCanvas.single;var skills=SkillsController.Instance;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
        skills.ResetSkills();holdCombat=true;freezeAnimation=true;
        var wm=WaveManager.single;var fixture=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        fixture.levelKey="LightningSkillQA";fixture.levelName="Lightning QA";fixture.mapGraphicKey="CH1_Node05";
        fixture.waves=new[]{new Wave{enemies=new[]{"MobTallowkin","MobBramblet","MobMurmlet"}},new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.Add(fixture);
        LevelController.Instance.AssetManager.HideMapPackage();if(gc.homeUI)gc.homeUI.gameObject.SetActive(false);gc.ToggleButtonGrid(false);gc.ToggleFusionBarUI(false);
        wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        var p=Player.single;p.lightningVal=1.5f;skills.lightningDemiGodlevel=1;
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.4f);
            var assets=LevelController.Instance.AssetManager;var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(p,camera);
            int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))assets.battleStyle.PlaceEnemy(enemy,camera,slot++);
            yield return new WaitForSecondsRealtime(3);
            var targets=gm.GetLivingEnemies(false);Check(targets.Count==3,"three eligible fixture actors "+ratio);
            targets[0].currentStat.hpNow=40000;targets[1].currentStat.hpNow=40000;targets[2].currentStat.hpNow=80000;
            p.ab.currentStat.atkSpd=ratio==1?200:ratio==2?75:100;
            Clear();Request();freezeAnimation=false;Attack(0);
            Check(p.currentAnimation=="Skill3","casts Skill3 "+ratio);
            float expectedHp=40000-Mathf.Round(150+p.SkillScheduler.Compensation/2);
            var track=p.skeletonAnimation.AnimationState.GetCurrent(0);
            while(track.TrackTime<1.9f)yield return null;
            Check(targets[0].currentStat.hpNow==40000&&targets[1].currentStat.hpNow==40000&&Bolts()==0,"no bolt or damage before raised-scythe event "+ratio);
            yield return new WaitForEndOfFrame();Shot(ratio+"-raised-before");
            while(targets[0].currentStat.hpNow==40000)yield return null;
            Check(track.TrackTime>=2.08f,"damage follows Skill3 event at 2.08 clip seconds "+ratio);
            Check(targets[0].currentStat.hpNow==expectedHp&&targets[1].currentStat.hpNow==expectedHp&&targets[2].currentStat.hpNow==80000,"exact damage and below-half eligibility "+ratio);
            yield return new WaitForSecondsRealtime(.10f);Check(Bolts()==2,"two formal lightning effects "+ratio);
            yield return new WaitForEndOfFrame();Shot(ratio+"-lightning");
            p.AnimationOnHit("Skill3");Check(targets[0].currentStat.hpNow==expectedHp,"duplicate event does not strike twice "+ratio);
            while(p.currentAnimation=="Skill3")yield return null;
            Check(p.currentAnimation=="Idle1","cast completes to idle "+ratio);
            freezeAnimation=true;yield return new WaitForSecondsRealtime(2);
        }
        var alive=gm.GetLivingEnemies(false);
        // Queued cast must never interrupt another animation, even when the timer becomes ready.
        freezeAnimation=false;automatic=true;p.ab.currentStat.atkSpd=100;
        Clear();p.StopAllCoroutines();p.isAttacking=false;p.currentAnimation="";p.skeletonAnimation.timeScale=1;
        p.SetAnimation("Skill2",false,false,1);
        p.StartCoroutine("LightningLoop");
        yield return new WaitForSecondsRealtime(3.2f);
        Check(p.currentAnimation=="Skill2","ready lightning does not interrupt existing Skill2");
        float end=Time.realtimeSinceStartup+12;bool sawSkill=false;
        while(Time.realtimeSinceStartup<end){
            if(p.currentAnimation=="Skill3"){sawSkill=true;break;}
            yield return null;
        }
        Check(sawSkill,"real cooldown loop schedules Skill3 between attacks");
        automatic=false;p.StopAllCoroutines();
        // Death during windup cancels pending hit.
        float before=alive.Sum(t=>t.currentStat.hpNow);p.ab.isDying=true;p.SetAnimation("Death1",false,false);p.AnimationOnHit("Skill3");
        Check(alive.Sum(t=>t.currentStat.hpNow)==before,"death cancels queued lightning");
        p.ab.isDying=false;p.currentAnimation="";p.skeletonAnimation.AnimationState.ClearTracks();
        foreach(var target in alive)target.currentStat.hpNow=80000;
        Request();Attack(0);Check(p.currentAnimation!="Skill3","no cast without below-half target");
        // Snapshot revalidation: a healed enemy is not hit when the raised-scythe event arrives.
        alive[0].currentStat.hpNow=40000;Clear();Request();Attack(0);alive[0].currentStat.hpNow=80000;
        p.AnimationOnHit("Skill3");Check(alive[0].currentStat.hpNow==80000,"revalidates healed target at hit");
        Clear();p.isCampPlayer=true;p.AnimationOnHit("Skill3");Check(alive[0].currentStat.hpNow==80000,"camp ignores stale hit event");p.isCampPlayer=false;
        freezeAnimation=true;
        Finish("PASS Skill3 timing at 3 ratios and 3 speeds; VFX/damage synchronization, target filtering, duplicate event, completion, timer and cancellation");
    }
}
