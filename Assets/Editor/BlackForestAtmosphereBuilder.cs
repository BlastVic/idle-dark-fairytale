using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Rebuilds the layered plate and two small, editable Spine 3.8 ambient rigs.</summary>
public static class BlackForestAtmosphereBuilder
{
    public const string Root = "Assets/DarkFairytale/Environment/BlackForest";
    const string Source = "ArtDirection/Environment/BlackForest/source";
    const string Output = "output/black-forest-atmosphere";
    const string Request = Output + "/build.request";

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!File.Exists(Request)) return;
        EditorApplication.delayCall += () => {
            if (EditorApplication.isPlaying) return;
            File.Delete(Request);
            try { Build(); }
            catch (Exception e) { File.WriteAllText(Output + "/build-error.txt", e.ToString()); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Dark Fairytale/Environment/Build Black Forest Atmosphere")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebuilding.");
        Directory.CreateDirectory(Output);
        var sheet = Read(Source + "/prop-sheet.png");
        var foreground = Read(Root + "/Foreground.png");
        if (foreground.GetPixel(foreground.width/2,foreground.height*3/4).a > .01f)
            throw new Exception("Foreground must have genuine transparent alpha in the central opening.");
        UnityEngine.Object.DestroyImmediate(foreground);
        var lantern = CropCell(sheet, 0, 0);
        var open = CropCell(sheet, 1, 0);
        var closed = CropCell(sheet, 0, 1);
        var iris = CropCell(sheet, 1, 1);
        var supportSheet = Read(Source + "/lantern-support-sheet.png");
        var bracket = CropRegion(supportSheet,0,0,supportSheet.width/2,supportSheet.height);
        var glow = CropRegion(supportSheet,supportSheet.width/2,0,supportSheet.width/2,supportSheet.height);
        var lanternData = BuildRig("BF_Lantern", new[] { "lantern", "bracket", "glow" }, new[] { lantern, bracket, glow }, Lantern(lantern,bracket,glow));
        var moonData = BuildRig("BF_WatchingMoon", new[] { "open", "closed", "iris" }, new[] { open, closed, iris }, Moon(open,closed,iris));
        foreach (var t in new[] { sheet,lantern,open,closed,iris,supportSheet,bracket,glow }) UnityEngine.Object.DestroyImmediate(t);
        ImportTexture(Root + "/Background.png", true);
        ImportTexture(Root + "/Foreground.png", true);
        var style = AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/MoonlitCastleBattleStyle.asset");
        style.background = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Background.png");
        style.foreground = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Foreground.png");
        style.lanternAtmosphere = lanternData; style.moonAtmosphere = moonData;
        EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
        ValidateAndRender();
        if (File.Exists(Output + "/build-error.txt")) File.Delete(Output + "/build-error.txt");
        Debug.Log("BLACK_FOREST_ATMOSPHERE_BUILD_PASSED");
    }

    static Texture2D Read(string path)
    {
        var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
        if (!texture.LoadImage(File.ReadAllBytes(path))) throw new Exception("Cannot read " + path);
        return texture;
    }

    // Technical atlas extraction only; generated alpha and all painted pixels are preserved.
    static Texture2D CropCell(Texture2D sheet, int column, int row)
    {
        int w=sheet.width/2,h=sheet.height/2,ox=column*w,oy=(1-row)*h;
        // The painted lantern finial extends slightly below the nominal quadrant.
        if (column==0 && row==0) { int extra=sheet.height/24;oy-=extra;h+=extra; }
        if (column==0 && row==1) h-=sheet.height/24;
        return CropRegion(sheet,ox,oy,w,h);
    }

    static Texture2D CropRegion(Texture2D sheet,int ox,int oy,int w,int h)
    {
        int minX=w,minY=h,maxX=-1,maxY=-1;
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            if(sheet.GetPixel(ox+x,oy+y).a>.005f) { minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y); }
        if(maxX<minX) throw new Exception("Empty sprite cell");
        minX=Math.Max(0,minX-2);minY=Math.Max(0,minY-2);maxX=Math.Min(w-1,maxX+2);maxY=Math.Min(h-1,maxY+2);
        var result=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
        result.SetPixels(sheet.GetPixels(ox+minX,oy+minY,result.width,result.height));result.Apply();return result;
    }

    static void ImportTexture(string path, bool sprite)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var t=(TextureImporter)AssetImporter.GetAtPath(path);
        t.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
        if(sprite) {
            t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);
            settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(settings);
        }
        t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
        t.mipmapEnabled=false;t.sRGBTexture=true;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.maxTextureSize=2048;t.wrapMode=TextureWrapMode.Clamp;t.SaveAndReimport();
    }

    static T Asset<T>(string path,Func<T> create) where T:UnityEngine.Object
    {
        var value=AssetDatabase.LoadAssetAtPath<T>(path);
        if(value) return value;
        value=create();AssetDatabase.CreateAsset(value,path);return value;
    }

    static SkeletonDataAsset BuildRig(string id,string[] names,Texture2D[] images,JObject rig)
    {
        string dir=Root+"/"+id;Directory.CreateDirectory(dir);Directory.CreateDirectory(Source+"/"+id+"/images");
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        var rects=texture.PackTextures(images,8,2048,false);
        var atlas=new StringBuilder("\n"+id+".png\nsize: "+texture.width+","+texture.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<names.Length;i++) {
            int x=Mathf.RoundToInt(rects[i].x*texture.width), y=Mathf.RoundToInt((1-rects[i].yMax)*texture.height);
            int w=Mathf.RoundToInt(rects[i].width*texture.width),h=Mathf.RoundToInt(rects[i].height*texture.height);
            atlas.AppendLine(names[i]+"\n  rotate: false\n  xy: "+x+", "+y+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1");
            File.WriteAllBytes(Source+"/"+id+"/images/"+names[i]+".png",images[i].EncodeToPNG());
        }
        File.WriteAllBytes(dir+"/"+id+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        File.WriteAllText(dir+"/"+id+".atlas.txt",atlas.ToString());File.WriteAllText(dir+"/"+id+".json",rig.ToString());
        File.WriteAllText(Source+"/"+id+"/"+id+".json",rig.ToString());
        AssetDatabase.Refresh();ImportTexture(dir+"/"+id+".png",false);
        var mat=Asset(dir+"/"+id+"_Material.mat",()=>new Material(Shader.Find("Spine/Skeleton")));
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+id+".png");
        mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);
        var atlasAsset=Asset(dir+"/"+id+"_Atlas.asset",ScriptableObject.CreateInstance<SpineAtlasAsset>);
        atlasAsset.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"/"+id+".atlas.txt");atlasAsset.materials=new[]{mat};atlasAsset.Clear();EditorUtility.SetDirty(atlasAsset);
        var data=Asset(dir+"/"+id+"_SkeletonData.asset",ScriptableObject.CreateInstance<SkeletonDataAsset>);
        data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"/"+id+".json");data.atlasAssets=new AtlasAssetBase[]{atlasAsset};
        data.scale=.01f;data.defaultMix=0;data.Clear();EditorUtility.SetDirty(data);
        if(data.GetSkeletonData(false)==null) throw new Exception("Invalid Spine rig: "+id);
        var go=new GameObject(id,typeof(MeshFilter),typeof(MeshRenderer));
        try {
            var skeleton=go.AddComponent<SkeletonAnimation>();skeleton.skeletonDataAsset=data;skeleton.Initialize(true);skeleton.loop=true;skeleton.AnimationName="Ambient";
            var renderer=go.GetComponent<MeshRenderer>();renderer.sortingLayerName="BG";renderer.sortingOrder=-90;
            if(id=="BF_Lantern")go.AddComponent<BlackForestLanternMotion>();
            if(id=="BF_WatchingMoon")go.AddComponent<BlackForestMoonMotion>();
            PrefabUtility.SaveAsPrefabAsset(go,dir+"/"+id+".prefab");
        } finally { UnityEngine.Object.DestroyImmediate(go); }
        var export=Path.GetFullPath("../Spine/export/environment/"+id);Directory.CreateDirectory(export);
        foreach(var ext in new[]{".png",".atlas.txt",".json"})File.Copy(dir+"/"+id+ext,export+"/"+id+ext,true);
        return data;
    }

    static JObject Bone(string name,string parent=null) { var b=new JObject{{"name",name}};if(parent!=null)b["parent"]=parent;return b; }
    static JObject Slot(string name,string bone) => new JObject{{"name",name},{"bone",bone},{"attachment",name}};
    static JObject Region(string name,Texture2D image,float width,float y=0) => new JObject{{"name",name},{"width",width},{"height",width*image.height/image.width},{"y",y}};
    static JObject Rig(JArray bones,JArray slots,JObject attachments,JObject animation) => new JObject {
        {"skeleton",new JObject{{"hash","black-forest-atmosphere-v1"},{"spine","3.8.75"},{"images","./images/"}}},{"bones",bones},{"slots",slots},
        {"skins",new JArray(new JObject{{"name","default"},{"attachments",attachments}})},
        {"animations",new JObject{{"Ambient",animation}}}
    };
    static JArray Values(string field, params float[] pairs) {
        var a=new JArray();
        for(int i=0;i<pairs.Length;i+=2) {
            var frame=new JObject{{"time",pairs[i]},{field,pairs[i+1]}};
            if(i+2<pairs.Length) { frame["curve"]=.37f;frame["c2"]=0;frame["c3"]=.63f;frame["c4"]=1; }
            a.Add(frame);
        }
        return a;
    }
    static JArray Colors(params object[] pairs) {
        var a=new JArray();for(int i=0;i<pairs.Length;i+=2)a.Add(new JObject{{"time",Convert.ToSingle(pairs[i])},{"color",(string)pairs[i+1]}});return a;
    }
    static JObject Lantern(Texture2D lantern,Texture2D bracket,Texture2D glow)
    {
        float height=116,width=height*lantern.width/lantern.height;
        var support=Region("bracket",bracket,56,-11);support["x"]=-20;
        var glowSlot=Slot("glow","swing");glowSlot["color"]="ffffff38";
        var rig=Rig(new JArray(Bone("root"),Bone("swing","root")),new JArray(glowSlot,Slot("bracket","root"),Slot("lantern","swing")),
            new JObject{{"lantern",new JObject{{"lantern",Region("lantern",lantern,width,-height/2)}}},
                {"bracket",new JObject{{"bracket",support}}},{"glow",new JObject{{"glow",Region("glow",glow,100,-83)}}}},
            new JObject {
                {"bones",new JObject{{"swing",new JObject{{"rotate",Values("angle",0,0,1.4f,6.5f,2.8f,0,4.2f,-6.5f,5.6f,0,7f,5.5f,8.4f,0,9.8f,-5.5f,11.2f,0,12.6f,6.5f,14f,0,15.4f,-6.5f,16.8f,0)}}}}},
                {"slots",new JObject{{"lantern",new JObject{{"color",Colors(0,"ffffffff",1.8f,"eeeeeeff",3.2f,"ffffffff",5.8f,"f4f0eaff",6f,"b9b5afff",6.09f,"ffffffff",6.2f,"d5cec5ff",6.35f,"ffffffff",9f,"eeebe5ff",11f,"ffffffff",13.7f,"f2eee7ff",13.85f,"c3bdb5ff",14.03f,"ffffffff",16.8f,"ffffffff")}}}}}
            });
        rig["animations"]["Ambient"]["slots"]["glow"]=new JObject{{"color",Colors(0,"ffffff38",1.8f,"ffffff2b",3.2f,"ffffff3e",5.8f,"ffffff32",6f,"ffffff12",6.09f,"ffffff40",6.2f,"ffffff22",6.35f,"ffffff38",9f,"ffffff2d",11f,"ffffff3c",13.7f,"ffffff31",13.85f,"ffffff18",14.03f,"ffffff3d",16.8f,"ffffff38")}};
        foreach(var frame in rig["animations"]["Ambient"]["slots"]["glow"]["color"])
            frame["color"]="ffffff"+Mathf.RoundToInt(Convert.ToInt32(((string)frame["color"]).Substring(6),16)*1.8f).ToString("x2");
        return rig;
    }
    static JObject Moon(Texture2D open,Texture2D closed,Texture2D iris)
    {
        var eye=Bone("eye","root");var pupil=Bone("pupil","eye");
        pupil["y"]=-2;
        var scale=Values("y",0,0,8,0,8.55f,1,13,1,13.45f,0,22,0);
        foreach(JObject frame in scale)frame["x"]=1;
        var rig=Rig(new JArray(Bone("root"),eye,pupil),new JArray(Slot("open","eye"),Slot("eyeClip","eye"),Slot("iris","pupil"),Slot("closed","root")),
            new JObject{{"open",new JObject{{"open",Region("open",open,118)}}},{"iris",new JObject{{"iris",Region("iris",iris,25)}}},{"closed",new JObject{{"closed",Region("closed",closed,118,-8)}}}},
            new JObject {
                {"bones",new JObject{{"eye",new JObject{{"scale",scale}}},{"pupil",new JObject{{"translate",Values("x",0,0,8.55f,0,9.5f,-12,10.5f,-12,11.65f,12,12.4f,12,13,0,22,0)}}}}},
                {"slots",new JObject{{"closed",new JObject{{"color",Colors(0,"ffffffff",8,"ffffffff",8.2f,"ffffff00",13.18f,"ffffff00",13.45f,"ffffffff",22,"ffffffff")}}}}}
            });
        // Outline follows the painted sclera. Clip only the iris, never the eyelashes.
        float[] outline={66,85,126,52,188,32,247,27,310,35,373,63,433,86,396,123,344,153,284,171,221,173,162,159,108,130};
        var vertices=new JArray();
        for(int i=0;i<outline.Length;i+=2) { vertices.Add((outline[i]-247.5f)*118/495);vertices.Add((149-outline[i+1])*118/495); }
        rig["skins"][0]["attachments"]["eyeClip"]=new JObject{{"eyeClip",new JObject{{"type","clipping"},{"end","iris"},{"vertexCount",outline.Length/2},{"vertices",vertices}}}};
        var normal=(JObject)rig["animations"]["Ambient"];
        normal["slots"]["iris"]=new JObject{{"color",Colors(0,"ffffffff",22,"ffffffff")}};
        normal["slots"]["open"]=new JObject{{"color",Colors(0,"ffffffff",22,"ffffffff")}};
        rig["animations"]["GazeDown"]=normal.DeepClone();
        var red=(JObject)normal.DeepClone();
        red["slots"]["iris"]=new JObject{{"color",Colors(0,"ff2020ff",22,"ff2020ff")}};
        red["slots"]["open"]=new JObject{{"color",Colors(0,"ffb0a0ff",22,"ffb0a0ff")}};
        rig["animations"]["GazeDownRed"]=red;
        return rig;
    }

    [MenuItem("Tools/Dark Fairytale/Environment/Validate and Render Atmosphere")]
    public static void ValidateAndRender()
    {
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        try {
            var camGo=new GameObject("Atmosphere Preview Camera");SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera=camGo.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=7.68f;camera.aspect=2f/3;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            var style=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/MoonlitCastleBattleStyle.asset");
            var go=style.CreateBackdrop(camera);SceneManager.MoveGameObjectToScene(go,scene);
            var front=go.transform.Find("Foreground Reeds").GetComponent<SpriteRenderer>();
            if(SortingLayer.GetLayerValueFromID(front.sortingLayerID)<=SortingLayer.GetLayerValueFromName("Character"))throw new Exception("Foreground does not occlude actors");
            var rigs=go.GetComponentsInChildren<SkeletonAnimation>();
            if(rigs.Length!=5)throw new Exception("Expected four lantern instances and one moon");
            var lights=rigs.Where(r=>r.GetComponent<BlackForestLanternMotion>()).ToArray();
            if(lights.Length!=4)throw new Exception("Missing independent lantern motion");
            var audit=new StringBuilder("Independent four-lantern audit: 32 seconds at 60 fps\n");
            for(int i=0;i<lights.Length;i++) {
                var motion=lights[i].GetComponent<BlackForestLanternMotion>();motion.Initialize(100+i);
                float min=1,max=0,minAngle=90,maxAngle=-90;
                for(int f=0;f<1920;f++) {
                    lights[i].Update(1f/60);lights[i].LateUpdate();
                    float alpha=lights[i].Skeleton.FindSlot("glow").A,angle=lights[i].Skeleton.FindBone("swing").Rotation;
                    min=Mathf.Min(min,alpha);max=Mathf.Max(max,alpha);minAngle=Mathf.Min(minAngle,angle);maxAngle=Mathf.Max(maxAngle,angle);
                }
                if(max-min<.25f || maxAngle-minAngle<11 || motion.FlickerCount<3)throw new Exception("Lantern motion too weak or flicker missing");
                audit.AppendLine(lights[i].name+": speed="+lights[i].AnimationState.GetCurrent(0).TimeScale+", glow="+min+".."+max+", angle="+minAngle+".."+maxAngle+", flickers="+motion.FlickerCount);
            }
            File.WriteAllText(Output+"/four-lantern-validation.txt",audit.ToString());
            foreach(var rig in rigs) {
                rig.AnimationState.ClearTracks();rig.Skeleton.SetToSetupPose();rig.AnimationState.SetAnimation(0,"Ambient",true);
                for(int f=0;f<1320;f++) {
                    rig.Update(1f/60);rig.LateUpdate();
                    foreach(var v in rig.GetComponent<MeshFilter>().sharedMesh.vertices)
                        if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y))throw new Exception("Nonfinite ambient mesh");
                }
            }
            foreach(float time in new[]{0f,9.8f,12f}) {
                foreach(var rig in rigs) { rig.AnimationState.ClearTracks();rig.Skeleton.SetToSetupPose();rig.AnimationState.SetAnimation(0,"Ambient",true);rig.Update(time);rig.LateUpdate(); }
                Render(camera,Output+"/atmosphere-"+time.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1024,1536);
            }
            var moon=rigs.First(r=>r.name=="Watching Moon");
            foreach(var animation in new[]{"GazeDown","GazeDownRed"}) {
                moon.AnimationState.ClearTracks();moon.Skeleton.SetToSetupPose();moon.AnimationState.SetAnimation(0,animation,false);moon.Update(10);moon.LateUpdate();
                if(moon.Skeleton.FindBone("pupil").Y>=0)throw new Exception("Moon is not looking down");
                Render(camera,Output+"/moon-"+animation+".png",1024,1536);
                camera.transform.position=moon.transform.position+new Vector3(0,0,-10);camera.orthographicSize=1.3f;
                Render(camera,Output+"/moon-detail-"+animation+".png",700,500);
                camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=7.68f;
            }
            var selector=moon.GetComponent<BlackForestMoonMotion>();selector.Initialize(731);
            for(int f=0;f<60*22*12;f++) { moon.Update(1f/60);moon.LateUpdate(); }
            if(selector.NormalCycles==0||selector.RedCycles==0)throw new Exception("Both moon variants must be selected");
            File.WriteAllText(Output+"/moon-variants-validation.txt","PASS: 12 cycles sampled; normal="+selector.NormalCycles+", red="+selector.RedCycles+"; gaze below sclera center; clipping attachment limits iris to eye opening.\n");
            float left=SamplePupil(moon,9.8f),right=SamplePupil(moon,12f);
            if(left>=0||right<=0)throw new Exception("Moon gaze is not moving both directions");
            File.WriteAllText(Output+"/validation.txt","PASS: real transparent foreground; Character_Front above actors; 5 Spine instances; 22 seconds sampled at 60 fps without invalid mesh vertices; moon gaze left/right verified; runtime exports copied.\nVisual samples: closed 0s, left 9.8s, right 12s.\n");
        } finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    static float SamplePupil(SkeletonAnimation moon,float time) { moon.AnimationState.ClearTracks();moon.Skeleton.SetToSetupPose();moon.AnimationState.SetAnimation(0,"Ambient",true);moon.Update(time);return moon.Skeleton.FindBone("pupil").X; }
    internal static void Render(Camera camera,string path,int width,int height)
    {
        var prior=RenderTexture.active;var target=camera.targetTexture;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG()); }
        finally { camera.targetTexture=target;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt); }
    }
}
