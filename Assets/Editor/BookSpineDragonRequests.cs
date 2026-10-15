using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class BookSpineDragonRequests {
 const string Dir="output/monster-animation/MobBookSpineDragon/";
 static BookSpineDragonRequests(){EditorApplication.update+=Tick;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  string path=Dir+"command.request";if(!File.Exists(path))return;string cmd=File.ReadAllText(path).Trim();File.Delete(path);
  try {
   if(cmd=="snapshot"){var report=new System.Text.StringBuilder();report.AppendLine("playing="+EditorApplication.isPlaying);var camera=LevelController.Instance?LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>():null;foreach(var sk in UnityEngine.Object.FindObjectsByType<Spine.Unity.SkeletonAnimation>(FindObjectsSortMode.None)){if(!sk.GetComponentInParent<Player>()&&!sk.GetComponentInParent<Enemy>()&&!sk.GetComponentInParent<PetDragon>())continue;var r=sk.GetComponent<MeshRenderer>();report.AppendLine(sk.transform.root.name+" local="+sk.transform.localScale+" pos="+sk.transform.position+" bounds="+r.bounds+" viewportMin="+(camera?camera.WorldToViewportPoint(r.bounds.min):Vector3.zero)+" viewportMax="+(camera?camera.WorldToViewportPoint(r.bounds.max):Vector3.zero));}File.WriteAllText(Dir+"snapshot.txt",report.ToString());}
   else if(cmd=="stop")EditorApplication.isPlaying=false;
   else if(cmd=="effects-build")BookPetEffectsBuilder.Build();
   else if(cmd=="effects-render")BookPetEffectsBuilder.Render();
   else if(cmd=="build")BookSpineDragonBuilder.Build();
   else if(cmd=="validate")BookSpineDragonBuilder.Validate();
   else if(cmd=="bind")BookSpineDragonBuilder.Bind();
   else if(cmd=="preview"||cmd=="combat"||cmd=="layout"||cmd=="effects")BookSpineDragonAcceptance.Begin(cmd);
   else if(cmd=="setup")BookSpineDragonBuilder.RefreshTestScene();
   else if(cmd=="play"){EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);EditorApplication.isPlaying=true;}
   else throw new Exception("Unknown command "+cmd);
   File.WriteAllText(Dir+"command-result.txt","PASS "+cmd);
  } catch(Exception e){File.WriteAllText(Dir+"command-result.txt","FAIL "+cmd+"\n"+e);Debug.LogException(e);}
 }
}
