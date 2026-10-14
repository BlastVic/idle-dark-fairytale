using PlayerPrefs = UnityEngine.PlayerPrefs;
using System.Collections;
using System.IO;
using Assets.Scripts.Services;
using Assets.Scripts.Game.Enum;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SubjectNumberValidation
{
    const string Out="output/subject-number/", Flag="SUBJECT_NUMBER_QA", Key="codex_subject_number_qa";
    static bool running; static double deadline;
    static SubjectNumberValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Flag,false))return;
            running=false;
            if(SessionState.GetInt(Flag+"stage",0)==2)return;
            Check(PlayerPrefs.GetString("savedata_v3","")==SessionState.GetString(Flag+"original",""),"Real save unchanged");
            PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(Key+"_lastbattle");PlayerPrefs.Save();
            SessionState.EraseString("STAT_ALLOCATION_QA_SAVE_KEY");SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.SetBool(Flag,false);
            File.AppendAllText(Out+"validation.txt","DONE\n");
        };
    }
    static void Check(bool ok,string text)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
            File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");
            SessionState.SetString(Flag+"original",PlayerPrefs.GetString("savedata_v3",""));
            PlayerPrefs.SetString(Key,PlayerPrefs.GetString("savedata_v3",JsonUtility.ToJson(new UserData())));
            SessionState.SetString("STAT_ALLOCATION_QA_SAVE_KEY",Key);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
            SessionState.SetInt(Flag+"stage",1);SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Flag,false))return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetInt(Flag+"stage",0)==2){SessionState.SetInt(Flag+"stage",3);EditorApplication.isPlaying=true;}
        if(EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Check(false,"Timeout");SessionState.SetInt(Flag+"stage",0);EditorApplication.isPlaying=false;}
    }
    static IEnumerator Run()
    {
        var gm=GameManager.single;var gc=GameplayCanvas.single;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        gc.CancelInvoke("TryShowingXpBoosterNotification");
        if(SessionState.GetInt(Flag+"stage",0)==3){
            Check(gm.playerDeathCount==2&&gm.SubjectNumber=="003","Death count survives Play restart and rebirth save");
            SessionState.SetInt(Flag+"stage",0);EditorApplication.isPlaying=false;yield break;
        }
        var old=JsonUtility.FromJson<UserData>("{\"playerLevel\":1}");
        Check(old.playerDeathCount==0,"Old save without death count starts at zero");
        gm.playerDeathCount=0;gc.homeUI.Refresh();Check(gm.SubjectNumber=="001"&&gc.homeUI.numberText.text=="No. 001","Initial portrait number 001");
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
        SaveManager.Instance.SaveDeathProgress();
        SkillsController.Instance.ressurrectionLevel=0;
        for(int death=1;death<=2;death++){
            gm.lastBattleKey="Khorasan Ruins I";
            Router.single.LastBattle();
            float end=Time.realtimeSinceStartup+20;
            while((!Player.single||Player.single.isCampPlayer||gm.gameState!=GameStateType.BATTLE)&&Time.realtimeSinceStartup<end)yield return null;
            Check(Player.single&&gm.gameState==GameStateType.BATTLE,"Battle loaded");
            var actor=Player.single.ab;actor.Death();actor.Death();
            Check(gm.playerDeathCount==death,"Player death counted once despite duplicate call");
            Check(JsonUtility.FromJson<UserData>(PlayerPrefs.GetString(Key)).playerDeathCount==death,"Death persisted immediately");
            end=Time.realtimeSinceStartup+15;while(gm.gameState!=GameStateType.DEFEAT&&Time.realtimeSinceStartup<end)yield return null;
            yield return Router.single.GoToCampFromBattle();yield return new WaitForSecondsRealtime(.5f);
        }
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
            gc.homeUI.Refresh();Check(gc.homeUI.numberText.text=="No. 003","Home number at "+size);
            yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
        }
        yield return Router.single.GoToInventory();yield return null;
        Check(gc.inventoryPanel.GetComponent<StorybookEquipmentUI>().number.text=="No. 003","Equipment shares death number");
        yield return Router.single.GoToSkills();yield return new WaitForSecondsRealtime(.3f);
        Check(gc.skillsPanel.GetComponent<StorybookSkillsUI>().subjectNumber.text=="No. 003","Skills shares death number");
        gm.playerDeathCount=999;Check(gm.SubjectNumber=="1000","Number grows beyond three digits");gm.playerDeathCount=2;
        gm.playerLevel=60;gm.rebirthLevel=0;LevelController.Instance.ProcessRebirth();
        Check(gm.playerLevel==1&&gm.rebirthLevel==1&&gm.playerDeathCount==2,"Real rebirth preserves lifetime death count");
        Check(JsonUtility.FromJson<UserData>(PlayerPrefs.GetString(Key)).playerDeathCount==2,"Rebirth save retains number");
        SessionState.SetInt(Flag+"stage",2);EditorApplication.isPlaying=false;
    }
}
