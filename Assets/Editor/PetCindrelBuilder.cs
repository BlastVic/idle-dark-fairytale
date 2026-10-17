using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PetCindrelBuilder {
 public const string Id="PetCindrel";
 public const string Root="Assets/DarkFairytale/Companions/"+Id;
 public const string Output="output/monster-animation/"+Id;
 static T Asset<T>(string path,Func<T> make) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a)return a;a=make();AssetDatabase.CreateAsset(a,path);return a;}
 [MenuItem("Tools/Dark Fairytale/Companions/Build Book Spine Dragon")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();
  var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+Id+".png");
  ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.sRGBTexture=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=4096;ti.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+Id+".png");
  var mat=Asset(Root+"/"+Id+"_Material.mat",()=>new Material(Shader.Find("Spine/Skeleton")));
  mat.mainTexture=tex;mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);
  var atlas=Asset(Root+"/"+Id+"_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());
  atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/"+Id+".atlas.txt");atlas.materials=new[]{mat};atlas.Clear();EditorUtility.SetDirty(atlas);
  var data=Asset(Root+"/"+Id+"_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());
  data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/"+Id+".json");data.atlasAssets=new AtlasAssetBase[]{atlas};data.scale=.004f;data.defaultMix=.06f;data.Clear();EditorUtility.SetDirty(data);
  if(data.GetSkeletonData(false)==null)throw new Exception("Source runtime load failed");
  if(!File.Exists(Root+"/"+Id+".prefab")){
   var go=new GameObject(Id);go.tag="Pet";
   var sa=SkeletonAnimation.NewSkeletonAnimationGameObject(data);sa.transform.SetParent(go.transform,false);sa.initialSkinName="1";sa.Initialize(true);sa.AnimationName="Idle1";sa.loop=true;
   var pet=go.AddComponent<PetCindrel>();pet.skeletonAnimation=sa;
   var point=new GameObject("root position");point.transform.SetParent(go.transform,false);pet.rootPos=point;
   var ground=go.AddComponent<PetCindrelCompanion>();ground.skeleton=sa;
   PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+Id+".prefab");UnityEngine.Object.DestroyImmediate(go);
  }
  string guid=AssetDatabase.AssetPathToGUID(Root+"/"+Id+".prefab");var settings=AddressableAssetSettingsDefaultObject.Settings;settings.CreateOrMoveEntry(guid,settings.DefaultGroup).address=Id;EditorUtility.SetDirty(settings);
  AssetDatabase.SaveAssets();RefreshTestScene();Validate();
 }
 public static void Bind(){
  var db=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/LevelDataDescriptions.asset"));var entries=db.FindProperty("_generalObjectsDb");bool found=false;
  for(int i=0;i<entries.arraySize;i++){var e=entries.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("key").stringValue!="PetCindrel")continue;e.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(Root+"/"+Id+".prefab");found=true;}
  if(!found)throw new Exception("PetCindrel binding missing");db.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
 }
    public static void RefreshTestScene()
    {
        var scene=SceneManager.GetSceneByPath(MonsterTestSceneBuilder.ScenePath);bool opened=!scene.isLoaded;
        if(!opened && scene.isDirty)throw new Exception("Save test scene edits before registering PetCindrel.");
        if(opened)scene=EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath,OpenSceneMode.Additive);
        try {
            var tester=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonsterAnimationTest>(true)).First();
            tester.discoveryFolders=tester.discoveryFolders.Concat(new[]{Root}).Distinct().ToArray();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PetCindrel.prefab");
            tester.monsterPrefabs=tester.monsterPrefabs.Concat(new[]{prefab}).Distinct().ToArray();tester.RefreshCatalog();
            var portrait=MonsterPreviewUiBuilder.MakePortrait(AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/PetCindrel_SkeletonData.asset"),tester,true);
            MonsterPreviewUiBuilder.RefreshHudCatalog(tester);
            var hudRoot=PrefabUtility.LoadPrefabContents(MonsterPreviewUiBuilder.HudPath);
            try {var card=hudRoot.GetComponent<MonsterPreviewHud>().cards.First(c=>c.data.name=="PetCindrel_SkeletonData");card.displayName=CreatureNames.DisplayName(card.data.name);card.title.text=card.displayName;card.portrait.sprite=portrait;PrefabUtility.SaveAsPrefabAsset(hudRoot,MonsterPreviewUiBuilder.HudPath);}
            finally {PrefabUtility.UnloadPrefabContents(hudRoot);}
            if(tester.character) {
                tester.character.skeletonDataAsset=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/PetCindrel_SkeletonData.asset");
                tester.character.initialSkinName="1";tester.character.transform.localScale=Vector3.one;tester.character.Initialize(true);tester.character.AnimationName="Idle1";EditorUtility.SetDirty(tester.character);
            }

            EditorUtility.SetDirty(tester);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static void Pose(SkeletonAnimation s,string skin,string clip,float time)
    {
        s.AnimationState.ClearTracks();s.Skeleton.SetSkin(skin);s.Skeleton.SetToSetupPose();var entry=s.AnimationState.SetAnimation(0,clip,false);entry.MixDuration=0;s.Update(time);s.LateUpdate();
    }
    [MenuItem("Tools/Dark Fairytale/Monsters/PetCindrel/Validate and Render")]
    public static void Validate()
    {
        Directory.CreateDirectory(Output);var report=new StringBuilder("PetCindrel Unity validation\n");
        var scene=EditorSceneManager.NewPreviewScene();SkeletonAnimation s=null;
        try {
            var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/PetCindrel_SkeletonData.asset");
            s=SkeletonAnimation.NewSkeletonAnimationGameObject(data);SceneManager.MoveGameObjectToScene(s.gameObject,scene);s.transform.localScale=Vector3.one;
            if(s.Skeleton.Bones.Count!=78||s.Skeleton.Slots.Count!=51)throw new Exception("Unexpected rig topology");
            var all=s.Skeleton.Data.Animations.ToArray();var bound=new Bounds();bool initialized=false;
            foreach(var anim in all) {
                foreach(string skin in new[]{"default","1"}) {
                    for(int f=0;f<=Mathf.CeilToInt(anim.Duration*60);f++) {
                        Pose(s,skin,anim.Name,f/60f);var mesh=s.GetComponent<MeshFilter>().sharedMesh;
                        foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y))throw new Exception("Invalid geometry "+anim.Name);
                        var b=s.GetComponent<Renderer>().bounds;if(!initialized){bound=b;initialized=true;}else bound.Encapsulate(b);
                    }
                }
                // Linked skin must inherit all original deformation, including alternate faces.
                for(int f=0;f<5;f++) {
                    Pose(s,"default",anim.Name,anim.Duration*f/4f);var a=s.GetComponent<MeshFilter>().sharedMesh.vertices;
                    Pose(s,"1",anim.Name,anim.Duration*f/4f);var b=s.GetComponent<MeshFilter>().sharedMesh.vertices;
                    if(a.Length!=b.Length || a.Where((v,i)=>(v-b[i]).sqrMagnitude>1e-10f).Any())throw new Exception("Linked deformation mismatch "+anim.Name);
                }
                report.AppendLine(anim.Name+": PASS both skins, finite geometry, identical deformation; duration="+anim.Duration);
            }
            foreach(var name in new[]{"Idle1","Attack1","Attack2","Attack3","Hit","Death1","Exit"}) {
                int hits=0,ends=0;Spine.AnimationState.TrackEntryEventDelegate handler=(entry,e)=>{if(e.Data.Name=="OnHit")hits++;if(e.Data.Name=="OnComplete")ends++;};
                Pose(s,"1",name,0);s.AnimationState.Event+=handler;
                for(int f=0;f<Mathf.CeilToInt(s.Skeleton.Data.FindAnimation(name).Duration*60)+3;f++)s.Update(1/60f);
                s.AnimationState.Event-=handler;
                if(hits!=(name.StartsWith("Attack")?1:0)||ends!=(name=="Idle1"?0:1))throw new Exception("Wrong event count "+name);
                report.AppendLine(name+": PASS events OnHit="+hits+", OnComplete="+ends);
            }
            int leakedHits=0,completions=0;Spine.AnimationState.TrackEntryEventDelegate interrupt=(entry,e)=>{if(e.Data.Name=="OnHit")leakedHits++;if(e.Data.Name=="OnComplete")completions++;};
            Pose(s,"1","Attack1",.2f);s.AnimationState.Event+=interrupt;s.AnimationState.SetAnimation(0,"Hit",false);s.Update(3f);s.AnimationState.Event-=interrupt;
            if(leakedHits!=0||completions!=1)throw new Exception("Interrupted attack leaked hit event");
            report.AppendLine("PASS interrupted attack: 0 hits, 1 completion.");
            leakedHits=0;completions=0;Pose(s,"1","Attack1",.1f);s.AnimationState.Event+=interrupt;s.AnimationState.SetAnimation(0,"Death1",false);s.Update(3f);s.AnimationState.Event-=interrupt;
            if(leakedHits!=0||completions!=1)throw new Exception("Death interruption leaked events");
            report.AppendLine("PASS attack interrupted by death: 0 hits, 1 completion.");
            var camGo=new GameObject("PetCindrel audit camera");SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.13f,.15f,.18f);cam.transform.position=new Vector3(bound.center.x,bound.center.y,-10);cam.orthographicSize=Mathf.Max(bound.size.y/2, bound.size.x/3f)*1.12f;
            foreach(var anim in all) {
                Pose(s,"1",anim.Name,0);var motion=s.GetComponent<Renderer>().bounds;
                for(int f=1;f<=60;f++){Pose(s,"1",anim.Name,anim.Duration*f/60f);motion.Encapsulate(s.GetComponent<Renderer>().bounds);}
                cam.transform.position=new Vector3(motion.center.x,motion.center.y,-10);cam.orthographicSize=Mathf.Max(motion.size.y/2,motion.size.x/3f)*1.15f;
                RenderStrip(s,cam,anim.Name,anim.Duration);
                if(new[]{"Idle1","Attack1","Attack2","Attack3","Hit","Death1","Exit"}.Contains(anim.Name)) {
                    cam.orthographicSize=Mathf.Max(motion.size.y,motion.size.x)*.56f;
                    for(int f=0;f<6;f++){Pose(s,"1",anim.Name,anim.Duration*f/5f);Render(cam,Output+"/"+anim.Name+"-"+f+".png",768,768);}
                }
            }
            Pose(s,"1","Idle1",0);var idle=s.GetComponent<Renderer>().bounds;cam.transform.position=new Vector3(idle.center.x,idle.center.y,-10);cam.orthographicSize=Mathf.Max(idle.size.y/2,idle.size.x/3f)*1.15f;Render(cam,Output+"/idle.png",1200,800);
            report.AppendLine("PASS 78 bones, 51 slots, 24 clips, 48 skin/clip combinations; rendered all clips.");
            File.WriteAllText(Output+"/unity-validation.txt",report.ToString());Debug.Log(report.ToString());
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void RenderStrip(SkeletonAnimation s,Camera cam,string clip,float duration)
    {
        const int w=480,h=320;var sheet=new Texture2D(w*6,h,TextureFormat.RGB24,false);var rt=new RenderTexture(w,h,24);var old=RenderTexture.active;cam.targetTexture=rt;
        try{for(int i=0;i<6;i++){Pose(s,"1",clip,duration*i/5f);cam.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,w,h),i*w,0);}sheet.Apply();File.WriteAllBytes(Output+"/"+clip+".png",sheet.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);}
    }
    static void Render(Camera cam,string path,int w,int h)
    {
        var rt=new RenderTexture(w,h,24);var old=RenderTexture.active;var image=new Texture2D(w,h,TextureFormat.RGB24,false);cam.targetTexture=rt;
        try{cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
