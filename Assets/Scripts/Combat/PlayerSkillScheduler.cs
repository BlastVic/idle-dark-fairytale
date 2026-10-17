using System;
using System.Collections.Generic;
using Scripts.Skills;
using Spine;
using UnityEngine;

/// <summary>FIFO, one active cast and at most one outstanding request per skill.</summary>
public sealed class PlayerSkillScheduler
{
    readonly Queue<SkillsTypeEnum> pending = new Queue<SkillsTypeEnum>();
    readonly HashSet<SkillsTypeEnum> outstanding = new HashSet<SkillsTypeEnum>();
    public SkillsTypeEnum Active { get; private set; }
    public int PendingCount => pending.Count;
    public float Compensation { get; private set; }
    bool hitConsumed;

    public static string AnimationFor(SkillsTypeEnum skill)
    {
        switch (skill) {
            case SkillsTypeEnum.Sinister_Stampede: return "Skill5";
            case SkillsTypeEnum.Boss_Killer: return "Skill7";
            case SkillsTypeEnum.GhostWolfPack: return "Skill2";
            case SkillsTypeEnum.Lightning_Demi_God: return "Skill3";
            default: return null;
        }
    }
    public bool Contains(SkillsTypeEnum skill) => outstanding.Contains(skill);
    public bool Enqueue(SkillsTypeEnum skill)
    {
        if (AnimationFor(skill) == null || !outstanding.Add(skill)) return false;
        pending.Enqueue(skill); return true;
    }
    public bool TryBegin(Func<SkillsTypeEnum, bool> eligible, out SkillsTypeEnum skill)
    {
        skill = SkillsTypeEnum.NONE;
        if (Active != SkillsTypeEnum.NONE) return false;
        while (pending.Count > 0) {
            var candidate = pending.Dequeue();
            if (!eligible(candidate)) { outstanding.Remove(candidate); continue; }
            Active = skill = candidate; hitConsumed = false; return true;
        }
        return false;
    }
    public void SetCompensation(float damage) => Compensation = Mathf.Max(0, damage);
    public bool ConsumeHit(string animation)
    {
        if (Active == SkillsTypeEnum.NONE || AnimationFor(Active) != animation || hitConsumed) return false;
        hitConsumed = true; return true;
    }
    public void Complete()
    {
        outstanding.Remove(Active); Active = SkillsTypeEnum.NONE; Compensation = 0; hitConsumed = false;
    }
    public void Clear()
    {
        pending.Clear(); outstanding.Clear(); Active = SkillsTypeEnum.NONE; Compensation = 0; hitConsumed = false;
    }
    public static float OpportunityAttacks(SkeletonData skeleton, string animation, float speed, float recovery)
    {
        speed = Mathf.Max(.01f, speed);
        float normal = (skeleton.FindAnimation("Attack1").Duration + skeleton.FindAnimation("Attack2").Duration) * .5f;
        return (skeleton.FindAnimation(animation).Duration / speed + recovery) / (normal / speed + recovery);
    }
}
