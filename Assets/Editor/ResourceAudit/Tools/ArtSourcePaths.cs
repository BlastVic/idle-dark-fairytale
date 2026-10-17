using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

// Compatibility for older authoring code that composes source paths dynamically.
public static class ArtSourcePaths
{
    public const string Root = "Assets/ArtSources/";
    [Serializable] public class Entry { public string old, path, sha256, guid; }
    [Serializable] public class Manifest { public Entry[] entries; }
    public static Manifest Read() => JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/Editor/ResourceAudit/Data/ArtSourceManifest.json"));
    public static string Resolve(string path) => Read().entries.FirstOrDefault(e => e.old == path)?.path ?? path;

    public static void Validate()
    {
        foreach (var e in Read().entries)
        {
            if (!File.Exists(e.path) || File.Exists(e.old)) throw new Exception("Source migration incomplete: " + e.path);
            if (!string.IsNullOrEmpty(e.guid) && AssetDatabase.AssetPathToGUID(e.path) != e.guid) throw new Exception("Source GUID changed: " + e.path);
            using (var sha = System.Security.Cryptography.SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(e.path))).Replace("-", "").ToLowerInvariant() != e.sha256)
                    throw new Exception("Source content changed: " + e.path);
            if (e.path.EndsWith(".png") && !AssetDatabase.LoadAssetAtPath<Texture2D>(e.path)) throw new Exception("Texture import failed: " + e.path);
        }
        ValidateBuildDependencies();
    }
    public static void ValidateBuildDependencies()
    {
        var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !p.Contains("/Editor/") && File.Exists(p)));
        foreach (var f in Directory.GetFiles("ProjectSettings", "*.asset"))
            roots.AddRange(System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(f), @"guid:\s*([a-f0-9]{32})")
                .Cast<System.Text.RegularExpressions.Match>().Select(m => AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))
                .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/AddressableAssetsData/")));
        var entries = new List<AddressableAssetEntry>();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings) settings.GetAllAssets(entries, false, g => g.GetSchema<BundledAssetGroupSchema>() != null && g.IncludeInBuild);
        roots.AddRange(entries.Select(e => e.AssetPath));
        var illegal = AssetDatabase.GetDependencies(roots.Distinct().ToArray(), true).Where(p => p.StartsWith(Root)).ToArray();
        if (illegal.Length > 0) throw new BuildFailedException("Authoring-only art must not ship:\n" + string.Join("\n", illegal));
    }
}

public sealed class ArtSourceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 1000;
    public void OnPreprocessBuild(BuildReport report) => ArtSourcePaths.ValidateBuildDependencies();
}

[InitializeOnLoad]
public static class ArtSourceMigrationValidation
{
    static ArtSourceMigrationValidation() { EditorApplication.update += () => {
        const string dir = "output/art-source-move/";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(dir + "validate.request")) return;
        File.Delete(dir + "validate.request");
        try { ArtSourcePaths.Validate(); File.WriteAllText(dir + "validation.txt", "PASS source bytes, GUIDs, texture imports and no runtime dependency on Assets/ArtSources."); }
        catch (Exception e) { File.WriteAllText(dir + "validation.txt", "FAIL " + e); Debug.LogException(e); }
    }; }
}
