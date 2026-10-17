using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Reviewed text-free icons: prepare their alpha offline and remove the runtime paper-keying material.</summary>
[InitializeOnLoad]
public static class ProductionUiIconExtraction
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json",Runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt/";
    static ProductionUiIconExtraction(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"extract-icons.request"))return;
        File.Delete(Dir+"extract-icons.request");try{Run();File.WriteAllText(Dir+"icons-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"icons-result.txt",e.ToString());Debug.LogException(e);}
    };}
    static readonly Dictionary<string,string[]> Reviewed=new Dictionary<string,string[]> {
        {"Equipment",new[]{"GradeDiamond","LockGlyph","ForestInk","CategoryIconV41","CategoryIconV42","CategoryIconV43","CategoryIconV44","BackpackIcon","ChangeSymbolV4"}},
        {"Shelter/Reference",new[]{"Heart","Swords","Shield","Speed","Critical","CriticalDamage","FoldChevron","Equipment","NavDivider","Shop","Crystal","Settings","HeaderSeparator","PortraitDividerV2","SettingsDivider","ChestBronze","ChestGold","ChestBlack","ChestDivider","Fusion","ExperienceV2","RebirthV2","Lock","RuleCross"}},
        {"Map",new[]{"HeaderThornLeft","HeaderThornRight","HeaderStarLeft","HeaderStarRight","HeaderTreesLeft","HeaderTreesRight","LocateGlyph","DetailSwords"}},
        {"Fusion",new[]{"ForestLeft","ForestRight","BlueCrystalV2","StatIcon","ExperienceIconV2","ReadySealV2","OuterFrameClean","ThinPanelFrameClean","HeaderFrameClean","WalletFrameClean","MeterFrameClean","RewardCardFrameClean","BlankButton"}},
        {"Chest",new[]{"RewardDivider","PlayGlyph","BackBookmark","BloodMoonCrystal","PrimaryButton","CostCrystal","AdvertisementButton"}},
        {"Rebirth",new[]{"ReadyWaxSeal","BackBookmark","RewardIconV2_0","RewardIconV2_1","RewardIconV2_2","RewardIconV2_3","PrimaryButton"}},
        {"DailyReward",new[]{"Crystal","ClaimButton","Button"}}
    };
    [MenuItem("Tools/Resource Optimization/Extract Reviewed UI Icons")]
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save manual scene edits before migration");
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        var paths=Reviewed.SelectMany(pair=>pair.Value.Select(name=>"Assets/DarkFairytale/UI/"+pair.Key+"/Sprites/"+name+".asset")).ToArray();
        foreach(var path in paths){
            if(catalog.entries.Any(e=>e.sprite==path))continue;
            var uses=report.entries.Where(e=>e.sprite==path).ToArray();
            if(uses.Length==0||uses.Select(e=>e.material).Distinct().Count()!=1||uses.Any(e=>e.shader!="UI/Reference Ink Cutout"&&e.shader!="UI/Chest Authored Cutout"))throw new Exception("Unexpected icon usage "+path);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(uses[0].material);
            if(mat.GetFloat("_Mode")!=0||mat.GetFloat("_InkOnly")!=0||mat.GetColor("_Color")!=Color.white)throw new Exception("Unexpected icon processing "+path);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);var source=AssetDatabase.GetAssetPath(sprite.texture);var r=sprite.rect;
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(source));int w=(int)r.width,h=(int)r.height;
            var pixels=raw.GetPixels((int)r.x,(int)r.y,w,h);float cutoff=mat.GetFloat("_Cutoff"),feather=mat.GetFloat("_Feather");
            for(int i=0;i<pixels.Length;i++){
                var c=pixels[i];float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
                float t=Mathf.Clamp01((luminance-cutoff)/feather);c.a*=1-t*t*(3-2*t);pixels[i]=c;
            }
            bool unusedCenter=uses.All(e=>e.imageType=="Sliced"&&!e.fillCenter);
            if(unusedCenter){var b=sprite.border;for(int y=(int)b.y+1;y<h-(int)b.w-1;y++)for(int x=(int)b.x+1;x<w-(int)b.z-1;x++)pixels[y*w+x]=Color.clear;}
            var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.SetPixels(pixels);image.Apply();var bytes=image.EncodeToPNG();string hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20).ToLowerInvariant();
            string texturePath=ArtSourcePaths.Resolve(Runtime+hash+".png");if(!File.Exists(texturePath))File.WriteAllBytes(texturePath,bytes);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+".asset";if(!File.Exists(backup))File.Copy(path,backup);
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);var border=sprite.border;
            var replacement=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(sprite.pivot.x/w,sprite.pivot.y/h),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,border);
            replacement.OverrideGeometry(sprite.vertices,sprite.triangles);replacement.name=sprite.name;EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
            catalog.entries.Add(new ProductionUiArt.Entry{sprite=path,source=source,texture=texturePath,rect=r,border=border,treatment="reviewed text-free icon; offline alpha; UI/Default",originalMaterial=uses[0].material});
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
