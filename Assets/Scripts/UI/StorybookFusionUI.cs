using IdleKnightHero.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Scripts.Level;
using Assets.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Only binds data and interaction to the authored fusion prefab.</summary>
public sealed class StorybookFusionUI : MonoBehaviour
{
    [Serializable] public sealed class RewardSlot
    {
        public GameObject root;
        public Image icon;
        public Text title, amount;
    }
    public Text balance, level, progress, cost, actionCaption, hint, rewardHint;
    public Button action, back, returnToShelter;
    public Image progressFill, ruin;
    public GameObject readySeal, costCrystal;
    public Material referenceInk;
    public Sprite dormantRuin, chargedRuin, statIcon, skillIcon, experienceIcon;
    public RewardSlot[] rewards;
    public ScrollRect rewardScroll;
    public float fillDuration = 1.2f;
    public bool Busy { get; private set; }
    public bool Leaving { get; private set; }
    public int Cost => GameManager.single ? Mathf.CeilToInt(Mathf.Max(0, GameManager.single.fxpMax-GameManager.single.fxpNow)) : 0;
    public bool Ready => GameManager.single && GameManager.single.fxpMax > 0 && GameManager.single.fxpNow >= GameManager.single.fxpMax;
    public bool CanUpgrade => GameManager.single && GameManager.single.fusionLevel >= 0 && GameManager.single.fusionLevel < LevelDataDescriptions.FusionLevelDb.Length;
    int boundLevel = -1;
    float lastBalance = float.NaN, lastProgress = float.NaN, lastMax = float.NaN;
    void Awake()
    {
        action.onClick.AddListener(Activate);
        back.onClick.AddListener(Close);
        returnToShelter.onClick.AddListener(Close);
    }
    void OnEnable() { boundLevel = -1; Refresh(); }
    void Update()
    {
        var gm = GameManager.single;
        if (gm && !Busy && (gm.crystals != lastBalance || gm.fxpNow != lastProgress || gm.fxpMax != lastMax || gm.fusionLevel != boundLevel)) Refresh();
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }
    public void Refresh()
    {
        var gm = GameManager.single;
        if (!gm) return;
        lastBalance = gm.crystals; lastProgress = gm.fxpNow; lastMax = gm.fxpMax;
        balance.text = "持有水晶  <color=#194E73>" + gm.crystals.ToString("N0") + "</color>";
        level.text = "融合等级   Lv." + gm.fusionLevel + (CanUpgrade ? " → Lv." + (gm.fusionLevel+1) : " · 已达上限");
        if (!Busy) progressFill.fillAmount = gm.fxpMax > 0 ? Mathf.Clamp01(gm.fxpNow/gm.fxpMax) : 0;
        progress.text = gm.fxpNow.ToString("0.##") + " / " + gm.fxpMax.ToString("0.##");
        bool ready = Ready, valid = CanUpgrade && gm.fxpMax > 0;
        readySeal.SetActive(ready && valid); costCrystal.SetActive(!ready && valid);
        cost.text = !valid ? "已完成当前融合成长" : ready ? "进度已满 · 可以提升" : "补满消耗 <color=#194E73>" + Cost + "</color> 水晶";
        actionCaption.text = Busy ? "融合中…" : !valid ? "已达上限" : ready ? "提升融合等级" : "注入水晶";
        hint.text = Busy ? "水晶正在融入遗迹…" : !valid ? "返回庇护所继续探索" : ready ? "提升后领取本次奖励" : gm.crystals < Cost ? "水晶不足，还需 " + Mathf.CeilToInt(Cost-gm.crystals) + " 水晶" : "消耗水晶，补满融合进度";
        action.interactable = !Busy && !Leaving && valid && (ready || gm.crystals >= Cost);
        back.interactable = returnToShelter.interactable = !Busy && !Leaving;
        ruin.sprite = ready ? chargedRuin : dormantRuin;
        if (boundLevel != gm.fusionLevel) { BindRewards(); boundLevel = gm.fusionLevel; }
    }
    void BindRewards()
    {
        foreach (var slot in rewards) slot.root.SetActive(false);
        int index = 0;
        if (CanUpgrade)
        {
            var entry = LevelDataDescriptions.FusionLevelDb[GameManager.single.fusionLevel];
            if (entry.statPointsGiven > 0) BindReward(ref index, statIcon, "属性点", "+" + entry.statPointsGiven);
            if (entry.skillPointsGiven > 0) BindReward(ref index, skillIcon, "技能点", "+" + entry.skillPointsGiven);
            if (entry.expGiven > 0) BindReward(ref index, experienceIcon, "经验", "+" + entry.expGiven.ToString("0.##"));
            if (entry.itemsGiven != null) foreach (var item in entry.itemsGiven)
            {
                if (item.m_ChanceDrop <= 0) continue;
                Sprite icon = InventoryManager.single ? InventoryManager.single.GetSpriteIcon(item.m_IconName) : null;
                BindReward(ref index, icon, EquipmentName(item), item.m_ChanceDrop >= 1 ? "必得 ×1" : "概率 " + (Mathf.Clamp01(item.m_ChanceDrop)*100).ToString("0.#") + "%");
            }
        }
        rewardHint.text = index > 2 ? "上下滑动查看全部奖励" : index == 0 ? "暂无额外奖励" : "";
        rewardScroll.content.sizeDelta = new Vector2(rewardScroll.content.sizeDelta.x, Mathf.Max(92, Mathf.Ceil(index/2f)*98));
        rewardScroll.verticalNormalizedPosition = 1;
    }
    static string EquipmentName(Item item)
    {
        // Match the existing item-category enum while keeping generated English names out of the Chinese UI.
        switch (item.m_ItemType.ToString())
        {
            case "WEAPON": return "武器";
            case "HELMET": return "头盔";
            case "SHIELD": return "盾牌";
            case "ARMOR": return "护甲";
            case "ACCESSORY": return "饰品";
            default: return "装备";
        }
    }
    void BindReward(ref int index, Sprite icon, string title, string amount)
    {
        if (index >= rewards.Length) return;
        var slot = rewards[index++]; slot.root.SetActive(true);
        slot.icon.sprite = icon; slot.icon.enabled = icon;
        slot.icon.material = icon == statIcon || icon == experienceIcon ? referenceInk : null;
        slot.title.text = title; slot.amount.text = amount;
        slot.amount.resizeTextForBestFit = false;
        slot.amount.horizontalOverflow = HorizontalWrapMode.Overflow;
        slot.amount.fontSize = amount.StartsWith("+") ? 33 : 20;
    }
    public void Activate()
    {
        if (Busy || Leaving || !CanUpgrade) return;
        var gm = GameManager.single;
        if (gm.fxpMax <= 0) return;
        if (Ready) StartCoroutine(Upgrade());
        else if (gm.crystals >= Cost) StartCoroutine(Fill());
        else Refresh();
    }
    IEnumerator Fill()
    {
        var gm = GameManager.single;
        Busy = true;
        float from = Mathf.Clamp01(gm.fxpNow/gm.fxpMax);
        Refresh();
        SoundManager.Instance.PlayClip("FUSION_FILL");
        for (float t=0; t<fillDuration; t+=Time.unscaledDeltaTime)
        {
            progressFill.fillAmount = Mathf.Lerp(from, 1, Mathf.SmoothStep(0, 1, t/Mathf.Max(.01f,fillDuration)));
            yield return null;
        }
        // Revalidate after the animation; commit both values together, then save once.
        int price = Cost;
        if (CanUpgrade && gm.fxpMax > 0 && gm.crystals >= price)
        {
            gm.crystals -= price;
            gm.IncFusion(Mathf.Max(0, gm.fxpMax-gm.fxpNow));
            SaveManager.Instance.Save();
        }
        Busy = false; Refresh();
    }
    IEnumerator Upgrade()
    {
        Busy = true; Refresh();
        // Keep the existing reward rolls, inventory writes, rollover and character-XP behavior.
        GameManager.single.FusionLevelUpEvent();
        GameplayCanvas.single.RefreshFusion();
        SaveManager.Instance.Save();
        hint.text = "已提升至融合 Lv." + GameManager.single.fusionLevel;
        actionCaption.text = "提升完成";
        yield return new WaitForSecondsRealtime(.6f);
        Busy = false;
        // Existing reward notifications remain on the shelter canvas and are visible on return.
        Close();
    }
    public void Close()
    {
        if (Busy || Leaving) return;
        Leaving = true; Refresh();
        Router.single.CallCampFromFusion();
    }
}
