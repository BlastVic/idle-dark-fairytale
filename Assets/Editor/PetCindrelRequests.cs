using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class PetCindrelRequests {
 const string Dir="output/monster-animation/PetCindrel/";
 static PetCindrelRequests(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  string path=Dir+"command.request";if(!File.Exists(path))return;string cmd=File.ReadAllText(path).Trim();File.Delete(path);
  try {
   if(cmd=="snapshot"){var report=new System.Text.StringBuilder();report.AppendLine("playing="+EditorApplication.isPlaying);var camera=LevelController.Instance?LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>():null;foreach(var sk in UnityEngine.Object.FindObjectsByType<Spine.Unity.SkeletonAnimation>(FindObjectsSortMode.None)){if(!sk.GetComponentInParent<Player>()&&!sk.GetComponentInParent<Enemy>()&&!sk.GetComponentInParent<PetCindrel>())continue;var r=sk.GetComponent<MeshRenderer>();report.AppendLine(sk.transform.root.name+" local="+sk.transform.localScale+" pos="+sk.transform.position+" bounds="+r.bounds+" viewportMin="+(camera?camera.WorldToViewportPoint(r.bounds.min):Vector3.zero)+" viewportMax="+(camera?camera.WorldToViewportPoint(r.bounds.max):Vector3.zero));}File.WriteAllText(Dir+"snapshot.txt",report.ToString());}
   else if(cmd=="stop")EditorApplication.isPlaying=false;
   else if(cmd=="claw-build")PetCindrelEffectsBuilder.Build("ClawArc");
   else if(cmd=="effects-build")PetCindrelEffectsBuilder.Build();
   else if(cmd=="effects-render")PetCindrelEffectsBuilder.Render();
   else if(cmd=="build")PetCindrelBuilder.Build();
   else if(cmd=="validate")PetCindrelBuilder.Validate();
   else if(cmd=="bind")PetCindrelBuilder.Bind();
   else if(cmd=="preview"||cmd=="combat"||cmd=="layout"||cmd=="effects")PetCindrelAcceptance.Begin(cmd);
   else if(cmd=="setup")PetCindrelBuilder.RefreshTestScene();
   else if(cmd=="play"){EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);EditorApplication.isPlaying=true;}
   else throw new Exception("Unknown command "+cmd);
   File.WriteAllText(Dir+"command-result.txt","PASS "+cmd);
  } catch(Exception e){File.WriteAllText(Dir+"command-result.txt","FAIL "+cmd+"\n"+e);Debug.LogException(e);}
 }
}
