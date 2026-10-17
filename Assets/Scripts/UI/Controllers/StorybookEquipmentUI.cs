using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Purchasing;
using IdleKnightHero.UI;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Inventory data and interaction binding for the editable equipment prefab.</summary>
public sealed class StorybookEquipmentUI : MonoBehaviour
{
    public StorybookEquipmentLayout layout;
    public StorybookEquipmentCell cellPrefab;
    public GridLayoutGroup grid;
    public ScrollRect gridScroll, comparisonScroll;
    public Button compactModeButton, detailedModeButton;
    public Button[] columnButtons;
    public const string ColumnsPreference="StorybookEquipment.Columns";
    public int Columns {get;private set;}=4;
    public bool detailedMode=false;
    public Button back, equip, decompose, previous, next, foldButton, sortButton, filterButton, batchButton, lockButton;
    public Button[] categoryButtons, navigationButtons, statButtons;
    public Image[] categoryBackgrounds, categoryIcons;
    public GameObject[] categoryBadges;
    public Text level, number, count, title, selectedName, equippedName, grade, equippedGrade, equipCaption, decomposeCaption, batchCaption, sortCaption, filterCaption, lockCaption, points, fusionText, skillBadge, emptyText, rebirthText;
    public Image selectedIcon, equippedIcon, fusionFill;
    public StorybookFusionReminder fusionReminder;
    public void OpenFusion()=>Navigate(()=>Router.single.CallFusionFromEquipment());
    public Text[] statValues;
    public StorybookAttributePanel sharedAttributes;
    public CanvasGroup attributeBody;
    public RectTransform foldArrow, comparisonContent;
    public ComparisonRow[] comparisonRows;
    public GameObject modal;
    public Text modalTitle, modalMessage;
    public Button modalCancel, modalConfirm;
    public GameObject options;
    public Text optionsTitle;
    public Button[] optionButtons;
    public Text[] optionCaptions;
    public bool attributesExpanded;
    public string subjectNumber => GameManager.single ? GameManager.single.SubjectNumber : "001";
    public float foldDuration=.22f;
    public ItemType Category {get;private set;}=ItemType.WEAPON;
    public Item Selected {get;private set;}
    public bool BatchMode {get;private set;}
    public bool IsNavigating {get;private set;}
    public IReadOnlyList<Item> VisibleItems=>items;
    public IReadOnlyList<StorybookEquipmentCell> Cells=>cells;
    [Serializable] public class ComparisonRow {public RectTransform root;public Text label,current,selected,delta;public Image icon;public EquipmentDeltaArrow deltaArrow;}
    readonly ItemType[] types={ItemType.WEAPON,ItemType.HELMET,ItemType.SHIELD,ItemType.ARMOR,ItemType.ACCESSORY};
    readonly LevelTypeEnum[] statTypes={LevelTypeEnum.HP,LevelTypeEnum.DMG,LevelTypeEnum.DEF,LevelTypeEnum.ATK_SPD,LevelTypeEnum.CRIT,LevelTypeEnum.CRIT_DMG};
    readonly List<Item> items=new List<Item>();
    readonly List<StorybookEquipmentCell> cells=new List<StorybookEquipmentCell>();
    readonly HashSet<int> batch=new HashSet<int>();
    readonly Dictionary<ItemType,int> selections=new Dictionary<ItemType,int>();
    readonly Dictionary<ItemType,float> positions=new Dictionary<ItemType,float>();
    readonly Dictionary<ItemType,int> sortModes=new Dictionary<ItemType,int>();
    readonly Dictionary<ItemType,int> filterModes=new Dictionary<ItemType,int>();
    Coroutine folding;
    Action confirmed;
    bool initialized;
    float refreshAt;
    float authoredDetailHeight, authoredViewportHeight;
    Vector2 equipPosition, decomposePosition;
    public bool sideActions;
    public bool layeredComparison;
    public float minimumComparisonHeight=214;
    RectTransform[] detailDividers;
    Vector2[] dividerSizes;

