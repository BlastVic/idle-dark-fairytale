using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Game.Enum;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Data/interaction only. All artwork, controls and route dots are persisted in the prefab.</summary>
public sealed class StorybookMapUI : MonoBehaviour
{
    public StorybookMapNode[] nodes;
    public CursedBookIntro storyPlayerPrefab;
    public CursedBookIntro ActiveStory { get; private set; }
    public Button backButton, locateButton, enterButton;
    public Button[] navigationButtons;
    public Text detailTitle, detailStatus, detailWaves, enterCaption, skillBadge;
    public Image detailPreview;
    public GameObject battleDetailGlyph, storyDetailGlyph;
    public Sprite completedSeal, availableSeal, lockedSeal;
    public Material completedMaterial, availableMaterial, lockedMaterial;
    public ScrollRect scroll;
    public CanvasGroup controls;
    public StorybookMapLayout layout;
    public StorybookMapCardMotion detailMotion;
    public int selectedIndex;
    public bool IsNavigating {get;private set;}
    public bool IsDragging {get;private set;}
    bool initialized;
    float nextRefresh, suppressClickUntil;

    void Awake()
    {
        for(int i=0;i<nodes.Length;i++){int n=i;nodes[i].button.onClick.AddListener(()=>ClickNode(n));}
        backButton.onClick.AddListener(()=>Navigate(()=>Router.single.CampButton()));
        locateButton.onClick.AddListener(LocateSelection);
        enterButton.onClick.AddListener(EnterSelected);
        navigationButtons[0].onClick.AddListener(()=>Navigate(()=>Router.single.InventoryButton()));
        navigationButtons[1].onClick.AddListener(()=>Navigate(()=>Router.single.SkillsButton()));
        navigationButtons[2].onClick.AddListener(()=>Navigate(()=>Router.single.CampButton()));
        navigationButtons[3].onClick.AddListener(LocateSelection);
        navigationButtons[4].onClick.AddListener(()=>Navigate(()=>Router.single.CashMallButton()));
    }
    void OnEnable(){initialized=false;IsNavigating=false;IsDragging=false;suppressClickUntil=0;nextRefresh=0;if(controls)controls.interactable=true;}
    void OnDisable(){if(layout)layout.RememberPosition();IsDragging=false;IsNavigating=false;}
    void Update()
    {
        if(!WaveManager.single||WaveManager.single.waveDb==null||!GameManager.single)return;
        if(!initialized){
            int last=Array.FindIndex(nodes,n=>n.levelKey==GameManager.single.lastBattleKey);
            selectedIndex=last>=0?last:0;initialized=true;layout.Apply();
        }
        if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;Refresh();}
    }
    public Waveset Data(int index)=>WaveManager.single?.waveDb?.FirstOrDefault(w=>w.levelKey==nodes[index].levelKey);
    public bool IsUnlocked(int index)
    {
        if(nodes[index].IsStory){
            var node=nodes[index];
            bool available=node.story && node.story.pages!=null && node.story.pages.Length>0;
            if(string.IsNullOrEmpty(node.storyUnlockLevelKey))return available;
            var prerequisite=WaveManager.single.waveDb.FirstOrDefault(w=>w.levelKey==node.storyUnlockLevelKey);
            return available && (GameManager.single.HasSeenThis(node.story.completionKey) ||
                prerequisite!=null && WaveManager.single.GetWaveseReachedByKey(prerequisite.levelKey)>=prerequisite.waves.Length);
        }
        var wave=Data(index);if(wave==null)return false;
        if(string.IsNullOrEmpty(wave.precondition))return true;
        var previous=WaveManager.single.waveDb.FirstOrDefault(w=>w.levelKey==wave.precondition);
        return previous!=null&&WaveManager.single.GetWaveseReachedByKey(previous.levelKey)>=previous.waves.Length;
    }
    public bool IsCompleted(int index){if(nodes[index].IsStory)return nodes[index].story && GameManager.single.HasSeenThis(nodes[index].story.completionKey);var w=Data(index);return w!=null&&WaveManager.single.GetWaveseReachedByKey(w.levelKey)>=w.waves.Length;}
    public void Refresh()
    {
        if(nodes.Length==0||!WaveManager.single)return;
        selectedIndex=Mathf.Clamp(selectedIndex,0,nodes.Length-1);
        for(int i=0;i<nodes.Length;i++){
            var n=nodes[i];var w=Data(i);bool unlocked=IsUnlocked(i),complete=IsCompleted(i),selected=i==selectedIndex;
            n.seal.sprite=!unlocked?lockedSeal:selected?availableSeal:complete?completedSeal:availableSeal;
            n.seal.material=!unlocked?lockedMaterial:selected?availableMaterial:complete?completedMaterial:availableMaterial;
            if(n.customSeal){n.seal.sprite=n.customSeal;n.seal.material=n.customMaterial;}
            if(n.lockBadge)n.lockBadge.SetActive(!unlocked);
            n.seal.color=Color.white;n.locator.SetActive(selected&&unlocked);
            if(n.labelRoot)n.labelRoot.SetActive(unlocked);
            n.progress.gameObject.SetActive(unlocked);
            n.label.text=NodeTitle(i);
            n.labelFrame.color=selected?new Color(1,.83f,.77f):Color.white;
            n.progress.text=n.IsStory?(!unlocked?"通关第一章解锁":complete?"已观看 · 可重播":"剧情") : w==null?"未配置":unlocked?Mathf.Clamp(WaveManager.single.GetWaveseReachedByKey(w.levelKey),0,w.waves.Length)+" / "+w.waves.Length:"未解锁";
            n.button.interactable=!IsNavigating;
            foreach(var dot in n.incomingRoute)dot.color=unlocked?new Color(1,.70f,.30f,.95f):new Color(.67f,.65f,.61f,.75f);
        }
        var node=nodes[selectedIndex];var data=Data(selectedIndex);bool canEnter=IsUnlocked(selectedIndex);
        if(battleDetailGlyph)battleDetailGlyph.SetActive(!node.IsStory);
        if(storyDetailGlyph)storyDetailGlyph.SetActive(node.IsStory);
        detailPreview.preserveAspect=false;
        detailTitle.text=NodeTitle(selectedIndex);
        detailStatus.text=node.IsStory?(!canEnter?"通关第一章后解锁":IsCompleted(selectedIndex)?"已观看 · 可再次播放":"剧情节点") : !canEnter?"尚未解锁":IsCompleted(selectedIndex)?"已通关 · 可再次探索":"当前探索";
        detailWaves.text=node.IsStory?"播放结束后返回地图" : data==null?"关卡数据未配置":canEnter?"波次  "+WaveManager.single.GetWaveseReachedByKey(data.levelKey)+" / "+data.waves.Length:"完成前置关卡后解锁";
        detailPreview.sprite=node.preview;enterButton.interactable=canEnter&&!IsNavigating;
        enterCaption.text=canEnter?(node.IsStory?"播放剧情":"进入关卡"):"未解锁";
        if(skillBadge&&GameplayCanvas.single&&GameplayCanvas.single.homeUI){var source=GameplayCanvas.single.homeUI.skillBadge;skillBadge.text=source.text;skillBadge.transform.parent.gameObject.SetActive(source.transform.parent.gameObject.activeSelf);}
    }
    public void ClickNode(int index)
    {
        if(IsNavigating||IsDragging||Time.unscaledTime<suppressClickUntil||index<0||index>=nodes.Length)return;
        if(index==selectedIndex&&IsUnlocked(index))EnterSelected();
        else SelectNode(index);
    }
    public void SelectNode(int index)
    {
        if(IsNavigating||IsDragging||Time.unscaledTime<suppressClickUntil||index<0||index>=nodes.Length)return;
        selectedIndex=index;Refresh();if(detailMotion)detailMotion.Play();SoundManager.Instance.PlayClip("CLICK");
    }
    public void BeginDrag(){IsDragging=true;}
    public void EndDrag(){IsDragging=false;suppressClickUntil=Time.unscaledTime+.15f;}
    public void LocateSelection()
    {
        if(IsNavigating)return;scroll.StopMovement();Canvas.ForceUpdateCanvases();
        var target=(RectTransform)nodes[selectedIndex].transform;
        float y=-target.anchoredPosition.y*scroll.content.localScale.y-scroll.viewport.rect.height*.5f;
        float maximum=Mathf.Max(0,scroll.content.rect.height*scroll.content.localScale.y-scroll.viewport.rect.height);
        scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(y,0,maximum));
    }
    void Navigate(Action action)
    {
        if(IsNavigating||GameManager.single.gameState!=GameStateType.MAP)return;
        layout.RememberPosition();IsNavigating=true;controls.interactable=false;StorybookNavigation.PlayClick();action();
    }
    public void EnterSelected()
    {
        if(IsNavigating||IsDragging||Time.unscaledTime<suppressClickUntil||!IsUnlocked(selectedIndex)||GameManager.single.gameState!=GameStateType.MAP)return;
        if(nodes[selectedIndex].IsStory){StartCoroutine(EnterStory(nodes[selectedIndex]));return;}
        layout.RememberPosition();string key=nodes[selectedIndex].levelKey;IsNavigating=true;controls.interactable=false;Refresh();
        SoundManager.Instance.PlayClip("CLICK");Router.single.StartCoroutine(Enter(key));
    }
    string NodeTitle(int index)
    {
        var node=nodes[index];
        if(node.IsStory)return node.displayName;
        int number=nodes.Take(index+1).Count(n=>!n.IsStory);
        return "1-"+number+"  "+node.displayName;
    }
    IEnumerator EnterStory(StorybookMapNode node)
    {
        var playerPrefab=node.storyPlayerOverride?node.storyPlayerOverride:storyPlayerPrefab;
        if(!playerPrefab){Debug.LogError("Story player prefab missing",this);yield break;}
        IsNavigating=true;controls.interactable=false;layout.RememberPosition();Refresh();
        var music=MusicManager.Instance?MusicManager.Instance.backgroundMusicSource:null;
        bool muted=music&&music.mute;
        try {
            if(music)music.mute=true;
            ActiveStory=Instantiate(playerPrefab);
            ActiveStory.mapStory=node.story;
            ActiveStory.returnToMapAfterEnding=true;
            ActiveStory.GetComponent<Canvas>().sortingOrder=GetComponent<Canvas>().sortingOrder+100;
            controls.alpha=0;controls.blocksRaycasts=false;
            while(ActiveStory&&!ActiveStory.Finished)yield return null;
        } finally {
            if(ActiveStory){Destroy(ActiveStory.gameObject);ActiveStory=null;}
            if(music)music.mute=muted;
            controls.alpha=1;controls.blocksRaycasts=true;controls.interactable=true;IsNavigating=false;
            Refresh();
        }
    }
    IEnumerator Enter(string key)
    {
        GameManager.single.lastBattleKey=key;
        // This screen provides the replacement first-node guidance through selection and the entry button.
        if(GameManager.single.HasSeenThis("Tutorial-0")&&!GameManager.single.HasSeenThis("Tutorial-1"))GameManager.single.thingsSeenPermanent.Add("Tutorial-1");
        TransitionCanvas.single.ToBlack("Storybook map node");yield return TransitionCanvas.single.WaitUntilCovered();
        LevelController.Instance.AssetManager.HideMapPackage();
        GameplayCanvas.single.ToggleButtonGrid(false);GameplayCanvas.single.ToggleFusionBarUI(false);
        WaveManager.single.CallStartLevel(key);
    }
}
