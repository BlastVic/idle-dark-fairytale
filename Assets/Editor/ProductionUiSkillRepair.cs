using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Assembles only the user-approved inpainted corner into otherwise unchanged source pixels.</summary>
[InitializeOnLoad]
public static class ProductionUiSkillRepair
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ProductionUiArtCatalog.json";
    static ProductionUiSkillRepair(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"repair-skills.request"))return;
        File.Delete(Dir+"repair-skills.request");try{Run();File.WriteAllText(Dir+"skills-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"skills-result.txt",e.ToString());Debug.LogException(e);}
    };}
    static readonly int[] Locked={2,5,8,9,11,12,13,14,15};
    static readonly float[] Left={.81f,.84f,.78f,.78f,.81f,.81f,.73f,.75f,.78f};
    static readonly float[] Top={.69f,.70f,.64f,.68f,.68f,.67f,.67f,.66f,.55f};
    [MenuItem("Tools/Resource Optimization/Assemble Approved Skill Edge Repairs")]
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scene edits before migration.");
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        Directory.CreateDirectory(Dir+"skills-assembled");Directory.CreateDirectory("Assets/DarkFairytale/UI/Skills/RuntimeArt");
        var proof=new System.Text.StringBuilder(File.Exists(Dir+"skills-pixel-proof.txt")?File.ReadAllText(Dir+"skills-pixel-proof.txt"):"Only approved corner rectangles use generated pixels. Outside each rectangle the source RGB pixels remain exact before alpha preparation.\n");
        for(int id=1;id<=15;id++){
            string path="Assets/DarkFairytale/UI/Skills/Sprites/CleanSkill"+id+".asset";
            var previous=catalog.entries.FirstOrDefault(e=>e.sprite==path);
            if(previous!=null&&(id!=15||previous.treatment.Contains("repair v2")))continue;
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new Exception("Missing "+path);
            var uses=report.entries.Where(e=>e.sprite==path).ToArray();if(uses.Length==0)throw new Exception("Unused skill "+path);
            string canonical="Assets/DarkFairytale/UI/Skills/CleanIcon"+id+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(canonical);
            foreach(var use in uses.Where(e=>previous==null)){var variant=AssetDatabase.LoadAssetAtPath<Material>(use.material);if(!variant||variant.shader.name!="UI/Skill Illustration Cutout"||variant.GetFloat("_Mode")!=0||variant.GetFloat("_InkOnly")!=0||variant.GetFloat("_Cutoff")!=material.GetFloat("_Cutoff")||variant.GetFloat("_Feather")!=material.GetFloat("_Feather"))throw new Exception("Incompatible skill variant "+use.material);}
            if(!material||material.shader.name!="UI/Skill Illustration Cutout"||material.GetFloat("_Mode")!=0||material.GetFloat("_InkOnly")!=0)throw new Exception("Unexpected skill material "+path);
            var source=previous!=null?previous.source:AssetDatabase.GetAssetPath(sprite.texture);var raw=Read(source);var rect=previous!=null?previous.rect:sprite.rect;int w=(int)rect.width,h=(int)rect.height;
            var pixels=raw.GetPixels((int)rect.x,(int)rect.y,w,h);var original=(Color[])pixels.Clone();
            int index=Array.IndexOf(Locked,id),left=w,top=0;
            if(index>=0){
                var patch=Read("Assets/ArtSources/UI/SkillRepairs/CleanSkill"+id+"-repair-source.png");
                left=Mathf.FloorToInt(w*Left[index]);top=Mathf.FloorToInt(h*Top[index]);int bottomHeight=h-top;
                for(int y=0;y<bottomHeight;y++)for(int x=left;x<w;x++){
                    float blend=Mathf.Min(Mathf.Clamp01((x-left)/2f),Mathf.Clamp01((bottomHeight-1-y)/2f));
                    pixels[y*w+x]=Color.Lerp(pixels[y*w+x],patch.GetPixelBilinear((x+.5f)/w,(y+.5f)/h),blend);
                }
                UnityEngine.Object.DestroyImmediate(patch);
            }
            int outsideChanges=0;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if((x<left||y>=h-top)&&pixels[y*w+x]!=original[y*w+x])outsideChanges++;
            if(outsideChanges!=0)throw new Exception("Source identity changed outside approved patch: "+path);
            var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.SetPixels(pixels);image.Apply();File.WriteAllBytes(Dir+"skills-assembled/CleanSkill"+id+"-paper.png",image.EncodeToPNG());
            float cutoff=material.GetFloat("_Cutoff"),feather=material.GetFloat("_Feather");
            for(int i=0;i<pixels.Length;i++){var c=pixels[i];float t=Mathf.Clamp01((c.r*.299f+c.g*.587f+c.b*.114f-cutoff)/feather);c.a*=1-t*t*(3-2*t);pixels[i]=c;}
            image.SetPixels(pixels);image.Apply();string texturePath="Assets/DarkFairytale/UI/Skills/RuntimeArt/Skill"+id.ToString("00")+".png";File.WriteAllBytes(texturePath,image.EncodeToPNG());
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            string backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+".asset";if(!File.Exists(backup))File.Copy(path,backup);
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);var border=sprite.border;var replacement=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(sprite.pivot.x/w,sprite.pivot.y/h),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,border);replacement.OverrideGeometry(sprite.vertices,sprite.triangles);replacement.name=sprite.name;EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
            if(previous!=null)catalog.entries.Remove(previous);
            catalog.entries.Add(new ProductionUiArt.Entry{sprite=path,source=source,texture=texturePath,rect=rect,border=border,originalMaterial=canonical,treatment=index>=0?"approved corner repair v2; source RGB unchanged elsewhere; offline alpha":"original skill illustration; offline alpha"});
            proof.AppendLine("Skill"+id+": "+w+"x"+h+", corner x >= "+left+", top-origin y >= "+top+", outside changes = "+outsideChanges);
            UnityEngine.Object.DestroyImmediate(replacement);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(raw);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);File.WriteAllText(Dir+"skills-pixel-proof.txt",proof.ToString());
        foreach(var path in report.entries.Where(e=>e.sprite.Contains("/Skills/Sprites/CleanSkill")&&e.material.Contains("/BattleRefresh/Sprites/Cooldown")).Select(e=>e.material).Distinct()){
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);Backup(path);mat.shader=Shader.Find("UI/Storybook Disabled");mat.SetFloat("_DisabledUI",1);EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        var paths=catalog.entries.Where(e=>e.sprite.Contains("/Skills/Sprites/CleanSkill")).Select(e=>e.sprite).ToArray();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);
            if(!report.entries.Any(e=>e.prefab==path&&paths.Contains(e.sprite)))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{if(ProductionUiArt.FinalizePrefab(root)){Backup(path);PrefabUtility.SaveAsPrefabAsset(root,path);}}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in report.entries.Where(e=>e.prefab.EndsWith(".unity")&&paths.Contains(e.sprite)).Select(e=>e.prefab).Distinct()){
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                bool changed=false;foreach(var root in scene.GetRootGameObjects())changed|=ProductionUiArt.FinalizePrefab(root);
                if(changed){Backup(path);if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Could not save scene bindings: "+path);}
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        UiProductionAudit.Run();
    }
    static void Backup(string path){string backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);if(!File.Exists(backup))File.Copy(path,backup);}
    static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(path));return t;}
}
