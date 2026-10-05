/*
 using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public AudioClipRef[] clips;
    public AudioSource[] m_AudioSource = new AudioSource[25];
    public float soundLevel, origSoundLevel;
    public float musicLevel, origMusicLevel;
    public bool usesLogging = false;
    private static AudioManager _instance;
    public static AudioManager single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<AudioManager>();
            return _instance;
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < m_AudioSource.Length; i++)
        {
            m_AudioSource[i] = gameObject.AddComponent<AudioSource>();
            m_AudioSource[i].playOnAwake = false;
            m_AudioSource[i].loop = false;
        }
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].clipName = clips[i].clipName.ToUpper();
        }
    }

    void Start()
    {
        AudioManager[] foundObjects = FindObjectsOfType<AudioManager>();
        if (foundObjects.Length > 1)
        {
            Destroy(foundObjects[1].gameObject);
        }

        //m_AudioSource = GetComponents<AudioSource>();
        Invoke("SetLevels", .5f);
    }

    public void SetLevels()
    {
        //Debug.Log("SetLevels");
        float sfxVolume = getSFXVolume();
        if (EventManager.single) EventManager.single.CallAudioLevelChanged();
        foreach (AudioSource a in m_AudioSource)
        {
            a.volume = sfxVolume;
        }
    }

    public float getSFXVolume()
    {
        return soundLevel;
    }

    public float getBGVolume()
    {
        return musicLevel;
    }

    public void PlayClip(string _clipName, bool shouldLoop = false, bool pitchShift = true, float volumeMult = 1)
    {
        //Debug.Log("Audio:" + _clipName);
        ClipRouterPlay(_clipName, shouldLoop, false, 1);
    }

    float m_NextPitch = 1;
    public bool m_HardsetNext = false;
    public void HardsetPitch(float pitch)
    {
        m_NextPitch = pitch;
        m_HardsetNext = true;
    }

    public void ClipRouterPlay(string _clipName, bool shouldLoop, bool pitchShift, float volumeMult)
    {
        _clipName = _clipName.ToUpper();
        if (m_AudioSource[0] == null) return;

        //if (_clipName.Contains("Skill"))
        //    Debug.Log("ClipRouterPlay:" + _clipName + " " + shouldLoop + " " + pitchShift + " " + volumeMult);
        AudioSource nextFreeAudioSource = null;

        bool audioSourceFound = false;
        for (int i = 0; i < m_AudioSource.Length; i++)
        {

            if (!m_AudioSource[i].isPlaying)
            {
                nextFreeAudioSource = m_AudioSource[i];
                audioSourceFound = true;
                break;
            }
        }

        if (!audioSourceFound && m_AudioSource[0] != null)
        {
            nextFreeAudioSource = m_AudioSource[0];
            nextFreeAudioSource.Stop();
        }
        nextFreeAudioSource.loop = shouldLoop;

        AudioClipRef audioClipRef = clips.Where(x => x.clipName == _clipName).FirstOrDefault();
        if (audioClipRef.clip == null)
        {
            if (usesLogging) Debug.Log("AUDIO CLIP NOT FOUND:" + _clipName);
            // throw new System.Exception("AUDIO CLIP NOT FOUND:" + _clipName);
            return;
        }
        nextFreeAudioSource.pitch = GetPitch(audioClipRef.clipName);

        nextFreeAudioSource.clip = audioClipRef.clip;
        if (pitchShift)
        {
            nextFreeAudioSource.pitch = Random.Range(.85f, 1.2f);
            //  float vol = getSFXVolume();
            //if (vol > 0)
            //    nextFreeAudioSource.volume = (vol + Random.Range(-.2f, .2f)) * volumeMult;
        }
        else
        {
            nextFreeAudioSource.pitch = 1;
            nextFreeAudioSource.volume = getSFXVolume();
        }

        //hardset pitch feature
        if (m_HardsetNext)
        {
            nextFreeAudioSource.pitch = m_NextPitch;
            m_HardsetNext = false;
        }
        else
        {
            m_HardsetNext = false;
        }

        //Debug.Log("Play weird Audio CLip");
        //nextFreeAudioSource.Play();
        nextFreeAudioSource.pitch = 1;

        nextFreeAudioSource.PlayOneShot(audioClipRef.clip);
    }

    public void StopLoop()
    {
        AudioSource nextFreeAudioSource;

        for (int i = 0; i < m_AudioSource.Length; i++)
        {
            if (m_AudioSource[i].isPlaying)
            {
                m_AudioSource[i].loop = false;
            }
        }
    }

    public void StopClip(string _clipName)
    {
        string _clipNameOfficial = GetAudioClipOfficialName(_clipName);

        for (int i = 0; i < m_AudioSource.Length; i++)
        {
            if (m_AudioSource[i].isPlaying && m_AudioSource[i].clip.name == _clipName)
            {
                m_AudioSource[i].Stop();
                m_AudioSource[i].loop = false;
            }
        }
    }

    string GetAudioClipOfficialName(string _clipName)
    {
        AudioClipRef audioClipRef = clips.Where(x => x.clipName == _clipName).FirstOrDefault();
        if (audioClipRef.clip == null) return "";
        return audioClipRef.clip.ToString();

    }

    public float GetPitch(string ac)
    {
        bool shouldShift = false;

        if (ac == "fist") shouldShift = true;

        //if pitch should shift
        if (shouldShift) return Random.Range(.8f, 1);
        return 1;

    }

    public void MusicLevelChanged(float f)
    {
        musicLevel = f;
        // m_AudioSourceMusicOnly.volume = f;
        //print("Music Lev Changed to:" + f);
        SetLevels();
    }

    public void SoundLevelChanged(float f)
    {
        // f = f * .7f;//setting it 30% less because it's too loud usually
        soundLevel = f;
        SetLevels();
        //print("Sound Lev Changed to:" + f);
    }
}

[System.Serializable]
public struct AudioClipRef
{
    public string clipName;
    public AudioClip clip;
}
*/