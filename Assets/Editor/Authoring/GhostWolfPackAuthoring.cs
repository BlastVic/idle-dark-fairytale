using System;
using System.IO;
using System.Linq;
using Scripts.Skills;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

// Explicit authoring only. Existing effect layout is never regenerated on import or Play.
public static class GhostWolfPackAuthoring
{
    public const string IconPath="Assets/DarkFairytale/UI/Skills/Textures/SummonGhostWolfPack.png";
    public const string PrefabPath="Assets/Resources/Effects/GhostWolfPack.prefab";
    public static readonly string Description=QueuedSkillAuthoring.WolvesDescription;
    [MenuItem("Tools/Dark Fairytale/Skills/Apply Summon Ghost Wolf Pack")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
        if(!File.Exists(PrefabPath)) BuildPack();
        if(!File.Exists(IconPath)) throw new FileNotFoundException("Missing authored ghost wolf pack icon; restore the production illustration.", IconPath);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        var root=PrefabUtility.LoadPrefabContents(ShadowWolfSkillAuthoring.UiPath);
        try {
            var node=root.GetComponentsInChildren<StorybookSkillNode>(true).Single(n=>n.key==SkillsTypeEnum.GhostWolfPack);
            node.name="Skill_GhostWolfPack";node.displayName="召唤：幽灵狼群";
            node.caption.text="召唤：\n幽灵狼群";
            var caption=node.caption.rectTransform;caption.sizeDelta=new Vector2(caption.sizeDelta.x,60);
            var level=node.level.rectTransform;level.anchoredPosition=new Vector2(level.anchoredPosition.x,-196);
            node.descriptionTemplate=Description;node.effectLabels=new[]{"基础冲撞伤害"};
            node.icon.sprite=icon;node.icon.material=null;node.icon.preserveAspect=true;
            PrefabUtility.SaveAsPrefabAsset(root,ShadowWolfSkillAuthoring.UiPath);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=AssetDatabase.LoadAssetAtPath<SkillsDataDescriptions>("Assets/Resources/Data/SkillsDataDescriptions.asset");
        var so=new SerializedObject(data);var entries=so.FindProperty("_skillDataArr");
        for(int i=0;i<entries.arraySize;i++){
            var entry=entries.GetArrayElementAtIndex(i);
            if(entry.FindPropertyRelative("skillsType").intValue!=(int)SkillsTypeEnum.GhostWolfPack)continue;
            entry.FindPropertyRelative("icon").objectReferenceValue=icon;
            entry.FindPropertyRelative("iconGray").objectReferenceValue=icon;
        }
        so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
    }
    static void BuildPack()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Companions/PetGhostWolf/Prefabs/PetGhostWolf.prefab");
        var template=source.GetComponent<WolfPetCompanion>();
        var root=new GameObject("GhostWolfPack");root.tag="Pet";
        try {
            var pack=root.AddComponent<GhostWolfPack>();pack.wolves=new GhostWolfCharge[5];
            float[] lanes={.29f,.38f,.33f,.42f,.26f};
            for(int i=0;i<5;i++){
                var go=new GameObject("GhostWolf_"+(i+1));go.transform.SetParent(root.transform,false);
                go.transform.localPosition=new Vector3(-i*1.5f,i*.4f,0);
                var charge=go.AddComponent<GhostWolfCharge>();pack.wolves[i]=charge;
                charge.skeleton=UnityEngine.Object.Instantiate(template.skeleton,go.transform,false);
                charge.skeleton.name="Ghost Wolf Spine";charge.skeleton.AnimationName="Run_Battle_00";charge.skeleton.loop=true;
                charge.viewportSize=template.viewportSize;charge.laneViewportY=lanes[i];charge.launchDelay=i*.20f;
            }
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
