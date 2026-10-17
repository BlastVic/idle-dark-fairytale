using System;
using Assets.Scripts.Game.Enum;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Ads;

/// <summary>Data binding for the authored shop prefab. Never constructs UI nodes.</summary>
public sealed class StorybookShopUI : MonoBehaviour
{
    public Text balance,playerLevel,subjectNumber,price80,price500,status,rewardAmount;
    public Button buy80,buy500,back;
    public Button dailyClaim,dailyAdvert;
    public Text dailyAmount,dailyCaption,dailyAdCaption,dailyHint;
    int dailyRequest;
    bool dailyAdPending;
    DateTime dailyAdDay;
    public void FocusDaily(){if(scroll)scroll.verticalNormalizedPosition=1;}
    public Button[] navigation;
    public GameObject reward;
    public CanvasGroup rewardGroup;
    public ScrollRect scroll;
    public bool Busy {get;private set;}
    bool leaving;
#if UNITY_EDITOR
    public bool validationPrices;
#endif
    void OnEnable(){AdManager.AdNotFinished+=DailyAdCancelled;}
    void OnDisable(){AdManager.AdNotFinished-=DailyAdCancelled;dailyRequest++;if(dailyAdPending)Busy=false;dailyAdPending=false;}
    void Awake(){if(dailyAdvert)dailyAdvert.onClick.AddListener(WatchDailyAd);if(dailyClaim)dailyClaim.onClick.AddListener(()=>CashMallPackage.single.DailyCherriesUI());buy80.onClick.AddListener(()=>CashMallPackage.single.BuyProduct80());buy500.onClick.AddListener(()=>CashMallPackage.single.BuyProduct500());back.onClick.AddListener(()=>Navigate(2));for(int i=0;i<navigation.Length;i++){int id=i;if(id!=4)navigation[i].onClick.AddListener(()=>Navigate(id));}}
    void Update(){Refresh();if(Input.GetKeyDown(KeyCode.Escape)&&!Busy)Navigate(2);}
    public void Refresh()
    {
        if(!GameManager.single)return;
        if(dailyClaim){dailyClaim.interactable=!StorybookDailyReward.ClaimedToday&&!Busy&&!leaving;dailyCaption.text=StorybookDailyReward.ClaimedToday?"今日已领取":"免费领取";dailyAmount.text="血月结晶 ×"+GameManager.single.dailyCherriesAmount;}
        if(dailyAdvert){dailyAdvert.interactable=!StorybookDailyReward.ClaimedToday&&!Busy&&!leaving;dailyAdCaption.text=StorybookDailyReward.ClaimedToday?"今日已领取":"看视频领双倍";}
        balance.text="血月结晶  <color=#83180F>"+Utilities.ConvertNumber(GameManager.single.premiumCurrency)+"</color>";playerLevel.text="Lv. "+GameManager.single.playerLevel;if(subjectNumber)subjectNumber.text="No. "+GameManager.single.SubjectNumber;
        SetPrice(price80,buy80,PurchaserManager.cherries_consumable_80,"$0.99");SetPrice(price500,buy500,PurchaserManager.cherries_consumable_500,"$4.99");
        back.interactable=!Busy&&!leaving;foreach(var b in navigation)b.interactable=!Busy&&!leaving;
    }
    public void WatchDailyAd()
    {
        if(!isActiveAndEnabled||Busy||leaving||StorybookDailyReward.ClaimedToday)return;
        dailyAdPending=true;dailyAdDay=DateTime.Today;int id=++dailyRequest;
        DailyAdStatus("正在等待视频…");SetBusy(true,"");
        AdManager.Instance.ShowRewardedAd("DailySupply",ready=>{
            if(!this||id!=dailyRequest)return;
            if(!ready)EndDailyAd("视频暂不可用，可免费领取");
        },()=>CompleteDailyAd(id));
    }
    void DailyAdStatus(string message){if(dailyHint)dailyHint.text=message;}
    void EndDailyAd(string message){dailyRequest++;dailyAdPending=false;Busy=false;DailyAdStatus(message);Refresh();}
    void DailyAdCancelled(string tag){if(tag=="DailySupply"&&dailyAdPending)EndDailyAd("视频未完成，可重试或免费领取");}
    void CompleteDailyAd(int id)
    {
        if(!this||!isActiveAndEnabled||id!=dailyRequest||!dailyAdPending)return;
        EndDailyAd("每日可领取一次");
        if(dailyAdDay!=DateTime.Today){DailyAdStatus("日期已更新，请重新领取");return;}
        if(StorybookDailyReward.TryClaim(2))CashMallPackage.single.PresentReward(GameManager.single.dailyCherriesAmount*2);
        Refresh();
    }
    void SetPrice(Text label,Button button,string id,string example)
    {
        var p=PurchaserManager.StoreController?.products.WithID(id);bool ready=p!=null&&p.availableToPurchase;
        string value=ready?(p.metadata?.localizedPriceString??"暂不可用"):"暂不可用";
#if UNITY_EDITOR
        // The Editor fake store may return an empty or zero-valued placeholder price.
        if(!ready||p?.metadata==null||p.metadata.localizedPrice<=0||string.IsNullOrWhiteSpace(value))value=example;
        if(validationPrices){value=example;ready=true;}
#endif
        label.text=Busy?"处理中…":value;button.interactable=ready&&!Busy&&!leaving;
    }
    public void SetBusy(bool value,string message){Busy=value;status.text=message;Refresh();}
    public void ShowReward(int amount){Busy=true;rewardAmount.text="血月结晶 <color=#83180F>+"+amount+"</color>";StorybookPopupMotion.Show(reward);Refresh();}
    public void HideReward(){StorybookPopupMotion.Hide(reward);Busy=false;Refresh();}
    public void Navigate(int destination)
    {
        if(Busy||leaving)return;
        leaving=true;
        Refresh();
        if(destination==3)
        {
            Router.single.MapButton();
            return;
        }
        Router.single.StartCoroutine(ReturnThenNavigate(destination));
    }
    IEnumerator ReturnThenNavigate(int destination){CashMallPackage.single.CloseButton();if(destination==2)yield break;float end=Time.unscaledTime+8;while(GameManager.single.gameState!=GameStateType.CAMP&&Time.unscaledTime<end)yield return null;yield return new WaitForSecondsRealtime(.6f);if(destination==0)Router.single.InventoryButton();else if(destination==1)Router.single.SkillsButton();}
    // Navigation continuation lives on Router because this Addressable is destroyed on leaving.
    public void SetLeaving()=>leaving=true;
}
