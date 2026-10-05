using System;
using System.IO;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DarkFairytaleValidation
{
    [MenuItem("Tools/Dark Fairytale/Validate Player %#F6")]
    public static void ValidatePlayer()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var report = new StringBuilder();
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var player = go.GetComponent<Player>();
            if (player.useEquipmentAppearance) throw new Exception("Red Hood must keep her costume.");
            var skeleton = player.skeletonAnimation;
            skeleton.Initialize(true);
            player.SetSkinsAndAttachments(); // Must not require inventory or legacy slots.
            player.SetSkin("0");
            foreach (var name in new[] { "Idle1", "Attack1", "Attack2", "Skill4", "Skill5", "Skill7", "Death1", "Rebirth", "Victory" })
            {
                var animation = skeleton.Skeleton.Data.FindAnimation(name);
                if (animation == null) throw new Exception("Missing " + name);
                int hits = 0, completes = 0;
                Spine.AnimationState.TrackEntryEventDelegate handler = (entry, e) =>
                {
                    if (e.Data.Name == "OnHit") hits++;
                    if (e.Data.Name == "OnComplete") completes++;
                };
                skeleton.AnimationState.ClearTracks();
                skeleton.Skeleton.SetToSetupPose();
                skeleton.AnimationState.Event += handler;
                skeleton.AnimationState.SetAnimation(0, name, false);
                for (int i = 0; i <= Mathf.CeilToInt(animation.Duration * 60) + 2; i++)
                    skeleton.Update(1f / 60);
                skeleton.AnimationState.Event -= handler;
                bool attack = name.StartsWith("Attack") || name.StartsWith("Skill");
                if (attack && (hits != 1 || completes != 1)) throw new Exception(name + " has invalid combat events");
                if (name == "Death1" && completes != 1) throw new Exception("Death must complete for resurrection");
                report.AppendLine(name + ": hits=" + hits + ", completes=" + completes);
            }
            skeleton.AnimationState.ClearTracks();
            skeleton.Skeleton.SetToSetupPose();
            skeleton.AnimationState.SetAnimation(0, "Idle1", true);
            skeleton.Update(0);
            skeleton.LateUpdate();
            var flash = go.GetComponentInChildren<MaterialSwapper>();
            if (flash.origMats.Count != 1 || flash.whiteMats.Length != 1 ||
                flash.origMats[0].mainTexture != flash.whiteMats[0].mainTexture)
                throw new Exception("Flash material must use the Red Hood atlas");
            var camObject = new GameObject("Preview Camera");
            SceneManager.MoveGameObjectToScene(camObject, scene);
            var camera = camObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, -4.5f, -15);
            camera.fieldOfView = 66;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            camera.scene = scene;
            Directory.CreateDirectory("output/dark-fairytale-battle");
            Render(camera, "output/dark-fairytale-battle/player-integration.png");
            report.AppendLine("PASS: prefab, costume, combat events, resurrection event, atlas and flash material.");
            File.WriteAllText("output/dark-fairytale-battle/validation.txt", report.ToString());
            Debug.Log("DARK_FAIRYTALE_PLAYER_VALIDATION_PASSED\n" + report);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [MenuItem("Tools/Dark Fairytale/Open Gameplay %#F7")]
    public static void OpenGameplay()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
    }

    public static void Render(Camera camera, string path)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var rt = new RenderTexture(720, 1280, 24);
        var texture = new Texture2D(720, 1280, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
