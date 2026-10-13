using System.IO;
using IdleKnightHero.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit request only. Does not rebuild UI or replace unrelated scene state.
[InitializeOnLoad]
public static class HunterCabinAudioSetup
{
    const string Root = "output/hunter-cabin/";
    const string ClipPath = "Assets/Audio/HunterCabin/HunterCabinLoop.wav";
    static double nextSample;
    static float lastPosition = -1;
    static bool wrapped;
    static HunterCabinAudioSetup() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Root + "install.request"))
        {
            File.Delete(Root + "install.request");
            Install();
        }
        if (!EditorApplication.isPlaying || !File.Exists(Root + "observe.request") || EditorApplication.timeSinceStartup < nextSample) return;
        nextSample = EditorApplication.timeSinceStartup + 1;
        var manager = Object.FindFirstObjectByType<MusicManager>();
        if (!manager || !manager.backgroundMusicSource) return;
        var s = manager.backgroundMusicSource;
        if (!s.clip || s.clip.name != "HunterCabinLoop" || !s.isPlaying || s.mute) return;
        if (lastPosition > s.time + 20) wrapped = true;
        lastPosition = s.time;
        File.WriteAllText(Root + "runtime-validation.txt", $"clip={s.clip.name}; duration={s.clip.length}; playing={s.isPlaying}; loop={s.loop}; mute={s.mute}; volume={s.volume}; time={s.time}; observedWrap={wrapped}\n");
        if (wrapped) File.Delete(Root + "observe.request");
    }
    [MenuItem("Tools/Dark Fairytale/Audio/Install Hunter Cabin Ambience")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScenes/Gameplay.unity") throw new System.InvalidOperationException("Open Gameplay scene first.");
        var manager = Object.FindFirstObjectByType<MusicManager>();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (!manager || !clip || manager.audioClips.Length == 0) throw new System.InvalidOperationException("Missing camp music slot or cabin clip.");
        Undo.RecordObject(manager, "Set hunter cabin ambience");
        manager.audioClips[0] = clip;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Root + "import-validation.txt", $"PASS: Gameplay MusicManager.audioClips[0]={clip.name}; duration={clip.length}; battle={manager.audioClips[1].name}\n");
    }
}
