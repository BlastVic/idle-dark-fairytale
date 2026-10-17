using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DeveloperLevelAuthoring
{
    static DeveloperLevelAuthoring(){EditorApplication.update += Tick;}
    static void Tick()
    {
        const string request = "output/developer-overlay/add-levels.request";
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        File.Delete(request);
        var root = PrefabUtility.LoadPrefabContents(DeveloperOverlayAuthoring.Path);
        try {
            var view = root.GetComponent<DeveloperOverlayUI>();
            if(!view.levels) {
                view.levels = Object.Instantiate(view.points, view.body.transform);
                view.levels.name = "等级 +10";
                view.levels.GetComponentInChildren<Text>().text = "等级 +10";
                DeveloperOverlayAuthoring.ArrangeActions(view);
                PrefabUtility.SaveAsPrefabAsset(root, DeveloperOverlayAuthoring.Path);
            }
            File.WriteAllText("output/developer-overlay/add-levels.txt", "PASS");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
