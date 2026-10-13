using IdleKnightHero.UI;
using Assets.Scripts.Services;

using System;
using System.Collections;
using Assets.Scripts.Services.Ads;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data and interaction only. All graphics are authored in the prefab.</summary>
public sealed class StorybookExperienceUI : MonoBehaviour
{
    public Text state,timer,balance,paidCaption,adCaption,notice;
    public Button paid,advert,close;
    public Image duration,illustration;
    public Sprite idleArt,activeArt;
    public CanvasGroup presentation;
    public RectTransform paper;
    public bool Busy {get;private set;}
    int session,request;
    float nextPurchase;
    bool closing => Router.single && Router.single.ExperienceTransitioning;
    void Awake(){paid.onClick.AddListener(Purchase);advert.onClick.AddListener(WatchAd);close.onClick.AddListener(Close);}
    void OnEnable(){session++;Busy=false;nextPurchase=0;notice.text="";AdManager.AdNotFinished+=AdCancelled;presentation.alpha=1;presentation.blocksRaycasts=true;presentation.interactable=true;Refresh();}
    void OnDisable(){session++;request++;AdManager.AdNotFinished-=AdCancelled;StopAllCoroutines();Busy=false;presentation.blocksRaycasts=false;presentation.interactable=false;}
    void Update(){Refresh();if(Input.GetKeyDown(KeyCode.Escape))Close();}
    public void Show(){if(gameObject.activeSelf)return;gameObject.SetActive(true);}
    public void Refresh(){
        var gm=GameManager.single;if(!gm)return;
        double seconds=Math.Max(0,(gm.xpBoostEndTime-DateTime.Now).TotalSeconds);
        bool active=gm.HasActiveXpBoost&&seconds>0;
        state.text=active?"加成生效中":"当前未开启";
        int total=(int)Math.Ceiling(seconds);
        timer.text=active?"剩余 <color=#83180F>"+(total/60).ToString("00")+":"+(total%60).ToString("00")+"</color>":"持续 10 分钟";
        duration.fillAmount=active?Mathf.Clamp01((float)seconds/600):0;
        illustration.sprite=active?activeArt:idleArt;
        balance.text="持有血月结晶  "+Utilities.ConvertNumber(gm.premiumCurrency);
        paidCaption.text="50 血月结晶"+(active?"延长":"开启");
        adCaption.text=Busy?"等待广告完成…":"观看广告"+(active?"延长":"开启");
        paid.interactable=!Busy&&!closing&&Time.unscaledTime>=nextPurchase&&gm.premiumCurrency>=50;
        advert.interactable=!Busy&&!closing;close.interactable=!Busy&&!closing;
        if(!Busy&&!closing&&gm.premiumCurrency<50&&string.IsNullOrEmpty(notice.text))notice.text="血月结晶不足，可观看广告开启";
    }
    public void Purchase(){
        if(!isActiveAndEnabled||Busy||closing||Time.unscaledTime<nextPurchase)return;
        var gm=GameManager.single;if(gm.premiumCurrency<50){notice.text="血月结晶不足";Refresh();return;}
        nextPurchase=Time.unscaledTime+.5f;gm.premiumCurrency-=50;Grant();
    }
    void Grant(){GameManager.single.AddXPBoost();SaveManager.Instance.Save();notice.text="已增加 10 分钟经验加成";SoundManager.Instance.PlayClip("FIRE");Refresh();}
    public void WatchAd(){
        if(!isActiveAndEnabled||Busy||closing)return;
        Busy=true;notice.text="";int token=session,id=++request;Refresh();
        AdManager.Instance.ShowRewardedAd("ExperienceModal",ready=>{
            if(!this||token!=session||id!=request)return;
            if(!ready){Busy=false;request++;notice.text="广告暂不可用，请稍后重试";Refresh();}
        },()=>CompleteAd(token,id));
    }
    void CompleteAd(int token,int id){if(!this||!isActiveAndEnabled||token!=session||id!=request||!Busy)return;request++;Busy=false;Grant();}
    void AdCancelled(string tag){if(tag!="ExperienceModal"||!Busy)return;request++;Busy=false;notice.text="广告未完成，未增加时长";Refresh();}
    public void Close(){if(Busy||closing||!gameObject.activeSelf)return;Router.single.CloseExperience();}
}
