using System;
using System.Collections;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Purchasing;
using IdleKnightHero.UI;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Only binds data and interactions. Every visual is a saved, editable prefab child.</summary>
public sealed class ShelterHomeUI : MonoBehaviour
{
    public Text levelText, numberText, hpText, xpText, crystalsText, cherriesText, pointsText, skillBadge;
    public Image hpFill, xpFill;
    public Button settingsButton, battleButton, collapseButton;
    public Button[] navigationButtons, functionButtons, statButtons, chestButtons;
    public Text[] statValues, chestLabels;
    public Image[] chestImages;
    public GameObject[] chestReady;
    public Image[] chestStatusPlates;
    public Sprite[] chestSprites;
    public Sprite chestReadyPlate,chestTimerPlate,chestReadyInterior,chestTimerInterior;
    public Material chestReadyFillMaterial,chestTimerFillMaterial;
    public Vector2 chestReadyPlateSize,chestTimerPlateSize;
    public float chestReadyPlateTop,chestTimerPlateTop;
    public GameObject[] chestEmptyFrames;
    public GameObject rebirthLock;
    public RectTransform attributePanel;
    public CanvasGroup attributeBody;
    public Text collapseText;
    public RectTransform collapseArrow;
    public float expandedHeight = 284, collapsedHeight = 64, foldDuration = .2f;
    public GameObject modal;
    public CanvasGroup modalGroup;
    public Text modalTitle, modalBody, modalActionText;
    public Button modalClose, modalAction;
    public GameObject audioControls;
    public StorybookSettingsUI settingsUI;
    public Slider musicSlider, soundSlider;
    public Text musicValue, soundValue;
    public bool attributesExpanded = true;
    [Tooltip("Shown until the narrative numbering rules are implemented. Editable, not a death counter.")]
    public string subjectNumber = "001";
    readonly LevelTypeEnum[] types = { LevelTypeEnum.HP, LevelTypeEnum.DMG, LevelTypeEnum.DEF, LevelTypeEnum.ATK_SPD, LevelTypeEnum.CRIT, LevelTypeEnum.CRIT_DMG };
    int selectedFunction = -1;
    bool navigating;
    float refreshAt;
    Coroutine fold, popup;
    public bool IsNavigating => navigating;

