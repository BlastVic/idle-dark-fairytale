using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Binds the persistent UGUI nodes; no runtime UI construction.</summary>
public sealed class StorybookChestUI : MonoBehaviour
{
    public Button back,pay,advert,free;
    public Text balance,title,source,timer,payCaption,notice,readyCaption,rewardCaption,hint;
    public GameObject waiting,ready,paidActions,freeActions;
    public Image chest;
    public Sprite waitingChest,readyChest;
    public Material waitingChestMaterial,readyChestMaterial;
    ChestPackage owner;
    float noticeUntil,openedAt=-1;
    void Awake()
    {
        back.onClick.AddListener(()=>owner.CloseButton());pay.onClick.AddListener(()=>owner.PayCherriesButton());
        advert.onClick.AddListener(()=>owner.WatchVidButton());free.onClick.AddListener(()=>owner.TapOpen());
    }
    public void Bind(ChestPackage value){owner=value;Refresh();}
    void Update(){Refresh();if(Input.GetKeyDown(KeyCode.Escape)&&owner)owner.CloseButton();}
    public void Notice(string value){notice.text=value;noticeUntil=Time.unscaledTime+4;}
    public void Refresh()
    {
        if(!owner||owner.CurrentChest==null)return;
        var data=owner.CurrentChest;
        title.text=data.chestGrade==ChestGrade.GOLD?"黄金宝箱":data.chestGrade==ChestGrade.DIAMOND?"钻石宝箱":"青铜宝箱";
        source.text=string.IsNullOrWhiteSpace(data.chestMapName)?"来自探索":data.chestMapName;
        balance.text=Utilities.ConvertNumber(GameManager.single.premiumCurrency);
        bool available=owner.CanAct,isReady=owner.Ready||owner.Opening;
        waiting.SetActive(!isReady);ready.SetActive(isReady);paidActions.SetActive(!isReady);freeActions.SetActive(isReady);
        chest.sprite=isReady?readyChest:waitingChest;chest.material=isReady?readyChestMaterial:waitingChestMaterial;
        double seconds=Math.Max(0,(owner.UnlockAt-DateTime.Now).TotalSeconds);
        var left=TimeSpan.FromSeconds(Math.Min(seconds,359999));
        timer.text=string.Format("{0:00}:{1:00}:{2:00}",(int)left.TotalHours,left.Minutes,left.Seconds);
        payCaption.text=owner.cherryCost+" 血月结晶 · 立即开启";
        pay.interactable=available&&!isReady&&GameManager.single.premiumCurrency>=owner.cherryCost;
        advert.interactable=available&&!isReady;free.interactable=available&&owner.Ready;
        back.interactable=!owner.Opening&&!owner.AwaitingAd&&!owner.Leaving;
        readyCaption.text=owner.Opening?"已开启宝箱":"已可免费开启";
        free.GetComponentInChildren<Text>().text=owner.Opening?"领取中…":"免费开启";
        hint.text=owner.Opening?"战利品将带回庇护所":"点击开启，领取战利品";
        if(owner.Opening){if(openedAt<0)openedAt=Time.unscaledTime;float t=Mathf.Clamp01((Time.unscaledTime-openedAt)/1.4f);rewardCaption.text="水晶 +"+Mathf.RoundToInt(owner.RewardAmount*t);}
        else rewardCaption.text="水晶";
        if(owner.AwaitingAd)notice.text="等待广告完成…";
        else if(Time.unscaledTime>noticeUntil)notice.text=!isReady&&GameManager.single.premiumCurrency<owner.cherryCost?"血月结晶不足，可等待免费开启":"";
    }
}
