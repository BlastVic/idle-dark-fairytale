using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Positions and binds the saved tutorial prefab; never creates UI nodes.</summary>
public sealed class StorybookTutorialUI : MonoBehaviour
{
    [Serializable] public class Step {public string title;[TextArea] public string body;public string cue;}
    public Step[] steps;
    public RectTransform card,highlight,arrow;
    public Image[] dimmers;
    public Text heading,body,cue;
    public Button close;
    public CanvasGroup presentation,glow;
    public float fadeDuration=.2f, targetPadding=10, cardGap=155;
    public Vector2 cardSize=new Vector2(940,470);
    public Button Target {get;private set;}
    public bool Visible=>gameObject.activeSelf;
    public Rect TargetRect {get;private set;}
    public Rect CardRect {get;private set;}
    public Action completed,dismissed,lostTarget;
    UnityAction targetListener;
    RectTransform root;
    float opened;
    void Awake(){root=(RectTransform)transform;close.onClick.AddListener(()=>{if(Time.unscaledTime-opened<.25f)return;IdleKnightHero.UI.SoundManager.Instance.PlayClip("CLICK");dismissed?.Invoke();});}
    public void Show(int key,Button target,Action done,Action cancel,Action lost,string title=null,string text=null,string hint=null){
        Detach();Target=target;completed=done;dismissed=cancel;lostTarget=lost;
        var step=steps[key];heading.text=title??step.title;body.text=text??step.body;cue.text=hint??step.cue;
        StorybookPopupMotion.Show(gameObject);opened=Time.unscaledTime;
        targetListener=()=>{if(Time.unscaledTime-opened>=.2f)completed?.Invoke();};Target.onClick.AddListener(targetListener);
        Place();
    }
    void LateUpdate(){var motion=GetComponent<StorybookPopupMotion>();if(motion&&motion.Closing)return;if(!Target||!Target.gameObject.activeInHierarchy){lostTarget?.Invoke();return;}Place();glow.alpha=.78f+.22f*Mathf.Sin((Time.unscaledTime-opened)*3);}
    void Update(){if(Input.GetKeyDown(KeyCode.Escape)&&Time.unscaledTime-opened>.25f)dismissed?.Invoke();}
    public void Hide(){Detach();StorybookPopupMotion.Hide(gameObject);}
    void Detach(){if(Target&&targetListener!=null)Target.onClick.RemoveListener(targetListener);targetListener=null;Target=null;}
    void OnDisable(){Detach();if(presentation){presentation.blocksRaycasts=false;presentation.interactable=false;}}
    public void Place(){
        if(!root)root=(RectTransform)transform;if(!Target||root.rect.width<1)return;
        var corners=new Vector3[4];((RectTransform)Target.transform).GetWorldCorners(corners);
        var canvas=Target.GetComponentInParent<Canvas>();Camera camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        foreach(var corner in corners){var screen=RectTransformUtility.WorldToScreenPoint(camera,corner);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var p);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        Rect bounds=root.rect;var n=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        Vector2 safeMin=bounds.min+Vector2.Scale(bounds.size,n.min),safeMax=bounds.min+Vector2.Scale(bounds.size,n.max);
        var supported=ShelterResponsiveLayout.SupportedSize(bounds.size);safeMin=Vector2.Max(safeMin,bounds.center-supported*.5f);safeMax=Vector2.Min(safeMax,bounds.center+supported*.5f);
        float scale=Mathf.Min((safeMax.x-safeMin.x)/1080,(safeMax.y-safeMin.y)/1500);float pad=targetPadding*scale;
        min=Vector2.Max(min-Vector2.one*pad,safeMin);max=Vector2.Min(max+Vector2.one*pad,safeMax);
        if(max.x<=min.x||max.y<=min.y){lostTarget?.Invoke();return;}
        TargetRect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);Box(highlight,TargetRect);
        Box(dimmers[0].rectTransform,Rect.MinMaxRect(bounds.xMin,max.y,bounds.xMax,bounds.yMax));
        Box(dimmers[1].rectTransform,Rect.MinMaxRect(bounds.xMin,bounds.yMin,bounds.xMax,min.y));
        Box(dimmers[2].rectTransform,Rect.MinMaxRect(bounds.xMin,min.y,min.x,max.y));
        Box(dimmers[3].rectTransform,Rect.MinMaxRect(max.x,min.y,bounds.xMax,max.y));
        float margin=24*scale,w=cardSize.x*scale,h=cardSize.y*scale;
        float above=safeMax.y-max.y,below=min.y-safeMin.y;bool onTop=above>=h+margin&&(above>=below||below<h+margin);
        float gap=Mathf.Min(cardGap*scale,Mathf.Max(16*scale,(onTop?above:below)-h-margin));
        float y=onTop?max.y+gap:min.y-gap-h;
        y=Mathf.Clamp(y,safeMin.y+margin,safeMax.y-margin-h);
        float x=(safeMin.x+safeMax.x-w)*.5f;
        CardRect=new Rect(x,y,w,h);card.anchorMin=card.anchorMax=card.pivot=Vector2.one*.5f;card.sizeDelta=cardSize;card.localScale=Vector3.one*scale;card.anchoredPosition=CardRect.center;
        float arrowH=Mathf.Min(200*scale,gap-12*scale);arrow.gameObject.SetActive(arrowH>24*scale);
        arrow.anchorMin=arrow.anchorMax=arrow.pivot=Vector2.one*.5f;arrow.sizeDelta=new Vector2(arrowH*.55f,arrowH);arrow.localEulerAngles=new Vector3(0,0,onTop?0:180);
        float ax=Mathf.Clamp(TargetRect.center.x,x+40*scale,x+w-40*scale);
        arrow.anchoredPosition=new Vector2(ax,onTop?max.y+gap*.5f:min.y-gap*.5f);
    }
    static void Box(RectTransform r,Rect bounds){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=bounds.center;r.sizeDelta=new Vector2(Mathf.Max(0,bounds.width),Mathf.Max(0,bounds.height));}
}
