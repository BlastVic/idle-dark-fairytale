using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SaveManager=Assets.Scripts.Services.SaveManager;

[InitializeOnLoad]
public static class ProductionUiSaveIsolationValidation
{
    const string Dir="output/ui-production-audit/save-safety/",Key="UI_SAVE_ISOLATION_CHECK";
    [Serializable] class SavedScene {public string path;public bool loaded,active;}
    [Serializable] class State {public SavedScene[] scenes;public string save,battle,previousKey;public bool hasSave,hasBattle,previousFlag;public int phase;}
    static ProductionUiSaveIsolationValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||BuildPipeline.isBuildingPlayer)return;
        string value=SessionState.GetString(Key,"");
        if(string.IsNullOrEmpty(value)){
            if(!File.Exists(Dir+"validate.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(Dir+"validate.request");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty){File.WriteAllText(Dir+"validation.txt","FAIL unsaved scene; no changes");return;}
            var s=new State{save=PlayerPrefs.GetString("savedata_v3",""),battle=PlayerPrefs.GetString("savedata_lastbattle",""),hasSave=PlayerPrefs.HasKey("savedata_v3"),hasBattle=PlayerPrefs.HasKey("savedata_lastbattle"),previousKey=SessionState.GetString("STAT_ALLOCATION_QA_SAVE_KEY",""),previousFlag=SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false),scenes=EditorSceneManager.GetSceneManagerSetup().Select(x=>new SavedScene{path=x.path,loaded=x.isLoaded,active=x.isActive}).ToArray()};
            File.WriteAllText(Dir+"validation-before.json",s.save);File.WriteAllText(Dir+"validation.txt","Check latched protection through late flag reset and OnApplicationQuit. Original data retained for recovery.\n");
            SessionState.SetString(Key,JsonUtility.ToJson(s));SessionState.EraseString("STAT_ALLOCATION_QA_SAVE_KEY");SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;return;
        }
        var state=JsonUtility.FromJson<State>(value);
        if(state.phase==0){
            if(!EditorApplication.isPlaying||!GameManager.single||!GameplayCanvas.single||!Player.single)return;
            GameManager.single.premiumCurrency+=12345;
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            SaveManager.Instance.Save();
            bool unchanged=Same(state);File.AppendAllText(Dir+"validation.txt",(unchanged?"PASS":"FAIL")+" Save remains blocked after mutable flag clears\n");
            state.phase=1;SessionState.SetString(Key,JsonUtility.ToJson(state));EditorApplication.isPlaying=false;return;
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        bool final=Same(state);File.AppendAllText(Dir+"validation.txt",(final?"PASS":"FAIL")+" OnApplicationQuit leaves real save and last battle byte-for-byte unchanged\n");
        if(!final){if(state.hasSave)PlayerPrefs.SetString("savedata_v3",state.save);else PlayerPrefs.DeleteKey("savedata_v3");if(state.hasBattle)PlayerPrefs.SetString("savedata_lastbattle",state.battle);else PlayerPrefs.DeleteKey("savedata_lastbattle");PlayerPrefs.Save();File.AppendAllText(Dir+"validation.txt","Original test-start values restored after failed isolation check.\n");}
        if(string.IsNullOrEmpty(state.previousKey))SessionState.EraseString("STAT_ALLOCATION_QA_SAVE_KEY");else SessionState.SetString("STAT_ALLOCATION_QA_SAVE_KEY",state.previousKey);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",state.previousFlag);SessionState.EraseString(Key);EditorSceneManager.RestoreSceneManagerSetup(state.scenes.Select(s=>new SceneSetup{path=s.path,isLoaded=s.loaded,isActive=s.active}).ToArray());File.AppendAllText(Dir+"validation.txt","COMPLETE\n");
    }
    static bool Same(State s)=>PlayerPrefs.HasKey("savedata_v3")==s.hasSave&&PlayerPrefs.HasKey("savedata_lastbattle")==s.hasBattle&&PlayerPrefs.GetString("savedata_v3","")==s.save&&PlayerPrefs.GetString("savedata_lastbattle","")==s.battle;
}
