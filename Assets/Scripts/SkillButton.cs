using Assets.Scripts.Services;
using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField]
    private Text skillsOwnedText;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private SkillsTypeEnum skillKey;
    [SerializeField]
    private SkillsTypeEnum prerequisiteKey;
    [SerializeField]
    private SkillsTypeEnum prerequisiteKey2;
    [SerializeField]
    private Image arrow;

    private void OnEnable()
    {
        RefreshMe();
        EventManager.OnRefreshSkillButtons += RefreshMe;
    }

    private void OnDisable()
    {
        EventManager.OnRefreshSkillButtons -= RefreshMe;
    }


    void RefreshMe()
    {
        if (HasPrerequisiteSkill())
        {
            icon.sprite = SkillsController.Instance.GetSkillIcon(skillKey);
            int skillLevel = SkillsController.Instance.GetSkillLevel(skillKey);
            skillsOwnedText.text = skillLevel + " / 10";
            GetComponent<Button>().interactable = true;

            if (skillLevel == 10)
            {
                skillsOwnedText.color = LevelController.Instance.AssetManager._colorsDb[1];
                skillsOwnedText.text = "Maxed";
                //arrow.enabled = false;
                //GetComponent<Button>().interactable = false;

            }
            else
            {
                if (HasPrerequisiteSkillPoints())
                {
                    skillsOwnedText.color = LevelController.Instance.AssetManager._colorsDb[3];
                    arrow.color = LevelController.Instance.AssetManager._colorsDb[3];
                    arrow.enabled = true;

                }
                else
                {
                    skillsOwnedText.color = LevelController.Instance.AssetManager._colorsDb[4];
                    arrow.color = LevelController.Instance.AssetManager._colorsDb[4];
                    arrow.enabled = true;
                }
            }
        }
        else
        {
            SetLocked();
        }
    }

    void SetLocked()
    {
        icon.sprite = SkillsPanel.single.lockedSprite;
        skillsOwnedText.text = "Locked";
        skillsOwnedText.color = LevelController.Instance.AssetManager._colorsDb[6];
        arrow.enabled = false;
        GetComponent<Button>().interactable = false;
    }

    //    we got 4 different states:
    //*First is fully colored when you can puurchase it because you have a skill point.
    //* greyed out because you don't have a skill point, but if you had one, you could
    //*FUlly locked because you havent purchased the prerequisite 
    //*Finally fully maxed out 10/10 skill with green text "max"
    //and yeah we show the icon colored, but simply dont let them click on black locked ones
    bool HasPrerequisiteSkill()
    {
        if (prerequisiteKey==SkillsTypeEnum.NONE && prerequisiteKey2==SkillsTypeEnum.NONE) return true;

        if (SkillsController.Instance.GetSkillLevel(prerequisiteKey) > 0 || SkillsController.Instance.GetSkillLevel(prerequisiteKey2) > 0)
        {
            return true;
        }
        return false;
    }
    bool HasPrerequisiteSkillPoints()
    {
        int requiredPoints = 1;
        if (SkillsController.Instance.GetSkillLevel(skillKey) == 9) requiredPoints = 2;

        if (GameManager.single.skillPoints >= requiredPoints)
        {
            return true;
        }
        return false;
    }

    public void Clicked()
    {


        SkillsPanel.single.SetPreview(skillKey);
    }
}
