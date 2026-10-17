using UnityEngine;
using Spine.Unity;

// The authored transform is shared by gameplay and the workbench; no bounds fitting.
public sealed class CommonSpineBattleEffect : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public float duration;
    public Bounds motionBounds;
    public bool preview;
    public bool targetEnemy;
    public bool targetEnemyGroup;
    public bool centerOnBody;
    public string animationName="texiao";
    public float startTime;
    public bool keepSpawnPosition;
    public AudioClip spawnSound;
    [Tooltip("Chengpu sweep: replace the original returning arc with a horizontal flight off the right edge.")]
    public bool horizontalSweep;
    Camera sweepCamera;
    void OnEnable() { if (skeleton) skeleton.UpdateLocal += ApplySweepMotion; }
    void OnDisable() { if (skeleton) skeleton.UpdateLocal -= ApplySweepMotion; }
    void ApplySweepMotion(ISkeletonAnimation animated)
    {
        if (!horizontalSweep) return;
        var entry = skeleton.AnimationState.GetCurrent(0);
        if (entry != null) ApplySweepMotion(entry.AnimationTime);
    }
    void ApplySweepMotion(float time)
    {
        if (!horizontalSweep || time < .6333f) return;
        if (!sweepCamera) {
            var level = FindFirstObjectByType<LevelController>();
            var assets = level ? level.AssetManager : null;
            sweepCamera = assets && assets.battleCampCamera ? assets.battleCampCamera.GetComponent<Camera>() : Camera.main;
        }
        if (!sweepCamera) return;
        var bone = skeleton.Skeleton.FindBone("xg1");
        if (bone == null) return;
        skeleton.Skeleton.UpdateWorldTransform();
        float scale = skeleton.skeletonDataAsset.scale;
        float startX = 170.36f * scale, y = 7.87f * scale;
        var right = DarkFairytaleBattleStyle.ViewportPoint(sweepCamera, new Vector2(1, .5f));
        // Include the full authored effect radius, so it leaves before its fade begins.
        right.x += motionBounds.extents.x;
        var local = skeleton.transform.InverseTransformPoint(right);
        bone.Parent.WorldToLocal(local.x, local.y, out float endX, out _);
        bone.X = Mathf.LerpUnclamped(startX, Mathf.Max(startX, endX), Mathf.InverseLerp(.6333f, 1.3f, time));
        bone.Y = y;
    }
    public Vector3 PositionFor(Bounds body)=>new Vector3(body.center.x,(targetEnemyGroup||centerOnBody)?body.center.y:body.min.y,0);
    public void AlignToTarget()
    {
        Renderer body=null;
        if(targetEnemy){
            float nearest=float.PositiveInfinity;
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){
                if(!enemy.skeletonAnimation||!enemy.ab||(!centerOnBody&&enemy.ab.isDying))continue;
                var anchor=centerOnBody&&enemy.rootPos?enemy.rootPos.transform.position:enemy.transform.position;
                float distance=(anchor-transform.position).sqrMagnitude;
                if(distance<nearest){nearest=distance;body=enemy.skeletonAnimation.GetComponent<Renderer>();}
            }
        }else if(Player.single&&Player.single.skeletonAnimation)body=Player.single.skeletonAnimation.GetComponent<Renderer>();
        if(!body)return;
        var bounds=body.bounds;
        int order=body.sortingOrder;
        if(targetEnemyGroup){
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){
                if(!enemy.skeletonAnimation||!enemy.ab||(!centerOnBody&&enemy.ab.isDying))continue;
                var other=enemy.skeletonAnimation.GetComponent<Renderer>();
                bounds.Encapsulate(other.bounds);order=Mathf.Max(order,other.sortingOrder);
            }
        }
        transform.position=PositionFor(bounds);
        var renderer=skeleton.GetComponent<Renderer>();
        renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=order+(targetEnemy?1:0);
    }
    void Start()
    {
        if(preview)return;
        skeleton.Initialize(false);
        skeleton.AnimationState.SetAnimation(0,animationName,false).TrackTime=startTime;
        if(!keepSpawnPosition)AlignToTarget();
        if(spawnSound)SkillTimedAudio.Play(transform,spawnSound,skeleton.timeScale);
        Destroy(gameObject,duration+.1f);
    }
    public void Sample(float time)
    {
        skeleton.timeScale=0;skeleton.Initialize(false);
        skeleton.AnimationState.ClearTracks();skeleton.Skeleton.SetToSetupPose();
        skeleton.AnimationState.SetAnimation(0,animationName,false).TrackTime=time+startTime;
        skeleton.AnimationState.Apply(skeleton.Skeleton);skeleton.Skeleton.UpdateWorldTransform();skeleton.LateUpdate();
        ApplySweepMotion(time+startTime);skeleton.Skeleton.UpdateWorldTransform();skeleton.LateUpdate();
    }
}
