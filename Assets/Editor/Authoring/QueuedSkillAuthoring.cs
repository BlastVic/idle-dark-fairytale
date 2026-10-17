using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

public static class QueuedSkillAuthoring
{
    public const string CompensationDescription = "施法按序执行，完整收招后衔接。\n额外补偿施法耗时内的普攻伤害，分摊至各次命中。";
    public static readonly string SweepDescription = SkillDesign.Descriptions[3];
    public static readonly string WolvesDescription = SkillDesign.Descriptions[8];
    public static readonly string LightningDescription = SkillDesign.Descriptions[11];

    [MenuItem("Tools/Dark Fairytale/Skills/Apply Queued Skill Descriptions")]
    public static void Apply()
    {
        var root = PrefabUtility.LoadPrefabContents(StorybookSkillsBuilder.Path);
        try {
            var nodes = root.GetComponentsInChildren<StorybookSkillNode>(true);
            nodes.Single(n => n.key == SkillsTypeEnum.Sinister_Stampede).descriptionTemplate = SweepDescription;
            var wolves = nodes.Single(n => n.key == SkillsTypeEnum.GhostWolfPack);
            wolves.descriptionTemplate = WolvesDescription; wolves.effectLabels = new[] { "基础冲撞伤害" };
            var lightning = nodes.Single(n => n.key == SkillsTypeEnum.Lightning_Demi_God);
            lightning.descriptionTemplate = LightningDescription; lightning.effectLabels = new[] { "基础雷击伤害" };
            PrefabUtility.SaveAsPrefabAsset(root, StorybookSkillsBuilder.Path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
