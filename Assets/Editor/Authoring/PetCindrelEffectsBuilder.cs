using System;
using System.IO;
using System.Linq;
using System.Text;
using Spine.Unity;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PetCindrelEffectsBuilder
{
 public const string Root=PetCindrelBuilder.Root+"/Effects";
 public const string Out="output/pet-effects";
 public static readonly string[] Names={"EmberBolt","EmberImpact","ClawArc","AshDust"};
 static T Asset<T>(string p,Func<T> create) where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(p);if(a)return a;a=create();AssetDatabase.CreateAsset(a,p);return a;}
 public static void Build(string only=null){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play before FX build");
  Directory.CreateDirectory(Out);AssetDatabase.Refresh();
  foreach(var id in Names){
   if(only!=null && id!=only)continue;
   string folder=Root+"/"+id;
   var atlas=Asset(folder+"/"+id+"_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());
   atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/"+id+".atlas.txt");
   var pages=File.ReadAllLines(folder+"/"+id+".atlas.txt").Where(l=>l.EndsWith(".png")).ToArray();
   var materials=pages.Select(page=>{
    string p=folder+"/"+page;var ti=(TextureImporter)AssetImporter.GetAtPath(p);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.sRGBTexture=true;ti.mipmapEnabled=false;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
    var mat=Asset(p+".mat",()=>new Material(Shader.Find("DarkFairytale/Book Ember Spine")));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    if(id=="AshDust"){mat.SetColor("_Shadow",new Color(.60f,.60f,.58f));mat.SetColor("_Mid",new Color(.86f,.86f,.83f));mat.SetColor("_Light",new Color(1f,1f,.97f));mat.SetFloat("_Opacity",1f);}else mat.SetFloat("_Opacity",.85f);
    EditorUtility.SetDirty(mat);return mat;
   }).ToArray();
   atlas.materials=materials;atlas.Clear();EditorUtility.SetDirty(atlas);
   var data=Asset(folder+"/"+id+"_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/"+id+".json");data.atlasAssets=new AtlasAssetBase[]{atlas};data.scale=.01f;data.defaultMix=0;data.Clear();EditorUtility.SetDirty(data);
   var sd=data.GetSkeletonData(false);if(sd==null)throw new Exception("Cannot load "+id);
   if(only=="ClawArc") {
    string path=folder+"/"+id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
    try {var fx=root.GetComponent<PetCindrelSpineEffect>();var sa=fx.skeleton;sa.skeletonDataAsset=data;sa.Initialize(true);sa.transform.localPosition=Vector3.zero;sa.transform.localScale=Vector3.one;fx.duration=sd.FindAnimation("texiao").Duration;
     bool any=false;Bounds bounds=new Bounds();
     for(int i=0;i<=60;i++){sa.AnimationState.ClearTracks();sa.Skeleton.SetToSetupPose();sa.AnimationState.SetAnimation(0,"texiao",false);sa.Update(fx.duration*i/60f);sa.LateUpdate();foreach(var v in sa.GetComponent<MeshFilter>().sharedMesh.vertices){if(!any){bounds=new Bounds(v,Vector3.zero);any=true;}else bounds.Encapsulate(v);}}
     if(!any)throw new Exception("Claw has no geometry");fx.motionBounds=bounds;PrefabUtility.SaveAsPrefabAsset(root,path);
    }finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   if(!File.Exists(folder+"/"+id+".prefab")){
    var root=new GameObject(id);var sa=SkeletonAnimation.NewSkeletonAnimationGameObject(data);sa.transform.SetParent(root.transform,false);sa.Initialize(false);
    var fx=root.AddComponent<PetCindrelSpineEffect>();fx.skeleton=sa;fx.duration=sd.FindAnimation("texiao").Duration;
    bool any=false;Bounds b=new Bounds();
    for(int i=0;i<=60;i++){sa.AnimationState.ClearTracks();sa.Skeleton.SetToSetupPose();sa.AnimationState.SetAnimation(0,"texiao",false);sa.Update(fx.duration*i/60f);sa.LateUpdate();foreach(var v in sa.GetComponent<MeshFilter>().sharedMesh.vertices){if(float.IsNaN(v.x)||float.IsInfinity(v.y))throw new Exception("Invalid FX geometry "+id);if(!any){b=new Bounds(v,Vector3.zero);any=true;}else b.Encapsulate(v);}}
    fx.motionBounds=b;PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);
   }
  }
  AssetDatabase.SaveAssets();if(only==null)Bind();Render();
 }
 public static void Bind(){
  string path="Assets/DarkFairytale/Companions/PetCindrel/Prefabs/PetCindrel.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{var controller=root.GetComponent<PetCindrelEffects>();if(!controller)controller=root.AddComponent<PetCindrelEffects>();controller.skeleton=root.GetComponentInChildren<SkeletonAnimation>();
   controller.emberBolt=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(Root+"/EmberBolt/EmberBolt.prefab");
   controller.emberImpact=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(Root+"/EmberImpact/EmberImpact.prefab");
   controller.clawArc=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(Root+"/ClawArc/ClawArc.prefab");
   controller.ashDust=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(Root+"/AshDust/AshDust.prefab");
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 public static void Render(){
  Directory.CreateDirectory(Out);var scene=EditorSceneManager.NewPreviewScene();var report=new StringBuilder();
  try{
   var camGo=new GameObject("FX render camera");SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.12f,.15f);cam.transform.position=new Vector3(0,0,-20);
   foreach(var id in Names){var prefab=AssetDatabase.LoadAssetAtPath<PetCindrelSpineEffect>(Root+"/"+id+"/"+id+".prefab");var fx=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(fx.gameObject,scene);fx.Play(5,10);cam.orthographicSize=Mathf.Max(1.6f,fx.motionBounds.size.y/fx.motionBounds.size.x*2.7f);var strip=new Texture2D(1600,320,TextureFormat.RGBA32,false);
    for(int i=0;i<5;i++){fx.skeleton.AnimationState.ClearTracks();fx.skeleton.Skeleton.SetToSetupPose();fx.skeleton.AnimationState.SetAnimation(0,"texiao",false);fx.skeleton.Update(fx.duration*(i+.5f)/5);fx.skeleton.LateUpdate();
     var rt=RenderTexture.GetTemporary(320,320,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t=new Texture2D(320,320,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,320,320),0,0);t.Apply();strip.SetPixels(i*320,0,320,320,t.GetPixels());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=null;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);}
    strip.Apply();File.WriteAllBytes(Out+"/"+id+"-strip.png",strip.EncodeToPNG());UnityEngine.Object.DestroyImmediate(strip);report.AppendLine("PASS "+id+" duration="+fx.duration+" bounds="+fx.motionBounds);UnityEngine.Object.DestroyImmediate(fx.gameObject);
   }
  }finally{EditorSceneManager.ClosePreviewScene(scene);}File.WriteAllText(Out+"/resources-validation.txt",report.ToString());
 }
}