    void Awake()
    {
        settingsButton.onClick.AddListener(OpenSettings);
        battleButton.onClick.AddListener(() => Navigate(() => GameplayCanvas.single.LastBattleButtonClicked()));
        collapseButton.onClick.AddListener(ToggleAttributes);
        for (int i=0;i<6;i++) { int index=i; statButtons[i].onClick.AddListener(()=>BuyStat(index)); }
        for (int i=0;i<4;i++) { int index=i; chestButtons[i].onClick.AddListener(()=>OpenChest(index)); }
        for (int i=0;i<3;i++) { int index=i; functionButtons[i].onClick.AddListener(()=>OpenFunction(index)); }
        navigationButtons[0].onClick.AddListener(()=>Navigate(()=>Router.single.InventoryButton()));
        navigationButtons[1].onClick.AddListener(()=>Navigate(()=>Router.single.SkillsButton()));
        navigationButtons[2].onClick.AddListener(CloseModal);
        navigationButtons[3].onClick.AddListener(()=>Navigate(()=>Router.single.MapButton()));
        navigationButtons[4].onClick.AddListener(()=>Navigate(()=>Router.single.CashMallButton()));
        modalClose.onClick.AddListener(CloseModal);
        modalAction.onClick.AddListener(ExecuteFunction);
        musicSlider.onValueChanged.AddListener(SetMusic);
        soundSlider.onValueChanged.AddListener(SetSound);
    }
    void Start()
    {
        if(PlayerPrefs.HasKey("Shelter.Music")) SoundManager.Instance.musicLevel=PlayerPrefs.GetFloat("Shelter.Music");
        if(PlayerPrefs.HasKey("Shelter.Sound")) SoundManager.Instance.soundLevel=PlayerPrefs.GetFloat("Shelter.Sound");
        SoundManager.Instance.SetLevels();
    }
    static void StoreAudio(string key,float value)
    {
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false)) return;
#endif
        PlayerPrefs.SetFloat(key,value);PlayerPrefs.Save();
    }
    void OnEnable()
    {
        navigating=false; refreshAt=0;
        if (modal) modal.SetActive(false);
        if(settingsUI)settingsUI.Close();
        ApplyFold(attributesExpanded ? 1 : 0);
    }
    void OnDisable() { StopAllCoroutines(); fold=null; popup=null; navigating=false; }
    void Update()
    {
        if (Time.unscaledTime < refreshAt) return;
        refreshAt=Time.unscaledTime+.15f;
        Refresh();
    }
    public void Refresh()
    {
        var gm=GameManager.single;
        if (!gm || gm.currentPlayerStat==null || !LevelController.Instance || !InventoryManager.single) return;
        levelText.text="Lv. "+gm.playerLevel;
        numberText.text="No. "+subjectNumber;
        var stat=gm.currentPlayerStat;
        float health=Player.single ? Player.single.ab.currentStat.hpNow : stat.hpMax;
        hpText.text=Utilities.ConvertNumber(health)+" / "+Utilities.ConvertNumber(stat.hpMax);
        hpFill.fillAmount=stat.hpMax>0 ? Mathf.Clamp01(health/stat.hpMax) : 0;
        xpFill.fillAmount=gm.xpMax>0 ? Mathf.Clamp01(gm.xpNow/gm.xpMax):0;
        xpText.text=Mathf.FloorToInt(xpFill.fillAmount*100)+"%";
        crystalsText.text=Utilities.ConvertNumber(gm.crystals); cherriesText.text=Utilities.ConvertNumber(gm.premiumCurrency);
        pointsText.text="可用属性点  <color=#761E20><size=52>"+gm.statPoints+"</size></color>";
        float[] values={stat.hpMax,stat.dmg,stat.def,stat.atkSpd,stat.crit,stat.critDmg};
        for(int i=0;i<6;i++) {
            statValues[i].text=Utilities.ConvertNumber(values[i])+(i>=3?"%":"");
            statButtons[i].interactable=!navigating && CanBuy(i);
            statButtons[i].GetComponentInChildren<Text>().color=statButtons[i].interactable?Color.black:new Color(.4f,.37f,.33f);
        }
        skillBadge.gameObject.SetActive(true);skillBadge.transform.parent.gameObject.SetActive(gm.skillPoints>0); skillBadge.text=gm.skillPoints.ToString();
        rebirthLock.SetActive(!LevelController.Instance.IsNextRebirthReady());
        var chests=InventoryManager.single.chestsOwned;
        for(int i=0;i<4;i++) {
            bool owned=i<chests.Count;
            chestButtons[i].interactable=owned&&!navigating;
            chestImages[i].color=owned?Color.white:new Color(1,1,1,.25f);
            if(chestEmptyFrames!=null&&i<chestEmptyFrames.Length){chestImages[i].enabled=owned;chestEmptyFrames[i].SetActive(!owned);}
            chestReady[i].SetActive(false);
            if(chestStatusPlates!=null&&i<chestStatusPlates.Length)chestStatusPlates[i].gameObject.SetActive(owned);
            if(!owned){chestLabels[i].text="空位";chestLabels[i].color=new Color(.18f,.13f,.10f);if(chestStatusPlates!=null&&i<chestStatusPlates.Length)chestStatusPlates[i].color=Color.clear;continue;}
            var chest=chests[i];
            chestImages[i].sprite=chestSprites[Mathf.Clamp((int)chest.chestGrade,0,2)];
            if(long.TryParse(chest.rewardTime,out long binary)) {
                var remaining=DateTime.FromBinary(binary)-DateTime.Now;
                bool ready=remaining.TotalSeconds<=0;
                chestReady[i].SetActive(ready);
                chestLabels[i].text=ready?"可领取":(remaining.TotalHours<1?string.Format("{0}:{1:00}",(int)remaining.TotalMinutes,remaining.Seconds):string.Format("{0}:{1:00}:{2:00}",(int)remaining.TotalHours,remaining.Minutes,remaining.Seconds));
                chestLabels[i].color=new Color(.98f,.94f,.84f);
                if(chestStatusPlates!=null&&i<chestStatusPlates.Length){
                    var plate=chestStatusPlates[i];
                    if(chestReadyPlate&&chestTimerPlate){plate.sprite=ready?chestReadyPlate:chestTimerPlate;plate.color=Color.white;var size=ready?chestReadyPlateSize:chestTimerPlateSize;if(size.x>0){plate.rectTransform.sizeDelta=size;plate.rectTransform.anchoredPosition=new Vector2(-size.x*.5f,-(ready?chestReadyPlateTop:chestTimerPlateTop));}var interior=plate.transform.Find("PlainPlateInterior");if(interior){var fill=interior.GetComponent<Image>();fill.sprite=ready?chestReadyInterior:chestTimerInterior;fill.material=ready?chestReadyFillMaterial:chestTimerFillMaterial;fill.color=Color.white;}}
                    else plate.color=ready?new Color(.48f,.075f,.065f):new Color(.23f,.20f,.15f);
                }
            } else chestLabels[i].text="查看";
        }
        battleButton.interactable=!navigating;
        foreach(var b in navigationButtons) b.interactable=!navigating;
        foreach(var b in functionButtons) b.interactable=!navigating;
        if(modal.activeSelf && selectedFunction>=0) RefreshFunction();
    }
    public bool CanBuy(int index)
    {
        var gm=GameManager.single;
        int cost=LevelController.Instance.GetStatLevel(types[index])>=100?2:1;
        return gm.statPoints>=cost && !PurchaserManager.Instance.LastStatPurchased(types[index]);
    }
    public void BuyStat(int index)
    {
        if(navigating || index<0 || index>=6 || !CanBuy(index)) return;
        GameManager.single.statPoints-=LevelController.Instance.GetStatLevel(types[index])>=100?2:1;
        LevelController.Instance.IncStat(types[index]);
        SoundManager.Instance.PlayClip("STAT_BUY"); Refresh();
    }
    void OpenChest(int index)
    {
        var chests=InventoryManager.single.chestsOwned;
        if(index<chests.Count) Navigate(()=>Router.single.CallChestFromCamp(chests[index]));
    }
    void Navigate(Action action)
    {
        if(navigating || GameManager.single.gameState!=GameStateType.CAMP) return;
        navigating=true; StorybookNavigation.PlayClick(); Refresh(); action();
    }
    public void ToggleAttributes()
    {
        attributesExpanded=!attributesExpanded;
        if(fold!=null) StopCoroutine(fold);
        fold=StartCoroutine(Fold());
    }
    IEnumerator Fold()
    {
        float start=Mathf.InverseLerp(collapsedHeight,expandedHeight,attributePanel.sizeDelta.y);
        float end=attributesExpanded?1:0;
        attributeBody.blocksRaycasts=false; attributeBody.interactable=false;
        for(float t=0;t<foldDuration;t+=Time.unscaledDeltaTime) { ApplyFold(Mathf.Lerp(start,end,Mathf.SmoothStep(0,1,t/foldDuration))); yield return null; }
        ApplyFold(end); fold=null;
    }
    void ApplyFold(float amount)
    {
        if(!attributePanel) return;
        attributePanel.sizeDelta=new Vector2(attributePanel.sizeDelta.x,Mathf.Lerp(collapsedHeight,expandedHeight,amount));
        attributeBody.alpha=amount;
        attributeBody.blocksRaycasts=attributeBody.interactable=amount>.99f;
        collapseText.text=attributesExpanded?"▾":"▴";
        if(collapseArrow)collapseArrow.localEulerAngles=new Vector3(0,0,attributesExpanded?0:180);
    }
    public void OpenFunction(int index)
    {
        if(index==1){if(!navigating)Router.single.XPBoosterButton();return;}
        if(navigating) return;
        if(index==0) { CloseModal(); Navigate(()=>Router.single.CallFusionFromCamp()); return; }
        selectedFunction=index; audioControls.SetActive(false); modalAction.gameObject.SetActive(true);
        RefreshFunction(); ShowModal();
    }
    void RefreshFunction()
    {
        var gm=GameManager.single;
        modalAction.interactable=!navigating;
        if(selectedFunction==0) {
            modalTitle.text="融合";
            modalBody.text="融合等级  "+gm.fusionLevel+"\n进度  "+Utilities.ConvertNumber(gm.fxpNow)+" / "+Utilities.ConvertNumber(gm.fxpMax)+"\n积累融合进度，提升成长能力。";
            modalActionText.text="查看融合";
        } else if(selectedFunction==1) {
            modalTitle.text="经验加成";
            var seconds=Math.Max(0,(gm.xpBoostEndTime-DateTime.Now).TotalSeconds);
            modalBody.text="经验获取 +33% · 每次持续 10 分钟\n可使用 50 枚血月结晶或激励视频开启。\n"+(gm.isBoostedXPActive && seconds>0 ? "剩余时间  "+TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss") : "当前未开启");
            modalActionText.text="查看加成";
        } else {
            bool ready=LevelController.Instance.IsNextRebirthReady();
            modalTitle.text="转生";
            modalBody.text="当前转生等级  "+gm.rebirthLevel+"\n"+(ready?"已满足转生条件，可进入查看详情。":"尚未达到下一次转生条件。继续升级后解锁。")+"\n转生规则沿用现有成长系统。";
            modalActionText.text=ready?"查看转生":"尚未解锁"; modalAction.interactable=ready&&!navigating;
        }
    }
    void ExecuteFunction()
    {
        if(selectedFunction==0) Navigate(()=>Router.single.CallFusionFromCamp());
        else if(selectedFunction==1) Navigate(()=>Router.single.XPBoosterButton());
        else if(selectedFunction==2 && LevelController.Instance.IsNextRebirthReady()) Navigate(()=>Router.single.CallRebirthFromCamp());
    }
    public void OpenSettings()
    {
        if(settingsUI){CloseModal();settingsUI.Open();return;}
        selectedFunction=-1; modalTitle.text="设置"; modalBody.text="";
        modalAction.gameObject.SetActive(false); audioControls.SetActive(true);
        musicSlider.SetValueWithoutNotify(SoundManager.Instance.musicLevel);
        soundSlider.SetValueWithoutNotify(SoundManager.Instance.soundLevel);
        UpdateAudioLabels(); ShowModal();
    }
    void SetMusic(float value) { SoundManager.Instance.musicLevel=value; StoreAudio("Shelter.Music",value); SoundManager.Instance.SetLevels(); UpdateAudioLabels(); }
    void SetSound(float value) { SoundManager.Instance.soundLevel=value; StoreAudio("Shelter.Sound",value); SoundManager.Instance.SetLevels(); UpdateAudioLabels(); }
    void UpdateAudioLabels() { musicValue.text=Mathf.RoundToInt(musicSlider.value*100)+"%"; soundValue.text=Mathf.RoundToInt(soundSlider.value*100)+"%"; }
    void ShowModal() { modal.SetActive(true); if(popup!=null)StopCoroutine(popup); popup=StartCoroutine(FadeModal()); }
    IEnumerator FadeModal() { modalGroup.alpha=0; for(float t=0;t<.16f;t+=Time.unscaledDeltaTime) { modalGroup.alpha=t/.16f; yield return null; } modalGroup.alpha=1; popup=null; }
    public void CloseModal() { if(popup!=null)StopCoroutine(popup); popup=null; modal.SetActive(false); }
}
