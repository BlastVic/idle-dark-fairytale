using Spine;
using Spine.Unity;
using UnityEngine;

public class DropIn : MonoBehaviour
{
    [Tooltip("Play spawn at the final position without falling.")]
    public bool spawnInPlace;
    public float howHigh = 10;
    public float origY = 0;
    public float moveSpeed = .1f;
    public bool isFalling = true;
    public enum EntrancePhase { Falling, Spawning, Ready }
    public EntrancePhase Phase { get; private set; } = EntrancePhase.Falling;
    public bool IsEntering => Phase != EntrancePhase.Ready;
    public bool UsesSpawn => spawnName != null;

    GameObject body, lifebar;
    Vector3 origScale, origScaleLifebar;
    SkeletonAnimation skeleton;
    Enemy enemy;
    TrackEntry entranceTrack;
    string spawnName;

    void OnEnable()
    {
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
        body.transform.position = new Vector3(body.transform.position.x, origY, body.transform.position.z);
        body.transform.localScale = origScale;
        if (lifebar) lifebar.transform.localScale = origScaleLifebar;
    }

    public void BeginDrop()
    {
        ReleaseTrack();
        Phase = EntrancePhase.Falling;
        isFalling = true;
        if (enemy) enemy.StopAttacking();
        origScaleLifebar = lifebar.transform.localScale;
        lifebar.transform.localScale = Vector3.zero;
        origY = body.transform.position.y;
        origScale = body.transform.localScale;
        spawnName = null;
        if (skeleton)
        {
            skeleton.Initialize(false);
            var data = skeleton.Skeleton.Data;
            spawnName = data.FindAnimation("spawn") != null ? "spawn" : data.FindAnimation("Spawn") != null ? "Spawn" : null;
        }
        if (spawnName != null) HoldSpawnPose();
        else body.transform.localScale = new Vector3(origScale.x * .8f, origScale.y, origScale.z);
        if (spawnInPlace && spawnName != null) return;
        body.transform.position += new Vector3(0, howHigh, 0);
    }

    // Enemy.Start applies its skin after OnEnable; reapply frame zero after that setup.
    public void HoldSpawnPose()
    {
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
        if (Phase != EntrancePhase.Falling) return;
        var position = body.transform.position;
        position.y = Mathf.Max(origY, position.y - moveSpeed);
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
        lifebar.transform.localScale = origScaleLifebar;
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
