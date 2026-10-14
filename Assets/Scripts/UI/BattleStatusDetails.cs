using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data binding for authored non-interactive skill indicators and portrait notifications.</summary>
public sealed class BattleStatusDetails : MonoBehaviour
{
    public SkillsTypeEnum[] keys;
    public GameObject[] skillSlots;
    public Image[] cooldownMasks;
    public RectTransform notificationAnchor, notification;
    public Text notificationText;
    public CanvasGroup notificationGroup;
    public GameObject levelDecoration;
    public Text rewardText, experienceText;
    public RectTransform waxSeal;
    public float floatDuration=1.65f, levelDuration=2.5f, floatDistance=120;
    bool levelMessage;
    float elapsed;
    bool showing;
    float pendingExperience;
    int firstLevel,lastLevel;
    public void Experience(float amount){if(amount>0)pendingExperience+=amount;}
    public void Level(int level){if(firstLevel==0)firstLevel=level;lastLevel=level;}
    void OnDisable(){pendingExperience=0;firstLevel=lastLevel=0;showing=false;if(notificationGroup)notificationGroup.alpha=0;}
    void Update()
    {
        var skills=SkillsController.Instance;var player=Player.single;
        for(int i=0;i<skillSlots.Length;i++){
            bool learned=skills.GetSkillLevel(keys[i])>0;skillSlots[i].SetActive(learned);
            float remaining=0;
            if(player&&!player.isCampPlayer&&!player.ab.isDying){
                if(keys[i]==SkillsTypeEnum.Regen)remaining=player.RegenRemaining;
                if(keys[i]==SkillsTypeEnum.Lightning_Demi_God)remaining=player.LightningRemaining;
            }
            // Counter-clockwise filled remainder leaves a clockwise cleared sweep from twelve.
            cooldownMasks[i].fillAmount=remaining;
        }
        if(!showing){
            if(firstLevel==0 && pendingExperience<=0)return;
            levelMessage=firstLevel>0;
            levelDecoration.SetActive(levelMessage);
            rewardText.gameObject.SetActive(levelMessage);
            notificationText.gameObject.SetActive(levelMessage);
            experienceText.gameObject.SetActive(!levelMessage);
            if(levelMessage){
                int count=lastLevel-firstLevel+1;
                notificationText.text=(count>1?"连续升级":"等级提升")+"  <size=48>Lv. "+lastLevel+"</size>";
                rewardText.text="属性点 +"+count+(lastLevel/5>(firstLevel-1)/5?"   ·   技能点 +"+(lastLevel/5-(firstLevel-1)/5):"");
                firstLevel=lastLevel=0;
            }else notificationText.text="<size=30>经验</size>  +"+Utilities.ConvertNumber(pendingExperience);
            if(!levelMessage){experienceText.text=notificationText.text;pendingExperience=0;}
            notificationText.color=levelMessage?new Color(.38f,.045f,.025f):new Color(1,.9f,.65f);
            var outline=notificationText.GetComponent<Outline>();outline.enabled=!levelMessage;
            elapsed=0;showing=true;
        }
        elapsed+=Time.unscaledDeltaTime;
        float duration=levelMessage?levelDuration:floatDuration;
        float t=Mathf.Clamp01(elapsed/duration);
        // Fast impact, restrained settle, reading pause, then an accelerating upward exit.
        float entry=Mathf.Clamp01(elapsed/.28f);
        float impact=1+2.5f*Mathf.Pow(entry-1,3)+1.5f*Mathf.Pow(entry-1,2);
        notification.localScale=Vector3.one*Mathf.LerpUnclamped(.65f,1,impact);
        float rise=20*t+22*Mathf.SmoothStep(0,1,entry)+floatDistance*Mathf.Pow(Mathf.InverseLerp(.55f,1,t),2);
        notification.anchoredPosition=notificationAnchor.anchoredPosition+new Vector2(0,rise);
        notificationGroup.alpha=Mathf.Clamp01(elapsed/.08f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,t)));
        if(levelMessage && waxSeal){
            float stamp=Mathf.Clamp01((elapsed-.10f)/.20f);
            waxSeal.localScale=Vector3.one*Mathf.Lerp(1.6f,1,1-Mathf.Pow(1-stamp,3));
            waxSeal.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-22,-8,stamp));
        }
        if(t>=1){showing=false;notificationGroup.alpha=0;}
    }
}
