using UnityEngine;
using System.Collections;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    private static EventManager _instance;
    public static EventManager single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<EventManager>();
            return _instance;
        }
    }


    //private void OnEnable()
    //{
    //    //EventManager.OnLoadComplete += HandleLoadComplete;
    //}

    //private void OnDisable()
    //{
    //    //EventManager.OnLoadComplete -= HandleLoadComplete;
    //}

    private void Awake()
    {
        //ensure 1 copy as we go from scene to scene, and it is not destroyed
        EventManager[] foundObjects = FindObjectsOfType<EventManager>();
        if (foundObjects.Length > 1)
        {
            Destroy(foundObjects[1].gameObject);
        }
    }

    public delegate void AudioLevelChanged();
    public static event AudioLevelChanged OnAudioLevelChanged;
    public void CallAudioLevelChanged()
    {
        if (OnAudioLevelChanged != null)
            OnAudioLevelChanged();
    }

    public delegate void LoadComplete();
    public static event LoadComplete OnLoadComplete;
    public void CallLoadComplete()
    {
        if (OnLoadComplete != null)
            OnLoadComplete();
    }

    public delegate void BattleStart();
    public static event BattleStart OnBattleStart;
    public void CallBattleStart()
    {
        if (OnBattleStart != null)
            OnBattleStart();
    }


    public delegate void BattleEnd();
    public static event BattleEnd OnBattleEnd;
    public void CallBattleEnd()
    {
        if (OnBattleEnd != null)
            OnBattleEnd();
    }


    public delegate void BossDefeated();
    public static event BossDefeated OnBossDefeated;
    public void CallBossDefeated()
    {
        if (OnBossDefeated != null)
            OnBossDefeated();
    }

    public delegate void ExpUpdate();
    public static event ExpUpdate OnExpUpdate;
    public void CallExpUpdate()
    {
        if (OnExpUpdate != null)
            OnExpUpdate();
    }

    public delegate void Freeze(bool b);
    public static event Freeze OnToggleFreeze;
    public void CallToggleFreeze(bool b)
    {
        OnToggleFreeze(b);
    }


    public delegate void PhotonConnectionChange(bool b);
    public static event PhotonConnectionChange OnPhotonConnectionChange;
    public void CallPhotonConnectionChange(bool b)
    {
        if (OnPhotonConnectionChange != null)
            OnPhotonConnectionChange(b);
    }

    public delegate void ToggleBlind(bool b);
    public static event ToggleBlind OnToggleBlind;
    public void CallToggleBlind(bool b)
    {
        OnToggleBlind(b);
    }



    public delegate void ToggleTrails(bool b);
    public static event ToggleTrails OnToggleTrails;
    public void CallToggleTrails(bool b)
    {
        if (OnToggleTrails != null)
            OnToggleTrails(b);
    }

    public delegate void ToggleScratches(bool b);
    public static event ToggleScratches OnToggleScratches;
    public void CallToggleScratches(bool b)
    {
        OnToggleScratches(b);
    }

    public delegate void RefreshInventory();
    public static event RefreshInventory OnRefreshInventory;
    public void CallRefreshInventory()
    {
        if (OnRefreshInventory != null)
            OnRefreshInventory();
    }

    public delegate void RefreshSuicide();
    public static event RefreshSuicide OnRefreshSuicide;
    public void CallRefreshSuicide()
    {
        if (OnRefreshSuicide != null)
            OnRefreshSuicide();
    }

    public delegate void RefreshIntentions();
    public static event RefreshIntentions OnRefreshIntentions;
    public void CallRefreshIntentions()
    {
        if (OnRefreshIntentions != null)
            OnRefreshIntentions();
    }


    public delegate void TutorialClosed();
    public static event TutorialClosed OnTutorialClosed;
    public void CallTutorialClosed()
    {
        if (OnTutorialClosed != null)
            OnTutorialClosed();
    }

    public delegate void Tutorial(string key);
    public static event Tutorial OnTutorial;
    public void PlayTutorial(string key)
    {
        if (OnTutorial != null)
            OnTutorial(key);
    }

    public delegate void UIRefresh(string key);
    public static event UIRefresh OnUIRefresh;
    public void CallUIRefresh(string key)
    {
        if (OnUIRefresh != null)
            OnUIRefresh(key);
    }

    public delegate void SkillPressed(int i);
    public static event SkillPressed OnSkillPressed;
    public void CallSkillPressed(int i)
    {
        if (OnSkillPressed != null)
            OnSkillPressed(i);
    }


    public delegate void LevelUpEffect(int i);
    public static event LevelUpEffect OnLevelUpEffect;
    public void CallLevelUpEffect(int i)
    {
        if (OnLevelUpEffect != null)
            OnLevelUpEffect(i);
    }

    public delegate void RefreshSkills();
    public static event RefreshSkills OnRefreshSkills;
    public void CallRefreshSkills()
    {
        if (OnRefreshSkills != null)
            OnRefreshSkills();
    }

    public delegate void RefreshRetirement();
    public static event RefreshRetirement OnRefreshRetirement;
    public void CallRefreshRetirement()
    {
        OnRefreshRetirement();
    }


    public delegate void DequipAll();
    public static event DequipAll OnDequipAll;
    public void CallDequipAll()
    {
        if (OnDequipAll != null)
            OnDequipAll();
    }

    public delegate void LanguageChange();
    public static event LanguageChange OnLanguageChange;
    public void CallLanguageChange()
    {
        if (OnLanguageChange != null)
            OnLanguageChange();
    }

    public delegate void PhotonMatchReady();
    public static event PhotonMatchReady OnPhotonMatchReady;
    public void CallPhotonMatchReady()
    {
        if (OnPhotonMatchReady != null)
            OnPhotonMatchReady();
    }


    public delegate void SetOutlines();
    public static event SetOutlines OnSetOutlines;
    public void CallSetOutlines()
    {
        OnSetOutlines();
    }

    public delegate void CharSwap();
    public static event CharSwap OnCharSwap;
    public void CallCharSwap()
    {
        if (OnCharSwap != null)
            OnCharSwap();
    }


    public delegate void MassRevive();
    public static event MassRevive OnMassRevive;
    public void CallMassRevive()
    {
        if (OnMassRevive != null)
            OnMassRevive();
    }


    public delegate void LustForLife();
    public static event LustForLife OnLustForLife;
    public void CallLustForLife()
    {
        if (OnLustForLife != null)
            OnLustForLife();
    }

    public delegate void MapRefresh();
    public static event MapRefresh OnMapRefresh;
    public void CallMapRefresh()
    {
        if (OnMapRefresh != null)
            OnMapRefresh();
    }

    //CallUnhighlightItemButtons

    public delegate void UnhighlightItemButtons();
    public static event UnhighlightItemButtons OnUnhighlightItemButtons;
    public void CallUnhighlightItemButtons()
    {
        if (OnUnhighlightItemButtons != null)
            OnUnhighlightItemButtons();
    }

    public delegate void HighlightEnemies();
    public static event HighlightEnemies OnHighlightEnemies;
    public void CallHighlightEnemies()
    {
        if (OnHighlightEnemies != null)
            OnHighlightEnemies();
    }

    public delegate void HighlightPlayer();
    public static event HighlightPlayer OnHighlightPlayer;
    public void CallHighlightPlayer()
    {
        if (OnHighlightPlayer != null)
            OnHighlightPlayer();
    }

    //public UnityEvent m_EndEnemyHover;

    public delegate void EndEnemyHovers();
    public static event EndEnemyHovers OnEndEnemyHovers;
    public void CallEndEnemyHovers()
    {
        if (OnEndEnemyHovers != null)
            OnEndEnemyHovers();
    }

    public delegate void ManaChanged();
    public static event ManaChanged OnManaChanged;
    public void CallManaChanged()
    {
        if (OnManaChanged != null)
            OnManaChanged();
    }


    public delegate void BuffsChanged();
    public static event BuffsChanged OnBuffsChanged;
    public void CallBuffsChanged()
    {
        if (OnBuffsChanged != null)
            OnBuffsChanged();
    }

    public delegate void AlterDebuffs(float multiple);
    public static event AlterDebuffs OnAlterDebuffs;
    public void CallAlterDebuffs(float multiple)
    {
        if (OnAlterDebuffs != null)
            OnAlterDebuffs(multiple);
    }

    public delegate void DeHighlightEnemies();
    public static event DeHighlightEnemies OnDehighlightCharacters;
    public void CallDehighlightCharacters()
    {
        if (OnDehighlightCharacters != null)
            OnDehighlightCharacters();
    }

    //OnRefreshNewFlags

    public delegate void RefreshNewFlags();
    public static event RefreshNewFlags OnRefreshNewFlags;
    public void CallRefreshNewFlags()
    {
        if (OnRefreshNewFlags != null)
            OnRefreshNewFlags();
    }

    public delegate void HideLifebars();
    public static event HideLifebars OnHideLifebars;
    public void CallHideLifebars()
    {
        if (OnHideLifebars != null)
            OnHideLifebars();
    }

    public delegate void RefreshMerchantGold();
    public static event RefreshMerchantGold OnRefreshMerchantGold;
    public void CallRefreshMerchantGold()
    {
        if (OnRefreshMerchantGold != null)
            OnRefreshMerchantGold();
    }

    public delegate void RefreshSkillButtons();
    public static event RefreshSkillButtons OnRefreshSkillButtons;
    public void CallRefreshSkillButtons()
    {
        if (OnRefreshSkillButtons != null)
            OnRefreshSkillButtons();
    }

    public delegate void UnlockMaps();
    public static event UnlockMaps OnUnlockMaps;
    public void CallUnlockMaps()
    {
        if (OnUnlockMaps != null)
            OnUnlockMaps();
    }
    //public delegate void DeselectMovementTiles();
    //public static event DeselectTiles OnDeselectMovementTiles;
    //public void CallDeselectMovementTiles()
    //{
    //    if (OnDeselectMovementTiles != null)
    //        OnDeselectMovementTiles();
    //}
}
