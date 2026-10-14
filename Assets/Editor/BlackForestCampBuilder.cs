using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring requests only; never rebuilds on play/import.</summary>
[InitializeOnLoad]
public static class BlackForestCampBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shelter/BlackForestCamp";
    public const string Out="output/black-forest-camp/";
    const string Source="ArtDirection/Storybook/Shelter/BlackForestCamp/SpineSource";
    static BlackForestCampBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build-result.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string p,Func<T> create) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(p);if(!a){a=create();AssetDatabase.CreateAsset(a,p);}return a;}
    static Texture2D Read(string name){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Root+"/"+name+".png"));return t;}
    static RectInt Bounds(Texture2D t,RectInt r){var c=t.GetPixels32();int x0=r.xMax,y0=r.yMax,x1=r.x,y1=r.y;for(int y=r.y;y<r.yMax;y++)for(int x=r.x;x<r.xMax;x++)if(c[(t.height-1-y)*t.width+x].a>24){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}return new RectInt(x0,y0,x1-x0+1,y1-y0+1);}
    static Texture2D Crop(Texture2D t,RectInt r,bool feather=false){var n=new Texture2D(r.width,r.height,TextureFormat.RGBA32,false);var c=t.GetPixels(r.x,t.height-r.yMax,r.width,r.height);if(feather)for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)c[y*r.width+x].a*=Mathf.Clamp01(Mathf.Min(x,y,r.width-1-x,r.height-1-y)/6f);n.SetPixels(c);n.Apply();return n;}
    static Texture2D Import(string p){AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=TextureImporterType.Default;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=4096;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);}
    static JObject Region(string path,float w,float h,float x=0,float y=0)=>new JObject{{"path",path},{"width",w},{"height",h},{"x",x},{"y",y}};
    static JObject ColorKey(float t,string color)=>new JObject{{"time",t},{"color",color}};
    static JObject Json(string name,JArray bones,JArray slots,JObject attachments,JObject animations)=>new JObject{{"skeleton",new JObject{{"spine","3.8.99"},{"hash",name},{"images","images/"}}},{"bones",bones},{"slots",slots},{"skins",new JArray(new JObject{{"name","default"},{"attachments",attachments}})},{"animations",animations}};
    static SkeletonDataAsset Rig(string name,Texture2D[] parts,string[] names,JObject json,float scale)
    {
        Directory.CreateDirectory(Source+"/"+name+"/images");
        var atlas=new Texture2D(4096,4096,TextureFormat.RGBA32,false);var rects=atlas.PackTextures(parts,4,4096,false);
        File.WriteAllBytes(Root+"/"+name+".png",atlas.EncodeToPNG());var text=new StringBuilder(name+".png\nsize: "+atlas.width+","+atlas.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<parts.Length;i++){var r=rects[i];int w=Mathf.RoundToInt(r.width*atlas.width),h=Mathf.RoundToInt(r.height*atlas.height);if(w!=parts[i].width||h!=parts[i].height)throw new Exception("Atlas downscale");text.Append(names[i]+"\n  rotate: false\n  xy: "+Mathf.RoundToInt(r.x*atlas.width)+", "+Mathf.RoundToInt((1-r.yMax)*atlas.height)+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1\n");File.WriteAllBytes(Source+"/"+name+"/images/"+names[i]+".png",parts[i].EncodeToPNG());}
        File.WriteAllText(Root+"/"+name+".atlas.txt",text.ToString());File.WriteAllText(Root+"/"+name+".json",json.ToString());File.WriteAllText(Source+"/"+name+"/"+name+".json",json.ToString());
        var tex=Import(Root+"/"+name+".png");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var mat=Asset(Root+"/"+name+".mat",()=>new Material(Shader.Find("Spine/SkeletonGraphic")));mat.mainTexture=tex;mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        var aa=Asset(Root+"/"+name+"_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());aa.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/"+name+".atlas.txt");aa.materials=new[]{mat};aa.Clear();EditorUtility.SetDirty(aa);
        var data=Asset(Root+"/"+name+"_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/"+name+".json");data.atlasAssets=new AtlasAssetBase[]{aa};data.scale=scale;data.defaultMix=.18f;data.Clear();EditorUtility.SetDirty(data);if(data.GetSkeletonData(false)==null)throw new Exception("Invalid Spine "+name);UnityEngine.Object.DestroyImmediate(atlas);return data;
    }
    static RectTransform Node(string name,Transform parent,Vector2 size,Vector2 position){var g=new GameObject(name,typeof(RectTransform));g.layer=5;var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.sizeDelta=size;r.anchoredPosition=position;return r;}
    static void Actor(string name,RectTransform parent,SkeletonDataAsset data,Vector2 pos,string clip,bool random)
    {
        string path=Root+"/"+name+".prefab";if(!File.Exists(path)){
            var rt=Node(name,null,new Vector2(400,450),Vector2.zero);try{var sg=rt.gameObject.AddComponent<SkeletonGraphic>();sg.skeletonDataAsset=data;sg.material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");sg.startingAnimation=clip;sg.startingLoop=!random;sg.unscaledTime=true;sg.raycastTarget=false;sg.Initialize(true);if(random)rt.gameObject.AddComponent<SpineCampfireRandom>();PrefabUtility.SaveAsPrefabAsset(rt.gameObject,path);}finally{UnityEngine.Object.DestroyImmediate(rt.gameObject);}}
        if(!parent.Find(name)){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.anchoredPosition=pos;}
    }
    [MenuItem("Tools/Dark Fairytale/UI/Build Black Forest Camp (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Source+"/images");
        const float pixel=1080f/941f;
        var original=Read("Character");var closed=Read("Blink");var bounds=Bounds(original,new RectInt(0,0,original.width,original.height));var body=Crop(original,bounds);
        // Closed-eye patch replaces only the eyes; silhouette and all other approved pixels stay still.
        var eyeRect=new RectInt(807,203,168,111);var eyes=Crop(closed,eyeRect,true);
        var bones=new JArray(new JObject{{"name","root"}});var slots=new JArray(new JObject{{"name","body"},{"bone","root"},{"attachment","body"}},new JObject{{"name","eyes"},{"bone","root"},{"attachment","eyes"},{"color","ffffff00"}});
        var attachments=new JObject{["body"]=new JObject{["body"]=Region("body",body.width,body.height)},["eyes"]=new JObject{["eyes"]=Region("eyes",eyes.width,eyes.height,eyeRect.center.x-bounds.center.x,bounds.center.y-eyeRect.center.y)}};
        var blink=new JArray(ColorKey(0,"ffffff00"),ColorKey(2.11f,"ffffff00"),ColorKey(2.16f,"ffffffff"),ColorKey(2.23f,"ffffffff"),ColorKey(2.31f,"ffffff00"),ColorKey(5.6f,"ffffff00"),ColorKey(5.66f,"ffffffff"),ColorKey(5.73f,"ffffffff"),ColorKey(5.82f,"ffffff00"),ColorKey(6,"ffffff00"),ColorKey(6.06f,"ffffffff"),ColorKey(6.12f,"ffffffff"),ColorKey(6.2f,"ffffff00"),ColorKey(10.9f,"ffffff00"));
        var character=Rig("ForestRedHood",new[]{body,eyes},new[]{"body","eyes"},Json("forest-redhood",bones,slots,attachments,new JObject{["Idle"]=new JObject{["slots"]=new JObject{["eyes"]=new JObject{["color"]=blink}}}}),.01f*393/body.height*pixel);
        var baseSource=Read("FireBase");var fireBase=Crop(baseSource,Bounds(baseSource,new RectInt(0,0,baseSource.width,baseSource.height)));var flameSource=Read("Flames");var parts=new Texture2D[4];parts[0]=fireBase;
        for(int i=0;i<3;i++){var r=new RectInt(i*flameSource.width/3,0,flameSource.width/3,flameSource.height);parts[i+1]=Crop(flameSource,Bounds(flameSource,r));}
        bones=new JArray(new JObject{{"name","root"}});slots=new JArray(new JObject{{"name","base"},{"bone","root"},{"attachment","base"}});attachments=new JObject{["base"]=new JObject{["base"]=Region("base",205,205f*fireBase.height/fireBase.width)}};
        var animations=new JObject();var deformClips=new JObject[3];var boneClips=new JObject[3];var colors=new JObject[3];for(int a=0;a<3;a++){deformClips[a]=new JObject();boneClips[a]=new JObject();colors[a]=new JObject();}
        float[] durations={3.2f,4.4f,5.6f};
        for(int i=0;i<3;i++){
            string n="flame"+i;bones.Add(new JObject{{"name",n},{"parent","root"},{"x",(i-1)*18},{"y",8}});slots.Add(new JObject{{"name",n},{"bone",n},{"attachment",n}});
            const int cols=4,rows=9;float w=new[]{35f,29f,31f}[i],h=new[]{66f,48f,55f}[i];var uv=new JArray();var verts=new JArray();var tri=new JArray();
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){uv.Add(x/(float)(cols-1));uv.Add(1-y/(float)(rows-1));verts.Add((x/(float)(cols-1)-.5f)*w);verts.Add(y/(float)(rows-1)*h);if(x<cols-1&&y<rows-1){int k=y*cols+x;foreach(int v in new[]{k,k+1,k+cols,k+1,k+cols+1,k+cols})tri.Add(v);}}
            attachments[n]=new JObject{[n]=new JObject{{"type","mesh"},{"path",n},{"uvs",uv},{"vertices",verts},{"triangles",tri},{"hull",0},{"width",parts[i+1].width},{"height",parts[i+1].height}}};
            for(int a=0;a<3;a++){
                var keys=new JArray();var scales=new JArray();var tint=new JArray();
                for(int k=0;k<=56;k++){float t=k/56f,phase=t*Mathf.PI*2,env=Mathf.Sin(t*Mathf.PI);var offsets=new JArray();for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){float v=y/(float)(rows-1);offsets.Add(Mathf.Sin(phase*(a+2)-v*4+i)*env*v*v*(5+a*2));offsets.Add(Mathf.Cos(phase*(3+a)+v*3+i)*env*v*v*4);}keys.Add(new JObject{{"time",t*durations[a]},{"vertices",offsets}});scales.Add(new JObject{{"time",t*durations[a]},{"x",1+Mathf.Sin(phase*(a+2)+i)*env*.08f},{"y",1+Mathf.Sin(phase*(a+3)+i)*env*.16f}});tint.Add(ColorKey(t*durations[a],"ffffff"+Mathf.RoundToInt(255*(.88f+Mathf.Sin(phase*4+i)*env*.1f)).ToString("x2")));}
                deformClips[a][n]=new JObject{[n]=keys};boneClips[a][n]=new JObject{["scale"]=scales};colors[a][n]=new JObject{["color"]=tint};
            }
        }
        for(int a=0;a<3;a++)animations["Burn"+(char)('A'+a)]=new JObject{["deform"]=new JObject{["default"]=deformClips[a]},["bones"]=boneClips[a],["slots"]=colors[a]};
        var fire=Rig("ForestCampfire",parts,new[]{"base","flame0","flame1","flame2"},Json("forest-campfire",bones,slots,attachments,animations),.01f*pixel);
        var background=Import(Root+"/Background.png");
        string scenePath=Root+"/BlackForestCamp.prefab";
        if(!File.Exists(scenePath)){
            var scene=Node("BlackForestCamp",null,new Vector2(1080,1920),Vector2.zero);try{var image=scene.gameObject.AddComponent<RawImage>();image.texture=background;image.raycastTarget=false;Actor("ForestRedHood",scene,character,new Vector2(305,-630)*pixel,"Idle",false);Actor("ForestCampfire",scene,fire,new Vector2(596,-972)*pixel,"BurnA",true);PrefabUtility.SaveAsPrefabAsset(scene.gameObject,scenePath);}finally{UnityEngine.Object.DestroyImmediate(scene.gameObject);}}
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);try{var layout=home.GetComponent<ShelterResponsiveLayout>();var selection=home.GetComponent<ShelterChapterBackdrop>();if(!selection){selection=home.AddComponent<ShelterChapterBackdrop>();selection.layout=layout;selection.cabin=layout.room;selection.cabinSize=layout.roomSize;selection.cabinFocalBottom=layout.feetInArtwork;var camp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(scenePath),layout.room.parent);camp.transform.SetSiblingIndex(layout.room.GetSiblingIndex());selection.forest=(RectTransform)camp.transform;selection.forest.gameObject.SetActive(false);}PrefabUtility.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(home);}
        // Explicit one-time wide framing migration; later builds preserve Inspector edits.
        if(!File.Exists(Root+"/wide-framing-v1.txt")){
            var wide=Import(Root+"/BackgroundWide.png");var camp=PrefabUtility.LoadPrefabContents(scenePath);
            try{var rt=(RectTransform)camp.transform;rt.sizeDelta=new Vector2(1440,1920);camp.GetComponent<RawImage>().texture=wide;
                foreach(RectTransform child in rt)child.anchoredPosition+=new Vector2(180,0);
                PrefabUtility.SaveAsPrefabAsset(camp,scenePath);
            }finally{PrefabUtility.UnloadPrefabContents(camp);}
            home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
            try{home.GetComponent<ShelterChapterBackdrop>().forestSize=new Vector2(1440,1920);PrefabUtility.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(home);}
            File.WriteAllText(Root+"/wide-framing-v1.txt","Wide background authored at 1440x1920; original core centered at x=180. Preserve manual edits.");
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Out+"asset-metrics.txt","Body bounds "+bounds+" eye rect "+eyeRect+"\nFire base "+fireBase.width+"x"+fireBase.height);
        foreach(var t in new[]{original,closed,body,eyes,baseSource,flameSource})UnityEngine.Object.DestroyImmediate(t);foreach(var t in parts)UnityEngine.Object.DestroyImmediate(t);
    }
}
