using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ResurrectionMotionAuthoring
{
    public static void Configure(GameObject root)
    {
        foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){
            ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startLifetime=2f;main.startSpeed=0;
            var size=ps.sizeOverLifetime;size.enabled=true;size.separateAxes=false;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.65f,.2f,1.12f));
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
            // All axes use curve mode, as required by Unity's velocity module.
            velocity.x=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
            velocity.z=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
            velocity.y=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,0),new Keyframe(.2f,0),new Keyframe(.3f,1.5f),new Keyframe(.65f,1.5f),new Keyframe(1,.6f)));
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(1,.65f),new GradientAlphaKey(0,1)});color.color=gradient;
        }
    }
    public static void Apply()
    {
        var path=CommonBattleEffectsAuthoring.PathAt(4);var root=PrefabUtility.LoadPrefabContents(path);
        try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var rigPath=SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab";var rig=PrefabUtility.LoadPrefabContents(rigPath);
        try{var t=rig.GetComponent<SpineEffectsTest>();t.particleBounds[4]=CommonBattleEffectsAuthoring.Measure(t.particleEffects[4],out t.particleDurations[4]);PrefabUtility.SaveAsPrefabAsset(rig,rigPath);}finally{PrefabUtility.UnloadPrefabContents(rig);}
        AssetDatabase.SaveAssets();ValidateMotion();
    }
    static void ValidateMotion()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CommonBattleEffectsAuthoring.PathAt(4)));
        try{
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=123;
            var particles=new ParticleSystem.Particle[32];float[] times={.1f,.4f,1.1f,1.5f,1.85f};float lastSize=0,peakSize=0,peakY=0,lastY=0;var report="Resurrection motion: production prefab\n";
            foreach(float time in times){ps.Simulate(time,false,true,true);int count=ps.GetParticles(particles);if(count<1)throw new Exception("Missing resurrection particle at "+time);
                float size=particles[0].GetCurrentSize(ps),y=particles[0].position.y,alpha=particles[0].GetCurrentColor(ps).a/255f;
                if(time==.1f)lastSize=size;
                if(time==.4f){if(size<=lastSize)throw new Exception("Expansion failed");peakSize=size;peakY=y;}
                if(time>=1.1f){if(Mathf.Abs(size-peakSize)>.04f||y<=peakY||y<lastY)throw new Exception("Rise after max size failed");}
                if(time==1.1f&&alpha<.95f)throw new Exception("Fades before rise finishes");
                if(time==1.85f&&alpha>.3f)throw new Exception("Fade out failed");
                lastY=y;report+=$"PASS t={time} size={size:F3} y={y:F3} alpha={alpha:F3}\n";
            }
            ps.Simulate(2.1f,false,true,true);if(ps.particleCount!=0)throw new Exception("Particle survives final fade");
            File.WriteAllText("output/common-effects/res-motion-validation.txt",report+"PASS expires after fade\n");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
