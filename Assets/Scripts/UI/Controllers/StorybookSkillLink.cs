using UnityEngine;
using UnityEngine.UI;

/// <summary>Both line styles and arrowheads are persistent inspector-editable prefab objects.</summary>
public sealed class StorybookSkillLink : MonoBehaviour
{
    public StorybookSkillNode source, target;
    public GameObject solid, dashed;
    public Graphic arrow;
    public Color satisfied = new Color(.48f,.055f,.045f), unmet = new Color(.38f,.36f,.33f);
    public void Refresh(SkillsController skills)
    {
        bool learned = skills.GetSkillLevel(source.key) > 0;
        solid.SetActive(learned); dashed.SetActive(!learned); arrow.color = learned ? satisfied : unmet;
    }
}
