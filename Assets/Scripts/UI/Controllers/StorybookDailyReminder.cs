using System;
using IdleKnightHero.UI;
using Assets.Scripts.Services.Ads;
using UnityEngine;
using UnityEngine.UI;

public sealed class StorybookDailyReminder : MonoBehaviour
{
    public Button close,open,advert;
    public Text amount,notice,adCaption,claimCaption;
    public CanvasGroup group;
    public bool Busy {get;private set;}
    int request;
    DateTime shownDay;
    void Awake(){close.onClick.AddListener(Dismiss);open.onClick.AddListener(Claim);advert.onClick.AddListener(WatchAd);}
    void OnEnable(){AdManager.AdNotFinished+=AdCancelled;}
    void OnDisable(){AdManager.AdNotFinished-=AdCancelled;request++;Busy=false;}
    public void Show(){request++;Busy=false;shownDay=DateTime.Today;amount.text=GameManager.single.dailyCherriesAmount.ToString();StorybookPopupMotion.Show(gameObject);notice.text="每日可领取一次";Refresh();}
    void Update(){if(Input.GetKeyDown(KeyCode.Escape))Dismiss();}
    void Refresh(){bool claimed=StorybookDailyReward.ClaimedToday;open.interactable=advert.interactable=!Busy&&!claimed;close.interactable=!Busy;adCaption.text="看广告领双倍";claimCaption.text=claimed?"今日已领取":"直接领取";}
    // Internal hiding (e.g. tutorial priority) must not count as a player's dismissal.
    public void Hide(){request++;Busy=false;StorybookPopupMotion.Hide(gameObject);}
    public void Dismiss(){if(Busy)return;StorybookDailyReward.DismissToday();Hide();}
    public void Claim(){if(!isActiveAndEnabled||Busy)return;Grant(1);}
    void Grant(int multiplier){
        if(shownDay!=DateTime.Today){Show();notice.text="日期已更新，请重新领取";return;}
        bool granted=StorybookDailyReward.TryClaim(multiplier);Refresh();
        notice.text=granted?"已领取血月结晶 × "+(GameManager.single.dailyCherriesAmount*multiplier):"今日补给已领取";
        if(granted){amount.text=(GameManager.single.dailyCherriesAmount*multiplier).ToString();SoundManager.Instance.PlayClip("CHERRY");}
    }
    public void WatchAd(){
        if(!isActiveAndEnabled||Busy||StorybookDailyReward.ClaimedToday)return;
        Busy=true;int id=++request;notice.text="正在等待广告…";Refresh();
        AdManager.Instance.ShowRewardedAd("DailySupply",ready=>{
            if(!this||id!=request)return;
            if(!ready){request++;Busy=false;notice.text="广告暂不可用，可直接领取";Refresh();}
        },()=>CompleteAd(id));
    }
    void CompleteAd(int id){if(!this||!isActiveAndEnabled||id!=request||!Busy)return;request++;Busy=false;Grant(2);}
    void AdCancelled(string tag){if(tag!="DailySupply"||!Busy)return;request++;Busy=false;notice.text="广告未完成，可重试或直接领取";Refresh();}
}
