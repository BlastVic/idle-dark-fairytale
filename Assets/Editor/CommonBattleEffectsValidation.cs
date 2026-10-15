using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Scripts.Level;

[InitializeOnLoad]
public static class CommonBattleEffectsValidation
{
    const string Out="output/common-effects/";
    const string Flag="COMMON_FX_QA";
    static bool active;static int phase,index,ratio,errors;static double next,started;
    static AsyncOperationHandle<GameObject> handle;static bool hasHandle;
    static string[] keys={"Hit_Particle","Shield_Particle","AOE_Particle","Heal_Particle","Res_Particle","Lightning_Particle","Absorb_Particle"};
    static int[] W={720,768,720},H={1280,1024,1440};
    static CommonBattleEffectsValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,k)=>{if(active&&(k==LogType.Error||k==LogType.Exception)){errors++;Log("ERROR "+m);}};}
    static void Log(string s)=>File.AppendAllText(Out+"battle-validation.txt",s+"\n");
    static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
    static void Resolution(){typeof(MonsterPreviewValidation).GetMethod("SetResolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{W[ratio],H[ratio]});}
    static void Finish(string message){Log(message);Time.timeScale=1;active=false;SessionState.SetBool("COMMON_FX_RETURN",true);EditorApplication.isPlaying=false;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode){
            if(SessionState.GetBool("COMMON_FX_RETURN",false)){SessionState.SetBool("COMMON_FX_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);EditorSceneManager.OpenScene(SpineEffectsTestBuilder.ScenePath);}
            if(File.Exists(Out+"battle.request")){File.Delete(Out+"battle.request");SessionState.SetBool(Flag,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;}
            return;
        }
        if(!EditorApplication.isPlaying)return;
        if(SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);active=true;phase=index=ratio=errors=0;started=EditorApplication.timeSinceStartup;File.WriteAllText(Out+"battle-validation.txt","Common FX live Gameplay; no save writes. Explicit VFX invocation, not skill-probability testing.\n");}
        if(!active)return;
        try{
            if(EditorApplication.timeSinceStartup-started>150)throw new Exception("Timeout phase "+phase);
            if(phase==0){var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;if(!WaveManager.single||!Player.single)return;
                SkillsController.Instance.ResetSkills();
                var wm=WaveManager.single;var wave=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));wave.levelKey="Common FX isolated QA";wave.mapGraphicKey="CH1_Node01";wave.waves=new[]{new Wave{enemies=new[]{"MobWaxcap","MobRuneWolf"}}};wm.waveDb.Add(wave);
                var ui=GameplayCanvas.single;ui.CancelInvoke("TryShowingXpBoosterNotification");ui.xpBoosterNotification.SetActive(false);ui.dailyCherriesPop.SetActive(false);ui.homeUI.gameObject.SetActive(false);ui.ToggleButtonGrid(false);ui.ToggleFusionBarUI(false);TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
                LevelController.Instance.AssetManager.HideMapPackage();wm.CallStartLevel(wave.levelKey);phase=1;next=EditorApplication.timeSinceStartup+2;return;
            }
            var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            if(Player.single){Player.single.StopAllCoroutines();Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax;}
            foreach(var e in enemies){e.StopAttacking();if(e.ab.currentStat!=null)e.ab.currentStat.hpNow=e.ab.currentStat.hpMax;}
            if(EditorApplication.timeSinceStartup<next)return;
            if(phase==1){if(enemies.Length<2||enemies.Any(e=>e.IsEntering))return;Player.single.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);foreach(var e in enemies)e.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);Time.timeScale=0;Resolution();phase=6;next=EditorApplication.timeSinceStartup+.6;return;}
            var assets=LevelController.Instance.AssetManager;var cam=assets.battleCampCamera.GetComponent<Camera>();
            if(phase==6){Time.timeScale=1;assets.battleStyle.PlacePlayer(Player.single,cam);for(int j=0;j<enemies.Length;j++)assets.battleStyle.PlaceEnemy(enemies[j],cam,j);phase=7;next=EditorApplication.timeSinceStartup+2;return;}
            if(phase==7){if(enemies.Any(e=>e.IsEntering))return;Time.timeScale=0;Player.single.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);foreach(var e in enemies){e.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);e.skeletonAnimation.Update(0);e.skeletonAnimation.LateUpdate();}phase=2;return;}
            if(phase==2){
                Vector3 hero=Player.single.rootPos.transform.position;
                Vector3 pos=index==0?enemies[0].rootPos.transform.position:index==1?hero+Vector3.up*4:index==2?assets.battleStyle.EnemyPosition(cam,0):index==3?hero+Vector3.up*1.7f:index==4?hero:index==5?enemies[0].transform.position:hero+Vector3.up*.5f;
                var entry=LevelDataDescriptions.ParticleDb.Single(e=>e.key==keys[index]);handle=Addressables.InstantiateAsync(entry.assetRef,pos,Quaternion.identity);hasHandle=true;phase=3;return;}
            if(phase==3){if(!handle.IsDone)return;Check(handle.Status==AsyncOperationStatus.Succeeded,"Addressables "+keys[index]);var go=handle.Result;
                foreach(var timer in go.GetComponentsInChildren<DisableDestroyOnTimer>(true)){timer.CancelInvoke();timer.enabled=false;}
                int n=0;foreach(var ps in go.GetComponentsInChildren<ParticleSystem>()){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(123+n++);ps.Simulate(index==3||index==6?.40f:.15f,false,true,true);ps.Pause(false);var mat=ps.GetComponent<Renderer>().sharedMaterial;Check(mat&&mat.shader.name=="DarkFairytale/Cursed Particle"&&mat.mainTexture,"new texture/material "+keys[index]);}
                phase=4;next=EditorApplication.timeSinceStartup+.2;return;}
            if(phase==4){ScreenCapture.CaptureScreenshot(Out+"battle-"+CommonBattleEffectsAuthoring.Ids[index]+"-"+W[ratio]+"x"+H[ratio]+".png");phase=5;next=EditorApplication.timeSinceStartup+.25;return;}
            if(phase==5){if(hasHandle){Addressables.ReleaseInstance(handle);hasHandle=false;}if(++index<7){phase=2;return;}index=0;if(++ratio<3){Resolution();phase=6;next=EditorApplication.timeSinceStartup+.5;return;}Check(errors==0,"zero runtime errors");Finish("COMPLETE: 7 effects x 3 aspect ratios, production Addressables and original trigger positions.");}
        }catch(Exception e){if(hasHandle&&handle.IsDone&&handle.Status==AsyncOperationStatus.Succeeded){Addressables.ReleaseInstance(handle);hasHandle=false;}Finish("FAIL "+e);Debug.LogException(e);}
    }
}
