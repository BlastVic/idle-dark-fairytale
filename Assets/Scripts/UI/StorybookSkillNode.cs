using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored node and original prerequisites. Positions/art remain editable in the prefab.</summary>
public sealed class StorybookSkillNode : MonoBehaviour
{
    public SkillsTypeEnum key, prerequisite, alternatePrerequisite;
    public string displayName;
    [TextArea] public string descriptionTemplate;
    public string[] effectLabels;
    public Button button;
    public Image icon;
    public Text caption, level;
    public GameObject selected, locked, available;
    public RectTransform Rect => (RectTransform)transform;
    public bool IsUnlocked(SkillsController skills) =>
        (prerequisite == SkillsTypeEnum.NONE && alternatePrerequisite == SkillsTypeEnum.NONE) ||
        skills.GetSkillLevel(prerequisite) > 0 || skills.GetSkillLevel(alternatePrerequisite) > 0;
}
