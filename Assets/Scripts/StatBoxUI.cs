using System;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Purchasing;
using IdleKnightHero.UI;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

public class StatBoxUI : MonoBehaviour
{
    [SerializeField]
    private Text _statLevel;
    [SerializeField]
    private Text _statVal;
    [SerializeField]
    private Text _statLabel;
    [SerializeField]
    private Image _bg;
    [SerializeField]
    private GameObject _plusSign;
    [SerializeField]
    private LevelTypeEnum _levelType;

    private Boolean clickable;

    public void RefreshMe()
    {
        switch (_levelType)
        {
            case LevelTypeEnum.HP:
                _statLevel.text = GameManager.single.hpLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.hpMax.ToString();
                break;
            case LevelTypeEnum.DMG:
                _statLevel.text = GameManager.single.dmgLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.dmg.ToString();
                break;
            case LevelTypeEnum.DEF:
                _statLevel.text = GameManager.single.defLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.def.ToString();
                break;
            case LevelTypeEnum.ATK_SPD:
                _statLevel.text = GameManager.single.atkSpdLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.atkSpd.ToString() + "%";
                break;
            case LevelTypeEnum.CRIT:
                _statLevel.text = GameManager.single.critLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.crit.ToString() + "%";
                break;
            case LevelTypeEnum.CRIT_DMG:
                _statLevel.text = GameManager.single.critDmgLevel.ToString();
                _statVal.text = GameManager.single.currentPlayerStat.critDmg.ToString() + "%";
                break;
        }

        int cost = 1;
        if (LevelController.Instance.GetStatLevel(_levelType) >= 100)
        {
            cost = 2;
        }

        if (PurchaserManager.Instance.LastStatPurchased(_levelType) || GameManager.single.statPoints < cost)
        {
            TintBlack();
        }
        else
        {
            TintYellow();
        }
    }

    public void TintYellow()
    {
        clickable = true;
        if (_plusSign != null) _plusSign.SetActive(true);
        _statLevel.color = LevelController.Instance.AssetManager._colorsDb[2];
        _statVal.color = LevelController.Instance.AssetManager._colorsDb[2];
        _statLabel.color = LevelController.Instance.AssetManager._colorsDb[2];
        _bg.color = LevelController.Instance.AssetManager._colorsDb[3];
    }

    public void TintBlack()
    {
        clickable = false;
        if (_plusSign != null) _plusSign.SetActive(false);
        _statLevel.color = LevelController.Instance.AssetManager._colorsDb[4];
        _statVal.color = LevelController.Instance.AssetManager._colorsDb[4];
        _statLabel.color = LevelController.Instance.AssetManager._colorsDb[4];
        _bg.color = LevelController.Instance.AssetManager._colorsDb[2];
    }

    public void ClickedStat()
    {
        if (!clickable) return;
        clickable = false;
        //if
        int cost = 1;
        if (LevelController.Instance.GetStatLevel(_levelType) >= 100)
        {
            cost = 2;
        }
        GameManager.single.statPoints -= cost;
        LevelController.Instance.IncStat(_levelType);
        SoundManager.Instance.PlayClip("STAT_BUY", false, true);
    }
}
