using UnityEngine;
using UnityEngine.UI;
using IdleKnightHero.UI;

/// <summary>Shared authored navigation. Only selection, data and safe-area placement vary by page.</summary>
[ExecuteAlways, DefaultExecutionOrder(10000)]
public sealed class StorybookNavigation : MonoBehaviour
{
    public RectTransform pageRoot;
    public Button[] buttons;
    public GameObject[] bookmarks;
    public Text[] captions;
    public Text skillBadge;
    public Image shelterIcon;
    public Material shelterOnPaper,shelterOnRed;
    [Range(0,4)] public int selectedIndex=2;
    public float height=218.06589f, bottomInset=39.022316f, sideInset=10.3294f;
    public Font normalFont,selectedFont;
    public Color normalText=Color.black, selectedText=new Color(.98f,.92f,.81f);
    public int normalFontSize=33, selectedFontSize=42;
    static int soundFrame=-1;
#if UNITY_EDITOR
    public static int ClickSoundCount {get;private set;}
#endif
    void Awake(){if(Application.isPlaying&&buttons!=null)foreach(var button in buttons)button.onClick.AddListener(PlayClick);}
    public static void PlayClick(){if(soundFrame==Time.frameCount)return;soundFrame=Time.frameCount;
#if UNITY_EDITOR
        ClickSoundCount++;
#endif
        SoundManager.Instance.PlayClip("CLICK");}
    void OnEnable(){Apply();Refresh();}
    void LateUpdate(){Apply();Refresh();}
    Vector2 Bounds(out Vector2 lo,out Vector2 hi){
        var size=pageRoot.rect.size;var bounds=ShelterResponsiveLayout.SupportedSize(size);
        var safe=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        lo=Vector2.Max((size-bounds)*.5f,Vector2.Scale(safe.min,size));
        hi=Vector2.Min((size+bounds)*.5f,Vector2.Scale(safe.max,size));return hi-lo;
    }
    public float ReservedHeight(RectTransform core){if(!pageRoot)return height+bottomInset+12;var usable=Bounds(out _,out _);float scale=Mathf.Min(usable.x/1080,usable.y/1920);return (height+bottomInset+12)*scale*pageRoot.lossyScale.y/core.lossyScale.y;}
    public void Apply(){
        if(!pageRoot||!transform.parent)return;
        var usable=Bounds(out var lo,out var hi);float scale=Mathf.Min(usable.x/1080,usable.y/1920);if(scale<=0)return;
        var r=(RectTransform)transform;var parent=(RectTransform)transform.parent;
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.sizeDelta=new Vector2(usable.x/scale-2*sideInset,height);
        r.localScale=Vector3.one*(scale*pageRoot.lossyScale.x/parent.lossyScale.x);
        var center=new Vector2((lo.x+hi.x)*.5f,lo.y+(bottomInset+height*.5f)*scale);
        r.position=pageRoot.TransformPoint(center-Vector2.Scale(pageRoot.rect.size,pageRoot.pivot));
    }
    public void Refresh(){
        if(bookmarks==null||captions==null)return;
        for(int i=0;i<bookmarks.Length;i++){bool selected=i==selectedIndex;bookmarks[i].SetActive(selected);captions[i].color=selected?selectedText:normalText;captions[i].fontSize=selected?selectedFontSize:normalFontSize;if(normalFont&&selectedFont)captions[i].font=selected?selectedFont:normalFont;}
        if(shelterIcon)shelterIcon.material=selectedIndex==2?shelterOnRed:shelterOnPaper;
        if(Application.isPlaying&&GameManager.single&&skillBadge){int points=GameManager.single.skillPoints;skillBadge.text=points.ToString();skillBadge.transform.parent.gameObject.SetActive(points>0);}
    }
}
