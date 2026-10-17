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
            if (stoppingMusic) return;
            StopAllCoroutines();
            backgroundMusicSource.volume = SoundManager.Instance.getBGVolume();
        }

        public AudioClip[] battleNormalClips;
        public AudioClip[] audioClips;
        [Tooltip("Looping ambience for the forest shelter before the cabin is unlocked.")]
        public AudioClip forestShelterAmbience;

        public void PlayMusicByClipNumber(int clipNum, bool loop = true, bool immediate = false)
        {
            StopAllCoroutines();
            stoppingMusic = false;
            AudioClip newClip = null;
            newClip = audioClips[clipNum];
            if (clipNum == 0 && forestShelterAmbience)
            {
                var home = GameplayCanvas.single ? GameplayCanvas.single.homeUI : null;
                var backdrop = home ? home.GetComponent<ShelterChapterBackdrop>() : null;
                if (backdrop) backdrop.Refresh();
                if (backdrop && !backdrop.IsCabin) newClip = forestShelterAmbience;
            }
            if (immediate)
            {
                backgroundMusicSource.Stop();
                backgroundMusicSource.clip = newClip;
                backgroundMusicSource.loop = loop;
                backgroundMusicSource.volume = SoundManager.Instance.getBGVolume();
                backgroundMusicSource.Play();
                return;
            }
            StartCoroutine(PlayNewClipChain(newClip, loop));
        }

        public void PlayBattleNormalMusic(bool loop = true)
        {
            StopAllCoroutines();
            stoppingMusic = false;
            AudioClip newClip = null;
            newClip = battleNormalClips[Random.Range(0, battleNormalClips.Length - 1)];
            StartCoroutine(PlayNewClipChain(newClip, loop));
        }

        public void PlayMusicByClip(AudioClip newClip)
        {
            StopAllCoroutines();
            stoppingMusic = false;
            StartCoroutine(PlayNewClipChain(newClip));
        }

        bool stoppingMusic;
        public void StopBackgroundMusic(float fadeDuration = 0)
        {
            StopAllCoroutines();
            stoppingMusic = true;
            if (!backgroundMusicSource) return;
            StartCoroutine(StopMusicChain(Mathf.Max(0, fadeDuration)));
        }

        IEnumerator StopMusicChain(float duration)
        {
            float startVolume = backgroundMusicSource.volume;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }
            backgroundMusicSource.volume = 0;
            backgroundMusicSource.Stop();
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

            // Stop first so retrying the same clip always starts from the beginning.
            backgroundMusicSource.Stop();
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
