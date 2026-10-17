using System;
using Assets.Scripts.Game.Enum;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data and animation for the persisted boss banner. No runtime UI construction.</summary>
public sealed class StorybookBossHUD : MonoBehaviour
{
    [Serializable] public class PortraitEntry { public string key,displayName; public Sprite portrait; }
    public PortraitEntry[] portraits;
    public RectTransform panel,header,portraitRoot,fillTrack,chargeTip;
    public CanvasGroup group,impact;
    public Image portrait,fill;
    public Text bossName,health,eyebrow;
    public float chargeDuration=1.25f,entranceDuration=.22f,gap=16,sideInset=18;
    public Actor_Base Target {get;private set;}
    public bool Presenting {get;private set;}
    public bool Charging => Presenting&&elapsed<chargeDuration;
    public bool EntranceFinished => Presenting&&elapsed>=chargeDuration+.25f;
    public int PresentationId {get;private set;}
    Action onEntranceStarted;
    float elapsed;
    string currentKey;
    readonly Vector3[] corners=new Vector3[4];
    public void BeginWave(Wave wave, Action onEntrance = null){
        Hide();if(wave==null||wave.enemies==null)return;
        foreach(var key in wave.enemies)foreach(var entry in portraits)if(entry.key==key){
            currentKey=key;bossName.text=entry.displayName;portrait.sprite=entry.portrait;
            onEntranceStarted=onEntrance;
            elapsed=0;Presenting=true;fill.fillAmount=0;health.text="";eyebrow.text="首领降临";Place();return;
        }
    }
    public void Bind(Actor_Base actor){
        if(!actor||!actor.isBoss)return;
        string key=actor.name.Replace("(Clone)","").Trim();
        if(Presenting&&key==currentKey)Target=actor;
    }
    public void Hide(){onEntranceStarted=null;PresentationId++;Presenting=false;Target=null;currentKey=null;if(group)group.alpha=0;if(impact)impact.alpha=0;if(chargeTip)chargeTip.gameObject.SetActive(false);}
    void Awake(){Hide();group.blocksRaycasts=group.interactable=false;impact.blocksRaycasts=impact.interactable=false;}
    void OnDisable(){Hide();}
    void Update(){
        if(!Presenting)return;
        if(GameManager.single&&GameManager.single.gameState!=GameStateType.BATTLE){Hide();return;}
        if(Target&&(Target.isDying||!Target.gameObject.activeInHierarchy)){Hide();return;}
        var transition=TransitionCanvas.single;
        if(transition&&transition.group&&transition.group.alpha>0){group.alpha=0;return;}
        var entranceCallback=onEntranceStarted;onEntranceStarted=null;entranceCallback?.Invoke();
        elapsed+=Time.unscaledDeltaTime;
        group.alpha=Mathf.Clamp01(elapsed/Mathf.Max(.01f,entranceDuration));
        float charge=Mathf.Clamp01((elapsed-.12f)/Mathf.Max(.01f,chargeDuration-.12f));
        if(elapsed<chargeDuration){fill.fillAmount=1-Mathf.Pow(1-charge,2);health.text=Mathf.RoundToInt(fill.fillAmount*100)+"%";}
        else if(Target&&Target.currentStat!=null){fill.fillAmount=Mathf.Clamp01(Target.currentStat.hpNow/Mathf.Max(1,Target.currentStat.hpMax));health.text=Utilities.ConvertNumber(Target.currentStat.hpNow)+" / "+Utilities.ConvertNumber(Target.currentStat.hpMax);eyebrow.text="首领";}
        else {fill.fillAmount=1;health.text="100%";}
        impact.alpha=elapsed>=chargeDuration?Mathf.Clamp01(1-(elapsed-chargeDuration)/.25f)*.65f:0;
        chargeTip.gameObject.SetActive(elapsed<chargeDuration);
        chargeTip.anchorMin=chargeTip.anchorMax=new Vector2(fill.fillAmount,.5f);chargeTip.anchoredPosition=Vector2.zero;
        float punch=elapsed>=chargeDuration?Mathf.Sin(Mathf.Clamp01((elapsed-chargeDuration)/.24f)*Mathf.PI)*.07f:0;
        portraitRoot.localScale=Vector3.one*(1+punch);
        Place();
    }
    public void Place(){
        if(!panel||!header)return;
        var root=(RectTransform)transform;var safe=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        var available=Vector2.Scale(root.rect.size,safe.size);float scale=Mathf.Min(available.x/1080f,available.y/1500f);
        header.GetWorldCorners(corners);float headerBottom=root.InverseTransformPoint(corners[0]).y;
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);panel.pivot=new Vector2(.5f,1);
        panel.sizeDelta=new Vector2(available.x/scale-sideInset*2,170);panel.localScale=Vector3.one*scale;
        panel.localPosition=new Vector3(root.rect.xMin+root.rect.width*safe.center.x,headerBottom-(gap+(1-Mathf.Clamp01(elapsed/Mathf.Max(.01f,entranceDuration)))*-24)*scale,0);
    }
    public float BottomIn(Transform relative){panel.GetWorldCorners(corners);return relative.InverseTransformPoint(corners[0]).y;}
}
