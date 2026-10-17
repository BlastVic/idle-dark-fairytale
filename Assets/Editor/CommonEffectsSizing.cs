using UnityEngine;
using UnityEditor;

// Explicit sizing pass; uses the original hit as the scale reference without modifying it.
public static class CommonEffectsSizing
{
    public static void Apply()
    {
        float[] sizes={0,4.8f,6.2f,0,5.2f,7f,4.6f};
        for(int i=1;i<7;i++){
            var path=CommonBattleEffectsAuthoring.PathAt(i);var root=PrefabUtility.LoadPrefabContents(path);
            try{
                if((i==2||i==5)&&root.GetComponent<CommonSpineBattleEffect>()){ /* Preserve authored Spine scales. */ }
                else if(i==3){root.transform.localScale=Vector3.one*4f;}
                else{
                    var depth=root.GetComponent<CursedBattleParticleLayer>();
                    depth.placement=i==2?CursedBattleParticleLayer.Placement.Ground:i==5?CursedBattleParticleLayer.Placement.AboveBody:CursedBattleParticleLayer.Placement.Body;
                    foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){
                        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                        bool primary=ps.gameObject==root;
                        var main=ps.main;
                        main.startSize=i==4&&!primary?.5f:sizes[i]*(primary?1:.86f);
                        ps.GetComponent<ParticleSystemRenderer>().pivot=Vector3.zero;
                        if(i==6){
                            var shape=ps.shape;shape.enabled=false;var velocity=ps.velocityOverLifetime;velocity.enabled=false;
                            main.startSpeed=0;
                            var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)1)});
                        }
                    }
                }
                if(i==4)ResurrectionMotionAuthoring.Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var rigPath=SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab";
        var rig=PrefabUtility.LoadPrefabContents(rigPath);
        try{
            var t=rig.GetComponent<SpineEffectsTest>();
            for(int i=0;i<7;i++)t.particleBounds[i]=CommonBattleEffectsAuthoring.Measure(t.particleEffects[i],out t.particleDurations[i]);
            t.widthSlider.value=100;
            PrefabUtility.SaveAsPrefabAsset(rig,rigPath);
        }finally{PrefabUtility.UnloadPrefabContents(rig);}
        AssetDatabase.SaveAssets();
    }
}
