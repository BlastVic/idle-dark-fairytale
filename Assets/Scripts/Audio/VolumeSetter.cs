using UnityEngine;
using System.Collections;
using IdleKnightHero.UI;

public class VolumeSetter : MonoBehaviour
{

    AudioSource[] audioSrc;
    public bool isSFX;
    void Start()
    {
        audioSrc = GetComponents<AudioSource>();
        //Invoke("setVolumes", .1f);
        setVolumes();
    }


    void OnEnable()
    {
        EventManager.OnAudioLevelChanged += OnAudioLevelChanged;
        setVolumes();
    }

    void OnDisable()
    {
        EventManager.OnAudioLevelChanged -= OnAudioLevelChanged;
    }

    void setVolumes()
    {
        if (audioSrc == null) audioSrc = GetComponents<AudioSource>();

        if (isSFX)
        {

            foreach (AudioSource _audio in audioSrc)
            {
                _audio.volume = SoundManager.Instance.getSFXVolume();
            }
        }
        else
        {
            foreach (AudioSource _audio in audioSrc)
            {
                _audio.volume = SoundManager.Instance.getBGVolume();
            }

        }

    }



    public void OnAudioLevelChanged()
    {
        // Debug.Log("Audio Level Chagned, fix all volumes");
        setVolumes();
    }
}
