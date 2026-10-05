using System;
using System.Linq;
using Assets.Scripts.Extentions;
using Scripts.Skills;
using UnityEngine;

public class SkillsController : MonoSingleton<SkillsController>
{
    private const String VAL1 = "[VAL1]";
    private const String VAL2 = "[VAL2]";
    private const String VAL3 = "[VAL3]";
    private const String VAL4 = "[VAL4]";

    public int regenLevel = 0;
    public int pathCleanerLevel = 0;
    public int sinisterStampedeLevel = 0;
    public int devilOnMyShoulderLevel = 0;
    public int angelOnMyShoulderLevel = 0;
    public int ressurrectionLevel = 0;
    public int luckyBastardLevel = 0;
    public int lastButNotLeastLevel = 0;
    public int twiceTheFunLevel = 0;
    public int taironTheDragonLevel = 0;
    public int lightningDemiGodlevel = 0;
    public int bossKillerLevel = 0;
    public int myLuckyStarsLevel = 0;
    public int battlefieldTacticianLevel = 0;
    public int breathOfHellLevel = 0;

    public int GetSkillLevel(SkillsTypeEnum key)
    {
        int levelFound = 0;
        switch (key)
        {
            case SkillsTypeEnum.Regen:
                levelFound = regenLevel;
                break;
            case SkillsTypeEnum.Path_Cleaner:
                levelFound = pathCleanerLevel;
                break;
            case SkillsTypeEnum.Sinister_Stampede:
                levelFound = sinisterStampedeLevel;
                break;
            case SkillsTypeEnum.Angel_On_My_Shoulder:
                levelFound = angelOnMyShoulderLevel;
                break;
            case SkillsTypeEnum.Devil_On_My_Shoulder:
                levelFound = devilOnMyShoulderLevel;
                break;
            case SkillsTypeEnum.Ressurrection:
                levelFound = ressurrectionLevel;
                break;
            case SkillsTypeEnum.Lucky_Bastard:
                levelFound = luckyBastardLevel;
                break;
            case SkillsTypeEnum.Last_But_Not_Least:
                levelFound = lastButNotLeastLevel;
                break;
            case SkillsTypeEnum.Tairon_The_Dragon:
                levelFound = taironTheDragonLevel;
                break;
            case SkillsTypeEnum.Twice_The_Fun:
                levelFound = twiceTheFunLevel;
                break;
            case SkillsTypeEnum.Lightning_Demi_God:
                levelFound = lightningDemiGodlevel;
                break;
            case SkillsTypeEnum.Boss_Killer:
                levelFound = bossKillerLevel;
                break;
            case SkillsTypeEnum.My_Lucky_Stars:
                levelFound = myLuckyStarsLevel;
                break;
            case SkillsTypeEnum.Battlefield_Tactician:
                levelFound = battlefieldTacticianLevel;
                break;
            case SkillsTypeEnum.Breath_Of_Hell:
                levelFound = breathOfHellLevel;
                break;
        }
        return levelFound;
    }


    public int AddSkill(SkillsTypeEnum key)
    {
        int levelFound = 0;
        switch (key)
        {
            case SkillsTypeEnum.Regen:
                regenLevel++;
                break;
            case SkillsTypeEnum.Path_Cleaner:
                pathCleanerLevel++;
                break;
            case SkillsTypeEnum.Sinister_Stampede:
                sinisterStampedeLevel++;
                break;
            case SkillsTypeEnum.Angel_On_My_Shoulder:
                angelOnMyShoulderLevel++;
                break;
            case SkillsTypeEnum.Devil_On_My_Shoulder:
                devilOnMyShoulderLevel++;
                break;
            case SkillsTypeEnum.Ressurrection:
                ressurrectionLevel++;
                break;
            case SkillsTypeEnum.Lucky_Bastard:
                luckyBastardLevel++;
                break;
            case SkillsTypeEnum.Last_But_Not_Least:
                lastButNotLeastLevel++;
                break;
            case SkillsTypeEnum.Tairon_The_Dragon:
                taironTheDragonLevel++;
                break;
            case SkillsTypeEnum.Twice_The_Fun:
                twiceTheFunLevel++;
                break;
            case SkillsTypeEnum.Lightning_Demi_God:
                lightningDemiGodlevel++;
                break;
            case SkillsTypeEnum.Boss_Killer:
                bossKillerLevel++;
                break;
            case SkillsTypeEnum.My_Lucky_Stars:
                myLuckyStarsLevel++;
                break;
            case SkillsTypeEnum.Battlefield_Tactician:
                battlefieldTacticianLevel++;
                break;
            case SkillsTypeEnum.Breath_Of_Hell:
                breathOfHellLevel++;
                break;

        }
        return levelFound;
    }

