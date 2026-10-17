using System;
using System.IO;
using System.Linq;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter01EnvironmentValidation
{
    [MenuItem("Tools/Dark Fairytale/Environment/Validate Chapter 01 Assets")]
    public static void Validate()
    {
        string dir=Chapter01EnvironmentBuilder.Output;Directory.CreateDirectory(dir);
        var report=new StringBuilder();
        var catalog=AssetDatabase.LoadAssetAtPath<ChapterEnvironmentCatalog>("Assets/Resources/Environment/Chapter01Environments.asset");
        if(!catalog||catalog.entries.Length!=5)throw new Exception("Five environment entries required");
        var scene=EditorSceneManager.NewPreviewScene();
        try {
            var camGo=new GameObject("Environment validation camera");SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera=camGo.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,0,-15);camera.clearFlags=CameraClearFlags.SolidColor;
            for(int n=0;n<5;n++){
                var entry=catalog.entries[n];if(!entry.prefab||entry.mapKey!="CH1_Node0"+(n+1))throw new Exception("Invalid node binding");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(entry.prefab,scene);
                try {
                    if(go.tag!="MapGraphics"||!go.GetComponent<DarkFairytaleBackdrop>())throw new Exception("Missing map lifecycle");
                    var foreground=go.transform.Find("Foreground").GetComponent<SpriteRenderer>();
                    if(foreground.sortingLayerName!="Character_Front")throw new Exception("Foreground must occlude characters");
                    foreach(var sk in go.GetComponentsInChildren<SkeletonAnimation>()){
                        sk.Initialize(true);
                        var data=sk.skeletonDataAsset.GetSkeletonData(false);
                        foreach(var animation in data.Animations){
                            sk.AnimationState.SetAnimation(0,animation.Name,false);
                            double signature=0,min=double.MaxValue,max=double.MinValue;
                            for(int f=0;f<=Mathf.CeilToInt(animation.Duration*30);f++){
                                sk.Update(1f/30);sk.LateUpdate();
                                var verts=sk.GetComponent<MeshFilter>().sharedMesh.vertices;
                                signature=0;foreach(var v in verts){if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y))throw new Exception("Invalid vertex");signature+=v.x*1.7+v.y;}
                                foreach(var slot in sk.Skeleton.Slots)signature+=slot.A*17+slot.R+slot.G;
                                min=Math.Min(min,signature);max=Math.Max(max,signature);
                            }
                            if(max-min<.00001)throw new Exception("Static animation: "+sk.name+" / "+animation.Name);
                            report.AppendLine("PASS Node"+(n+1)+" "+sk.name+" / "+animation.Name+" duration="+animation.Duration+" sampled at 30fps; vertex/color motion="+(max-min));
                        }
                    }
                    int[] widths={720,768,720},heights={1280,1024,1440};string[] names={"9x16","3x4","1x2"};
                    for(int r=0;r<3;r++){
                        camera.aspect=widths[r]/(float)heights[r];go.GetComponent<DarkFairytaleBackdrop>().Initialize(camera);
                        Sample(go,0);Render(camera,dir+"/node0"+(n+1)+"-"+names[r]+".png",widths[r],heights[r]);
                    }
                    if(n==3){Sample(go,7.8f);Render(camera,dir+"/node04-wolf-shadow.png",720,1280);}
                    if(n==2||n==3){Sample(go,10);Render(camera,dir+"/node0"+(n+1)+"-moon-open.png",720,1280);}
                    Sample(go,1);Render(camera,dir+"/node0"+(n+1)+"-motion-1.png",720,1280);
                    Sample(go,2);Render(camera,dir+"/node0"+(n+1)+"-motion-2.png",720,1280);
                    report.AppendLine("PASS Node"+(n+1)+" saved prefab, foreground, 3 aspect renders");
                }finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            report.AppendLine("PASS: all five environments validated.");
        }finally{EditorSceneManager.ClosePreviewScene(scene);File.WriteAllText(dir+"/asset-validation.txt",report.ToString());}
    }

    public static void CaptureMotion()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ChapterEnvironmentCatalog>("Assets/Resources/Environment/Chapter01Environments.asset");
        var scene=EditorSceneManager.NewPreviewScene();
        try {
            var cameraObject=new GameObject("Chapter motion camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=8;camera.aspect=9f/16;camera.transform.position=new Vector3(0,0,-15);
            for(int n=0;n<5;n++){
                var go=(GameObject)PrefabUtility.InstantiatePrefab(catalog.entries[n].prefab,scene);
                try {
                    go.GetComponent<DarkFairytaleBackdrop>().Initialize(camera);
                    string dir=Chapter01EnvironmentBuilder.Output+"/motion/node0"+(n+1);Directory.CreateDirectory(dir);
                    for(int f=0;f<264;f++){Sample(go,f/12f);Render(camera,dir+"/"+f.ToString("D4")+".png",360,640);}
                }finally{UnityEngine.Object.DestroyImmediate(go);}
            }
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    static void Sample(GameObject root,float time)
    {
        foreach(var sk in root.GetComponentsInChildren<SkeletonAnimation>()){
            sk.AnimationState.ClearTracks();sk.Skeleton.SetToSetupPose();
            string animation=sk.Skeleton.Data.FindAnimation("GazeDown")!=null?"GazeDown":"Ambient";
            var track=sk.AnimationState.SetAnimation(0,animation,true);track.TrackTime=time;sk.Update(0);sk.LateUpdate();
        }
    }
    public static void Render(Camera camera,string path,int width,int height)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
