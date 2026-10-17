using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pack unique ornamental edges without retaining a huge empty panel centre or repeating unique art.</summary>
[InitializeOnLoad]
public static class ProductionUiSparseFrames
{
    const string Dir="output/ui-production-audit/",CatalogPath="Assets/Editor/ResourceAudit/Data/ProductionUiArtCatalog.json",Runtime="Assets/DarkFairytale/UI/Shared/RuntimeArt/";
    static ProductionUiSparseFrames(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"sparse-frames.request"))return;
        File.Delete(Dir+"sparse-frames.request");try{Run();File.WriteAllText(Dir+"sparse-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"sparse-result.txt",e.ToString());Debug.LogException(e);}
    };}
    struct Piece {public string name;public RectInt source;public Piece(string n,int x,int y,int w,int h){name=n;source=new RectInt(x,y,w,h);}}
    public static void Run(){
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene edits");
        var report=JsonUtility.FromJson<UiProductionAudit.Report>(File.ReadAllText(Dir+"ui-nodes.json"));
        var catalog=JsonUtility.FromJson<ProductionUiArt.Catalog>(File.ReadAllText(CatalogPath));
        foreach(var entry in catalog.entries.ToArray())foreach(var variant in entry.variants??new List<ProductionUiArt.Variant>())if(!catalog.entries.Any(e=>e.sprite==variant.sprite)){
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(variant.sprite);
            catalog.entries.Add(new ProductionUiArt.Entry{sprite=variant.sprite,source=entry.source,rect=entry.rect,border=sprite.border,texture=AssetDatabase.GetAssetPath(sprite.texture),treatment="prepared material-state variant"});
        }
        Directory.CreateDirectory("Assets/DarkFairytale/UI/Shared/Sprites/FrameParts");var proof=new System.Text.StringBuilder(File.Exists(Dir+"sparse-proof.txt")?File.ReadAllText(Dir+"sparse-proof.txt"):"");
        foreach(var entry in catalog.entries){
            if(entry.frameParts!=null&&entry.frameParts.Count>0)continue;
            var uses=report.entries.Where(e=>e.sprite==entry.sprite).ToArray();
            if(entry.rect.width*entry.rect.height<=120000||uses.Length==0||uses.Any(e=>e.imageType!="Sliced"||e.shader!="UI/Default"||(e.fillCenter&&(e.color.r!=0||e.color.g!=0||e.color.b!=0))))continue;
            // These reviewed frame Images are decorative; never disable a raycast/transition target.
            foreach(var use in uses.Where(e=>e.prefab.EndsWith(".prefab"))){
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(use.prefab);
                foreach(var image in prefab.GetComponentsInChildren<Image>(true).Where(i=>AssetDatabase.GetAssetPath(i.sprite)==entry.sprite))
                    if(image.GetComponent<Mask>()||image.raycastTarget||prefab.GetComponentsInChildren<Selectable>(true).Any(s=>s.targetGraphic==image))throw new Exception("Interactive frame requires separate review: "+entry.sprite);
            }
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(entry.texture));int w=raw.width,h=raw.height;
            int l=(int)entry.border.x,r=(int)entry.border.z,b=(int)entry.border.y,t=(int)entry.border.w;
            if(uses.Any(e=>e.fillCenter))for(int y=b;y<h-t;y++)for(int x=l;x<w-r;x++)if(raw.GetPixel(x,y).a<1)throw new Exception("Shadow centre is not opaque: "+entry.sprite);
            if(Mathf.Min(l,r,b,t)<1){UnityEngine.Object.DestroyImmediate(raw);continue;}
            var corners=new[]{new Piece("BottomLeft",0,0,l,b),new Piece("BottomRight",w-r,0,r,b),new Piece("TopLeft",0,h-t,l,t),new Piece("TopRight",w-r,h-t,r,t)};
            var horizontal=new[]{new Piece("Bottom",l,0,w-l-r,b),new Piece("Top",l,h-t,w-l-r,t)};
            var vertical=new[]{new Piece("Left",0,b,l,h-b-t),new Piece("Right",w-r,b,r,h-b-t)};
            entry.frameParts=new List<ProductionUiArt.FramePart>();long packed=0;
            foreach(var group in new[]{corners,horizontal,vertical}){
                bool sideways=group==vertical;int atlasW=sideways?group.Sum(p=>p.source.width+4):group.Max(p=>p.source.width+4),atlasH=sideways?group.Max(p=>p.source.height+4):group.Sum(p=>p.source.height+4);
                var atlas=new Texture2D(atlasW,atlasH,TextureFormat.RGBA32,false);atlas.SetPixels(new Color[atlasW*atlasH]);int cursor=0;var rects=new List<Rect>();
                foreach(var part in group){int ox=sideways?cursor+2:2,oy=sideways?2:cursor+2;var s=part.source;
                    for(int y=-2;y<s.height+2;y++)for(int x=-2;x<s.width+2;x++)atlas.SetPixel(ox+x,oy+y,raw.GetPixel(Mathf.Clamp(s.x+x,0,w-1),Mathf.Clamp(s.y+y,0,h-1)));
                    rects.Add(new Rect(ox,oy,s.width,s.height));cursor+=sideways?s.width+4:s.height+4;
                }
                atlas.Apply();var bytes=atlas.EncodeToPNG();string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,20).ToLowerInvariant();string path=Runtime+hash+".png";if(!File.Exists(path))File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                for(int i=0;i<group.Length;i++){
                    string spritePath="Assets/DarkFairytale/UI/Shared/Sprites/FrameParts/"+AssetDatabase.AssetPathToGUID(entry.sprite)+"-"+group[i].name+".asset";
                    var part=Sprite.Create(texture,rects[i],Vector2.one*.5f,100,0,SpriteMeshType.FullRect);part.name=group[i].name;
                    var old=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);if(old){EditorUtility.CopySerialized(part,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(part);}else AssetDatabase.CreateAsset(part,spritePath);
                    entry.frameParts.Add(new ProductionUiArt.FramePart{name=group[i].name,sprite=spritePath});
                }
                packed+=(long)atlasW*atlasH;UnityEngine.Object.DestroyImmediate(atlas);
            }
            proof.AppendLine(entry.sprite+": "+w+"x"+h+" ("+((long)w*h)+" texels) -> "+packed+" texels in three tightly packed edge/corner textures; all retained texels copied exactly, 2px sampling guards.");
            entry.treatment+="; sparse ornamental frame: separate corners/edges; empty centre excluded from runtime";UnityEngine.Object.DestroyImmediate(raw);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(CatalogPath,JsonUtility.ToJson(catalog,true));AssetDatabase.ImportAsset(CatalogPath);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);try{if(ProductionUiArt.FinalizePrefab(root)){Backup(path);PrefabUtility.SaveAsPrefabAsset(root,path);}}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in report.entries.Where(e=>e.prefab.EndsWith(".unity")).Select(e=>e.prefab).Distinct()){
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{bool changed=false;foreach(var root in scene.GetRootGameObjects())changed|=ProductionUiArt.FinalizePrefab(root);if(changed){Backup(path);if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Could not save "+path);}}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Dir+"sparse-proof.txt",proof.ToString());UiProductionAudit.Run();
    }
    public static void Apply(Image image,ProductionUiArt.Entry entry){
        if(image.type!=Image.Type.Sliced||(image.fillCenter&&(image.color.r!=0||image.color.g!=0||image.color.b!=0))||image.raycastTarget||image.GetComponent<Mask>())throw new Exception("Sparse frame contract changed: "+image.name);
        float unit=1/image.pixelsPerUnit/image.pixelsPerUnitMultiplier;var b=entry.border*unit;
        if(image.fillCenter){
            var centre=new GameObject("ShadowCenter",typeof(RectTransform),typeof(Image));centre.layer=image.gameObject.layer;centre.transform.SetParent(image.transform,false);centre.transform.SetAsFirstSibling();
            var rt=(RectTransform)centre.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(b.x,b.y);rt.offsetMax=new Vector2(-b.z,-b.w);
            var graphic=centre.GetComponent<Image>();graphic.color=image.color;graphic.raycastTarget=false;
        }
        foreach(var part in entry.frameParts){
            var g=new GameObject("Border_"+part.name,typeof(RectTransform),typeof(Image));g.layer=image.gameObject.layer;g.transform.SetParent(image.transform,false);g.transform.SetAsFirstSibling();
            var rt=(RectTransform)g.transform;rt.pivot=Vector2.one*.5f;
            switch(part.name){
                case "BottomLeft":rt.anchorMin=rt.anchorMax=Vector2.zero;rt.offsetMin=Vector2.zero;rt.offsetMax=new Vector2(b.x,b.y);break;
                case "BottomRight":rt.anchorMin=rt.anchorMax=new Vector2(1,0);rt.offsetMin=new Vector2(-b.z,0);rt.offsetMax=new Vector2(0,b.y);break;
                case "TopLeft":rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.offsetMin=new Vector2(0,-b.w);rt.offsetMax=new Vector2(b.x,0);break;
                case "TopRight":rt.anchorMin=rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(-b.z,-b.w);rt.offsetMax=Vector2.zero;break;
                case "Bottom":rt.anchorMin=Vector2.zero;rt.anchorMax=new Vector2(1,0);rt.offsetMin=new Vector2(b.x,0);rt.offsetMax=new Vector2(-b.z,b.y);break;
                case "Top":rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(b.x,-b.w);rt.offsetMax=new Vector2(-b.z,0);break;
                case "Left":rt.anchorMin=Vector2.zero;rt.anchorMax=new Vector2(0,1);rt.offsetMin=new Vector2(0,b.y);rt.offsetMax=new Vector2(b.x,-b.w);break;
                case "Right":rt.anchorMin=new Vector2(1,0);rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(-b.z,b.y);rt.offsetMax=new Vector2(0,-b.w);break;
            }
            var child=g.GetComponent<Image>();child.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(part.sprite);child.color=image.color;child.raycastTarget=false;
        }
        image.sprite=null;image.enabled=false;EditorUtility.SetDirty(image);
    }
    static void Backup(string path){string p=Dir+"backup/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);if(!File.Exists(p))File.Copy(path,p);}
}
