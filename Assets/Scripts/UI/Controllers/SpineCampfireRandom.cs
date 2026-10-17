using Spine.Unity;
using UnityEngine;

/// <summary>Three authored clips, sampled without immediate repeats; no runtime UI creation.</summary>
[RequireComponent(typeof(SkeletonGraphic))]
public sealed class SpineCampfireRandom : MonoBehaviour
{
    public string[] clips={"BurnA","BurnB","BurnC"};
    public float mixDuration=.18f;
    SkeletonGraphic graphic;
    Spine.TrackEntry entry;
    int previous=-1;
    public int Transitions {get;private set;}
    public int PlayedMask {get;private set;}
    public string CurrentClip => entry?.Animation.Name;
    void OnEnable(){graphic=GetComponent<SkeletonGraphic>();graphic.Initialize(false);previous=-1;entry=null;}
    void Update()
    {
        if(!graphic || !graphic.IsValid || clips==null || clips.Length==0)return;
        if(entry!=null && entry.TrackTime<entry.AnimationEnd)return;
        int next=clips.Length==1?0:Random.Range(0,previous<0?clips.Length:clips.Length-1);
        if(previous>=0 && clips.Length>1 && next>=previous)next++;
        if(graphic.Skeleton.Data.FindAnimation(clips[next])==null){enabled=false;Debug.LogError("Missing campfire animation "+clips[next],this);return;}
        entry=graphic.AnimationState.SetAnimation(0,clips[next],false);entry.MixDuration=mixDuration;
        previous=next;Transitions++;PlayedMask|=1<<next;
    }
    void OnDisable(){entry=null;}
}
