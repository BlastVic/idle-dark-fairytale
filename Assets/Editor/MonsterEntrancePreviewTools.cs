using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit requests only; never rebuilds UI on import or on entering Play.
[InitializeOnLoad]
public static class MonsterEntrancePreviewTools
{
    const string Dir = "output/monster-entrance/";
    const string Key = "MonsterEntranceQA";
    static int step;
    static double until;
    static int sample;
    static readonly string[] Targets = { "MobTallowkin", "MobHexmaw", "MobGloamweb" };
    static float pausedY, pausedTime;
    static MonsterEntrancePreviewTools() { EditorApplication.update += Tick; }

    [MenuItem("Tools/Dark Fairytale/Monsters/Add Entrance Test Button")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play before authoring.");
        var root = PrefabUtility.LoadPrefabContents(MonsterPreviewUiBuilder.HudPath);
        try {
            var hud = root.GetComponent<MonsterPreviewHud>();
            if (!hud.entranceButton) {
                var button = UnityEngine.Object.Instantiate(hud.replayButton, hud.replayButton.transform.parent);
                button.name = "TestEntrance";
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = new Vector2(510, -4);
                rect.sizeDelta = new Vector2(158, 34);
                button.GetComponentInChildren<Text>().text = "测试出场";
                button.GetComponentInChildren<Text>().fontSize = 20;
                button.gameObject.AddComponent<StorybookEquipmentButtonState>();
                hud.entranceButton = button;
                ((RectTransform)hud.statusText.transform).sizeDelta = new Vector2(475, 28);
                PrefabUtility.SaveAsPrefabAsset(root, MonsterPreviewUiBuilder.HudPath);
            }
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Dark Fairytale/Monsters/Validate Entrance Preview")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new Exception("Save the current scene before validation.");
        EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Dir + "validation.txt", "Entrance preview: shared production DropIn\n");
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + "Size", (int)InvokeQA("CurrentSize"));
        step = 0; sample = 0;
        EditorApplication.isPlaying = true;
    }
    static object InvokeQA(string name, params object[] args) => typeof(MonsterPreviewValidation).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    static void SetResolution(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameViewSizes");
        var instance = type.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var groupType = type.GetProperty("currentGroupType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        var group = type.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
        var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
            new object[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Entrance QA " + width + "x" + height }, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        InvokeQA("SetSize", (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1);
    }
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); File.AppendAllText(Dir + "validation.txt", "PASS " + text + "\n"); }
    static void Next(int value, double delay = .5) { step = value; until = EditorApplication.timeSinceStartup + delay; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try {
            if (File.Exists(Dir + "command.request")) {
                var cmd = File.ReadAllText(Dir + "command.request").Trim(); File.Delete(Dir + "command.request");
                if (cmd == "install") { Install(); File.WriteAllText(Dir + "installed.txt", "OK"); }
                if (cmd == "validate") Validate();
            }
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < until) return;
            var t = UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
            if (!t || t.SelectedMonster < 0) return;
            if (step == 0) {
                int[] w = {720, 768, 720}, h = {1280, 1024, 1440};
                SetResolution(w[sample], h[sample]);
                t.SelectMonster(Array.FindIndex(t.monsters, x => x.name.StartsWith(Targets[sample], StringComparison.Ordinal)));
                Check(t.CanTestEntrance && t.hud.entranceButton, Targets[sample] + " button and prefab settings");
                if (!t.GetComponent<MonsterPreviewRuntimeProbe>()) t.gameObject.AddComponent<MonsterPreviewRuntimeProbe>().hud = t.hud;
                Next(1, 1);
            } else if (step == 1) {
                var probe = t.GetComponent<MonsterPreviewRuntimeProbe>();
                Check(probe.width == new[] {720,768,720}[sample] && probe.height == new[] {1280,1024,1440}[sample], "Actual Game View ratio " + probe.width + "x" + probe.height);
                t.hud.entranceButton.onClick.Invoke();
                Check(t.EntranceMode, "Button starts entrance");
                t.SetSpeed(.5f);
                Next(2, .3);
            } else if (step == 2) {
                t.SetPaused(true); Next(3, .15);
            } else if (step == 3) {
                pausedY = t.character.transform.position.y;
                pausedTime = t.character.AnimationState.GetCurrent(0).TrackTime;
                ScreenCapture.CaptureScreenshot(Dir + Targets[sample] + "-paused.png");
                Next(4, .4);
            } else if (step == 4) {
                Check(Mathf.Abs(pausedY - t.character.transform.position.y) < .001f && Mathf.Abs(pausedTime - t.character.AnimationState.GetCurrent(0).TrackTime) < .001f, "Pause freezes movement and Spine");
                t.SetPaused(false); t.SetSpeed(2); Next(5, 5);
            } else if (step == 5) {
                Check(t.EntranceStatus == "完成 · 待机", "Entrance completes and remains idle");
                ScreenCapture.CaptureScreenshot(Dir + Targets[sample] + "-complete.png");
                t.hud.replayButton.onClick.Invoke(); Check(t.EntranceMode, "Replay restarts entrance");
                t.Play(0); Check(!t.EntranceMode, "Animation selection cancels entrance");
                t.TestEntrance(); t.SelectMonster(t.SelectedMonster); Check(!t.EntranceMode, "Reselect during entrance cleans up");
                if (++sample < Targets.Length) Next(0); else {
                    Check(!UnityEngine.Object.FindFirstObjectByType<Enemy>(), "No gameplay Enemy instantiated");
                    File.AppendAllText(Dir + "validation.txt", "COMPLETE\n");
                    SessionState.SetBool(Key, false);
                    InvokeQA("SetSize", SessionState.GetInt(Key + "Size", 0));
                    EditorApplication.isPlaying = false;
                }
            }
        } catch (Exception e) {
            Directory.CreateDirectory(Dir); File.AppendAllText(Dir + "validation.txt", "FAIL " + e + "\n");
            SessionState.SetBool(Key, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Debug.LogException(e);
        }
    }
}
