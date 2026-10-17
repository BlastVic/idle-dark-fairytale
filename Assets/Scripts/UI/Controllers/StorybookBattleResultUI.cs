using System.Collections;
using System.Linq;
using IdleKnightHero.UI;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Ads;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bindings and interaction for the authored victory/defeat prefabs.</summary>
public sealed class StorybookBattleResultUI : MonoBehaviour
{
    public bool victory;
    public Image chestImage;
    public Sprite normalChest, goldChest, diamondChest;
    public Sprite ChestSprite(ChestGrade grade) => grade == ChestGrade.NORMAL ? normalChest : grade == ChestGrade.GOLD ? goldChest : grade == ChestGrade.DIAMOND ? diamondChest : null;
    public static string ChestName(ChestGrade grade) => grade == ChestGrade.NORMAL ? "普通宝箱" : grade == ChestGrade.GOLD ? "黄金宝箱" : grade == ChestGrade.DIAMOND ? "钻石宝箱" : "战利品宝箱";
    public StorybookMapUI chapterMap;
    public Text location, rewardTitle, rewardHint, currencyCaption;
    public Button replay, home, next, advert, currency;
    public GameObject standardActions, nextActions, rewardOptions;
    public Button replaySmall, homeSmall;
    public CanvasGroup presentation;
    public bool IsBusy { get; private set; }
    public bool RewardClaimed { get; private set; }
    bool opening;
    int session;
    void Awake()
    {
        replay.onClick.AddListener(()=>Navigate(false,false)); home.onClick.AddListener(()=>Navigate(false,true));
        replaySmall.onClick.AddListener(()=>Navigate(false,false));homeSmall.onClick.AddListener(()=>Navigate(false,true));
        next.onClick.AddListener(()=>Navigate(true,false));
        advert.onClick.AddListener(OpenAdvert); currency.onClick.AddListener(OpenCurrency);
    }
    void OnEnable(){AdManager.AdNotFinished+=AdCancelled;session++;IsBusy=false;opening=false;RewardClaimed=false;Refresh();StartCoroutine(Entrance());}
    void OnDisable(){AdManager.AdNotFinished-=AdCancelled;session++;StopAllCoroutines();}
    IEnumerator Entrance(){presentation.alpha=0;for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){presentation.alpha=Mathf.Clamp01(t/.2f);yield return null;}presentation.alpha=1;}
    public string NextKey()
    {
        var wm=WaveManager.single;if(!victory||!chapterMap||wm==null||wm.currentWaveset==null)return null;
        int current=System.Array.FindIndex(chapterMap.nodes,n=>n.levelKey==wm.currentWaveset.levelKey&&!n.IsStory);
        if(current<0||wm.GetWaveseReachedByKey(wm.currentWaveset.levelKey)<wm.currentWaveset.waves.Length)return null;
        for(int i=current+1;i<chapterMap.nodes.Length;i++){
            var n=chapterMap.nodes[i];if(n.IsStory)continue;
            for(var p=n.transform;p&&p!=chapterMap.transform;p=p.parent)if(!p.gameObject.activeSelf)return null;
            var data=chapterMap.Data(i);
            return data!=null&&data.waves!=null&&data.waves.Length>0&&chapterMap.IsUnlocked(i)?data.levelKey:null;
        }
        return null;
    }
    public void Refresh()
    {
        if(!GameManager.single||!WaveManager.single||WaveManager.single.currentWaveset==null)return;
        var drop=Router.single.newChest;
        if(chestImage){chestImage.sprite=drop!=null?ChestSprite(drop.chestGrade):null;chestImage.enabled=victory&&chestImage.sprite!=null;}
        var w=WaveManager.single.currentWaveset;
        int index=chapterMap?System.Array.FindIndex(chapterMap.nodes,n=>!n.IsStory&&n.levelKey==w.levelKey):-1;
        location.text=index>=0?"1-"+chapterMap.nodes.Take(index+1).Count(n=>!n.IsStory)+"  "+chapterMap.nodes[index].displayName:w.levelName;
        bool hasNext=!string.IsNullOrEmpty(NextKey());nextActions.SetActive(hasNext);standardActions.SetActive(!hasNext);
        bool ready=!IsBusy&&!opening&&!Router.single.waitingForBattleCleanup;
        foreach(var b in new[]{replay,home,next,replaySmall,homeSmall})b.interactable=ready;
        advert.interactable=currency.interactable=ready&&!RewardClaimed;
        if(victory&&!RewardClaimed&&!opening){
            bool full=InventoryManager.single.chestsOwned.Count>=4;
            rewardTitle.text=drop!=null?ChestName(drop.chestGrade):"战利品宝箱";
            rewardHint.text=full?"宝箱栏已满 · 可立即开启本次宝箱":"立即开启 · 有机会获得 2 倍或 3 倍奖励";
            currencyCaption.text="消耗 "+GameManager.single.ChestPremiumOpenCost+" 货币开启";
        }
        rewardOptions.SetActive(victory&&!RewardClaimed);
    }
    void Update(){Refresh();}
    void Navigate(bool advance,bool camp)
    {
        if(IsBusy||opening||Router.single.waitingForBattleCleanup)return;
        string key=advance?NextKey():null;if(advance&&string.IsNullOrEmpty(key))return;
        IsBusy=true;Refresh();
        if(advance)GameManager.single.lastBattleKey=key;
        if(camp)Router.single.StartCoroutine(Router.single.GoToCampFromBattle());else GameplayCanvas.single.ReplayButtonClicked();
    }
    void OpenCurrency()
    {
        if(!CanOpen())return;
        var gm=GameManager.single;
        if(gm.premiumCurrency<gm.ChestPremiumOpenCost){rewardHint.text="货币不足";StartCoroutine(Notice());return;}
        gm.premiumCurrency-=gm.ChestPremiumOpenCost;ClaimReward();
    }
    IEnumerator Notice(){opening=true;yield return new WaitForSecondsRealtime(1.5f);opening=false;}
    bool CanOpen()=>Router.single.newChest!=null&&victory&&!RewardClaimed&&!opening&&!IsBusy&&!Router.single.waitingForBattleCleanup;
    void OpenAdvert()
    {
        if(!CanOpen())return;opening=true;int token=session;
        AdManager.Instance.ShowRewardedAd("BattleResult", result=>{if(this&&token==session&&!result){opening=false;rewardHint.text="广告暂不可用";StartCoroutine(Notice());}},()=>{if(this&&isActiveAndEnabled&&token==session)ClaimReward();});
    }
    void AdCancelled(string tag){if(tag=="BattleResult"){opening=false;rewardHint.text="广告未完成，未领取奖励";StartCoroutine(Notice());}}
    public void ClaimReward()
    {
        if(!victory||RewardClaimed||IsBusy)return;
        var chest=Router.single.newChest;if(chest==null){opening=false;return;}
        RewardClaimed=true;opening=false;
        int multiplier=Random.value<=.5f?3:2;
        int amount=(int)(Random.Range(chest.crystalsMin,chest.crystalsMax)*multiplier);
        GameManager.single.crystals+=amount;
        rewardTitle.text="获得水晶 +"+amount;rewardHint.text=multiplier+" 倍奖励 · 已领取";
        SoundManager.Instance.PlayClip("CHEST_DROP_CRYSTALS");SaveManager.Instance.Save();Refresh();
    }
}
