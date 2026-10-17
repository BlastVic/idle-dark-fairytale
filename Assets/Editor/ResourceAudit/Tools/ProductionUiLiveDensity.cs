using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProductionUiLiveDensity
{
    const string Path="output/ui-production-audit/ui-density-live.json";
    [Serializable] public class Row {public string sprite,texture,kind,owner;public float maxPixelsPerTexel,maxAt1440Width,screenWidth,screenHeight;public int sourceWidth,sourceHeight,samples;}
    [Serializable] public class Report {public string scope="Measured active UGUI during actual verification flows. 1440-width values extrapolate the observed layout only. Unvisited states and world-space art are not automatically approved for resizing.";public List<Row> entries=new List<Row>();}
    static readonly Dictionary<string,Row> Rows=new Dictionary<string,Row>();static double next;
    static ProductionUiLiveDensity(){
        if(File.Exists(Path))foreach(var row in JsonUtility.FromJson<Report>(File.ReadAllText(Path)).entries)Rows[row.sprite+"|"+row.kind]=row;
        EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&Rows.Count>0)Save();};
    }
    static void Save(){File.WriteAllText(Path,JsonUtility.ToJson(new Report{entries=Rows.Values.ToList()},true));}
    static void Tick(){
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next||string.IsNullOrEmpty(SessionState.GetString("UI_PRODUCTION_FLOW","")))return;next=EditorApplication.timeSinceStartup+.5;
        foreach(var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){
            if(!image.enabled||!image.sprite||image.color.a<.05f||!image.canvas)continue;
            var sprite=image.sprite;var key=AssetDatabase.GetAssetPath(sprite);if(string.IsNullOrEmpty(key))continue;
            var corners=new Vector3[4];image.rectTransform.GetWorldCorners(corners);var camera=image.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:image.canvas.worldCamera;
            var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();float w=Vector2.Distance(points[0],points[3]),h=Vector2.Distance(points[0],points[1]);
            float ratio;
            if(image.type==Image.Type.Sliced||image.type==Image.Type.Tiled)ratio=Mathf.Max(w/Mathf.Max(1,image.rectTransform.rect.width),h/Mathf.Max(1,image.rectTransform.rect.height))/image.pixelsPerUnit/image.pixelsPerUnitMultiplier;
            else ratio=image.preserveAspect?Mathf.Min(w/sprite.rect.width,h/sprite.rect.height):Mathf.Max(w/sprite.rect.width,h/sprite.rect.height);
            string id=key+"|"+image.type;
            if(!Rows.TryGetValue(id,out var row)||row.texture!=AssetDatabase.GetAssetPath(sprite.texture)||row.sourceWidth!=(int)sprite.rect.width||row.sourceHeight!=(int)sprite.rect.height){row=new Row{sprite=key,texture=AssetDatabase.GetAssetPath(sprite.texture),kind=image.type.ToString(),sourceWidth=(int)sprite.rect.width,sourceHeight=(int)sprite.rect.height};Rows[id]=row;}
            row.samples++;row.maxPixelsPerTexel=Mathf.Max(row.maxPixelsPerTexel,ratio);
            float normalized=ratio*1440/Screen.width;if(normalized>row.maxAt1440Width){row.maxAt1440Width=normalized;row.screenWidth=Screen.width;row.screenHeight=Screen.height;row.owner=image.transform.root.name+"/"+image.name;}
        }
        Save();
    }
}
