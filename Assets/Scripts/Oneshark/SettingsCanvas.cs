using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsCanvas : MonoBehaviour {

    public GameObject deleteBox, mainStuffBox;

    public void CloseSettings()
    {
        //GameManager.single.ToCampFromSettings();
        //GameplayCanvas.single.ToggleSettings();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void OnEnable()
    {
        SetSliders();
        OnSFXChanged();
        OnMusicChanged();
        mainStuffBox.SetActive(true);
        deleteBox.SetActive(false);
    }

    public void OpenDeleteBox()
    {
        deleteBox.SetActive(true);
        mainStuffBox.SetActive(false);
    }

    public void CloseDeleteBox()
    {
        deleteBox.SetActive(false);
        mainStuffBox.SetActive(true);
    }

    //public Text languageText;
    //public string[] languages;
    //public int index;
    //public void LanguageArrow(int i)
    //{
    //    Debug.Log("Language arrow:" + i);
    //    //get the 
    //    index += i;
    //    if (index >= languages.Length) index = 0;
    //    if (index == -1) index = languages.Length-1;
    //    //if (i>)
    //    languageText.text = languages[index];
    //}

    void SetSliders()
    {
        //m_SfxText.text = (int)(AudioManager.single.soundLevel * 100) + "%";
        //m_SfxSlider.value = AudioManager.single.soundLevel;

        //m_MusicText.text = (int)(AudioManager.single.musicLevel * 100) + "%";
        //m_MusicSlider.value = AudioManager.single.musicLevel;
    }

    public Text m_SfxText;
    public Slider m_SfxSlider;
    public void OnSFXChanged()
    {
        float val = m_SfxSlider.value / 1;
        m_SfxText.text = (int)(val * 100) + "%";
        //AudioManager.single.SoundLevelChanged(val);
    }

    public Text m_MusicText;
    public Slider m_MusicSlider;
    public void OnMusicChanged()
    {
        float val = m_MusicSlider.value / 1;
        m_MusicText.text = (int)(val * 100) + "%";
        //AudioManager.single.MusicLevelChanged(val);
    }

    public void DeleteProgress()
    {
        Debug.Log("Deleted Progress!");
        //SaveManager.Instance.DeleteData();
        //TransitionCanvas.single.ToBlack();
        Invoke("LateLoadLevel", 1);
    }

    void LateLoadLevel()
    {
        Application.LoadLevel("Gameplay");
    }
}
