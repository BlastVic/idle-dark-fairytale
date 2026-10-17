using Spine;
using Spine.Unity;
using UnityEngine;

public class DropIn : MonoBehaviour
{
    [Tooltip("Play spawn at the final position without falling.")]
    public bool spawnInPlace;
    [Tooltip("Run in from beyond the right camera edge instead of falling.")]
    public bool runFromRight;
    public string runAnimation = "Run_Battle_00";
    [Min(.1f)] public float runDuration = .6f;
    public float howHigh = 10;
    public float origY = 0;
    public float moveSpeed = .1f;
    public bool isFalling = true;
    public enum EntrancePhase { Falling, Spawning, Ready, Running }
    public EntrancePhase Phase { get; private set; } = EntrancePhase.Falling;
    public bool IsEntering => Phase != EntrancePhase.Ready;
    public bool UsesSpawn => spawnName != null;

    GameObject body, lifebar;
    Vector3 origScale, origScaleLifebar;
    SkeletonAnimation skeleton;
    Enemy enemy;
    TrackEntry entranceTrack;
    string spawnName;

    bool previewMode;
    Vector3 previewOffset;
    float origX, runDistance, runElapsed;
    Camera entranceCamera;
    Transform contactShadow;
    Vector3 shadowRest;

    // The animation workbench supplies layout and time; the production state machine is shared.
    public void InitializePreview(SkeletonAnimation actor, DropIn settings, Camera camera = null)
    {
        previewMode = true;
        skeleton = actor;
        body = actor.gameObject;
        spawnInPlace = settings.spawnInPlace;
        runFromRight = settings.runFromRight;
        runAnimation = settings.runAnimation;
        runDuration = settings.runDuration;
        entranceCamera = camera;
        howHigh = settings.howHigh;
        moveSpeed = settings.moveSpeed;
        skeleton.UpdateLocal += KeepEntranceVisible;
        BeginDrop();
        previewOffset = body.transform.position - new Vector3(origX, origY, body.transform.position.z);
    }

    public void ApplyPreview(float deltaTime)
    {
        // Called after the stage lays out the resting pose, including resizing and zoom.
        origY = body.transform.position.y;
        origX = body.transform.position.x;
        origScale = body.transform.localScale;
        body.transform.position += previewOffset;
        if (Phase == EntrancePhase.Running) AdvanceRun(deltaTime);
        if (Phase == EntrancePhase.Falling)
        {
            if (!UsesSpawn) body.transform.localScale = new Vector3(origScale.x * .8f, origScale.y, origScale.z);
            AdvanceFall(moveSpeed * deltaTime / Time.fixedDeltaTime);
        }
        previewOffset = body.transform.position - new Vector3(origX, origY, body.transform.position.z);
    }

    void OnEnable()
    {
        if (previewMode) return;
        enemy = GetComponent<Enemy>();
        skeleton = GetComponentInChildren<SkeletonAnimation>();
        if (skeleton) skeleton.UpdateLocal += KeepEntranceVisible;
        lifebar = transform.GetChild(0).gameObject;
        body = skeleton ? skeleton.gameObject : transform.GetChild(1).gameObject;
        BeginDrop();
    }

    public void ReplaceLifebar(GameObject replacement)
    {
        lifebar = replacement;
        origScaleLifebar = replacement.transform.localScale;
    }

    public void ResetForLayout()
    {
        body.transform.position = new Vector3(runFromRight ? origX : body.transform.position.x, origY, body.transform.position.z);
        body.transform.localScale = origScale;
        if (contactShadow) contactShadow.position = shadowRest;
        if (lifebar) lifebar.transform.localScale = origScaleLifebar;
    }

    public void BeginDrop()
    {
        ReleaseTrack();
        Phase = EntrancePhase.Falling;
        isFalling = true;
        if (enemy) enemy.StopAttacking();
        if (lifebar) { origScaleLifebar = lifebar.transform.localScale; lifebar.transform.localScale = Vector3.zero; }
        origY = body.transform.position.y;
        origX = body.transform.position.x;
        origScale = body.transform.localScale;
        contactShadow = previewMode ? null : transform.Find("Battle Contact Shadow");
        if (contactShadow) shadowRest = contactShadow.position;
        spawnName = null;
        if (skeleton)
        {
            skeleton.Initialize(false);
            var data = skeleton.Skeleton.Data;
            spawnName = data.FindAnimation("spawn") != null ? "spawn" : data.FindAnimation("Spawn") != null ? "Spawn" : null;
        }
        if (runFromRight && skeleton && skeleton.Skeleton.Data.FindAnimation(runAnimation) != null)
        {
            spawnName = null;
            Phase = EntrancePhase.Running;
            isFalling = false;
            runElapsed = 0;
            HoldSpawnPose();
            var camera = entranceCamera ? entranceCamera : Camera.main;
            var bounds = skeleton.GetComponent<MeshRenderer>().bounds;
            float depth = camera ? camera.WorldToViewportPoint(body.transform.position).z : 0;
            float right = camera ? camera.ViewportToWorldPoint(new Vector3(1.08f, .5f, depth)).x : bounds.max.x + 10;
            runDistance = Mathf.Max(.1f, right - bounds.min.x);
            SetRunOffset(runDistance);
            return;
        }
        if (spawnName != null) HoldSpawnPose();
        else body.transform.localScale = new Vector3(origScale.x * .8f, origScale.y, origScale.z);
        if (spawnInPlace && spawnName != null) return;
        body.transform.position += new Vector3(0, howHigh, 0);
    }

