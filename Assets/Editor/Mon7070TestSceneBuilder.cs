using System;
using System.IO;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Mon7070TestSceneBuilder
{
    const string Root = "Assets/Tests/Mon7070/";
    public const string ScenePath = Root + "Mon7070AnimationTest.unity";

    [MenuItem("Tools/Dark Fairytale/Mon7070/Create and Validate Test Scene %#F10")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var loaded = SceneManager.GetSceneByPath(ScenePath);
        if (loaded.isLoaded) throw new InvalidOperationException("Open a different scene before rebuilding the test scene.");
        ImportSource();
        var texturePath = Root + "Art/mon_7070.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true; // ImportSource converts PMA to straight alpha for Linear rendering.
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/mon_7070_Material.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Spine/Skeleton"));
            AssetDatabase.CreateAsset(material, Root + "Art/mon_7070_Material.mat");
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.SetFloat("_StraightAlphaInput", 1);
        material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
        EditorUtility.SetDirty(material);
        var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Root + "Art/mon_7070_Atlas.asset");
        if (atlas == null)
        {
            atlas = ScriptableObject.CreateInstance<SpineAtlasAsset>();
            AssetDatabase.CreateAsset(atlas, Root + "Art/mon_7070_Atlas.asset");
        }
        atlas.atlasFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "Art/mon_7070.atlas.txt");
        atlas.materials = new[] { material };
        atlas.Clear();
        EditorUtility.SetDirty(atlas);
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root + "Art/mon_7070_SkeletonData.asset");
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SkeletonDataAsset>();
            AssetDatabase.CreateAsset(data, Root + "Art/mon_7070_SkeletonData.asset");
        }
        data.atlasAssets = new AtlasAssetBase[] { atlas };
        data.skeletonJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "Art/mon_7070.json");
        data.scale = 0.01f;
        data.defaultMix = 0;
        data.Clear();
        EditorUtility.SetDirty(data);
        if (data.GetSkeletonData(false) == null) throw new Exception("Skeleton import failed.");
        AssetDatabase.SaveAssets();

        var previousScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var character = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            character.name = "mon_7070";
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.13f, 0.18f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            camera.scene = scene;
            var report = new StringBuilder("mon_7070: Unity / Spine animation validation\n");
            report.AppendLine("Source 3.8.75; local compatibility header 3.8.99; source animation data unchanged.");
            Bounds allBounds = Validate(character, report);
            SetPose(character, "idle", 0);
            Bounds bounds = character.GetComponent<MeshFilter>().sharedMesh.bounds;
            report.AppendLine("All animation bounds: " + allBounds + "; idle framing: " + bounds);
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.size.y * 0.17f, -10);
            camera.orthographicSize = Mathf.Max(bounds.size.y * 0.85f, bounds.size.x / (9f / 16) * 0.65f);
            var tester = new GameObject("Animation Controls").AddComponent<Mon7070AnimationTest>();
            tester.character = character;
            tester.previewCamera = camera;
            Directory.CreateDirectory("output/mon7070");
            foreach (var animation in data.GetSkeletonData(false).Animations)
            {
                SetPose(character, animation.Name, animation.Duration * 0.45f);
                DarkFairytaleValidation.Render(camera, "output/mon7070/" + animation.Name.Replace('/', '_') + ".png");
            }
            SetPose(character, "idle", 0);
            character.AnimationName = "idle";
            character.loop = true;
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Scene save failed.");
            report.AppendLine("PASS: all animations completed, animated finite mesh geometry, rendered previews, scene saved.");
            File.WriteAllText("output/mon7070/validation.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        finally
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.Refresh();
    }

    static Bounds Validate(SkeletonAnimation character, StringBuilder report)
    {
        var total = new Bounds();
        bool initialized = false;
        foreach (var animation in character.Skeleton.Data.Animations)
        {
            SetPose(character, animation.Name, 0);
            int completes = 0;
            character.AnimationState.GetCurrent(0).Complete += entry => completes++;
            Vector3[] first = null;
            bool changed = false;
            int frames = Mathf.CeilToInt(animation.Duration * 60) + 2;
            for (int frame = 0; frame <= frames; frame++)
            {
                character.Update(1f / 60);
                character.LateUpdate();
                var mesh = character.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null || mesh.vertexCount == 0) throw new Exception(animation.Name + ": empty mesh");
                var vertices = mesh.vertices;
                if (first == null) first = vertices;
                if (first.Length != vertices.Length) changed = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var p = vertices[i];
                    if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
                        throw new Exception(animation.Name + ": non-finite mesh vertex");
                    if (i < first.Length && (p - first[i]).sqrMagnitude > 0.000001f) changed = true;
                }
                if (!initialized) { total = mesh.bounds; initialized = true; }
                else total.Encapsulate(mesh.bounds);
            }
            if (!changed || completes != 1) throw new Exception(animation.Name + ": movement/completion check failed");
            report.AppendLine(animation.Name + ": duration=" + animation.Duration.ToString("0.000") + "s; samples=" + frames + "; moving mesh; complete=" + completes);
        }
        return total;
    }

    static void ImportSource()
    {
        const string source = "Spine/mon_7070/mon_7070";
        string json = File.ReadAllText(source + ".json");
        if (!json.Contains("\"spine\": \"3.8.75\""))
            throw new InvalidOperationException("Recheck compatibility: expected the original mon_7070 Spine 3.8.75 export.");
        File.WriteAllText(Root + "Art/mon_7070.json", json.Replace("\"spine\": \"3.8.75\"", "\"spine\": \"3.8.99\""));
        File.Copy(source + ".atlas.txt", Root + "Art/mon_7070.atlas.txt", true);
        // Unpremultiply source bytes before sRGB sampling in this Linear-color-space project.
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            if (!texture.LoadImage(File.ReadAllBytes(source + ".png"))) throw new Exception("PNG decode failed.");
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a > 0 && p.a < 255)
                {
                    p.r = (byte)Mathf.Min(255, (p.r * 255 + p.a / 2) / p.a);
                    p.g = (byte)Mathf.Min(255, (p.g * 255 + p.a / 2) / p.a);
                    p.b = (byte)Mathf.Min(255, (p.b * 255 + p.a / 2) / p.a);
                    pixels[i] = p;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Root + "Art/mon_7070.png", texture.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    static void SetPose(SkeletonAnimation character, string name, float time)
    {
        character.AnimationState.ClearTracks();
        character.Skeleton.SetToSetupPose();
        character.AnimationState.SetAnimation(0, name, false);
        character.Update(time);
        character.LateUpdate();
    }

    [MenuItem("Tools/Dark Fairytale/Mon7070/Open Test Scene %#F11")]
    public static void Open()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
    }
}
