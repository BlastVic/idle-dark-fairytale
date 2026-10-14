using System;
using System.Globalization;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services;
using UnityEngine;

/// <summary>Daily claims persist with the same save as the currency. Reminder dismissal is session-only.</summary>
public sealed class StorybookDailyReward : MonoBehaviour
{
    public StorybookDailyReminder reminder;
    public float delaySeconds=2;
    public bool ReminderHandled {get;private set;}
    float readySince=-1;
    public static string ClaimKey(DateTime date)=>"DailyBloodMoon.Claimed."+date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    public static bool ClaimedToday=>GameManager.single&&GameManager.single.thingsSeenPermanent.Contains(ClaimKey(DateTime.Today));
    public static bool TryClaim()
    {
        var gm=GameManager.single;if(!gm||ClaimedToday)return false;
        // Legacy _DAILY_CHERRIES means shown, never claimed: deliberately do not migrate it.
        gm.thingsSeenPermanent.Add(ClaimKey(DateTime.Today));
        gm.premiumCurrency+=gm.dailyCherriesAmount;
        SaveManager.Instance.Save();
        GameplayCanvas.single.RefreshCampUI();
        return true;
    }
    public bool HomeVisible()
    {
        var gc=GameplayCanvas.single;var gm=GameManager.single;
        return gm&&gc&&gm.gameState==GameStateType.CAMP&&gc.homeUI&&gc.homeUI.gameObject.activeInHierarchy&&!gc.homeUI.IsNavigating&&!FindAnyObjectByType<CursedBookIntro>()&&!(TransitionCanvas.single&&TransitionCanvas.single.isBlack);
    }
    public bool CanPresent()
    {
        if(!HomeVisible())return false;
        var gc=GameplayCanvas.single;var home=gc.homeUI;
        return !home.modal.activeSelf&&!(home.settingsUI&&home.settingsUI.IsOpen)&&!(gc.experienceUI&&gc.experienceUI.gameObject.activeSelf)&&!(gc.levelUpUI&&gc.levelUpUI.Presenting)&&!(gc.itemToast&&gc.itemToast.Presenting)&&!(TutorialManager.single&&TutorialManager.single.BlocksDailyReminder);
    }
    public void DeferForTutorial()
    {
        readySince=-1;
        if (!reminder || !reminder.gameObject.activeSelf) return;
        reminder.Hide();
        ReminderHandled=false;
    }
    void Update()
    {
        if(TutorialManager.single && TutorialManager.single.BlocksDailyReminder)
        {
            DeferForTutorial();
            return;
        }
        if(ReminderHandled)return;
        if(ClaimedToday){ReminderHandled=true;return;}
        if(!CanPresent()){readySince=-1;return;}
        if(readySince<0)readySince=Time.unscaledTime;
        if(Time.unscaledTime-readySince<delaySeconds)return;
        ReminderHandled=true;
        var gc=GameplayCanvas.single;if(gc.experienceReminder)gc.experienceReminder.Hide();
        reminder.Show();
    }
}
