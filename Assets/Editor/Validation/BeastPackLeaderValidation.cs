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
public static class BeastPackLeaderValidation
{
    const string Dir="output/beast-pack-leader/";
    static bool running,holdCombat;
    static bool freezePets=true;
    static string mode;
    static double deadline;
    static readonly int[] Width={720,768,720}, Height={1280,1024,1440};
    static BeastPackLeaderValidation(){EditorApplication.update+=Tick;}
    static void Log(string message)=>File.AppendAllText(Dir+mode+".txt",message+"\n");
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var request=Dir+"command.request";
        if(File.Exists(request)){
            var cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{
                if(cmd=="apply"){BeastPackLeaderAuthoring.Apply();File.WriteAllText(Dir+"apply.txt","PASS");return;}
                if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop first");
                if(cmd!="validate")throw new Exception("Unknown command");
                SessionState.SetString("BEAST_LEADER_QA",cmd);
                SessionState.SetString("BEAST_LEADER_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("BEAST_LEADER_RETURN",false)){
            SessionState.SetBool("BEAST_LEADER_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            var scene=SessionState.GetString("BEAST_LEADER_SCENE","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);return;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetString("BEAST_LEADER_QA","")!=""&&GameManager.single&&GameplayCanvas.single){
            EditorApplication.LockReloadAssemblies();
            mode=SessionState.GetString("BEAST_LEADER_QA","");SessionState.SetString("BEAST_LEADER_QA","");running=true;holdCombat=false;
            deadline=EditorApplication.timeSinceStartup+300;File.WriteAllText(Dir+mode+".txt","Actual Unity QA, save writes disabled\n");
            GameManager.single.StartCoroutine(Guard(Run()));
        }
        if(running&&holdCombat&&Player.single&&!Player.single.isCampPlayer&&!Player.single.ab.isDying){
            Player.single.ab.currentStat.hpNow=100000;Player.single.ab.currentStat.dmg=100;
            Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=0;
            var dragon=PetCindrel.single;if(dragon){dragon.StopAllCoroutines();if(freezePets)dragon.skeletonAnimation.timeScale=0;}
            foreach(var wolf in UnityEngine.Object.FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None)){wolf.StopAllCoroutines();wolf.skeleton.timeScale=0;}
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.ab&&!enemy.ab.isDying){
                enemy.StopAttacking();enemy.ab.currentStat.def=0;enemy.ab.currentStat.hpMax=100000;
            }
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static IEnumerator Guard(IEnumerator routine){while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){EditorApplication.UnlockReloadAssemblies();Log(result);running=false;holdCombat=false;SessionState.SetBool("BEAST_LEADER_RETURN",true);EditorApplication.isPlaying=false;}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static IEnumerator Run()
    {
        var gm=GameManager.single;var gc=GameplayCanvas.single;var s=SkillsController.Instance;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
        s.ResetSkills();s.cindrelLevel=1;gm.skillPoints=20;
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.skillsPanel.GetComponent<StorybookSkillsUI>();var node=ui.nodes.Single(n=>n.key==SkillsTypeEnum.BeastPackLeader);
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.5f);
            s.beastPackLeaderLevel=0;gm.skillPoints=20;ui.Refresh();ui.layout.Apply();ui.treeScroll.verticalNormalizedPosition=0;ui.treeScroll.velocity=Vector2.zero;
            yield return new WaitForSecondsRealtime(.4f);ui.Select(node);yield return new WaitForSecondsRealtime(.7f);
            Check(node.displayName=="兽群领袖"&&ui.description.text.Contains("影狼、幽灵狼群和书脊龙"),"leader identity and scope "+ratio);
            Check(!ui.currentValues.transform.parent.gameObject.activeSelf&&ui.nextValues.text.Contains("4%"),"unlearned shows 4 percent "+ratio);
            yield return new WaitForEndOfFrame();Shot(ratio+"-unlearned");
            var feedback=ui.purchase.GetComponent<ShelterButtonFeedback>();var pointer=new PointerEventData(EventSystem.current);var scale=ui.purchase.transform.localScale;
            feedback.OnPointerDown(pointer);Check(ui.purchase.transform.localScale.x<scale.x,"pressed feedback "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-pressed");feedback.OnPointerUp(pointer);
            ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Check(s.beastPackLeaderLevel==1&&gm.skillPoints==19&&ui.currentValues.text.Contains("4%")&&ui.nextValues.text.Contains("8%"),"learn and next rank "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-learned");
            gm.skillPoints=0;ui.Refresh();Check(!ui.purchase.interactable,"disabled "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-disabled");
            gm.skillPoints=20;ui.Refresh();Check(ui.purchase.interactable,"restored "+ratio);
            s.beastPackLeaderLevel=9;ui.Refresh();ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
            Check(s.beastPackLeaderLevel==10&&gm.skillPoints==18&&!ui.purchase.interactable&&ui.currentValues.text.Contains("44%")&&!ui.nextValues.transform.parent.gameObject.activeSelf,"max rank 44 percent "+ratio);
            yield return new WaitForEndOfFrame();Shot(ratio+"-max");ui.CloseDrawer();yield return new WaitForSecondsRealtime(.4f);Check(!ui.drawerGroup.blocksRaycasts,"closed raycasts "+ratio);
        }
        Check((int)SkillsTypeEnum.BeastPackLeader==15,"stable save slot 15");
        Check(!ui.nodes.Single(n=>n.key==SkillsTypeEnum.PetCindrel).descriptionTemplate.Contains("爪"),"dragon detail omits innate claw");
        for(int level=0;level<=10;level++){
            s.beastPackLeaderLevel=level;float expected=1+s.GetSkillValues(level,SkillsTypeEnum.BeastPackLeader)[0];
            foreach(var key in new[]{SkillsTypeEnum.PetCindrel,SkillsTypeEnum.PetShadowWolf,SkillsTypeEnum.GhostWolfPack})Check(Mathf.Approximately(s.GetSummonDamageMultiplier(key),expected),"shared multiplier "+key+" rank "+level);
            Check(s.GetSummonDamageMultiplier(SkillsTypeEnum.Lightning_Demi_God)==1&&s.GetSummonDamageMultiplier(SkillsTypeEnum.Sinister_Stampede)==1,"non-summons unaffected rank "+level);
        }
        Check(PetCindrel.AttackForRoll(0)=="Attack2"&&PetCindrel.AttackForRoll(.199999f)=="Attack2"&&PetCindrel.AttackForRoll(.2f)=="Attack1"&&PetCindrel.AttackForRoll(.999f)=="Attack1","exact fixed 20 percent boundary");
        Router.single.CallCampFromSkills();yield return new WaitForSecondsRealtime(2);
        s.ResetSkills();s.cindrelLevel=s.shadowWolfLevel=s.ghostWolfPackLevel=1;holdCombat=true;freezePets=true;
        var wm=WaveManager.single;var fixture=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        fixture.levelKey="BeastLeaderQA";fixture.levelName="Beast Leader QA";fixture.mapGraphicKey="CH1_Node05";
        fixture.waves=new[]{new Wave{enemies=new[]{"MobTallowkin","MobBramblet","MobMurmlet"}},new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.Add(fixture);
        LevelController.Instance.AssetManager.HideMapPackage();if(gc.homeUI)gc.homeUI.gameObject.SetActive(false);gc.ToggleButtonGrid(false);gc.ToggleFusionBarUI(false);
        wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        var dragon=PetCindrel.single;var wolf=UnityEngine.Object.FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None).Single(w=>w.damageSkill==SkillsTypeEnum.PetShadowWolf);
        Check(dragon&&wolf,"both permanent summons present");dragon.myDmg=150;wolf.damageMultiplier=1.5f;
        var target=gm.GetLivingEnemies(false)[0];float dragonBase=0,wolfBase=0;
        foreach(int rank in new[]{0,1,10}){
            s.beastPackLeaderLevel=rank;float multiplier=s.GetSummonDamageMultiplier(SkillsTypeEnum.PetCindrel);
            target.currentStat.hpNow=40000;UnityEngine.Random.InitState(510);dragon.AnimationOnHit("Attack1");float dragonDamage=40000-target.currentStat.hpNow;
            target.currentStat.hpNow=40000;UnityEngine.Random.InitState(510);
            var entry=wolf.skeleton.AnimationState.SetAnimation(0,"Attack1",false);
            typeof(WolfPetCompanion).GetField("pendingHit",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(wolf,true);
            typeof(WolfPetCompanion).GetMethod("OnEvent",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(wolf,new object[]{entry,new Spine.Event(0,new Spine.EventData("OnHit"))});
            float wolfDamage=40000-target.currentStat.hpNow;
            if(rank==0){dragonBase=dragonDamage;wolfBase=wolfDamage;}
            Check(Mathf.Abs(dragonDamage-dragonBase*multiplier)<=1,"dragon real damage rank "+rank+" = "+dragonDamage);
            Check(Mathf.Abs(wolfDamage-wolfBase*multiplier)<=1,"shadow wolf real damage rank "+rank+" = "+wolfDamage);
            foreach(var t in gm.GetLivingEnemies(false))t.currentStat.hpNow=40000;
            var targets=gm.GetLivingEnemies(false);float hp=targets.Sum(t=>t.currentStat.hpNow);var pack=GhostWolfPack.Cast(Player.single,100);
            int hits=0;pack.Impact+=(w,t)=>hits++;
            while(pack)yield return null;
            float total=hp-targets.Sum(t=>t.currentStat.hpNow);float expected=250*multiplier;
            Check(hits==5&&Mathf.Abs(total-expected)<=expected*.11f+3,"pack five impacts amplify base plus compensation rank "+rank+" = "+total);
        }
        // Replaying the same RNG stream across rank changes must select the same attacks.
        string baseline=null;var rng=UnityEngine.Random.state;
        foreach(int rank in new[]{0,1,10}){
            s.beastPackLeaderLevel=rank;s.cindrelLevel=rank==0?1:rank;UnityEngine.Random.InitState(1729);
            var choices=new System.Text.StringBuilder();int claws=0;bool noBreath=true;
            for(int i=0;i<1000;i++){dragon.currentAnimation="";typeof(PetCindrel).GetMethod("Attack",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(dragon,null);choices.Append(dragon.currentAnimation);if(dragon.currentAnimation=="Attack2")claws++;noBreath &= dragon.currentAnimation!="Attack3";}
            Check(noBreath,"no breath selection rank "+rank);
            if(baseline==null)baseline=choices.ToString();else Check(choices.ToString()==baseline,"upgrades cannot change claw selection rank "+rank);
            Check(claws>150&&claws<250,"fixed-rate claw sample rank "+rank+" count="+claws);
        }
        UnityEngine.Random.state=rng;
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.4f);
            var assets=LevelController.Instance.AssetManager;var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(Player.single,camera);
            int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))assets.battleStyle.PlaceEnemy(enemy,camera,slot++);dragon.GetComponent<PetCindrelCompanion>().Place();wolf.Place();
            yield return new WaitForSecondsRealtime(3);
            foreach(var clip in new[]{"Attack1","Attack2"}){
                dragon.currentAnimation="";dragon.skeletonAnimation.timeScale=1;freezePets=false;var fx=dragon.GetComponent<PetCindrelEffects>();int hits=fx.ImpactCount;
                dragon.SetAnimation(clip,false,false);float hitAt=PetCindrelEffects.HitTime(dragon.skeletonAnimation.Skeleton.Data.FindAnimation(clip));
                yield return new WaitForSecondsRealtime(hitAt+.10f);yield return new WaitForEndOfFrame();Shot(ratio+"-"+clip);
                Check(fx.ImpactCount==hits+1,"retained attack has one real impact "+ratio+" "+clip);
                yield return new WaitForSecondsRealtime(2);freezePets=true;
            }
        }
        dragon.currentAnimation="";dragon.SetAnimation("Idle1",true,false);dragon.SetAnimation("Attack3",false,false);
        Check(dragon.currentAnimation=="Idle1","retired breath cannot be played by combat controller");
        target.currentStat.hpNow=40000;dragon.AnimationOnHit("Attack3");Check(target.currentStat.hpNow==40000,"retired breath cannot deal damage");
        var deps=AssetDatabase.GetDependencies("Assets/DarkFairytale/Companions/PetCindrel/Prefabs/PetCindrel.prefab",true);
        Check(!deps.Any(x=>x.Contains("/EmberBreath/")),"dragon runtime prefab no longer depends on breath effect");
        Finish("PASS leader UI at three ratios, all summon multipliers, real damage, compensation, fixed claw selection, no breath, retained single fireball/claw");
    }
}
