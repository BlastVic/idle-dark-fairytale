using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit file requests are a local editor command interface, not an automatic rebuild.
[InitializeOnLoad]
public static class Chapter01EnvironmentWorkRequests
{
    static Chapter01EnvironmentWorkRequests(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("CH1_ENV_CLEANUP",false)){
            SessionState.SetBool("CH1_ENV_CLEANUP",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            string scene=SessionState.GetString("CH1_ENV_RETURN_SCENE","");if(!string.IsNullOrEmpty(scene) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=scene)EditorSceneManager.OpenScene(scene);
        }
        string path=Chapter01EnvironmentBuilder.Output+"/command.request";
        if(!File.Exists(path))return;
        string command=File.ReadAllText(path).Trim();File.Delete(path);
        try{
            if(command=="build")Chapter01EnvironmentBuilder.Build();
            else if(command=="live")Chapter01GameplayProbe.Begin();
            else if(command=="validate")Chapter01EnvironmentValidation.Validate();
            else if(command=="motion")Chapter01EnvironmentValidation.CaptureMotion();
            else if(command=="stop")EditorApplication.isPlaying=false;
            else throw new Exception("Unknown chapter environment command: "+command);
            File.WriteAllText(Chapter01EnvironmentBuilder.Output+"/command-result.txt","PASS "+command);
        }catch(Exception e){File.WriteAllText(Chapter01EnvironmentBuilder.Output+"/command-result.txt","FAIL "+command+"\n"+e);Debug.LogException(e);}
    }
}
