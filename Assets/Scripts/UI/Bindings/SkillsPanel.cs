using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services;
using IdleKnightHero.UI;
using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanel : MonoBehaviour
{
    private static SkillsPanel _instance;
    public static SkillsPanel single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<SkillsPanel>();
            return _instance;
        }
    }
    public string[] oneLiners;

    public Text oneLinerText;
    public Text skillTitleText;
    public Text skillDescText;
    public Text costText;
    public Text skillAmountOwnedText;
    public GameObject container;
    public Text skillPtsText;
    GameManager gm;
    public GameObject skillPreviewBox;
    public GameObject skillPreviewOverlay;
    public Button buyButton;
    public Image skillIcon;
    public Sprite lockedSprite;

    private void OnEnable()
    {
        gm = GameManager.single;
        oneLinerText.text = oneLiners[Random.Range(0, oneLiners.Length - 1)];
        RefreshMe();
    }

    public void RefreshMe()
    {
        skillPtsText.text = gm.skillPoints.ToString();
    }

    public SkillsTypeEnum lastSkillKey;
    public int lastCost;
    public void SetPreview(SkillsTypeEnum key)
    {
        int skillLevel = SkillsController.Instance.GetSkillLevel(key);
        int requiredPoints = 1;
        if (SkillsController.Instance.GetSkillLevel(key) == 9) requiredPoints = 2;

        skillAmountOwnedText.text = skillLevel + " / 10";
        if (skillLevel == 10)
        {
            skillAmountOwnedText.color = LevelController.Instance.AssetManager._colorsDb[1];
            skillAmountOwnedText.text = "MAXED";
        }
        else
        {
            skillAmountOwnedText.color = LevelController.Instance.AssetManager._colorsDb[7];
        }

        int costNow = 1;
        if (skillLevel == 9) costNow = 2;
        if (skillLevel == 10) costNow = 0;
        costText.text = costNow.ToString();
        skillPreviewBox.SetActive(true);
        skillDescText.text = SkillsController.Instance.GetSkillDescription(key, SkillsController.Instance.GetSkillLevel(key) + 1);
        skillIcon.sprite = SkillsController.Instance.GetSkillIcon(key);
        skillTitleText.text = key.ToString();
        lastSkillKey = key;
        lastCost = requiredPoints;

        if (GameManager.single.skillPoints >= requiredPoints && skillLevel < 10)
        {
            skillPreviewOverlay.SetActive(false);
            buyButton.interactable = true;

        }
        else
        {
            skillPreviewOverlay.SetActive(true);
            buyButton.interactable = false;
        }
    }

    public void TryBuySkill()
    {

        GameManager.single.skillPoints -= lastCost;
        SkillsController.Instance.AddSkill(lastSkillKey);
        SoundManager.Instance.PlayClip("SKILL_BUY", false, false);
        RefreshMe();
        EventManager.single.CallRefreshSkillButtons();
        SetPreview(lastSkillKey);
        GameplayCanvas.single.RefreshCampUI();
    }

    public void CloseButton()
    {
        Router.single.CallCampFromSkills();
    }


}
