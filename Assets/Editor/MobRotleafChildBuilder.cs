using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit tools only; never replaces original enemies or automatically rebuilds user assets.
public static class MobRotleafChildBuilder
{
    public const string Root = "Assets/DarkFairytale/Monsters/MobRotleafChild";
    public const string Source = "ArtDirection/Monsters/C031_Dummy_G/source";
    public const string Original = "../Spine/monster2/动画/C031_Dummy_G";
    public const string Output = "output/monster-animation/MobRotleafChild";
    public static readonly string[] Parts = {"Front_hat_01","Front_hat_02","Front_Body_01","Front_Body_02","Front_arm","Front_Leg_01","Front_Leg_02","shadow","Front_Mouth_01","Front_Eye_01","Front_Eye_02","Front_Eye_03","Front_Eye_04","Front_Eye_05","Front_Eye_06","Front_Mouth_02","Front_Mouth_03","Front_Mouth_05"};
    static Texture2D Read(string path) { var t = new Texture2D(2,2,TextureFormat.RGBA32,false); t.LoadImage(File.ReadAllBytes(path)); return t; }
    static RectInt Cell(int i, Texture2D t)
    {
        float scale = 440f / Mathf.Max(t.width,t.height);
        int w = Mathf.RoundToInt(t.width*scale), h = Mathf.RoundToInt(t.height*scale);
        return new RectInt(i%3*512+(512-w)/2,1536-(i/3+1)*512+(512-h)/2,w,h);
    }
    static JObject attachments;
    static float Cross(Vector2 a,Vector2 b,Vector2 c) => (a.x-c.x)*(b.y-c.y)-(b.x-c.x)*(a.y-c.y);
    static bool Inside(string part,float u,float v)
    {
        if(part=="shadow")return true;
        if(attachments==null)attachments=(JObject)JObject.Parse(File.ReadAllText(Original+"/C031_Dummy_G.json"))["skins"][0]["attachments"];
        var mesh=attachments.Properties().SelectMany(p=>((JObject)p.Value).Properties()).First(p=>(string)p.Value["path"]==part || p.Name==part).Value;
        if(mesh["uvs"]==null)return true;
        var uv=(JArray)mesh["uvs"];var tri=(JArray)mesh["triangles"];var point=new Vector2(u,v);
        for(int t=0;t<tri.Count;t+=3){
            int a=(int)tri[t]*2,b=(int)tri[t+1]*2,c=(int)tri[t+2]*2;
            var va=new Vector2((float)uv[a],(float)uv[a+1]);var vb=new Vector2((float)uv[b],(float)uv[b+1]);var vc=new Vector2((float)uv[c],(float)uv[c+1]);
            float d1=Cross(point,va,vb),d2=Cross(point,vb,vc),d3=Cross(point,vc,va);
            if(!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0)))return true;
        }
        return false;
    }
    [MenuItem("Tools/Dark Fairytale/Monsters/MobRotleafChild/Make Paint References")]
    public static void References()
    {
        Directory.CreateDirectory(Source);
        for(int page=0;page<2;page++) {
            var sheet=new Texture2D(1536,1536,TextureFormat.RGBA32,false); sheet.SetPixels(new Color[1536*1536]);
            for(int i=0;i<9 && page*9+i<Parts.Length;i++) {
                var t=Read(Original+"/images/"+Parts[page*9+i]+".png"); var r=Cell(i,t);
                for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)sheet.SetPixel(r.x+x,r.y+y,Inside(Parts[page*9+i],(x+.5f)/r.width,1-(y+.5f)/r.height)?t.GetPixelBilinear((x+.5f)/r.width,(y+.5f)/r.height):Color.clear);
                UnityEngine.Object.DestroyImmediate(t);
            }
            sheet.Apply();File.WriteAllBytes(Source+"/reference-"+page+".png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
        }
        File.WriteAllText(Source+"/parts.json",new JArray(Parts).ToString());
        Debug.Log("MOBROTLEAFCHILD_REFERENCES_READY");
    }
    static RectInt AlphaBounds(Texture2D t,RectInt r,Func<int,int,bool> include=null) {
        int x0=r.xMax,y0=r.yMax,x1=-1,y1=-1;
        for(int y=r.y;y<r.yMax;y++)for(int x=r.x;x<r.xMax;x++)if(t.GetPixel(x,y).a>.08f && (include==null||include(x,y))) {x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
        if(x1<x0)throw new Exception("Empty paint cell "+r);return new RectInt(x0,y0,x1-x0+1,y1-y0+1);
    }
    static T Asset<T>(string path, Func<T> create) where T:UnityEngine.Object
    { var item=AssetDatabase.LoadAssetAtPath<T>(path);if(item)return item;item=create();AssetDatabase.CreateAsset(item,path);return item; }

    [MenuItem("Tools/Dark Fairytale/Monsters/MobRotleafChild/Build and Register")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        Directory.CreateDirectory(Root);Directory.CreateDirectory(Output);Directory.CreateDirectory(Source+"/images");
        var sheets=new[]{Read(Source+"/painted-0.png"),Read(Source+"/painted-1.png")};
        var textures=new List<Texture2D>();
        for(int i=0;i<Parts.Length;i++) {
            var original=Read(Original+"/images/"+Parts[i]+".png");
            var crop=original;
            if(Parts[i]!="shadow") {
                var sheet=sheets[i/9];
                var search=new RectInt((i%3)*sheet.width/3,sheet.height-(i%9/3+1)*sheet.height/3,sheet.width/3,sheet.height/3);
                var painted=AlphaBounds(sheet,search);
                var target=AlphaBounds(original,new RectInt(0,0,original.width,original.height),(x,y)=>Inside(Parts[i],(x+.5f)/original.width,1-(y+.5f)/original.height));
                crop=new Texture2D(original.width*2,original.height*2,TextureFormat.RGBA32,false);
                crop.SetPixels(new Color[crop.width*crop.height]);
                int padX=Parts[i]=="Front_hat_01"?Mathf.RoundToInt(target.width*.05f):0;
                int padY=Parts[i]=="Front_hat_01"?Mathf.RoundToInt(target.height*.025f):0;
                for(int y=target.y*2+padY;y<target.yMax*2-padY;y++)for(int x=target.x*2+padX;x<target.xMax*2-padX;x++) {
                    float u=(x+.5f-target.x*2-padX)/(target.width*2-padX*2),v=(y+.5f-target.y*2-padY)/(target.height*2-padY*2);
                    crop.SetPixel(x,y,sheet.GetPixelBilinear((painted.x+u*painted.width)/sheet.width,(painted.y+v*painted.height)/sheet.height));
                }
                crop.Apply();UnityEngine.Object.DestroyImmediate(original);
            }
            File.WriteAllBytes(Source+"/images/"+Parts[i]+".png",crop.EncodeToPNG());textures.Add(crop);
        }
        var packed=new Texture2D(2,2,TextureFormat.RGBA32,false);
        var rects=packed.PackTextures(textures.ToArray(),8,4096,false);
        var atlas=new StringBuilder("\nMobRotleafChild.png\nsize: "+packed.width+","+packed.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<Parts.Length;i++) {
            var r=rects[i];int x=Mathf.RoundToInt(r.x*packed.width),y=Mathf.RoundToInt((1-r.yMax)*packed.height),w=Mathf.RoundToInt(r.width*packed.width),h=Mathf.RoundToInt(r.height*packed.height);
            atlas.AppendLine(Parts[i]+"\n  rotate: false\n  xy: "+x+", "+y+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1");
        }
        File.WriteAllBytes(Root+"/MobRotleafChild.png",packed.EncodeToPNG());File.WriteAllText(Root+"/MobRotleafChild.atlas.txt",atlas.ToString());
        File.Copy(Source+"/MobRotleafChild.json",Root+"/MobRotleafChild.json",true);
        foreach(var t in textures.Concat(sheets))UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(packed);
        AssetDatabase.Refresh();
        var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/MobRotleafChild.png");
        importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/MobRotleafChild.png");
        var material=Asset(Root+"/MobRotleafChild_Material.mat",()=>new Material(Shader.Find("Spine/Skeleton")));
        material.mainTexture=texture;material.SetFloat("_StraightAlphaInput",1);material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(material);
        var white=Asset(Root+"/MobRotleafChild_White.mat",()=>new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/RedHoodWhite.mat")));
        white.mainTexture=texture;EditorUtility.SetDirty(white);AssetDatabase.SaveAssets();
        var atlasAsset=Asset(Root+"/MobRotleafChild_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());
        atlasAsset.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/MobRotleafChild.atlas.txt");atlasAsset.materials=new[]{material};atlasAsset.Clear();EditorUtility.SetDirty(atlasAsset);
        var data=Asset(Root+"/MobRotleafChild_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());
        data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/MobRotleafChild.json");data.atlasAssets=new AtlasAssetBase[]{atlasAsset};data.scale=.004f;data.defaultMix=.06f;data.Clear();EditorUtility.SetDirty(data);
        if(data.GetSkeletonData(false)==null)throw new Exception("MobRotleafChild source format cannot load.");
        // Existing editable prefabs are preserved on texture rebuilds.
        if(!File.Exists(Root+"/MobRotleafChild.prefab"))MakePrefab(data,material,white);
        Register();RefreshTestScene();AssetDatabase.SaveAssets();
        var export=Path.GetFullPath("../Spine/export/monster/MobRotleafChild");Directory.CreateDirectory(export);
        foreach(var ext in new[]{".json",".atlas.txt",".png"})File.Copy(Root+"/MobRotleafChild"+ext,export+"/MobRotleafChild"+ext,true);
        Validate();Debug.Log("MOBROTLEAFCHILD_BUILD_PASS");
    }
    static void MakePrefab(SkeletonDataAsset data,Material material,Material white)
    {
        var go=PrefabUtility.LoadPrefabContents("Assets/DarkFairytale/Monsters/BlackForestMushroom/MobWaxcap/MobWaxcap.prefab");
        try {
            go.name="MobRotleafChild";var s=go.GetComponentInChildren<SkeletonAnimation>(true);
            s.skeletonDataAsset=data;s.initialSkinName="1";s.initialFlipX=false;s.initialFlipY=false;
            s.transform.localPosition=Vector3.zero;s.transform.localScale=new Vector3(-1,1,1);s.AnimationName="Idle1";s.loop=true;s.Initialize(true);
            var enemy=go.GetComponent<Enemy>();enemy.skeletonAnimation=s;enemy.maxSkins=1;enemy.maxAttacks=2;enemy.waitBetweenAttacks=2.8f;enemy.currentAnimation="";
            enemy.rootPos.transform.localPosition=new Vector3(0,1.0f,0);
            var swap=s.GetComponent<MaterialSwapper>();swap.skeletonAnimation=s;swap.origMats=new List<Material>{material};swap.whiteMats=new[]{white};
            var drop=go.GetComponent<DropIn>();drop.spawnInPlace=false;
            go.transform.GetChild(0).localPosition=new Vector3(0,2.5f,0);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/MobRotleafChild.prefab");
        } finally {PrefabUtility.UnloadPrefabContents(go);}
    }
    static void Register()
    {
        string guid=AssetDatabase.AssetPathToGUID(Root+"/MobRotleafChild.prefab");var settings=AddressableAssetSettingsDefaultObject.Settings;
        settings.CreateOrMoveEntry(guid,settings.DefaultGroup).address="MobRotleafChild";EditorUtility.SetDirty(settings);
        var db=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/LevelDataDescriptions.asset"));var entries=db.FindProperty("_enemyDb");SerializedProperty entry=null;
        for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue=="MobRotleafChild")entry=entries.GetArrayElementAtIndex(i);
        if(entry==null){entries.InsertArrayElementAtIndex(entries.arraySize);entry=entries.GetArrayElementAtIndex(entries.arraySize-1);}
        entry.FindPropertyRelative("key").stringValue="MobRotleafChild";entry.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue=guid;db.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void RefreshTestScene()
    {
        var scene=SceneManager.GetSceneByPath(MonsterTestSceneBuilder.ScenePath);bool opened=!scene.isLoaded;
        if(!opened && scene.isDirty)throw new Exception("Save test scene edits before registering MobRotleafChild.");
        if(opened)scene=EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath,OpenSceneMode.Additive);
        try {
            var tester=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonsterAnimationTest>(true)).First();
            tester.discoveryFolders=tester.discoveryFolders.Concat(new[]{Root}).Distinct().ToArray();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/MobRotleafChild.prefab");
            tester.monsterPrefabs=tester.monsterPrefabs.Concat(new[]{prefab}).Distinct().ToArray();tester.RefreshCatalog();
            var portrait=MonsterPreviewUiBuilder.MakePortrait(AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/MobRotleafChild_SkeletonData.asset"),tester,true);
            MonsterPreviewUiBuilder.RefreshHudCatalog(tester);
            var hudRoot=PrefabUtility.LoadPrefabContents(MonsterPreviewUiBuilder.HudPath);
            try {var card=hudRoot.GetComponent<MonsterPreviewHud>().cards.First(c=>c.data.name=="MobRotleafChild_SkeletonData");card.displayName="MobRotleafChild\n腐叶祈童";card.title.text=card.displayName;card.portrait.sprite=portrait;PrefabUtility.SaveAsPrefabAsset(hudRoot,MonsterPreviewUiBuilder.HudPath);}
            finally {PrefabUtility.UnloadPrefabContents(hudRoot);}
            if(tester.character) {
                tester.character.skeletonDataAsset=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/MobRotleafChild_SkeletonData.asset");
                tester.character.initialSkinName="1";tester.character.transform.localScale=new Vector3(-1,1,1);tester.character.Initialize(true);tester.character.AnimationName="Idle1";EditorUtility.SetDirty(tester.character);
            }

            EditorUtility.SetDirty(tester);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static void Pose(SkeletonAnimation s,string skin,string clip,float time)
    {
        s.AnimationState.ClearTracks();s.Skeleton.SetSkin(skin);s.Skeleton.SetToSetupPose();var entry=s.AnimationState.SetAnimation(0,clip,false);entry.MixDuration=0;s.Update(time);s.LateUpdate();
    }
    [MenuItem("Tools/Dark Fairytale/Monsters/MobRotleafChild/Validate and Render")]
    public static void Validate()
    {
        Directory.CreateDirectory(Output);var report=new StringBuilder("MobRotleafChild Unity validation\n");
        var scene=EditorSceneManager.NewPreviewScene();SkeletonAnimation s=null;
        try {
            var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/MobRotleafChild_SkeletonData.asset");
            s=SkeletonAnimation.NewSkeletonAnimationGameObject(data);SceneManager.MoveGameObjectToScene(s.gameObject,scene);s.transform.localScale=new Vector3(-1,1,1);
            if(s.Skeleton.Bones.Count!=20||s.Skeleton.Slots.Count!=11)throw new Exception("Unexpected rig topology");
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
            foreach(var name in new[]{"Idle1","Attack1","Hit","Death1"}) {
                int hits=0,ends=0;Spine.AnimationState.TrackEntryEventDelegate handler=(entry,e)=>{if(e.Data.Name=="OnHit")hits++;if(e.Data.Name=="OnComplete")ends++;};
                Pose(s,"1",name,0);s.AnimationState.Event+=handler;
                for(int f=0;f<Mathf.CeilToInt(s.Skeleton.Data.FindAnimation(name).Duration*60)+3;f++)s.Update(1/60f);
                s.AnimationState.Event-=handler;
                if(hits!=(name=="Attack1"?1:0)||ends!=(name=="Idle1"?0:1))throw new Exception("Wrong event count "+name);
                report.AppendLine(name+": PASS events OnHit="+hits+", OnComplete="+ends);
            }
            int leakedHits=0,completions=0;Spine.AnimationState.TrackEntryEventDelegate interrupt=(entry,e)=>{if(e.Data.Name=="OnHit")leakedHits++;if(e.Data.Name=="OnComplete")completions++;};
            Pose(s,"1","Attack1",.2f);s.AnimationState.Event+=interrupt;s.AnimationState.SetAnimation(0,"Hit",false);s.Update(.8f);s.AnimationState.Event-=interrupt;
            if(leakedHits!=0||completions!=1)throw new Exception("Interrupted attack leaked hit event");
            report.AppendLine("PASS interrupted attack: 0 hits, 1 completion.");
            leakedHits=0;completions=0;Pose(s,"1","Attack1",.1f);s.AnimationState.Event+=interrupt;s.AnimationState.SetAnimation(0,"Death1",false);s.Update(1.7f);s.AnimationState.Event-=interrupt;
            if(leakedHits!=0||completions!=1)throw new Exception("Death interruption leaked events");
            report.AppendLine("PASS attack interrupted by death: 0 hits, 1 completion.");
            var camGo=new GameObject("MobRotleafChild audit camera");SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.13f,.15f,.18f);cam.transform.position=new Vector3(bound.center.x,bound.center.y,-10);cam.orthographicSize=Mathf.Max(bound.size.y/2, bound.size.x/3f)*1.12f;
            foreach(var anim in all) {
                Pose(s,"1",anim.Name,0);var motion=s.GetComponent<Renderer>().bounds;
                for(int f=1;f<=60;f++){Pose(s,"1",anim.Name,anim.Duration*f/60f);motion.Encapsulate(s.GetComponent<Renderer>().bounds);}
                cam.transform.position=new Vector3(motion.center.x,motion.center.y,-10);cam.orthographicSize=Mathf.Max(motion.size.y/2,motion.size.x/3f)*1.15f;
                RenderStrip(s,cam,anim.Name,anim.Duration);
                if(new[]{"Idle1","Attack1","Hit","Death1"}.Contains(anim.Name)) {
                    cam.orthographicSize=Mathf.Max(motion.size.y,motion.size.x)*.56f;
                    for(int f=0;f<6;f++){Pose(s,"1",anim.Name,anim.Duration*f/5f);Render(cam,Output+"/"+anim.Name+"-"+f+".png",768,768);}
                }
            }
            Pose(s,"1","Idle1",0);var idle=s.GetComponent<Renderer>().bounds;cam.transform.position=new Vector3(idle.center.x,idle.center.y,-10);cam.orthographicSize=Mathf.Max(idle.size.y/2,idle.size.x/3f)*1.15f;Render(cam,Output+"/idle.png",1200,800);
            report.AppendLine("PASS 20 bones, 11 slots, 18 clips, 36 skin/clip combinations; rendered all clips.");
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
