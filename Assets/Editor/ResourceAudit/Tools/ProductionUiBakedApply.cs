using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProductionUiBakedApply
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json",Runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt/";
    static ProductionUiBakedApply(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"apply-reviewed.request"))return;
        File.Delete(Dir+"apply-reviewed.request");try{Run();File.WriteAllText(Dir+"apply-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"apply-result.txt",e.ToString());Debug.LogException(e);}
    };}
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits");
        var batch=JsonUtility.FromJson<ProductionUiBake.Batch>(File.ReadAllText(Dir+"baked-review.json"));
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        Directory.CreateDirectory("Assets/DarkFairytale/UI/Shared/Sprites/Variants");
        foreach(var group in batch.items.GroupBy(i=>i.sprite)){
            if(catalog.entries.Any(e=>e.sprite==group.Key))continue;
            var original=AssetDatabase.LoadAssetAtPath<Sprite>(group.Key);var rect=original.rect;var border=original.border;var source=AssetDatabase.GetAssetPath(original.texture);
            var entry=new ProductionUiArt.Entry{sprite=group.Key,source=source,rect=rect,border=border,treatment="reviewed offline alpha preparation; original geometry and state variants retained; no runtime source masking"};
            var items=group.OrderBy(i=>string.IsNullOrEmpty(i.material)?0:1).ToArray();
            for(int index=0;index<items.Length;index++){
                var item=items[index];var bytes=File.ReadAllBytes(item.png);string hash;
                using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20).ToLowerInvariant();
                string texturePath=ArtSourcePaths.Resolve(Runtime+hash+".png");if(!File.Exists(texturePath))File.WriteAllBytes(texturePath,bytes);AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(texture.width!=(int)rect.width||texture.height!=(int)rect.height)throw new Exception("Unexpected bake dimensions "+item.png);
                var replacement=Sprite.Create(texture,new Rect(0,0,rect.width,rect.height),new Vector2(original.pivot.x/rect.width,original.pivot.y/rect.height),original.pixelsPerUnit,0,SpriteMeshType.FullRect,border);replacement.OverrideGeometry(original.vertices,original.triangles);replacement.name=original.name;
                if(index==0){Backup(group.Key);EditorUtility.CopySerialized(replacement,original);EditorUtility.SetDirty(original);entry.texture=texturePath;entry.originalMaterial=item.material;UnityEngine.Object.DestroyImmediate(replacement);}
                else if(texturePath==entry.texture){entry.variants.Add(new ProductionUiArt.Variant{material=item.material,sprite=group.Key});UnityEngine.Object.DestroyImmediate(replacement);}
                else{
                    string path="Assets/DarkFairytale/UI/Shared/Sprites/Variants/"+Path.GetFileNameWithoutExtension(group.Key)+"-"+AssetDatabase.AssetPathToGUID(item.material).Substring(0,8)+".asset";
                    var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing){EditorUtility.CopySerialized(replacement,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(replacement);}else AssetDatabase.CreateAsset(replacement,path);
                    entry.variants.Add(new ProductionUiArt.Variant{material=item.material,sprite=path});
                }
            }
            catalog.entries.Add(entry);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            try{if(ProductionUiArt.FinalizePrefab(root)){Backup(path);PrefabUtility.SaveAsPrefabAsset(root,path);}}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in report.entries.Where(e=>e.prefab.EndsWith(".unity")).Select(e=>e.prefab).Distinct()){
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{bool changed=false;foreach(var root in scene.GetRootGameObjects())changed|=ProductionUiArt.FinalizePrefab(root);if(changed){Backup(path);if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Could not save "+path);}}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();UiProductionAudit.Run();
    }
    static void Backup(string path){string p=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);if(!File.Exists(p))File.Copy(path,p);}
}
