using System;
using Scripts.Level;
using Assets.Scripts.Services.Purchasing;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared authored attributes. Only binds values, fold state and host-specific uniform sizing.</summary>
[ExecuteAlways]
public sealed class StorybookAttributePanel : MonoBehaviour
{
    public RectTransform contents,arrow;
    public CanvasGroup body;
    public Button fold;
    public Button[] add;
    public Text points,rebirth;
    public Text[] values;
    public Image[] unavailableBackgrounds;
    public Text[] unavailableCaptions;
    static readonly LevelTypeEnum[] StatTypes={LevelTypeEnum.HP,LevelTypeEnum.DMG,LevelTypeEnum.DEF,LevelTypeEnum.ATK_SPD,LevelTypeEnum.CRIT,LevelTypeEnum.CRIT_DMG};
    public bool fitHomeContents;
    public float homeScale=1052f/925;
    void OnEnable()=>Fit();
    void LateUpdate()=>Fit();
    public void Fit()
    {
        if(!fitHomeContents||!contents)return;
        var rect=(RectTransform)transform;
        contents.anchorMin=contents.anchorMax=contents.pivot=new Vector2(.5f,1);
        contents.anchoredPosition=Vector2.zero;
        contents.sizeDelta=new Vector2(rect.rect.width/homeScale,rect.rect.height/homeScale);
        contents.localScale=Vector3.one*homeScale;
    }
    public void RefreshValues(bool canInteract,Func<int,bool> canBuy)
    {
        var gm=GameManager.single;if(!gm||gm.currentPlayerStat==null)return;
        points.text="可用点数 <color=#761E20><size=39>"+gm.statPoints+"</size></color>";
        var s=gm.currentPlayerStat;float[] v={s.hpMax,s.dmg,s.def,s.atkSpd,s.crit,s.critDmg};
        for(int i=0;i<6;i++){values[i].text=Utilities.ConvertNumber(v[i])+(i>=3?"%":"");bool available=canBuy(i);add[i].interactable=canInteract&&available;
            if(unavailableBackgrounds!=null&&i<unavailableBackgrounds.Length&&unavailableBackgrounds[i])unavailableBackgrounds[i].gameObject.SetActive(!available);
            if(unavailableCaptions!=null&&i<unavailableCaptions.Length&&unavailableCaptions[i]){
                unavailableCaptions[i].gameObject.SetActive(!available);
                unavailableCaptions[i].text=PurchaserManager.Instance.LastStatPurchased(StatTypes[i])?"不可连续加点":"点数不足";
            }}
        rebirth.gameObject.SetActive(gm.rebirthLevel>0);rebirth.text="转生 "+gm.rebirthLevel;
    }
    public void SetFold(float amount,bool settled=true)
    {
        body.alpha=amount;body.gameObject.SetActive(amount>0);
        body.blocksRaycasts=body.interactable=settled&&amount>=.999f;
        arrow.localEulerAngles=new Vector3(0,0,180*amount);
        Fit();
    }
}
