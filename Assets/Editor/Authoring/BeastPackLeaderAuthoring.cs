using System;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

public static class BeastPackLeaderAuthoring
{
    public const string Description = "提升影狼、幽灵狼群和书脊龙的伤害。";
    [MenuItem("Tools/Dark Fairytale/Skills/Apply Beast Pack Leader")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
        var root=PrefabUtility.LoadPrefabContents(StorybookSkillsBuilder.Path);
        try {
            var node=root.GetComponentsInChildren<StorybookSkillNode>(true).Single(n=>n.key==SkillsTypeEnum.BeastPackLeader);
            node.name="Skill_BeastPackLeader";node.displayName="兽群领袖";node.caption.text=node.displayName;
            node.descriptionTemplate=Description;node.effectLabels=new[]{"召唤伤害提升"};
            PrefabUtility.SaveAsPrefabAsset(root,StorybookSkillsBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        const string path="Assets/DarkFairytale/Companions/PetCindrel/Prefabs/PetCindrel.prefab";
        root=PrefabUtility.LoadPrefabContents(path);
        try {PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
