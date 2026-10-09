using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;

// Lossless coordinate reference and deterministic slicing only; artwork is painted separately.
public static class MushroomOriginalSkinSheet
{
    public static readonly string[] Parts = { "head", "head_back", "body_02", "body_01", "center", "eye", "arm_left_01", "arm_right_01", "arm_left_02", "arm_right_02", "leg_left", "leg_right", "body_back", "shadow" };
    public const string Dir = "ArtDirection/Monsters/BlackForestMushroom/source/original-rig";
    [InitializeOnLoadMethod] static void Prepare() { if (!File.Exists(Dir+"/reference.png")) EditorApplication.delayCall += MakeReference; }
    public static RectInt PartRect(int i, Texture2D input)
    {
        float scale = 320f / Mathf.Max(input.width,input.height);
        int w = Mathf.RoundToInt(input.width*scale), h = Mathf.RoundToInt(input.height*scale);
        return new RectInt(i%4*384+(384-w)/2,1536-(i/4+1)*384+(384-h)/2,w,h);
    }
    static JObject attachments;
    static float Cross(Vector2 a,Vector2 b,Vector2 c) { return (a.x-c.x)*(b.y-c.y)-(b.x-c.x)*(a.y-c.y); }
    public static bool Inside(string part,float u,float v)
    {
        if(attachments==null) attachments=(JObject)JObject.Parse(File.ReadAllText("../Spine/monster/mon_7080/mon_7080.json"))["skins"][0]["attachments"];
        var mesh=attachments.Properties().SelectMany(p=>((JObject)p.Value).Properties()).Select(p=>p.Value).First(p=>(string)p["name"]==part || (string)p["path"]==part || p.Parent is JProperty prop && prop.Name==part);
        var uv=(JArray)mesh["uvs"]; var tri=(JArray)mesh["triangles"]; var point=new Vector2(u,v);
        for(int t=0;t<tri.Count;t+=3) {
            int a=(int)tri[t]*2,b=(int)tri[t+1]*2,c=(int)tri[t+2]*2;
            var va=new Vector2((float)uv[a],(float)uv[a+1]); var vb=new Vector2((float)uv[b],(float)uv[b+1]); var vc=new Vector2((float)uv[c],(float)uv[c+1]);
            float d1=Cross(point,va,vb),d2=Cross(point,vb,vc),d3=Cross(point,vc,va);
            if(!((d1<0 || d2<0 || d3<0)&&(d1>0 || d2>0 || d3>0))) return true;
        }
        return false;
    }
    static void MakeReference()
    {
        Directory.CreateDirectory(Dir);
        var sheet = new Texture2D(1536,1536,TextureFormat.RGBA32,false);
        sheet.SetPixels(new Color[1536*1536]);
        for(int i=0;i<Parts.Length;i++) {
            var input= new Texture2D(2,2); input.LoadImage(File.ReadAllBytes("../Spine/monster/mon_7080/images/"+Parts[i]+".png"));
            var rect=PartRect(i,input);
            for(int y=0;y<rect.height;y++) for(int x=0;x<rect.width;x++) sheet.SetPixel(rect.x+x,rect.y+y,Inside(Parts[i],(x+.5f)/rect.width,1-(y+.5f)/rect.height) ? input.GetPixelBilinear((x+.5f)/rect.width,(y+.5f)/rect.height) : Color.clear);
            Object.DestroyImmediate(input);
        }
        sheet.Apply(); File.WriteAllBytes(Dir+"/reference.png",sheet.EncodeToPNG()); Object.DestroyImmediate(sheet);
    }
}
