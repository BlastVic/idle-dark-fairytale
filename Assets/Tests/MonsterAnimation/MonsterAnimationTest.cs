using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class MonsterAnimationTest : MonoBehaviour
{
    public SkeletonAnimation character;
    public Camera previewCamera;
    public SkeletonDataAsset[] monsters = new SkeletonDataAsset[0];
    [Tooltip("Optional prefab references: preview copies the display transform scale, including facing.")]
    public GameObject[] monsterPrefabs = new GameObject[0];
    [Tooltip("Editor Play mode scans these folders for imported SkeletonDataAsset assets.")]
    public string[] discoveryFolders = { "Assets/Tests" };
    public bool loop = true;
    public bool autoCycle;
    [Range(0.1f, 2f)] public float speed = 1f;
    string[] names = new string[0];
    int selected, selectedMonster = -1;
    bool paused;
    float elapsed, baseSize = 5, zoom = 1;
    string search = "", error;
    Vector2 monsterScroll, animationScroll;

    public void RefreshCatalog()
    {
        var found = new List<SkeletonDataAsset>();
        foreach (var data in monsters ?? new SkeletonDataAsset[0])
            if (data != null && !found.Contains(data)) found.Add(data);
        if (character != null && character.skeletonDataAsset != null && !found.Contains(character.skeletonDataAsset))
            found.Add(character.skeletonDataAsset);
#if UNITY_EDITOR
        var folders = new List<string>();
        foreach (var folder in discoveryFolders ?? new string[0])
            if (!string.IsNullOrEmpty(folder) && UnityEditor.AssetDatabase.IsValidFolder(folder)) folders.Add(folder);
        if (folders.Count > 0)
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:SkeletonDataAsset", folders.ToArray()))
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && !found.Contains(data)) found.Add(data);
            }
