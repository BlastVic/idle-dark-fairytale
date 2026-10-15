using IdleKnightHero.UI;
using UnityEngine;

/// <summary>Saved prefab sources follow the book's visual clock, including loading holds.</summary>
[DisallowMultipleComponent]
public sealed class CursedBookIntroAudio : MonoBehaviour
{
    public CursedBookIntro intro;
    public AudioSource foley, atmosphere;
    [Range(0,1)] public float foleyGain = .9f;
    [Range(0,1)] public float atmosphereGain = .14f;
    public float Drift { get; private set; }
    public bool Holding { get; private set; }
    AudioSource music;
    bool wasMuted, ownsMute, started;
    float lastTime = -1;

    void OnEnable() { lastTime = -1; started = false; Holding = false; }
    void LateUpdate()
    {
        if (!intro || intro.Finished || !foley || !atmosphere || !foley.clip || !atmosphere.clip) return;
        if (!ownsMute && MusicManager.Instance && MusicManager.Instance.backgroundMusicSource) {
            music = MusicManager.Instance.backgroundMusicSource;
            wasMuted = music.mute; music.mute = true; ownsMute = true;
        }
        if (!intro.StartupSelected) return;
        if (intro.SilentStartup) {
            foley.Stop(); atmosphere.Stop(); Holding = true; return;
        }
        float level = SoundManager.Instance ? SoundManager.Instance.getSFXVolume() : 1;
        if (intro.prologue && intro.prologue.IsPlaying) {
            // Page segments carry their own forest/fire ambience and single-page foley.
            foley.Pause(); atmosphere.Pause(); Holding = true;
            return;
        }
        if (started) atmosphere.UnPause();
        float t = intro.Elapsed;
        float end = 4.6f + intro.revealDuration + intro.revealedHoldDuration + intro.enterPageDuration + intro.controlsRevealDuration;
        foley.volume = level * foleyGain;
        atmosphere.volume = level * atmosphereGain * Mathf.Clamp01(t/.5f) * Mathf.Clamp01((end-t)/.9f);
        if (!started) {
            foley.time = 0; foley.Play(); atmosphere.Play(); started = true;
        }
        Holding = lastTime >= 0 && Mathf.Abs(t-lastTime) < .00001f;
        // Keep the air bed alive while the illustrated page waits for loading.
        if (Holding) foley.Pause();
        else {
            float audioTime = AudioTime(t);
            if (audioTime < foley.clip.length-.01f) {
                foley.UnPause();
                Drift = Mathf.Abs(foley.time-audioTime);
                if (Drift > .075f) foley.time = audioTime;
            }
        }
        lastTime = t;
    }
    float AudioTime(float t)
    {
        if(t<=4.6f)return t;
        t-=4.6f;
        if(t<intro.revealDuration)return 4.6f+1.8f*t/Mathf.Max(.01f,intro.revealDuration);
        t-=intro.revealDuration;
        if(t<intro.revealedHoldDuration)return 6.4f+.3f*t/Mathf.Max(.01f,intro.revealedHoldDuration);
        t-=intro.revealedHoldDuration;
        if(t<intro.enterPageDuration)return 6.7f+2.2f*t/Mathf.Max(.01f,intro.enterPageDuration);
        t-=intro.enterPageDuration;
        return Mathf.Min(9.79f,8.9f+.9f*t/Mathf.Max(.01f,intro.controlsRevealDuration));
    }
    void OnDisable()
    {
        if(foley)foley.Stop();
        if(atmosphere)atmosphere.Stop();
        if(ownsMute && music)music.mute=wasMuted;
        ownsMute=false; started=false;
    }
}
