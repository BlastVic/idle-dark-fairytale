using System;
using System.Collections;
using System.IO;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Purchasing;
using Scripts.Level;
using UnityEditor;
using UnityEngine;
using PlayerPrefs = UnityEngine.PlayerPrefs;

[InitializeOnLoad]
public static class StatAllocationPersistenceValidation
{
    const string Out="output/stat-allocation-persistence/", Key="codex_stat_allocation_qa", Flag="STAT_PERSIST_QA", Stage="STAT_PERSIST_STAGE";
    static bool running;static double deadline;
    static StatAllocationPersistenceValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Flag,false))return;
        running=false;
        if(SessionState.GetInt(Stage,0)==0){
            Check(PlayerPrefs.GetString("savedata_v3","")==SessionState.GetString("STAT_PERSIST_ORIGINAL",""),"Real player save unchanged");
            PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(Key+"_lastbattle");PlayerPrefs.Save();
            SessionState.EraseString("STAT_ALLOCATION_QA_SAVE_KEY");SessionState.EraseString("STAT_PERSIST_ORIGINAL");SessionState.SetBool(Flag,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            File.AppendAllText(Out+"validation.txt","DONE\n");
        }
    };}
    static void Check(bool value,string message){File.AppendAllText(Out+"validation.txt",(value?"PASS ":"FAIL ")+message+"\n");}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
            File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");
            SessionState.SetString("STAT_PERSIST_ORIGINAL",PlayerPrefs.GetString("savedata_v3",""));
            var original=PlayerPrefs.GetString("savedata_v3",JsonUtility.ToJson(new UserData()));PlayerPrefs.SetString(Key,original);
            SessionState.SetString("STAT_ALLOCATION_QA_SAVE_KEY",Key);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool(Flag,true);SessionState.SetInt(Stage,1);deadline=EditorApplication.timeSinceStartup+180;
            EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Flag,false))return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetInt(Stage,0)==2){SessionState.SetInt(Stage,3);EditorApplication.isPlaying=true;}
        if(EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(Run());}
        if(deadline>0&&EditorApplication.timeSinceStartup>deadline){Check(false,"Timeout");SessionState.SetInt(Stage,0);EditorApplication.isPlaying=false;}
    }
    static IEnumerator Run(){
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        var gc=GameplayCanvas.single;var home=gc.homeUI;var gm=GameManager.single;
        if(!home.attributesExpanded){home.ToggleAttributes();yield return new WaitForSecondsRealtime(.4f);}
        if(SessionState.GetInt(Stage,0)==1){
            var legacy=JsonUtility.FromJson<UserData>("{\"hpLevel\":2,\"statPoints\":7}");Check(legacy.LastAllocatedStat==LevelTypeEnum.NONE,"Legacy save defaults to no previous allocation");
            foreach(LevelTypeEnum type in new[]{LevelTypeEnum.HP,LevelTypeEnum.DMG,LevelTypeEnum.DEF,LevelTypeEnum.ATK_SPD,LevelTypeEnum.CRIT,LevelTypeEnum.CRIT_DMG,LevelTypeEnum.NONE}){
                var data=new UserData{LastAllocatedStat=type};Check(JsonUtility.FromJson<UserData>(JsonUtility.ToJson(data)).LastAllocatedStat==type,"Round-trip "+type);
            }
            Check(JsonUtility.FromJson<UserData>("{\"lastAllocatedStatId\":999}").LastAllocatedStat==LevelTypeEnum.NONE,"Invalid history safely defaults to NONE");
            gm.statPoints=7;gm.lastStatPurchased=LevelTypeEnum.NONE;gm.dmgLevel=0;gm.hpLevel=0;gm.CalcPlayer();home.Refresh();
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            home.statButtons[1].onClick.Invoke();
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
            var saved=JsonUtility.FromJson<UserData>(PlayerPrefs.GetString(Key));Check(saved.LastAllocatedStat==LevelTypeEnum.DMG&&saved.dmgLevel==1&&saved.statPoints==6,"Purchase immediately saves history, level and point deduction together");
            Check(!home.CanBuy(1)&&home.CanBuy(0),"Only last allocated stat blocked");
            SessionState.SetInt(Stage,2);EditorApplication.isPlaying=false;yield break;
        }
        Check(gm.lastStatPurchased==LevelTypeEnum.DMG&&gm.dmgLevel==1&&gm.statPoints==6,"Fresh play session restores allocation and remaining points");
        home.Refresh();Check(!home.CanBuy(1)&&!home.statButtons[1].interactable&&home.sharedAttributes.unavailableBackgrounds[1].gameObject.activeSelf,"Home repeat restriction and whole-cell state restored");
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);var equipment=gc.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        if(!equipment.attributesExpanded){equipment.ToggleAttributes();yield return new WaitForSecondsRealtime(.4f);}
        equipment.RefreshStats();Check(!equipment.CanBuy(1)&&!equipment.statButtons[1].interactable,"Equipment respects loaded history");
        int points=gm.statPoints;equipment.statButtons[1].onClick.Invoke();Check(gm.statPoints==points&&gm.dmgLevel==1,"Repeated purchase cannot bypass UI guard");
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);equipment.statButtons[0].onClick.Invoke();SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        var next=JsonUtility.FromJson<UserData>(PlayerPrefs.GetString(Key));Check(next.LastAllocatedStat==LevelTypeEnum.HP&&next.hpLevel==1&&next.statPoints==5,"Different stat replaces persisted history");
        Check(equipment.CanBuy(1)&&!equipment.CanBuy(0),"Previous stat becomes available again");
        gm.lastStatPurchased=LevelTypeEnum.NONE;SaveManager.Instance.Load();Check(gm.lastStatPurchased==LevelTypeEnum.HP,"Reload restores newest history");
        SaveManager.Instance.ResetStats();Check(gm.lastStatPurchased==LevelTypeEnum.NONE,"Rebirth/new game stat reset clears history");
        SessionState.SetInt(Stage,0);EditorApplication.isPlaying=false;
    }
}
