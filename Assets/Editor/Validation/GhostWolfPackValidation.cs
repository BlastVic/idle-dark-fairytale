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
public static class GhostWolfPackValidation
{
    const string Dir="output/ghost-wolf-pack/";
    static bool running,holdCombat,freezeAnimation;
    static string mode;
    static double deadline;
    static readonly int[] Width={720,768,720}, Height={1280,1024,1440};
    static GhostWolfPackValidation(){EditorApplication.update+=Tick;}
    static void Log(string message)=>File.AppendAllText(Dir+mode+".txt",message+"\n");
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Log("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var request=Dir+"command.request";
        if(File.Exists(request)){
            var cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{
                if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
                if(cmd=="apply"){GhostWolfPackAuthoring.Apply();File.WriteAllText(Dir+"apply.txt","PASS");return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop first");
                if(cmd!="before"&&cmd!="validate"&&cmd!="dragon-name"&&cmd!="icon")throw new Exception("Unknown command");
                SessionState.SetString("GHOST_PACK_QA",cmd);
                SessionState.SetString("GHOST_PACK_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("GHOST_PACK_RETURN",false)){
            SessionState.SetBool("GHOST_PACK_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            var scene=SessionState.GetString("GHOST_PACK_SCENE","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);return;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetString("GHOST_PACK_QA","")!=""&&GameManager.single&&GameplayCanvas.single){
            mode=SessionState.GetString("GHOST_PACK_QA","");SessionState.SetString("GHOST_PACK_QA","");running=true;holdCombat=false;
            deadline=EditorApplication.timeSinceStartup+240;File.WriteAllText(Dir+mode+".txt","Actual Unity QA, save writes disabled\n");
            GameManager.single.StartCoroutine(Guard(Run()));
        }
        if(running&&holdCombat&&Player.single&&!Player.single.isCampPlayer&&!Player.single.ab.isDying){
            Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=freezeAnimation?0:1;
            Player.single.ab.currentStat.hpNow=100000;Player.single.ab.currentStat.dmg=100;Player.single.ab.currentStat.crit=0;Player.single.ab.currentStat.atkSpd=100;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.ab&&!enemy.ab.isDying){
                enemy.StopAttacking();enemy.ab.currentStat.def=0;enemy.ab.currentStat.hpMax=100000;if(enemy.ab.currentStat.hpNow<1000)enemy.ab.currentStat.hpNow=100000;
            }
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static IEnumerator Guard(IEnumerator routine){while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){Log(result);running=false;holdCombat=false;SessionState.SetBool("GHOST_PACK_RETURN",true);EditorApplication.isPlaying=false;}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static GhostWolfPack Pack()=>UnityEngine.Object.FindFirstObjectByType<GhostWolfPack>();
    static void Attack(int previousActions)
    {
        var p=Player.single;typeof(Player).GetMethod("ResetCombatActions",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,null);p.StopAllCoroutines();p.isAttacking=false;p.currentAnimation="";p.attackIndexer=previousActions;p.skeletonAnimation.timeScale=1;
        typeof(Player).GetMethod("Attack",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,null);
    }
    static IEnumerator Run()
    {
        var gm=GameManager.single;var gc=GameplayCanvas.single;var skills=SkillsController.Instance;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
        skills.ResetSkills();skills.devilOnMyShoulderLevel=1;skills.sinisterStampedeLevel=1;gm.skillPoints=20;
        if(mode=="dragon-name")skills.ressurrectionLevel=1;
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.skillsPanel.GetComponent<StorybookSkillsUI>();var node=ui.nodes.Single(n=>(int)n.key==(mode=="dragon-name"?9:8));
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.4f);
            skills.ghostWolfPackLevel=0;gm.skillPoints=20;ui.Refresh();ui.layout.Apply();ui.treeScroll.verticalNormalizedPosition=mode=="dragon-name"?.25f:.7f;ui.treeScroll.velocity=Vector2.zero;
            yield return new WaitForSecondsRealtime(.4f);
            ui.Select(node);yield return new WaitForSecondsRealtime(.7f);yield return new WaitForEndOfFrame();Shot(ratio+"-unlearned");
            if(mode=="before"||mode=="dragon-name"){if(mode=="dragon-name")Check(node.displayName=="召唤：书脊龙","dragon skill name "+ratio);ui.CloseDrawer();yield return new WaitForSecondsRealtime(.4f);continue;}
            Check(node.displayName=="召唤：幽灵狼群"&&node.effectLabels.Single()=="基础冲撞伤害","authored skill identity "+ratio);
            Check(!ui.currentValues.transform.parent.gameObject.activeSelf,"unlearned hides current section "+ratio);
            var feedback=ui.purchase.GetComponent<ShelterButtonFeedback>();var pointer=new PointerEventData(EventSystem.current);var scale=ui.purchase.transform.localScale;
            feedback.OnPointerDown(pointer);Check(ui.purchase.transform.localScale.x<scale.x,"pressed state "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-pressed");feedback.OnPointerUp(pointer);
            ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Check(skills.ghostWolfPackLevel==1&&gm.skillPoints==19,"learn and cost "+ratio);
            Check(ui.currentValues.text.Contains("30%")&&ui.nextValues.text.Contains("35%"),"per-wolf level values "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-learned");
            gm.skillPoints=0;ui.Refresh();yield return new WaitForSecondsRealtime(.2f);Check(!ui.purchase.interactable,"disabled state "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-disabled");
            gm.skillPoints=20;ui.Refresh();Check(ui.purchase.interactable,"button restored "+ratio);
            skills.ghostWolfPackLevel=9;ui.Refresh();ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
            Check(skills.ghostWolfPackLevel==10&&gm.skillPoints==18&&!ui.purchase.interactable&&ui.currentValues.text.Contains("80%"),"max upgrade "+ratio);
            Check(!ui.nextValues.transform.parent.gameObject.activeSelf,"max hides target "+ratio);yield return new WaitForEndOfFrame();Shot(ratio+"-max");
            ui.CloseDrawer();yield return new WaitForSecondsRealtime(.5f);Check(!ui.drawerGroup.blocksRaycasts&&!ui.drawer.gameObject.activeSelf,"closed drawer "+ratio);
        }
        if(mode=="before"||mode=="dragon-name"){Finish("PASS name screenshots at three ratios");yield break;}
        if(mode=="icon"){Finish("PASS illustrated icon UI at three ratios");yield break;}
        Check((int)SkillsTypeEnum.GhostWolfPack==8,"stable persisted enum slot");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GhostWolfPackAuthoring.PrefabPath);
        Check(prefab.GetComponent<GhostWolfPack>().wolves.Length==5&&prefab.GetComponentsInChildren<WolfPetCompanion>(true).Length==0,"five authored runners without pet auto-attack");
        Check(!AssetDatabase.GetDependencies(GhostWolfPackAuthoring.PrefabPath,true).Any(p=>p.StartsWith("Assets/ArtSources/")),"no runtime source-art dependencies");
        Router.single.CallCampFromSkills();yield return new WaitForSecondsRealtime(2);
        skills.ResetSkills();holdCombat=freezeAnimation=true;
        var wm=WaveManager.single;var fixture=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        fixture.levelKey="GhostWolfPackQA";fixture.levelName="Ghost Wolf Pack QA";fixture.mapGraphicKey="CH1_Node05";
        fixture.waves=new[]{new Wave{enemies=new[]{"MobTallowkin","MobBramblet","MobMurmlet"}},new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.Add(fixture);
        LevelController.Instance.AssetManager.HideMapPackage();if(gc.homeUI)gc.homeUI.gameObject.SetActive(false);gc.ToggleButtonGrid(false);gc.ToggleFusionBarUI(false);
        wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        Attack(2);Check(Player.single.currentAnimation!="Skill2"&&!Pack(),"unlearned third action does not cast");
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(Width[ratio],Height[ratio]);yield return new WaitForSecondsRealtime(.4f);
            var assets=LevelController.Instance.AssetManager;var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(Player.single,camera);
            int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))assets.battleStyle.PlaceEnemy(enemy,camera,slot++);
            yield return new WaitForSecondsRealtime(3);
            skills.ghostWolfPackLevel=ratio==2?10:1;freezeAnimation=false;
            Attack(0);Check(Player.single.currentAnimation!="Skill2","first action stays normal "+ratio);
            Attack(1);Check(Player.single.currentAnimation!="Skill2","second action stays normal "+ratio);
            var targets=gm.GetLivingEnemies(false);var hp=targets.Sum(t=>t.currentStat.hpNow);
            Attack(2);Check(Player.single.currentAnimation=="Skill2","third action plays Skill2 "+ratio);
            yield return new WaitForSecondsRealtime(.5f);Check(!Pack()&&Mathf.Approximately(hp,targets.Sum(t=>t.currentStat.hpNow)),"no damage or pack before cast event "+ratio);
            float end=Time.realtimeSinceStartup+6;while(!Pack()&&Time.realtimeSinceStartup<end)yield return null;
            var pack=Pack();Check(pack,"Skill2 event spawns pack "+ratio);
            Check(pack.wolves.Length==5&&pack.DamageHits==0&&Mathf.Approximately(hp,targets.Sum(t=>t.currentStat.hpNow)),"spawn causes no premature damage "+ratio);
            Player.single.AnimationOnHit("Skill2");Check(UnityEngine.Object.FindObjectsByType<GhostWolfPack>(FindObjectsSortMode.None).Length==1,"duplicate event does not double cast "+ratio);
            int hits=0;bool premature=false;var hitWolves=new HashSet<GhostWolfCharge>();var hitTargets=new HashSet<Actor_Base>();
            pack.Impact+=(wolf,target)=>{
                float x=wolf.MuzzleViewportX(camera);if(x<pack.impactViewportX-.0001f)premature=true;
                hits++;hitWolves.Add(wolf);if(target)hitTargets.Add(target);
                Log("Impact "+hits+" "+wolf.name+" muzzleX="+x+" target="+(target?target.name:"none"));
            };
            float began=Time.realtimeSinceStartup;bool captured=false;end=began+6;
            while(pack&&Time.realtimeSinceStartup<end){
                if(!captured&&Time.realtimeSinceStartup-began>=1.05f){yield return new WaitForEndOfFrame();Shot(ratio+"-charge");captured=true;}
                yield return null;
            }
            Check(!pack,"all runners exit right and root cleans up "+ratio);
            Check(hits==5&&hitWolves.Count==5&&!premature,"exactly five distinct crossing impacts "+ratio);
            Check(hitTargets.Count>1,"random impacts reach multiple enemies "+ratio);
            float totalDamage=hp-targets.Sum(t=>t.currentStat.hpNow);float nominal=(ratio==2?400:150)+Player.single.SkillCompensation("Skill2");
            Check(totalDamage>=nominal*.9f-3&&totalDamage<=nominal*1.1f+3,"only five skill damage packets, total="+totalDamage+" "+ratio);
            freezeAnimation=true;
        }
        // Cancellation before the first crossing must leave all targets unharmed.
        var living=gm.GetLivingEnemies(false);float before=living.Sum(t=>t.currentStat.hpNow);
        var cancelled=GhostWolfPack.Cast(Player.single);Check(cancelled,"cancellation fixture");
        Player.single.ab.isDying=true;yield return new WaitForSecondsRealtime(.3f);
        Check(!cancelled&&Mathf.Approximately(before,living.Sum(t=>t.currentStat.hpNow)),"death cancels the pack before impact");Player.single.ab.isDying=false;
        var cleanup=GhostWolfPack.Cast(Player.single);gm.BattleCleanup();yield return null;Check(!cleanup,"battle cleanup destroys pending pack");
        wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        Check(Player.single.attackIndexer<=1,"new battle resets third-action cadence");
        // Exhaust one wave while a pack is in flight. Its later runners must not hit the next wave.
        var previousWave=GhostWolfPack.Cast(Player.single);foreach(var enemy in gm.GetLivingEnemies(false))enemy.Hit(1000000);
        yield return new WaitForSecondsRealtime(5);Check(!previousWave&&gm.GetLivingEnemies(false).Count==1&&!wm.victoryCalled,"pack finishes across wave transition");
        foreach(var enemy in gm.GetLivingEnemies(false))enemy.Hit(1000000);
        yield return new WaitForSecondsRealtime(4);Check(wm.victoryCalled,"victory still completes");gc.GoToCampFromVictoryOrDefeat();yield return new WaitForSecondsRealtime(3);Check(!Pack(),"camp has no leftover pack");
        Finish("PASS UI and 3 ratios; Skill2 timing; five random collision hits; no early/double damage; offscreen, death, wave and camp cleanup");
    }
}
