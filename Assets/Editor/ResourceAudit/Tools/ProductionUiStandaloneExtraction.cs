using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit extraction of reviewed default-material art and unused frame centres.</summary>
[InitializeOnLoad]
public static class ProductionUiStandaloneExtraction
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json",Runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt/";
    static ProductionUiStandaloneExtraction(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"extract-standalone.request"))return;
        File.Delete(Dir+"extract-standalone.request");try{Run();File.WriteAllText(Dir+"standalone-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"standalone-result.txt",e.ToString());Debug.LogException(e);}
    };}
    static readonly Dictionary<string,string[]> Reviewed=new Dictionary<string,string[]> {
        {"BattleRefresh",new[]{"Panel","Portrait"}},
        {"Shelter/Reference",new[]{"AttributeBorder","HeaderBorder","OuterFrameV2","Portrait","ChestBorder","EmptyDashedBorder","EmptyChest","ReadyPlate","BattleRibbon","RibbonLeft","RibbonRight","RibbonTop","RibbonBottom","BattleWax","Scythe"}},
        {"Map",new[]{"NodeLabelFrameV2_0","NodeLabelFrameV2_1","NodeLabelFrameV2_2","NodeLabelFrameV2_3","NodeLabelFrameV2_4","HeaderFrame","BackBookmark","DetailFrame","PreviewFrame","EnterFrame"}},
        {"Chest",new[]{"PageFrameV2"}}, {"Shop",new[]{"CardFrame"}},
        {"Fusion",new[]{"RuinDormant","MeterBack","MeterFill"}},
        {"Rebirth",new[]{"ForestDoorway"}}

    };
    [MenuItem("Tools/Resource Optimization/Extract Reviewed Standalone UI Art")]
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save manual scene edits before migration");
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        var paths=Reviewed.SelectMany(pair=>pair.Value.Select(name=>"Assets/DarkFairytale/UI/"+pair.Key+"/Sprites/"+name+".asset")).ToArray();
        foreach(var path in paths){
            if(catalog.entries.Any(e=>e.sprite==path))continue;
            var uses=report.entries.Where(e=>e.sprite==path).ToArray();
            if(uses.Length==0||uses.Any(e=>e.shader!="UI/Default"))throw new Exception("Unexpected standalone usage "+path);
            bool frame=uses.All(e=>e.imageType=="Sliced"&&!e.fillCenter);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);var source=AssetDatabase.GetAssetPath(sprite.texture);var r=sprite.rect;
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(source));int w=(int)r.width,h=(int)r.height;
            var pixels=raw.GetPixels((int)r.x,(int)r.y,w,h);
            // The entire centre is unused in every persisted Image. Keep a texel guard for bilinear edge sampling.
            if(path.EndsWith("/HeaderBorder.asset")){
                // Use the same approved border strips as the former overlay nodes, now part of the asset.
                foreach(var patch in new[]{new[]{245,22,74,22,24,0},new[]{250,158,80,26,22,136}})
                    for(int py=0;py<patch[3];py++)for(int px=0;px<patch[2];px++){
                        int dx=patch[4]+px,dy=h-1-patch[5]-py;
                        if(dx>=0&&dx<w&&dy>=0&&dy<h)pixels[dy*w+dx]=raw.GetPixel(patch[0]+px,raw.height-1-patch[1]-py);
                    }
            }
            var b=sprite.border;
            if(frame)for(int y=(int)b.y+1;y<h-(int)b.w-1;y++)for(int x=(int)b.x+1;x<w-(int)b.z-1;x++)pixels[y*w+x]=Color.clear;
            var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.SetPixels(pixels);image.Apply();var bytes=image.EncodeToPNG();string hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20).ToLowerInvariant();
            string texturePath=ArtSourcePaths.Resolve(Runtime+hash+".png");if(!File.Exists(texturePath))File.WriteAllBytes(texturePath,bytes);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+".asset";if(!File.Exists(backup))File.Copy(path,backup);
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);var border=sprite.border;
            var replacement=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(sprite.pivot.x/w,sprite.pivot.y/h),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,border);
            replacement.OverrideGeometry(sprite.vertices,sprite.triangles);replacement.name=sprite.name;EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
            catalog.entries.Add(new ProductionUiArt.Entry{sprite=path,source=source,texture=texturePath,rect=r,border=border,treatment=frame?"original border pixels; unused text-bearing center removed":"reviewed independent source crop"});
            UnityEngine.Object.DestroyImmediate(replacement);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(raw);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);
        // Only persisted bindings change. Runtime UI creation is not involved.
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
        AssetDatabase.SaveAssets();UiProductionAudit.Run();
    }
    static void Backup(string path){string backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);if(!File.Exists(backup))File.Copy(path,backup);}
}
