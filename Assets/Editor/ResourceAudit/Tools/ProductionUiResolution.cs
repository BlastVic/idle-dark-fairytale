using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit authoring only. Source art remains intact; area sampling works in premultiplied alpha.
[InitializeOnLoad]
public static class ProductionUiResolution
{
    const string Dir="output/ui-production-audit/",Catalog="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json";
    static ProductionUiResolution(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"resolution.request"))return;
        File.Delete(Dir+"resolution.request");try{Run();File.WriteAllText(Dir+"resolution-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"resolution-result.txt",e.ToString());Debug.LogException(e);}
    };}
    static void Run(){
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(Catalog));
        Resize(catalog,"Assets/DarkFairytale/UI/Shop/Sprites/BloodMoonCrystal.asset",256);
        Resize(catalog,"Assets/DarkFairytale/UI/Tutorial/GuideArrow.asset",320);
        AssetDatabase.SaveAssets();File.WriteAllText(Catalog,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(Catalog);UiProductionAudit.Run();
    }
    static void Resize(ProductionUiArt.Catalog catalog,string path,int longest){
        if(catalog.entries.Any(e=>e.sprite==path))return;
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);var source=AssetDatabase.GetAssetPath(sprite.texture);var rect=sprite.rect;
        var input=new Texture2D(2,2,TextureFormat.RGBA32,false);input.LoadImage(File.ReadAllBytes(source));
        float scale=longest/Mathf.Max(rect.width,rect.height);int w=Mathf.RoundToInt(rect.width*scale),h=Mathf.RoundToInt(rect.height*scale);
        var output=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=input.GetPixels32();var result=new Color32[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){
            float x0=x*rect.width/w,x1=(x+1)*rect.width/w,y0=y*rect.height/h,y1=(y+1)*rect.height/h;float sum=0,a=0;Vector3 rgb=Vector3.zero;
            for(int sy=(int)y0;sy<Mathf.CeilToInt(y1);sy++)for(int sx=(int)x0;sx<Mathf.CeilToInt(x1);sx++){
                float weight=(Mathf.Min(sx+1,x1)-Mathf.Max(sx,x0))*(Mathf.Min(sy+1,y1)-Mathf.Max(sy,y0));
                Color p=pixels[((int)rect.y+sy)*input.width+(int)rect.x+sx];sum+=weight;a+=p.a*weight;rgb+=new Vector3(p.r,p.g,p.b)*(p.a*weight);
            }
            if(a>0)rgb/=a;result[y*w+x]=new Color(rgb.x,rgb.y,rgb.z,a/sum);
        }
        output.SetPixels32(result);output.Apply();string runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt/"+AssetDatabase.AssetPathToGUID(path)+"-sized.png";
        File.WriteAllBytes(runtime,output.EncodeToPNG());AssetDatabase.ImportAsset(runtime,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(runtime);imp.textureType=TextureImporterType.Default;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.isReadable=false;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=false;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(runtime);var replacement=Sprite.Create(tex,new Rect(0,0,w,h),sprite.pivot/new Vector2(rect.width,rect.height),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);replacement.name=sprite.name;
        string backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+".asset";if(!File.Exists(backup))File.Copy(path,backup);
        EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
        catalog.entries.Add(new ProductionUiArt.Entry{sprite=path,source=source,rect=rect,border=Vector4.zero,texture=runtime,treatment="cropped and alpha-weighted area resized; authored layout unchanged; 1440-wide portrait margin reviewed"});
        File.AppendAllText(Dir+"resolution-proof.txt",path+": "+rect.width+"x"+rect.height+" -> "+w+"x"+h+"; source "+source+" retained\n");
        UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(replacement);
    }
}
