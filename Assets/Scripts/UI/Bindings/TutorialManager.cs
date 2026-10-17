using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Statistics;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    static TutorialManager instance;
    public static TutorialManager single => instance ? instance : (instance=FindFirstObjectByType<TutorialManager>());
    // Retained for old scene references. They are never displayed by the new guide.
    public List<GameObject> tutorials=new List<GameObject>();
    public GameObject tutorialCharacter,overlay;
    public StorybookTutorialUI view;
    public bool automatic=true;
    public int ActiveKey {get;private set;}=-1;
    int phase;
    float retry;
    readonly HashSet<int> requested=new HashSet<int>();
    void Awake(){instance=this;foreach(var t in tutorials)if(t)t.SetActive(false);if(tutorialCharacter)tutorialCharacter.SetActive(false);if(overlay)overlay.SetActive(false);if(view)view.Hide();}
    public void PlayTutorial(int key){if(key>=0&&key<9)requested.Add(key);}
    bool Seen(int key)=>GameManager.single.HasSeenThis("Tutorial-"+key);
    // Steps 0–7 are the onboarding chain. Step 8 is an explicitly requested shop hint.
    public bool BlocksDailyReminder
    {
        get
        {
            if (!GameManager.single || (view && view.Visible)) return true;
            if (ActiveKey >= 0 && !Seen(ActiveKey)) return true;
            for (int key = 0; key < 8; key++) if (!Seen(key)) return true;
            return requested.Any(key => !Seen(key));
        }
    }

    bool Blocked(){
        if(FindFirstObjectByType<CursedBookIntro>())return true;
        var transition=TransitionCanvas.single;
        var group=transition?transition.group:null;
        return group&&group.blocksRaycasts;
    }
    void Update(){
        if(!view||!GameManager.single||!GameplayCanvas.single)return;
        if(Blocked()){if(view.Visible)LoseTarget();return;}
        if(view.Visible||Time.unscaledTime<retry)return;
        retry=Time.unscaledTime+.25f;
        for(int key=0;key<9;key++){
            if(Seen(key)||(key>0&&!Seen(key-1))||(!automatic&&!requested.Contains(key))||(key==8&&!requested.Contains(key)))continue;
            if(ActiveKey!=key)phase=0;
            var target=Resolve(key,out string title,out string text,out string hint);
            if(!target||!target.gameObject.activeInHierarchy||!target.IsInteractable())continue;
            GameplayCanvas.single.GetComponent<StorybookDailyReward>()?.DeferForTutorial();
            ActiveKey=key;view.Show(key,target,Advance,ClosePopUp,LoseTarget,title,text,hint);break;
        }
    }
    Button Resolve(int key,out string title,out string text,out string hint){
        title=text=hint=null;
        var gm=GameManager.single;var home=GameplayCanvas.single.homeUI;
        bool camp=gm.gameState==GameStateType.CAMP&&home&&home.gameObject.activeInHierarchy&&!home.IsNavigating&&!home.modal.activeSelf;
        var equipment=FindFirstObjectByType<StorybookEquipmentUI>();
        switch(key){
            case 0:return camp?home.navigationButtons[3]:null;
            case 1:
                var map=FindFirstObjectByType<StorybookMapUI>();if(!map||map.IsNavigating||gm.gameState!=GameStateType.MAP)return null;
                if(phase>0){title="踏入黑森林";text="节点已选中，点击进入关卡。\n再次点击选中的节点也能出发。";hint="点击「进入关卡」";return map.enterButton;}
                for(int i=0;i<map.nodes.Length;i++)if(!map.nodes[i].IsStory&&map.IsUnlocked(i)){map.selectedIndex=i;map.Refresh();map.LocateSelection();return map.nodes[i].button;}return null;
            case 2:
                if(!camp||gm.statPoints<=0)return null;
                if(!home.attributesExpanded){home.ToggleAttributes();return null;}return home.attributeBody.blocksRaycasts?home.statButtons[0]:null;
            case 3:return camp&&InventoryManager.single.GetTotalItemCount()>0?home.navigationButtons[0]:null;
            case 4:
            case 5:
                if(!equipment||equipment.IsNavigating||equipment.HasOverlay||equipment.BatchMode)return null;
                var action=key==4?equipment.equip:equipment.decompose;
                if(action.IsInteractable())return action;
                var spare=equipment.Cells.FirstOrDefault(c=>c.Item!=null&&c.Item!=equipment.Equipped(equipment.Category)&&!c.Item.isLocked&&c.gameObject.activeInHierarchy);
                if(spare){title=key==4?"挑选新的装备":"选择多余装备";text=key==4?"先选中一件未穿戴的装备，\n查看属性与换装变化。":"选中一件未锁定的多余装备。\n已穿戴的装备不会被分解。";hint="点击下方装备";return spare.button;}return null;
            case 6:
                if(!camp)return null;var rail=home.GetComponentInChildren<ShelterChestRail>(true);if(rail&&rail.Collapsed){rail.Expand();return null;}if(rail&&rail.IsAnimating)return null;return home.chestButtons.FirstOrDefault(b=>b&&b.IsInteractable());
            case 7:
                if(gm.fxpMax<=0||gm.fxpNow<gm.fxpMax)return null;
                var fusion=FindFirstObjectByType<StorybookFusionUI>();if(fusion&&fusion.Ready&&!fusion.Busy&&!fusion.Leaving){title="领取融合成果";text="融合进度已满。\n完成融合，领取本次成长奖励。";hint="点击融合按钮";return fusion.action;}return camp?home.functionButtons[0]:null;
            case 8:return camp?home.navigationButtons[4]:null;
        }
        return null;
    }
    void Advance(){
        int key=ActiveKey;var target=view.Target;
        var map=FindFirstObjectByType<StorybookMapUI>();
        bool next=(key==1&&phase==0&&map&&!map.IsNavigating&&GameManager.single.gameState==GameStateType.MAP)||(key==7&&target.GetComponentInParent<ShelterHomeUI>())||((key==4||key==5)&&target.GetComponentInParent<StorybookEquipmentCell>());
        if(next){phase++;view.Hide();retry=Time.unscaledTime+.75f;return;}
        Complete();
    }
    void Complete(){if(ActiveKey<0)return;int key=ActiveKey;if(!Seen(key)){GameManager.single.thingsSeenPermanent.Add("Tutorial-"+key);StatisticsTracker.Instance.LogEvent("Tutorial-"+key);Assets.Scripts.Services.SaveManager.Instance.Save();}requested.Remove(key);ActiveKey=-1;phase=0;view.Hide();retry=Time.unscaledTime+.75f;}
    public void ClosePopUp(){if(view&&view.Visible)Complete();}
    void LoseTarget(){view.Hide();retry=Time.unscaledTime+.75f;}
}
