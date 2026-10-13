using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only: rewards are granted by the existing gameplay systems.</summary>
public sealed class StorybookItemToast : MonoBehaviour
{
    public RectTransform card;
    public CanvasGroup group;
    public Image icon,qualityAccent;
    public Text heading,itemName,detail;
    public Vector2 designSize=new Vector2(800,190);
    public float visualScale=.9f;
    public float topInset=460,enterDuration=.24f,holdDuration=2.1f,exitDuration=.22f;
    float motion;
    public bool Presenting {get;private set;}
    public void Present(Item item)
    {
        bool reward=item.m_IconName=="Stat"||item.m_IconName=="Skill"||item.m_IconName=="Exp"||item.itemGrade==ItemGrade.UI_ONLY;
        icon.sprite=InventoryManager.single.GetSpriteIcon(item.m_IconName);icon.enabled=icon.sprite;
        heading.text=reward?"获得奖励":"获得装备";
        string name=string.IsNullOrWhiteSpace(item.m_ItemFinalTitle)?item.m_ItemBasicTitle:item.m_ItemFinalTitle;
        if(item.m_IconName=="Stat")name=(name??"").Replace("Stats","属性点");
        if(item.m_IconName=="Skill")name=(name??"").Replace("Skills","技能点");
        if(item.m_IconName=="Exp")name=(name??"").Replace("XP","经验");
        itemName.text=string.IsNullOrWhiteSpace(name)?"旅途收获":name;
        detail.text=reward?"奖励已获得":StorybookEquipmentUI.GradeName(item.itemGrade)+(item.baseStat!=null?"  ·  "+StorybookEquipmentUI.PrimaryText(item):"");
        qualityAccent.color=reward?new Color(.55f,.12f,.08f):StorybookEquipmentUI.GradeColor(item.itemGrade);
        gameObject.SetActive(true);StopAllCoroutines();Presenting=true;group.alpha=0;group.blocksRaycasts=false;group.interactable=false;StartCoroutine(Animate());
    }
    bool Covered(){var t=TransitionCanvas.single;return (t&&t.isBlack)||FindAnyObjectByType<CursedBookIntro>()||(GameplayCanvas.single&&GameplayCanvas.single.levelUpUI&&GameplayCanvas.single.levelUpUI.Presenting);}
    IEnumerator Animate()
    {
        float elapsed=0,total=enterDuration+holdDuration+exitDuration;
        while(elapsed<total){
            if(Covered()){group.alpha=0;yield return null;continue;}
            elapsed+=Time.unscaledDeltaTime;
            float a=elapsed<enterDuration?Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,enterDuration)):elapsed<enterDuration+holdDuration?1:1-Mathf.SmoothStep(0,1,(elapsed-enterDuration-holdDuration)/Mathf.Max(.01f,exitDuration));
            group.alpha=a;motion=(1-a)*20;Place();yield return null;
        }
        Presenting=false;gameObject.SetActive(false);if(GameplayCanvas.single)GameplayCanvas.single.ItemPopUpEnded();
    }
    void LateUpdate()=>Place();
    void OnDisable(){StopAllCoroutines();Presenting=false;if(group){group.alpha=0;group.blocksRaycasts=false;}}
    public void Place(){
        var root=(RectTransform)transform;var bounds=root.rect;if(bounds.width<1||!card)return;
        var normalized=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        Vector2 available=Vector2.Scale(bounds.size,normalized.size);
        float scale=Mathf.Min(available.x/1080,available.y/1500)*visualScale;
        card.anchorMin=card.anchorMax=card.pivot=Vector2.one*.5f;card.sizeDelta=designSize;card.localScale=Vector3.one*scale;
        card.anchoredPosition=new Vector2(bounds.xMin+bounds.width*normalized.center.x,bounds.yMin+bounds.height*normalized.yMax-(topInset+designSize.y*.5f-motion)*scale);
    }
}
