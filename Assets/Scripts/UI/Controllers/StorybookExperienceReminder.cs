using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Game.Enum;

public sealed class StorybookExperienceReminder : MonoBehaviour
{
    public RectTransform card;
    public CanvasGroup group;
    public Button open,close;
    public float lifetime=8,cooldownHours=4;
    public const string Preference="Storybook.ExperienceReminder.LastShownUtc";
    public bool ShownThisSession {get;private set;}
    float age;
    void Awake(){open.onClick.AddListener(()=>{Hide();Router.single.XPBoosterButton();});close.onClick.AddListener(()=>{StorybookNavigation.PlayClick();Hide();});}
    public bool Eligible(){var gm=GameManager.single;var gc=GameplayCanvas.single;if(!gm||!gc||gm.gameState!=GameStateType.CAMP||gm.HasActiveXpBoost||!gm.HasSeenThis("Tutorial-4"))return false;
        var home=gc.homeUI;if(!home||!home.gameObject.activeInHierarchy||home.IsNavigating||home.modal.activeSelf||(home.settingsUI&&home.settingsUI.IsOpen))return false;
        if((gc.experienceUI&&gc.experienceUI.gameObject.activeSelf)||(gc.levelUpUI&&gc.levelUpUI.Presenting)||(gc.itemToast&&gc.itemToast.Presenting)||(TutorialManager.single&&TutorialManager.single.view&&TutorialManager.single.view.Visible)||gc.dailyCherriesPop.activeSelf)return false;
        return !(TransitionCanvas.single&&TransitionCanvas.single.isBlack)&&!FindAnyObjectByType<CursedBookIntro>();
    }
    public bool TryShow(){
        if(ShownThisSession||!Eligible())return false;
        if(long.TryParse(PlayerPrefs.GetString(Preference,""),out long ticks)&&ticks>0&&ticks<=DateTime.MaxValue.Ticks&&(DateTime.UtcNow-new DateTime(ticks,DateTimeKind.Utc)).TotalHours<cooldownHours)return false;
        ShownThisSession=true;age=0;gameObject.SetActive(true);group.alpha=0;group.blocksRaycasts=group.interactable=true;
#if UNITY_EDITOR
        if(!UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false))
#endif
        {PlayerPrefs.SetString(Preference,DateTime.UtcNow.Ticks.ToString());PlayerPrefs.Save();}
        Place();return true;
    }
    void Update(){if(!Eligible()){Hide();return;}age+=Time.unscaledDeltaTime;group.alpha=Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01((lifetime-age)/.25f));Place();if(age>=lifetime)Hide();}
    void Place(){var r=((RectTransform)transform).rect;var safe=BattleHudSafeArea.NormalizedSafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));float s=Mathf.Min(r.width*safe.width/1080,r.height*safe.height/1500)*.9f;card.sizeDelta=new Vector2(800,190);card.localScale=Vector3.one*s;card.anchoredPosition=new Vector2(r.xMin+r.width*safe.center.x,r.yMin+r.height*safe.yMax-(450+95)*s);}
    public void Hide(){group.alpha=0;group.blocksRaycasts=group.interactable=false;gameObject.SetActive(false);}
    void OnDisable(){if(group){group.alpha=0;group.blocksRaycasts=group.interactable=false;}}
}
