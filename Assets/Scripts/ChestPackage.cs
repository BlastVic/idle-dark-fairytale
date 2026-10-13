using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Services.Ads;
using IdleKnightHero.UI;
using UnityEngine;

/// <summary>Owns a single chest transaction; the authored view only displays its state.</summary>
public class ChestPackage : MonoBehaviour
{
    static ChestPackage instance;
    public static ChestPackage single => instance ? instance : (instance=FindFirstObjectByType<ChestPackage>());
    public StorybookChestUI view;
    public int cherryCost=2;
    public Chest CurrentChest {get;private set;}
    public bool Opening {get;private set;}
    public bool AwaitingAd {get;private set;}
    public bool Leaving {get;private set;}
    public int RewardAmount {get;private set;}
    public DateTime UnlockAt {get;private set;}
    public bool Ready => CurrentChest!=null&&DateTime.Now>=UnlockAt;
    public bool Owned => CurrentChest!=null&&InventoryManager.single&&InventoryManager.single.chestsOwned.Any(c=>c.uniqueKey==CurrentChest.uniqueKey);
    public bool CanAct => CurrentChest!=null&&Owned&&!Opening&&!AwaitingAd&&!Leaving;
    void Awake(){instance=this;}
    void OnEnable(){AdManager.AdNotFinished+=AdCancelled;}
    void OnDisable(){AdManager.AdNotFinished-=AdCancelled;}
    void OnDestroy(){if(instance==this)instance=null;}
    public void SetMe(Chest chest)
    {
        if(Opening||AwaitingAd||Leaving)return;
        CurrentChest=chest;
        UnlockAt=DateTime.MaxValue;
        if(chest!=null&&long.TryParse(chest.rewardTime,out var binary))
            try{UnlockAt=DateTime.FromBinary(binary);}catch(ArgumentException){}
        view.Bind(this);
    }
    public void TapOpen(){if(CanAct&&Ready)Claim();}
    public void PayCherriesButton()
    {
        if(!CanAct||Ready)return;
        if(GameManager.single.premiumCurrency<cherryCost){view.Notice("血月结晶不足");return;}
        GameManager.single.premiumCurrency-=cherryCost;
        SoundManager.Instance.PlayClip("STAT_BUY");Claim();
    }
    public void WatchVidButton()
    {
        if(!CanAct||Ready)return;
        AwaitingAd=true;view.Refresh();
        AdManager.Instance.ShowRewardedAd("ChestDetail",available=>{
            if(!this||!AwaitingAd)return;
            if(!available){AwaitingAd=false;view.Notice("广告暂不可用，请稍后重试");view.Refresh();}
        },()=>{
            if(!this||!isActiveAndEnabled||!AwaitingAd)return;
            AwaitingAd=false;if(CanAct)Claim();
        });
    }
    void AdCancelled(string tag){if(tag!="ChestDetail"||!AwaitingAd)return;AwaitingAd=false;view.Notice("广告未完成，宝箱尚未开启");view.Refresh();}
    void Claim()
    {
        if(!CanAct)return;
        Opening=true;
        InventoryManager.single.RemoveChest(CurrentChest);
        float minimum=Mathf.Max(0,CurrentChest.crystalsMin),maximum=Mathf.Max(minimum,CurrentChest.crystalsMax);
        RewardAmount=Mathf.FloorToInt(UnityEngine.Random.Range(minimum,maximum));
        // Commit once; the displayed count-up is presentation only.
        GameManager.single.crystals+=RewardAmount;
        SoundManager.Instance.PlayClip("CHEST_DROP_CRYSTALS");
        view.Refresh();StartCoroutine(Complete());
    }
    IEnumerator Complete(){yield return new WaitForSecondsRealtime(3);CloseButtonWithRewards();}
    public void CloseButton(){if(Opening||AwaitingAd||Leaving)return;Leaving=true;view.Refresh();Router.single.CallCampFromChest(false);}
    public void CloseButtonWithRewards(){if(!Opening||Leaving)return;Leaving=true;view.Refresh();Router.single.CallCampFromChest(true);}
}
