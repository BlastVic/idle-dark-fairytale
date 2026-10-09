using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in live regression: frozen fall, full spawn playback, damage/target isolation and unlock.</summary>
public static class MushroomEntranceProbe
{
    sealed class Sample
    {
        public bool fall, spawn, ready;
        public float spawnStart, duration;
    }
    static readonly Dictionary<Enemy,Sample> samples = new Dictionary<Enemy,Sample>();
    static readonly HashSet<string> captured = new HashSet<string>();
    const string Folder = "output/monster-animation/entrance";
    const string Report = Folder + "/validation.txt";
    static double started;
    static int errors;
    static bool legacy;
    static Waveset originalWave;
    static int originalWaveIndex;

    [MenuItem("Tools/Dark Fairytale/Monsters/Test Legacy Slime Replacement (Play mode)")]
    public static void BeginLegacy()
    {
        if (!EditorApplication.isPlaying || !WaveManager.single) throw new Exception("Start Gameplay first.");
        originalWaveIndex = WaveManager.single.waveDb.FindIndex(w => w.levelKey == "Khorasan Ruins I");
        originalWave = WaveManager.single.waveDb[originalWaveIndex];
        var temporary = JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(originalWave));
        temporary.waves = new[] { new Wave { enemies = new[] { "Mob1", "Mob2", "Mob4" } } };
        WaveManager.single.waveDb[originalWaveIndex] = temporary;
        Begin(); legacy = true;
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Test Mushroom Entrance (Play mode)")]
    public static void Begin()
    {
        legacy = false;
        if(!EditorApplication.isPlaying || !WaveManager.single)throw new Exception("Start Gameplay first.");
        samples.Clear();captured.Clear();errors=0;started=EditorApplication.timeSinceStartup;
        Directory.CreateDirectory(Folder);File.WriteAllText(Report,"Live mushroom entrance regression\n");
        Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        BlackForestMushroomBuilder.RunFirstLevel();
    }
    static void Log(string message,string stack,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) { errors++;File.AppendAllText(Report,"ERROR: "+message+"\n"); }
    }
    static void Tick()
    {
        try {
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started>120) { Stop("INCOMPLETE: stopped before all four entrances finished");return; }
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
                string key = enemy.name.Replace("(Clone)", "");
                int legacyIndex = Array.IndexOf(new[] { "Mob1", "Mob2", "Mob4" }, key);
                if(!enemy.name.StartsWith("BF_Mushroom_") && !(legacy && legacyIndex >= 0))continue;
                var drop=enemy.GetComponent<DropIn>();var skeleton=enemy.GetComponentInChildren<SkeletonAnimation>();
                if (legacyIndex >= 0 && skeleton.skeletonDataAsset.name != BlackForestMushroomBuilder.Ids[legacyIndex] + "_SkeletonData")
                    throw new Exception(key + " still loads old slime graphics");
                var entry=skeleton.AnimationState.GetCurrent(0);
                if(entry==null || enemy.ab.currentStat==null)continue;
                if(!samples.TryGetValue(enemy,out var sample)) { sample=new Sample();samples.Add(enemy,sample); }
                if(drop.Phase==DropIn.EntrancePhase.Falling) {
                    if(entry.Animation.Name!="spawn" || entry.TrackTime!=0 || entry.TimeScale!=0)throw new Exception("Fall must hold spawn frame zero");
                    if(skeleton.Skeleton.FindSlot("head").A < .99f)throw new Exception("Frozen spawn pose must remain visible");
                    if(!sample.fall) { AssertProtected(enemy);sample.fall=true;File.AppendAllText(Report,enemy.name+": Falling, spawn held at 0, hit/attack/target exclusion PASS\n"); }
                    if(skeleton.transform.position.y-drop.origY<2.5f && captured.Add("fall"))ScreenCapture.CaptureScreenshot(Folder+"/01-fall.png");
                }
                else if(drop.Phase==DropIn.EntrancePhase.Spawning) {
                    if(entry.Animation.Name!="spawn" || entry.Loop || entry.TimeScale!=1)throw new Exception("Landing must play original spawn once");
                    if(!drop.spawnInPlace || drop.isFalling || Mathf.Abs(skeleton.transform.position.y-drop.origY)>.001f)throw new Exception("Mushroom entrance must stay at its final position");
                    if(!sample.spawn) {

                        AssertProtected(enemy);sample.spawn=true;sample.spawnStart=Time.time-entry.TrackTime;sample.duration=entry.Animation.Duration;
                        File.AppendAllText(Report,enemy.name+": Stationary original spawn playing, hit/attack/target exclusion PASS\n");
                    }
                    if(entry.TrackTime>.3f && captured.Add("spawn"))ScreenCapture.CaptureScreenshot(Folder+"/02-spawn.png");
                }
                else if(!sample.ready) {
                    if (skeleton.transform.lossyScale.x >= 0) throw new Exception("Mushroom must face left");
                    if (enemy.ab.enemyLifebar.name != "CursedStorybookEnemyBar") throw new Exception("Mushroom must use the battle-style health bar");
                    if(!sample.spawn || Time.time-sample.spawnStart<sample.duration-.04f)throw new Exception("Combat unlocked before spawn completed");
                    if(!GameManager.single.GetLivingEnemies(false).Contains(enemy.ab))throw new Exception("Ready enemy not targetable");
                    sample.ready=true;
                    File.AppendAllText(Report,enemy.name+": Ready after full "+sample.duration+"s spawn, targetable PASS\n");
                    if(captured.Add("ready"))ScreenCapture.CaptureScreenshot(Folder+"/03-ready.png");
                }
            }
            if(samples.Count>=(legacy ? 3 : 4) && samples.Values.All(s=>s.ready))Stop(errors==0?"PASS: " + samples.Count + " complete entrances; stationary spawn; no early attacks, damage, targeting or wave removal; runtime errors=0":"FAIL: runtime errors="+errors);
        } catch(Exception e) { Stop("FAIL: "+e.Message);Debug.LogException(e); }
    }
    static void AssertProtected(Enemy enemy)
    {
        var entry=enemy.skeletonAnimation.AnimationState.GetCurrent(0);
        float hp=enemy.ab.currentStat.hpNow, playerHp=Player.single.ab.currentStat.hpNow;
        enemy.ab.Hit(1);
        enemy.ab.Hit(1,false,0,150,true);
        enemy.ab.Hit(1,false,0,150,false,true);
        enemy.StartAttacking(true);enemy.Attack();enemy.AnimationOnHit("Attack1");
        if(enemy.ab.currentStat.hpNow!=hp || Player.single.ab.currentStat.hpNow!=playerHp)throw new Exception("Entrance allowed incoming/outgoing damage");
        if(enemy.skeletonAnimation.AnimationState.GetCurrent(0)!=entry)throw new Exception("Attack interrupted entrance");
        if(GameManager.single.GetLivingEnemies(false).Contains(enemy.ab))throw new Exception("Entering enemy targetable");
        if(!GameManager.single.GetLivingEnemies().Contains(enemy.ab))throw new Exception("Entering enemy removed from wave accounting");
    }
    static void Stop(string result)
    {
        File.AppendAllText(Report,result+"\n");EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        if (legacy && originalWave != null && WaveManager.single) WaveManager.single.waveDb[originalWaveIndex] = originalWave;
        originalWave = null;
    }
}
