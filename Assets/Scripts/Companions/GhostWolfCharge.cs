using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>Presentation and crossing state only; damage is owned by GhostWolfPack.</summary>
public sealed class GhostWolfCharge : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    [Range(0,1)] public float laneViewportY = .32f;
    [Min(0)] public float launchDelay;
    public Vector2 viewportSize = new Vector2(.432f, .144f);
    public string runAnimation = "Run_Battle_00";
    public string impactBone = "Front_Mouth_01";
    public bool Started { get; private set; }
    public bool Finished { get; private set; }
    public bool HasImpacted { get; private set; }
    Bounds setupBounds;
    Vector3 authoredScale;
    Bone muzzle;
    MeshRenderer body;

    public void Initialize(Camera camera, float startX)
    {
        gameObject.SetActive(true);
        skeleton.Initialize(false);
        skeleton.AnimationState.ClearTracks();skeleton.Skeleton.SetToSetupPose();
        skeleton.Update(0);skeleton.LateUpdate();
        authoredScale = skeleton.transform.localScale;
        setupBounds = skeleton.GetComponent<MeshFilter>().sharedMesh.bounds;
        muzzle = skeleton.Skeleton.FindBone(impactBone);
        body = skeleton.GetComponent<MeshRenderer>();
        body.sortingLayerName = "Character";
        body.sortingOrder = Mathf.RoundToInt((1-laneViewportY)*100)+1;
        Started = Finished = HasImpacted = false;
        Place(camera, startX);
        gameObject.SetActive(false);
    }

    public void Advance(Camera camera, float elapsed, float startX, float speed)
    {
        if (Finished || elapsed < launchDelay) return;
        if (!Started) {
            Started = true;gameObject.SetActive(true);
            skeleton.AnimationState.SetAnimation(0, runAnimation, true);
            skeleton.Update(0);skeleton.LateUpdate();
        }
        Place(camera, startX+(elapsed-launchDelay)*speed);
        // Remove only once the entire rendered silhouette has left the right edge.
        if (camera.WorldToViewportPoint(body.bounds.min).x > 1.03f) {
            Finished = true;gameObject.SetActive(false);
        }
    }

    void Place(Camera camera, float x)
    {
        float scale=DarkFairytaleBattleStyle.DisplayFitScale(Vector3.Scale(setupBounds.size,authoredScale),
            DarkFairytaleBattleStyle.WorldSize(camera),viewportSize);
        transform.localScale=Vector3.one*scale;
        var feet=DarkFairytaleBattleStyle.ViewportPoint(camera,new Vector2(x,laneViewportY));
        transform.position=feet-new Vector3(setupBounds.center.x*authoredScale.x*scale,setupBounds.min.y*authoredScale.y*scale,0);
    }
    public float MuzzleViewportX(Camera camera)
    {
        var point=muzzle!=null?skeleton.transform.TransformPoint(new Vector3(muzzle.WorldX,muzzle.WorldY,0)):
            new Vector3(body.bounds.max.x,body.bounds.center.y,body.bounds.center.z);
        return camera.WorldToViewportPoint(point).x;
    }
    public void MarkImpact(){HasImpacted=true;}
}
