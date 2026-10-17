namespace Scripts.Skills
{
    // Fixed trigger rules; the serialized value tables contain effect strength only.
    public static class SkillDesign
    {
        public const float SweepChance=.20f, BossChance=.20f, ReviveChance=.25f, KillHealChance=.30f, ExperienceChance=.30f, WardChance=1f;
        public const float LightningInterval=6f;
        public const string CastNote="施法依次执行，并额外补偿占用的普攻伤害。";
        public static readonly string[] Names={"","生命复苏","召唤：影狼","回旋镰斩","荆棘庇护","猩红契约","余烬复生","血色收割","召唤：幽灵狼群","召唤：书脊龙","遗忘馈赠","雷霆裁决","猎王镰击","星辉护佑","以寡敌众","兽群领袖"};
        public static readonly string[] Descriptions={"",
            "每2秒恢复最大生命的一定比例。",
            "召唤影狼持续攻击敌人，伤害基于角色攻击力。",
            "每次攻击时有20%概率挥出旋转镰刃，伤害所有敌人。\n"+CastNote,
            "永久提升最大生命与防御。",
            "永久提升攻击力与攻击速度。",
            "阵亡时有25%概率复生。\n恢复最大生命的一定比例。",
            "击败敌人时有30%概率恢复最大生命的一定比例。",
            "每第3次攻击召唤5头幽灵狼。各自冲至敌人前方时，随机伤害1名敌人，可重复命中。\n"+CastNote,
            "召唤书脊龙持续攻击敌人，伤害基于角色攻击力。",
            "击败敌人时有30%概率获得额外经验，按该敌人的基础经验计算。",
            "每6秒尝试对生命低于50%的敌人施放落雷，举镰后命中。施法结束后重新计时。\n"+CastNote,
            "攻击首领时有20%概率发动强力镰击，伤害1名首领。\n"+CastNote,
            "受到暴击时获得护盾，可吸收伤害。再次触发只补满护盾，不叠加。",
            "场上有至少2名敌人时，提升普通攻击和猎王镰击伤害。",
            "提升影狼、幽灵狼群与书脊龙的伤害。"};
        public static readonly string[][] Labels={null,new[]{"每次恢复生命"},new[]{"每击攻击伤害"},new[]{"每目标基础伤害"},new[]{"生命提升","防御提升"},new[]{"攻击提升","攻速提升"},new[]{"复生恢复生命"},new[]{"恢复生命"},new[]{"每头基础伤害"},new[]{"每击攻击伤害"},new[]{"额外经验"},new[]{"每目标基础伤害"},new[]{"基础镰击伤害"},new[]{"护盾生命比例"},new[]{"伤害提升"},new[]{"召唤伤害提升"}};
    }
}
