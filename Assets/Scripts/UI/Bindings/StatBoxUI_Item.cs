using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

public class StatBoxUI_Item : MonoBehaviour
{
    [SerializeField]
    private Text statVal;
    [SerializeField]
    private Text statLabel;
    [SerializeField]
    private Image arrow;
    [SerializeField]
    private Image arrowDown;
    [SerializeField]
    private Text dashImg;
    [SerializeField]
    private LevelTypeEnum levelType;

    public void ColorAndArrow(Item item, Item equipped)
    {
        if (equipped == null)
        {
            //default green
            statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
            statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
            arrowDown.enabled = false;
            arrow.color = LevelController.Instance.AssetManager._colorsDb[1];
            dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
            return;
        }
        //green is gm color 1
        //red is 6
        switch (levelType)
        {
            case LevelTypeEnum.HP:
                if (item.baseStat.hpMax > equipped.baseStat.hpMax)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.hpMax < equipped.baseStat.hpMax)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.hpMax == equipped.baseStat.hpMax)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;

                }
                break;
            case LevelTypeEnum.DMG:
                if (item.baseStat.dmg > equipped.baseStat.dmg)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.dmg < equipped.baseStat.dmg)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.dmg == equipped.baseStat.dmg)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.DEF:
                if (item.baseStat.def > equipped.baseStat.def)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.def < equipped.baseStat.def)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.def == equipped.baseStat.def)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.ATK_SPD:
                if (item.baseStat.atkSpd > equipped.baseStat.atkSpd)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.atkSpd < equipped.baseStat.atkSpd)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.atkSpd == equipped.baseStat.atkSpd)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.CRIT:
                if (item.baseStat.crit > equipped.baseStat.crit)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.crit < equipped.baseStat.crit)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.crit == equipped.baseStat.crit)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.CRIT_DMG:
                if (item.baseStat.critDmg > equipped.baseStat.critDmg)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.critDmg < equipped.baseStat.critDmg)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.critDmg == equipped.baseStat.critDmg)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.HP_PERC:
                if (item.baseStat.hpPerc > equipped.baseStat.hpPerc)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.hpPerc < equipped.baseStat.hpPerc)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.hpPerc == equipped.baseStat.hpPerc)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.DMG_PERC:
                if (item.baseStat.dmgPerc > equipped.baseStat.dmgPerc)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.dmgPerc < equipped.baseStat.dmgPerc)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.dmgPerc == equipped.baseStat.dmgPerc)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
            case LevelTypeEnum.DEF_PERC:
                if (item.baseStat.defPerc > equipped.baseStat.defPerc)
                {
                    //green
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[1];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[1];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[1];
                    arrow.enabled = true;
                    arrowDown.enabled = false;
                }
                if (item.baseStat.defPerc < equipped.baseStat.defPerc)
                {
                    //red
                    arrow.enabled = true;
                    statLabel.color = LevelController.Instance.AssetManager._colorsDb[6];
                    statVal.color = LevelController.Instance.AssetManager._colorsDb[6];
                    dashImg.color = LevelController.Instance.AssetManager._colorsDb[6];
                    arrow.enabled = false;
                    arrowDown.enabled = true;
                }
                if (item.baseStat.defPerc == equipped.baseStat.defPerc)
                {
                    statLabel.color = Color.white;
                    statVal.color = Color.white;
                    arrow.enabled = false;
                    arrowDown.enabled = false;
                    dashImg.color = Color.white;
                }
                break;
        }
    }

    public void SetMe(Item item) //set the stats, make arrow green or red or disappear
    {
        switch (levelType)
        {
            case LevelTypeEnum.HP:
                statVal.text = item.baseStat.hpMax.ToString();
                break;
            case LevelTypeEnum.DMG:
                statVal.text = item.baseStat.dmg.ToString();
                break;
            case LevelTypeEnum.DEF:
                statVal.text = item.baseStat.def.ToString();
                break;
            case LevelTypeEnum.ATK_SPD:
                statVal.text = item.baseStat.atkSpd.ToString() + "%";
                break;
            case LevelTypeEnum.CRIT:
                statVal.text = item.baseStat.crit.ToString() + "%";
                break;
            case LevelTypeEnum.CRIT_DMG:
                statVal.text = item.baseStat.critDmg.ToString() + "%";
                break;
            case LevelTypeEnum.HP_PERC:
                statVal.text = item.baseStat.hpPerc + "%";
                break;
            case LevelTypeEnum.DMG_PERC:
                statVal.text = item.baseStat.dmgPerc + "%";
                break;
            case LevelTypeEnum.DEF_PERC:
                statVal.text = item.baseStat.defPerc + "%";
                break;
        }

    }

    bool clickable = false;
    public void TintGreen()
    {
        clickable = true;
        statVal.color = LevelController.Instance.AssetManager._colorsDb[2];
        statLabel.color = LevelController.Instance.AssetManager._colorsDb[2];
    }

    public void TintWhite()
    {
        clickable = false;
        statVal.color = LevelController.Instance.AssetManager._colorsDb[4];
        statLabel.color = LevelController.Instance.AssetManager._colorsDb[4];
    }

    //public void ClickedStat()
    //{
    //    if (!clickable) return;
    //    clickable = false;
    //    GameManager.single.statPoints--;
    //    StatManager.single.IncStat(statString);
    //}
}
