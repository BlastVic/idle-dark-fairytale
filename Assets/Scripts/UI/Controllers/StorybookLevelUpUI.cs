using UnityEngine;
using UnityEngine.UI;

/// <summary>Editable paper banner and authored glints; never grants progression rewards.</summary>
public sealed class StorybookLevelUpUI : MonoBehaviour
{
    public RectTransform card;
    public CanvasGroup group;
    public Text level,rewards,heading;
    public Graphic[] sparks;
    public Vector2 designSize=new Vector2(800,220);
    public float topInset=450,visualScale=.9f,duration=2.5f,fadeDuration=.28f;
    public bool Presenting {get;private set;}
    public int FromLevel {get;private set;}
    public int ToLevel {get;private set;}
    public int AttributePoints {get;private set;}
    public int SkillPoints {get;private set;}
    float elapsed;
    Vector2[] origins;
    void Awake(){origins=new Vector2[sparks.Length];for(int i=0;i<sparks.Length;i++)origins[i]=sparks[i].rectTransform.anchoredPosition;}
    public void Present(int newLevel)
    {
        if(!Presenting){FromLevel=newLevel-1;AttributePoints=0;SkillPoints=0;}
        ToLevel=newLevel;AttributePoints++;if(newLevel%5==0)SkillPoints++;
        level.text="Lv. "+FromLevel+"  →  "+ToLevel;
        heading.text=ToLevel-FromLevel>1?"连续升级":"等级提升";
        rewards.text="属性点 +"+AttributePoints+(SkillPoints>0?"    ·    技能点 +"+SkillPoints:"");
        elapsed=0;Presenting=true;gameObject.SetActive(true);group.alpha=0;group.blocksRaycasts=group.interactable=false;Place(0);
    }
    void Update()
    {
        var t=TransitionCanvas.single;if((t&&t.isBlack)||FindAnyObjectByType<CursedBookIntro>()){group.alpha=0;return;}
        elapsed+=Time.unscaledDeltaTime;float a=Mathf.Min(Mathf.Clamp01(elapsed/fadeDuration),Mathf.Clamp01((duration-elapsed)/fadeDuration));a=Mathf.SmoothStep(0,1,a);group.alpha=a;Place(a);
        for(int i=0;i<sparks.Length;i++){
            float p=Mathf.Clamp01((elapsed-i*.035f)/1.6f);sparks[i].rectTransform.anchoredPosition=origins[i]+new Vector2((i%2==0?-1:1)*35*p,60*p);
            sparks[i].rectTransform.localScale=Vector3.one*(.55f+.7f*Mathf.Sin(p*Mathf.PI));
            var c=sparks[i].color;c.a=Mathf.Sin(p*Mathf.PI)*.85f;sparks[i].color=c;
        }
        if(elapsed>=duration){Presenting=false;gameObject.SetActive(false);}
    }
    void Place(float alpha){var root=(RectTransform)transform;var r=root.rect;var safe=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));var available=Vector2.Scale(r.size,safe.size);float scale=Mathf.Min(available.x/1080,available.y/1500)*visualScale;card.anchorMin=card.anchorMax=card.pivot=Vector2.one*.5f;card.sizeDelta=designSize;card.localScale=Vector3.one*scale;card.anchoredPosition=new Vector2(r.xMin+r.width*safe.center.x,r.yMin+r.height*safe.yMax-(topInset+designSize.y*.5f-16*(1-alpha))*scale);}
    void OnDisable(){Presenting=false;if(group){group.alpha=0;group.blocksRaycasts=false;}}
}
