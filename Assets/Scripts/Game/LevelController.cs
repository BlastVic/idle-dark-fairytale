using System;
using Assets.Scripts.Extentions;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Statistics;
using Scripts.Level;
using UnityEngine;

public class LevelController : MonoSingleton<LevelController>
{
    [SerializeField]
    private AssetManager _assetManager;

    GameManager gm;
    protected override void Awake()
    {
        base.Awake();
        gm = GameManager.single;
    }

    public AssetManager AssetManager => Instance._assetManager;

    public float GetStatByLevel(LevelTypeEnum levelType, Int32 level)
    {
        float val = 1;
        switch (levelType)
        {
            case LevelTypeEnum.HP:
                val = LevelDataDescriptions.HpLevelDb[level];
                break;
            case LevelTypeEnum.DMG:
                val = LevelDataDescriptions.DmgLevelDb[level];
                break;
            case LevelTypeEnum.DEF:
                val = LevelDataDescriptions.DefLevelDb[level];
                break;
            case LevelTypeEnum.ATK_SPD:
                val = LevelDataDescriptions.AtkSpdLevelDb[level];
                break;
            case LevelTypeEnum.CRIT:
                val = LevelDataDescriptions.CritLevelDb[level];
                break;
            case LevelTypeEnum.CRIT_DMG:
                val = LevelDataDescriptions.CritDmgLevelDb[level];
                break;
            default:
                LoggerMethods.Log("Level Controller get stat by level: " + levelType);
                break;
        }
        return val;
    }

    public Int32 GetStatLevel(LevelTypeEnum levelType)
    {
        Int32 lvl = 0;
        GameManager gm = GameManager.single;
        switch (levelType)
        {
            case LevelTypeEnum.HP:
                lvl = gm.hpLevel;
                break;
            case LevelTypeEnum.DMG:
                lvl = gm.dmgLevel;
                break;
            case LevelTypeEnum.DEF:
                lvl = gm.defLevel;
                break;
            case LevelTypeEnum.ATK_SPD:
                lvl = gm.atkSpdLevel;
                break;
            case LevelTypeEnum.CRIT:
                lvl = gm.critLevel;
                break;
            case LevelTypeEnum.CRIT_DMG:
                lvl = gm.critDmgLevel;
                break;
            default:
                LoggerMethods.Log("Level Controller get stat level: " + levelType);
                break;
        }
        return lvl;
    }

    public void IncStat(LevelTypeEnum levelType)
    {
        PurchaserManager.Instance.SetLastStatPurchased(levelType);

        GameManager gm = GameManager.single;
        switch (levelType)
        {
            case LevelTypeEnum.HP:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-HP");
                gm.hpLevel++;
                if (gm.hpLevel >= LevelDataDescriptions.HpLevelDb.Length) gm.hpLevel = LevelDataDescriptions.HpLevelDb.Length;
                break;
            case LevelTypeEnum.DMG:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-DMG");
                gm.dmgLevel++;
                if (gm.dmgLevel >= LevelDataDescriptions.DmgLevelDb.Length) gm.dmgLevel = LevelDataDescriptions.DmgLevelDb.Length;
                break;
            case LevelTypeEnum.DEF:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-DEF");
                gm.defLevel++;
                if (gm.defLevel >= LevelDataDescriptions.DefLevelDb.Length) gm.defLevel = LevelDataDescriptions.DefLevelDb.Length;
                break;
            case LevelTypeEnum.ATK_SPD:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-ATKSPD");
                gm.atkSpdLevel++;
                if (gm.atkSpdLevel >= LevelDataDescriptions.AtkSpdLevelDb.Length) gm.atkSpdLevel = LevelDataDescriptions.AtkSpdLevelDb.Length;
                break;
            case LevelTypeEnum.CRIT:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-CRIT");
                gm.critLevel++;
                if (gm.critLevel >= LevelDataDescriptions.CritLevelDb.Length) gm.critLevel = LevelDataDescriptions.CritLevelDb.Length;
                break;
            case LevelTypeEnum.CRIT_DMG:
                StatisticsTracker.Instance.LogFlurryEvent("Stats-CRITDMG");
                gm.critDmgLevel++;
                if (gm.critDmgLevel >= LevelDataDescriptions.CritDmgLevelDb.Length) gm.critDmgLevel = LevelDataDescriptions.CritDmgLevelDb.Length;
                break;
        }
        //implement new hp
        if (Player.single != null)
        {
            Player.single.ab.currentStat = GameManager.single.CalcPlayer();
            Player.single.ab.currentStat.hpNow = Player.single.ab.currentStat.hpMax;
        }

        if (InventoryUI.single != null)
        {
            InventoryUI.single.RefreshStats();
        }
        GameplayCanvas.single.RefreshCampUI();
    }

