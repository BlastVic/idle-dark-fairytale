using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Opt-in integration test. Temporary waves/stats exist only in Play mode.
public static class MobDrownedKingCombatProbe
{
    sealed class Sample { public Enemy enemy; public int hits, attacks, reactions, deaths, phase, reactionTarget; public bool damageObserved, entranceChecked, audioObserved; }
    static readonly List<Sample> samples=new List<Sample>();
    const string Key="MobDrownedKing Integration Test";
    const string Report=MobDrownedKingBuilder.Output+"/combat-validation.txt";
    static int errors;static bool active;static double started;
    [MenuItem("Tools/Dark Fairytale/Monsters/MobDrownedKing/Run Combat Test (Play mode)")]
    public static void Begin()
    {
        if(!EditorApplication.isPlaying||!WaveManager.single)throw new Exception("Open Gameplay and enter Play mode first.");
        if(active)throw new Exception("MobDrownedKing test already running.");
        Directory.CreateDirectory(MobDrownedKingBuilder.Output);File.WriteAllText(Report,"MobDrownedKing Addressables / live combat event regression\nTemporary high enemy HP preserves one complete attack before controlled hit and kill. Player HP replenished for deterministic testing; not balance validation.\n");
        samples.Clear();errors=0;started=EditorApplication.timeSinceStartup;active=true;SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        var wm=WaveManager.single;
        GameManager.single.xpNow=0;GameManager.single.xpMax=100000;
        var test=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb.First(w=>w.levelKey=="Khorasan Ruins I")));
        test.mapGraphicKey="CH1_Node04"; // Temporary waterside validation; persistent waves are untouched.
        test.levelKey=Key;test.levelName=Key;test.waves=new[]{new Wave{enemies=new[]{"MobDrownedKing"}},new Wave{enemies=new[]{"MobDrownedKing","MobDrownedMoon"}}};
        wm.waveDb.RemoveAll(w=>w.levelKey==Key);wm.waveDb.Add(test);
        Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        var assets=LevelController.Instance.AssetManager;assets.HideCampPackage();assets.HideMapPackage();
        if(GameplayCanvas.single.homeUI)GameplayCanvas.single.homeUI.gameObject.SetActive(false);
        GameplayCanvas.single.ToggleButtonGrid(false);GameplayCanvas.single.ToggleFusionBarUI(false);
        GameplayCanvas.single.victoryPanel.SetActive(false);GameplayCanvas.single.defeatPanel.SetActive(false);
        GameplayCanvas.single.dailyCherriesPop.SetActive(false);
        wm.CallStartLevel(Key);
    }
    static void Log(string message,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;File.AppendAllText(Report,"ERROR "+message+"\n");}}
    static void Tick()
    {
        try {
            if(!EditorApplication.isPlaying){Stop("INCOMPLETE: exited Play");return;}
            if(EditorApplication.timeSinceStartup-started>90){Stop("FAIL timeout");return;}
            if(Player.single && Player.single.ab.currentStat!=null){Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax;Player.single.ab.currentStat.block=0;}
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
                if(enemy.name!="MobDrownedKing(Clone)"||enemy.ab.currentStat==null)continue;
                var sample=samples.FirstOrDefault(s=>s.enemy==enemy);
                if(sample==null){
                    sample=new Sample{enemy=enemy};samples.Add(sample);enemy.ab.currentStat.hpMax=enemy.ab.currentStat.hpNow=1000;enemy.ab.chanceInterruptAttack=0;
                    var captured=sample;
                    enemy.skeletonAnimation.AnimationState.Event+=(entry,e)=>{
                        if(!active)return;
                        if(e.Data.Name=="OnHit"){captured.hits++;captured.audioObserved |= IdleKnightHero.UI.SoundManager.Instance.audioSource.isPlaying;captured.damageObserved |= Player.single.ab.currentStat.hpNow<Player.single.ab.currentStat.hpMax;}
                        if(e.Data.Name=="OnComplete"){
                            if(entry.Animation.Name=="Attack1")captured.attacks++;
                            if(entry.Animation.Name=="Hit")captured.reactions++;
                            if(entry.Animation.Name=="Death1")captured.deaths++;
                        }
                        File.AppendAllText(Report,"sample="+samples.IndexOf(captured)+" phase="+captured.phase+" time="+entry.TrackTime+" wave="+WaveManager.single.waveNumber+" "+entry.Animation.Name+" "+e.Data.Name+"\n");
                    };
                    if(enemy.maxSkins!=1||enemy.GetMaxAttacks()!=2||enemy.skeletonAnimation.Skeleton.Skin.Name!="1")throw new Exception("Wrong runtime skin or AttackN count");
                    File.AppendAllText(Report,"MobDrownedKing instantiated from DB/Addressables; skin=1, one game attack\n");
                }
                if(enemy.IsEntering){
                    float hp=enemy.ab.currentStat.hpNow;
                    enemy.ab.Hit(100);enemy.Attack();
                    if(enemy.ab.currentStat.hpNow!=hp || enemy.currentAnimation.Contains("Attack"))throw new Exception("Entrance isolation failed");
                    if(!GameManager.single.GetLivingEnemies().Contains(enemy.ab)||GameManager.single.GetLivingEnemies(false).Contains(enemy.ab))throw new Exception("Entrance wave/target list failed");
                    sample.entranceChecked=true;continue;
                }
                if(sample.phase==0){
                    if(enemy.skeletonAnimation.transform.lossyScale.x>=0)throw new Exception("Wrong facing");
                    ScreenCapture.CaptureScreenshot(MobDrownedKingBuilder.Output+"/combat-wave"+WaveManager.single.waveNumber+".png");sample.phase=1;
                }
                if(sample.phase==1 && sample.hits>=1 && sample.attacks>=1){
                    enemy.StopAttacking();sample.reactionTarget=sample.reactions+1;enemy.ab.Hit(1);sample.phase=2;
                }
                if(sample.phase==2 && sample.reactions>=sample.reactionTarget){
                    if(enemy.currentAnimation!="Idle1")throw new Exception("Hit did not return to idle");
                    enemy.ab.chanceInterruptAttack=100;
                    enemy.Attack();enemy.StopAttacking();enemy.ab.Hit(1);
                    if(enemy.currentAnimation!="Hit")throw new Exception("Actual attack interrupt failed");
                    sample.reactionTarget=sample.reactions+1;sample.phase=3;
                }
                if(sample.phase==3 && sample.reactions>=sample.reactionTarget){
                    if(enemy.currentAnimation!="Idle1")throw new Exception("Interrupted attack failed to return idle");
                    float xp=GameManager.single.xpNow;
                    int inventoryBefore=InventoryManager.single.GetTotalItemCount();
                    int guaranteed=enemy.itemsGiven.Count(item=>item.m_ChanceDrop>=1f);
                    enemy.ab.Hit(100000);
                    if(GameManager.single.xpNow<=xp)throw new Exception("No actual XP award on death");
                    Chapter01BalanceTools.ValidateConfiguredDrops(enemy);
                    int awarded=InventoryManager.single.GetTotalItemCount()-inventoryBefore;
                    if(awarded<guaranteed || awarded>enemy.itemsGiven.Count)throw new Exception("Configured loot was not awarded to inventory");
                    File.AppendAllText(Report,"PASS actual XP award="+(GameManager.single.xpNow-xp)+", inventory drops="+awarded+", guaranteed="+guaranteed+", configured entries="+enemy.itemsGiven.Count+"; entrance="+sample.entranceChecked+", audio source playing at hit="+sample.audioObserved+"\n");
                    sample.phase=4;
                }
            }
            if(WaveManager.single.victoryCalled && samples.Count==2 && samples.All(s=>s.enemy==null)){
                File.AppendAllText(Report,string.Join("\n",samples.Select(s=>"sample "+samples.IndexOf(s)+": hits="+s.hits+" attacks="+s.attacks+" reactions="+s.reactions+" deaths="+s.deaths+" entry="+s.entranceChecked))+"\n");
                if(samples.Any(s=>!s.damageObserved||!s.entranceChecked||s.hits!=1||s.attacks!=1||s.reactions<1||s.deaths!=1||s.enemy!=null))throw new Exception("Wrong combat events or death cleanup");
                Stop(errors==0?"PASS: 2 MobDrownedKing instances, single/mixed waves; actual player HP loss, attack hit/completion, entrance isolation, actual attack interruption -> Hit -> Idle1, XP award, configured inventory drops, Death1 completion/destruction, next wave and victory; runtime errors=0":"FAIL errors="+errors);
            }
        }catch(Exception e){Stop("FAIL: "+e.Message);Debug.LogException(e);}
    }
    static void Stop(string result)
    {
        File.AppendAllText(Report,result+"\n");active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        EditorApplication.isPlaying=false;SessionState.SetBool("MOBDROWNEDKING_RETURN_TO_TEST",true);
    }
}
