using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit production build only. Never rebuilds authored prefabs on import or Play.</summary>
public static class Chapter01EnvironmentBuilder
{
    public const string Root="Assets/DarkFairytale/Environment/Chapter01";
    public const string Source="ArtDirection/Environment/Chapter01/production";
    public const string Output="output/chapter01-environments";
    public static readonly string[] Levels={"Khorasan Ruins I","Khorasan Ruins II","test01","test02","Mount Zion"};
    public static readonly string[] Names={"暮色林缘","蘑菇森林","月下过桥","溪畔小屋","小屋内部"};
    static readonly string[] Props={"ritual","candle","hearth","smoke","wolf","fall","ripple","marker"};
    static Dictionary<string,Texture2D> images;
    static Dictionary<string,SkeletonDataAsset> rigs;

    [MenuItem("Tools/Dark Fairytale/Environment/Build Chapter 01 Environments")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play before building environments.");
        Directory.CreateDirectory(Root);Directory.CreateDirectory(Output);
        images=new Dictionary<string,Texture2D>();rigs=new Dictionary<string,SkeletonDataAsset>();
        try {
            var sheet=Read(Source+"/Props.png");
            for(int i=0;i<Props.Length;i++) images[Props[i]]=Crop(sheet,(i%4)*sheet.width/4,(1-i/4)*sheet.height/2,sheet.width/4,sheet.height/2);
            UnityEngine.Object.DestroyImmediate(sheet);
            UnityEngine.Object.DestroyImmediate(images["ritual"]);images["ritual"]=Read(Source+"/RitualGlow.png");
            images["moon"]=CropAll(Read(Source+"/MoonDisc.png"));
            for(int i=2;i<=4;i++) images["water"+i]=Read(Source+"/Water0"+i+".png");
            foreach(var name in new[]{"ritual","candle","hearth","smoke","wolf","fall"}) rigs[name]=BuildRig("CH1_"+name,new[]{name},Effect(name));
            for(int i=2;i<=4;i++)rigs["water"+i]=BuildRig("CH1_River0"+i,new[]{"water"+i,"ripple"},River(i));
            // Reuse the validated Spine eyelid/pupil animation, add the full moon disc as its own attachment.
            string moonPath="Assets/DarkFairytale/Environment/BlackForest/BF_WatchingMoon";
            var moonRig=JObject.Parse(File.ReadAllText(moonPath+"/BF_WatchingMoon.json"));
            foreach(var name in new[]{"open","closed","iris"}) images[name]=Read("ArtDirection/Environment/BlackForest/source/BF_WatchingMoon/images/"+name+".png");
            ((JArray)moonRig["slots"]).Insert(0,Slot("moon","root"));
            ((JObject)moonRig["skins"][0]["attachments"])["moon"]=new JObject{{"moon",Region("moon",160,160,0,16)}};
            rigs["moon"]=BuildRig("CH1_WatchingMoon",new[]{"moon","open","closed","iris"},moonRig);
            SaveImage("Marker",images["marker"],true);
            var lantern=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/DarkFairytale/Environment/BlackForest/BF_Lantern/BF_Lantern_SkeletonData.asset");
            if(!lantern)throw new Exception("Validated lantern rig missing");
            var entries=new List<ChapterEnvironmentCatalog.Entry>();
            for(int n=1;n<=5;n++) {
                string dir=Root+"/Node0"+n;Directory.CreateDirectory(dir);
                File.Copy(Source+"/Background0"+n+".png",dir+"/Background.png",true);Import(dir+"/Background.png",true);
                string fg=n==3?"ForegroundBridge":n==5?"ForegroundCabin":"ForegroundForest";
                File.Copy(Source+"/"+fg+".png",dir+"/Foreground.png",true);Import(dir+"/Foreground.png",true);
                var root=new GameObject("Chapter01 Node0"+n+" "+Names[n-1],typeof(SpriteRenderer),typeof(DarkFairytaleBackdrop));
                root.tag="MapGraphics";
                try {
                    var bg=root.GetComponent<SpriteRenderer>();bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(dir+"/Background.png");bg.sortingLayerName="BG";bg.sortingOrder=-100;
                    AddSprite(root,"Foreground",AssetDatabase.LoadAssetAtPath<Sprite>(dir+"/Foreground.png"),.5f,.5f,1000,"Character_Front",100);
                    if(n==1){
                        AddRig(root,"Ritual fire beyond forest",rigs["ritual"],.5f,.5f,1f,-96);
                        AddRig(root,"Road lantern",lantern,.854f,.265f,1.25f,-80);
                        AddSprite(root,"Previous Red Hood scratched arrow",AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Marker.png"),.18f,.48f,78,"BG",-70);
                    }
                    if(n==2){AddRig(root,"Road lantern",lantern,.208f,.192f,1.15f,-80);AddRig(root,"Creek surface and current",rigs["water2"],.5f,.5f,1,-92);}
                    if(n==3){
                        // Extract the exact far railing strip as an occluder, preserving painted pixels.
                        var original=Read(dir+"/Background.png");int top=Mathf.RoundToInt(original.height*.48f),bottom=Mathf.RoundToInt(original.height*.575f);
                        var strip=new Texture2D(original.width,bottom-top,TextureFormat.RGBA32,false);
                        strip.SetPixels(original.GetPixels(0,original.height-bottom,original.width,bottom-top));strip.Apply();
                        string occluder=dir+"/FarRailOccluder.png";File.WriteAllBytes(occluder,strip.EncodeToPNG());UnityEngine.Object.DestroyImmediate(strip);UnityEngine.Object.DestroyImmediate(original);Import(occluder,true);
                        AddSprite(root,"Far bridge rail and bank occlusion",AssetDatabase.LoadAssetAtPath<Sprite>(occluder),.5f,(top+bottom)/2f/1672,1000,"BG",-85);
                        AddRig(root,"Watching moon",rigs["moon"],.405f,.16f,.79f,-94);
                        AddRig(root,"Bridge lantern",lantern,.106f,.355f,1.35f,-80);
                        AddRig(root,"Creek surface and current",rigs["water3"],.5f,.5f,1,-92);
                        AddRig(root,"Upper waterfall flow",rigs["fall"],.12f,.39f,.72f,-88);
                        AddRig(root,"Lower waterfall flow",rigs["fall"],.225f,.48f,.65f,-88);
                    }
                    if(n==4){
                        string tree=dir+"/MoonTreeOccluder.png";File.Copy(Source+"/MoonTreeOccluder.png",tree,true);Import(tree,true);
                        AddSprite(root,"Tree in front of moon",AssetDatabase.LoadAssetAtPath<Sprite>(tree),.5f,.5f,1000,"BG",-93);
                        AddRig(root,"Watching moon",rigs["moon"],.266f,.156f,.79f,-94);
                        AddRig(root,"Creek surface and current",rigs["water4"],.5f,.5f,1,-92);
                        AddRig(root,"Tree lantern",lantern,.092f,.208f,.75f,-80);
                        AddRig(root,"Door lantern",lantern,.76f,.315f,.9f,-80);
                        AddRig(root,"Chimney smoke",rigs["smoke"],.849f,.173f,.72f,-90);
                        AddRig(root,"Attic wolf shadow",rigs["wolf"],.66f,.237f,1,-79);
                    }
                    if(n==5){
                        AddRig(root,"Candle flame",rigs["candle"],.037f,.409f,1,-80);
                        AddRig(root,"Hearth fire",rigs["hearth"],.969f,.488f,1,-80);
                    }
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,dir+"/Environment.prefab");
                    entries.Add(new ChapterEnvironmentCatalog.Entry{mapKey="CH1_Node0"+n,levelKey=Levels[n-1],displayName=Names[n-1],prefab=prefab});
                } finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            var catalog=Asset<ChapterEnvironmentCatalog>("Assets/Resources/Chapter01Environments.asset",ScriptableObject.CreateInstance<ChapterEnvironmentCatalog>);
            catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();

            File.WriteAllText(Output+"/build.txt","PASS: five saved environment prefabs, ten Spine rigs, five explicit node bindings.\n");
        } finally {if(images!=null)foreach(var t in images.Values)if(t)UnityEngine.Object.DestroyImmediate(t);}
    }

    static Vector3 Position(GameObject root,float u,float v){var s=root.GetComponent<SpriteRenderer>().sprite.bounds.size;return new Vector3((u-.5f)*s.x,(.5f-v)*s.y,0);}
    static void AddSprite(GameObject root,string name,Sprite sprite,float u,float v,float width,string layer,int order)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(root.transform,false);go.transform.localPosition=Position(root,u,v);
        var r=go.GetComponent<SpriteRenderer>();r.sprite=sprite;r.sortingLayerName=layer;r.sortingOrder=order;
        float scale=root.GetComponent<SpriteRenderer>().sprite.bounds.size.x*width/1000/sprite.bounds.size.x;go.transform.localScale=Vector3.one*scale;
    }
    static void AddRig(GameObject root,string name,SkeletonDataAsset data,float u,float v,float scale,int order)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.localPosition=Position(root,u,v);
        float units=root.GetComponent<SpriteRenderer>().sprite.bounds.size.x/1000;
        go.transform.localScale=Vector3.one*(units/data.scale*scale);
        var sk=go.AddComponent<SkeletonAnimation>();sk.skeletonDataAsset=data;sk.Initialize(true);sk.loop=true;sk.AnimationName="Ambient";sk.AnimationState.SetAnimation(0,"Ambient",true);sk.Update(0);sk.LateUpdate();
        var r=go.GetComponent<MeshRenderer>();r.sortingLayerName="BG";r.sortingOrder=order;
        if(data.name=="BF_Lantern_SkeletonData")go.AddComponent<BlackForestLanternMotion>();
        if(data.name=="CH1_WatchingMoon_SkeletonData")go.AddComponent<BlackForestMoonMotion>();
    }

    static JObject Bone(string name,string parent=null){var b=new JObject{{"name",name}};if(parent!=null)b["parent"]=parent;return b;}
    static JObject Slot(string name,string bone,string attachment=null)=>new JObject{{"name",name},{"bone",bone},{"attachment",attachment??name}};
    static JObject Region(string path,float w,float h,float x=0,float y=0)=>new JObject{{"name",path},{"path",path},{"width",w},{"height",h},{"x",x},{"y",y}};
    static JObject Rig(JArray bones,JArray slots,JObject attachments,JObject animation)=>new JObject{
        {"skeleton",new JObject{{"hash","chapter01-environment-v1"},{"spine","3.8.75"},{"images","./images/"}}},{"bones",bones},{"slots",slots},
        {"skins",new JArray(new JObject{{"name","default"},{"attachments",attachments}})},{"animations",new JObject{{"Ambient",animation}}}};
    static JArray XY(params float[] xyz){var a=new JArray();for(int i=0;i<xyz.Length;i+=3)a.Add(new JObject{{"time",xyz[i]},{"x",xyz[i+1]},{"y",xyz[i+2]}});return a;}
    static JArray Colors(params object[] tc){var a=new JArray();for(int i=0;i<tc.Length;i+=2)a.Add(new JObject{{"time",Convert.ToSingle(tc[i])},{"color",(string)tc[i+1]}});return a;}

    static JObject Effect(string kind)
    {
        if(kind=="ritual")return Rig(new JArray(Bone("root")),new JArray(Slot("ritual","root")),
            new JObject{{"ritual",new JObject{{"ritual",Region("ritual",1000,1000f*images["ritual"].height/images["ritual"].width)}}}},
            new JObject{{"slots",new JObject{{"ritual",new JObject{{"color",Colors(0,"ffffffb0",.4f,"ffd5aa8a",.7f,"ffffffc0",1.15f,"ffe5bf9b",1.6f,"ffffffb0")}}}}}});
        float w=kind=="ritual"?165:kind=="candle"?30:kind=="hearth"?155:kind=="smoke"?90:kind=="wolf"?43:86;
        float h=kind=="ritual"?175:kind=="candle"?45:kind=="hearth"?165:kind=="smoke"?210:kind=="wolf"?61:180;
        var bones=new JArray(Bone("root"),Bone("motion","root"));
        var slots=new JArray();var attachments=new JObject();var animations=new JObject();var boneAnim=new JObject();var slotAnim=new JObject();
        if(kind=="wolf"){
            slots.Add(Slot("windowClip","root"));
            attachments["windowClip"]=new JObject{{"windowClip",new JObject{{"type","clipping"},{"end",kind},{"vertexCount",5},{"vertices",new JArray(-23,-26,23,-26,23,22,0,45,-23,22)}}}};
        }
        slots.Add(Slot(kind,"motion"));attachments[kind]=new JObject{{kind,Region(kind,w,h,0,kind=="wolf"?0:h/2)}};
        if(kind=="wolf"){
            boneAnim["motion"]=new JObject{{"translate",XY(0,-48,0,7,-48,0,7.6f,0,0,8.1f,8,1,8.8f,49,0,18,49,0)}};
            slotAnim[kind]=new JObject{{"color",Colors(0,"15101800",7,"15101800",7.35f,"15101880",8.1f,"15101899",8.8f,"15101800",18,"15101800")}};
        } else if(kind=="smoke"){
            boneAnim["motion"]=new JObject{{"translate",XY(0,0,-8,2,8,15,4,20,38,6,32,60)},{"scale",XY(0,.72f,.65f,3,1,1,6,1.28f,1.2f)}};
            slotAnim[kind]=new JObject{{"color",Colors(0,"c4cadd00",.6f,"c4cadd55",3,"c4cadd65",6,"c4cadd00")}};
        } else if(kind=="fall"){
            // Downward displacement; fade out before resetting upstream.
            boneAnim["motion"]=new JObject{{"translate",XY(0,0,18,1.2f,2,-18,1.25f,0,18,2.5f,0,18)}};
            slotAnim[kind]=new JObject{{"color",Colors(0,"8da8d800",.3f,"8da8d84f",1,"8da8d84f",1.2f,"8da8d800",2.5f,"8da8d800")}};
        } else {
            boneAnim["motion"]=new JObject{{"scale",XY(0,1,1,.23f,.96f,1.08f,.51f,1.04f,.91f,.82f,.97f,1.12f,1.15f,1.03f,.96f,1.6f,1,1)},
                {"rotate",new JArray(new JObject{{"time",0},{"angle",0}},new JObject{{"time",.53f},{"angle",-2}},new JObject{{"time",1.04f},{"angle",2}},new JObject{{"time",1.6f},{"angle",0}})}};
            slotAnim[kind]=new JObject{{"color",Colors(0,"ffffffff",.17f,"ffe6c0b8",.31f,"ffffffff",.55f,"ffe2bddd",.82f,"ffffffff",1.11f,"ffdeb3af",1.26f,"fff2dbef",1.6f,"ffffffff")}};
        }
        animations["bones"]=boneAnim;animations["slots"]=slotAnim;return Rig(bones,slots,attachments,animations);
    }

    static JObject River(int node)
    {
        string image="water"+node;const int cols=12,rows=20;float h=1000f*images[image].height/images[image].width;
        var uv=new JArray();var vertices=new JArray();var triangles=new JArray();
        for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
            uv.Add(x/(float)cols);uv.Add(y/(float)rows);vertices.Add((x/(float)cols-.5f)*1000);vertices.Add((.5f-y/(float)rows)*h);
            if(x<cols&&y<rows){int a=y*(cols+1)+x;foreach(int v in new[]{a,a+1,a+cols+1,a+1,a+cols+2,a+cols+1})triangles.Add(v);}
        }
        var mesh=new JObject{{"type","mesh"},{"path",image},{"uvs",uv},{"triangles",triangles},{"vertices",vertices},{"hull",4},{"width",1000},{"height",h}};
        var bones=new JArray(Bone("root"));var surfaceSlot=Slot("surface","root");surfaceSlot["color"]="b0c0df55";var slots=new JArray(surfaceSlot);
        var attachments=new JObject{{"surface",new JObject{{"surface",mesh}}}};var frames=new JArray();
        for(int f=0;f<=8;f++){
            var offsets=new JArray();double time=f*.5;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                double amp=(x==0||y==0||x==cols||y==rows)?0:1.5;
                offsets.Add(amp*Math.Sin(x*.6+y*.8-time*Math.PI/2));offsets.Add(.65*amp*Math.Sin(y*.7-time*Math.PI/2));
            }
            frames.Add(new JObject{{"time",time},{"vertices",offsets}});
        }
        var deform=new JObject{{"default",new JObject{{"surface",new JObject{{"surface",frames}}}}}};
        var boneAnim=new JObject();var slotAnim=new JObject();
        // Narrow glints move downstream and fade, never sweeping over dry banks.
        float[][] points=node==2?new[]{new[]{.86f,.424f},new[]{.84f,.454f},new[]{.79f,.478f}}:
            node==3?new[]{new[]{.43f,.369f},new[]{.57f,.428f},new[]{.66f,.512f},new[]{.51f,.843f},new[]{.79f,.899f}}:
            new[]{new[]{.169f,.315f},new[]{.16f,.402f},new[]{.055f,.412f}};
        for(int i=0;i<points.Length;i++){
            string name="current"+i;var b=Bone(name,"root");b["x"]=(points[i][0]-.5f)*1000;b["y"]=(.5f-points[i][1])*h;bones.Add(b);slots.Add(Slot(name,name,"ripple"));
            attachments[name]=new JObject{{"ripple",Region("ripple",node==3?65:45,node==3?15:10)}};
            float dx=node==4?-18:node==2?-9:12,dy=node==4?-2:-12;
            boneAnim[name]=new JObject{{"translate",XY(0,0,0,2,dx,dy,2.01f,0,0,4,0,0)}};
            slotAnim[name]=new JObject{{"color",Colors(0,"aac6e000",.3f,"aac6e040",1.4f,"aac6e055",2,"aac6e000",4,"aac6e000")}};
        }
        return Rig(bones,slots,attachments,new JObject{{"deform",deform},{"bones",boneAnim},{"slots",slotAnim}});
    }

    static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);return t;}
    static Texture2D CropAll(Texture2D t){var r=Crop(t,0,0,t.width,t.height);UnityEngine.Object.DestroyImmediate(t);return r;}
    static Texture2D Crop(Texture2D source,int ox,int oy,int w,int h)
    {
        // Technical atlas extraction: preserve generated pixels/alpha; no painted artwork is synthesized here.
        int minX=w,minY=h,maxX=-1,maxY=-1;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(source.GetPixel(ox+x,oy+y).a>.025f){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
        if(maxX<0)throw new Exception("Empty generated sprite cell");
        var r=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);r.SetPixels(source.GetPixels(ox+minX,oy+minY,r.width,r.height));r.Apply();return r;
    }
    static void SaveImage(string name,Texture2D image,bool sprite){File.WriteAllBytes(Root+"/"+name+".png",image.EncodeToPNG());Import(Root+"/"+name+".png",sprite);}
    static void Import(string path,bool sprite)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
        if(sprite){t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spritePivot=new Vector2(.5f,.5f);s.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(s);}
        t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.sRGBTexture=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.wrapMode=TextureWrapMode.Clamp;t.SaveAndReimport();
    }
    static T Asset<T>(string path,Func<T> create)where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a)return a;a=create();AssetDatabase.CreateAsset(a,path);return a;}
    static SkeletonDataAsset BuildRig(string id,string[] keys,JObject rig)
    {
        string dir=Root+"/"+id;Directory.CreateDirectory(dir);string sourceDir=Source+"/Spine/"+id;Directory.CreateDirectory(sourceDir+"/images");
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);var rects=texture.PackTextures(keys.Select(k=>images[k]).ToArray(),4,2048,false);
        var atlas=new StringBuilder("\n"+id+".png\nsize: "+texture.width+","+texture.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<keys.Length;i++){
            int x=Mathf.RoundToInt(rects[i].x*texture.width),y=Mathf.RoundToInt((1-rects[i].yMax)*texture.height),w=Mathf.RoundToInt(rects[i].width*texture.width),h=Mathf.RoundToInt(rects[i].height*texture.height);
            atlas.AppendLine(keys[i]+"\n  rotate: false\n  xy: "+x+", "+y+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1");
            File.WriteAllBytes(sourceDir+"/images/"+keys[i]+".png",images[keys[i]].EncodeToPNG());
        }
        File.WriteAllBytes(dir+"/"+id+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        File.WriteAllText(dir+"/"+id+".atlas.txt",atlas.ToString());File.WriteAllText(dir+"/"+id+".json",rig.ToString());File.WriteAllText(sourceDir+"/"+id+".json",rig.ToString());
        AssetDatabase.Refresh();Import(dir+"/"+id+".png",false);
        var mat=Asset(dir+"/"+id+"_Material.mat",()=>new Material(Shader.Find("Spine/Skeleton")));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+id+".png");mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);
        var atlasData=Asset(dir+"/"+id+"_Atlas.asset",ScriptableObject.CreateInstance<SpineAtlasAsset>);atlasData.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"/"+id+".atlas.txt");atlasData.materials=new[]{mat};atlasData.Clear();EditorUtility.SetDirty(atlasData);
        var data=Asset(dir+"/"+id+"_SkeletonData.asset",ScriptableObject.CreateInstance<SkeletonDataAsset>);data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(dir+"/"+id+".json");data.atlasAssets=new AtlasAssetBase[]{atlasData};data.scale=.01f;data.defaultMix=0;data.Clear();EditorUtility.SetDirty(data);
        if(data.GetSkeletonData(false)==null)throw new Exception("Invalid Spine "+id);return data;
    }
}
