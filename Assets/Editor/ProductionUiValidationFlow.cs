using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit audit run; restores the user's previously open scenes after the verification sequence.
[InitializeOnLoad]
public static class ProductionUiValidationFlow
{
    const string Dir="output/ui-production-audit/",Key="UI_PRODUCTION_FLOW",SaveKey="codex_ui_production_qa";
    [Serializable] class SavedScene {public string path;public bool loaded,active;}
    [Serializable] class State {public SavedScene[] scenes;public string previousSaveKey;public int index;public bool started;public double deadline;}
    static readonly string[] Requests={"output/home-ui/visual.request","output/equipment-ui/validate.request","output/skills-ui/validate.request","output/map-ui/validate.request","output/chest-ui/validate.request","output/fusion-ui/validate.request","output/rebirth-ui/validate.request","output/shop-ui/validate.request","output/settings-ui/validate.request","output/experience-ui/validate.request","output/tutorial-ui/validate.request","output/daily-reward/validate.request","output/daily-supply/validate.request","output/packaging-fix/validate.request"};
    static readonly string[] Results={"output/home-ui/validation.txt","output/equipment-ui/validation.txt","output/skills-ui/validation.txt","output/map-ui/validation.txt","output/chest-ui/validation.txt","output/fusion-ui/validation.txt","output/rebirth-ui/validation.txt","output/shop-ui/validation.txt","output/settings-ui/validation.txt","output/experience-ui/validation.txt","output/tutorial-ui/validation.txt","output/daily-reward/validation.txt","output/daily-supply/validation.txt","output/packaging-fix/validation.txt"};
    static ProductionUiValidationFlow(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        string saved=SessionState.GetString(Key,"");
        if(string.IsNullOrEmpty(saved)){
            if(!File.Exists(Dir+"validate-flow.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Dir+"validate-flow.request");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty){File.WriteAllText(Dir+"flow-result.txt","BLOCKED: unsaved scene edits; no scenes changed");return;}
            var state=new State{previousSaveKey=SessionState.GetString("STAT_ALLOCATION_QA_SAVE_KEY",""),scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>new SavedScene{path=s.path,loaded=s.isLoaded,active=s.isActive}).ToArray()};string originalKey=string.IsNullOrEmpty(state.previousSaveKey)?"savedata_v3":state.previousSaveKey;
            string originalBattleKey=originalKey=="savedata_v3"?"savedata_lastbattle":originalKey+"_lastbattle";
            Directory.CreateDirectory(Dir+"save-safety");string snapshot=Dir+"save-safety/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.WriteAllText(snapshot+".json",PlayerPrefs.GetString(originalKey,""));File.WriteAllText(snapshot+"-lastbattle.txt",PlayerPrefs.GetString(originalBattleKey,""));
            if(PlayerPrefs.HasKey(originalKey))PlayerPrefs.SetString(SaveKey,PlayerPrefs.GetString(originalKey));else PlayerPrefs.DeleteKey(SaveKey);
            if(PlayerPrefs.HasKey(originalBattleKey))PlayerPrefs.SetString(SaveKey+"_lastbattle",PlayerPrefs.GetString(originalBattleKey));else PlayerPrefs.DeleteKey(SaveKey+"_lastbattle");
            SessionState.SetString("STAT_ALLOCATION_QA_SAVE_KEY",SaveKey);
            File.WriteAllText(Dir+"flow-result.txt","Running; previous scene setup will be restored.\n");SessionState.SetString(Key,JsonUtility.ToJson(state));EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");return;
        }
        var current=JsonUtility.FromJson<State>(saved);
        if(current.index>=Requests.Length){if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorSceneManager.RestoreSceneManagerSetup(current.scenes.Select(s=>new SceneSetup{path=s.path,isLoaded=s.loaded,isActive=s.active}).ToArray());if(string.IsNullOrEmpty(current.previousSaveKey))SessionState.EraseString("STAT_ALLOCATION_QA_SAVE_KEY");else SessionState.SetString("STAT_ALLOCATION_QA_SAVE_KEY",current.previousSaveKey);PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.DeleteKey(SaveKey+"_lastbattle");SessionState.EraseString(Key);File.AppendAllText(Dir+"flow-result.txt","COMPLETE; previous scene setup restored.\n");return;}
        if(!current.started){if(EditorApplication.isPlayingOrWillChangePlaymode)return;Directory.CreateDirectory(Path.GetDirectoryName(Requests[current.index]));if(File.Exists(Results[current.index]))File.Copy(Results[current.index],Dir+"previous-validation-"+current.index+".txt",true);File.Delete(Results[current.index]);File.WriteAllText(Requests[current.index],"");current.started=true;current.deadline=EditorApplication.timeSinceStartup+650;SessionState.SetString(Key,JsonUtility.ToJson(current));return;}
        if(File.Exists(Requests[current.index]))return;
        bool timeout=EditorApplication.timeSinceStartup>current.deadline;
        if(timeout&&EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!timeout&&(SessionState.GetBool("SETTINGS_QA_PENDING",false)||SessionState.GetBool("SETTINGS_QA",false)))return;
        string result=File.Exists(Results[current.index])?File.ReadAllText(Results[current.index]):"Missing result";
        bool complete=result.Contains("COMPLETE")||result.Contains("DONE")||result.Contains("all map checks")||result.Contains("final visual + interaction regression");
        if(!complete)result+="\nFAIL incomplete validation; no completion marker.\n";
        File.WriteAllText(Dir+"final-validation-"+current.index+".txt",result);
        File.AppendAllText(Dir+"flow-result.txt",Requests[current.index]+": "+(timeout?"TIMEOUT":result.Contains("FAIL")||result.Contains("ERROR")?"FAIL (see detailed log)":"finished; see detailed log")+"\n");
        current.index++;current.started=false;SessionState.SetString(Key,JsonUtility.ToJson(current));
    }
}
