using System.Collections;
using Assets.Scripts.Extentions;
using UnityEngine;

namespace IdleKnightHero.UI
{
    public class MusicManager : MonoSingleton<MusicManager>
    {
        void OnEnable()
        {
            EventManager.OnAudioLevelChanged += OnAudioLevelChanged;
        }

        void OnDisable()
        {
            EventManager.OnAudioLevelChanged -= OnAudioLevelChanged;
        }

        bool callNewMusicCool = true;
        void NewMusicCooldown()
        {
            callNewMusicCool = true;
        }

        void OnAudioLevelChanged()
        {
            StopAllCoroutines();
            backgroundMusicSource.volume = SoundManager.Instance.getBGVolume();
        }

        public AudioClip[] battleNormalClips;
        public AudioClip[] audioClips;

        public void PlayMusicByClipNumber(int clipNum, bool loop = true)
        {
            StopAllCoroutines();
            AudioClip newClip = null;
            newClip = audioClips[clipNum];
            StartCoroutine(PlayNewClipChain(newClip, loop));
        }

        public void PlayBattleNormalMusic(bool loop = true)
        {
            StopAllCoroutines();
            AudioClip newClip = null;
            newClip = battleNormalClips[Random.Range(0, battleNormalClips.Length - 1)];
            StartCoroutine(PlayNewClipChain(newClip, loop));
        }

        public void PlayMusicByClip(AudioClip newClip)
        {
            StartCoroutine(PlayNewClipChain(newClip));
        }

        public int originalBattleClip = 1;
        public AudioSource backgroundMusicSource;
        private IEnumerator PlayNewClipChain(AudioClip newClip, bool loop = true)
        {
            // Debug.Log("Clip play" + newClip.name);
            //FADE OUT
            float startVolume = backgroundMusicSource.volume;
            fadeComplete = false;
            while (backgroundMusicSource.volume > 0)
            {
                backgroundMusicSource.volume -= startVolume * Time.deltaTime * 1.25f;
                yield return null;
            }

            fadeComplete = true;

            //CHANGE CLIP
            backgroundMusicSource.clip = newClip;

            //FADE IN
            float bgVolume = SoundManager.Instance.getBGVolume();
            backgroundMusicSource.loop = loop;//does it need to loop?
            backgroundMusicSource.Play();

            while (backgroundMusicSource.volume < bgVolume)
            {
                backgroundMusicSource.volume += Time.deltaTime * 1.25f;
                yield return null;
            }
        }

        bool fadeComplete = false;
        //float lastClipTime = 0;
        public IEnumerator FadeBackgroundMusic()
        {
            float startVolume = backgroundMusicSource.volume;
            fadeComplete = false;
            while (backgroundMusicSource.volume > 0)
            {
                backgroundMusicSource.volume -= startVolume * Time.deltaTime / 2;
                yield return null;
            }

            fadeComplete = true;
            // lastClipTime = backgroundMusicSource.time;
            // Debug.Log("Clip Time was:" + lastClipTime);
        }

        public void VolumeZero()
        {
            backgroundMusicSource.volume = 0;// ();
        }

        public void FadeToZero()
        {
            StartCoroutine(FadeBackgroundMusic());
        }
    }
}