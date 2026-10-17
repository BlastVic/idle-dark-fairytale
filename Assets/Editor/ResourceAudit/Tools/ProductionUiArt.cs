using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, lossless extraction of reviewed artwork. Never runs on import or Play.</summary>
[InitializeOnLoad]
public static class ProductionUiArt
{
    const string Output="output/ui-production-audit/";
    const string CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json";
    const string Runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt";
    [Serializable] public class FramePart {public string name,sprite;}
    [Serializable] public class Variant {public string material,sprite;}
    [Serializable] public class Entry {
        public string sprite,source,texture;public Rect rect;public Vector4 border;
        public string treatment, originalMaterial;public bool tiledBorder;public string[] paperMaterials;public List<Variant> variants=new List<Variant>();public List<FramePart> frameParts=new List<FramePart>();
    }
    [Serializable] public class Catalog {public List<Entry> entries=new List<Entry>();}
    static Catalog Read()=>File.Exists(CatalogPath)?JsonUtility.FromJson<Catalog>(File.ReadAllText(CatalogPath)):new Catalog();
    static ProductionUiArt(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Output+"extract-equipment.request"))return;
        File.Delete(Output+"extract-equipment.request");try{ExtractEquipment();File.WriteAllText(Output+"extraction-result.txt","PASS");}catch(Exception e){File.WriteAllText(Output+"extraction-result.txt",e.ToString());Debug.LogException(e);}
    };}
    // Builders must use this entry point so deleting/re-authoring a Sprite cannot bring the design sheet back.
    public static Sprite Create(string assetPath,Texture2D original,Rect rect,Vector2 pivot,float ppu,uint extrude,SpriteMeshType mesh,Vector4 border)
    {
        var entry=Read().entries.FirstOrDefault(e=>e.sprite==assetPath);
        if(entry==null)return Sprite.Create(original,rect,pivot,ppu,extrude,mesh,border);
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(entry.texture);
        if(!tex)throw new InvalidOperationException("Missing production art: "+entry.texture);
        return Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),pivot,ppu,extrude,mesh,entry.border);
    }
    public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu=100,uint extrude=0,SpriteMeshType mesh=SpriteMeshType.Tight,Vector4 border=default)
    {
        var source=AssetDatabase.GetAssetPath(texture);
        var entry=Read().entries.FirstOrDefault(e=>e.source==source&&e.rect==rect&&e.border==border);
        if(entry==null)return Sprite.Create(texture,rect,pivot,ppu,extrude,mesh,border);
        return Create(entry.sprite,texture,rect,pivot,ppu,extrude,mesh,border);
    }
    public static bool FinalizePrefab(GameObject root)
    {
        var catalog=Read().entries.ToDictionary(e=>e.sprite);
        bool changed=false;
        foreach(var entry in catalog.Values.Where(e=>e.paperMaterials!=null))foreach(var path in entry.paperMaterials){
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat)continue;
            var rect=new Vector4(0,0,1,1);
            if(mat.shader.name!="UI/Production Paper"||mat.GetVector("_SourceRect")!=rect){mat.shader=Shader.Find("UI/Production Paper");mat.SetVector("_SourceRect",rect);EditorUtility.SetDirty(mat);}
        }
        foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)){
            if(!image.sprite||!catalog.TryGetValue(AssetDatabase.GetAssetPath(image.sprite),out var entry))continue;
            if(entry.tiledBorder&&image.type!=UnityEngine.UI.Image.Type.Tiled){image.type=UnityEngine.UI.Image.Type.Tiled;EditorUtility.SetDirty(image);changed=true;}
            var variant=entry.variants?.FirstOrDefault(v=>v.material==AssetDatabase.GetAssetPath(image.material));
            if(variant!=null){image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(variant.sprite);image.material=null;EditorUtility.SetDirty(image);changed=true;}
            if(!string.IsNullOrEmpty(entry.originalMaterial)&&AssetDatabase.GetAssetPath(image.material)==entry.originalMaterial){image.material=null;EditorUtility.SetDirty(image);changed=true;}
        }
        foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)){
            if(image.sprite&&catalog.TryGetValue(AssetDatabase.GetAssetPath(image.sprite),out var entry)&&entry.frameParts!=null&&entry.frameParts.Count>0){ProductionUiSparseFrames.Apply(image,entry);changed=true;}
        }
        if(catalog.ContainsKey("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/HeaderBorder.asset"))
            foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if(image.sprite&&(AssetDatabase.GetAssetPath(image.sprite)=="Assets/DarkFairytale/UI/Shelter/Reference/Sprites/HeaderRepair.asset"||AssetDatabase.GetAssetPath(image.sprite)=="Assets/DarkFairytale/UI/Shelter/Reference/Sprites/HeaderLowerRepair.asset")){
                    UnityEngine.Object.DestroyImmediate(image.gameObject);changed=true;
                }
        foreach(var fusion in root.GetComponentsInChildren<StorybookFusionUI>(true)){
            if(fusion.referenceInk&&catalog.ContainsKey(AssetDatabase.GetAssetPath(fusion.statIcon))&&catalog.ContainsKey(AssetDatabase.GetAssetPath(fusion.experienceIcon))){fusion.referenceInk=null;EditorUtility.SetDirty(fusion);changed=true;}
        }
        foreach(var nav in root.GetComponentsInChildren<StorybookNavigation>(true)){
            if(IsPrepared(nav.shelterIcon?nav.shelterIcon.sprite:null,nav.shelterOnPaper,catalog)&&IsPrepared(nav.shelterIcon.sprite,nav.shelterOnRed,catalog)){
                nav.shelterOnPaper=nav.shelterOnRed=null;EditorUtility.SetDirty(nav);changed=true;
            }
        }
        foreach(var chest in root.GetComponentsInChildren<StorybookChestUI>(true)){
            if(IsPrepared(chest.waitingChest,chest.waitingChestMaterial,catalog)){chest.waitingChestMaterial=null;EditorUtility.SetDirty(chest);changed=true;}
            if(IsPrepared(chest.readyChest,chest.readyChestMaterial,catalog)){chest.readyChestMaterial=null;EditorUtility.SetDirty(chest);changed=true;}
        }
        foreach(var opening in root.GetComponentsInChildren<StorybookChestOpening>(true)){
            if(IsPrepared(opening.crystal,opening.crystalMaterial,catalog)){opening.crystalMaterial=null;EditorUtility.SetDirty(opening);changed=true;}
        }
        foreach(var map in root.GetComponentsInChildren<StorybookMapUI>(true)){
            if(IsPrepared(map.completedSeal,map.completedMaterial,catalog)){map.completedMaterial=null;EditorUtility.SetDirty(map);changed=true;}
            if(IsPrepared(map.availableSeal,map.availableMaterial,catalog)){map.availableMaterial=null;EditorUtility.SetDirty(map);changed=true;}
            if(IsPrepared(map.lockedSeal,map.lockedMaterial,catalog)){map.lockedMaterial=null;EditorUtility.SetDirty(map);changed=true;}
        }
        foreach(var node in root.GetComponentsInChildren<StorybookMapNode>(true))if(IsPrepared(node.customSeal,node.customMaterial,catalog)){node.customMaterial=null;EditorUtility.SetDirty(node);changed=true;}
        return changed;
    }
    static bool IsPrepared(Sprite sprite,Material material,Dictionary<string,Entry> catalog)=>sprite&&material&&catalog.TryGetValue(AssetDatabase.GetAssetPath(sprite),out var e)&&e.originalMaterial==AssetDatabase.GetAssetPath(material);
    public static GameObject SaveAsPrefabAsset(GameObject root,string path){FinalizePrefab(root);return PrefabUtility.SaveAsPrefabAsset(root,path);}
    public static GameObject SaveAsPrefabAsset(GameObject root,string path,out bool success){FinalizePrefab(root);return PrefabUtility.SaveAsPrefabAsset(root,path,out success);}
    [MenuItem("Tools/Resource Optimization/Extract Reviewed Equipment Frames")]
    public static void ExtractEquipment()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before editing art.");
        Directory.CreateDirectory(Runtime);Directory.CreateDirectory(Output+"backup");
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Output+"ui-nodes.json"));
        var catalog=Read();
        string[] frames={"PanelFrameV3","DetailsFrameV2","CellFrame","SelectedCellFrame","CategoryBookmarkEdge","RedButtonFrame","AttributesFrameV2","StatCellFrame","PlusBorder","ToolbarButtonFrame"};
        string[] plain={"CleanPaperV3","Portrait","BackBookmark","NewDot"};
        var paper=ReadImage("Assets/ArtSources/UI/Equipment/Art/ApprovedEquipment.png");
        try {
            foreach(var name in frames.Concat(plain)){
                string path="Assets/DarkFairytale/UI/Equipment/Sprites/"+name+".asset";
                if(catalog.entries.Any(e=>e.sprite==path))continue;
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new Exception("Missing "+path);
                var uses=report.entries.Where(e=>e.sprite==path).ToArray();
                if(uses.Length==0||uses.Any(e=>e.shader!="UI/Default"))throw new Exception("Unexpected material contract: "+path);
                bool frame=frames.Contains(name);
                if(frame&&uses.Any(e=>e.imageType!="Sliced"))throw new Exception("Non-sliced use: "+path);
                var source=AssetDatabase.GetAssetPath(sprite.texture);var raw=ReadImage(source);var r=sprite.rect;
                var b=sprite.border;int w=(int)r.width,h=(int)r.height;
                var pixels=raw.GetPixels((int)r.x,(int)r.y,w,h);
                // Preserve every sampled border texel, including a one-pixel guard inside each edge.
                // The interior is made from the EXISTING clean paper tile, never from text-bearing pixels.
                bool fill=uses.Any(e=>e.fillCenter);
                if(frame)for(int y=(int)b.y+1;y<h-(int)b.w-1;y++)for(int x=(int)b.x+1;x<w-(int)b.z-1;x++)
                    pixels[y*w+x]=fill?paper.GetPixel(370+x%40,paper.height-345-35+y%35):Color.clear;
                var image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.SetPixels(pixels);image.Apply();
                var bytes=image.EncodeToPNG();string digest;
                using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20).ToLowerInvariant();
                string texturePath=ArtSourcePaths.Resolve(Runtime+"/"+digest+".png");if(!File.Exists(texturePath))File.WriteAllBytes(texturePath,bytes);
                AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                string backup=Output+"backup/"+name+".asset";if(!File.Exists(backup))File.Copy(path,backup);
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                var replacement=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(sprite.pivot.x/w,sprite.pivot.y/h),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,b);replacement.name=sprite.name;
                EditorUtility.CopySerialized(replacement,sprite);EditorUtility.SetDirty(sprite);
                catalog.entries.Add(new Entry{sprite=path,source=source,texture=texturePath,rect=r,border=b,treatment=frame?(fill?"original borders + existing clean paper interior":"original borders; text-bearing unused center removed"):"lossless standalone crop"});
                UnityEngine.Object.DestroyImmediate(replacement);UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(image);
            }
            AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);
            UiProductionAudit.Run();
        }finally{UnityEngine.Object.DestroyImmediate(paper);}
    }
    static Texture2D ReadImage(string path){var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!tex.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);return tex;}
}
