using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

[InitializeOnLoad]
public static class LegacyFontMigration
{
    const string Dir="output/font-migration/";
    const string Root="Assets/DarkFairytale/UI/CursedStorybook/Fonts/";
    static LegacyFontMigration(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"apply.request"))return;
        File.Delete(Dir+"apply.request");try{Apply();}catch(Exception e){File.WriteAllText(Dir+"result.txt",e.ToString());Debug.LogException(e);}
    };}
    static void Apply(){
        var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"NotoSerifCJKsc-Regular.otf");
        string path=Root+"NotoSerifCJKsc-Regular SDF.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(!font){
            font=TMP_FontAsset.CreateFontAsset(source,64,6,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="NotoSerifCJKsc-Regular SDF";AssetDatabase.CreateAsset(font,path);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
        }
        if(!font.TryAddCharacters("中文技能属性经验生命攻击防御胜利失败0123456789+-/%Lv.",out string missing))throw new Exception("Missing characters: "+missing);
        var settings=AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        var so=new SerializedObject(settings);so.FindProperty("m_defaultFontAsset").objectReferenceValue=font;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        string[] roots={"Assets/Fonts","Assets/TextMesh Pro/Fonts","Assets/TextMesh Pro/Resources/Fonts & Materials"};
        var old=roots.SelectMany(r=>Directory.GetFiles(r,"*",SearchOption.AllDirectories)).Where(p=>!p.EndsWith(".meta")).ToArray();
        var guids=old.Select(AssetDatabase.AssetPathToGUID).Where(g=>!string.IsNullOrEmpty(g)).ToArray();
        foreach(var p in Directory.GetFiles("Assets","*",SearchOption.AllDirectories).Where(p=>new[]{".unity",".prefab",".asset",".mat",".controller"}.Contains(Path.GetExtension(p))&&!roots.Any(r=>p.StartsWith(r+"/")))){
            var text=File.ReadAllText(p);if(guids.Any(g=>text.Contains(g)))throw new Exception("Remaining old font dependency: "+p);
        }
        File.WriteAllLines(Dir+"deleted.txt",old);
        foreach(var root in roots)if(!AssetDatabase.DeleteAsset(root))throw new Exception("Cannot delete "+root);
        AssetDatabase.SaveAssets();File.WriteAllText(Dir+"result.txt","PASS old font dependencies removed; new TMP Chinese/numeric glyphs generated; old folders deleted.\n");
    }
}
