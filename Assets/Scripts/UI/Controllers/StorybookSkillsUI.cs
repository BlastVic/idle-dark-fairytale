using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Assets.Scripts.Services;
using Scripts.Skills;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data binding and interaction only; all graphics, text, paths and controls are saved in the prefab.</summary>
public sealed class StorybookSkillsUI : MonoBehaviour
{
    public StorybookSkillsLayout layout;
    public StorybookSkillNode[] nodes;
    public StorybookSkillLink[] links;
    public Button back, close, purchase;
    public Button[] navigation;
    public Text playerLevel, subjectNumber, points, title, level, description, prerequisites, cost, purchaseCaption;
    public Text currentValues, nextValues;
    public Text currentHeading, nextHeading;
    public GameObject comparison, currentOnly;
    public Text currentOnlyText;
    public GameObject[] prerequisiteRows;
    public Image[] prerequisiteIcons;
    public Text[] prerequisiteLabels;
    public Sprite prerequisiteMet, prerequisiteUnmet;
    public Image detailIcon, purchaseSurface;
    public Sprite paper, crimson;
    public RectTransform drawer, drawerBody, footer, overlay;
    public ScrollRect treeScroll, detailScroll;
    public CanvasGroup drawerGroup;
    public float drawerWidth=392, slideDuration=.22f, edgeInset=12;
    public Vector2 footerInset=new Vector2(28,24);
    [Min(0)] public float bodyFooterGap=18;
    public StorybookSkillNode Selected { get; private set; }
    public bool DrawerOpen { get; private set; }
    public bool DrawerOnRight { get; private set; }=true;
    public bool IsAnimating { get; private set; }
    public bool IsNavigating { get; private set; }
    float visible, refreshAt, previousScroll;
    Vector2 previousViewport;
    Coroutine animation;
    int signature=int.MinValue;
    bool initialized, closing;
    public int Cost => Selected && SkillLevel(Selected)<10 ? (SkillLevel(Selected)==9 ? 2 : 1) : 0;
    int SkillLevel(StorybookSkillNode n)=>SkillsController.Instance.GetSkillLevel(n.key);
    void Awake()
    {
        foreach(var n in nodes){var node=n;node.button.onClick.AddListener(()=>Select(node));}
        back.onClick.AddListener(()=>Navigate(()=>Router.single.CallCampFromSkills()));
        close.onClick.AddListener(CloseDrawer);purchase.onClick.AddListener(Buy);
        navigation[0].onClick.AddListener(()=>Navigate(()=>Router.single.InventoryButton()));
        navigation[2].onClick.AddListener(()=>Navigate(()=>Router.single.CallCampFromSkills()));
        navigation[3].onClick.AddListener(()=>Navigate(()=>Router.single.MapButton()));
        navigation[4].onClick.AddListener(()=>Navigate(()=>Router.single.CashMallButton()));
        treeScroll.onValueChanged.AddListener(_=>{
            float y=treeScroll.content.anchoredPosition.y;
            if(DrawerOpen && Mathf.Abs(y-previousScroll)>2)CloseDrawer();previousScroll=y;
        });
        initialized=true;
    }
    void OnEnable(){IsNavigating=false;signature=int.MinValue;HideImmediately();Refresh();}
    void OnDisable(){StopAllCoroutines();animation=null;HideImmediately();IsNavigating=false;}
    void Update()
    {
        if(!Application.isPlaying||!GameManager.single)return;
        if(Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+.15f;Refresh();}
        if(Input.GetKeyDown(KeyCode.Escape)){if(DrawerOpen)CloseDrawer();else Navigate(()=>Router.single.CallCampFromSkills());}
    }
    void LateUpdate()
    {
        if(!DrawerOpen)return;
        if(previousViewport!=overlay.rect.size){previousViewport=overlay.rect.size;LayoutDrawer();}
        PositionDrawer();
    }
    void Navigate(Action action)
    {
        if(IsNavigating)return;IsNavigating=true;if(animation!=null)StopCoroutine(animation);animation=null;HideImmediately();action();
    }
    public void Refresh()
    {
        if(!initialized||!GameManager.single)return;
        var skills=SkillsController.Instance;int hash=unchecked(GameManager.single.skillPoints*31+(IsNavigating?1:0));
        foreach(var n in nodes)hash=unchecked(hash*31+skills.GetSkillLevel(n.key));
        playerLevel.text="Lv. "+GameManager.single.playerLevel;
        if(subjectNumber)subjectNumber.text="No. "+GameManager.single.SubjectNumber;
        points.text="技能点  <color=#83180F>"+GameManager.single.skillPoints+"</color>";
        if(hash==signature)return;signature=hash;
        foreach(var n in nodes){int value=skills.GetSkillLevel(n.key);bool unlocked=n.IsUnlocked(skills);
            n.button.interactable=unlocked&&!IsNavigating;n.level.text=!unlocked?"未解锁":value>=10?"10 / 10 · 满级":"Lv. "+value+" / 10";
            n.icon.color=unlocked?Color.white:new Color(.57f,.55f,.51f,.65f);
            n.locked.SetActive(!unlocked);n.available.SetActive(unlocked&&!IsNavigating&&value<10&&GameManager.single.skillPoints>=(value==9?2:1));
            if(n.availableBackground)n.availableBackground.gameObject.SetActive(n.available.activeSelf);
            if(n.availableSymbol)n.availableSymbol.text=value==0?"+":"↑";
        }
        foreach(var link in links)link.Refresh(skills);
        if(Selected&&DrawerOpen)BindDetails();
    }
    public void Select(StorybookSkillNode node)
    {
        if(!node||!node.IsUnlocked(SkillsController.Instance)||IsNavigating)return;
        Vector2 p=overlay.InverseTransformPoint(node.Rect.TransformPoint(node.Rect.rect.center));
        float x=p.x-overlay.rect.xMin;
        bool side=Mathf.Abs(x-overlay.rect.width*.5f)<12?DrawerOnRight:x<overlay.rect.width*.5f;
        if(animation!=null)StopCoroutine(animation);
        closing=false;IsAnimating=true;
        if(DrawerOpen && side!=DrawerOnRight){animation=StartCoroutine(SwitchSide(node,side));return;}
        Show(node,side);animation=StartCoroutine(RunMotion(1,false));
    }
    IEnumerator SwitchSide(StorybookSkillNode node,bool side){yield return Animate(0,false);Show(node,side);yield return Animate(1,false);IsAnimating=false;drawerGroup.interactable=true;animation=null;}
    IEnumerator RunMotion(float target,bool hide){yield return Animate(target,hide);IsAnimating=false;closing=false;if(!hide)drawerGroup.interactable=true;animation=null;}
    void Show(StorybookSkillNode node,bool side)
    {
        Selected=node;DrawerOnRight=side;DrawerOpen=true;
        // Close button placement is authored in the prefab; preserve Inspector adjustments.
        drawer.gameObject.SetActive(true);drawerGroup.alpha=1;drawerGroup.blocksRaycasts=true;drawerGroup.interactable=false;
        foreach(var n in nodes)n.selected.SetActive(n==Selected);
        detailScroll.verticalNormalizedPosition=1;BindDetails();PositionDrawer();
    }
    public void CloseDrawer()
    {
        if(!DrawerOpen||closing)return;
        closing=true;IsAnimating=true;
        if(animation!=null)StopCoroutine(animation);
        animation=StartCoroutine(RunMotion(0,true));
    }
    void HideImmediately()
    {
        visible=0;DrawerOpen=false;IsAnimating=false;closing=false;
        if(drawerGroup){drawerGroup.blocksRaycasts=false;drawerGroup.interactable=false;drawerGroup.alpha=0;}
        if(drawer)drawer.gameObject.SetActive(false);
        if(nodes!=null)foreach(var n in nodes)if(n&&n.selected)n.selected.SetActive(false);
    }
    IEnumerator Animate(float target,bool hide)
    {
        IsAnimating=true;drawerGroup.interactable=false;
        float start=visible,t=0;
        while(t<slideDuration){t+=Mathf.Min(Time.unscaledDeltaTime,1f/30f);visible=Mathf.Lerp(start,target,Mathf.SmoothStep(0,1,t/slideDuration));PositionDrawer();yield return null;}
        visible=target;PositionDrawer();
        if(hide)HideImmediately();
    }
    string Number(float value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
    float[] Values(int wanted)=>SkillsController.Instance.GetSkillValues(wanted,Selected.key);
    string EffectValues(int wanted)
    {
        var values=Values(wanted);
        return string.Join("\n",Selected.effectLabels.Select((label,i)=>{
            string number=Number(values[i]*((Selected.key==SkillsTypeEnum.Devil_On_My_Shoulder&&i>=2)?1:100))+"%";
            return label+"  "+"<color=#8F2018>"+number+"</color>";
        }));
    }
    void BindPrerequisites()
    {
        var keys=new[]{Selected.prerequisite,Selected.alternatePrerequisite};
        bool any=keys.Any(k=>k!=SkillsTypeEnum.NONE);
        prerequisites.transform.parent.gameObject.SetActive(any);
        prerequisites.text=any?"<b>解锁条件"+(keys.All(k=>k!=SkillsTypeEnum.NONE)?" · 满足任一项":"")+"</b>":"无前置要求";
        for(int i=0;i<prerequisiteRows.Length;i++){
            var key=keys[i];bool show=key!=SkillsTypeEnum.NONE;prerequisiteRows[i].SetActive(show);if(!show)continue;
            bool met=SkillsController.Instance.GetSkillLevel(key)>0;
            prerequisiteIcons[i].sprite=met?prerequisiteMet:prerequisiteUnmet;
            prerequisiteLabels[i].text=nodes.First(n=>n.key==key).displayName+" ≥1级";
            prerequisiteLabels[i].color=new Color(.075f,.055f,.035f);
        }
    }
    void BindDetails()
    {
        int value=SkillLevel(Selected);bool max=value>=10;
        title.text=Selected.displayName;
        detailIcon.sprite=Selected.icon.sprite;detailIcon.material=Selected.icon.material;
        description.text=Selected.descriptionTemplate;
        comparison.SetActive(true);currentOnly.SetActive(max);
        currentValues.transform.parent.gameObject.SetActive(value>0);
        nextValues.transform.parent.gameObject.SetActive(!max);
        currentHeading.text="当前技能等级   Lv. "+value;
        nextHeading.text="目标技能等级   <color=#8F2018>Lv. "+(value+1)+"</color>";
        currentValues.text=value>0?EffectValues(value):"";
        nextValues.text=max?"":EffectValues(value+1);
        if(max)currentOnlyText.text="已达到最高等级";
        BindPrerequisites();cost.text=max?"无需继续消耗":"技能点消耗   <color=#861A12>"+Cost+" 点</color>";
        bool eligible=!max&&Selected.IsUnlocked(SkillsController.Instance)&&GameManager.single.skillPoints>=Cost;
        purchase.interactable=eligible;purchaseSurface.sprite=crimson;
        purchaseCaption.text=max?"已满级":!eligible?"技能点不足":value==0?"学 习":"升 级";
        purchaseCaption.color=new Color(.98f,.94f,.82f);
        LayoutDrawer();
    }
    public void LayoutDrawer()
    {
        if(!Selected||!drawer.gameObject.activeSelf)return;
        Vector2 node=overlay.InverseTransformPoint(Selected.Rect.TransformPoint(Selected.Rect.rect.center));
        float centerX=node.x-overlay.rect.xMin;
        float available=DrawerOnRight?overlay.rect.width-centerX-Selected.Rect.rect.width*.5f-16:centerX-Selected.Rect.rect.width*.5f-16;
        float width=Mathf.Min(drawerWidth,Mathf.Max(310,available));
        drawer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(drawerBody);
        float wanted=LayoutUtility.GetPreferredHeight(drawerBody);
        float footerHeight=footer.rect.height;
        float bottomReserved=footerHeight+footerInset.y+bodyFooterGap;
        float chromeHeight=76+bottomReserved;
        float height=Mathf.Min(wanted+chromeHeight,overlay.rect.height-edgeInset*2);
        drawer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        detailScroll.viewport.offsetMin=new Vector2(24,bottomReserved);detailScroll.viewport.offsetMax=new Vector2(-24,-76);
        drawerBody.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,wanted);
        detailScroll.vertical=wanted>height-chromeHeight;
        StorybookEquipmentLayout.Box(footer,footerInset.x,height-footerHeight-footerInset.y,width-footerInset.x*2,footerHeight);
        previousViewport=overlay.rect.size;PositionDrawer();
    }
    void PositionDrawer()
    {
        if(!Selected||!drawer.gameObject.activeSelf)return;
        Vector2 node=overlay.InverseTransformPoint(Selected.Rect.TransformPoint(Selected.Rect.rect.center));
        float y=overlay.rect.yMax-node.y-60;
        y=Mathf.Clamp(y,edgeInset,Mathf.Max(edgeInset,overlay.rect.height-drawer.rect.height-edgeInset));
        float x=DrawerOnRight?overlay.rect.width-drawer.rect.width:0;
        x+=(DrawerOnRight?1:-1)*(1-visible)*(drawer.rect.width+20);
        drawer.anchoredPosition=new Vector2(x,-y);
    }
    public void Buy()
    {
        // Recheck authoritative state on click, including programmatic invocations and stale UI frames.
        if(!DrawerOpen||IsAnimating||IsNavigating||!Selected||!Selected.IsUnlocked(SkillsController.Instance))return;
        int value=SkillLevel(Selected),amount=value==9?2:1;
        if(value>=10||GameManager.single.skillPoints<amount)return;
        GameManager.single.skillPoints-=amount;SkillsController.Instance.AddSkill(Selected.key);
        SoundManager.Instance.PlayClip("SKILL_BUY",false,false);
        EventManager.single.CallRefreshSkillButtons();GameplayCanvas.single.RefreshCampUI();
        signature=int.MinValue;Refresh();
    }
}
