using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bindings for the saved, scrollable settings page.</summary>
public sealed class StorybookSettingsUI : MonoBehaviour
{
    public Button back,skip,developer;
    public Slider music,sound;
    public Text musicValue,soundValue,skipValue,version;
    public Image skipMark,skipOff;
    public ScrollRect scroll;
    public CanvasGroup group;
    int openedFrame;
    public bool IsOpen=>gameObject.activeSelf;
    void Awake(){if(developer)developer.onClick.AddListener(OpenDeveloper);back.onClick.AddListener(Close);skip.onClick.AddListener(()=>{StorybookPreferences.SkipLoadingAnimation=!StorybookPreferences.SkipLoadingAnimation;RefreshSkip();});music.onValueChanged.AddListener(SetMusic);sound.onValueChanged.AddListener(SetSound);}
    public void Open(){openedFrame=Time.frameCount;gameObject.SetActive(true);group.blocksRaycasts=group.interactable=true;music.SetValueWithoutNotify(SoundManager.Instance.musicLevel);sound.SetValueWithoutNotify(SoundManager.Instance.soundLevel);Labels();RefreshSkip();version.text="版本 "+Application.version;scroll.verticalNormalizedPosition=1;}
    void OpenDeveloper(){var debug=DebugMenu.Instance;if(!debug)return;StorybookNavigation.PlayClick();Close();debug.Open();}
    public void Close(){group.blocksRaycasts=group.interactable=false;gameObject.SetActive(false);}
    void Update(){if(Time.frameCount>openedFrame&&Input.GetKeyDown(KeyCode.Escape))Close();}
    void RefreshSkip(){bool enabled=StorybookPreferences.SkipLoadingAnimation;skipMark.enabled=enabled;if(skipOff)skipOff.enabled=!enabled;skip.targetGraphic=enabled?skipMark:skipOff;skipValue.text=enabled?"已开启":"已关闭";skipValue.color=enabled?new Color(.46f,.07f,.045f):new Color(.32f,.28f,.23f);}
    void SetMusic(float value){SoundManager.Instance.musicLevel=value;Store("Shelter.Music",value);SoundManager.Instance.SetLevels();Labels();}
    void SetSound(float value){SoundManager.Instance.soundLevel=value;Store("Shelter.Sound",value);SoundManager.Instance.SetLevels();Labels();}
    static void Store(string key,float value){
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false))return;
#endif
        PlayerPrefs.SetFloat(key,value);PlayerPrefs.Save();}
    void Labels(){musicValue.text=Mathf.RoundToInt(music.value*100)+"%";soundValue.text=Mathf.RoundToInt(sound.value*100)+"%";}
}
