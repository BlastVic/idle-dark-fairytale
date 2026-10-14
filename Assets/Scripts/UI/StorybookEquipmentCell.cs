using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class StorybookEquipmentCell : MonoBehaviour
{
    public Button button;
    public Image icon, quality, selectedFrame;
    public Image cardBackground;
    public Color normalBackground=Color.white, selectedBackground=new Color(1f,.76f,.66f,1f);
    public GameObject stateRibbon, lockIcon, multiMark;
    public Text primary, state, detailLabels, detailValues;
    public GameObject detailedBody;
    public RectTransform compactIconPose, detailedIconPose, visual;
    public Item Item {get;private set;}
    StorybookEquipmentUI owner;
    void Awake()=>button.onClick.AddListener(()=>{if(owner)owner.Select(Item);});
    public void Bind(StorybookEquipmentUI ui,Item item,bool equipped,bool selected,bool multi)
    {
        owner=ui;Item=item;
        float scale=ui.grid.cellSize.x/157.4f;
        visual.sizeDelta=new Vector2(157.4f,ui.grid.cellSize.y/scale);visual.localScale=Vector3.one*scale;
        var im=InventoryManager.single;
        icon.sprite=im.GetSpriteIcon(item.m_IconName);icon.enabled=icon.sprite;
        quality.color=StorybookEquipmentUI.GradeColor(item.itemGrade);
        primary.text=StorybookEquipmentUI.PrimaryText(item);
        primary.gameObject.SetActive(!ui.detailedMode);detailedBody.SetActive(ui.detailedMode);
        var pose=ui.detailedMode?detailedIconPose:compactIconPose;
        icon.rectTransform.anchoredPosition=pose.anchoredPosition;icon.rectTransform.sizeDelta=pose.sizeDelta;
        var indices=StorybookEquipmentUI.StatIndices(item).ToArray();var values=StorybookEquipmentUI.Values(item);
        detailLabels.text=string.Join("\n",indices.Select((v,n)=>n==0?"<b>"+StorybookEquipmentUI.StatNames[v]+"</b>":StorybookEquipmentUI.StatNames[v]));
        detailValues.text=string.Join("\n",indices.Select((v,n)=>n==0?"<b>"+StorybookEquipmentUI.Value(values[v],v)+"</b>":StorybookEquipmentUI.Value(values[v],v)));
        bool highlighted=ui.BatchMode?multi:selected;
        selectedFrame.gameObject.SetActive(highlighted);
        if(cardBackground)cardBackground.color=highlighted?selectedBackground:normalBackground;
        stateRibbon.SetActive(!ui.BatchMode&&(equipped||selected));state.text=equipped?"已装备":"选中";
        stateRibbon.GetComponent<Image>().color=equipped?new Color(.32f,.25f,.15f):new Color(.5f,.07f,.065f);
        lockIcon.SetActive(item.isLocked);multiMark.SetActive(multi);
        button.interactable=!ui.IsNavigating&&(!ui.BatchMode||(!equipped&&!item.isLocked));
    }
}
