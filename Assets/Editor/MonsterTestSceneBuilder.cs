using System;
using System.IO;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MonsterTestSceneBuilder
{
    public const string ScenePath = "Assets/Tests/MonsterAnimation/MonsterAnimationTest.unity";

    [MenuItem("Tools/Dark Fairytale/Monsters/Open Test Scene %#F11")]
    public static void Open()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Refresh Monster List")]
    public static void Refresh()
    {
        var tester = FindTester();
        Undo.RecordObject(tester, "Refresh monster catalog");
        tester.RefreshCatalog();
        EditorUtility.SetDirty(tester);
        EditorSceneManager.MarkSceneDirty(tester.gameObject.scene);
        Debug.Log("Monster catalog: " + tester.monsters.Length + " assets. Save the scene to retain the list for builds.");
    }

    static MonsterAnimationTest FindTester()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects())
            {
                var tester = root.GetComponentInChildren<MonsterAnimationTest>(true);
                if (tester != null) return tester;
            }
        throw new InvalidOperationException("Open the shared MonsterAnimationTest scene first.");
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Validate Imported Monsters %#F10")]
    public static void Validate()
    {
        var tester = FindTester();
        tester.RefreshCatalog();
        var report = new StringBuilder("Shared monster animation validation\n");
        int failed = 0;
        if (tester.monsters.Length == 0) throw new InvalidOperationException("No monsters in the catalog.");
        foreach (var data in tester.monsters)
        {
            SkeletonAnimation preview = null;
            try
            {
                preview = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
                preview.gameObject.hideFlags = HideFlags.HideAndDontSave;
                // Validation does not need to render into the user's scene.
                preview.GetComponent<Renderer>().enabled = false;
                foreach (var animation in preview.Skeleton.Data.Animations)
                {
                    preview.AnimationState.ClearTracks();
                    preview.Skeleton.SetToSetupPose();
                    preview.AnimationState.SetAnimation(0, animation.Name, false);
                    int frames = Mathf.CeilToInt(animation.Duration * 60) + 2;
                    for (int frame = 0; frame <= frames; frame++)
                    {
                        preview.Update(1f / 60);
                        preview.LateUpdate();
                        var mesh = preview.GetComponent<MeshFilter>().sharedMesh;
                        if (mesh == null) throw new Exception(animation.Name + ": no mesh");
                        foreach (var vertex in mesh.vertices)
                            if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsInfinity(vertex.x) || float.IsInfinity(vertex.y))
                                throw new Exception(animation.Name + ": non-finite vertex");
                    }
                    report.AppendLine(data.name + " / " + animation.Name + ": PASS (" + frames + " frames)");
                }
                report.AppendLine(data.name + ": " + preview.Skeleton.Data.Animations.Count + " animations loaded.");
            }
            catch (Exception ex) { failed++; report.AppendLine(data.name + ": FAIL " + ex.Message); }
            finally { if (preview != null) UnityEngine.Object.DestroyImmediate(preview.gameObject); }
        }
        report.AppendLine("Assets: " + tester.monsters.Length + "; failed: " + failed);
        Directory.CreateDirectory("output/monster-animation");
        File.WriteAllText("output/monster-animation/validation.txt", report.ToString());
        if (failed > 0) Debug.LogError(report.ToString());
        else Debug.Log(report.ToString());
    }
}
