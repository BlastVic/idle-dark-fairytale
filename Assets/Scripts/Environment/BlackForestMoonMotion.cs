using System;
using Spine.Unity;
using UnityEngine;

/// <summary>Chooses one complete gaze while the eyelid is closed; never switches color mid-gaze.</summary>
[RequireComponent(typeof(SkeletonAnimation))]
public sealed class BlackForestMoonMotion : MonoBehaviour
{
    SkeletonAnimation skeleton;
    System.Random random;
    public int NormalCycles { get; private set; }
    public int RedCycles { get; private set; }

    void Start() { if(skeleton==null)Initialize(Guid.NewGuid().GetHashCode()); }

    public void Initialize(int seed)
    {
        if(skeleton!=null)skeleton.AnimationState.Complete-=QueueNext;
        skeleton=GetComponent<SkeletonAnimation>();skeleton.Initialize(false);
        random=new System.Random(seed);NormalCycles=RedCycles=0;
        skeleton.AnimationState.Complete+=QueueNext;
        skeleton.AnimationState.SetAnimation(0,Choose(),false);
    }

    string Choose()
    {
        if(random.Next(2)==0) { NormalCycles++;return "GazeDown"; }
        RedCycles++;return "GazeDownRed";
    }

    void QueueNext(Spine.TrackEntry entry)
    {
        if(entry.TrackIndex!=0 || (entry.Animation.Name!="GazeDown" && entry.Animation.Name!="GazeDownRed"))return;
        skeleton.AnimationState.AddAnimation(0,Choose(),false,0);
    }

    void OnDestroy() { if(skeleton!=null)skeleton.AnimationState.Complete-=QueueNext; }
}
