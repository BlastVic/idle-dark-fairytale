using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class WolfPetRequests {
 const string Dir="output/wolf-pets/";
 static WolfPetRequests(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  var path=Dir+"command.request";if(!File.Exists(path))return;var cmd=File.ReadAllText(path).Trim();File.Delete(path);
  try{
   if(cmd=="stop")EditorApplication.isPlaying=false;
   else if(cmd=="preview"||cmd=="combat"||cmd.StartsWith("sizes-"))WolfPetAcceptance.Begin(cmd);
   else if(cmd=="build")WolfPetBuilder.BuildAll();
   else if(cmd=="play"){EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);EditorApplication.isPlaying=true;}
   else throw new Exception("Unknown command");
   File.WriteAllText(Dir+"result.txt","PASS "+cmd);
  }catch(Exception e){File.WriteAllText(Dir+"result.txt",e.ToString());Debug.LogException(e);}
 }
}
