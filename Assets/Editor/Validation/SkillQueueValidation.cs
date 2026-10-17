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
public static class SkillQueueValidation
{
    const string Dir="output/skill-queue/";
    static bool running,holdCombat,freezeAnimation,automatic;
    static string mode;
    static double deadline;
    static readonly int[] Width={720,768,720}, Height={1280,1024,1440};
    static SkillQueueValidation(){EditorApplication.update+=Tick;}
    static void Log(string message)=>File.AppendAllText(Dir+mode+".txt",message+"\n");
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var request=Dir+"command.request";
        if(File.Exists(request)){
            var cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{
                if(cmd=="apply"){QueuedSkillAuthoring.Apply();File.WriteAllText(Dir+"apply.txt","PASS");return;}
                if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop first");
                if(cmd!="validate")throw new Exception("Unknown command");
                SessionState.SetString("SKILL_QUEUE_QA",cmd);
                SessionState.SetString("SKILL_QUEUE_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("SKILL_QUEUE_RETURN",false)){
            SessionState.SetBool("SKILL_QUEUE_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            var scene=SessionState.GetString("SKILL_QUEUE_SCENE","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);return;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetString("SKILL_QUEUE_QA","")!=""&&GameManager.single&&GameplayCanvas.single){
            mode=SessionState.GetString("SKILL_QUEUE_QA","");SessionState.SetString("SKILL_QUEUE_QA","");running=true;holdCombat=false;
            deadline=EditorApplication.timeSinceStartup+300;File.WriteAllText(Dir+mode+".txt","Actual Unity QA, save writes disabled\n");
            GameManager.single.StartCoroutine(Guard(Run()));
        }
        if(running&&holdCombat&&Player.single&&!Player.single.isCampPlayer&&!Player.single.ab.isDying){
            Player.single.ab.currentStat.hpNow=100000;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.ab&&!enemy.ab.isDying){
                enemy.StopAttacking();enemy.ab.currentStat.def=0;enemy.ab.currentStat.hpMax=100000;
            }
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static IEnumerator Guard(IEnumerator routine){while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){Log(result);running=false;holdCombat=false;SessionState.SetBool("SKILL_QUEUE_RETURN",true);EditorApplication.isPlaying=false;}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static readonly SkillsTypeEnum[] Order={SkillsTypeEnum.Sinister_Stampede,SkillsTypeEnum.GhostWolfPack,SkillsTypeEnum.Lightning_Demi_God};
    static void Reset()
    {
        var p=Player.single;p.StopAllCoroutines();
        typeof(Player).GetMethod("ResetCombatActions",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,null);
        p.skeletonAnimation.AnimationState.ClearTracks();p.skeletonAnimation.Skeleton.SetToSetupPose();
        p.skeletonAnimation.timeScale=1;p.currentAnimation="";p.ab.isDying=false;
        p.ab.currentStat.dmg=100;p.ab.currentStat.crit=0;p.ab.currentStat.atkSpd=100;p.sinisterStampedeChance=0;p.bossKillerChance=0;
        p.lightningVal=1.5f;p.attackIndexer=0;
        foreach(var t in GameManager.single.GetLivingEnemies(false))t.currentStat.hpNow=40000;
    }
    static void Attack()=>typeof(Player).GetMethod("Attack",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Player.single,null);
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
        skills.ResetSkills();skills.sinisterStampedeLevel=1;skills.ghostWolfPackLevel=1;skills.lightningDemiGodlevel=1;skills.devilOnMyShoulderLevel=1;gm.skillPoints=20;
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.skillsPanel.GetComponent<StorybookSkillsUI>();
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.5f);
            foreach(var key in Order){
                ui.Refresh();ui.layout.Apply();ui.treeScroll.verticalNormalizedPosition=key==Order[0]?1:key==Order[1]?.6f:.2f;ui.treeScroll.velocity=Vector2.zero;
                yield return new WaitForSecondsRealtime(.4f);ui.Select(ui.nodes.Single(n=>n.key==key));yield return new WaitForSecondsRealtime(.7f);
                Check(ui.description.text.Contains("额外补偿")&&ui.drawerGroup.blocksRaycasts,"persisted compensation description "+ratio+" "+key);
                yield return new WaitForEndOfFrame();Shot(ratio+"-ui-"+key);
                ui.CloseDrawer();yield return new WaitForSecondsRealtime(.3f);
            }
        }
        Router.single.CallCampFromSkills();yield return new WaitForSecondsRealtime(2);holdCombat=true;
        var wm=WaveManager.single;var fixture=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        fixture.levelKey="SkillQueueQA";fixture.levelName="Skill Queue QA";fixture.mapGraphicKey="CH1_Node05";
        fixture.waves=new[]{new Wave{enemies=new[]{"MobTallowkin","MobBramblet","MobMurmlet"}},new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.Add(fixture);
        LevelController.Instance.AssetManager.HideMapPackage();if(gc.homeUI)gc.homeUI.gameObject.SetActive(false);gc.ToggleButtonGrid(false);gc.ToggleFusionBarUI(false);
        wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        var p=Player.single;
        for(int ratio=0;ratio<3;ratio++){
            Reset();StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.5f);
            var assets=LevelController.Instance.AssetManager;var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(p,camera);
            int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))assets.battleStyle.PlaceEnemy(enemy,camera,slot++);
            yield return new WaitForSecondsRealtime(3);Reset();
            // Queue all three while a normal attack owns the animation track.
            p.SetAnimation("Attack1",false,false,1);var normal=p.skeletonAnimation.AnimationState.GetCurrent(0);
            foreach(var key in Order)Check(p.QueueSkill(key),"enqueue "+key+" "+ratio);
            Check(!p.QueueSkill(Order[0])&&p.SkillScheduler.PendingCount==3,"deduplicate pending skill "+ratio);
            p.ForceAnimation("Skill3",false);p.AnimationComplete(0,"Attack1");Attack();
            Check(p.currentAnimation=="Attack1"&&p.isAttacking,"normal attack cannot be overwritten or completed early "+ratio);
            foreach(var key in Order){
                float deadline=Time.realtimeSinceStartup+10;
                while(p.SkillScheduler.Active!=key&&Time.realtimeSinceStartup<deadline)yield return null;
                Check(p.SkillScheduler.Active==key,"FIFO starts "+key+" "+ratio);
                var track=p.skeletonAnimation.AnimationState.GetCurrent(0);string name=track.Animation.Name;
                Check(!p.QueueSkill(key),"active cast cannot enqueue itself "+key);
                float totalHp=gm.GetLivingEnemies(false).Sum(t=>t.currentStat.hpNow);
                p.AnimationOnHit(name=="Skill3"?"Skill2":"Skill3");p.AnimationComplete(0,name);Attack();
                Check(p.currentAnimation==name&&p.isAttacking&&Mathf.Approximately(totalHp,gm.GetLivingEnemies(false).Sum(t=>t.currentStat.hpNow)),"stale events cannot interrupt/hit "+name);
                float bonus=p.SkillScheduler.Compensation;Check(bonus>=99,"positive opportunity compensation "+name);
                Log("Compensation "+name+" = "+bonus);
                float hitTime=name=="Skill5"?.56f:name=="Skill2"?2.03f:2.08f;
                while(track.TrackTime<hitTime+.15f)yield return null;
                yield return new WaitForEndOfFrame();Shot(ratio+"-cast-"+name);
                float hp=gm.GetLivingEnemies(false).Sum(t=>t.currentStat.hpNow);p.AnimationOnHit(name);
                Check(Mathf.Approximately(hp,gm.GetLivingEnemies(false).Sum(t=>t.currentStat.hpNow)),"one hit event per cast "+name);
                while(p.currentAnimation==name)yield return null;
                Check(track.TrackTime+.001f>=track.Animation.Duration,"full clip completed before unlocking "+name);
                if(key==Order[2])p.StopAllCoroutines();
                yield return new WaitForSecondsRealtime(.01f);
            }
            Check(p.SkillScheduler.PendingCount==0&&p.SkillScheduler.Active==SkillsTypeEnum.NONE,"FIFO drained "+ratio);
        }
        // Same opportunity triggers both sweep and the third-attack wolves.
        Reset();p.attackIndexer=2;p.sinisterStampedeChance=1;Attack();
        Check(p.SkillScheduler.Active==Order[0]&&p.SkillScheduler.Contains(Order[1]),"simultaneous chance and third attack are both retained");
        Reset();
        // Numerical compensation checks across speeds; not a fixed damage multiplier.
        foreach(float speed in new[]{75f,100f,200f}){
            p.ab.currentStat.atkSpd=speed;
            foreach(var key in Order){var anim=PlayerSkillScheduler.AnimationFor(key);float a=p.skeletonAnimation.Skeleton.Data.FindAnimation(anim).Duration;
                float normalDuration=p.skeletonAnimation.Skeleton.Data.FindAnimation("Attack1").Duration;
                float expected=100*(a/(speed*.01f)+.2f)/(normalDuration/(speed*.01f)+.2f);
                Check(Mathf.Abs(p.SkillCompensation(anim)-expected)<.01f,"time-based compensation "+anim+" speed "+speed);
            }
        }
        // Isolated total damage: group bonus is shared, not multiplied by enemy count.
        foreach(var key in Order){
            Reset();var targets=gm.GetLivingEnemies(false);float before=targets.Sum(t=>t.currentStat.hpNow);
            p.QueueSkill(key);Attack();var anim=PlayerSkillScheduler.AnimationFor(key);float bonus=p.SkillScheduler.Compensation;
            float baseDamage=key==Order[0]?300:key==Order[1]?150:450;
            float end=Time.realtimeSinceStartup+9;while((p.isAttacking||UnityEngine.Object.FindFirstObjectByType<GhostWolfPack>())&&Time.realtimeSinceStartup<end)yield return null;
            p.StopAllCoroutines();float dealt=before-targets.Sum(t=>t.currentStat.hpNow);
            float tolerance=key==Order[2]?3:(baseDamage+bonus)*.11f+3;
            Check(Mathf.Abs(dealt-baseDamage-bonus)<=tolerance,"one shared compensation budget "+key+" dealt="+dealt+" expected="+(baseDamage+bonus));
        }
        Reset();p.ab.currentStat.crit=50;p.ab.currentStat.critDmg=200;
        float critBonus=p.SkillCompensation("Skill3");p.ab.currentStat.crit=0;
        Check(Mathf.Abs(critBonus/p.SkillCompensation("Skill3")-1.5f)<.001f,"noncritical skill compensation includes expected normal crit");
        // Invalid queued skills are skipped, and lifecycle transitions clear the queue.
        Reset();foreach(var target in gm.GetLivingEnemies(false))target.currentStat.hpNow=80000;
        p.QueueSkill(Order[2]);p.QueueSkill(Order[1]);Attack();Check(p.SkillScheduler.Active==Order[1],"invalid lightning skips without blocking next cast");
        p.QueueSkill(Order[0]);p.ab.isDying=true;p.SetAnimation("Death1",false,false);
        Check(p.SkillScheduler.PendingCount==0&&p.SkillScheduler.Active==SkillsTypeEnum.NONE,"death clears active and pending casts");
        Reset();p.QueueSkill(Order[0]);p.QueueSkill(Order[2]);p.BeginVictoryRelaxation();
        Check(p.SkillScheduler.PendingCount==0&&p.SkillScheduler.Active==SkillsTypeEnum.NONE,"victory clears queue");
        Finish("PASS FIFO, full animation lock, simultaneous triggers, compensation budgets, crit expectation, invalid targets and lifecycle; UI and battle at three ratios");
    }
}