    public bool IsNextRebirthReady()
    {
        if (gm.rebirthLevel + 1 == LevelDataDescriptions.RebirthLevelDb.Length)
        {
            LoggerMethods.Log("rebirth is maxed"); //try to protect overrunning the array
            return false;
        }

        if (gm.playerLevel >= LevelDataDescriptions.RebirthLevelDb[gm.rebirthLevel + 1].minLevel)
        {
            return true;
        }
        return false;
    }

    public void ProcessRebirth()
    {

        RebirthLevel myRebirthLevel = LevelDataDescriptions.RebirthLevelDb[gm.rebirthLevel + 1];

        gm.rebirthLevel++;

        gm.stackedXp = GetRebirthXpByLevel(gm.playerLevel, gm.rebirthLevel, gm.stackedXp);// currentRebirthLvl.expGiven;
        gm.stackedCrystals = GetRebirthCrystalsByLevel(gm.playerLevel, gm.rebirthLevel, gm.stackedCrystals);// currentRebirthLvl.expGiven;

        gm.playerLevel = 1;

        SaveManager.Instance.ResetFusionAndXp();
        SaveManager.Instance.ResetStats();
        SaveManager.Instance.ResetInventory();
        SkillsController.Instance.ResetSkills();

        gm.skillPoints = 0;
        gm.statPoints = 0;
        gm.crystals = 0;

        WaveManager.single.highestWavesReached.Clear();
        GameplayCanvas.single.RefreshCampUI();


        gm.crystals += gm.stackedCrystals;
        gm.statPoints += myRebirthLevel.statPointsGiven;
        gm.skillPoints += myRebirthLevel.skillPointsGiven;
        //gm.IncXp(myRebirthLevel.expGiven);

        SaveManager.Instance.Save();
    }

    public float GetRebirthXpByLevel(int psuedoPlayerLevel, int psuedoRebirthLevel, float carryoverBonus = 50)
    {
        RebirthLevel rl = LevelDataDescriptions.RebirthLevelDb[psuedoRebirthLevel];
        float baseXpBonus = rl.expGiven;
        float levelsPastMin = psuedoPlayerLevel - rl.minLevel;
        if (levelsPastMin == 0) return baseXpBonus + carryoverBonus; //no overlevel - return base + carryover
        float finalXp = carryoverBonus + baseXpBonus + levelsPastMin;
        LoggerMethods.Log("Rebirth Lvl:" + psuedoRebirthLevel + " Player Lvl:" + psuedoPlayerLevel + " Carryover Xp:" + carryoverBonus + " Base Reward:" + baseXpBonus + " Final Xp:" + finalXp);
        return finalXp;
    }

    public float GetRebirthCrystalsByLevel(int psuedoPlayerLevel, int psuedoRebirthLevel, float carryoverBonus = 50)
    {
        //base crystal bonus + 
        RebirthLevel rl = LevelDataDescriptions.RebirthLevelDb[psuedoRebirthLevel];
        float baseCrystalsBonus = rl.crystalsGiven;
        float levelsPastMin = psuedoPlayerLevel - rl.minLevel;
        if (levelsPastMin == 0) return baseCrystalsBonus + carryoverBonus; //no overlevel - return base + carryover
        float finalCrystals = carryoverBonus + baseCrystalsBonus + (levelsPastMin * 15);
        LoggerMethods.Log("Rebirth Lvl:" + psuedoRebirthLevel + " Player Lvl:" + psuedoPlayerLevel + " Carryover:" + carryoverBonus + " Base Reward:" + baseCrystalsBonus + " Overleveled by:" + levelsPastMin + " Overleveled bonus:" + (levelsPastMin * 10) + " Final Crystals:" + finalCrystals);
        return finalCrystals;
    }

    public RebirthLevel GetNearestRebirthLevel(int psuedoPlayerLevel)
    {
        foreach (RebirthLevel rl in LevelDataDescriptions.RebirthLevelDb)
        {
            if (psuedoPlayerLevel >= rl.minLevel) return rl;
        }
        LoggerMethods.Log("Uh Oh You did not find any rebirth levels for :" + psuedoPlayerLevel);
        return null;
    }
}
