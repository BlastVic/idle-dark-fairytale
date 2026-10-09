using Spine.Unity;
using UnityEngine;

public class Mon7070AnimationTest : MonoBehaviour
{
    public SkeletonAnimation character;
    public Camera previewCamera;
    public bool loop = true;
    public bool autoCycle;
    [Range(0.1f, 2f)] public float speed = 1f;
    string[] names;
    int selected;
    bool paused;
    float elapsed;
    float baseSize;
    float zoom = 1;

    void Start()
    {
        character.Initialize(false);
        baseSize = previewCamera.orthographicSize;
        var animations = character.Skeleton.Data.Animations;
        names = new string[animations.Count];
        for (int i = 0; i < names.Length; i++) names[i] = animations.Items[i].Name;
        selected = System.Array.IndexOf(names, "idle");
        Play(Mathf.Max(0, selected));
    }

    void Update()
    {
        character.timeScale = paused ? 0 : speed;
        previewCamera.orthographicSize = baseSize / zoom;
        if (paused || !autoCycle || names == null) return;
        elapsed += Time.deltaTime * speed;
        float duration = character.Skeleton.Data.FindAnimation(names[selected]).Duration;
        if (elapsed >= Mathf.Max(1f, duration) + 0.4f) Play((selected + 1) % names.Length);
    }

    void Play(int index)
    {
        selected = index;
        elapsed = 0;
        character.AnimationState.ClearTracks();
        character.Skeleton.SetToSetupPose();
        character.AnimationState.SetAnimation(0, names[index], loop && !autoCycle);
        character.Update(0);
    }

    void OnGUI()
    {
        if (names == null) return;
        float scale = Mathf.Max(0.5f, Screen.width / 720f);
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUILayout.BeginArea(new Rect(12, 12, 696, 230), GUI.skin.box);
        GUILayout.Label("MON 7070 | Animation Test | " + names[selected]);
        int next = GUILayout.SelectionGrid(selected, names, 3, GUILayout.Height(90));
        if (next != selected) Play(next);
        GUILayout.BeginHorizontal();
        paused = GUILayout.Toggle(paused, "Pause");
        bool nextLoop = GUILayout.Toggle(loop, "Loop");
        bool nextAuto = GUILayout.Toggle(autoCycle, "Auto cycle (all 9)");
        if (nextLoop != loop || nextAuto != autoCycle)
        {
            loop = nextLoop;
            autoCycle = nextAuto;
            Play(selected);
        }
        if (GUILayout.Button("Restart")) Play(selected);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Speed " + speed.ToString("0.00") + "x", GUILayout.Width(100));
        speed = GUILayout.HorizontalSlider(speed, 0.1f, 2f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Zoom " + zoom.ToString("0.00") + "x", GUILayout.Width(100));
        zoom = GUILayout.HorizontalSlider(zoom, 0.3f, 2f);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUI.matrix = previous;
    }
}
