using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// Explicit Spine authoring, with independently animated flame meshes, embers and a foreground guard.
[InitializeOnLoad]
public static class ShelterHearthBuilder
{
    public const string Root=ShelterRefinedBuilder.Root+"/Hearth";
    static ShelterHearthBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("output/home-refined/fire.request"))return;File.Delete("output/home-refined/fire.request");try{Build();File.WriteAllText("output/home-refined/fire-result.txt","PASS");}catch(Exception e){File.WriteAllText("output/home-refined/fire-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string name,Func<T> create) where T:UnityEngine.Object{string p=Root+"/"+name;var v=AssetDatabase.LoadAssetAtPath<T>(p);if(!v){v=create();AssetDatabase.CreateAsset(v,p);}return v;}
    static Texture2D Crop(Texture2D t,Rect r){var n=new Texture2D((int)r.width,(int)r.height,TextureFormat.RGBA32,false);n.SetPixels(t.GetPixels((int)r.x,(int)r.y,n.width,n.height));n.Apply();return n;}
    static JObject Key(float t,string x,float a,string y,float b)=>new JObject{{"time",t},{x,a},{y,b}};
    [MenuItem("Tools/Dark Fairytale/UI/Build Hearth Spine (Explicit)")]
    public static void Build(){
        Directory.CreateDirectory(Root);Directory.CreateDirectory("ArtDirection/UI/CursedStorybook/Home/Refined/HearthSource/images");
        var f=ShelterRefinedBuilder.Import(ShelterRefinedBuilder.Root+"/Flames.png");var g=ShelterRefinedBuilder.Import(ShelterRefinedBuilder.Root+"/Fireguard.png");
        var parts=new Texture2D[5];for(int i=0;i<4;i++)parts[i]=Crop(f,ShelterRefinedBuilder.Trim(f,new RectInt(i%2*f.width/2,(1-i/2)*f.height/2,f.width/2,f.height/2)));parts[4]=Crop(g,ShelterRefinedBuilder.Trim(g,new RectInt(0,0,g.width,g.height)));
        var atlas=new Texture2D(2048,2048,TextureFormat.RGBA32,false);var rects=atlas.PackTextures(parts,4,2048,false);File.WriteAllBytes(Root+"/Hearth.png",atlas.EncodeToPNG());
        var text=new StringBuilder("Hearth.png\nsize: "+atlas.width+","+atlas.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<5;i++){string n=i==4?"guard":"flame"+i;var r=rects[i];int w=Mathf.RoundToInt(r.width*atlas.width),h=Mathf.RoundToInt(r.height*atlas.height);text.Append(n+"\n  rotate: false\n  xy: "+Mathf.RoundToInt(r.x*atlas.width)+", "+Mathf.RoundToInt((1-r.yMax)*atlas.height)+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1\n");File.WriteAllBytes("ArtDirection/UI/CursedStorybook/Home/Refined/HearthSource/images/"+n+".png",parts[i].EncodeToPNG());}
        var texture=ShelterRefinedBuilder.Import(Root+"/Hearth.png");
        var mat=Asset("Hearth_UI.mat",()=>new Material(Shader.Find("Spine/SkeletonGraphic")));mat.mainTexture=texture;mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        File.WriteAllText(Root+"/Hearth.atlas.txt",text.ToString());AssetDatabase.ImportAsset(Root+"/Hearth.atlas.txt",ImportAssetOptions.ForceSynchronousImport);
        var bones=new JArray(new JObject{{"name","root"}});var slots=new JArray();var attachments=new JObject();var boneKeys=new JObject();var deform=new JObject();var slotKeys=new JObject();
        for(int i=0;i<6;i++){
            string n="flame_"+i;float x=new[]{-70,-20,38,-46,13,63}[i];float width=new[]{125,105,108,95,82,76}[i];float height=new[]{166,238,181,151,207,147}[i];float period=new[]{4,2,4,2,4,2}[i];
            bones.Add(new JObject{{"name",n},{"parent","root"},{"x",x},{"y",18+i%2*5}});slots.Add(new JObject{{"name",n},{"bone",n},{"attachment",n},{"color",i<3?"ffffffff":"ffffffd9"}});
            const int cols=5,rows=13;var uv=new JArray();var verts=new JArray();var tri=new JArray();
            for(int y=0;y<rows;y++)for(int j=0;j<cols;j++){uv.Add((float)j/(cols-1));uv.Add(1-(float)y/(rows-1));verts.Add(((float)j/(cols-1)-.5f)*width);verts.Add((float)y/(rows-1)*height);if(y<rows-1&&j<cols-1){int a=y*cols+j;foreach(int v in new[]{a,a+1,a+cols,a+1,a+cols+1,a+cols})tri.Add(v);}}
            attachments[n]=new JObject{[n]=new JObject{{"type","mesh"},{"path","flame"+(i%4)},{"uvs",uv},{"vertices",verts},{"triangles",tri},{"hull",0},{"width",parts[i%4].width},{"height",parts[i%4].height}}};
            var keys=new JArray();var scales=new JArray();var rots=new JArray();
            for(int k=0;k<=32;k++){float t=k*.25f;float phase=t/period*Mathf.PI*2+i*1.13f;var offsets=new JArray();for(int y=0;y<rows;y++)for(int j=0;j<cols;j++){float v=(float)y/(rows-1);offsets.Add(Mathf.Sin(phase-v*5+j*.17f)*v*v*12);offsets.Add(Mathf.Sin(phase*2+v*3)*v*v*7);}keys.Add(new JObject{{"time",t},{"vertices",offsets}});scales.Add(Key(t,"x",1+Mathf.Sin(phase)*.055f,"y",1+Mathf.Sin(phase+1)*.085f));rots.Add(new JObject{{"time",t},{"angle",Mathf.Sin(phase)*2.3f}});}
            deform[n]=new JObject{[n]=keys};boneKeys[n]=new JObject{{"scale",scales},{"rotate",rots}};
        }
        for(int i=0;i<9;i++){
            string n="ember_"+i;bones.Add(new JObject{{"name",n},{"parent","root"},{"x",-80+i*20},{"y",35}});slots.Add(new JObject{{"name",n},{"bone",n},{"attachment",n}});attachments[n]=new JObject{[n]=new JObject{{"path","flame1"},{"width",3+i%3},{"height",7+i%4}}};
            var travel=new JArray();var colors=new JArray();float period=i%2==0?4:2;float offset=(i*.37f)%period;
            for(int k=0;k<=160;k++){float t=k*.05f;float phase=((t+offset)%period)/period;float alpha=Mathf.Sin(phase*Mathf.PI);alpha*=alpha;travel.Add(Key(t,"x",Mathf.Sin(phase*5+i)*14,"y",phase*(155+i%3*17)));colors.Add(new JObject{{"time",t},{"color","fff2b8"+Mathf.RoundToInt(alpha*170).ToString("x2")}});}
            // Duplicate the opening pose exactly; motion resets while alpha is effectively zero.
            travel[160]=travel[0].DeepClone();travel[160]["time"]=8;colors[160]=colors[0].DeepClone();colors[160]["time"]=8;
            boneKeys[n]=new JObject{{"translate",travel}};slotKeys[n]=new JObject{{"color",colors}};
        }
        slots.Add(new JObject{{"name","guard"},{"bone","root"},{"attachment","guard"}});attachments["guard"]=new JObject{["guard"]=new JObject{{"path","guard"},{"x",0},{"y",42},{"width",260},{"height",90}}};
        var burn=new JObject { ["bones"]=boneKeys, ["slots"]=slotKeys, ["deform"]=new JObject { ["default"]=deform } };
        var json=new JObject { ["skeleton"]=new JObject { ["hash"]="shelter-hearth-v1", ["spine"]="3.8.99", ["width"]=260, ["height"]=280, ["images"]="images/" }, ["bones"]=bones, ["slots"]=slots, ["skins"]=new JArray(new JObject { ["name"]="default", ["attachments"]=attachments }), ["animations"]=new JObject { ["Burn"]=burn } };
        File.WriteAllText(Root+"/Hearth.json",json.ToString());File.WriteAllText("ArtDirection/UI/CursedStorybook/Home/Refined/HearthSource/Hearth.json",json.ToString());AssetDatabase.ImportAsset(Root+"/Hearth.json",ImportAssetOptions.ForceSynchronousImport);
        var aa=Asset("Hearth_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());aa.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Hearth.atlas.txt");aa.materials=new[]{mat};aa.Clear();EditorUtility.SetDirty(aa);
        var data=Asset("Hearth_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Hearth.json");data.atlasAssets=new AtlasAssetBase[]{aa};data.scale=.01f;data.Clear();EditorUtility.SetDirty(data);if(data.GetSkeletonData(false)?.FindAnimation("Burn")==null)throw new Exception("Hearth animation failed to load");AssetDatabase.SaveAssets();
        string pp=Root+"/Hearth.prefab";if(!File.Exists(pp)){var go=new GameObject("Hearth",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkeletonGraphic));try{var sg=go.GetComponent<SkeletonGraphic>();sg.skeletonDataAsset=data;sg.material=mat;sg.startingAnimation="Burn";sg.startingLoop=true;sg.unscaledTime=true;sg.raycastTarget=false;sg.Initialize(true);((RectTransform)go.transform).sizeDelta=new Vector2(270,290);PrefabUtility.SaveAsPrefabAsset(go,pp);}finally{UnityEngine.Object.DestroyImmediate(go);}}
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);try{var room=home.GetComponent<ShelterResponsiveLayout>().room;if(!room.Find("Hearth")){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pp),room);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(108,-839);r.SetAsFirstSibling();}PrefabUtility.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(home);}AssetDatabase.SaveAssets();foreach(var p in parts)UnityEngine.Object.DestroyImmediate(p);UnityEngine.Object.DestroyImmediate(atlas);
    }
}
