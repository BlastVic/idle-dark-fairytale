using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Services;

public sealed class DeveloperOverlayUI : MonoBehaviour
{
    public RectTransform panel;
    public GameObject body,confirmation;
    public Button fold,close,points,stats,fusion,currency,normal,fast,unlock,kill,reset,confirmReset,cancelReset;
    public Text status,foldLabel;
    [System.NonSerialized] public DebugMenu owner;
    public bool Expanded=>body.activeSelf;
    Vector2 lastSize;Rect lastSafe;
    void Awake(){Bind(fold,()=>SetExpanded(!Expanded));Bind(close,()=>owner.Close());Bind(points,()=>{GameManager.single.statPoints+=10;GameManager.single.skillPoints+=10;Refresh();});Bind(stats,()=>{var gm=GameManager.single;gm.hpLevel+=2;gm.dmgLevel++;gm.atkSpdLevel++;Refresh();});Bind(fusion,()=>GameManager.single.IncFusion(GameManager.single.fxpMax*.25f));Bind(currency,()=>{GameManager.single.premiumCurrency+=100;Refresh();});Bind(normal,()=>Time.timeScale=1);Bind(fast,()=>Time.timeScale=2);Bind(unlock,()=>EventManager.single.CallUnlockMaps());Bind(kill,()=>{var enemies=GameManager.single.GetLivingEnemies();if(enemies.Count>0)enemies[0].Hit(enemies[0].currentStat.hpNow*2);});Bind(reset,()=>confirmation.SetActive(true));Bind(cancelReset,()=>confirmation.SetActive(false));Bind(confirmReset,()=>{confirmation.SetActive(false);SaveManager.Instance.DeleteData();Refresh();});}
    void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick.AddListener(()=>{StorybookNavigation.PlayClick();action();});}
    void Refresh(){if(GameplayCanvas.single)GameplayCanvas.single.RefreshCampUI();}
    public void SetExpanded(bool value){body.SetActive(value);confirmation.SetActive(false);foldLabel.text=value?"收起":"展开";panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,value?480:52);Clamp();}
    void OnDisable(){confirmation.SetActive(false);}
    void LateUpdate(){var root=(RectTransform)transform;if(lastSize!=root.rect.size||lastSafe!=Screen.safeArea){lastSize=root.rect.size;lastSafe=Screen.safeArea;Clamp();}var gm=GameManager.single;bool ready=gm&&GameplayCanvas.single;foreach(var b in new[]{points,stats,fusion,currency,unlock,reset})b.interactable=ready;kill.interactable=ready&&gm.GetLivingEnemies().Count>0;normal.interactable=!Mathf.Approximately(Time.timeScale,1);fast.interactable=!Mathf.Approximately(Time.timeScale,2);status.text=ready?$"Lv.{gm.playerLevel}   属性点 {gm.statPoints}   技能点 {gm.skillPoints}\n{Time.timeScale:0.#} 倍速 · FPS {1f/Mathf.Max(Time.unscaledDeltaTime,.001f):0}":"等待游戏加载…";}
    public void Clamp(){if(!panel)return;var root=(RectTransform)transform;var size=root.rect.size;var safe=Screen.safeArea;float sx=size.x/Screen.width,sy=size.y/Screen.height;float left=safe.xMin*sx+8,right=safe.xMax*sx-8,top=(Screen.height-safe.yMax)*sy+8,bottom=(Screen.height-safe.yMin)*sy-8;float scale=Mathf.Min(1,(right-left)/panel.rect.width,(bottom-top)/panel.rect.height);panel.localScale=Vector3.one*scale;var p=panel.anchoredPosition;p.x=Mathf.Clamp(p.x,left,Mathf.Max(left,right-panel.rect.width*scale));p.y=-Mathf.Clamp(-p.y,top,Mathf.Max(top,bottom-panel.rect.height*scale));panel.anchoredPosition=p;}
}
