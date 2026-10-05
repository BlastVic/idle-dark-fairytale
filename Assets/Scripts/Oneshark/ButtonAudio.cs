using System.Collections;
using System.Collections.Generic;
using IdleKnightHero.UI;
using UnityEngine;

public class ButtonAudio : MonoBehaviour
{
    public bool m_Mute = false;
    public bool m_MuteClickOnly = false;
    public string m_HoverClipName = "HOVER";
    public string m_ClickClipName = "CLICK";

    //use only for this game
    public int m_IndexNumber = 1;//circles have an 

    private void Awake()
    {
        if (GetComponents<ButtonAudio>().Length > 1)
        {
            //Debug.Log("Duplicate Button Audio:" + gameObject);
        }
    }

    public void Hover()
    {
        if (m_Mute) return;
        // SoundManager.Instance.PlayClip(m_HoverClipName, false);
    }
    public void Clicked()
    {
        if (m_Mute) return;
        if (m_MuteClickOnly) return;
        SoundManager.Instance.PlayClip(m_ClickClipName, false);
    }
}
