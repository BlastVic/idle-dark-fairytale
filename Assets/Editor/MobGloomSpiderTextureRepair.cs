using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

// Explicit deterministic extraction of imagegen parts. Never changes the rig or animation JSON.
public static class MobGloomSpiderTextureRepair
{
    const string Source="ArtDirection/Monsters/C032_S01B/source/MobGloomSpider";
    const string Paint=Source+"/repair-v2";
    static string Original => Directory.Exists("../Spine/monster2/动画/C032_S01B") ? "../Spine/monster2/动画/C032_S01B" : "../Spine/monster2/C032_S01B";
    static Texture2D Read(string path)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
        t.LoadImage(File.ReadAllBytes(path));t.wrapMode=TextureWrapMode.Clamp;
        return t;
    }
    static RectInt Bounds(Texture2D texture, RectInt search, Func<int,int,bool> include=null)
    {
        int x0=search.xMax,y0=search.yMax,x1=-1,y1=-1;
        for(int y=search.yMin;y<search.yMax;y++)for(int x=search.xMin;x<search.xMax;x++)
            if(texture.GetPixel(x,y).a>.08f && (include==null || include(x,y)))
            {x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
        if(x1<x0)throw new Exception("Empty source cell");
        return new RectInt(x0,y0,x1-x0+1,y1-y0+1);
    }
    static RectInt Cell(Texture2D texture,string name,bool legs)
    {
        string[] row1={"Front_Eye","Front_Eye_L_01","Front_Eye_L_02","Front_Eye_L_03","Front_Eye_R_01","Front_Eye_R_02"};
        string[] row2={"Front_Eye_R_03","Front_EyeLid_L_01","Front_EyeLid_R_01","Front_Mouth_L_01","Front_Mouth_R_01"};
        float x0,x1,y0,y1;
        if(legs)
        {
            var names=((JObject)JObject.Parse(File.ReadAllText(Source+"/layout.json"))).Properties().Where(p=>(string)p.Value["group"]=="legs").Select(p=>p.Name).ToArray();
            int n=Array.IndexOf(names,name);if(n<0)throw new Exception(name);
            x0=n%4/4f;x1=(n%4+1)/4f;y0=n/4*.32f;y1=n/4==2?1:(n/4+1)*.32f;
        }
        else
        {
            int n=Array.IndexOf(row1,name);
            if(n>=0){x0=n/6f;x1=(n+1)/6f;y0=.60f;y1=.775f;}
            else{n=Array.IndexOf(row2,name);if(n<0)throw new Exception(name);x0=n/6f;x1=(n+1)/6f;y0=.775f;y1=1;}
        }
        return new RectInt(Mathf.RoundToInt(x0*texture.width),Mathf.RoundToInt((1-y1)*texture.height),Mathf.RoundToInt((x1-x0)*texture.width),Mathf.RoundToInt((y1-y0)*texture.height));
    }
    static bool Inside(JToken attachment,int x,int y,int w,int h)
    {
        if((string)attachment["type"]!="mesh")return true;
        var uv=(JArray)attachment["uvs"];var tri=(JArray)attachment["triangles"];
        Vector2 p=new Vector2((x+.5f)/w,1-(y+.5f)/h);
        for(int i=0;i<tri.Count;i+=3)
        {
            var a=new Vector2((float)uv[(int)tri[i]*2],(float)uv[(int)tri[i]*2+1]);
            var b=new Vector2((float)uv[(int)tri[i+1]*2],(float)uv[(int)tri[i+1]*2+1]);
            var c=new Vector2((float)uv[(int)tri[i+2]*2],(float)uv[(int)tri[i+2]*2+1]);
            float ab=Cross(p,a,b),bc=Cross(p,b,c),ca=Cross(p,c,a);
            if(!((ab<0||bc<0||ca<0)&&(ab>0||bc>0||ca>0)))return true;
        }
        return false;
    }
    static float Cross(Vector2 p,Vector2 a,Vector2 b)=>(p.x-b.x)*(a.y-b.y)-(a.x-b.x)*(p.y-b.y);
    [MenuItem("Tools/Dark Fairytale/Monsters/Repack MobGloomSpider Repaired Parts")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
        var rig=JObject.Parse(File.ReadAllText(Original+"/C032_S01B.json"));
        var attachments=((JObject)rig["skins"][0]["attachments"]).Properties().SelectMany(p=>((JObject)p.Value).Properties()).ToDictionary(p=>p.Name,p=>p.Value);
        var layout=JObject.Parse(File.ReadAllText(Source+"/layout.json"));
        var textures=new List<Texture2D>();var names=new List<string>();var report=new JArray();
        var main=Read(Paint+"/main.png");var legs=Read(Paint+"/legs.png");var blink=Read(Paint+"/blink.png");
        foreach(var entry in layout.Properties())
        {
            string name=entry.Name;Texture2D result;
            if(name=="Front_Body_01" || name=="Front_Head_01")
                result=Read(Source+"/before-repair-v2/images/"+name+".png");
            else
            {
                var original=Read(Original+"/images/"+name+".png");
                var target=Bounds(original,new RectInt(0,0,original.width,original.height),(x,y)=>Inside(attachments[name],x,y,original.width,original.height));
                var sheet=(string)entry.Value["group"]=="legs"?legs:main;
                var crop=Bounds(sheet,Cell(sheet,name,sheet==legs));
                bool expression = new[]{"Front_Eye_L_02","Front_Eye_L_03","Front_Eye_R_02","Front_Eye_R_03"}.Contains(name);
                if(expression || name.Contains("EyeLid"))
                {
                    // Sparse expression marks retain the previous full-eye canvas registration.
                    if(name.Contains("EyeLid"))crop=Bounds(blink,Cell(blink,name,false));
                    sheet=blink;
                }
                result=new Texture2D(original.width*2,original.height*2,TextureFormat.RGBA32,false);
                result.SetPixels(new Color[result.width*result.height]);
                for(int y=target.y*2;y<target.yMax*2;y++)for(int x=target.x*2;x<target.xMax*2;x++)
                {
                    float u=((x+.5f)/2-target.x)/target.width,v=((y+.5f)/2-target.y)/target.height;
                    // Use generated alpha; never convert transparent background RGB into opaque pixels.
                    var color=sheet.GetPixelBilinear((crop.x+u*crop.width)/sheet.width,(crop.y+v*crop.height)/sheet.height);
                    result.SetPixel(x,y,color);
                }
                result.Apply();
                report.Add(new JObject{["part"]=name,["sourceBounds"]=JToken.FromObject(new[]{crop.x,crop.y,crop.width,crop.height}),["targetBounds"]=JToken.FromObject(new[]{target.x,target.y,target.width,target.height})});
                UnityEngine.Object.DestroyImmediate(original);
            }
            File.WriteAllBytes(Source+"/images/"+name+".png",result.EncodeToPNG());textures.Add(result);names.Add(name);
        }
        textures.Add(Read(Original+"/images/shadow.png"));names.Add("shadow");
        var atlas=new Texture2D(2,2,TextureFormat.RGBA32,false);
        var rects=atlas.PackTextures(textures.ToArray(),8,2048,false);
        var text=new StringBuilder("\nMobGloomSpider.png\nsize: "+atlas.width+","+atlas.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int i=0;i<names.Count;i++)
        {
            var r=rects[i];int x=Mathf.RoundToInt(r.x*atlas.width),y=Mathf.RoundToInt((1-r.yMax)*atlas.height),w=Mathf.RoundToInt(r.width*atlas.width),h=Mathf.RoundToInt(r.height*atlas.height);
            if(w!=textures[i].width || h!=textures[i].height)throw new Exception("Atlas unexpectedly rescaled "+names[i]);
            text.AppendLine(names[i]+"\n  rotate: false\n  xy: "+x+", "+y+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1");
        }
        foreach(var dest in new[]{Source,MobGloomSpiderBuilder.Dir,"../Spine/export/monster/MobGloomSpider"})
        {
            Directory.CreateDirectory(dest);File.WriteAllBytes(dest+"/MobGloomSpider.png",atlas.EncodeToPNG());File.WriteAllText(dest+"/MobGloomSpider.atlas.txt",text.ToString());
        }
        File.Copy(Source+"/MobGloomSpider.json", "../Spine/export/monster/MobGloomSpider/MobGloomSpider.json", true);
        File.WriteAllText(Paint+"/registration.json",report.ToString());
        foreach(var t in textures)UnityEngine.Object.DestroyImmediate(t);
        UnityEngine.Object.DestroyImmediate(main);UnityEngine.Object.DestroyImmediate(legs);UnityEngine.Object.DestroyImmediate(blink);UnityEngine.Object.DestroyImmediate(atlas);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var atlasAsset=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(MobGloomSpiderBuilder.Dir+"/MobGloomSpider_Atlas.asset");atlasAsset.Clear();
        AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(MobGloomSpiderBuilder.Dir+"/MobGloomSpider_SkeletonData.asset").Clear();
        MobGloomSpiderBuilder.PrepareAcceptance();
        Debug.Log("MobGloomSpider repaired parts repacked; bone/animation JSON not modified.");
    }
}
