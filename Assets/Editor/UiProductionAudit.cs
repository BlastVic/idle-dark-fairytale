using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;

[InitializeOnLoad]
public static class UiProductionAudit
{
    const string Dir="output/ui-production-audit/";
    [Serializable] public class Entry {
        public string prefab,node,component,sprite,texture,material,shader,imageType;
        public Rect spriteRect,displayRect;public Vector4 border;public Color color;
        public bool fillCenter,active;public int textureWidth,textureHeight;public float pixelsPerUnit;
        public string materialJson;
    }
    [Serializable] public class Reference {public string owner,node,component,property,target,texture;}
    [Serializable] public class TextureEntry {public string path,format,compression;public int width,height,maxSize;public long runtimeBytes,sourceBytes;public bool readable,mipmaps;}
    [Serializable] public class Report {public List<Entry> entries=new List<Entry>();public List<Reference> references=new List<Reference>();public List<TextureEntry> textures=new List<TextureEntry>();}
    static UiProductionAudit(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;if(!File.Exists(Dir+"audit.request"))return;File.Delete(Dir+"audit.request");try{Run();}catch(Exception e){File.WriteAllText(Dir+"error.txt",e.ToString());Debug.LogException(e);}};}
    static string Node(Transform t){return t.parent?Node(t.parent)+"/"+t.name:t.name;}
    [MenuItem("Tools/Resource Optimization/Audit UI Production Assets")]
    public static void Run(){
        Directory.CreateDirectory(Dir);var report=new Report();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Collect(path,prefab,report);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var scene=EditorSceneManager.OpenPreviewScene(path);
            try{foreach(var root in scene.GetRootGameObjects())Collect(path,root,report);}finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)continue;
            report.textures.Add(new TextureEntry{path=path,width=texture.width,height=texture.height,format=texture.format.ToString(),compression=importer.textureCompression.ToString(),maxSize=importer.maxTextureSize,readable=importer.isReadable,mipmaps=importer.mipmapEnabled,runtimeBytes=Profiler.GetRuntimeMemorySizeLong(texture),sourceBytes=new FileInfo(path).Length});
        }
        File.WriteAllText(Dir+"ui-nodes.json",JsonUtility.ToJson(report,true));
        Debug.Log("UI production audit: "+report.entries.Count+" graphics; "+report.references.Count+" serialized art references");
    }
    static void Collect(string path,GameObject prefab,Report report){
            foreach(var graphic in prefab.GetComponentsInChildren<Graphic>(true)){
                if(graphic is Text||graphic is TMPro.TMP_Text)continue;
                var image=graphic as Image;var raw=graphic as RawImage;
                var sprite=image?image.sprite:null;var texture=sprite?sprite.texture:raw?raw.texture:null;
                var mat=graphic.material;
                report.entries.Add(new Entry {prefab=path,node=Node(graphic.transform),component=graphic.GetType().Name,sprite=AssetDatabase.GetAssetPath(sprite),texture=AssetDatabase.GetAssetPath(texture),material=AssetDatabase.GetAssetPath(mat),shader=mat?mat.shader.name:"",imageType=image?image.type.ToString():"RawImage",spriteRect=sprite?sprite.rect:Rect.zero,displayRect=graphic.rectTransform.rect,border=sprite?sprite.border:Vector4.zero,color=graphic.color,fillCenter=!image||image.fillCenter,active=graphic.gameObject.activeInHierarchy,textureWidth=texture?texture.width:0,textureHeight=texture?texture.height:0,pixelsPerUnit=sprite?sprite.pixelsPerUnit:100,materialJson=mat?EditorJsonUtility.ToJson(mat):""});
            }
            foreach(var component in prefab.GetComponentsInChildren<Component>(true)){
                if(!component)continue;
                var so=new SerializedObject(component);var it=so.GetIterator();
                while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference){
                    var value=it.objectReferenceValue;if(!(value is Sprite)&&!(value is Material)&&!(value is Texture))continue;
                    var target=AssetDatabase.GetAssetPath(value);if(string.IsNullOrEmpty(target))continue;
                    report.references.Add(new Reference{owner=path,node=Node(component.transform),component=component.GetType().Name,property=it.propertyPath,target=target,texture=value is Sprite referencedSprite?AssetDatabase.GetAssetPath(referencedSprite.texture):value is Texture?target:""});
                }
            }
    }
}
