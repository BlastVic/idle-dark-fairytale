using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonsterPreviewHud : MonoBehaviour
{
    public GameObject catalogPanel;
    public Button catalogButton, resetButton, pauseButton, replayButton, entranceButton;
    public Toggle redHoodToggle, loopToggle, cycleToggle;
    public Slider speedSlider, zoomSlider;
    public InputField searchField;
    public Text selectedTitle, catalogArrow, catalogCount, emptyText, statusText, speedText, zoomText, pauseText;
    public Image selectedPortrait;
    public MonsterPreviewCard[] cards;
    public MonsterPreviewCard cardTemplate;
    public RectTransform cardContent, animationContent;
    public ScrollRect catalogScroll, animationScroll;
    public Button animationTemplate;
    public Button[] animationButtons;
    public RectTransform playerSlot, monsterSlot;
    public Color normal = new Color(.10f,.12f,.14f,1), selected = new Color(.38f,.085f,.085f,1);
    MonsterAnimationTest model;
    readonly List<MonsterPreviewCard> liveCards = new List<MonsterPreviewCard>();
    readonly List<Button> liveAnimations = new List<Button>();
    string animationKey;

    public void Bind(MonsterAnimationTest value)
    {
        if (model) model.Changed -= Refresh;
        model = value;
        model.Changed += Refresh;
        catalogPanel.SetActive(false);
        catalogButton.onClick.AddListener(() => SetCatalogOpen(!catalogPanel.activeSelf));
        resetButton.onClick.AddListener(model.FitCamera);
        pauseButton.onClick.AddListener(() => model.SetPaused(!model.Paused));
        replayButton.onClick.AddListener(model.Replay);
        if (entranceButton) entranceButton.onClick.AddListener(model.TestEntrance);
        redHoodToggle.onValueChanged.AddListener(v => { if (model.stage) model.stage.ShowRedHood(v); });
        loopToggle.onValueChanged.AddListener(model.SetLoop);
        cycleToggle.onValueChanged.AddListener(model.SetAutoCycle);
        speedSlider.onValueChanged.AddListener(model.SetSpeed);
        zoomSlider.onValueChanged.AddListener(model.SetZoom);
        searchField.onValueChanged.AddListener(Filter);
        liveCards.AddRange(cards);
        liveAnimations.AddRange(animationButtons);
        // New catalog entries reuse an authored card prefab; no layout is constructed in code.
        foreach (var data in model.monsters)
        {
            var card = liveCards.Find(c => c && c.data == data);
            if (!card)
            {
                card = Instantiate(cardTemplate, cardContent);
                card.data = data; card.displayName = CreatureNames.DisplayName(data.name);
                card.title.text = card.displayName;
                liveCards.Add(card);
            }
        }
        foreach (var card in liveCards)
        {
            int index = Array.IndexOf(model.monsters, card.data);
            card.button.onClick.AddListener(() => { model.SelectMonster(index); SetCatalogOpen(false); });
            card.gameObject.SetActive(index >= 0);
        }
        if (model.stage) model.stage.ShowRedHood(redHoodToggle.isOn);
        Refresh(); Filter(searchField.text);
    }
    void OnDestroy() { if (model) model.Changed -= Refresh; }
    public void SetCatalogOpen(bool open)
    {
        catalogPanel.SetActive(open); catalogArrow.text = open ? "收起  −" : "怪物库  +";
    }
    public void Filter(string query)
    {
        int count = 0;
        foreach (var card in liveCards)
        {
            bool show = Array.IndexOf(model.monsters, card.data) >= 0 &&
                (string.IsNullOrWhiteSpace(query) || (card.displayName + " " + card.data.name).IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            card.gameObject.SetActive(show); if (show) count++;
        }
        emptyText.gameObject.SetActive(count == 0);
        catalogCount.text = count + " / " + model.monsters.Length + " 个怪物";
        catalogScroll.verticalNormalizedPosition = 1;
    }
    void Refresh()
    {
        if (!model) return;
        speedText.text = model.speed.ToString("0.00") + "×";
        zoomText.text = model.Zoom.ToString("0.00") + "×";
        speedSlider.SetValueWithoutNotify(model.speed); zoomSlider.SetValueWithoutNotify(model.Zoom);
        loopToggle.SetIsOnWithoutNotify(model.loop); cycleToggle.SetIsOnWithoutNotify(model.autoCycle);
        pauseText.text = model.Paused ? "继续" : "暂停";
        bool hasAnimations = model.AnimationNames.Length > 0;
        pauseButton.interactable = replayButton.interactable = loopToggle.interactable = cycleToggle.interactable = hasAnimations;
        statusText.text = model.Error ?? (hasAnimations ? "动画  /  " + model.AnimationNames[model.SelectedAnimation] : "无动画 · 显示初始姿态");
        if (model.EntranceMode) statusText.text = "出场测试  /  " + model.EntranceStatus;
        if (entranceButton) entranceButton.interactable = model.CanTestEntrance;
        statusText.color = model.Error == null ? new Color(.79f,.75f,.66f) : new Color(1,.45f,.4f);
        foreach (var c in liveCards)
        {
            bool active = model.SelectedMonster >= 0 && c.data == model.monsters[model.SelectedMonster];
            c.selection.color = active ? selected : normal;
            if (active) { selectedTitle.text = c.displayName.Replace("\n", " · "); selectedPortrait.sprite = c.portrait.sprite; }
        }
        string key = string.Join("|", model.AnimationNames);
        if (animationKey != key)
        {
            animationKey = key;
            while (liveAnimations.Count < model.AnimationNames.Length)
                liveAnimations.Add(Instantiate(animationTemplate, animationContent));
            for (int i = 0; i < liveAnimations.Count; i++)
            {
                var b = liveAnimations[i]; int index = i;
                b.gameObject.SetActive(i < model.AnimationNames.Length);
                b.onClick.RemoveAllListeners();
                if (i < model.AnimationNames.Length)
                {
                    b.GetComponentInChildren<Text>().text = model.AnimationNames[i];
                    b.onClick.AddListener(() => model.Play(index));
                }
            }
            animationScroll.verticalNormalizedPosition = 1;
        }
        for (int i = 0; i < liveAnimations.Count; i++)
            liveAnimations[i].GetComponent<Image>().color = i == model.SelectedAnimation ? selected : normal;
    }
}
