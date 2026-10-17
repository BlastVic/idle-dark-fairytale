using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProductionUiTiledBorders
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json";
    static ProductionUiTiledBorders(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"tile-borders.request"))return;
        File.Delete(Dir+"tile-borders.request");try{Run();File.WriteAllText(Dir+"tile-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"tile-result.txt",e.ToString());Debug.LogException(e);}
    };}
    [MenuItem("Tools/Resource Optimization/Compact Reviewed Plain Borders")]
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits");
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var names=new[]{"ToolbarButtonFrame","StatCellFrame","PlusBorder"};
        var proof=new System.Text.StringBuilder("Corners preserved exactly; straight middle edges use an 8-pixel repeating sample.\n");
        foreach(var name in names){
            var entry=catalog.entries.Single(e=>e.sprite=="Assets/DarkFairytale/UI/Equipment/Sprites/"+name+".asset");if(entry.tiledBorder)continue;
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(entry.sprite);var b=sprite.border;
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(entry.texture));int oldW=raw.width,oldH=raw.height;
            int left=(int)b.x,right=(int)b.z,bottom=(int)b.y,top=(int)b.w,w=left+right+8,h=bottom+top+8;
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                int sx=x<left?x:x>=w-right?oldW-w+x:(oldW-left-right-8)/2+x;
                int sy=y<bottom?y:y>=h-top?oldH-h+y:(oldH-bottom-top-8)/2+y;
                tex.SetPixel(x,y,raw.GetPixel(sx,sy));
            }
            tex.Apply();string path="Assets/DarkFairytale/UI/Shared/RuntimeArt/"+name+"-Tiled.png";File.WriteAllBytes(path,tex.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.alphaIsTransparency=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=64;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var replacement=Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(path),new Rect(0,0,w,h),Vector2.one*.5f,sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,b);replacement.name=sprite.name;EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
            entry.texture=path;entry.tiledBorder=true;entry.treatment="compact nine-slice with tiled 8px straight edges; original corners; clean center";
            proof.AppendLine(name+": "+oldW+"x"+oldH+" -> "+w+"x"+h);
            UnityEngine.Object.DestroyImmediate(replacement);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(raw);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);
        var paths=names.Select(n=>"Assets/DarkFairytale/UI/Equipment/Sprites/"+n+".asset").ToArray();
        foreach(var owner in report.entries.Where(e=>paths.Contains(e.sprite)).Select(e=>e.prefab).Distinct()){
            if(owner.EndsWith(".prefab")){
                var root=PrefabUtility.LoadPrefabContents(owner);try{if(ProductionUiArt.FinalizePrefab(root)){Backup(owner);PrefabUtility.SaveAsPrefabAsset(root,owner);}}finally{PrefabUtility.UnloadPrefabContents(root);}
            }else{
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(owner);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(owner,OpenSceneMode.Additive);
                try{bool changed=false;foreach(var root in scene.GetRootGameObjects())changed|=ProductionUiArt.FinalizePrefab(root);if(changed){Backup(owner);if(!EditorSceneManager.SaveScene(scene,owner))throw new Exception("Failed to save "+owner);}}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
        File.WriteAllText(Dir+"tiled-border-proof.txt",proof.ToString());UiProductionAudit.Run();
    }
    static void Backup(string path){string backup=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);if(!File.Exists(backup))File.Copy(path,backup);}
}