    // Enemy.Start applies its skin after OnEnable; reapply frame zero after that setup.
    public void HoldSpawnPose()
    {
        if (Phase == EntrancePhase.Running)
        {
            skeleton.AnimationState.ClearTracks();
            entranceTrack = skeleton.AnimationState.SetAnimation(0, runAnimation, true);
            entranceTrack.MixDuration = 0;
            skeleton.Update(0); skeleton.LateUpdate();
            if (enemy) enemy.currentAnimation = runAnimation;
            return;
        }
        if (!IsEntering || spawnName == null) return;
        ReleaseTrack();
        skeleton.AnimationState.ClearTracks();
        skeleton.Skeleton.SetToSetupPose();
        entranceTrack = skeleton.AnimationState.SetAnimation(0, spawnName, false);
        entranceTrack.MixDuration = 0;
        entranceTrack.TimeScale = spawnInPlace ? 1 : 0;
        if (spawnInPlace)
        {
            Phase = EntrancePhase.Spawning;
            isFalling = false;
            entranceTrack.Complete += SpawnComplete;
        }
        entranceTrack.TrackTime = 0;
        skeleton.Update(0);
        skeleton.LateUpdate();
        if (enemy) enemy.currentAnimation = spawnName;
    }

    void FixedUpdate()
    {
        if (!previewMode) { AdvanceRun(Time.fixedDeltaTime); AdvanceFall(moveSpeed); }
    }

    void SetRunOffset(float offset)
    {
        var p = body.transform.position;
        p.x = origX + offset; p.y = origY;
        body.transform.position = p;
        if (contactShadow) contactShadow.position = shadowRest + Vector3.right * offset;
    }

    void AdvanceRun(float deltaTime)
    {
        if (Phase != EntrancePhase.Running) return;
        runElapsed += deltaTime;
        float progress = Mathf.Clamp01(runElapsed / Mathf.Max(.1f, runDuration));
        SetRunOffset(runDistance * (1 - progress));
        if (progress >= 1) FinishEntrance();
    }

    void AdvanceFall(float distance)
    {
        if (Phase != EntrancePhase.Falling) return;
        var position = body.transform.position;
        position.y = Mathf.Max(origY, position.y - distance);
        body.transform.position = position;
        if (position.y > origY) return;
        isFalling = false;
        body.transform.localScale = origScale;
        if (spawnName == null) { FinishEntrance(); return; }
        Phase = EntrancePhase.Spawning;
        entranceTrack.TimeScale = 1;
        entranceTrack.Complete += SpawnComplete;
    }

    // The source spawn fades in from fully transparent. Preserve its first-frame
    // pose, but show it during the fall and avoid a transparency flash on landing.
    void KeepEntranceVisible(ISkeletonAnimation animated)
    {
        if (spawnInPlace || !IsEntering || spawnName == null || entranceTrack == null || entranceTrack.TrackTime > .0667f) return;
        foreach (var slot in skeleton.Skeleton.Slots)
            if (slot.Attachment != null) slot.A = slot.Data.A;
    }

    void SpawnComplete(TrackEntry entry)
    {
        if (Phase == EntrancePhase.Spawning && entry == entranceTrack) FinishEntrance();
    }

    void FinishEntrance()
    {
        ReleaseTrack();
        Phase = EntrancePhase.Ready;
        isFalling = false;
        if (lifebar) lifebar.transform.localScale = origScaleLifebar;
        if (!enemy) return;
        enemy.currentAnimation = "";
        enemy.SetAnimation("Idle1", true, false);
        enemy.StartAttacking(Random.Range(0, 100) < 50);
    }

    void ReleaseTrack()
    {
        if (entranceTrack != null) entranceTrack.Complete -= SpawnComplete;
        entranceTrack = null;
    }

    void OnDisable()
    {
        if (skeleton) skeleton.UpdateLocal -= KeepEntranceVisible;
        ReleaseTrack();
        if (body) ResetForLayout();
        if (enemy) enemy.StopAttacking();
    }
}