    public Sprite GetSkillIcon(SkillsTypeEnum type, bool isGray = false)
    {
        Debug.Log("skillsType:" + type);
        if (isGray)
        {
            Sprite found = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().iconGray;
            return found;

        }
        else
        {
            Sprite found = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().icon;
            return found;

        }
    }



    public string GetSkillDescription(SkillsTypeEnum type, int levelWanted)
    {
        string desc = "";
        SkillData sd = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault();
        float[] val = { 0, 0, 0, 0, 0 };
        switch (type)
        {
            case SkillsTypeEnum.Regen:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Path_Cleaner:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Sinister_Stampede:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Angel_On_My_Shoulder:
                val = GetSkillValues(levelWanted, type);
                val[0] *= 100;
                val[1] *= 100;
                val[2] *= 100;
                //val[0] = GetSkillValues(GetSkillLevel(key), key)[0];
                desc = sd.desc.Replace("[VAL1]", val[0].ToString()).Replace(VAL2, val[1].ToString()).Replace(VAL3, val[2].ToString());
                break;
            case SkillsTypeEnum.Devil_On_My_Shoulder:
                val = GetSkillValues(levelWanted, type);
                val[0] *= 100;
                val[1] *= 100;
                //val[2] *= 100;
                //val[3] *= 100;

                //val[0] = GetSkillValues(GetSkillLevel(key), key)[0];
                desc = sd.desc.Replace(VAL1, val[0].ToString()).Replace(VAL2, val[1].ToString()).Replace(VAL3, val[2].ToString()).Replace(VAL4, val[3].ToString());
                break;
            case SkillsTypeEnum.Ressurrection:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;

            case SkillsTypeEnum.Lucky_Bastard:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;

            case SkillsTypeEnum.Twice_The_Fun:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Tairon_The_Dragon:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Last_But_Not_Least:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Lightning_Demi_God:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Boss_Killer:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.My_Lucky_Stars:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Battlefield_Tactician:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
            case SkillsTypeEnum.Breath_Of_Hell:
                val[0] = GetSkillValues(levelWanted, type)[0];
                val[0] *= 100;
                desc = sd.desc.Replace(VAL1, val[0].ToString());
                break;
        }
        return desc;
    }

    public float[] GetSkillValues(int level, SkillsTypeEnum type)
    {
        level = Mathf.Clamp(level, 0, 10);
        float[] val = { 0, 0, 0, 0, 0 };
        switch (type)
        {
            case SkillsTypeEnum.Regen:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Path_Cleaner:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Sinister_Stampede:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Angel_On_My_Shoulder:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                val[1] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value2[level];
                val[2] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value3[level];
                break;
            case SkillsTypeEnum.Devil_On_My_Shoulder:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                val[1] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value2[level];
                val[2] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value3[level];
                val[3] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value4[level];
                break;
            case SkillsTypeEnum.Ressurrection:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Lucky_Bastard:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Last_But_Not_Least:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Tairon_The_Dragon:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Twice_The_Fun:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Lightning_Demi_God:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Boss_Killer:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.My_Lucky_Stars:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Battlefield_Tactician:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
            case SkillsTypeEnum.Breath_Of_Hell:
                val[0] = SkillsDataDescriptions.SkillDataArr.Where(x => x.skillsType == type).FirstOrDefault().value1[level];
                break;
        }
        return val;
    }

    public void ResetSkills()
    {
        regenLevel = 0;
        pathCleanerLevel = 0;
        sinisterStampedeLevel = 0;
        devilOnMyShoulderLevel = 0;
        angelOnMyShoulderLevel = 0;
        ressurrectionLevel = 0;
        luckyBastardLevel = 0;
        twiceTheFunLevel = 0;
        taironTheDragonLevel = 0;
        lastButNotLeastLevel = 0;
        lightningDemiGodlevel = 0;
        bossKillerLevel = 0;
        myLuckyStarsLevel = 0;
        battlefieldTacticianLevel = 0;
        breathOfHellLevel = 0;
    }
}
