using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Read-only audit. No import settings, scenes, prefabs or Addressables are changed.
[InitializeOnLoad]
public static class ArtResourceAudit
{
    const string Output = "output/resource-audit";
    static ArtResourceAudit() { EditorApplication.update += Poll; }

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var request = Output + "/audit.request";
        if (!File.Exists(request)) return;
        File.Delete(request);
        try { Run(); }
        catch (Exception e) { File.WriteAllText(Output + "/error.txt", e.ToString()); Debug.LogException(e); }
    }

    static HashSet<string> Dependencies(IEnumerable<string> roots)
    {
        var paths = roots.Distinct().ToArray();
        return paths.Length == 0 ? new HashSet<string>() : new HashSet<string>(AssetDatabase.GetDependencies(paths, true));
    }

    static string Cell(object value) { return "\"" + Convert.ToString(value).Replace("\"", "\"\"") + "\""; }
    static void Row(StringBuilder csv, params object[] values) { csv.AppendLine(string.Join(",", values.Select(Cell))); }

    [MenuItem("Tools/Resource Optimization/Export Read-only Audit")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && File.Exists(p)).OrderBy(p => p).ToArray();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var sceneDeps = Dependencies(scenes);
        var resourceRoots = paths.Where(p => p.Contains("/Resources/") && !p.Contains("/Editor/")).ToArray();
        var resourceDeps = Dependencies(resourceRoots);
        var settingsRoots = Directory.GetFiles("ProjectSettings", "*.asset").SelectMany(p =>
            System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(p), @"guid:\s*([a-f0-9]{32})")
            .Cast<System.Text.RegularExpressions.Match>().Select(m => AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))
            .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/AddressableAssetsData/"));
        var settingsDeps = Dependencies(settingsRoots);
        var entries = new List<AddressableAssetEntry>();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null) settings.GetAllAssets(entries, false, g =>
            g.GetSchema<BundledAssetGroupSchema>() != null && g.IncludeInBuild);
        var addressDeps = Dependencies(entries.Select(e => e.AssetPath));
        var incoming = paths.ToDictionary(p => p, p => new List<string>());
        foreach (var path in paths)
            foreach (var dep in AssetDatabase.GetDependencies(path, false))
                if (dep != path && incoming.TryGetValue(dep, out var refs)) refs.Add(path);
        var csv = new StringBuilder();
        Row(csv, "path", "source_bytes", "scene_dependency", "resources_dependency", "addressable_dependency", "settings_dependency", "direct_referencers", "referencers", "max_texture_size", "texture_compression", "readable");
        foreach (var path in paths)
        {
            var texture = AssetImporter.GetAtPath(path) as TextureImporter;
            Row(csv, path, new FileInfo(path).Length, sceneDeps.Contains(path), resourceDeps.Contains(path), addressDeps.Contains(path), settingsDeps.Contains(path), incoming[path].Count, string.Join(" | ", incoming[path]), texture ? texture.maxTextureSize.ToString() : "", texture ? texture.textureCompression.ToString() : "", texture ? texture.isReadable.ToString() : "");
        }
        File.WriteAllText(Output + "/assets.csv", csv.ToString());
        csv.Clear();
        Row(csv, "address", "path", "group", "source_dependency_bytes");
        foreach (var entry in entries)
            Row(csv, entry.address, entry.AssetPath, entry.parentGroup.Name, Dependencies(new[] { entry.AssetPath }).Where(File.Exists).Sum(p => new FileInfo(p).Length));
        File.WriteAllText(Output + "/addressables.csv", csv.ToString());
        var summary = new StringBuilder();
        summary.AppendLine("Audit UTC: " + DateTime.UtcNow.ToString("O"));
        summary.AppendLine("Enabled scenes: " + string.Join(", ", scenes));
        summary.AppendLine("Asset files: " + paths.Length + "; source bytes: " + paths.Sum(p => new FileInfo(p).Length));
        summary.AppendLine("Included bundled Addressable entries: " + entries.Count);
        summary.AppendLine("Source sizes are NOT build sizes. Dependency overlap is NOT proof of duplication. Unreachable assets are candidates, not safe-to-delete conclusions: string loading, custom build processors, editor authoring and retained references require review.");
        var report = BuildReport.GetLatestReport();
        csv.Clear();
        Row(csv, "packed_file", "source_path", "packed_bytes");
        if (report != null)
        {
            summary.AppendLine("Last build: " + report.summary.buildStartedAt.ToString("O") + "; platform=" + report.summary.platform + "; result=" + report.summary.result + "; total bytes=" + report.summary.totalSize);
            summary.AppendLine("Last build output: " + report.summary.outputPath);
            summary.AppendLine("Historical build contents may differ from current assets; packed byte sizes are not final compressed download sizes and may omit bundle internals.");
            File.WriteAllLines(Output + "/last-build-warnings.txt", report.steps.SelectMany(step => step.messages).Where(message => message.type == LogType.Warning || message.type == LogType.Error).Select(message => message.type + ": " + message.content));
            foreach (var pack in report.packedAssets)
                foreach (var item in pack.contents) Row(csv, pack.shortPath, item.sourceAssetPath, item.packedSize);
        }
        else summary.AppendLine("No previous BuildReport available.");
        File.WriteAllText(Output + "/last-build-assets.csv", csv.ToString());
        File.WriteAllText(Output + "/summary.txt", summary.ToString());
        Debug.Log("Resource audit exported to " + Output);
    }
}
