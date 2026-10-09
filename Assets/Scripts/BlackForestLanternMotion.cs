using System;
using Spine.Unity;
using UnityEngine;

/// <summary>Independent candle timing on top of the Spine sway, without changing gameplay RNG.</summary>
[RequireComponent(typeof(SkeletonAnimation))]
public sealed class BlackForestLanternMotion : MonoBehaviour
{
    SkeletonAnimation skeleton;
    System.Random random;
    Spine.Slot glow, lantern;
    float nextFlicker, pulseStart = -10, pulseDuration, depth, phase, previousTime;
    bool doubleDip;
    public int FlickerCount { get; private set; }

    void Start() { if (skeleton == null) Initialize(Guid.NewGuid().GetHashCode()); }

    public void Initialize(int seed)
    {
        if (skeleton != null) skeleton.UpdateLocal -= Apply;
        skeleton = GetComponent<SkeletonAnimation>();
        skeleton.Initialize(false);
        random = new System.Random(seed);
        glow = skeleton.Skeleton.FindSlot("glow");
        lantern = skeleton.Skeleton.FindSlot("lantern");
        var track = skeleton.AnimationState.GetCurrent(0);
        track.TimeScale = Range(.86f,1.22f);
        track.TrackTime = Range(0,16.8f);
        phase = Range(0,Mathf.PI*2);
        previousTime = track.TrackTime;
        nextFlicker = previousTime + Range(.5f,3.5f);
        skeleton.UpdateLocal += Apply;
    }

    float Range(float min,float max) { return min + (float)random.NextDouble()*(max-min); }

    void Apply(ISkeletonAnimation animated)
    {
        var track=skeleton.AnimationState.GetCurrent(0);
        if(track==null)return;
        float time=track.TrackTime;
        if(time<previousTime) { nextFlicker=time+Range(.5f,3.5f);pulseStart=-10; }
        previousTime=time;
        if(time>=nextFlicker) {
            pulseStart=time;pulseDuration=Range(.28f,.62f);depth=Range(.48f,.78f);
            doubleDip=random.NextDouble()<.5;
            nextFlicker=time+pulseDuration+Range(1.8f,5.5f);
            FlickerCount++;
        }
        float pulse=Mathf.Clamp01((time-pulseStart)/Mathf.Max(.01f,pulseDuration));
        float dip=pulse<1 ? Mathf.Pow(Mathf.Abs(Mathf.Sin(pulse*Mathf.PI*(doubleDip?2:1))),1.4f)*depth : 0;
        float breath=.5f+.32f*Mathf.Sin(time*1.73f+phase)+.18f*Mathf.Sin(time*3.19f+phase*.7f);
        glow.A=Mathf.Clamp((.24f+.34f*breath)*(1-dip),.045f,.64f);
        float light=(.88f+.12f*breath)*(1-dip*.65f);
        lantern.R=light;lantern.G=light;lantern.B=light;
    }

    void OnDestroy() { if(skeleton!=null)skeleton.UpdateLocal-=Apply; }
}
