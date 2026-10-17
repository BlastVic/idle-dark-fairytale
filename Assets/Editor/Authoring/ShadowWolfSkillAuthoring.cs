using System;
using System.IO;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

// Explicit, scoped migration. Never rebuild the skill tree or overwrite layout on import.
public static class ShadowWolfSkillAuthoring
{
    public const string IconPath = "Assets/DarkFairytale/UI/Skills/Textures/SummonShadowWolf.png";
    public const string UiPath = "Assets/DarkFairytale/UI/Skills/Prefabs/StorybookSkills.prefab";
    public const string PetPath = "Assets/DarkFairytale/Companions/PetShadowWolf/Prefabs/PetShadowWolf.prefab";
    [MenuItem("Tools/Dark Fairytale/Skills/Apply Summon Shadow Wolf")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
        if (!File.Exists(IconPath)) throw new FileNotFoundException("Missing authored shadow wolf head icon; restore the production art instead of rendering the companion model.", IconPath);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        var root = PrefabUtility.LoadPrefabContents(UiPath);
        try {
            var node = root.GetComponentsInChildren<StorybookSkillNode>(true).Single(n => n.key == SkillsTypeEnum.PetShadowWolf);
            node.name = "Skill_PetShadowWolf";
            node.displayName = "召唤：影狼";
            node.caption.text = node.displayName;
            node.descriptionTemplate = SkillDesign.Descriptions[2];
            node.effectLabels = SkillDesign.Labels[2];
            node.icon.sprite = icon;
            node.icon.material = null;
            node.icon.preserveAspect = true;
            PrefabUtility.SaveAsPrefabAsset(root, UiPath);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        root = PrefabUtility.LoadPrefabContents(PetPath);
        try {
            var pet = root.GetComponent<WolfPetCompanion>();
            pet.damageSkill = SkillsTypeEnum.PetShadowWolf;
            // A separate row permits both learned companions without overlapping their bodies.
            pet.viewportFeet = new Vector2(.28f, .245f);
            PrefabUtility.SaveAsPrefabAsset(root, PetPath);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        root = PrefabUtility.LoadPrefabContents(CursedStorybookUiBuilder.HudPath);
        try {
            var status = root.GetComponentInChildren<BattleStatusDetails>(true);
            if (!status.keys.Contains(SkillsTypeEnum.PetShadowWolf)) {
                int dragon = Array.IndexOf(status.keys, SkillsTypeEnum.PetCindrel);
                var slot = UnityEngine.Object.Instantiate(status.skillSlots[dragon], status.skillSlots[dragon].transform.parent);
                slot.name = "PetShadowWolf";
                var image = slot.GetComponent<UnityEngine.UI.Image>();image.sprite=icon;image.material=null;
                var mask = slot.transform.Find("ClockwiseCooldown").GetComponent<UnityEngine.UI.Image>();mask.sprite=icon;
                status.keys=status.keys.Concat(new[]{SkillsTypeEnum.PetShadowWolf}).ToArray();
                status.skillSlots=status.skillSlots.Concat(new[]{slot}).ToArray();
                status.cooldownMasks=status.cooldownMasks.Concat(new[]{mask}).ToArray();
                slot.transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing=20;
            }
            PrefabUtility.SaveAsPrefabAsset(root, CursedStorybookUiBuilder.HudPath);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        var data = AssetDatabase.LoadAssetAtPath<SkillsDataDescriptions>("Assets/Resources/Data/SkillsDataDescriptions.asset");
        var so = new SerializedObject(data);
        var entries = so.FindProperty("_skillDataArr");
        for (int i = 0; i < entries.arraySize; i++) {
            var entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("skillsType").intValue != (int)SkillsTypeEnum.PetShadowWolf) continue;
            entry.FindPropertyRelative("icon").objectReferenceValue = icon;
            entry.FindPropertyRelative("iconGray").objectReferenceValue = icon;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
}
