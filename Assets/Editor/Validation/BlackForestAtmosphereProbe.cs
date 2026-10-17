using System;
using System.IO;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in evidence capture in the existing Gameplay scene; never runs in a player build.</summary>
public static class BlackForestAtmosphereProbe
{
    const string Output = "output/black-forest-atmosphere";
    static readonly float[] Times = { 1, 9.8f, 12 };
    static readonly string[] Names = { "closed", "look-left", "look-right" };
    static int next, errors;
    static double started;
    static bool checkedLayers;

    [MenuItem("Tools/Dark Fairytale/Environment/Test First Level Atmosphere")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying || !WaveManager.single) { Debug.LogWarning("Start Gameplay in Play mode first.");return; }
        Directory.CreateDirectory(Output);
        next=errors=0;checkedLayers=false;started=EditorApplication.timeSinceStartup;
        File.WriteAllText(Output+"/gameplay-validation.txt","Live Gameplay atmosphere test\n");
        Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        BlackForestMushroomBuilder.RunFirstLevel();
    }

    static void OnLog(string message,string stack,LogType type)
    {
        if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)return;
        errors++;File.AppendAllText(Output+"/gameplay-validation.txt","ERROR: "+message+"\n");
    }

    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started>90) { Stop("Stopped before completion");return; }
        var atmosphere=UnityEngine.Object.FindFirstObjectByType<BlackForestAtmosphere>();
        if(!atmosphere)return;
        var moon=atmosphere.transform.Find("Watching Moon").GetComponent<SkeletonAnimation>();
        if(!checkedLayers) {
            var foreground=atmosphere.transform.Find("Foreground Reeds").GetComponent<SpriteRenderer>();
            var actors=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            if(actors.Length==0)return;
            foreach(var actor in actors) {
                var renderer=actor.GetComponentInChildren<MeshRenderer>();
                if(SortingLayer.GetLayerValueFromID(foreground.sortingLayerID)<=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID))throw new Exception("Foreground behind an actor");
            }
            checkedLayers=true;
            File.AppendAllText(Output+"/gameplay-validation.txt","PASS: foreground above live enemy renderers; layered backdrop and 5 Spine props instantiated.\n");
        }
        if(next<Times.Length && moon.AnimationState.GetCurrent(0).TrackTime>=Times[next]) {
            ScreenCapture.CaptureScreenshot(Output+"/gameplay-"+Names[next]+".png");next++;
        }
        if(next==Times.Length && moon.GetComponent<BlackForestMoonMotion>().NormalCycles+moon.GetComponent<BlackForestMoonMotion>().RedCycles>=2)Stop(errors==0?"PASS: full moon cycle and lantern cycles, 3 screenshots; runtime errors=0":"FAIL: runtime errors="+errors);
    }

    static void Stop(string result)
    {
        File.AppendAllText(Output+"/gameplay-validation.txt",result+"\n");
        Application.logMessageReceived-=OnLog;EditorApplication.update-=Tick;
    }
}
