using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

// Read-only validation, explicitly requested; never regenerates production assets.
[InitializeOnLoad]
public static class ProjectStructureValidation
{
    const string Dir = "output/project-structure/";
    [Serializable] public class Entry { public string old, path, guid, sha256; }
    [Serializable] public class Manifest { public Entry[] entries; }
    static ProjectStructureValidation() { EditorApplication.update += () => {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Dir+"validate.request")) return;
        File.Delete(Dir+"validate.request");
        try { Validate(); } catch(Exception e) { File.WriteAllText(Dir+"validation.txt", "FAIL "+e); Debug.LogException(e); }
    }; }
    [MenuItem("Tools/Resource Optimization/Validate Project Structure")]
    public static void Validate()
    {
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/Editor/ResourceAudit/Data/ProjectStructureMigration.json"));
        int prefabs=0, skeletons=0;
        foreach(var e in manifest.entries) {
            if(File.Exists(e.old) || !File.Exists(e.path)) throw new Exception("Migration path: "+e.path);
            if(AssetDatabase.AssetPathToGUID(e.path)!=e.guid) throw new Exception("GUID changed: "+e.path);
            var asset=AssetDatabase.LoadMainAssetAtPath(e.path);
            if(!asset) throw new Exception("Cannot import: "+e.path);
            if(e.path.EndsWith(".prefab")) {
                var go=asset as GameObject;
                if(!go || go.GetComponentsInChildren<Component>(true).Any(c=>!c)) throw new Exception("Missing prefab script: "+e.path);
                prefabs++;
            }
            if(asset is SkeletonDataAsset skeleton) {
                if(skeleton.GetSkeletonData(false)==null) throw new Exception("Cannot load Spine: "+e.path);
                skeletons++;
            }
        }
        foreach(var key in new[]{"Data/GameSettings","Data/SkillsDataDescriptions","Data/LevelDataDescriptions","Environment/Chapter01Environments","Localization/IdleLocalTexts"})
            if(!Resources.Load(key)) throw new Exception("Resource load failed: "+key);
        if(!Assets.Scripts.Services.GameSettings.Instance || !Scripts.Level.LevelDataDescriptions.Instance || !Scripts.Skills.SkillsDataDescriptions.Instance) throw new Exception("Data singleton load failed");
        ArtSourcePaths.ValidateBuildDependencies();
        foreach(var record in Resources.LoadAll<LocalPrefabEntry>("LocalPrefabs"))
            if(!record.prefab) throw new Exception("Missing local prefab: "+record.name);
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Dir+"validation.txt",$"PASS {manifest.entries.Length} moved assets imported with original GUIDs; {prefabs} prefabs have no missing scripts; {skeletons} Spine skeletons load.\nPASS local prefab records resolve; authoring-only sources excluded from runtime dependencies.\nDONE\n");
    }
}
