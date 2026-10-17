using System.IO;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class ProductionUiSkillSources
{
    static ProductionUiSkillSources(){EditorApplication.update+=()=>{
        const string dir="output/ui-production-audit/skills-original/";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/ui-production-audit/skill-sources.request"))return;
        File.Delete("output/ui-production-audit/skill-sources.request");Directory.CreateDirectory(dir);
        foreach(int i in new[]{2,5,8,9,11,12,13,14,15}){
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Skills/Sprites/CleanSkill"+i+".asset");
            var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));var r=sprite.rect;
            var crop=new Texture2D((int)r.width,(int)r.height,TextureFormat.RGBA32,false);crop.SetPixels(raw.GetPixels((int)r.x,(int)r.y,(int)r.width,(int)r.height));crop.Apply();File.WriteAllBytes(dir+sprite.name+".png",crop.EncodeToPNG());Object.DestroyImmediate(raw);Object.DestroyImmediate(crop);
        }
    };}
}
