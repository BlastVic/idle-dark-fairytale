using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Spine.Unity;
using Scripts.Level;

/// <summary>Samples actual authored meshes after the production battle fit. Does not resize textures.</summary>
[InitializeOnLoad]
public static class SpineTextureDensityAudit
{
    const string Dir="output/ui-production-audit/";
    [Serializable] public class Row {public string actor,texture,animation,profile;public int width,height;public float maxScreenPixelsPerTexel,sampleTime;public int triangles;public float visibleAreaWeightedP99;[NonSerialized] public double[] weights=new double[256];}
    [Serializable] public class Report {public string scope="Production battle Fit; setup pose and 65 samples per animation; authored common-effect transforms. Invisible triangles and near-degenerate UV triangles excluded. Maximum is sensitive to thin triangles; inspect artwork before deriving resize decisions. Includes authored companion fit, companion effect widths, common Spine and particle effects. Area-weighted P99 is across samples; rare detailed regions must still pass visual review before any resize. Other uses and devices still require review.";public List<Row> entries=new List<Row>();public List<string> skipped=new List<string>();}
    static SpineTextureDensityAudit(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"density.request"))return;
        File.Delete(Dir+"density.request");try{Run();}catch(Exception e){File.WriteAllText(Dir+"density-error.txt",e.ToString());Debug.LogException(e);}
    };}
    [MenuItem("Tools/Resource Optimization/Measure Battle Texture Density")]
    public static void Run(){
        Directory.CreateDirectory(Dir);var report=new Report();
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Gameplay.unity");
        GameObject parent=null;
        try{
            var am=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AssetManager>(true)).First();
            var camera=am.battleCampCamera.GetComponent<Camera>();var style=am.battleStyle;
            if(!camera||!style)throw new Exception("Missing production camera/style");
            parent=new GameObject("Texture density probe (inactive)");parent.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent,scene);
            var companionPath="Assets/DarkFairytale/Companions/PetCindrel/PetCindrel.prefab";
            var companionSource=AssetDatabase.LoadAssetAtPath<GameObject>(companionPath).GetComponent<PetCindrelEffects>();
            var extras=new[]{companionPath,"Assets/Resources/Effects/DongzhuoCast.prefab"}.Concat(new[]{companionSource.emberBolt,companionSource.emberImpact,companionSource.clawArc,companionSource.ashDust,companionSource.emberBreath}.Select(e=>AssetDatabase.GetAssetPath(e.gameObject)));
            var paths=LevelDataDescriptions.EnemyDb.Select(e=>AssetDatabase.GUIDToAssetPath(e.assetRef.AssetGUID)).Concat(new[]{"Assets/Prefab/Player.prefab"}).Concat(LevelDataDescriptions.ParticleDb.Select(e=>AssetDatabase.GUIDToAssetPath(e.assetRef.AssetGUID))).Concat(extras).Distinct().ToArray();
            foreach(var path in paths){
                File.WriteAllText(Dir+"density-progress.txt",path);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Missing prefab "+path);
                foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1440,2560),new Vector2Int(1440,1920),new Vector2Int(1440,2880)}){
                    var rt=new RenderTexture(size.x,size.y,0);camera.targetTexture=rt;camera.aspect=(float)size.x/size.y;
                    var actor=UnityEngine.Object.Instantiate(prefab,parent.transform);var skeleton=actor.GetComponentInChildren<SkeletonAnimation>(true);
                    try{
                        if(!skeleton){
                            parent.SetActive(true);
                            var particleRows=new Dictionary<string,Row>();
                            foreach(var ps in actor.GetComponentsInChildren<ParticleSystem>(true)){
                                var renderer=ps.GetComponent<ParticleSystemRenderer>();if(!renderer)continue;
                                var mesh=new Mesh();try{ps.useAutoRandomSeed=false;ps.randomSeed=123;
                                    for(int i=0;i<=64;i++){float time=(ps.main.duration+ps.main.startLifetime.constantMax)*i/64f;ps.Simulate(time,false,true,true);renderer.BakeMesh(mesh,camera,false);SampleMesh(renderer.transform,mesh,renderer.sharedMaterials,camera,path,"particles",time,size,particleRows);}
                                }finally{UnityEngine.Object.DestroyImmediate(mesh);}
                            }
                            parent.SetActive(false);if(particleRows.Count==0)report.skipped.Add(path+" @ "+size+": particle renderer produced no measurable mesh");
                            report.entries.AddRange(particleRows.Values);continue;
                        }
                        skeleton.Initialize(false);skeleton.Update(0);skeleton.LateUpdate();
                        var companion=actor.GetComponent<PetCindrelCompanion>();
                        if(companion){typeof(PetCindrelCompanion).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(companion,null);typeof(PetCindrelCompanion).GetField("battleCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(companion,camera);companion.Place();}
                        var petEffect=actor.GetComponent<PetCindrelSpineEffect>();
                        if(petEffect){var sourceEffect=prefab.GetComponent<PetCindrelSpineEffect>();petEffect.Play(companionSource.EffectWorldWidth(sourceEffect,camera),0);}

                        var enemy=actor.GetComponent<Enemy>();bool player=actor.GetComponent<Player>();
                        if(enemy||player){
                            var bar=actor.GetComponentInChildren<EnemyLifebar>(true);bool boss=bar&&(bar.isBoss||bar.isMiniBoss);
                            var fit=typeof(DarkFairytaleBattleStyle).GetMethod("Fit",BindingFlags.Static|BindingFlags.NonPublic);
                            var feet=player?style.playerFeet:boss?new Vector2(.72f,.33f):style.enemyFeet[0];
                            var display=player?style.playerSize:style.EnemyDisplaySize(0,boss)*Mathf.Max(.1f,enemy.displaySizeMultiplier);
                            fit.Invoke(null,new object[]{actor.transform,skeleton,camera,feet,display});
                        }
                        var rows=new Dictionary<string,Row>();
                        Sample(skeleton,camera,path,"setup",0,size,rows);
                        foreach(var clip in skeleton.Skeleton.Data.Animations){
                            for(int i=0;i<=64;i++){
                                skeleton.AnimationState.ClearTracks();skeleton.Skeleton.SetToSetupPose();
                                float time=clip.Duration*i/64f;
                                skeleton.AnimationState.SetAnimation(0,clip.Name,false).TrackTime=time;
                                skeleton.AnimationState.Apply(skeleton.Skeleton);skeleton.Skeleton.UpdateWorldTransform();skeleton.LateUpdate();
                                Sample(skeleton,camera,path,clip.Name,time,size,rows);
                            }
                        }
                        report.entries.AddRange(rows.Values);
                    }finally{UnityEngine.Object.DestroyImmediate(actor);camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);}
                }
            }
            foreach(var row in report.entries){double total=row.weights.Sum(),sum=0;for(int i=0;i<row.weights.Length;i++){sum+=row.weights[i];if(total>0&&sum>=total*.99){row.visibleAreaWeightedP99=Mathf.Pow(2,-8+(i+1)/16f);break;}}}
            File.WriteAllText(Dir+"spine-density.json",JsonUtility.ToJson(report,true));File.WriteAllText(Dir+"density-progress.txt","COMPLETE: "+report.entries.Count+" actor/texture/profile rows");
        }finally{foreach(var raw in Raw.Values)if(raw)UnityEngine.Object.DestroyImmediate(raw);Raw.Clear();if(parent)UnityEngine.Object.DestroyImmediate(parent);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void Sample(SkeletonAnimation skeleton,Camera camera,string actor,string animation,float time,Vector2Int size,Dictionary<string,Row> rows){
        var mesh=skeleton.GetComponent<MeshFilter>().sharedMesh;
        SampleMesh(skeleton.transform,mesh,skeleton.GetComponent<MeshRenderer>().sharedMaterials,camera,actor,animation,time,size,rows);
    }
    static readonly Dictionary<string,Texture2D> Raw=new Dictionary<string,Texture2D>();
    static void SampleMesh(Transform transform,Mesh mesh,Material[] materials,Camera camera,string actor,string animation,float time,Vector2Int size,Dictionary<string,Row> rows){
        if(!mesh||mesh.vertexCount==0)return;var vertices=mesh.vertices;var colors=mesh.colors32;var uv=mesh.uv;if(uv.Length!=vertices.Length)return;
        for(int sub=0;sub<mesh.subMeshCount&&sub<materials.Length;sub++){
            var tex=materials[sub]?materials[sub].mainTexture:null;if(!tex)continue;var path=AssetDatabase.GetAssetPath(tex);if(string.IsNullOrEmpty(path))continue;
            if(!rows.TryGetValue(path,out var row)){row=new Row{actor=actor,texture=path,width=tex.width,height=tex.height,profile=size.x+"x"+size.y};rows.Add(path,row);}
            var indices=mesh.GetTriangles(sub);
            for(int i=0;i<indices.Length;i+=3){
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                if(colors.Length==vertices.Length&&Mathf.Max(colors[a].a,Mathf.Max(colors[b].a,colors[c].a))<16)continue;
                Vector2 x=camera.WorldToScreenPoint(transform.TransformPoint(vertices[a]));
                Vector2 y=(Vector2)camera.WorldToScreenPoint(transform.TransformPoint(vertices[b]))-x;
                Vector2 z=(Vector2)camera.WorldToScreenPoint(transform.TransformPoint(vertices[c]))-x;
                var u=Vector2.Scale(uv[b]-uv[a],new Vector2(tex.width,tex.height));var v=Vector2.Scale(uv[c]-uv[a],new Vector2(tex.width,tex.height));
                float det=u.x*v.y-v.x*u.y;if(Mathf.Abs(det)<4f||Mathf.Abs(y.x*z.y-y.y*z.x)<2f)continue;
                float aa=(y.x*v.y-z.x*u.y)/det,bb=(-y.x*v.x+z.x*u.x)/det,cc=(y.y*v.y-z.y*u.y)/det,dd=(-y.y*v.x+z.y*u.x)/det;
                float trace=aa*aa+bb*bb+cc*cc+dd*dd,d=aa*dd-bb*cc;
                float magnification=Mathf.Sqrt(.5f*(trace+Mathf.Sqrt(Mathf.Max(0,trace*trace-4*d*d))));
                if(!Raw.TryGetValue(path,out var alpha)){alpha=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!alpha.LoadImage(File.ReadAllBytes(path))){UnityEngine.Object.DestroyImmediate(alpha);alpha=null;}Raw[path]=alpha;}
                if(alpha){var center=(uv[a]+uv[b]+uv[c])/3;float visible=alpha.GetPixelBilinear(center.x,center.y).a;foreach(var t in new[]{Vector2.Lerp(center,uv[a],.6f),Vector2.Lerp(center,uv[b],.6f),Vector2.Lerp(center,uv[c],.6f)})visible=Mathf.Max(visible,alpha.GetPixelBilinear(t.x,t.y).a);if(visible<.05f)continue;}
                if(Mathf.Max(x.x,Mathf.Max(x.x+y.x,x.x+z.x))<0||Mathf.Min(x.x,Mathf.Min(x.x+y.x,x.x+z.x))>size.x||Mathf.Max(x.y,Mathf.Max(x.y+y.y,x.y+z.y))<0||Mathf.Min(x.y,Mathf.Min(x.y+y.y,x.y+z.y))>size.y)continue;
                row.triangles++;
                int bin=Mathf.Clamp(Mathf.FloorToInt((Mathf.Log(Mathf.Max(.0001f,magnification),2)+8)*16),0,255);row.weights[bin]+=Mathf.Min(size.x*size.y,Mathf.Abs(y.x*z.y-y.y*z.x)*.5f);
                if(magnification>row.maxScreenPixelsPerTexel){row.maxScreenPixelsPerTexel=magnification;row.animation=animation;row.sampleTime=time;}
            }
        }
    }
}
