using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LegacyBannerCleanup
{
    const string Request = "output/battle-hud-dock/remove-banner.request";
    static LegacyBannerCleanup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        File.Delete(Request);
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Gameplay") { File.WriteAllText(Request+".result", "FAIL: Open Gameplay first"); return; }
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Banner Ad Background").ToArray();
        foreach (var node in nodes) Undo.DestroyObjectImmediate(node.gameObject);
        if (nodes.Length > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        File.WriteAllText(Request+".result", "PASS removed "+nodes.Length+" unused banner background");
    }
}
