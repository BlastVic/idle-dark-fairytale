using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services;
using IdleKnightHero.UI;
using Scripts.Level;
using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<GameManager>();
            return _instance;
        }
    }

    [Header("Game State Info")]
    public GameStateType gameState;

    [Header("Player Data")]
    public int playerLevel = 1;
    public int rebirthLevel = 0;
    public int fusionLevel;
    public int hpLevel;
    public int dmgLevel;
    public int defLevel;
    public int atkSpdLevel;
    public int critLevel;
    public int critDmgLevel;
    public Stat basePlayerStat, currentPlayerStat;
    public float xpNow = 0;
    public float xpMax = 10;
    public float fxpNow = 0;
    public float fxpMax = 10;
    public int skillPoints = 0;
    public int statPoints = 0;
    public float crystals = 0;
    public float premiumCurrency = 0;

    [Header("Tunables")]
    public int ChestPremiumOpenCost = 100;
    public int dailyCherriesAmount = 10;
    //stacked
    public float stackedXp;
    //public float stackedStatPoints;
    //public float stackedSkillPoints;
    public float stackedCrystals;

    public List<string> thingsSeenPermanent = new List<string>();
    GameplayCanvas gc;
    public string lastBattleKey = "Khorasan Ruins I";

    private void Awake()
    {
        gc = GameplayCanvas.single;
    }

    private void OnEnable()
    {
        EventManager.OnLoadComplete += HandleLoadComplete;
    }

    private void OnDisable()
    {
        EventManager.OnLoadComplete -= HandleLoadComplete;
    }

    private void Start()
    {
        //TEMPORARY AUTO_LEVEL LOADING
        //WaveManager.single.StartLevel("Level1");
        gc.ToggleBattleUi(false);

        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);
        //am.battleCampCamera.SetActive(true);

        //Invoke("LateCampTest", 5);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);

        //gc.RefreshFusion();
        //  TransitionCanvas.single.ToClear("GameManager-After spawnplayer");
        //gc.RefreshHp();
        //gc.RefreshXp();
        SetGameState(GameStateType.CAMP);


        Invoke("LateMusic", 1);
    }

    public IEnumerator WaitForCamp()
    {
        TransitionCanvas.single.ToBlack("Intro");

        while (LevelController.Instance.AssetManager.campPackageObj == null) yield return null;

        TransitionCanvas.single.ForceClear();
    }


    void LateMusic()
    {
        MusicManager.Instance.PlayMusicByClipNumber(0);//0 is default

    }

    public Stat CalcPlayer()
    {
        currentPlayerStat = Utilities.DeepClone(basePlayerStat);

        //add in stat points and stuff here after we have them
        currentPlayerStat.hpMax += LevelController.Instance.GetStatByLevel(LevelTypeEnum.HP, hpLevel);
        float hplevelPerc = hpLevel * .02f;
        currentPlayerStat.hpMax += hplevelPerc * currentPlayerStat.hpMax;

        currentPlayerStat.dmg += LevelController.Instance.GetStatByLevel(LevelTypeEnum.DMG, dmgLevel);
        float dmgLevelPerc = dmgLevel * .02f;
        currentPlayerStat.dmg += dmgLevelPerc * currentPlayerStat.dmg;

        currentPlayerStat.def += LevelController.Instance.GetStatByLevel(LevelTypeEnum.DEF, defLevel);
        float defLevelPerc = defLevel * .02f;
        currentPlayerStat.def += defLevelPerc * currentPlayerStat.def;

        currentPlayerStat.atkSpd += LevelController.Instance.GetStatByLevel(LevelTypeEnum.ATK_SPD, atkSpdLevel);
        float atkSpdLevelPerc = atkSpdLevel * .02f;
        currentPlayerStat.atkSpd += atkSpdLevelPerc * currentPlayerStat.atkSpd;
        Debug.Log("Attack Speed:" + currentPlayerStat.atkSpd);

        currentPlayerStat.crit += LevelController.Instance.GetStatByLevel(LevelTypeEnum.CRIT, critLevel);
        float critLevelPerc = critLevel * .02f;
        currentPlayerStat.crit += critLevelPerc * currentPlayerStat.crit;

        currentPlayerStat.critDmg += LevelController.Instance.GetStatByLevel(LevelTypeEnum.CRIT_DMG, critDmgLevel);
        float critDmgLevelPerc = critDmgLevel * .02f;
        currentPlayerStat.critDmg += critDmgLevelPerc * currentPlayerStat.critDmg;

        InventoryManager im = InventoryManager.single;
        if (im.hasWeapon)
        {
            AddItemStats(ref currentPlayerStat, im.weaponEq);
        }
        if (im.hasAcc)
        {
            AddItemStats(ref currentPlayerStat, im.accEq);
        }
        if (im.hasArmor)
        {
            AddItemStats(ref currentPlayerStat, im.armorEq);
        }
        if (im.hasHelmet)
        {
            AddItemStats(ref currentPlayerStat, im.helmetEq);
        }
        if (im.hasShield)
        {
            AddItemStats(ref currentPlayerStat, im.shieldEq);
        }

        //add in the percents stats here
        currentPlayerStat.dmg += currentPlayerStat.dmg * (currentPlayerStat.dmgPerc * .01f); //percent based stat, not to be confused with .02 bonus that occurs above
        currentPlayerStat.hpMax += currentPlayerStat.hpMax * (currentPlayerStat.hpPerc * .01f); //percent based stat, not to be confused with .02 bonus that occurs above
        currentPlayerStat.def += currentPlayerStat.def * (currentPlayerStat.defPerc * .01f); //percent based stat, not to be confused with .02 bonus that occurs above


        //add in the skills here
        //angel - Increases HP+[VAL1], DEF +[VAL2] & Block +[VAL3]
        int angelLevel = SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Angel_On_My_Shoulder);
        if (angelLevel > 0)
        {
            float[] vals = SkillsController.Instance.GetSkillValues(angelLevel, SkillsTypeEnum.Angel_On_My_Shoulder);
            currentPlayerStat.hpMax += vals[0] * currentPlayerStat.hpMax;
            currentPlayerStat.def += vals[1] * currentPlayerStat.def;
            currentPlayerStat.block += vals[2]; // * currentPlayerStat.def;
        }

        //devil - Increase DMG+[VAL1], ATK SPD+[VAL2], CRIT +[VAL3] & CRIT DMG +[VAL4]
        int devilLevel = SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Devil_On_My_Shoulder);
        if (devilLevel > 0)
        {
            float[] vals = SkillsController.Instance.GetSkillValues(devilLevel, SkillsTypeEnum.Devil_On_My_Shoulder);
            currentPlayerStat.dmg += vals[0] * currentPlayerStat.dmg;
            currentPlayerStat.atkSpd += vals[1] * currentPlayerStat.atkSpd;
            currentPlayerStat.crit += vals[2];
            currentPlayerStat.critDmg += vals[3];// * currentPlayerStat.critDmg;
        }



        //round the stats here
        currentPlayerStat.dmg = Mathf.Round(currentPlayerStat.dmg);
        currentPlayerStat.def = Mathf.Round(currentPlayerStat.def);
        currentPlayerStat.atkSpd = Mathf.Round(currentPlayerStat.atkSpd);
        currentPlayerStat.crit = (float)System.Math.Round(currentPlayerStat.crit, 1);
        currentPlayerStat.critDmg = Mathf.Round(currentPlayerStat.critDmg);
        currentPlayerStat.hpMax = Mathf.Round(currentPlayerStat.hpMax);

        return currentPlayerStat;
    }

    void HandleLoadComplete()
    {
        Debug.Log("Loaded Game Data if existing");
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.RefreshCampUI();

        if (SaveManager.Instance._userData != null)
        {
            // Debug.Log(SaveManager.Instance.m_UserData);
            GameplayCanvas.single.RefreshCampUI();
            GameplayCanvas.single.RefreshXp();
        }
    }

    void AddItemStats(ref Stat stat, Item item)
    {
        if (item.baseStat.dmg > 0)
        {
            stat.dmg += item.baseStat.dmg;
        }
        if (item.baseStat.def > 0)
        {
            stat.def += item.baseStat.def;
        }
        if (item.baseStat.hpMax > 0)
        {
            stat.hpMax += item.baseStat.hpMax;
        }
        if (item.baseStat.atkSpd > 0)
        {
            stat.atkSpd += item.baseStat.atkSpd;
        }
        if (item.baseStat.crit > 0)
        {
            stat.crit += item.baseStat.crit;
        }
        if (item.baseStat.critDmg > 0)
        {
            stat.critDmg += item.baseStat.critDmg;
        }
        if (item.baseStat.hpPerc > 0)
        {
            stat.hpPerc += item.baseStat.hpPerc;
        }
        if (item.baseStat.defPerc > 0)
        {
            stat.defPerc += item.baseStat.defPerc;
        }
        if (item.baseStat.dmgPerc > 0)
        {
            stat.dmgPerc += item.baseStat.dmgPerc;
        }
    }

    public void IncXp(float amount)
    {
        #region BONUS for Rebirth
        if (rebirthLevel > 0)
        {
            Debug.Log("Amount XP Given:" + amount);
            amount += amount * (.01f * stackedXp);
            Debug.Log("Stacked Xp:" + stackedXp + " total XP Given:" + amount);
        }
        #endregion

        #region BONUS for XP Booster
        if (isBoostedXPActive)
        {
            amount += amount * .33f;
        }
        #endregion


        xpNow += amount;
        if (xpNow >= xpMax)
        {
            LevelUpEvent();
        }
        gc.RefreshXp();
    }

    public void IncFusion(float amount)
    {
        if (amount > 0) SoundManager.Instance.PlayClip("FUSION_FILL", false, true);
        fxpNow += amount;
        if (fxpNow >= fxpMax)
        {
            gc.ToggleFusionButton(true);
        }
        gc.RefreshFusion();
        //if (InventoryUI.single != null) InventoryUI.single.RefreshFusion();
    }

    void LevelUpEvent()
    {
        SoundManager.Instance.PlayClip("LEVEL_UP", false, true);
        xpNow = 0;
        if (playerLevel <= LevelDataDescriptions.ExpLevelDb.Length - 1)
        {
            xpMax = LevelDataDescriptions.ExpLevelDb[playerLevel + 1];
        }
        else
        {
            xpMax = LevelDataDescriptions.ExpLevelDb[LevelDataDescriptions.ExpLevelDb.Length - 1];
        }
        playerLevel++;
        statPoints++;

        if (Utilities.IsDivisible(playerLevel, 5)) skillPoints++;
        gc.PlayLevelUp();
        SaveManager.Instance.Save();

    }

    float rolloverFusion = 0;
    public void FusionLevelUpEvent()
    {
        SoundManager.Instance.PlayClip("FUSION_REWARD", false, true);
        gc.ToggleFusionButton(false);//right away protect abuse of double taps

        rolloverFusion = fxpNow - fxpMax;
        fxpNow = 0;

        if (fusionLevel <= LevelDataDescriptions.FusionLevelDb.Length - 1)
        {
            fxpMax = LevelDataDescriptions.FusionLevelDb[fusionLevel].requirement;
        }
        else
        {
            fxpMax = LevelDataDescriptions.FusionLevelDb[LevelDataDescriptions.FusionLevelDb.Length - 1].requirement;
        }
        FusionLevel flData = Utilities.DeepClone(LevelDataDescriptions.FusionLevelDb[fusionLevel]);

        //DROPS
        for (int i = 0; i < flData.itemsGiven.Count; i++)
        {
            bool shouldDrop = false;
            float chance = Random.Range(0, 1f);
            if (chance <= flData.itemsGiven[i].m_ChanceDrop)
            {
                //Debug.Log("Drop a:" + itemsGiven[i].m_ItemBasicTitle);
                Item newItem = InventoryManager.single.CreateItemStats(flData.itemsGiven[i]);
                //for now just keep same name
                newItem.m_ItemFinalTitle = InventoryManager.single.GetRandomName(newItem);
                newItem.uniqueId = Random.Range(0, 999999);
                InventoryManager.single.AddItem(newItem);
                GameplayCanvas.single.ShowItemPopUp(newItem);
            }
        }


        fusionLevel++;
        Debug.Log("HERE WE GIVE ALL THE FUSION REWARDS FOR THE LEVEL");
        if (flData.statPointsGiven > 0)
        {
            statPoints += flData.statPointsGiven;
            GameplayCanvas.single.ShowItemPopUp(new Item { m_IconName = "Stat", m_ItemFinalTitle = "Stats +" + flData.statPointsGiven });
        }
        if (flData.skillPointsGiven > 0)
        {
            skillPoints += flData.skillPointsGiven;
            GameplayCanvas.single.ShowItemPopUp(new Item { m_IconName = "Skill", m_ItemFinalTitle = "Skills +" + flData.skillPointsGiven });

        }

        IncXp(flData.expGiven);
        gc.RefreshXp();

        if (flData.expGiven > 0)
        {
            GameplayCanvas.single.ShowItemPopUp(new Item { m_IconName = "Exp", m_ItemFinalTitle = "XP +" + flData.expGiven });
        }
        //we should always be on inventory screen when fusing things
        if (InventoryUI.single != null && InventoryUI.single.gameObject.activeSelf)
        {
            InventoryUI.single.RefreshStats();
        }

        fxpNow += rolloverFusion;
        if (fxpNow > fxpMax)
        {
            FusionLevelUpEvent();
        }
        else
        {
            //gc.RefreshFusion();
        }
        gc.RefreshCampUI();
    }

    public List<Actor_Base> GetLivingEnemies()
    {
        List<Actor_Base> actorsFound = new List<Actor_Base>();
        GameObject[] objFound = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject obj in objFound)
        {
            Actor_Base nextAb = obj.GetComponent<Actor_Base>();
            if (nextAb.isDying) continue;
            actorsFound.Add(nextAb);
        }
        return actorsFound;
    }

    public List<Actor_Base> GetAllBosses()
    {
        List<Actor_Base> actorsFound = new List<Actor_Base>();
        GameObject[] objFound = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject obj in objFound)
        {
            Actor_Base nextAb = obj.GetComponent<Actor_Base>();
            if (nextAb.isDying) continue;
            if (!nextAb.isBoss) continue;
            actorsFound.Add(nextAb);
        }
        return actorsFound;
    }



    public void EnemyCleanup()
    {
        DestroyByTag("Enemy");
        DestroyByTag("DyingEnemy");
        DestroyByTag("Pet");
    }

    public void MapCleanup()
    {
        DestroyByTag("MapGraphics");
    }

    public void BattleCleanup()
    {
        //clean map / player / enemies

        LevelController.Instance.AssetManager.HidePlayer();
        EnemyCleanup();
        MapCleanup();
    }

    void DestroyByTag(string tag)
    {
        GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in found)
        {
            Destroy(obj);
        }
    }

    public bool HasSeenThis(string s)
    {
        foreach (string next in thingsSeenPermanent)
        {
            if (s == next) return true;
        }
        return false;
    }

    public void SetGameState(GameStateType newState)
    {
        gameState = newState;
        gc.OrderButtonGrid();
        gc.ToggleButtonGrid(true);
        if (gameState == GameStateType.CAMP)
        {
            GameplayCanvas.single.XpBoostButton.GetComponent<Button>().interactable = true;
        }
        if (gameState == GameStateType.BATTLE)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.VICTORY)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.DEFEAT)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.CHEST_PACKAGE)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.FUSION)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.REBIRTH)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.CASH_MALL)
        {
            gc.ToggleButtonGrid(false);
        }
        if (gameState == GameStateType.XP_BOOST)
        {
            gc.ToggleButtonGrid(false);
        }
    }

    #region XP BOOSTER
    Timer xpBoostTimer;
    public System.DateTime xpBoostEndTime, xpBoostStartedTime;
    public string xpBoostEndTimeString;

    public bool isBoostedXPActive = false;
    public void AddXPBoost(string overrideTime = "", bool useOverrideTime = false)
    {
        if (useOverrideTime)
        {
            //restoring from a save restores a time to this thing
            long restoreDateLong = System.Convert.ToInt64(overrideTime);
            //long to date
            xpBoostEndTime = System.DateTime.FromBinary(restoreDateLong);
        }
        else
        {
            if(xpBoostEndTime < System.DateTime.Now)
            {
                xpBoostEndTime = System.DateTime.Now.AddMinutes(10);
            }
            else
            {
                xpBoostEndTime = xpBoostEndTime.AddMinutes(10);
            }
        }

        isBoostedXPActive = true;
        //string back to a long
        if (xpBoostTimer == null)
        {
            xpBoostTimer = GameplayCanvas.single.xpBoosterTimerText.gameObject.AddComponent<Timer>();
        }
        else
        {

            //xpBoostTimer.enabled = false;
            //xpBoostTimer.enabled = true;//= GameplayCanvas.single.xpBoosterTimerText.gameObject.AddComponent<Timer>();

        }

        //long restoreDateLong = System.Convert.ToInt64(xpBoostEndTimeString);
        ////long to date
        //System.DateTime restoreDate = System.DateTime.FromBinary(restoreDateLong);


        xpBoostTimer.initEachEnable = true;//we want this so we can close/open and it will refresh itself
        xpBoostTimer.emptyisCustom = true;
        xpBoostTimer.customEmptyString = "No Active Boost";
        xpBoostStartedTime = System.DateTime.Now;

        xpBoostEndTimeString = xpBoostEndTime.ToBinary().ToString();

        //string rewardTimeString = rewardTime.ToBinary().ToString();
        xpBoostTimer.InitializeTimerByDT(xpBoostEndTime, GameplayCanvas.single.xpBoosterTimerText, Color.yellow);

    }

    #endregion

}