#endif
        found.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        monsters = found.ToArray();
    }

    void Start()
    {
        var initial = character != null ? character.skeletonDataAsset : null;
        RefreshCatalog();
        if (monsters.Length > 0) SelectMonster(Mathf.Max(0, Array.IndexOf(monsters, initial)));
        else error = "No monsters. Import Spine assets into Assets/Tests/Monsters or assign Monsters in the Inspector.";
    }

    public void SelectMonster(int index)
    {
        if (index < 0 || index >= monsters.Length) return;
        selectedMonster = index;
        names = new string[0];
        selected = 0;
        elapsed = 0;
        animationScroll = Vector2.zero;
        error = null;
        if (character != null) character.gameObject.SetActive(false);
        try
        {
            var data = monsters[index];
            if (data == null || data.GetSkeletonData(false) == null) throw new Exception("Skeleton data could not be loaded.");
            if (character != null) Destroy(character.gameObject);
            character = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            character.name = data.name;
            character.transform.SetParent(transform, false);
            foreach (var prefab in monsterPrefabs)
            {
                if (prefab == null) continue;
                var display = prefab.GetComponentInChildren<SkeletonAnimation>(true);
                if (display != null && display.skeletonDataAsset == data)
                {
                    character.transform.localScale = display.transform.localScale;
                    break;
                }
            }
            character.timeScale = paused ? 0 : speed;
            var animations = character.Skeleton.Data.Animations;
            names = new string[animations.Count];
            for (int i = 0; i < names.Length; i++) names[i] = animations.Items[i].Name;
            int idle = Array.FindIndex(names, n => n == "Idle1");
            if (idle < 0) idle = Array.FindIndex(names, n => string.Equals(n, "idle", StringComparison.OrdinalIgnoreCase));
            if (names.Length > 0) Play(Mathf.Max(0, idle));
            else { character.Update(0); character.LateUpdate(); }
            FitCamera();
        }
        catch (Exception ex)
        {
            names = new string[0];
            if (character != null) character.gameObject.SetActive(false);
            error = "Cannot preview " + monsters[index].name + ": " + ex.Message;
            Debug.LogWarning(error, this);
        }
    }

    void Update()
    {
        if (character == null || !character.gameObject.activeSelf) return;
        character.timeScale = paused ? 0 : speed;
        if (previewCamera != null) previewCamera.orthographicSize = baseSize / zoom;
        if (paused || !autoCycle || names.Length == 0) return;
        elapsed += Time.deltaTime * speed;
        float duration = character.Skeleton.Data.FindAnimation(names[selected]).Duration;
        if (elapsed >= Mathf.Max(1f, duration) + 0.4f) Play((selected + 1) % names.Length);
    }

    public void Play(int index)
    {
        if (index < 0 || index >= names.Length || character == null) return;
        selected = index;
        elapsed = 0;
        character.AnimationState.ClearTracks();
        character.Skeleton.SetToSetupPose();
        character.AnimationState.SetAnimation(0, names[index], loop && !autoCycle);
        character.Update(0);
        character.LateUpdate();
    }

    public void FitCamera()
    {
        if (previewCamera == null || character == null || !character.valid) return;
        var bounds = character.GetComponent<Renderer>().bounds;
        // Reserve the left panel in landscape, and the top panel in portrait.
        bool landscape = Screen.width >= Screen.height;
        float aspect = Mathf.Max(0.1f, previewCamera.aspect);
        float usableWidth = landscape ? 0.52f : 0.9f;
        float usableHeight = landscape ? 0.85f : 0.5f;
        baseSize = Mathf.Max(0.5f, Mathf.Max(bounds.size.y / usableHeight, bounds.size.x / (aspect * usableWidth)) * 0.6f);
        var center = bounds.center;
        center.x -= landscape ? baseSize * aspect * 0.42f : 0;
        center.y += landscape ? 0 : baseSize * 0.4f;
        previewCamera.transform.position = new Vector3(center.x, center.y, -10);
        previewCamera.orthographicSize = baseSize;
        zoom = 1;
    }

    void OnGUI()
    {
        float scale = Mathf.Max(0.5f, Mathf.Min(Screen.width / 720f, Screen.height / 720f));
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        bool landscape = Screen.width >= Screen.height;
        float width = landscape ? 310 : 696;
        float height = Mathf.Min(550, Screen.height / scale - 24);
        GUILayout.BeginArea(new Rect(12, 12, width, height), GUI.skin.box);
        GUILayout.Label("MONSTER ANIMATION TEST | " + monsters.Length + " monsters");
        search = GUILayout.TextField(search);
        monsterScroll = GUILayout.BeginScrollView(monsterScroll, GUILayout.Height(90));
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (GUILayout.Button((i == selectedMonster ? "> " : "") + monsters[i].name)) SelectMonster(i);
        }
        GUILayout.EndScrollView();
        if (!string.IsNullOrEmpty(error)) GUILayout.Label(error);
        else if (names.Length == 0) GUILayout.Label("No animations; showing setup pose.");
        else
        {
            GUILayout.Label(names[selected]);
            animationScroll = GUILayout.BeginScrollView(animationScroll, GUILayout.Height(100));
            int next = GUILayout.SelectionGrid(selected, names, landscape ? 2 : 3);
            if (next != selected) Play(next);
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            paused = GUILayout.Toggle(paused, "Pause");
            bool nextLoop = GUILayout.Toggle(loop, "Loop");
            bool nextAuto = GUILayout.Toggle(autoCycle, "Auto cycle");
            if (nextLoop != loop || nextAuto != autoCycle) { loop = nextLoop; autoCycle = nextAuto; Play(selected); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Restart")) Play(selected);
        }
        GUILayout.Label("Speed " + speed.ToString("0.00") + "x");
        speed = GUILayout.HorizontalSlider(speed, 0.1f, 2f);
        GUILayout.Label("Zoom " + zoom.ToString("0.00") + "x");
        zoom = GUILayout.HorizontalSlider(zoom, 0.3f, 2f);
        if (GUILayout.Button("Fit camera")) FitCamera();
        GUILayout.EndArea();
        GUI.matrix = previous;
    }
}
