using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services.Ads;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class XPBoosterPackage : MonoBehaviour
{
    private static XPBoosterPackage _instance;
    public static XPBoosterPackage single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<XPBoosterPackage>();
            return _instance;
        }
    }

    public GameObject[] fireRelated;
    public void ToggleFire(bool b)
    {
        foreach (GameObject obj in fireRelated)
        {
            if (obj.activeSelf != b) obj.SetActive(b);
        }
    }

    private void OnEnable()
    {
        InvokeRepeating("RefreshMeter", 0, .5f);
    }

    private void OnDisable()
    {
    }

    public void RewardVideoPlayButton()
    {

    }

    private void Update()
    {
        RefreshMe();
    }

    public void CherryButton()
    {
        if (GameManager.single.premiumCurrency < 50)
        {
            GameplayCanvas.single.nextIsRanOutOfCherries = true;

            Router.single.CashMallButton();
            return;
        }
        else
        {
            GameManager.single.premiumCurrency -= 50;
            SoundManager.Instance.PlayClip("FUSION_REWARD");
        }
        StartXpBoost();
    }

    public void RewardVideoFromXPBoostPressed()
    {
        AdManager.Instance.ShowRewardedAd("XPBoosterPackage", result => { }, () =>
        {
            StartXpBoost();
        });
    }


    void StartXpBoost(bool issueRewards = true)
    {
        if (issueRewards)
        {
            SoundManager.Instance.PlayClip("FIRE");

            GameManager.single.AddXPBoost();

            if (XPBoosterPackage.single != null && XPBoosterPackage.single.xpBoostTimer != null)
            {
                //XPBoosterPackage.single.xpBoostTimer = XPBoosterPackage.single.gameObject.AddComponent<Timer>();
                XPBoosterPackage.single.xpBoostTimer.InitializeTimerByDT(GameManager.single.xpBoostEndTime, XPBoosterPackage.single.timerText, Color.white);
            }
            RefreshMe();
        }
    }

    bool closeClicked = false;
    public void CloseButton()
    {
        if (closeClicked) return;
        closeClicked = true;
        SoundManager.Instance.PlayClip("CLICK");
        Router.single.CallCampFromXpBooster();
    }

    public Image meter;
    public Text timerText;
    public Timer xpBoostTimer;
    bool menuJustOpened = true;
    void RefreshMe()
    {
        if (GameManager.single.isBoostedXPActive)
        {
            if (xpBoostTimer == null)
            {
                xpBoostTimer = gameObject.AddComponent<Timer>();
                xpBoostTimer.InitializeTimerByDT(GameManager.single.xpBoostEndTime, timerText, Color.yellow);
                xpBoostTimer.emptyisCustom = true;
                xpBoostTimer.customEmptyString = "No Active Boost";
            }

        }
        else
        {
            meter.fillAmount = 0;// (null, 0);
            timerText.text = "No Active Boost";
        }
    }

    void RefreshMeter()
    {

        if (GameManager.single.isBoostedXPActive)
        {
            TimeSpan duration = GameManager.single.xpBoostEndTime.Subtract(DateTime.Now);

            //get the total seconds left / 600 = float perc of the meter

            float totalseconds = (int)duration.TotalSeconds;


            float perc = totalseconds / 600;

            //Debug.Log("totalseconds:" + totalseconds);
            if (totalseconds <= 0) GameManager.single.isBoostedXPActive = false;//we use greater than zero to avoid abug where the number is reversed
            meter.fillAmount = perc;
            ToggleFire(true);
        }
        else
        {
            meter.fillAmount = 0;
            ToggleFire(false);
        }
    }

}
