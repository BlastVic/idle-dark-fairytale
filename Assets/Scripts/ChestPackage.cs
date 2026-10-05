using System.Collections;
using System.Collections.Generic;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class ChestPackage : MonoBehaviour
{
    private static ChestPackage _instance;

    public static ChestPackage single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<ChestPackage>();
            return _instance;
        }
    }

    private void OnEnable()
    {
        crystalIcon.SetActive(false);
        crystalText.text = "";
    }

    private void Update()
    {
        if (timerText.text == "Ready!")
        {
            //this means we are ready
            buttons[0].SetActive(true);
            buttons[1].SetActive(false);
            buttons[2].SetActive(false);
        }
        else
        {
            buttons[0].SetActive(false);
        }
    }

    public Text mapName;
    public Text timerText;
    public Text chestGradeText;
    public Timer myTimer;
    Chest myChest;
    public void SetMe(Chest _chest)
    {
        myChest = _chest;
        mapName.text = myChest.chestMapName;
        chestGradeText.text = myChest.chestGrade + " CHEST";
        //add timer
        myTimer = gameObject.AddComponent<Timer>();
        myTimer.notReadyColor = Color.black;

        //string back to a long
        long restoreDateLong = System.Convert.ToInt64(myChest.rewardTime);
        //long to date
        System.DateTime restoreDate = System.DateTime.FromBinary(restoreDateLong);

        myTimer.InitializeTimerByDT(restoreDate, timerText, Color.black);
    }

    public void WatchVidButton()
    {
        Debug.Log("Play a video!");
        GetChest(); //this will be a callback
    }

    public GameObject waitForChestSilhouette;
    void GetChest()
    {
        foreach (GameObject obj in buttons)
        {
            obj.SetActive(false);
        }
        myTimer.enabled = false;
        timerText.text = "";
        waitForChestSilhouette.SetActive(false);
        ChestAnimateIn();
        InventoryManager.single.RemoveChest(myChest);
    }

    public int cherryCost = 2;
    public GameObject closeButton;
    public void PayCherriesButton()
    {

        if (GameManager.single.premiumCurrency < cherryCost)
        {
            GameplayCanvas.single.nextIsRanOutOfCherries = true;
            Router.single.CashMallButton();
            //cant afford
            return;
        }
        closeButton.SetActive(false);
        GameManager.single.premiumCurrency -= 2;
        SoundManager.Instance.PlayClip("STAT_BUY");


        Debug.Log("Pay cherries ");
        GetChest(); //this will be a callback
    }

    public void TapOpen()
    {
        Debug.Log("Tap Open");
        closeButton.SetActive(false);
        GetChest(); //this will be a callback
    }


    public GameObject[] chestGraphics;
    void ChestAnimateIn()
    {
        SoundManager.Instance.PlayClip("CHEST_DROP_CRYSTALS");
        switch (myChest.chestGrade)
        {
            case ChestGrade.NORMAL:
                chestGraphics[0].SetActive(true);
                break;
            case ChestGrade.GOLD:
                chestGraphics[1].SetActive(true);
                break;
            case ChestGrade.DIAMOND:
                chestGraphics[2].SetActive(true);
                break;
        }
        StartCoroutine(CrystalCountUp());
        Invoke("CloseButtonWithRewards", 3f);
    }

    public GameObject crystalIcon;
    public Text crystalText;
    IEnumerator CrystalCountUp()
    {
        //crystals
        float rewardCrystals = Random.Range(GameplayCanvas.single.lastChestClicked.crystalsMin, GameplayCanvas.single.lastChestClicked.crystalsMax);
        //if (GameplayCanvas.single.lastChestClicked.chestGrade == ChestGrade.GOLD)
        //{
        //    rewardCrystals *= 3;
        //}
        //if (GameplayCanvas.single.lastChestClicked.chestGrade == ChestGrade.GOLD)
        //{
        //    rewardCrystals *= 10;
        //}
        GameManager.single.crystals += (int)rewardCrystals;
        yield return new WaitForSeconds(1.7f);
        crystalIcon.SetActive(true);

        GameManager.single.crystals += rewardCrystals;//give it right away, let counter pretend to run adding
        for (int i = 0; i <= rewardCrystals; i++)
        {
            yield return new WaitForSeconds(.01f);

            crystalText.text = "+ " + i;

        }

    }


    public GameObject[] buttons;//0 tap open, 1 regular pay for, 2 watch vid
    bool closeClicked = false;
    public void CloseButton()
    {
        if (closeClicked) return;
        closeClicked = true;
        Router.single.CallCampFromChest(false);
    }

    public void CloseButtonWithRewards()
    {
        if (closeClicked) return;
        closeClicked = true;
        Router.single.CallCampFromChest(true);
    }
}
