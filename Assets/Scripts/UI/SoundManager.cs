using UnityEngine;
using System.Collections;
using System.Linq;
using Assets.Scripts.Extentions;

namespace IdleKnightHero.UI
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        public AudioClipRef[] clips;
        public AudioSource audioSource;
        public AudioSource audioSource_Music;

        public float soundLevel, origSoundLevel;
        public float musicLevel, origMusicLevel;
        public bool usesLogging = false;

        private void OnEnable()
        {
            //convert all strings to upper and avoid string mismatching
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].clipName = clips[i].clipName.ToUpper();
            }
        }

        void Start()
        {
            SoundManager[] foundObjects = FindObjectsOfType<SoundManager>();
            if (foundObjects.Length > 1)
            {
                Destroy(foundObjects[1].gameObject);
            }

            //m_AudioSource = GetComponents<AudioSource>();
            Invoke("SetLevels", .5f);
            // StartCoroutine(AudioDemo());
        }

        IEnumerator AudioDemo()
        {
            yield return new WaitForSeconds(1);
            for (int i = 0; i < 100; i++)
            {
                Debug.Log(i);
                int rand = Random.Range(1, 4);
                PlayClip("attack" + rand, false);
                yield return new WaitForSeconds(.1f);
            }
        }

        public void SetLevels()
        {
            //Debug.Log("SetLevels");
            float sfxVolume = getSFXVolume();

            if (EventManager.single) EventManager.single.CallAudioLevelChanged();

            audioSource.volume = sfxVolume;

        }

        public float getSFXVolume()
        {
            return soundLevel;
        }

        public float getBGVolume()
        {
            return musicLevel;
        }

        public void PlayClip(string _clipName, bool shouldLoop = false, bool pitchShift = false)
        {
            _clipName = _clipName.ToUpper();
            AudioClipRef audioClipRef = clips.Where(x => x.clipName == _clipName).FirstOrDefault();
            if (pitchShift)
            {
                audioSource.pitch = Random.Range(.8f, 1);
            }
            else
            {
                audioSource.pitch = 1;
            }
            audioSource.PlayOneShot(audioClipRef.clip, soundLevel);
            //Debug.Log("Played Clip:" + audioClipRef.clipName);
        }

        /*
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
        */

        public void MusicLevelChanged(float f)
        {
            musicLevel = f;
            SetLevels();
        }

        public void SoundLevelChanged(float f)
        {
            soundLevel = f;
            SetLevels();
        }
    }

    [System.Serializable]
    public struct AudioClipRef
    {
        public string clipName;
        public AudioClip clip;
    }
}