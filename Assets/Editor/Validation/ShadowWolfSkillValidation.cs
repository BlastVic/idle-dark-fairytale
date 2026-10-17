using System;
using System.Collections;
using System.IO;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class ShadowWolfSkillValidation
{
    const string Dir="output/shadow-wolf-skill/";
    static bool running, holdCombat;
    static double deadline;
    static string mode;
    static readonly int[] Width={720,768,720}, Height={1280,1024,1440};
    static ShadowWolfSkillValidation(){EditorApplication.update+=Tick;}
    static void Log(string text){File.AppendAllText(Dir+mode+".txt",text+"\n");}
    static void Check(bool value,string text){if(!value)throw new Exception(text);Log("PASS "+text);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var request=Dir+"command.request";
        if(File.Exists(request)){
            var cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try {
                if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
                if(cmd=="apply"){ShadowWolfSkillAuthoring.Apply();File.WriteAllText(Dir+"apply.txt","PASS");return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop first");
                if(cmd!="before"&&cmd!="validate"&&cmd!="combat"&&cmd!="icon")throw new Exception("Unknown command");
                SessionState.SetString("SHADOW_SKILL_QA",cmd);
                SessionState.SetString("SHADOW_SKILL_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){File.WriteAllText(Dir+"command-error.txt",e.ToString());}
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("SHADOW_SKILL_RETURN",false)){
            SessionState.SetBool("SHADOW_SKILL_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            var scene=SessionState.GetString("SHADOW_SKILL_SCENE","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);return;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetString("SHADOW_SKILL_QA","")!=""&&GameManager.single&&GameplayCanvas.single){
            mode=SessionState.GetString("SHADOW_SKILL_QA","");SessionState.SetString("SHADOW_SKILL_QA","");running=true;
            File.WriteAllText(Dir+mode+".txt","Isolated QA, persistent save disabled\n");deadline=EditorApplication.timeSinceStartup+240;
            GameManager.single.StartCoroutine(Guard(Run()));
        }
        if(running&&holdCombat&&Player.single&&!Player.single.isCampPlayer&&!Player.single.ab.isDying){
            Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=0;Player.single.ab.currentStat.hpNow=100000;
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy.ab!=null&&!enemy.ab.isDying){enemy.StopAttacking();enemy.ab.currentStat.def=0;enemy.ab.currentStat.hpMax=100000;if(enemy.ab.currentStat.hpNow<1000)enemy.ab.currentStat.hpNow=100000;}
        }
        if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");
    }
    static IEnumerator Guard(IEnumerator routine){while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){Log(result);running=false;SessionState.SetBool("SHADOW_SKILL_RETURN",true);EditorApplication.isPlaying=false;}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+mode+"-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static WolfPetCompanion Wolf()=>UnityEngine.Object.FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None).FirstOrDefault(p=>p.damageSkill==SkillsTypeEnum.PetShadowWolf&&!p.Exiting);
    static void FreezeCombat(){
        Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=0;Player.single.ab.currentStat.hpNow=100000;
        foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)){e.StopAttacking();e.ab.currentStat.def=0;e.ab.currentStat.hpMax=100000;e.ab.currentStat.hpNow=100000;}
    }
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
        s.ResetSkills();s.sinisterStampedeLevel=1;gm.skillPoints=20;
        if(mode!="combat"){
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.skillsPanel.GetComponent<StorybookSkillsUI>();
        var node=ui.nodes.Single(n=>n.key==SkillsTypeEnum.PetShadowWolf);
        Check(ui.isActiveAndEnabled,"normal Skills route");
        if(mode!="before"){
            Check(node.displayName=="召唤：影狼"&&node.effectLabels.Single()=="攻击伤害","authored name and effect label");
            Check(node.icon.sprite&&node.icon.material==UnityEngine.UI.Graphic.defaultGraphicMaterial,"production icon with standard UI material");
            Check(s.GetSkillValues(1,node.key)[0]==1.5f&&s.GetSkillValues(10,node.key)[0]==4f,"level 1 / 10 values");
            Check((int)node.key==2&&node.prerequisite==SkillsTypeEnum.Sinister_Stampede,"stable save slot and prerequisite");
        }
        for(int i=0;i<3;i++){
            StorybookEquipmentValidation.Resolution(Width[i],Height[i]);yield return new WaitForSecondsRealtime(.5f);
            s.shadowWolfLevel=0;gm.skillPoints=20;ui.Refresh();ui.layout.Apply();ui.treeScroll.verticalNormalizedPosition=1;ui.treeScroll.velocity=Vector2.zero;yield return new WaitForSecondsRealtime(.4f);
            ui.Select(node);yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();Shot(i+"-unlearned");
            Check(!ui.currentValues.transform.parent.gameObject.activeSelf,"unlearned current section hidden "+i);
            if(mode=="before"){ui.CloseDrawer();yield return new WaitForSecondsRealtime(.4f);continue;}
            var feedback=ui.purchase.GetComponent<ShelterButtonFeedback>();var data=new PointerEventData(EventSystem.current);var scale=ui.purchase.transform.localScale;
            feedback.OnPointerDown(data);Check(ui.purchase.transform.localScale.x<scale.x,"pressed feedback "+i);yield return new WaitForEndOfFrame();Shot(i+"-pressed");feedback.OnPointerUp(data);
            ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Check(s.shadowWolfLevel==1&&gm.skillPoints==19,"learn consumes one point "+i);
            Check(ui.currentValues.text.Contains("150%")&&ui.nextValues.text.Contains("175%"),"current/next damage "+i);
            yield return new WaitForEndOfFrame();Shot(i+"-learned");
            gm.skillPoints=0;ui.Refresh();yield return new WaitForSecondsRealtime(.2f);Check(!ui.purchase.interactable,"no points disables "+i);yield return new WaitForEndOfFrame();Shot(i+"-disabled");
            gm.skillPoints=20;ui.Refresh();Check(ui.purchase.interactable,"points restore button "+i);
            s.shadowWolfLevel=9;ui.Refresh();ui.purchase.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
            Check(s.shadowWolfLevel==10&&gm.skillPoints==18&&!ui.purchase.interactable,"max level costs two points "+i);
            Check(!ui.nextValues.transform.parent.gameObject.activeSelf&&ui.currentValues.text.Contains("400%"),"max hides target "+i);yield return new WaitForEndOfFrame();Shot(i+"-max");
            ui.CloseDrawer();yield return new WaitForSecondsRealtime(.5f);Check(!ui.drawerGroup.blocksRaycasts&&!ui.drawer.gameObject.activeSelf,"closed drawer raycasts "+i);
        }
        if(mode=="before"||mode=="icon"){Finish("PASS skill icon screenshots and UI checks at three ratios");yield break;}
        Router.single.CallCampFromSkills();yield return new WaitForSecondsRealtime(2);
        }
        s.ResetSkills();
        var wm=WaveManager.single;var fixture=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        fixture.levelKey="ShadowWolfSkillQA";fixture.levelName="Shadow Wolf QA";fixture.mapGraphicKey="CH1_Node05";
        fixture.waves=new[]{new Wave{enemies=new[]{"MobTallowkin"}},new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.Add(fixture);
        LevelController.Instance.AssetManager.HideMapPackage();if(gc.homeUI)gc.homeUI.gameObject.SetActive(false);gc.ToggleButtonGrid(false);gc.ToggleFusionBarUI(false);
        holdCombat=true;wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);
        FreezeCombat();Check(!Wolf(),"unlearned does not spawn wolf");
        gm.BattleCleanup();yield return null;
        s.shadowWolfLevel=1;s.cindrelLevel=1;
        holdCombat=true;wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);FreezeCombat();
        var wolf=Wolf();Check(wolf&&PetCindrel.single,"both learned companions spawn from skill initialization");
        Check(Mathf.Approximately(wolf.damageMultiplier,1.5f),"wolf reads level 1 multiplier");
        PetCindrel.single.StopAllCoroutines();PetCindrel.single.skeletonAnimation.timeScale=0;
        var target=gm.GetLivingEnemies(false)[0];var hp=target.currentStat.hpNow;
        yield return new WaitForSecondsRealtime(4);
        var loss=hp-target.currentStat.hpNow;Check(loss>0,"wolf animation hits reduce real enemy HP");Log("Observed wolf damage="+loss+", player attack="+Player.single.ab.currentStat.dmg);
        for(int i=0;i<3;i++){
            StorybookEquipmentValidation.Resolution(Width[i],Height[i]);yield return new WaitForSecondsRealtime(.4f);
            var assets=LevelController.Instance.AssetManager;var cam=assets.battleCampCamera.GetComponent<Camera>();
            assets.battleStyle.PlacePlayer(Player.single,cam);int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))assets.battleStyle.PlaceEnemy(enemy,cam,slot++);
            wolf.Place();PetCindrel.single.GetComponent<PetCindrelCompanion>().Place();yield return new WaitForSecondsRealtime(3);yield return new WaitForEndOfFrame();Shot(i+"-combat");
            s.regenLevel=s.angelOnMyShoulderLevel=s.devilOnMyShoulderLevel=s.lightningDemiGodlevel=1;
            yield return null;yield return new WaitForEndOfFrame();Shot(i+"-all-status-icons");
            var status=UnityEngine.Object.FindFirstObjectByType<BattleStatusDetails>();
            Check(status.keys.Length==6&&status.skillSlots.All(slot=>slot.activeSelf),"six learned status icons "+i);
            s.regenLevel=s.angelOnMyShoulderLevel=s.devilOnMyShoulderLevel=s.lightningDemiGodlevel=0;

        }
        wolf.StopAllCoroutines();target.Hit(1000000);yield return new WaitForSecondsRealtime(5);FreezeCombat();
        Check(gm.GetLivingEnemies(false).Count>0&&!wm.victoryCalled,"second wave proceeds");Check(Wolf()==wolf,"wave transition retains one wolf");
        foreach(var enemy in gm.GetLivingEnemies(false))enemy.Hit(1000000);
        yield return new WaitForSecondsRealtime(5);Check(wm.victoryCalled,"victory flow");gc.GoToCampFromVictoryOrDefeat();yield return new WaitForSecondsRealtime(3);
        Check(!Wolf()&&!UnityEngine.Object.FindFirstObjectByType<WolfPetCompanion>(),"victory return cleans companions");
        s.shadowWolfLevel=10;s.cindrelLevel=0;s.ressurrectionLevel=0;Player.single.skeletonAnimation.timeScale=1;
        holdCombat=true;wm.CallStartLevel(fixture.levelKey);yield return new WaitForSecondsRealtime(4);FreezeCombat();wolf=Wolf();Check(wolf&&Mathf.Approximately(wolf.damageMultiplier,4f),"new battle uses level 10 multiplier");
        // Exercise the real death/animation-complete resurrection route with a deterministic successful roll.
        s.ressurrectionLevel=10;int seed=0;
        while(true){UnityEngine.Random.InitState(seed);if(UnityEngine.Random.Range(0,1f)<.4f)break;seed++;}
        UnityEngine.Random.InitState(seed);holdCombat=false;Player.single.skeletonAnimation.timeScale=1;
        Player.single.ab.Death();Check(Player.single.ab.ressurrectionWaiting,"successful resurrection roll");
        float reviveDeadline=Time.realtimeSinceStartup+12;
        while((Player.single.ab.isDying||!Wolf())&&Time.realtimeSinceStartup<reviveDeadline)yield return null;
        yield return new WaitForSecondsRealtime(.2f);
        holdCombat=true;Check(!Player.single.ab.isDying&&Wolf()&&Wolf()!=wolf,"resurrection summons a new learned wolf");
        Check(UnityEngine.Object.FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None).Count(p=>!p.Exiting)==1,"resurrection has one active wolf");
        wolf=Wolf();s.ressurrectionLevel=0;
        holdCombat=false;Player.single.skeletonAnimation.timeScale=1;Player.single.ab.currentStat.block=0;Player.single.ab.currentStat.hpNow=1;Player.single.ab.Hit(1000000);
        yield return new WaitForSecondsRealtime(.2f);Check(wolf&&wolf.Exiting,"death cancels wolf attack and exits");yield return new WaitForSecondsRealtime(2);Check(!wolf,"death removes wolf");
        Finish("PASS "+mode+": 3 ratios, normal summon, damage, upgrades, two waves, victory, resurrection and death cleanup");
    }
}
