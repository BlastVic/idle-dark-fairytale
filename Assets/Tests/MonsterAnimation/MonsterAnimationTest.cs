using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>Animation data and preview only; all controls live in the authored UGUI prefab.</summary>
public class MonsterAnimationTest : MonoBehaviour
{
    public SkeletonAnimation character;
    public Camera previewCamera;
    public SkeletonDataAsset[] monsters = new SkeletonDataAsset[0];
    public GameObject[] monsterPrefabs = new GameObject[0];
    public string[] discoveryFolders = { "Assets/Tests" };
    public MonsterPreviewHud hud;
    public MonsterPreviewStage stage;
    public bool loop = true;
    public bool autoCycle;
    public Bounds CombatSetupBounds { get; private set; }
    public Vector3 CombatDisplayScale { get; private set; } = Vector3.one;
    public bool CombatIsBoss { get; private set; }
    public float CombatSizeMultiplier { get; private set; } = 1f;
    [Range(.1f, 2f)] public float speed = 1;
    public string[] AnimationNames { get; private set; } = new string[0];
    public int SelectedMonster { get; private set; } = -1;
    public int SelectedAnimation { get; private set; }
    public bool Paused { get; private set; }
    public string Error { get; private set; }
    public float Zoom { get; private set; } = 1;
    float elapsed;
    public event Action Changed;

    public void RefreshCatalog()
    {
        var found = new List<SkeletonDataAsset>();
        foreach (var data in monsters ?? new SkeletonDataAsset[0])
            if (data && !found.Contains(data)) found.Add(data);
        if (character && character.skeletonDataAsset && !found.Contains(character.skeletonDataAsset)) found.Add(character.skeletonDataAsset);
#if UNITY_EDITOR
        var folders = new List<string>();
        foreach (var folder in discoveryFolders ?? new string[0])
            if (!string.IsNullOrEmpty(folder) && UnityEditor.AssetDatabase.IsValidFolder(folder)) folders.Add(folder);
        if (folders.Count > 0)
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:SkeletonDataAsset", folders.ToArray()))
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (data && !found.Contains(data)) found.Add(data);
            }
#endif
        found.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        monsters = found.ToArray();
    }

    void Start()
    {
        var initial = character ? character.skeletonDataAsset : null;
        RefreshCatalog();
        if (hud) hud.Bind(this);
        if (monsters.Length > 0) SelectMonster(Mathf.Max(0, Array.IndexOf(monsters, initial)));
        else { Error = "尚无怪物，请在编辑器刷新怪物目录。"; Changed?.Invoke(); }
    }

    public void SelectMonster(int index)
    {
        if (index < 0 || index >= monsters.Length) return;
        SelectedMonster = index;
        CombatDisplayScale = Vector3.one;
        CombatIsBoss = false;
        CombatSizeMultiplier = 1f;
        AnimationNames = new string[0]; SelectedAnimation = 0; elapsed = 0; Error = null;
        if (character) { character.gameObject.SetActive(false); Destroy(character.gameObject); }
        try
        {
            var data = monsters[index];
            if (!data || data.GetSkeletonData(false) == null) throw new Exception("骨架数据无法加载");
            character = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            character.name = data.name;
            character.transform.SetParent(transform, false);
            foreach (var prefab in monsterPrefabs ?? new GameObject[0])
            {
                var display = prefab ? prefab.GetComponentInChildren<SkeletonAnimation>(true) : null;
                if (display && display.skeletonDataAsset == data) { CombatDisplayScale = display.transform.lossyScale;
                    var lifebar = prefab.GetComponentInChildren<EnemyLifebar>(true);
                    CombatIsBoss = lifebar && (lifebar.isBoss || lifebar.isMiniBoss);
                    var enemy = prefab.GetComponent<Enemy>();
                    CombatSizeMultiplier = enemy ? Mathf.Max(.1f, enemy.displaySizeMultiplier) : 1f;
                    character.transform.localScale = CombatDisplayScale;
                    if (!string.IsNullOrEmpty(display.initialSkinName))
                    {
                        character.Skeleton.SetSkin(display.initialSkinName);
                        character.Skeleton.SetSlotsToSetupPose();
                    }
                    var blood = display.GetComponent<MonsterIdleBlood>();
                    if (blood) { var previewBlood = character.gameObject.AddComponent<MonsterIdleBlood>(); previewBlood.skeleton = character; previewBlood.CopySettingsFrom(blood); }
                    break; }
            }
            character.Update(0); character.LateUpdate();
            CombatSetupBounds = character.GetComponent<MeshFilter>().sharedMesh.bounds;
            var renderer = character.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Default"; renderer.sortingOrder = 10;
            character.timeScale = Paused ? 0 : speed;
            var animations = character.Skeleton.Data.Animations;
            var ordered = new List<string>();
            foreach (var key in new[] { "Idle1", "Attack1", "Hit", "Death1" })
                if (character.Skeleton.Data.FindAnimation(key) != null) ordered.Add(key);
            foreach (var anim in animations) if (!ordered.Contains(anim.Name)) ordered.Add(anim.Name);
            AnimationNames = ordered.ToArray();
            int idle = Array.FindIndex(AnimationNames, n => n == "Idle1" || string.Equals(n, "idle", StringComparison.OrdinalIgnoreCase));
            if (AnimationNames.Length > 0) Play(Mathf.Max(0, idle));
            else { character.Update(0); character.LateUpdate(); }
            FitCamera();
        }
        catch (Exception ex)
        {
            AnimationNames = new string[0];
            if (character) character.gameObject.SetActive(false);
            Error = "无法预览 " + (monsters[index] ? monsters[index].name : "空资源") + "：" + ex.Message;
            Debug.LogWarning(Error, this);
        }
        Changed?.Invoke();
    }

    void Update()
    {
        if (!character || !character.gameObject.activeSelf) return;
        character.timeScale = Paused ? 0 : speed;
        if (Paused || !autoCycle || AnimationNames.Length == 0) return;
        elapsed += Time.deltaTime * speed;
        float duration = character.Skeleton.Data.FindAnimation(AnimationNames[SelectedAnimation]).Duration;
        if (elapsed >= Mathf.Max(1f, duration) + .4f) Play((SelectedAnimation + 1) % AnimationNames.Length);
    }

    public void Play(int index)
    {
        if (index < 0 || index >= AnimationNames.Length || !character) return;
        SelectedAnimation = index; elapsed = 0;
        character.AnimationState.ClearTracks(); character.Skeleton.SetToSetupPose();
        character.AnimationState.SetAnimation(0, AnimationNames[index], loop && !autoCycle);
        character.Update(0); character.LateUpdate();
        Changed?.Invoke();
    }
    public void SetPaused(bool value) { Paused = value; Changed?.Invoke(); }
    public void SetLoop(bool value) { loop = value; Play(SelectedAnimation); }
    public void SetAutoCycle(bool value) { autoCycle = value; Play(SelectedAnimation); }
    public void SetSpeed(float value) { speed = Mathf.Clamp(value, .1f, 2); Changed?.Invoke(); }
    public void SetZoom(float value) { Zoom = Mathf.Clamp(value, .3f, 2); if (stage) stage.Apply(); Changed?.Invoke(); }
    public void FitCamera() { Zoom = 1; if (stage) { stage.MeasureMonster(); stage.Apply(); } Changed?.Invoke(); }
}
