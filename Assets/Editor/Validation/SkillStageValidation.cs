using System;
using System.Collections;
using System.IO;
using System.Linq;
using Scripts.Level;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SkillStageValidation
{
    const string Dir="output/skill-stage-fix/";
    static bool running;
    static double deadline;
    static string mode;
    static SkillStageValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&File.Exists(Dir+"command.request")){
            mode=File.ReadAllText(Dir+"command.request").Trim();File.Delete(Dir+"command.request");
            if(mode=="save-placement"){
                EditorSceneManager.OpenScene(SkillTestBuilder.ScenePath);
                var stage=UnityEngine.Object.FindFirstObjectByType<SkillTestStage>();
                stage.player.ab=stage.player.GetComponent<Actor_Base>();
                foreach(var enemy in stage.targets){enemy.ab=enemy.GetComponent<Actor_Base>();enemy.skeletonAnimation=enemy.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();}
                stage.Place();
                foreach(var component in stage.GetComponentsInChildren<Component>())if(component)EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(stage.player.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(stage.player.skeletonAnimation.transform);
                foreach(var enemy in stage.targets){PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.skeletonAnimation.transform);}
                EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);EditorSceneManager.SaveScene(stage.gameObject.scene);
                AssetDatabase.DeleteAsset("Assets/Tests/Skills/SkillTestBattleStyle.asset");
                File.WriteAllText(Dir+"placement.txt","PASS saved shared battle style and deterministic actor placement");return;
            }
            SessionState.SetString("SKILL_STAGE_QA",mode);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
            EditorSceneManager.OpenScene(mode=="production"?"Assets/Scenes/Gameplay.unity":SkillTestBuilder.ScenePath);
            EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&!running&&!string.IsNullOrEmpty(SessionState.GetString("SKILL_STAGE_QA",""))){
            mode=SessionState.GetString("SKILL_STAGE_QA","");
            if(mode!="production" && !(UnityEngine.Object.FindFirstObjectByType<SkillTest>()?.Ready??false))return;
            if(!Player.single)return;
            SessionState.SetString("SKILL_STAGE_QA","");running=true;deadline=EditorApplication.timeSinceStartup+180;
            File.WriteAllText(Dir+mode+".txt","");GameManager.single.StartCoroutine(Guard(mode=="single"?SingleTarget():mode=="balance"?Balance():Run()));
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static void Check(bool pass,string label){if(!pass)throw new Exception(label);File.AppendAllText(Dir+mode+".txt","PASS "+label+"\n");}
    static void Finish(string result){File.AppendAllText(Dir+mode+".txt",result+"\n");running=false;Time.timeScale=1;EditorApplication.isPlaying=false;SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
    static IEnumerator Guard(IEnumerator work){while(true){object next;try{if(!work.MoveNext())break;next=work.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return next;}}
    static void Shot(string suffix){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+suffix+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
    static IEnumerator Run(){
        SkillTest test=UnityEngine.Object.FindFirstObjectByType<SkillTest>();
        if(mode=="production"){
            while(!WaveManager.single||!GameplayCanvas.single||(UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>() is CursedBookIntro intro&&!intro.Finished))yield return null;
            var wm=WaveManager.single;var wave=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));wave.levelKey="Skill stage isolated QA";wave.mapGraphicKey="CH1_Node01";wave.waves=new[]{new Wave{enemies=new[]{"MobTallowkin","MobBramblet","MobMurmlet"}}};wm.waveDb.Add(wave);
            var ui=GameplayCanvas.single;ui.CancelInvoke("TryShowingXpBoosterNotification");ui.xpBoosterNotification.SetActive(false);ui.dailyCherriesPop.SetActive(false);ui.homeUI.gameObject.SetActive(false);ui.ToggleButtonGrid(false);ui.ToggleFusionBarUI(false);TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
            LevelController.Instance.AssetManager.HideMapPackage();wm.CallStartLevel(wave.levelKey);
            yield return new WaitForSeconds(2);
        }
        var player=Player.single;player.manualControl=true;
        Enemy[] enemies;
        do{yield return null;enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);}while(enemies.Length!=3||enemies.Any(e=>e.IsEntering));
        enemies=enemies.OrderBy(e=>e.name.Contains("Tallowkin")?0:e.name.Contains("Bramblet")?1:2).ToArray();
        foreach(var enemy in enemies){enemy.StopAttacking();enemy.ab.trainingTarget=true;enemy.SetAnimation("Idle1",true,false);}
        var assets=LevelController.Instance.AssetManager;var cam=assets.battleCampCamera.GetComponent<Camera>();
        Check(assets.battleStyle==AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset"),"shared production layout asset");
        Check(!cam.orthographic&&Mathf.Approximately(cam.fieldOfView,66)&&Mathf.Approximately(cam.transform.position.z,-15),"production camera projection");
        int[] widths={720,768,720},heights={1280,1024,1440};
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(widths[ratio],heights[ratio]);yield return new WaitForSeconds(.5f);
            while(player.isAttacking)yield return null;
            if(test){test.ResetTest();yield return null;test.stage.Place();}
            else{assets.battleStyle.PlacePlayer(player,cam);player.SetAnimation("Idle1",true,false);for(int i=0;i<3;i++){assets.battleStyle.PlaceEnemy(enemies[i],cam,i);enemies[i].StopAttacking();}yield return new WaitForSeconds(2);}
            // Sample the exact same idle frame in both scenes for viewport comparison.
            var actors=new AnimationController[]{player,enemies[0],enemies[1],enemies[2]};
            foreach(var actor in actors){actor.skeletonAnimation.AnimationState.ClearTracks();actor.skeletonAnimation.Skeleton.SetToSetupPose();actor.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true).TrackTime=0;actor.skeletonAnimation.Update(0);actor.skeletonAnimation.LateUpdate();}
            var points=actors.Select(actor=>{var b=actor.skeletonAnimation.GetComponent<Renderer>().bounds;return cam.WorldToViewportPoint(new Vector3(b.center.x,b.min.y,0));}).ToArray();
            File.WriteAllText(Dir+mode+"-"+ratio+"-positions.json",JsonUtility.ToJson(new Points{values=points},true));
            if(mode=="production"){
                var expected=JsonUtility.FromJson<Points>(File.ReadAllText(Dir+"test-"+ratio+"-positions.json")).values;
                for(int i=0;i<points.Length;i++)Check(Vector2.Distance(points[i],expected[i])<.003f,"same viewport actor "+i+" ratio "+ratio);
            }
            yield return new WaitForEndOfFrame();Shot(ratio+"-idle");
            SkillsController.Instance.sinisterStampedeLevel=1;player.PerformManualAction(SkillsTypeEnum.Sinister_Stampede);
            CommonSpineBattleEffect fx;
            do{yield return null;fx=UnityEngine.Object.FindObjectsByType<CommonSpineBattleEffect>(FindObjectsSortMode.None).FirstOrDefault(e=>e.horizontalSweep);}while(!fx);
            float lastX=float.NegativeInfinity,firstY=0;int samples=0,shot=0;bool exited=false;
            while(fx){
                yield return new WaitForEndOfFrame();if(!fx)break;
                float time=fx.skeleton.AnimationState.GetCurrent(0).AnimationTime;
                if(time>=.64f&&time<=1.31f){
                    var bone=fx.skeleton.Skeleton.FindBone("xg1");var world=fx.skeleton.transform.TransformPoint(new Vector3(bone.WorldX,bone.WorldY,0));var v=cam.WorldToViewportPoint(world);
                    if(samples==0)firstY=world.y;
                    if(world.x<lastX-.001f||Mathf.Abs(world.y-firstY)>.001f)throw new Exception($"Sweep must move horizontally right: t={time} world={world} previousX={lastX} expectedY={firstY} bone=({bone.X},{bone.Y})");
                    lastX=world.x;samples++;if(v.x>1.2f)exited=true;
                    if(shot<3&&time>=new[]{.75f,.95f,1.25f}[shot]){Shot(ratio+"-sweep-"+shot);shot++;}
                }
            }
            Check(samples>5&&exited,"live sweep exits right with constant height ratio "+ratio);
            while(player.isAttacking)yield return null;
        }
        Finish("PASS layout and live sweep in "+mode+" at all three ratios");
    }
    static IEnumerator SingleTarget(){
        var test=UnityEngine.Object.FindFirstObjectByType<SkillTest>();
        test.ResetTest();yield return null;
        var p=Player.single;
        for(int i=1;i<test.stage.targets.Length;i++)test.stage.targets[i].gameObject.SetActive(false);
        Check(GameManager.single.GetLivingEnemies(false).Count==1,"one living enemy");
        test.Select(SkillsTypeEnum.Sinister_Stampede);test.SetLevel(1);yield return null;test.Cast();
        Check(p.SkillScheduler.Active==SkillsTypeEnum.Sinister_Stampede,"manual single-target cast starts");
        yield return new WaitForSeconds(4);
        Check(test.HitCount==1,"single target receives sweep damage");
        test.ResetTest();yield return null;
        SkillsController.Instance.sinisterStampedeLevel=1;p.sinisterStampedeChance=1;p.manualControl=false;
        typeof(Player).GetMethod("Attack",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);
        Check(p.SkillScheduler.Active==SkillsTypeEnum.Sinister_Stampede,"normal attack roll triggers sweep with one enemy");
        p.manualControl=true;yield return new WaitForSeconds(4);
        Check(test.HitCount==1,"automatic single-target sweep deals damage once");
        test.ResetTest();yield return null;
        SkillsController.Instance.sinisterStampedeLevel=1;
        test.stage.targets[0].gameObject.SetActive(false);
        p.PerformManualAction(SkillsTypeEnum.Sinister_Stampede);
        Check(!p.isAttacking&&p.SkillScheduler.Active==SkillsTypeEnum.NONE,"no empty-field cast");
        var node=AssetDatabase.LoadAssetAtPath<GameObject>(StorybookSkillsBuilder.Path).GetComponentsInChildren<StorybookSkillNode>(true).Single(n=>n.key==SkillsTypeEnum.Sinister_Stampede);
        Check(node.descriptionTemplate==QueuedSkillAuthoring.SweepDescription&&!node.descriptionTemplate.Contains("多于1"),"saved description and authoring agree");
        Finish("PASS single-target manual/automatic damage and empty-field guard");
    }
    static IEnumerator Balance(){
        foreach(var data in SkillsDataDescriptions.SkillDataArr){
            var columns=new[]{data.value1,data.value2,data.value3,data.value4,data.value5};
            int key=(int)data.skillsType;
            for(int c=0;c<SkillDesign.Labels[key].Length;c++){
                var values=columns[c];Check(values.Length==11&&values[0]==0,"unlearned zero / ten levels "+key+" column "+c);
                for(int rank=1;rank<=10;rank++)Check(Mathf.Abs(values[rank]-values[1]*(rank==10?3:1+.2f*(rank-1)))<.0001f,"shared growth "+key+" rank "+rank);
            }
        }
        var test=UnityEngine.Object.FindFirstObjectByType<SkillTest>();test.ResetTest();yield return null;
        var p=Player.single;
        foreach(var animation in new[]{"Skill2","Skill3"}){var seconds=p.skeletonAnimation.Skeleton.Data.FindAnimation(animation).Duration;Check(Mathf.Abs(p.SkillCompensation(animation)-(seconds/1.2f+.2f)/1.38f*100)<.01f,"compensation uses actual animation rate "+animation);}
        var ab=p.ab;ab.currentStat.hpMax=1000;ab.currentStat.hpNow=500;ab.currentStat.absorb=0;
        ab.AddAbsorb(.05f);ab.AddAbsorb(.05f);Check(ab.currentStat.absorb==50&&ab.currentStat.hpNow==550,"shield refresh never stacks");
        ab.AddAbsorb(.15f);Check(ab.currentStat.absorb==150&&ab.currentStat.hpNow==650,"shield strength scales with level");
        ab.currentStat.absorb=0;ab.currentStat.hpNow=990;ab.HealByPercent(.12f);Check(ab.currentStat.hpNow==1000,"kill healing cannot exceed health cap");
        for(int rank=1;rank<=10;rank+=9){
            test.ResetTest();yield return null;test.Select(SkillsTypeEnum.Boss_Killer);test.SetLevel(rank);yield return null;test.Cast();
            Check(p.SkillScheduler.Active==SkillsTypeEnum.Boss_Killer,"boss action participates in scheduler level "+rank);
            yield return new WaitForSeconds(3);
            Check(test.HitCount==1&&test.TotalDamage>0,"one boss strike, not fictional six hits level "+rank);
        }
        Finish("PASS all effect curves, shield cap, healing cap and boss queue at levels 1/10");
    }
    [Serializable]class Points{public Vector3[] values;}
}
