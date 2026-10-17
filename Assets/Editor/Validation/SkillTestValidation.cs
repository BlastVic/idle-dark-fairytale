using System;
using System.Collections;
using System.IO;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SkillTestValidation
{
    const string Dir="output/skill-test/";
    static bool running;static double deadline;static string save;
    static SkillTestValidation(){EditorApplication.update+=Tick;}
    public static void Begin(bool visualOnly=false){SessionState.SetBool("SKILL_TEST_VISUAL",visualOnly);if(EditorApplication.isPlaying)throw new Exception("Stop first");save=PlayerPrefs.GetString("savedata_v3");SessionState.SetString("SKILL_TEST_SAVE",save);SessionState.SetBool("SKILL_TEST_QA",true);EditorSceneManager.OpenScene(SkillTestBuilder.ScenePath);EditorApplication.isPlaying=true;}
    static void Tick(){if(EditorApplication.isPlaying&&SessionState.GetBool("SKILL_TEST_QA",false)&&!running){var t=UnityEngine.Object.FindFirstObjectByType<SkillTest>();if(!t||!t.Ready)return;running=true;SessionState.SetBool("SKILL_TEST_QA",false);EditorApplication.LockReloadAssemblies();deadline=EditorApplication.timeSinceStartup+300;File.WriteAllText(Dir+"validation.txt","");t.StartCoroutine(Guard(Run(t)));}if(running&&EditorApplication.timeSinceStartup>deadline)Finish("FAIL timeout");}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Dir+"validation.txt","PASS "+message+"\n");}
    static IEnumerator Guard(IEnumerator work){while(true){object value;try{if(!work.MoveNext())break;value=work.Current;}catch(Exception e){Finish("FAIL "+e);yield break;}yield return value;}}
    static void Finish(string result){File.AppendAllText(Dir+"validation.txt",result+"\n");running=false;EditorApplication.UnlockReloadAssemblies();EditorApplication.isPlaying=false;}
    static void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    static IEnumerator Run(SkillTest t){
        Check(t.skillButtons.Length==15,"15 skill controls");
        Check(Time.timeSinceLevelLoad<2,"ready directly without campaign loading");
        Check(!UnityEngine.Object.FindAnyObjectByType<GameplayCanvas>(FindObjectsInactive.Include),"no GameplayCanvas loaded");
        Check(!UnityEngine.Object.FindAnyObjectByType<Router>(FindObjectsInactive.Include)&&!UnityEngine.Object.FindAnyObjectByType<WaveManager>(FindObjectsInactive.Include),"no camp/router/wave flow");
        Check(!UnityEngine.Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include)&&!UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>(FindObjectsInactive.Include),"no tutorial or prologue");
        Check(!UnityEngine.Object.FindAnyObjectByType<Assets.Scripts.Services.SaveManager>(FindObjectsInactive.Include),"no save manager loaded");
        Check(UnityEngine.SceneManagement.SceneManager.sceneCount==1,"one standalone scene");
        Check(GameManager.single.GetLivingEnemies(false).Count==3,"three training targets");
        foreach(var target in GameManager.single.GetLivingEnemies(false)){target.Hit(1e20f);Check(!target.isDying&&target.currentStat.hpNow==40,"unlimited healthless hits");Check(!target.enemyLifebar.gameObject.activeSelf,"hidden health bar");}
        yield return new WaitForSecondsRealtime(3);

        if(!SessionState.GetBool("SKILL_TEST_VISUAL",false)){
        for(int i=1;i<=15;i++){
            t.ResetTest();yield return null;t.Select((SkillsTypeEnum)i);t.SetLevel(1);yield return null;t.Cast();
            yield return new WaitForSecondsRealtime(i==6?12:6);
            Check(!Player.single.ab.isDying,"skill completes "+i);
            Check(GameManager.single.GetLivingEnemies(false).Count==3,"targets survive skill "+i);
            if(i==2||i==3||i==8||i==9||i==11||i==12||i==14||i==15)Check(t.HitCount>0,"real skill damage "+i);
            if(i==1)Check(Player.single.ab.currentStat.hpNow>300,"regen ticks");
            if(i==7)Check(Player.single.ab.currentStat.hpNow>300,"kill healing");
            if(i==13)Check(Player.single.ab.currentStat.absorb>0,"critical ward");
        }
        t.ResetTest();yield return null;
        foreach(var key in new[]{SkillsTypeEnum.Sinister_Stampede,SkillsTypeEnum.GhostWolfPack,SkillsTypeEnum.Lightning_Demi_God}){t.Select(key);yield return null;t.Cast();}
        Check(Player.single.SkillScheduler.PendingCount==2,"three animated skills queue");yield return new WaitForSecondsRealtime(13);Check(Player.single.SkillScheduler.PendingCount==0&&!Player.single.isAttacking,"queue drains fully");
        }
        int[] w={720,768,720},h={1280,1024,1440};
        for(int r=0;r<3;r++){
            StorybookEquipmentValidation.Resolution(w[r],h[r]);yield return new WaitForSecondsRealtime(.6f);t.ResetTest();t.Select(SkillsTypeEnum.GhostWolfPack);t.SetLevel(10);yield return null;
            yield return new WaitForEndOfFrame();Shot(r+"-idle");
            var b=t.castButton;var f=b.GetComponent<ShelterButtonFeedback>();var p=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);f.OnPointerDown(p);yield return new WaitForEndOfFrame();Shot(r+"-pressed");f.OnPointerUp(p);t.Cast();
            yield return new WaitForSecondsRealtime(2.5f);yield return new WaitForEndOfFrame();Shot(r+"-cast");Check(!t.resetButton.interactable,"reset disabled during cast "+r);
            yield return new WaitForSecondsRealtime(5);Check(t.resetButton.interactable,"reset restored "+r);
            t.SetCatalog(true);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Shot(r+"-catalog");Check(t.catalogGroup.blocksRaycasts&&t.catalogGroup.alpha==1,"expanded catalog "+r);
            foreach(var button in t.skillButtons){var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);Check(corners.All(v=>v.x>=-1&&v.x<=Screen.width+1&&v.y>=-1&&v.y<=Screen.height+1),"button fits "+r+" "+button.name);}
        }
            t.SetCatalog(false);yield return new WaitForSecondsRealtime(.2f);Check(!t.catalogGroup.blocksRaycasts&&!t.catalogGroup.gameObject.activeSelf,"closed catalog no raycast blocker");
        Check(!UnityEngine.Object.FindAnyObjectByType<Assets.Scripts.Services.SaveManager>(FindObjectsInactive.Include),"skills never create save manager");Check(PlayerPrefs.GetString("savedata_v3")==SessionState.GetString("SKILL_TEST_SAVE",""),"formal save unchanged");
        Finish(SessionState.GetBool("SKILL_TEST_VISUAL",false)?"PASS visual revision: three ratios, button states and save isolation":"PASS all 15 skills, repeated healthless hits, queue, levels, three ratios and save isolation");
    }
}
