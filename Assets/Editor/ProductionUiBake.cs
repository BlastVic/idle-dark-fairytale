using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit offline preparation of existing pixels. This tool never reconstructs a UI at runtime.
[InitializeOnLoad]
public static class ProductionUiBake
{
    const string Dir="output/ui-production-audit/";
    [Serializable] public class Item {public string sprite,material,png;public bool clearCenter,mesh;}
    [Serializable] public class Batch {public List<Item> items=new List<Item>();}
    static ProductionUiBake(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"bake-reviewed.request"))return;
        File.Delete(Dir+"bake-reviewed.request");try{Run();File.WriteAllText(Dir+"bake-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"bake-result.txt",e.ToString());Debug.LogException(e);}
    };}
    static readonly string[] Names={"BackRibbon","Film","CategoryIconV40","PaperButtonFrame","NavigationBookmarkBorder","Skills","Shelter","Map","OriginalLocationPin","RewardPreviewFrame","ReadySeal","ExperienceDark","HealthFill","HealthFrame","ExperienceBlue","ExperienceFrame","NavigationSkillBookV4","ForestEngraving","ForestFooter","ClosedChestMesh","CrystalRewardMesh","EquipmentRewardMesh"};
    public static void Run(){
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var batch=new Batch();Directory.CreateDirectory(Dir+"baked-review");
        foreach(var group in report.entries.Where(e=>!string.IsNullOrEmpty(e.sprite)&&Names.Contains(Path.GetFileNameWithoutExtension(e.sprite))).GroupBy(e=>e.sprite)){
            foreach(var uses in group.GroupBy(e=>e.material))batch.items.Add(new Item{sprite=group.Key,material=uses.Key,clearCenter=uses.All(e=>e.imageType=="Sliced"&&!e.fillCenter),mesh=Path.GetFileNameWithoutExtension(group.Key)=="Skills"});
        }
        // Serialized state variants are not necessarily the currently displayed Image.
        batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Chest/Sprites/ReadyChestMesh.asset",material="Assets/DarkFairytale/UI/Chest/ReadyChestMesh.mat"});

        foreach(var n in new[]{"AvailableSealClean","LockedSealClean"})batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Map/Sprites/"+n+".asset",material="Assets/DarkFairytale/UI/Map/"+n+".mat"});
        batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Map/Sprites/WolfSeal.asset",material="Assets/DarkFairytale/UI/Map/WolfCircle.mat"});
        batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Shelter/Reference/Sprites/TimerPlate.asset",material="",clearCenter=true});
        batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Fusion/Sprites/RuinCharged.asset",material=""});
        batch.items.Add(new Item{sprite="Assets/DarkFairytale/UI/Tutorial/QuillBookmark.asset",material=""});
        var prepared=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText("Assets/Editor/ProductionUiArtCatalog.json")).entries.Select(e=>e.sprite).ToArray();
        batch.items=batch.items.Where(i=>!prepared.Contains(i.sprite)).ToList();
        batch.items=batch.items.GroupBy(i=>i.sprite+"|"+i.material).Select(g=>g.First()).ToList();
        foreach(var item in batch.items){
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(item.sprite);if(!sprite)throw new Exception("Missing sprite "+item.sprite);
            var original=string.IsNullOrEmpty(item.material)?null:AssetDatabase.LoadAssetAtPath<Material>(item.material);if(!string.IsNullOrEmpty(item.material)&&!original)throw new Exception("Missing material "+item.material);
            var shader=original?original.shader:Shader.Find("UI/Default");
            var bakeShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Editor/UiBakeShaders/"+(original?Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(shader)):"Default")+".shader");
            if(!bakeShader)throw new Exception("Missing editor-only baker for "+shader.name);
            var material=new Material(bakeShader);if(original)material.CopyPropertiesFromMaterial(original);material.mainTexture=sprite.texture;material.SetFloat("_StencilComp",8);material.SetFloat("_Stencil",0);material.SetFloat("_ColorMask",15);
            material.DisableKeyword("UNITY_UI_CLIP_RECT");material.DisableKeyword("UNITY_UI_ALPHACLIP");
            bool intactBook=Path.GetFileNameWithoutExtension(item.sprite)=="NavigationSkillBookV4";
            if(intactBook){material.SetFloat("_Mode",0);material.SetFloat("_InkOnly",0);}
            int w=(int)sprite.rect.width,h=(int)sprite.rect.height;var r=sprite.rect;
            var mesh=new Mesh();
            if(intactBook){
                var points=new[]{new Vector2(0,0),new Vector2(76,0),new Vector2(84,32),new Vector2(92,32),new Vector2(92,84),new Vector2(0,84)};
                mesh.vertices=points.Select(p=>new Vector3(p.x,h-p.y,0)).ToArray();mesh.uv=points.Select(p=>new Vector2((r.x+p.x)/sprite.texture.width,(r.y+h-p.y)/sprite.texture.height)).ToArray();mesh.triangles=new[]{5,0,1,5,1,2,5,2,3,5,3,4};
            }
            else if(item.mesh){mesh.vertices=sprite.vertices.Select(v=>new Vector3(v.x*sprite.pixelsPerUnit+sprite.pivot.x,v.y*sprite.pixelsPerUnit+sprite.pivot.y,0)).ToArray();mesh.uv=sprite.uv;mesh.triangles=sprite.triangles.Select(v=>(int)v).ToArray();}
            else{mesh.vertices=new[]{new Vector3(0,0),new Vector3(w,0),new Vector3(w,h),new Vector3(0,h)};mesh.uv=new[]{new Vector2(r.x/sprite.texture.width,r.y/sprite.texture.height),new Vector2(r.xMax/sprite.texture.width,r.y/sprite.texture.height),new Vector2(r.xMax/sprite.texture.width,r.yMax/sprite.texture.height),new Vector2(r.x/sprite.texture.width,r.yMax/sprite.texture.height)};mesh.triangles=new[]{0,1,2,0,2,3};}
            mesh.colors=Enumerable.Repeat(Color.white,mesh.vertexCount).ToArray();
            var rt=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;bool srgb=GL.sRGBWrite;Texture2D image=null;
            try{
                ShaderUtil.CompilePass(material,0,true);
                using(var command=new CommandBuffer()){
                    command.SetRenderTarget(rt);command.SetViewport(new Rect(0,0,w,h));command.ClearRenderTarget(true,true,Color.clear);
                    command.SetViewProjectionMatrices(Matrix4x4.identity,GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0,w,0,h,-1,1),false));
                    command.DrawMesh(mesh,Matrix4x4.identity,material,0,0);GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.ExecuteCommandBuffer(command);
                }
                RenderTexture.active=rt;
                image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();
                if(image.GetPixels32().Count(c=>c.a>8)<w*h*.005f)throw new Exception("Offline draw unexpectedly empty: "+item.sprite);
                if(item.clearCenter){var b=sprite.border;for(int y=(int)b.y+1;y<h-(int)b.w-1;y++)for(int x=(int)b.x+1;x<w-(int)b.z-1;x++)image.SetPixel(x,y,Color.clear);image.Apply();}
                if(Path.GetFileNameWithoutExtension(item.sprite)=="WolfSeal"){
                    image.LoadImage(File.ReadAllBytes("Assets/ArtSources/UI/MapRepairs/WolfSeal-original-alpha.png"));
                    var patch=new Texture2D(2,2,TextureFormat.RGBA32,false);patch.LoadImage(File.ReadAllBytes("Assets/ArtSources/UI/MapRepairs/WolfSeal-repair-source.png"));
                    for(int top=24;top<72;top++)for(int x=61;x<w;x++){
                        int y=h-1-top;float blend=Mathf.Min(Mathf.Clamp01((x-61)/2f),Mathf.Min(Mathf.Clamp01((top-24)/2f),Mathf.Clamp01((71-top)/2f)));
                        image.SetPixel(x,y,Color.Lerp(image.GetPixel(x,y),patch.GetPixelBilinear((x+.5f)/w,(y+.5f)/h),blend));
                    }
                    image.Apply();UnityEngine.Object.DestroyImmediate(patch);
                }
                item.png=Dir+"baked-review/"+AssetDatabase.AssetPathToGUID(item.sprite)+"-"+(original?AssetDatabase.AssetPathToGUID(item.material):"default")+".png";File.WriteAllBytes(item.png,image.EncodeToPNG());
            }finally{GL.sRGBWrite=srgb;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(image)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(mesh);}
        }
        File.WriteAllText(Dir+"baked-review.json",JsonUtility.ToJson(batch,true));
    }
}
