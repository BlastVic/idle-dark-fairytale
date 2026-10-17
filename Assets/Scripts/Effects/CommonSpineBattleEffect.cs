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
        Destroy(gameObject,duration+.1f);
    }
    public void Sample(float time)
    {
        skeleton.timeScale=0;skeleton.Initialize(false);
        skeleton.AnimationState.ClearTracks();skeleton.Skeleton.SetToSetupPose();
        skeleton.AnimationState.SetAnimation(0,animationName,false).TrackTime=time+startTime;
        skeleton.AnimationState.Apply(skeleton.Skeleton);skeleton.Skeleton.UpdateWorldTransform();skeleton.LateUpdate();
    }
}