    public bool HasOverlay=>modal.activeSelf||options.activeSelf;
    void CacheDetailGeometry()
    {
        authoredDetailHeight=layout.detailHeight;
        authoredViewportHeight=comparisonScroll.viewport.sizeDelta.y;
        equipPosition=((RectTransform)equip.transform).anchoredPosition;
        decomposePosition=((RectTransform)decompose.transform).anchoredPosition;
        detailDividers=layout.detailContent.Cast<Transform>().Where(t=>t.name=="InkRule").Select(t=>(RectTransform)t).ToArray();
        dividerSizes=detailDividers.Select(r=>r.sizeDelta).ToArray();
    }
    void Awake()
    {
        CacheDetailGeometry();
        Columns=Mathf.Clamp(UnityEngine.PlayerPrefs.GetInt(ColumnsPreference,4),3,5);
        for(int i=0;i<columnButtons.Length;i++){int columns=i+3;columnButtons[i].onClick.AddListener(()=>SetColumns(columns));}
        cells.AddRange(grid.GetComponentsInChildren<StorybookEquipmentCell>(true));
        compactModeButton.onClick.AddListener(()=>SetDisplayMode(false));detailedModeButton.onClick.AddListener(()=>SetDisplayMode(true));
        for(int i=0;i<types.Length;i++){int n=i;categoryButtons[i].onClick.AddListener(()=>SetCategory(types[n]));}
        for(int i=0;i<6;i++){int n=i;statButtons[i].onClick.AddListener(()=>BuyStat(n));}
        back.onClick.AddListener(()=>Navigate(()=>Router.single.CampButton()));
        navigationButtons[0].onClick.AddListener(()=>{});
        navigationButtons[1].onClick.AddListener(()=>Navigate(()=>Router.single.SkillsButton()));
        navigationButtons[2].onClick.AddListener(()=>Navigate(()=>Router.single.CampButton()));
        navigationButtons[3].onClick.AddListener(()=>Navigate(()=>Router.single.MapButton()));
        navigationButtons[4].onClick.AddListener(()=>Navigate(()=>Router.single.CashMallButton()));
        equip.onClick.AddListener(EquipSelected);decompose.onClick.AddListener(RequestDecompose);
        previous.onClick.AddListener(()=>Step(-1));next.onClick.AddListener(()=>Step(1));
        foldButton.onClick.AddListener(ToggleAttributes);lockButton.onClick.AddListener(ToggleLock);
        sortButton.onClick.AddListener(()=>OpenOptions(true));filterButton.onClick.AddListener(()=>OpenOptions(false));
        batchButton.onClick.AddListener(ToggleBatch);
        modalCancel.onClick.AddListener(CloseOverlays);
        modalConfirm.onClick.AddListener(()=>{var action=confirmed;CloseOverlays();action?.Invoke();});
    }
    void OnEnable()
    {
        IsNavigating=false;BatchMode=false;batch.Clear();CloseOverlays();refreshAt=0;
        ApplyFold(attributesExpanded?1:0);
        if(GameManager.single&&InventoryManager.single){GameManager.single.CalcPlayer();Rebuild(true);initialized=true;}
    }
    void OnDisable(){StorybookPopupMotion.Hide(modal,true);StorybookPopupMotion.Hide(options,true);Remember();StopAllCoroutines();folding=null;IsNavigating=false;BatchMode=false;batch.Clear();}
    void Update()
    {
        if(!GameManager.single||!InventoryManager.single)return;
        if(!initialized){Rebuild(true);initialized=true;}
        if(Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+.15f;RefreshStats();}
        if(Input.GetKeyDown(KeyCode.Escape)) {
            if(HasOverlay)CloseOverlays();else if(BatchMode)ToggleBatch();
        }
    }
    public List<Item> Owned(ItemType type)
    {
        var im=InventoryManager.single;
        switch(type){case ItemType.WEAPON:return im.weaponsOwned;case ItemType.HELMET:return im.helmetsOwned;case ItemType.SHIELD:return im.shieldsOwned;case ItemType.ARMOR:return im.armorsOwned;default:return im.accOwned;}
    }
    public Item Equipped(ItemType type)
    {
        var im=InventoryManager.single;
        switch(type){case ItemType.WEAPON:return im.hasWeapon?im.weaponEq:null;case ItemType.HELMET:return im.hasHelmet?im.helmetEq:null;case ItemType.SHIELD:return im.hasShield?im.shieldEq:null;case ItemType.ARMOR:return im.hasArmor?im.armorEq:null;default:return im.hasAcc?im.accEq:null;}
    }
    void SetEquipped(ItemType type,Item item)
    {
        var im=InventoryManager.single;
        switch(type){case ItemType.WEAPON:im.hasWeapon=true;im.weaponEq=item;break;case ItemType.HELMET:im.hasHelmet=true;im.helmetEq=item;break;case ItemType.SHIELD:im.hasShield=true;im.shieldEq=item;break;case ItemType.ARMOR:im.hasArmor=true;im.armorEq=item;break;default:im.hasAcc=true;im.accEq=item;break;}
    }
    void Remember(){positions[Category]=Mathf.Max(0,gridScroll.content.anchoredPosition.y);if(Selected!=null)selections[Category]=Selected.uniqueId;}
    int SortMode=>sortModes.TryGetValue(Category,out int n)?n:0;
    int FilterMode=>filterModes.TryGetValue(Category,out int n)?n:0;
    public void SetCategory(ItemType type)
    {
        if(IsNavigating||HasOverlay)return;Remember();Category=type;Selected=null;BatchMode=false;batch.Clear();
        var im=InventoryManager.single;
        switch(type){case ItemType.WEAPON:im.weaponFlag=false;break;case ItemType.HELMET:im.helmetFlag=false;break;case ItemType.SHIELD:im.shieldFlag=false;break;case ItemType.ARMOR:im.armorFlag=false;break;default:im.accFlag=false;break;}
        Rebuild(true);
    }
    public void Rebuild(bool restore=false)
    {
        if(!InventoryManager.single)return;
        int? selectedId=Selected?.uniqueId;
        if(!selectedId.HasValue&&selections.TryGetValue(Category,out int remembered))selectedId=remembered;
        var eq=Equipped(Category);items.Clear();if(eq!=null&&FilterMode!=1&&(FilterMode!=2||!eq.isLocked))items.Add(eq);
        items.AddRange(Owned(Category).Where(i=>FilterMode!=2||!i.isLocked));
        if(SortMode==1)items.Sort((a,b)=>PrimaryValue(b).CompareTo(PrimaryValue(a)));
        else if(SortMode==2)items.Sort((a,b)=>{int c=b.itemGrade.CompareTo(a.itemGrade);return c!=0?c:PrimaryValue(b).CompareTo(PrimaryValue(a));});
        Selected=selectedId.HasValue?items.FirstOrDefault(i=>i.uniqueId==selectedId.Value):null;
        if(Selected==null)Selected=items.FirstOrDefault(i=>i!=eq)??items.FirstOrDefault();
        while(cells.Count<items.Count)cells.Add(Instantiate(cellPrefab,grid.transform));
        for(int i=0;i<cells.Count;i++)cells[i].gameObject.SetActive(i<items.Count);
        gridStatLines=items.Count==0?3:Mathf.Max(3,items.Max(i=>StatIndices(i).Count()));
        ResizeGrid(true);
        count.text="背包  "+(Owned(Category).Count+(eq!=null?1:0))+"件";
        emptyText.gameObject.SetActive(items.Count==0);emptyText.text=FilterMode==0?"暂无此类装备":"没有符合筛选条件的装备";
        sortCaption.text=SortMode==0?"排序":SortMode==1?"主属性 ↓":"品质 ↓";
        filterCaption.text=FilterMode==0?"筛选":FilterMode==1?"未装备":"未锁定";
        RefreshSelection();RefreshStats();layout.Apply();Canvas.ForceUpdateCanvases();
        if(restore){gridScroll.StopMovement();float y=positions.TryGetValue(Category,out float p)?p:0;SetScroll(y);}
    }
    int gridStatLines=3;
    float lastGridWidth=-1;
    void LateUpdate(){if(initialized){layout.Apply(false);ResizeGrid();}}
    void ResizeGrid(bool force=false)
    {
        float available=layout.gridViewport.rect.width;
        if(available<=0||(!force&&Mathf.Abs(available-lastGridWidth)<.01f))return;
        lastGridWidth=available;grid.constraintCount=Columns;
        float width=(available-grid.spacing.x*(Columns-1))/Columns;
        grid.cellSize=new Vector2(width,(detailedMode?218+(gridStatLines-3)*28:218)*width/157.4f);
        int rows=Mathf.CeilToInt(items.Count/(float)Columns);
        gridScroll.content.sizeDelta=new Vector2(gridScroll.content.sizeDelta.x,Mathf.Max(0,rows*grid.cellSize.y+Mathf.Max(0,rows-1)*grid.spacing.y));
    }
    public void SetColumns(int columns)
    {
        if(IsNavigating||HasOverlay)return;
        int first=Mathf.FloorToInt(gridScroll.content.anchoredPosition.y/(grid.cellSize.y+grid.spacing.y))*Columns;
        Columns=Mathf.Clamp(columns,3,5);
        UnityEngine.PlayerPrefs.SetInt(ColumnsPreference,Columns);UnityEngine.PlayerPrefs.Save();
        Rebuild();SetScroll((first/Columns)*(grid.cellSize.y+grid.spacing.y));Remember();
    }
    public void SetDisplayMode(bool detailed)
    {
        if(IsNavigating||HasOverlay)return;
        float row=gridScroll.content.anchoredPosition.y/(grid.cellSize.y+grid.spacing.y);
        detailedMode=detailed;Rebuild();SetScroll(row*(grid.cellSize.y+grid.spacing.y));Remember();
    }
    public static readonly string[] StatNames={"生命","攻击","防御","攻速","暴击","暴伤","生命%","攻击%","防御%"};
    public static IEnumerable<int> StatIndices(Item item)
    {
        int primary=item.m_ItemType==ItemType.WEAPON?1:item.m_ItemType==ItemType.ACCESSORY?0:2;
        var v=Values(item);return new[]{primary,4,3,0,1,2,5,6,7,8}.Distinct().Where(i=>i==primary||v[i]!=0);
    }
    void SetScroll(float y){var p=gridScroll.content.anchoredPosition;p.y=Mathf.Clamp(y,0,Mathf.Max(0,gridScroll.content.rect.height-gridScroll.viewport.rect.height));gridScroll.content.anchoredPosition=p;}
    public void Select(Item item)
    {
        if(IsNavigating||HasOverlay||item==null||!items.Contains(item))return;
        if(BatchMode){if(item==Equipped(Category)||item.isLocked)return;if(!batch.Add(item.uniqueId))batch.Remove(item.uniqueId);}
        else {Selected=item;selections[Category]=item.uniqueId;}
        RefreshSelection();
    }
    public void Step(int direction)
    {
        if(IsNavigating||HasOverlay||BatchMode||items.Count==0)return;
        int index=Mathf.Clamp(items.IndexOf(Selected)+direction,0,items.Count-1);Select(items[index]);
        float y=index/grid.constraintCount*(grid.cellSize.y+grid.spacing.y), visibleTop=gridScroll.content.anchoredPosition.y;
        gridScroll.StopMovement();if(y<visibleTop)SetScroll(y);else if(y+grid.cellSize.y>visibleTop+gridScroll.viewport.rect.height)SetScroll(y+grid.cellSize.y-gridScroll.viewport.rect.height);
    }
    public void RefreshSelection()
    {
        var eq=Equipped(Category);
        for(int c=0;c<columnButtons.Length;c++){
            bool active=Columns==c+3;columnButtons[c].GetComponent<Image>().color=active?new Color(.45f,.06f,.055f):Color.white;
            foreach(var glyph in columnButtons[c].GetComponentsInChildren<Image>())if(glyph.name.StartsWith("GridGlyph"))glyph.color=active?new Color(.98f,.94f,.84f):new Color(.15f,.12f,.09f);
        }
        foreach(var modeButton in new[]{compactModeButton,detailedModeButton}){
            bool active=(modeButton==detailedModeButton)==detailedMode;modeButton.GetComponent<Image>().color=active?new Color(.45f,.06f,.055f):Color.white;
            modeButton.GetComponentInChildren<Text>().color=active?new Color(.98f,.94f,.84f):Color.black;
        }
        for(int i=0;i<items.Count;i++)cells[i].Bind(this,items[i],items[i]==eq,items[i]==Selected,batch.Contains(items[i].uniqueId));
        selectedName.text=Selected==null?"请选择装备":(layeredComparison?GradeName(Selected.itemGrade)+" · ":"")+EquipmentNames.Display(Selected);
        equippedName.text=eq==null?"未装备":(layeredComparison?GradeName(eq.itemGrade)+" · ":"")+EquipmentNames.Display(eq);
        selectedName.GetComponent<EquipmentSingleLineName>()?.SetTitle(selectedName.text);
        equippedName.GetComponent<EquipmentSingleLineName>()?.SetTitle(equippedName.text);
        grade.text=Selected==null?"":GradeName(Selected.itemGrade);equippedGrade.text=eq==null?"":GradeName(eq.itemGrade);
        title.text=Selected==null?"装备详情":EquipmentNames.Display(Selected)+" · "+grade.text;
        selectedIcon.sprite=Selected==null?null:InventoryManager.single.GetSpriteIcon(Selected.m_IconName);selectedIcon.enabled=selectedIcon.sprite;
        equippedIcon.sprite=eq==null?null:InventoryManager.single.GetSpriteIcon(eq.m_IconName);equippedIcon.enabled=equippedIcon.sprite;
        bool canEquip=Selected!=null&&Selected!=eq&&Owned(Category).Contains(Selected);
        equip.interactable=!IsNavigating&&(BatchMode?items.Any(i=>i!=eq&&!i.isLocked):canEquip);
        decompose.interactable=!IsNavigating&&(BatchMode?batch.Count>0:canEquip&&!Selected.isLocked);
        equipCaption.text=BatchMode?"全选":Selected==eq&&eq!=null?"已装备":"装 备";
        decomposeCaption.text=BatchMode?"分解（"+batch.Count+"）":"分 解";
        batchCaption.text=BatchMode?"退出多选":"批量分解";
        lockButton.interactable=Selected!=null&&!BatchMode&&!IsNavigating;lockCaption.text=Selected!=null&&Selected.isLocked?"已锁定":"锁定";
        previous.interactable=!BatchMode&&items.IndexOf(Selected)>0;next.interactable=!BatchMode&&Selected!=null&&items.IndexOf(Selected)<items.Count-1;
        var a=Values(eq);var b=Values(Selected);int row=0;
        int primary=Category==ItemType.WEAPON?1:Category==ItemType.ACCESSORY?0:2;
        var order=new[]{primary,4,3,0,1,2,5,6,7,8}.Distinct();
        foreach(int index in order){
            if(a[index]==0&&b[index]==0)continue;
            var r=comparisonRows[index];r.root.gameObject.SetActive(true);StorybookEquipmentLayout.Box(r.root,0,row*37,comparisonScroll.viewport.rect.width,37);
            if(layout.responsiveContents){r.root.anchorMax=new Vector2(1,1);r.root.sizeDelta=new Vector2(0,37);}
            row++;
            if(layeredComparison){
                var weight=index==primary?FontStyle.Bold:FontStyle.Normal;
                r.current.fontStyle=r.selected.fontStyle=weight;
                foreach(var label in r.root.GetComponentsInChildren<Text>(true))if(label.name.StartsWith("Label"))label.fontStyle=weight;
            }
            SetComparisonColumnVisible(r,0,a[index]!=0);
            SetComparisonColumnVisible(r,1,b[index]!=0);
            r.current.text=a[index]==0?"":Value(a[index],index);r.selected.text=b[index]==0?"":Value(b[index],index);float delta=b[index]-a[index];
            bool changed=Mathf.Abs(delta)>=.0001f;
            r.delta.text=!changed?"—":(delta>0?"+":"")+Value(delta,index);
            r.delta.color=!changed?Color.black:delta>0?new Color(.62f,.08f,.055f):new Color(.10f,.34f,.16f);
            if(r.deltaArrow){
                r.deltaArrow.gameObject.SetActive(changed);
                r.deltaArrow.color=r.delta.color;
                r.deltaArrow.rectTransform.localRotation=Quaternion.Euler(0,0,delta>0?0:180);
            }
        }
        for(int i=0;i<9;i++)if(a[i]==0&&b[i]==0)comparisonRows[i].root.gameObject.SetActive(false);
        comparisonContent.sizeDelta=new Vector2(layout.responsiveContents?0:comparisonScroll.viewport.rect.width,row*37);
        if(layout.responsiveContents)comparisonContent.anchorMax=new Vector2(1,1);comparisonScroll.verticalNormalizedPosition=1;
        if(detailDividers==null)CacheDetailGeometry();
        // Collapse unused comparison rows while preserving the authored button/border padding.
        float height=Mathf.Min(authoredViewportHeight,Mathf.Max(sideActions?2:1,row)*37);
        if(layeredComparison)height=Mathf.Max(height,authoredViewportHeight-authoredDetailHeight+minimumComparisonHeight);
        float reduction=authoredViewportHeight-height;
        if(layeredComparison){
            float rowOffset=Mathf.Max(0,(height-row*37)*.5f);
            foreach(var entry in comparisonRows)if(entry.root.gameObject.activeSelf)entry.root.anchoredPosition+=Vector2.down*rowOffset;
        }
        comparisonScroll.viewport.sizeDelta=new Vector2(comparisonScroll.viewport.sizeDelta.x,height);
        if(!sideActions){
            ((RectTransform)equip.transform).anchoredPosition=equipPosition+Vector2.up*reduction;
            ((RectTransform)decompose.transform).anchoredPosition=decomposePosition+Vector2.up*reduction;
        }
        for(int i=0;i<detailDividers.Length;i++)detailDividers[i].sizeDelta=dividerSizes[i]-Vector2.up*(dividerSizes[i].y>100?reduction:0);
        layout.detailHeight=authoredDetailHeight-reduction;layout.Apply();
    }
    static void SetComparisonColumnVisible(ComparisonRow row,int column,bool visible)
    {
        row.root.Find("StatIcon"+column)?.gameObject.SetActive(visible);
        row.root.Find("Label"+column)?.gameObject.SetActive(visible);
        var rules=row.root.Cast<Transform>().Where(t=>t.name=="InkRule").OrderBy(t=>((RectTransform)t).anchorMin.x).ToArray();
        if(column<rules.Length)rules[column].gameObject.SetActive(visible);
    }
    public void EquipSelected()
    {
        if(IsNavigating||HasOverlay)return;
        if(BatchMode){foreach(var i in items)if(i!=Equipped(Category)&&!i.isLocked)batch.Add(i.uniqueId);RefreshSelection();return;}
        var item=Selected;if(item==null||item==Equipped(Category)||!Owned(Category).Contains(item))return;
        var old=Equipped(Category);if(old!=null)InventoryManager.single.AddItem(Utilities.DeepClone(old));
        SetEquipped(Category,Utilities.DeepClone(item));InventoryManager.single.RemoveItem(item);
        GameManager.single.CalcPlayer();if(Player.single)Player.single.SetSkinsAndAttachments();
        SoundManager.Instance.PlayClip("EQUIP_ITEM");Rebuild();
    }
    public void ToggleLock(){if(Selected==null||BatchMode||HasOverlay)return;Selected.isLocked=!Selected.isLocked;Remember();Rebuild();}
    public void ToggleBatch(){if(IsNavigating||HasOverlay)return;BatchMode=!BatchMode;batch.Clear();RefreshSelection();}
    public void RequestDecompose()
    {
        if(IsNavigating||HasOverlay)return;
        var targets=BatchMode?items.Where(i=>batch.Contains(i.uniqueId)).ToArray():Selected==null?Array.Empty<Item>():new[]{Selected};
        targets=targets.Where(i=>Owned(Category).Contains(i)&&!i.isLocked).ToArray();if(targets.Length==0)return;
        float experience=targets.Sum(i=>i.fusionExp);
        modalTitle.text="确认分解";modalMessage.text="分解 "+targets.Length+" 件装备\n获得 "+Utilities.ConvertNumber(experience)+" 融合经验\n已装备及锁定装备不会被分解";
        confirmed=()=>{
            float gain=0;foreach(var item in targets){if(!Owned(item.m_ItemType).Contains(item)||item.isLocked)continue;gain+=item.fusionExp;InventoryManager.single.RemoveItem(item);}
            if(gain>0){GameManager.single.IncFusion(gain);if(fusionReminder)fusionReminder.Notify(gain);}BatchMode=false;batch.Clear();Rebuild();
        };StorybookPopupMotion.Show(modal);
    }
    public void SetSort(int mode){Remember();sortModes[Category]=Mathf.Clamp(mode,0,2);Rebuild(true);}
    public void SetFilter(int mode){Remember();filterModes[Category]=Mathf.Clamp(mode,0,2);Rebuild(true);}
    void OpenOptions(bool sorting)
    {
        if(IsNavigating||HasOverlay||BatchMode)return;StorybookPopupMotion.Show(options);optionsTitle.text=sorting?"装备排序":"装备筛选";
        string[] labels=sorting?new[]{"获得顺序","主属性从高到低","品质从高到低","取消"}:new[]{"全部装备","仅未装备","仅未锁定","取消"};
        for(int i=0;i<4;i++){int n=i;optionCaptions[i].text=labels[i];optionButtons[i].onClick.RemoveAllListeners();optionButtons[i].onClick.AddListener(()=>{CloseOverlays();if(n<3){if(sorting)SetSort(n);else SetFilter(n);}});}
    }
    public void CloseOverlays(){if(modal)StorybookPopupMotion.Hide(modal);if(options)StorybookPopupMotion.Hide(options);confirmed=null;}
    public void RefreshStats()
    {
        var gm=GameManager.single;if(!gm||gm.currentPlayerStat==null)return;
        level.text="Lv. "+gm.playerLevel;number.text="No. "+gm.SubjectNumber;
        if(sharedAttributes)sharedAttributes.RefreshValues(!IsNavigating,CanBuy);
        else {
        points.text="可用点数 <color=#761E20><size=39>"+gm.statPoints+"</size></color>";
        var s=gm.currentPlayerStat;float[] v={s.hpMax,s.dmg,s.def,s.atkSpd,s.crit,s.critDmg};
        for(int i=0;i<6;i++){statValues[i].text=Utilities.ConvertNumber(v[i])+(i>=3?"%":"");statButtons[i].interactable=!IsNavigating&&CanBuy(i);}
        }
        if(fusionFill)fusionFill.fillAmount=gm.fxpMax>0?Mathf.Clamp01(gm.fxpNow/gm.fxpMax):0;
        if(fusionText&&fusionFill)fusionText.text=Mathf.FloorToInt(fusionFill.fillAmount*100)+"%";
        skillBadge.text=gm.skillPoints.ToString();skillBadge.transform.parent.gameObject.SetActive(gm.skillPoints>0);
        rebirthText.gameObject.SetActive(gm.rebirthLevel>0);rebirthText.text="转生 "+gm.rebirthLevel;
        var im=InventoryManager.single;bool[] flags={im.weaponFlag,im.helmetFlag,im.shieldFlag,im.armorFlag,im.accFlag};
        if(categoryIcons!=null&&categoryIcons.Length>0)categoryIcons[0].color=Category==ItemType.WEAPON?Color.white:Color.black;
        for(int i=0;i<5;i++){categoryBadges[i].SetActive(flags[i]);categoryBackgrounds[i].gameObject.SetActive(types[i]==Category);categoryButtons[i].GetComponentInChildren<Text>().color=types[i]==Category?new Color(.98f,.93f,.81f):Color.black;}
    }
    public bool CanBuy(int i){if(i<0||i>=6)return false;return GameManager.single.statPoints>=(LevelController.Instance.GetStatLevel(statTypes[i])>=100?2:1)&&!PurchaserManager.Instance.LastStatPurchased(statTypes[i]);}
    public void BuyStat(int i){if(IsNavigating||HasOverlay||!attributesExpanded||!CanBuy(i))return;GameManager.single.statPoints-=LevelController.Instance.GetStatLevel(statTypes[i])>=100?2:1;LevelController.Instance.IncStat(statTypes[i]);SoundManager.Instance.PlayClip("STAT_BUY");RefreshStats();}
    public void ToggleAttributes(){if(IsNavigating||HasOverlay)return;attributesExpanded=!attributesExpanded;if(folding!=null)StopCoroutine(folding);folding=StartCoroutine(Fold());}
    IEnumerator Fold(){float start=layout.expansion,end=attributesExpanded?1:0;attributeBody.blocksRaycasts=attributeBody.interactable=false;for(float t=0;t<foldDuration;t+=Time.unscaledDeltaTime){ApplyFold(Mathf.Lerp(start,end,Mathf.SmoothStep(0,1,t/foldDuration)),false);yield return null;}ApplyFold(end);folding=null;}
    void ApplyFold(float value,bool settled=true){layout.expansion=value;layout.Apply();if(sharedAttributes){sharedAttributes.SetFold(value,settled);return;}attributeBody.alpha=value;attributeBody.gameObject.SetActive(value>0);attributeBody.blocksRaycasts=attributeBody.interactable=settled&&value>=.999f;foldArrow.localEulerAngles=new Vector3(0,0,180*value);}
    void Navigate(Action action){if(IsNavigating||HasOverlay)return;IsNavigating=true;Remember();action();}
    public static string GradeName(ItemGrade grade){string[] names={"普通","精良","稀有","史诗","传说",""};return names[Mathf.Clamp((int)grade,0,5)];}
    public static Color GradeColor(ItemGrade g){Color[] colors={new Color(.55f,.57f,.56f),new Color(.3f,.48f,.25f),new Color(.16f,.37f,.57f),new Color(.48f,.25f,.57f),new Color(.7f,.47f,.13f)};return colors[Mathf.Clamp((int)g,0,4)];}
    public static float PrimaryValue(Item i){if(i?.baseStat==null)return 0;return i.m_ItemType==ItemType.WEAPON?i.baseStat.dmg:i.m_ItemType==ItemType.ACCESSORY?i.baseStat.hpMax:i.baseStat.def;}
    public static string PrimaryText(Item i)=>(i.m_ItemType==ItemType.WEAPON?"攻击 ":i.m_ItemType==ItemType.ACCESSORY?"生命 ":"防御 ")+Utilities.ConvertNumber(PrimaryValue(i));
    public static string Value(float n,int stat)=>Utilities.ConvertNumber(n)+(stat>=3?"%":"");
    public static float[] Values(Item i){var s=i?.baseStat;return s==null?new float[9]:new[]{s.hpMax,s.dmg,s.def,s.atkSpd,s.crit,s.critDmg,s.hpPerc,s.dmgPerc,s.defPerc};}
}
