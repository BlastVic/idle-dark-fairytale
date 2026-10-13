using UnityEngine;
using UnityEngine.UI;

/// <summary>Only binds and animates authored prefab nodes; placement is projected from the hit position.</summary>
public sealed class StorybookCombatFloat : MonoBehaviour
{
    public RectTransform card;
    public Text value,caption;
    public CanvasGroup group;
    public bool experience;
    public float duration=.95f,rise=72,normalSize=38,criticalSize=52;
    public Color normal=new Color(.96f,.91f,.78f),playerDamage=new Color(1,.36f,.30f),critical=new Color(1,.77f,.35f),pet=new Color(.80f,.57f,1),lightning=new Color(.48f,.83f,1);
    Camera sourceCamera;Vector3 world;float elapsed;bool configured;Vector2 drift;
    public void Present(Vector3 position,Camera camera,bool hitPlayer=false,bool crit=false,float amount=0,bool purple=false,bool blue=false)
    {
        world=position;sourceCamera=camera;configured=true;
        value.text=experience?"经验 ×2":Utilities.ConvertNumber(amount);
        value.fontSize=Mathf.RoundToInt(crit?criticalSize:normalSize);value.resizeTextMaxSize=value.fontSize;
        value.color=experience?critical:hitPlayer?playerDamage:purple?pet:blue?lightning:crit?critical:normal;
        caption.gameObject.SetActive(!experience&&crit);caption.text="暴击";caption.color=value.color;
        drift=new Vector2(Random.Range(-18f,18f),0);group.blocksRaycasts=group.interactable=false;Place();
    }
    void LateUpdate(){if(!configured)return;elapsed+=Time.deltaTime;float t=elapsed/Mathf.Max(.1f,duration);if(t>=1){Destroy(gameObject);return;}group.alpha=t<.12f?Mathf.Clamp01(t/.06f):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t));float punch=t<.16f?Mathf.Lerp(.78f,1.12f,t/.16f):Mathf.Lerp(1.12f,1,Mathf.Clamp01((t-.16f)/.18f));card.localScale=Vector3.one*punch;Place();}
    void Place(){if(!sourceCamera){group.alpha=0;return;}var screen=sourceCamera.WorldToScreenPoint(world);if(screen.z<=0){group.alpha=0;return;}var root=(RectTransform)transform;var safe=Screen.safeArea;float sx=root.rect.width/Screen.width,sy=root.rect.height/Screen.height;float halfW=card.rect.width*card.localScale.x*.5f,halfH=card.rect.height*card.localScale.y*.5f;float t=Mathf.Clamp01(elapsed/Mathf.Max(.1f,duration));var p=new Vector2(screen.x*sx+drift.x*t,screen.y*sy+rise*t);p.x=Mathf.Clamp(p.x,safe.xMin*sx+halfW,safe.xMax*sx-halfW);p.y=Mathf.Clamp(p.y,safe.yMin*sy+halfH,safe.yMax*sy-halfH);card.anchoredPosition=p;}
}
