using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Statistics;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

//all switching of gamestates/UI transitions logic in here
public class Router : MonoBehaviour
{
    private static Router _instance;
    public static Router single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<Router>();
            return _instance;
        }
    }

    GameManager gm;
    GameplayCanvas gc;

    // Start is called before the first frame update
    void Start()
    {
        gm = GameManager.single;
        gc = GameplayCanvas.single;
    }

    bool loadingFusionFromCamp = false;
    public void CallFusionFromCamp()
    {
        if (loadingFusionFromCamp) return;
        loadingFusionFromCamp = true;
        StartCoroutine(GoToFusion());
    }

    public IEnumerator GoToFusion()
    {
        TransitionCanvas.single.ToBlack("GM TO Fusion");
        yield return new WaitForSeconds(.9f);
        gm.SetGameState(GameStateType.FUSION);

        gc.ToggleCampUi(false);

        CameraEffects.single.SetOriginalCameraPosition();
        gc.inventoryPanel.SetActive(false);

        gc.skillsPanel.SetActive(false);

        LevelController.Instance.AssetManager.HideCampPackage();
        LevelController.Instance.AssetManager.HidePlayer();
        LevelController.Instance.AssetManager.HideMapPackage();

        gc.ToggleButtonGrid(false);
        gc.ToggleFusionBarUI(false);
        LevelController.Instance.AssetManager.SpawnGeneralObj("FusionPackage");
        yield return new WaitForSeconds(.3f);

        while (FusionPackage.single == null)
        {
            yield return null;//wait in black until done
        }

        TransitionCanvas.single.ToClear("GM TO Fusion");
        loadingFusionFromCamp = false;

    }


    public void CallCampFromFusion(bool finishFeeding = false)
    {
        if (loadingCampFromFusion) return;
        loadingCampFromFusion = true;
        StartCoroutine(GoToCampFromFusion());
    }
    bool loadingCampFromFusion = false;

    public IEnumerator GoToCampFromFusion(bool finishFeeding = false)
    {
        TransitionCanvas.single.ToBlack("GM TO ");
        //GameplayCanvas.single.lastChestClicked = chestChosen;
        yield return new WaitForSeconds(.7f);
        Destroy(FusionPackage.single.gameObject);
        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.ToggleFusionBarUI(true);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);

        gm.SetGameState(GameStateType.CAMP);

        TransitionCanvas.single.ToClear("GM TO ");

        yield return new WaitForSeconds(.5f);

        if (finishFeeding) gm.IncFusion(1);//push it over the edge
        loadingCampFromFusion = false;
    }

    public void CallRebirthFromCamp()
    {
        if (loadingRebirthFromCamp) return;
        loadingChestFromCamp = true;
        StartCoroutine(GoToRebirth());
    }

    bool loadingRebirthFromCamp = false;
    public IEnumerator GoToRebirth()
    {
        TransitionCanvas.single.ToBlack("GM TO reb");

        yield return new WaitForSeconds(.7f);
        gc.ToggleCampUi(false);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(false);

        LevelController.Instance.AssetManager.HideCampPackage();
        LevelController.Instance.AssetManager.HidePlayer();
        gc.ToggleFusionBarUI(false);
        gc.ToggleButtonGrid(false);
        LevelController.Instance.AssetManager.SpawnGeneralObj("RebirthPackage");
        TransitionCanvas.single.ToClear("GM TO reb");
        loadingRebirthFromCamp = false;
        gm.SetGameState(GameStateType.REBIRTH);

    }

    //REBIRTH
    public void CallCampFromRebirth(bool getRewards = false)
    {
        if (loadingCampFromRebirth) return;
        loadingCampFromRebirth = true;
        StartCoroutine(GoToCampFromRebirth());
    }
    bool loadingCampFromRebirth = false;

    public IEnumerator GoToCampFromRebirth()
    {
        TransitionCanvas.single.ToBlack("GM TO c");
        //GameplayCanvas.single.lastChestClicked = chestChosen;
        yield return new WaitForSeconds(.7f);
        Destroy(RebirthPackage.single.gameObject);
        gc.ToggleCampUi(true);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);

        gc.ToggleFusionBarUI(true);
        gc.ToggleButtonGrid(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.OrderButtonGrid();
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        yield return new WaitForSeconds(.3f);
        TransitionCanvas.single.ToClear("GM TO C");
        gm.SetGameState(GameStateType.CAMP);
        gc.OrderButtonGrid();
        yield return new WaitForSeconds(.5f);


        loadingCampFromRebirth = false;

    }

    public void CallChestFromCamp(Chest _chest)
    {
        if (loadingChestFromCamp) return;
        loadingChestFromCamp = true;
        StartCoroutine(GoToChest(_chest));
    }

    public bool loadingChestFromCamp = false;
    public IEnumerator GoToChest(Chest chestChosen)
    {
        TransitionCanvas.single.ToBlack("GM TO CHEST");
        GameplayCanvas.single.lastChestClicked = chestChosen;

        yield return new WaitForSeconds(.7f);
        gc.ToggleCampUi(false);
        LevelController.Instance.AssetManager.HideCampPackage();
        LevelController.Instance.AssetManager.HidePlayer();
        gc.ToggleFusionBarUI(false);
        gc.ToggleButtonGrid(false);
        LevelController.Instance.AssetManager.SpawnGeneralObj("ChestPackage");

        //verify it's loaded before allowing clear of the overlay
        while (ChestPackage.single == null) yield return null;


        TransitionCanvas.single.ToClear("GM TO CHEST");
        loadingChestFromCamp = false;
        gm.SetGameState(GameStateType.CHEST_PACKAGE);

    }


    public void CallCampFromCashMall()
    {
        if (loadingCampFromCashMall) return;
        loadingCampFromCashMall = true;
        StartCoroutine(GoToCampFromCashMall());
    }
    bool loadingCampFromCashMall = false;
    public IEnumerator GoToCampFromCashMall()
    {
        TransitionCanvas.single.ToBlack("GM TO CashMall");
        //GameplayCanvas.single.lastChestClicked = chestChosen;
        CameraEffects.single.SetOriginalCameraPosition();
        yield return new WaitForSeconds(.7f);
        Destroy(CashMallPackage.single.gameObject);

        if (GameObject.FindGameObjectWithTag("CherryChest"))
        {
            Destroy(GameObject.FindGameObjectWithTag("CherryChest"));
        }
        gc.ToggleCampUi(true);
        gc.ToggleFusionBarUI(true);
        gc.ToggleButtonGrid(true);
        gc.ToggleHpXpLevelUi(true);
        gc.ToggleChestButtonUI(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.OrderButtonGrid();
        LevelController.Instance.AssetManager.HideMapPackage();
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.skillsPanel.SetActive(false);

        yield return new WaitForSeconds(.3f);
        TransitionCanvas.single.ToClear("GM TO CHEST");
        gm.SetGameState(GameStateType.CAMP);
        gc.OrderButtonGrid();
        yield return new WaitForSeconds(.5f);
        loadingCampFromCashMall = false;
    }




    public void CallCampFromXpBooster()
    {
        if (loadingCampFromXpBooster) return;
        loadingCampFromXpBooster = true;
        StartCoroutine(GoToCampFromXpBooster());
    }
    bool loadingCampFromXpBooster = false;
    public IEnumerator GoToCampFromXpBooster()
    {
        TransitionCanvas.single.ToBlack("GM TO CashMall");
        //GameplayCanvas.single.lastChestClicked = chestChosen;
        yield return new WaitForSeconds(.7f);
        Destroy(XPBoosterPackage.single.gameObject);
        gc.ToggleCampUi(true);
        gc.ToggleFusionBarUI(true);
        gc.ToggleButtonGrid(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.OrderButtonGrid();
        LevelController.Instance.AssetManager.HideMapPackage();

        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.skillsPanel.SetActive(false);

        yield return new WaitForSeconds(.3f);
        TransitionCanvas.single.ToClear("GM TO CHEST");
        gm.SetGameState(GameStateType.CAMP);
        gc.OrderButtonGrid();
        yield return new WaitForSeconds(.5f);

        loadingCampFromXpBooster = false;

    }
    /// <summary>
    /// //
    /// </summary>
    /// <param name="getRewards"></param>

    public void CallCampFromChest(bool getRewards = false)
    {
        if (loadingCampFromChest) return;
        loadingCampFromChest = true;
        StartCoroutine(GoToCampFromChest(getRewards));
    }
    bool loadingCampFromChest = false;

    public IEnumerator GoToCampFromChest(bool getRewards)
    {
        TransitionCanvas.single.ToBlack("GM TO CHEST");
        //GameplayCanvas.single.lastChestClicked = chestChosen;
        yield return new WaitForSeconds(.7f);
        Destroy(ChestPackage.single.gameObject);
        gc.ToggleCampUi(true);
        gc.ToggleFusionBarUI(true);
        gc.ToggleButtonGrid(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.OrderButtonGrid();
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        yield return new WaitForSeconds(.3f);
        TransitionCanvas.single.ToClear("GM TO CHEST");
        gm.SetGameState(GameStateType.CAMP);
        gc.OrderButtonGrid();
        yield return new WaitForSeconds(.5f);

        if (getRewards) FromChestRewards();

        loadingCampFromChest = false;

    }


    void FromChestRewards()
    {
        Chest next = GameplayCanvas.single.lastChestClicked;
        List<Item> nextListOfItems = next.possibleRewards;

        if (next.chestGrade == ChestGrade.GOLD)
        {
            nextListOfItems = next.possibleGoldRewards;
        }
        if (next.chestGrade == ChestGrade.DIAMOND)
        {
            nextListOfItems = next.possibleDiamondRewards;
        }


        foreach (Item item in nextListOfItems)
        {
            float chance = Random.Range(0, 1f);
            if (chance <= item.m_ChanceDrop)
            {
                Item newItem = InventoryManager.single.CreateItemStats(item);
                newItem.m_ItemFinalTitle = InventoryManager.single.GetRandomName(newItem);
                newItem.uniqueId = Random.Range(0, 999999);
                InventoryManager.single.AddItem(newItem);
                GameplayCanvas.single.ShowItemPopUp(newItem);
            }
        }

    }


    public IEnumerator GoToLastBattle()
    {
        TransitionCanvas.single.ToBlack("GM goto lastbattle");

        yield return new WaitForSeconds(.7f);

        while (waitingForBattleCleanup == true)
        {
            yield return null;
        }
        gc.skillsPanel.SetActive(false);
        CameraEffects.single.SetOriginalCameraPosition();
        gc.inventoryPanel.SetActive(false);
        LevelController.Instance.AssetManager.HideCampPackage();
        LevelController.Instance.AssetManager.HidePlayer();
        gc.victoryPanel.SetActive(false);
        gc.defeatPanel.SetActive(false);
        if (LevelController.Instance.AssetManager.mapPackageObj != null) LevelController.Instance.AssetManager.mapPackageObj.SetActive(false);
        gc.ToggleFusionBarUI(false);

        WaveManager.single.CallStartLevel(gm.lastBattleKey,()=> {
            gc.ToggleButtonGrid(false);
            TransitionCanvas.single.ToClear("Gm to camp");
        });

        Assets.Scripts.Services.Ads.AdManager.Instance.IsVideoAdReady();
    }

    bool loadingCampFromSkills = false;
    public void CallCampFromSkills()
    {
        if (loadingCampFromSkills) return;
        loadingCampFromSkills = true;
        StartCoroutine(GoToCampFromSkills());
    }
    public IEnumerator GoToCampFromSkills()
    {
        TransitionCanvas.single.ToBlack("GM goto menu");
        yield return new WaitForSeconds(.7f);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.skillsPanel.SetActive(false);
        TransitionCanvas.single.ToClear("GM goto menu");
        loadingCampFromSkills = false;
        gm.gameState = GameStateType.CAMP;
        GameplayCanvas.single.OrderButtonGrid();

    }



    //public IEnumerator GoToMenuFromCamp()
    //{
    //    TransitionCanvas.single.ToBlack("GM goto menu");
    //    yield return new WaitForSeconds(.7f);
    //    gc.ToggleBattleUi(false);
    //    gc.ToggleCampUi(false);
    //    gc.ToggleHpXpLevelUi(false);
    //    am.battleCampCamera.SetActive(false);
    //    AssetManager.single.HideCampPackage();
    //    AssetManager.single.HidePlayer();
    //    TransitionCanvas.single.ToClear("GM goto menu");
    //}


    public IEnumerator GoToCampFromInventory()
    {
        TransitionCanvas.single.ToBlack("GM goto menu");
        yield return new WaitForSeconds(.7f);
        CameraEffects.single.SetOriginalCameraPosition();
        gc.inventoryPanel.SetActive(false);

        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        TransitionCanvas.single.ToClear("GM goto menu");

        gm.SetGameState(GameStateType.CAMP);

    }

    public IEnumerator GoToCampFromBattle()
    {
        TransitionCanvas.single.ToBlack("GM goto campt");
        yield return new WaitForSeconds(.7f);
        gm.BattleCleanup();

        gc.victoryPanel.SetActive(false);
        gc.defeatPanel.SetActive(false);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);

        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        gc.ToggleFusionBarUI(true);

        if (GameObject.FindGameObjectWithTag("ChestDropIn") != null) Destroy(GameObject.FindGameObjectWithTag("ChestDropIn"));//.SetActive(false);

        waitingForBattleCleanup = false;

        TransitionCanvas.single.ToClear("Gm to camp");
        gm.SetGameState(GameStateType.CAMP);

    }


    bool isToCampFromLeaveButton = false;

    public void GoToCampFromBatle()
    {
        if (isToCampFromLeaveButton) return;
        isToCampFromLeaveButton = true;
        StartCoroutine(GoToCampFromLeaveBattle_Chain());
    }
    public IEnumerator GoToCampFromLeaveBattle_Chain()
    {

        TransitionCanvas.single.ToBlack("GM goto camp from leave button");
        yield return new WaitForSeconds(.7f);
        gm.BattleCleanup();
        gc.victoryPanel.SetActive(false);
        gc.defeatPanel.SetActive(false);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);

        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        TransitionCanvas.single.ToClear("Gm to camp from leave button");
        isToCampFromLeaveButton = false;
        gm.SetGameState(GameStateType.CAMP);
    }

    public IEnumerator GoToCampFromMap()
    {
        TransitionCanvas.single.ToBlack("GM goto camp from map");
        yield return new WaitForSeconds(.7f);
        LevelController.Instance.AssetManager.HideMapPackage();
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        TransitionCanvas.single.ToClear("Gm to camp from map");
        gm.SetGameState(GameStateType.CAMP);

    }

    public IEnumerator GoToCampFromMenu()
    {
        TransitionCanvas.single.ToBlack("GM goto camp from menu");
        yield return new WaitForSeconds(.7f);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(true);
        gc.ToggleHpXpLevelUi(true);

        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        TransitionCanvas.single.ToClear("Gm to camp from menu");
        gm.SetGameState(GameStateType.CAMP);

    }

    public IEnumerator GoToMap()
    {
        TransitionCanvas.single.ToBlack("GM goto map");
        yield return new WaitForSeconds(.7f);
        CameraEffects.single.SetOriginalCameraPosition();
        gc.inventoryPanel.SetActive(false);
        gc.RefreshNotifiers();

        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(false);
        gc.ToggleHpXpLevelUi(false);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(false);
        LevelController.Instance.AssetManager.HideCampPackage();
        LevelController.Instance.AssetManager.HidePlayer();
        gc.skillsPanel.SetActive(false);

        LevelController.Instance.AssetManager.TurnOnMapPackage();
        TransitionCanvas.single.ToClear("GM goto map");
        gm.SetGameState(GameStateType.MAP);
        gc.OrderButtonGrid();
    }


    public IEnumerator GoToDefeat()
    {

        waitingForBattleCleanup = true;

        EventManager.single.CallHideLifebars();
        WaveManager.single.SetWaveseReachedByKey(WaveManager.single.currentWaveset.levelKey, WaveManager.single.waveNumber);
        gc.defeatMapName.text = WaveManager.single.currentWaveset.levelName.ToUpper();
        gc.ToggleFusionBarUI(false);
        gc.ToggleCurrencyChestButton(false, 100);
        gc.ToggleRewardVideoButton(false);
        gc.defeatPanel.SetActive(true);
        //gc.ToggleMonetizeBottom(false);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        //am.SpawnGeneralObj("ChestDropIn");
        yield return new WaitForSeconds(2.7f);
        gm.EnemyCleanup();
        gm.MapCleanup();

        LevelController.Instance.AssetManager.nextDropInGrade = ChestGrade.TURD;
        LevelController.Instance.AssetManager.SpawnGeneralObj("ChestDropIn");

        yield return new WaitForSeconds(.5f);
        waitingForBattleCleanup = false;
        gm.SetGameState(GameStateType.DEFEAT);
        StatisticsTracker.Instance.LogFlurryEvent(WaveManager.single.currentWaveset.levelName.ToUpper() + "-Fail");
        SaveManager.Instance.Save();

    }

    public Chest newChest;
    public bool waitingForBattleCleanup = false;
    public IEnumerator GoToVictory()
    {
        waitingForBattleCleanup = true;
        gc.ToggleButtonGrid(false);

        gc.ToggleCurrencyChestButton(true, gm.ChestPremiumOpenCost);

        gc.ToggleRewardVideoButton(true);
        int reached = WaveManager.single.GetWaveseReachedByKey(WaveManager.single.currentWaveset.levelKey);
        if (reached < WaveManager.single.currentWaveset.waves.Length)
        {
            Debug.Log("FIRST TIME DEFEATING THIS MAP");
            StatisticsTracker.Instance.LogFlurryEvent(WaveManager.single.currentWaveset.levelName + "-Complete");

        }

        WaveManager.single.SetWaveseReachedByKey(WaveManager.single.currentWaveset.levelKey, WaveManager.single.currentWaveset.waves.Length);
        gc.victoryMapName.text = WaveManager.single.currentWaveset.levelName.ToUpper();
        gc.crystalIcon.SetActive(false);
        gc.crystalText.text = "";
        gc.ToggleFusionBarUI(false);

        if (InventoryManager.single.chestsOwned.Count >= 4)
        {
            gc.iconForOpenNow.SetActive(false);
            gc.textForFullChest.SetActive(true);
            gc.textForOpenNowChest.SetActive(false);
            gc.textForOpenNowHeader.GetComponent<Text>().text = "Uh Oh...Chests Full";

        }
        else
        {
            gc.iconForOpenNow.SetActive(true);

            gc.textForFullChest.SetActive(false);
            gc.textForOpenNowChest.SetActive(true);
            gc.textForOpenNowHeader.SetActive(true);
            gc.textForOpenNowHeader.GetComponent<Text>().text = "Open Now";

        }


        //crystals
        float rewardCrystals = Random.Range(newChest.crystalsMin, newChest.crystalsMax);
        GameManager.single.crystals += (int)rewardCrystals;
        //gc.victoryCrystals.text = "+ " + (int)rewardCrystals;

        yield return new WaitForSeconds(.3f);
        gc.victoryPanel.SetActive(true);

        //gc.ToggleMonetizeBottom(false);
        //am.nextDropInGrade = chestGradeChosen;


        yield return new WaitForSeconds(.3f);
        LevelController.Instance.AssetManager.SpawnGeneralObj("ChestDropIn");
        gm.BattleCleanup();

        //let it get dark, then clean objects away
        waitingForBattleCleanup = false;
        gm.SetGameState(GameStateType.VICTORY);
        SaveManager.Instance.Save();
    }


    #region BOTTOM NAV BUTTONS (FROM ANYWHERE)
    public void CampButton()
    {
        gc.ToggleChestButtonUI(true);
        gc.ToggleFusionBarUI(true);
        //have the ability to go to camp from any menu
        switch (gm.gameState)
        {
            case (GameStateType.MAP):
                StartCoroutine(GoToCampFromMap());
                break;
            case (GameStateType.CHEST_PACKAGE):
                StartCoroutine(GoToCampFromChest(false));
                break;
            case (GameStateType.BATTLE):
                StartCoroutine(GoToCampFromBattle());
                break;
            case (GameStateType.SKILLS):
                Debug.Log("Skills Skills Skills");
                StartCoroutine(GoToCampFromSkills());
                break;
            case (GameStateType.INVENTORY):
                StartCoroutine(GoToCampFromInventory());
                break;
            case (GameStateType.VICTORY):
                gc.ToggleFusionBarUI(false);

                gc.GoToCampFromVictoryOrDefeat();
                break;
            case (GameStateType.DEFEAT):
                gc.ToggleFusionBarUI(false);

                gc.GoToCampFromVictoryOrDefeat();
                break;
        }
        gc.OrderButtonGrid();
    }

    public void SkillsButton()
    {
        StartCoroutine(GoToSkills());
    }

    public IEnumerator GoToSkills()
    {
        TransitionCanvas.single.ToBlack("GM goto skills");
        yield return new WaitForSeconds(.7f);

        CameraEffects.single.SetOriginalCameraPosition();
        gc.inventoryPanel.SetActive(false);
        gc.ToggleChestButtonUI(false);
        gc.ToggleFusionBarUI(true);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(false);
        gc.ToggleHpXpLevelUi(false);
        gc.skillsPanel.SetActive(true);

        if (LevelController.Instance.AssetManager.mapPackageObj != null) LevelController.Instance.AssetManager.mapPackageObj.SetActive(false);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        //CameraEffects.single.SetCameraPosition(0);
        TransitionCanvas.single.ToClear("GM goto skills");
        gm.SetGameState(GameStateType.SKILLS);
        gc.OrderButtonGrid();
        gc.RefreshCampUI();

    }

    public void CashMallButton()
    {
        StartCoroutine(GoToCashMall(false));
    }

    public void DailyCherriesButton()
    {
        StartCoroutine(GoToCashMall(true));
    }

    public IEnumerator GoToCashMall(bool dailyCherriesOnly = false)
    {
        TransitionCanvas.single.ToBlack("GM goto CashMall");
        Assets.Scripts.Services.Purchasing.PurchaserManager.Instance.InitializePurchasing();
        yield return new WaitForSeconds(.7f);
        GameplayCanvas.single.victoryPanel.SetActive(false);
        if (ChestPackage.single != null) Destroy(ChestPackage.single.gameObject);
        if (XPBoosterPackage.single) Destroy(XPBoosterPackage.single.gameObject);

        if (dailyCherriesOnly) gc.dailyCherriesPop.SetActive(false);

        CameraEffects.single.SetOriginalCameraPosition();

        LevelController.Instance.AssetManager.HideMapPackage();

        gc.inventoryPanel.SetActive(false);
        gc.ToggleChestButtonUI(false);
        gc.ToggleFusionBarUI(false);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(false);
        gc.ToggleHpXpLevelUi(false);
        //gc.skillsPanel.SetActive(true);
        LevelController.Instance.AssetManager.SpawnGeneralObj("CashMallPackage");
        yield return new WaitForSeconds(.2f);

        gc.skillsPanel.SetActive(false);

        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        //CameraEffects.single.SetCameraPosition(0);
        gm.SetGameState(GameStateType.CASH_MALL);
        gc.OrderButtonGrid();
        gc.RefreshCampUI();
        TransitionCanvas.single.ToClear("GM goto CashMall");


    }

    #region XP BOOSTER BUTTON


    public void XPBoosterButton()
    {
        StartCoroutine(GoToXPBooster());
    }

    public IEnumerator GoToXPBooster()
    {
        SoundManager.Instance.PlayClip("CLICK");

        TransitionCanvas.single.ToBlack("GM goto xp booster");
        yield return new WaitForSeconds(.7f);
        //AssetManager.single.HideCampPackage();
        //AssetManager.single.HidePlayer();

        CameraEffects.single.SetOriginalCameraPosition();

        LevelController.Instance.AssetManager.HideMapPackage();

        gc.inventoryPanel.SetActive(false);
        gc.ToggleChestButtonUI(false);
        gc.ToggleFusionBarUI(false);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(false);
        gc.ToggleHpXpLevelUi(false);
        //gc.skillsPanel.SetActive(true);
        LevelController.Instance.AssetManager.SpawnGeneralObj("XPBoosterPackage");
        gc.skillsPanel.SetActive(false);

        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        //CameraEffects.single.SetCameraPosition(0);
        gm.SetGameState(GameStateType.XP_BOOST);
        gc.OrderButtonGrid();
        gc.RefreshCampUI();
        yield return new WaitForSeconds(.5f);

        TransitionCanvas.single.ToClear("GM goto CashMall");

    }

    #endregion

    public void InventoryButton()
    {
        StartCoroutine(GoToInventory());
    }

    public IEnumerator GoToInventory()
    {
        TransitionCanvas.single.ToBlack("GM goto inv");
        yield return new WaitForSeconds(.7f);
        gc.skillsPanel.SetActive(false);
        gc.ToggleChestButtonUI(false);
        gc.ToggleFusionBarUI(true);
        gc.ToggleBattleUi(false);
        gc.ToggleCampUi(false);
        gc.ToggleHpXpLevelUi(false);
        gc.inventoryPanel.SetActive(true);
        if (LevelController.Instance.AssetManager.mapPackageObj != null) LevelController.Instance.AssetManager.mapPackageObj.SetActive(false);
        LevelController.Instance.AssetManager.TurnOnCampPackage(1.4f);
        LevelController.Instance.AssetManager.TurnOnPlayer(true);
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        CameraEffects.single.SetCameraPosition(0);
        TransitionCanvas.single.ToClear("GM goto inv");

        gm.SetGameState(GameStateType.INVENTORY);
        GameplayCanvas.single.OrderButtonGrid();
    }

    public void MapButton()
    {
        //have the ability to go to skills from any menu
        StartCoroutine(GoToMap());

    }

    public void LastBattle()
    {
        StartCoroutine(GoToLastBattle());
    }
    #endregion
}
