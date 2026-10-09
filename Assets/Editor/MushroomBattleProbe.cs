using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor-only evidence capture for the explicitly started three-wave integration test.
public static class MushroomBattleProbe
{
    static readonly HashSet<Enemy> seen = new HashSet<Enemy>();
    static readonly HashSet<string> captured = new HashSet<string>();
    static readonly HashSet<Enemy> deaths = new HashSet<Enemy>();
    static readonly HashSet<int> capturedWaves = new HashSet<int>();
    static double started;
    static int runtimeErrors;
    const string Report = "output/monster-animation/mushroom-battle-validation.txt";
    public static void Begin()
    {
        runtimeErrors=0;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        seen.Clear(); captured.Clear(); deaths.Clear(); capturedWaves.Clear(); started = EditorApplication.timeSinceStartup;
        File.WriteAllText(Report,"Live Gameplay test: Addressables -> Enemy -> combat events -> next wave\n");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void OnLog(string message,string stack,LogType type)
    {
        if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert) return;
        runtimeErrors++;
        File.AppendAllText(Report,"RUNTIME ERROR: "+message+"\n");
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started > 120)
        {
            File.AppendAllText(Report,"Stopped; completed enemy deaths="+deaths.Count+"\n"); EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; return;
        }
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            string id = enemy.name.Replace("(Clone)","");
            if(Array.IndexOf(BlackForestMushroomBuilder.Ids,id)<0 || enemy.skeletonAnimation == null) continue;
            if(seen.Add(enemy))
            {
                File.AppendAllText(Report,id+": spawned, scaleX="+enemy.skeletonAnimation.transform.lossyScale.x+"\n");
                enemy.skeletonAnimation.AnimationState.Event += (entry,e) => {
                    File.AppendAllText(Report,id+" / "+entry.Animation.Name+" / "+e.Data.Name+" playerHP="+Player.single.ab.currentStat.hpNow+"\n");
                    if(entry.Animation.Name=="Death1" && e.Data.Name=="OnComplete") deaths.Add(enemy);
                };
            }
            if(!enemy.GetComponent<DropIn>().IsEntering && !captured.Contains(id))
            {
                captured.Add(id);
                ScreenCapture.CaptureScreenshot("output/monster-animation/"+id+"-gameplay.png");
            }
        }
        var wm = WaveManager.single;
        var live = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (wm.currentWaveset != null && live.Length == wm.currentWaveset.waves[wm.waveNumber].enemies.Length
            && live.Length > 0 && live.All(e => !e.GetComponent<DropIn>().IsEntering) && capturedWaves.Add(wm.waveNumber))
        {
            ScreenCapture.CaptureScreenshot("output/monster-animation/layout-wave"+(wm.waveNumber+1)+".png");
            File.AppendAllText(Report,"Layout captured: "+wm.currentWaveset.levelKey+" wave="+(wm.waveNumber+1)+" actors="+live.Length+"\n");
        }
        if(wm.victoryCalled && deaths.Count == seen.Count && seen.Count > 0)
        {
            File.AppendAllText(Report,(runtimeErrors==0 ? "PASS: " : "FAIL: ")+wm.currentWaveset.levelKey+", "+seen.Count+" enemy instances completed Death1; level victory reached; runtime errors="+runtimeErrors+".\n");
            EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        }
    }
}
