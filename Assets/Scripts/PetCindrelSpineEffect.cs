using Spine.Unity;
using UnityEngine;

// A reusable, authored Spine effect. No gameplay damage is applied here.
public sealed class PetCindrelSpineEffect : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public Bounds motionBounds;
    public float duration;
    public void Play(float worldWidth, int sortingOrder, float speed = 1)
    {
        skeleton.Initialize(false);
        skeleton.AnimationState.SetAnimation(0,"texiao",false);
        skeleton.timeScale=speed;
        float fit=worldWidth/Mathf.Max(.01f,motionBounds.size.x);
        skeleton.transform.localScale=Vector3.one*fit;
        skeleton.transform.localPosition=-motionBounds.center*fit;
        var renderer=skeleton.GetComponent<MeshRenderer>();
        renderer.sortingLayerName="Character";renderer.sortingOrder=sortingOrder;
    }
}
