using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Extentions;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Ads;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class GameplayCanvas : MonoBehaviour
{
    private static GameplayCanvas _instance;
    public static GameplayCanvas single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<GameplayCanvas>();
            return _instance;
        }
    }

    Router r;

    public Text waveText;
    [Header("Prefab-authored battle HUD (optional legacy fallback)")]
    public CursedBattleHud battleHud;
    [Header("Editable shelter home prefab")]
    public ShelterHomeUI homeUI;
    public StorybookExperienceUI experienceUI;
    public Text levelText;
    public Text fusionLevelText;
    public Text fusionPercText;
    public Text premiumCurrencyText;

    public Text xpText;
    public Text victoryMapName;
    public Text victoryCrystals;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public GameObject inventoryPanel;
    public GameObject skillsPanel;
    public Text defeatMapName;
    public MeterMover xpMeter;
    public MeterMover fusionMeter;
    public GameObject[] notifiers;
    public Chest lastChestClicked;
    [Header("Sliding Lifebar Up/Down")]
    public Vector3 upPosition, downPosition;
    public GameObject slidingLifebarStuff;
    public void SetLifebarUp()
    {
        if(slidingLifebarStuff) slidingLifebarStuff.GetComponent<RectTransform>().anchoredPosition = upPosition;
    }
    public void SetLifebarDown()
    {
        if(slidingLifebarStuff) slidingLifebarStuff.GetComponent<RectTransform>().anchoredPosition = downPosition;
    }

    public Text hpText;
    public MeterMover hpMeter;
    public Color cRed, cPurple;

    //public RectTransform hpMarker;
    //Image hpMarkerImage;
    public RectTransform[] markerends;
    GameManager gm;
    private void OnEnable()
    {
        gm = GameManager.single;
        InvokeRepeating("TryShowingXpBoosterNotification", 5, 50);
        //gm.premiumCurrency += 100;
        //hpMarkerImage = hpMarker.GetComponent<Image>();
    }

    private void Start()
    {
        r = Router.single;
    }

    #region POP UP DAILY CHERRIES
    public GameObject dailyCherriesPop;
    public Text cherriesDailyText;
    public bool nextCashMallIsDailyCherries = false;
    public void ClaimDailyCherriesButton()
    {
        Router.single.DailyCherriesButton();// (true);
    }

    public void TryShowDailyCherries(int amount = 10)
    {
        // Startup reminder scheduling is owned by the persistent StorybookDailyReward component.

    }

    #endregion

    public void SkillsButtonClicked()
    {
        Router.single.StartCoroutine(Router.single.GoToSkills());
    }

    //public void BackButtonClicked()
    //{
    //    Router.single.GoToCampFromLeaveButton();
    //}
    public GameObject xpBoosterNotification;
    public StorybookExperienceReminder experienceReminder;
    public GameObject[] bottomNavButtons; //camp / Inventory/Skills/Last Battle/World Map/cash mall
    public GameObject campNavButton, skillsNavButton, lastBattleNavButton, invNavButton, mapNavButton, cashMallButton, XpBoostButton;
    public Text xpBoosterTimerText;
    public GameObject buttonGridParent;
    public void ToggleButtonGrid(bool b)
    {
        if(buttonGridParent) buttonGridParent.SetActive(b);
    }
    public void OrderButtonGrid()
    {
        if(!buttonGridParent)return;
        GameManager gm = GameManager.single;

        //Set the natural order first, any re-ordering is done later
        campNavButton.transform.SetSiblingIndex(0);
        invNavButton.transform.SetSiblingIndex(1);
        skillsNavButton.transform.SetSiblingIndex(2);
        lastBattleNavButton.transform.SetSiblingIndex(3);
        mapNavButton.transform.SetSiblingIndex(4);
        cashMallButton.transform.SetSiblingIndex(5);

        //set default order: 0 camp / 1 inv / 2 skills / 3 last / 4 world / 5 cash
        int loop = 0;
        foreach (GameObject obj in bottomNavButtons)
        {
            obj.SetActive(true);//start w all on
            obj.GetComponent<Button>().interactable = true;//reset this
            obj.transform.SetSiblingIndex(loop);
            loop++;
        }
        //have the ability to go to camp from any menu
        switch (gm.gameState)
        {
            case (GameStateType.CAMP):
                //bottomNavButtons[0].SetActive(false);//camp button on
                campNavButton.SetActive(false);
                break;
            case (GameStateType.MAP):
                //bottomNavButtons[0].SetActive(true);//camp button on
                campNavButton.SetActive(true);
                mapNavButton.SetActive(false);
                //bottomNavButtons[4].SetActive(false);//my button off
                break;
            case (GameStateType.CHEST_PACKAGE):
                //bottomNavButtons[0].SetActive(true);//camp button on
                //bottomNavButtons[4].SetActive(false);//my button off
                break;
            case (GameStateType.BATTLE):
                break;
            case (GameStateType.SKILLS):
                campNavButton.SetActive(true);
                skillsNavButton.SetActive(false);
                //bottomNavButtons[0].SetActive(true);//camp button on
                //bottomNavButtons[2].SetActive(false);//my button off

                invNavButton.transform.SetSiblingIndex(0);
                lastBattleNavButton.transform.SetSiblingIndex(1);

                campNavButton.transform.SetSiblingIndex(2);

                mapNavButton.transform.SetSiblingIndex(3);
                cashMallButton.transform.SetSiblingIndex(4);
                break;
            case (GameStateType.INVENTORY):
                campNavButton.SetActive(true);
                invNavButton.SetActive(false);
                //bottomNavButtons[0].SetActive(true);//camp button on
                //bottomNavButtons[1].SetActive(false);//my button off
                break;
            case (GameStateType.VICTORY):
                break;
            case (GameStateType.DEFEAT):
                break;
        }
    }

    void TryShowingXpBoosterNotification()
    {
        if (experienceReminder) experienceReminder.TryShow();
    }

    bool lastBattleButtonCool = true;
    public void LastBattleButtonClicked()
    {
        if (!lastBattleButtonCool) return;
        lastBattleButtonCool = false;
        r.StartCoroutine(r.GoToLastBattle());
    }

    public bool replayButtonCool = true;
    public void ReplayButtonClicked()
    {
        if (!replayButtonCool) return;
        replayButtonCool = false;
        if (GameObject.FindGameObjectWithTag("ChestDropIn") != null) Destroy(GameObject.FindGameObjectWithTag("ChestDropIn"));//.SetActive(false);
        r.StartCoroutine(r.GoToLastBattle());

    }

    bool lastCampButtonCool = true;
    public void GoToCampFromVictoryOrDefeat()
    {
        if (!lastCampButtonCool) return;
        lastCampButtonCool = false;
        r.StartCoroutine(r.GoToCampFromBattle());
        if (GameObject.FindGameObjectWithTag("ChestDropIn") != null) Destroy(GameObject.FindGameObjectWithTag("ChestDropIn"));//.SetActive(false);
    }


    public GameObject hpXpLevelUI;
    public void ToggleHpXpLevelUi(bool b)
    {
        // The authored battle/shelter HUDs own these stats in every state, including settlement delays.
        if (hpXpLevelUI) hpXpLevelUI.SetActive(b && !battleHud && !homeUI);

        if (b)
        {
            RefreshHp();
            RefreshXp();
        }
    }
    public GameObject[] battleRelatedUI;
    public void ToggleBattleUi(bool b)
    {
        foreach (GameObject obj in battleRelatedUI)
        {
            if(obj) obj.SetActive(b && !battleHud);
        }

        if (battleHud)
        {
            battleHud.gameObject.SetActive(b);
            if (hpXpLevelUI) hpXpLevelUI.SetActive(false);
            if (b) { RefreshHp(); RefreshXp(); RefreshWaveText(); }
        }

        if (b)
        {
            SetLifebarDown();
        }
        else
        {
            SetLifebarUp();
        }
        //ToggleMonetizeBottom(b);
    }

    public CanvasGroup chestParentCg;

    public void ToggleChestButtonUI(bool b)
    {
        if(!chestParentCg)return;
        if (b)
        {
            chestParentCg.alpha = 1;
            chestParentCg.interactable = true;
            chestParentCg.blocksRaycasts = true;
        }
        else
        {
            chestParentCg.alpha = 0;

            chestParentCg.interactable = false;
            chestParentCg.blocksRaycasts = false;
        }
    }

    public CanvasGroup fusionBarCg;

    public void ToggleFusionBarUI(bool b)
    {
        if(!fusionBarCg)return;
        if (b)
        {
            fusionBarCg.alpha = 1;
            fusionBarCg.interactable = true;
            fusionBarCg.blocksRaycasts = true;
        }
        else
        {
            fusionBarCg.alpha = 0;

            fusionBarCg.interactable = false;
            fusionBarCg.blocksRaycasts = false;
        }
    }

    public GameObject[] campRelatedUI;
    public void ToggleCampUi(bool b)
    {
        if (homeUI) homeUI.gameObject.SetActive(b);

        foreach (GameObject obj in campRelatedUI)
        {
            if(obj) obj.SetActive(b);
        }

        if (b)
        {
            MusicManager.Instance.PlayMusicByClipNumber(0);//0 is default for camp

            //reset flags
            lastBattleButtonCool = true;
            lastCampButtonCool = true;
            replayButtonCool = true;
            RefreshCampUI();

            if (gm.fxpMax>0 && gm.fxpNow>=gm.fxpMax) TutorialManager.single.PlayTutorial(7);


            if (InventoryManager.single.chestsOwned.Count > 0)
            {
                TutorialManager.single.PlayTutorial(6);
            }

            //TutorialManager.single.PlayTutorial(6);


            if (gm.statPoints > 0) TutorialManager.single.PlayTutorial(2);


        }

    }

    public void RefreshHp()
    {
        if (Player.single == null) return;//dont try refresh if there is no player
        if (battleHud) battleHud.SetHealth(Player.single.ab.currentStat.hpNow, Player.single.ab.currentStat.hpMax, Player.single.ab.currentStat.absorb > 0);
        if(!hpMeter||!hpText)return;
        hpMeter.SetValue(null, Player.single.ab.currentStat.hpNow / Player.single.ab.currentStat.hpMax);
        hpText.text = Utilities.ConvertNumber(Player.single.ab.currentStat.hpNow) + " / " + Utilities.ConvertNumber(Player.single.ab.currentStat.hpMax);
        //SetMarker(Player.single.ab.currentStat.hpNow / Player.single.ab.currentStat.hpMax);
        if (Player.single.ab.currentStat.absorb > 0)
        {
            //turn life purple
            hpMeter.GetComponent<Image>().color = GameplayCanvas.single.cPurple;
        }
        else
        {
            hpMeter.GetComponent<Image>().color = GameplayCanvas.single.cRed;
        }
    }

    public void FillHp()
    {
        if (Player.single == null) return;//dont try refresh if there is no player
        if (battleHud) battleHud.SetHealth(Player.single.ab.currentStat.hpNow, Player.single.ab.currentStat.hpMax, Player.single.ab.currentStat.absorb > 0);
        if(!hpMeter||!hpText)return;
        hpMeter.GetComponent<Image>().fillAmount = 1;
        hpText.text = Player.single.ab.currentStat.hpNow + " / " + Player.single.ab.currentStat.hpMax;
        //SetMarker(Player.single.ab.currentStat.hpNow / Player.single.ab.currentStat.hpMax);
    }

    public void RefreshXp()
    {
        if (battleHud) battleHud.SetExperience(gm.playerLevel, gm.xpNow, gm.xpMax);
        if(!xpMeter||!xpText||!levelText)return;
        xpMeter.SetValue(null, gm.xpNow / gm.xpMax);
        xpText.text = (int)(gm.xpNow / gm.xpMax * 100) + "%";
        levelText.text = gm.playerLevel.ToString();
        //SetMarker(p.ab.currentStat.hpNow / p.ab.currentStat.hpMax);
    }

    public GameObject chestPrefab;
    public void RefreshChests()
    {
        if(!chestContainer||!chestPrefab)return;
        ClearChests();
        foreach (Chest c in InventoryManager.single.chestsOwned)
        {
            ChestButton cb = GameObject.Instantiate(chestPrefab, chestContainer.transform).GetComponent<ChestButton>();
            cb.SetMe(c);
        }
    }

    public GameObject chestContainer;
    void ClearChests()
    {
        foreach (Transform t in chestContainer.transform)
        {
            Destroy(t.gameObject);
        }
    }

    public void RefreshNotifiers()
    {
        if(notifiers==null||notifiers.Length<2||!notifiers[0]||!notifiers[1])return;
        if (GameManager.single.skillPoints > 0)
        {
            notifiers[0].SetActive(true);
            notifiers[0].GetComponentInChildren<Text>().text = GameManager.single.skillPoints.ToString();
        }
        else
        {
            notifiers[0].SetActive(false);
        }

        InventoryManager im = InventoryManager.single;
        if (im.accFlag || im.armorFlag || im.helmetFlag || im.shieldFlag || im.weaponFlag)
        {
            notifiers[1].SetActive(true);
        }
        else
        {
            notifiers[1].SetActive(false);
        }
    }

    public void RefreshFusion()
    {
        if(!fusionPercText||!fusionMeter||!fusionLevelText)return;
        float fusionPerc = (GameManager.single.fxpNow / GameManager.single.fxpMax) * 100;
        fusionPercText.text = Utilities.ConvertNumber(fusionPerc) + "%";
        if (fusionPerc >= 100) fusionPercText.text = "Maxed!";
        fusionMeter.SetValue(null, gm.fxpNow / gm.fxpMax);
        fusionLevelText.text = "Lv " + gm.fusionLevel.ToString();
        //SetMarker(p.ab.currentStat.hpNow / p.ab.currentStat.hpMax);

        ToggleFusionBoostButton(false);

        if (GameManager.single.fxpNow >= GameManager.single.fxpMax)
        {
            ToggleFusionButton(true);
        }
        else
        {
            ToggleFusionButton(false);
            float remain = gm.fxpMax - gm.fxpNow;
            if (remain < gm.crystals) ToggleFusionBoostButton(true);

        }
    }

    public void FusionBoostClicked()
    {
        r.CallFusionFromCamp();
    }

    //public GameObject monetizeBottom;
    //public void ToggleMonetizeBottom(bool b)
    //{
    //    monetizeBottom.SetActive(b);
    //}

    public StorybookLevelUpUI levelUpUI;

    public void PlayLevelUp()
    {
        if (levelUpUI)
        {
            levelUpUI.Present(GameManager.single.playerLevel);
            RefreshXp();
            RefreshCampUI();
            return;
        }
    }

    public void RefreshWaveText()
    {
        if (WaveManager.single.currentWaveset == null) return;
        if(waveText) waveText.text = (WaveManager.single.waveNumber + 1) + "/" + WaveManager.single.currentWaveset.waves.Length;
        if (battleHud) battleHud.SetWave(WaveManager.single.waveNumber + 1, WaveManager.single.currentWaveset.waves.Length);
    }

    /*
    void SetMarker(float perc)
    {
        if (perc > .98f || perc < .02f)
        {
            hpMarkerImage.enabled = false;
            return;
        }
        else
        {
            hpMarkerImage.enabled = true;
        }
        float dist = markerends[1].localPosition.x - markerends[0].localPosition.x;
        float addOn = dist * perc;

        float val = addOn + markerends[0].localPosition.x;
        if (float.IsNaN(val)) return;//not a number - dont set the bar yet
        hpMarker.localPosition = new Vector3(addOn + markerends[0].localPosition.x, hpMarker.localPosition.y);
    }
    */

    public Text statPoints;
    public StatBoxUI dmgBox, defBox, atkSpdBox, critBox, critDmgBox, hpBox;
    public void RefreshCampUI()
    {
        EventManager.single.CallUIRefresh("RebirthButton");
        GameManager.single.CalcPlayer();//first get player up to speed! then update the numbers
        if(statPoints) statPoints.text = gm.statPoints.ToString();
        if(dmgBox) dmgBox.RefreshMe();
        if(atkSpdBox) atkSpdBox.RefreshMe();
        if(defBox) defBox.RefreshMe();
        if(critBox) critBox.RefreshMe();
        if(critDmgBox) critDmgBox.RefreshMe();
        if(hpBox) hpBox.RefreshMe();
        RefreshFusion();
        RefreshNotifiers();
        RefreshChests();
        RefreshCrystals();
        //refresh all the stat boxes

        if (InventoryManager.single.GetTotalItemCount() > 0) TutorialManager.single.PlayTutorial(3);
        if (WaveManager.single.GetWaveseReachedByKey("Mount Zion") == 4 && !GameManager.single.thingsSeenPermanent.Contains("PLEASE RATE"))
        {
            Debug.Log("You've beaten mt zion - need to try a rating pop up");
            GameManager.single.thingsSeenPermanent.Add("PLEASE RATE");
#if UNITY_IOS && !UNITY_EDITOR
UnityEngine.iOS.Device.RequestStoreReview();
#endif

        }

        //TryShowDailyCherries();

    }

    public Text crystalsText;
    void RefreshCrystals()
    {
        if(crystalsText) crystalsText.text = Utilities.ConvertNumber(gm.crystals);
        if(premiumCurrencyText) premiumCurrencyText.text = Utilities.ConvertNumber(gm.premiumCurrency);
    }

    public GameObject itemPopUp;
    public StorybookItemToast itemToast;
    public List<Item> itemPopUpQueue;
    public float itemPopUpPadding = 25;
    public void ShowItemPopUp(Item item)
    {
        if (item == null) return;

        if (itemPopUp.activeSelf)
        {
            //Debug.Log("Item Pop Up is Busy");
            itemPopUpQueue.Add(item);
            return;
        }
        if (itemToast)
        {
            itemToast.Present(item);
            SoundManager.Instance.PlayClip("ITEM_DROP");
            return;
        }
        itemPopUp.SetActive(true);
        SoundManager.Instance.PlayClip("ITEM_DROP");
        itemPopUp.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = InventoryManager.single.GetSpriteIcon(item.m_IconName);

        itemPopUp.transform.GetChild(0).transform.GetChild(1).GetComponent<Text>().text = item.m_ItemFinalTitle;
    }

    public void ItemPopUpEnded()
    {

        //Debug.Log("Check Queue");
        if (itemPopUpQueue.Count > 0)
        {
            //Debug.Log("There is item in Queue");
            itemPopUp.SetActive(false);
            ShowItemPopUp(itemPopUpQueue[0]);
            itemPopUpQueue.RemoveAt(0);
        }
        else
        {
            itemPopUp.SetActive(false);
        }

        RefreshCampUI();
    }


    public void SettingsButton()
    {

    }

    public GameObject fusionButtonObj, fusionBoostButtonObj, fusionBoostGrayObj;
    public void ToggleFusionBoostButton(bool b)
    {
        if(fusionBoostButtonObj) fusionBoostButtonObj.SetActive(b);
        if(fusionBoostGrayObj) fusionBoostGrayObj.SetActive(!b);
    }
    public void ToggleFusionButton(bool b)
    {
        if(fusionButtonObj) fusionButtonObj.SetActive(b);
    }
    public void FusionButton()
    {
        GameManager.single.FusionLevelUpEvent();
    }

    public void InventoryButton()
    {
        r.StartCoroutine(r.GoToInventory());

    }

    public bool nextIsRanOutOfCherries = false;

    // Compatibility for the advertisement service; rewards belong to the new result UI.
    public void StartVictoryChest(bool issueRewards = true)
    {
        if (!issueRewards || !victoryPanel || !victoryPanel.activeInHierarchy) return;
        var result = victoryPanel.GetComponent<StorybookBattleResultUI>();
        if (result) result.ClaimReward();
    }
}
