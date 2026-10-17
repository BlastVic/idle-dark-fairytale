using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class MobStillmereWorkRequests {
 const string Dir="output/monster-animation/MobStillmere/";
 static MobStillmereWorkRequests(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.SetBool("MOBSTILLMERE_START_COMBAT",false);}
 };}
 static void Tick(){
  if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("MOBSTILLMERE_RETURN_TO_TEST",false)) {
   SessionState.SetBool("MOBSTILLMERE_RETURN_TO_TEST",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
   EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);return;
  }
  if(EditorApplication.isPlaying && SessionState.GetBool("MOBSTILLMERE_START_COMBAT",false) && WaveManager.single && Player.single && Player.single.ab.currentStat!=null) {
   var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();
   if(intro && !intro.Finished)return;
   SessionState.SetBool("MOBSTILLMERE_START_COMBAT",false);MobStillmereCombatProbe.Begin();return;
  }
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  var p=Dir+"command.request";if(!File.Exists(p))return;
  string command=File.ReadAllText(p).Trim();File.Delete(p);
  try {
   if(command=="stop"){EditorApplication.isPlaying=false;}
   else if(command=="references"){if(EditorApplication.isPlaying)throw new Exception("Exit Play first");MobStillmereBuilder.References();}
   else if(command=="build"){MobStillmereBuilder.Build();}
   else if(command=="configure"){MobStillmereBuilder.ConfigureBossBar();AssetDatabase.SaveAssets();}
   else if(command=="validate"){MobStillmereBuilder.Validate();}
   else if(command=="preview"){if(EditorApplication.isPlaying)throw new Exception("Exit Play first");Directory.CreateDirectory("output/monster-preview-scale");File.WriteAllText("output/monster-preview-scale/command.request","validate MobStillmere");}
   else if(command=="combat"){
    if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
    SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("MOBSTILLMERE_START_COMBAT",true);
    EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");EditorApplication.isPlaying=true;
   }
   else if(command=="setup"){MobStillmereBuilder.RefreshTestScene();}
   else if(command=="play"){EditorApplication.isPlaying=true;}
   else throw new Exception("Unknown explicit command");
   File.WriteAllText(Dir+"command-result.txt","PASS "+command);
  }catch(Exception e){File.WriteAllText(Dir+"command-result.txt","FAIL "+command+"\n"+e);Debug.LogException(e);}
 }
}
