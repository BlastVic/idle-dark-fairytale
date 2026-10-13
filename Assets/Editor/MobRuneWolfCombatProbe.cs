using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Opt-in integration test. Temporary waves/stats exist only in Play mode.
public static class MobRuneWolfCombatProbe
{
    sealed class Sample { public Enemy enemy; public int hits, attacks, reactions, deaths, phase, reactionTarget; }
    static readonly List<Sample> samples=new List<Sample>();
    const string Key="MobRuneWolf Integration Test";
    const string Report=MobRuneWolfBuilder.Output+"/combat-validation.txt";
    static int errors;static bool active;static double started;
    [MenuItem("Tools/Dark Fairytale/Monsters/MobRuneWolf/Run Combat Test (Play mode)")]
    public static void Begin()
    {
        if(!EditorApplication.isPlaying||!WaveManager.single)throw new Exception("Open Gameplay and enter Play mode first.");
        if(active)throw new Exception("MobRuneWolf test already running.");
        Directory.CreateDirectory(MobRuneWolfBuilder.Output);File.WriteAllText(Report,"MobRuneWolf Addressables / live combat event regression\nTemporary high enemy HP preserves one complete attack before controlled hit and kill. Player HP replenished for deterministic testing; not balance validation.\n");
        samples.Clear();errors=0;started=EditorApplication.timeSinceStartup;active=true;
        var wm=WaveManager.single;
        var test=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb.First(w=>w.levelKey=="Khorasan Ruins I")));
        test.levelKey=Key;test.levelName=Key;test.waves=new[]{new Wave{enemies=new[]{"MobRuneWolf"}},new Wave{enemies=new[]{"MobRuneWolf","MobWaxcap","MobRuneWolf"}}};
        wm.waveDb.RemoveAll(w=>w.levelKey==Key);wm.waveDb.Add(test);
        Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        var assets=LevelController.Instance.AssetManager;assets.HideCampPackage();assets.HideMapPackage();
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
            if(Player.single && Player.single.ab.currentStat!=null)Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
                if(enemy.name!="MobRuneWolf(Clone)"||enemy.ab.currentStat==null)continue;
                var sample=samples.FirstOrDefault(s=>s.enemy==enemy);
                if(sample==null){
                    sample=new Sample{enemy=enemy};samples.Add(sample);enemy.ab.currentStat.hpMax=enemy.ab.currentStat.hpNow=1000;enemy.ab.chanceInterruptAttack=0;
                    var captured=sample;
                    enemy.skeletonAnimation.AnimationState.Event+=(entry,e)=>{
                        if(!active)return;
                        if(e.Data.Name=="OnHit")captured.hits++;
                        if(e.Data.Name=="OnComplete"){
                            if(entry.Animation.Name=="Attack1")captured.attacks++;
                            if(entry.Animation.Name=="Hit")captured.reactions++;
                            if(entry.Animation.Name=="Death1")captured.deaths++;
                        }
                        File.AppendAllText(Report,"wave="+WaveManager.single.waveNumber+" "+entry.Animation.Name+" "+e.Data.Name+"\n");
                    };
                    if(enemy.maxSkins!=1||enemy.GetMaxAttacks()!=2||enemy.skeletonAnimation.Skeleton.Skin.Name!="1")throw new Exception("Wrong runtime skin or AttackN count");
                    File.AppendAllText(Report,"MobRuneWolf instantiated from DB/Addressables; skin=1, one game attack\n");
                }
                if(enemy.IsEntering)continue;
                if(sample.phase==0){
                    if(enemy.skeletonAnimation.transform.lossyScale.x>=0)throw new Exception("Wrong facing");
                    ScreenCapture.CaptureScreenshot(MobRuneWolfBuilder.Output+"/combat-wave"+WaveManager.single.waveNumber+".png");sample.phase=1;
                }
                if(sample.phase==1 && sample.hits>=1 && sample.attacks>=1){
                    enemy.StopAttacking();sample.reactionTarget=sample.reactions+1;enemy.ab.Hit(1);sample.phase=2;
                }
                if(sample.phase==2 && sample.reactions>=sample.reactionTarget){
                    if(enemy.currentAnimation!="Idle1")throw new Exception("Hit did not return to idle");
                    enemy.ab.Hit(100000);sample.phase=3;
                }
            }
            if(WaveManager.single.victoryCalled && samples.Count==3 && samples.All(s=>s.enemy==null)){
                if(samples.Any(s=>s.hits!=1||s.attacks!=1||s.reactions<1||s.deaths!=1||s.enemy!=null))throw new Exception("Wrong combat events or death cleanup");
                Stop(errors==0?"PASS: 3 MobRuneWolf instances, single/mixed waves; attack hit/completion, Hit -> Idle1, Death1 completion/destruction, next wave and victory; runtime errors=0":"FAIL errors="+errors);
            }
        }catch(Exception e){Stop("FAIL: "+e.Message);Debug.LogException(e);}
    }
    static void Stop(string result)
    {
        File.AppendAllText(Report,result+"\n");active=false;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
    }
}
