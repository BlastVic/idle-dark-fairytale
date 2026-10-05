using UnityEngine;
using System.Collections;
using Spine.Unity;
using Spine;

public class AnimationController : MonoBehaviour
{
    //SET, BLEND, RECEIVE EVENTS - FROM ANIMATION
    public SkeletonAnimation skeletonAnimation;
    public void Start()
    {
        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        skeletonAnimation.state.Event += HandleEvent;
    }


    void HandleEvent(Spine.TrackEntry entry, Spine.Event e)
    {
        //Debug.Log("Handle Event:" + e.data.name);
        if (entry.TrackIndex == 0)
        {
            if (e.Data.Name == "OnComplete")
            {
                string animationName = skeletonAnimation.state.GetCurrent(0).Animation.Name;
                AnimationComplete(0, animationName);
            }
        }
        if (entry.TrackIndex == 1)
        {
            if (e.Data.Name == "OnComplete")
            {
                string animationName = skeletonAnimation.state.GetCurrent(1).Animation.Name;
                AnimationComplete(1, animationName);
            }
        }

        if (e.Data.Name == "OnHit")
        {
            string animationName = skeletonAnimation.state.GetCurrent(0).Animation.Name;
            AnimationOnHit(animationName);
        }
        //if (e.Data.Name == "OnHit")
        //{
        //    string animationName = skeletonAnimation.state.GetCurrent(1).Animation.Name;
        //    AnimationOnHit_TrackOne(animationName);
        //}
        if (e.Data.Name == "OnSkill")
        {
            string animationName = skeletonAnimation.state.GetCurrent(0).Animation.Name;
            AnimationOnSkill(animationName);
        }
    }

    public void SetLoop(bool b)
    {
        skeletonAnimation.loop = b;
    }


    public int GetMaxAnimations(string anims)
    {
        int count = 0;
        //if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        //string[] animations = new string[skeletonAnimation.skeleton.Data.Animations.Count + 1];
        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name.Contains(anims)) count++;
        }
        return count;
    }

    public int GetMaxAttacks()
    {
        int count = 0;
        //string[] animations = new string[skeletonAnimation.skeleton.Data.Animations.Count + 1];
        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name.Contains("attack") || s.Name.Contains("Attack")) count++;
        }
        return count + 1;
    }

    public int GetMaxDeaths()
    {
        int count = 0;
        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name.Contains("Death")) count++;
        }
        return count;
    }

    public int GetMaxIdles()
    {
        int count = 0;
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name.Contains("Idle")) count++;
        }
        return count;
    }


    public int GetMaxRuns()
    {
        int count = 0;
        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name.Contains("Run")) count++;
        }
        return count;
    }

    public bool HasThisAnimation(string _desiredAnimation)
    {
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        bool b = false;
        //string[] animations = new string[skeletonAnimation.skeleton.Data.Animations.Count + 1];
        Spine.ExposedList<Spine.Animation> anim = skeletonAnimation.skeleton.Data.Animations;
        foreach (Spine.Animation s in anim)
        {
            if (s.Name == _desiredAnimation) return true;
        }
        return false;
    }

    public virtual void SetAnimationOnTrack(int trackIndex, string animation, bool isLoop, bool isOnComplete)
    {
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.state.SetAnimation(trackIndex, animation, isLoop);
    }

    public virtual void SetAnimation(string animation, bool isLoop, bool isOnComplete, float speed=1)
    {
        // print("animation:" + animation.ToString());
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null) return;
        if (skeletonAnimation.state == null) return;
        skeletonAnimation.loop = isLoop;
        skeletonAnimation.state.SetAnimation(0, animation, isLoop);
        //SetAnimationOccurred(animation);

        var trackEntry = skeletonAnimation.state.SetAnimation(0, animation, isLoop);//.timeScale = speed;
        if (animation.Contains("Attack"))
        {
            //Debug.Log("Attack Speed:" + speed);
        }
        trackEntry.TimeScale = speed;
    }

    public virtual void ForceAnimation(string animation, bool isLoop)
    {
        // print("animation:" + animation.ToString());
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        skeletonAnimation.loop = isLoop;
        skeletonAnimation.state.SetAnimation(0, animation, isLoop);
        SetAnimationOccurred(animation);
    }


    public virtual void ForceAnimation(string animation, bool isLoop, bool isOnComplete, bool ignoreRules)
    {
        // print("animation:" + animation.ToString());
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        skeletonAnimation.loop = isLoop;
        skeletonAnimation.state.SetAnimation(0, animation, isLoop);
        SetAnimationOccurred(animation);
    }

    public virtual void SetAnimationTown(string animation, bool isLoop, bool isOnComplete)
    {
        // print("animation:" + animation.ToString());
        if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

        skeletonAnimation.loop = isLoop;
        skeletonAnimation.state.SetAnimation(0, animation, isLoop);
        SetAnimationOccurred(animation);
    }

    ////ATTACKS HAPPEN HERE
    //public virtual void SetAnimationTrackOne(string animation, bool isLoop)
    //{
    //    // print("animation:" + animation.ToString());
    //    if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();

    //    skeletonAnimation.loop = isLoop;
    //    skeletonAnimation.state.SetAnimation(1, animation, isLoop);
    //    SetAnimationOccurred(animation);
    //}

    public virtual void SetAnimationOccurred(string animation)
    {
    }
    public virtual void AnimationComplete(int trackIndex, string animationName)
    {
    }
    public virtual void AnimationOnHit(string animation)
    {
    }
    public virtual void AnimationOnHit_TrackOne(string animation)
    {
    }
    public virtual void AnimationOnGunHit(string animation)
    {
    }
    public virtual void AnimationOnSkill(string animation)
    {
    }


}
