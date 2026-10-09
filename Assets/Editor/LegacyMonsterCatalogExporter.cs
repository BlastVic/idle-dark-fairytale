using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Spine.Unity;
using Newtonsoft.Json.Linq;

// Explicit, read-only asset export. Never rebuilds game prefabs or scenes.
public static class LegacyMonsterCatalogExporter {
 const string CatalogDir = "ArtDirection/Monsters/LegacyCatalog";
 [MenuItem("Tools/Dark Fairytale/Monsters/Export Legacy Catalog")]
 public static void Export() { ExportDirectory(CatalogDir); }
 [MenuItem("Tools/Dark Fairytale/Monsters/Export Mob1 Skins")]
 public static void ExportMob1Skins() { ExportDirectory(CatalogDir+"/Mob1Skins"); }
 static void ExportDirectory(string Dir) {
  if(EditorApplication.isPlayingOrWillChangePlaymode) return;
  Directory.CreateDirectory(Dir+"/images");
  var scene=EditorSceneManager.NewPreviewScene();
  var log=new System.Text.StringBuilder();
  try {
   var cameraObject=new GameObject("Catalog camera");
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
   var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene; camera.orthographic=true;
   camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.10f,.12f,.15f,1); camera.nearClipPlane=.01f; camera.farClipPlane=100;
   foreach(var row in (JArray)JObject.Parse(File.ReadAllText(Dir+"/manifest.json"))["items"]) {
    GameObject go=null; RenderTexture rt=null; Texture2D tex=null; SkeletonDataAsset temporaryData=null;
    try {
     var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>((string)row["asset"]);
     if(data.atlasAssets.Length==0) {
      temporaryData=UnityEngine.Object.Instantiate(data);
      temporaryData.atlasAssets=new AtlasAssetBase[]{AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Path.GetDirectoryName((string)row["asset"])+"/skeleton_Atlas.asset")};
      temporaryData.Clear(); data=temporaryData;
     }
     var sk=SkeletonAnimation.NewSkeletonAnimationGameObject(data); go=sk.gameObject;
     UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
     sk.Skeleton.SetSkin((string)row["skin"]); sk.Skeleton.SetSlotsToSetupPose();
     string anim=(string)row["animation"];
     if(sk.Skeleton.Data.FindAnimation(anim)==null) anim=sk.Skeleton.Data.Animations.FirstOrDefault(a=>a.Name.ToLower().StartsWith("idle"))?.Name;
     if(!string.IsNullOrEmpty(anim)) sk.AnimationState.SetAnimation(0,anim,true);
     sk.Update(.1f); sk.LateUpdate();
     var bounds=go.GetComponent<MeshRenderer>().bounds;
     camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x)*.59f;
     camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-20);
     rt=new RenderTexture(640,640,24); camera.targetTexture=rt; camera.Render();
     var old=RenderTexture.active; RenderTexture.active=rt;
     tex=new Texture2D(640,640,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,640,640),0,0); tex.Apply(); RenderTexture.active=old;
     File.WriteAllBytes(Dir+"/images/"+(string)row["image"],tex.EncodeToPNG());
     log.AppendLine((string)row["id"]+" OK "+anim);
    } catch(Exception e) {log.AppendLine((string)row["id"]+" ERROR "+e);}
    finally {camera.targetTexture=null; if(go) UnityEngine.Object.DestroyImmediate(go); if(rt)UnityEngine.Object.DestroyImmediate(rt); if(tex)UnityEngine.Object.DestroyImmediate(tex); if(temporaryData)UnityEngine.Object.DestroyImmediate(temporaryData);}
   }
  } finally {EditorSceneManager.ClosePreviewScene(scene); File.WriteAllText(Dir+"/export.log",log.ToString());}
 }
}
