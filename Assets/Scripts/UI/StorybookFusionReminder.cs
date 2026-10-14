using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored equipment-page notification, shown only after successful decomposition.</summary>
public sealed class StorybookFusionReminder : MonoBehaviour
{
    public RectTransform card;
    public CanvasGroup group;
    public Button open, close;
    public Text heading, progress;
    public Image fill;
    public float lifetime = 8;
    StorybookEquipmentUI owner;
    bool pending, visible;
    float age, gained;
    public bool Presenting => visible;
    void Awake()
    {
        owner=GetComponentInParent<StorybookEquipmentUI>();
        open.onClick.AddListener(()=>{if(!visible)return;Hide();owner.OpenFusion();});
        close.onClick.AddListener(()=>{StorybookNavigation.PlayClick();Hide();});
        Hide();
    }
    public void Notify(float amount)
    {
        if(amount<=0)return;
        gained+=amount;pending=true;age=0;
    }
    void Update()
    {
        if(!pending)return;
        if(!owner||owner.IsNavigating){Hide();return;}
        if(owner.HasOverlay || (TutorialManager.single && TutorialManager.single.view && TutorialManager.single.view.Visible))
        {visible=false;card.gameObject.SetActive(false);group.alpha=0;group.blocksRaycasts=group.interactable=false;return;}
        visible=true;card.gameObject.SetActive(true);age+=Time.unscaledDeltaTime;
        group.alpha=Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01((lifetime-age)/.25f));
        group.blocksRaycasts=group.interactable=age<lifetime-.25f;
        var gm=GameManager.single;float fraction=gm.fxpMax>0?Mathf.Clamp01(gm.fxpNow/gm.fxpMax):0;
        fill.fillAmount=fraction;
        heading.text=fraction>=1?"融合已就绪":"融合经验 +"+Utilities.ConvertNumber(gained);
        progress.text=Mathf.FloorToInt(fraction*100)+"%";
        var root=((RectTransform)transform).rect;
        var safe=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        float scale=Mathf.Min(root.width*safe.width/1080,root.height*safe.height/1500)*.9f;
        card.localScale=Vector3.one*scale;
        card.anchoredPosition=new Vector2(root.xMin+root.width*safe.center.x,root.yMin+root.height*safe.yMax-545*scale+16*(1-Mathf.Clamp01(age/.2f))*scale);
        if(age>=lifetime)Hide();
    }
    public void Hide(){pending=visible=false;gained=age=0;if(card)card.gameObject.SetActive(false);if(group){group.alpha=0;group.blocksRaycasts=group.interactable=false;}}
    void OnDisable()=>Hide();
}
