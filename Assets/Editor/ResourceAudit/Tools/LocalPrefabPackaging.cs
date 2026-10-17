using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Scripts.Level;

// Addressable GUIDs remain the authoring keys. The offline player resolves them
// through individual Resources records, without a second copy in asset bundles.
public sealed class LocalPrefabPackaging : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report) { Prepare(); }

    [MenuItem("Tools/Resource Optimization/Prepare Single-copy Local Prefabs")]
    public static void Prepare()
    {
        const string root = "Assets/Resources/LocalPrefabs";
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!settings) throw new BuildFailedException("Missing prefab authoring settings");
        var entries = settings.groups.Where(g => g != null).SelectMany(g => g.entries).ToArray();
        Directory.CreateDirectory(root);
        AssetDatabase.Refresh();
        var expected = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in entries)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
            if (!prefab) throw new BuildFailedException("Local prefab is missing or not a GameObject: " + entry.AssetPath);
            var path = root + "/" + entry.guid + ".asset";
            expected.Add(path);
            var record = AssetDatabase.LoadAssetAtPath<LocalPrefabEntry>(path);
            if (!record) { record = ScriptableObject.CreateInstance<LocalPrefabEntry>(); AssetDatabase.CreateAsset(record, path); }
            if (record.prefab != prefab) { record.prefab = prefab; EditorUtility.SetDirty(record); }
        }
        foreach (var path in Directory.GetFiles(root, "*.asset"))
            if (!expected.Contains(path.Replace('\\','/'))) AssetDatabase.DeleteAsset(path);
        foreach (var group in settings.groups.Where(g=>g != null))
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema && schema.IncludeInBuild) { schema.IncludeInBuild = false; EditorUtility.SetDirty(schema); }
        }
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        // Remove stale generated bundles/catalogs from earlier builds as well.
        AddressableAssetSettings.CleanPlayerContent();
        var database = AssetDatabase.LoadAssetAtPath<LevelDataDescriptions>("Assets/Resources/Data/LevelDataDescriptions.asset");
        var serialized = new SerializedObject(database);
        foreach (var name in new[]{"_enemyDb","_mapDb","_particleDb","_generalObjectsDb"})
        {
            var list=serialized.FindProperty(name);
            for(int i=0;i<list.arraySize;i++)
            {
                var row=list.GetArrayElementAtIndex(i);
                var guid=row.FindPropertyRelative("assetRef").FindPropertyRelative("m_AssetGUID").stringValue;
                if (!expected.Contains(root+"/"+guid+".asset")) throw new BuildFailedException("Missing local prefab for "+row.FindPropertyRelative("key").stringValue);
            }
        }
        Debug.Log("Local prefab packaging validated: " + expected.Count + " prefabs; no duplicate Addressable bundles.");
    }
}
