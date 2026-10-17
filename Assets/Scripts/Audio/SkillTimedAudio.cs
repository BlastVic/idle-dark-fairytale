using UnityEngine;
using IdleKnightHero.UI;

// A separate source prevents other one-shots from changing a skill's pitch.
public sealed class SkillTimedAudio : MonoBehaviour
{
    AudioSource source;
    float rate, elapsed, lifetime;
    public static void Play(Transform owner, AudioClip clip, float rate = 1)
    {
        if (!clip) return;
        var node = new GameObject("Skill audio " + clip.name);
        node.transform.SetParent(owner, false);
        var audio = node.AddComponent<SkillTimedAudio>();
        audio.rate = Mathf.Clamp(rate, .01f, 3);
        audio.lifetime = clip.length / audio.rate;
        audio.source = node.AddComponent<AudioSource>();
        audio.source.playOnAwake = false;
        audio.source.spatialBlend = 0;
        audio.source.clip = clip;
        audio.Apply();
        audio.source.Play();
    }
    void Apply()
    {
        var sound = SoundManager.Instance;
        float level = sound ? sound.getSFXVolume() : 1;
        source.volume = level * level; // Match SoundManager's source and one-shot gain.
        source.pitch = rate * Time.timeScale;
    }
    void Update()
    {
        Apply();
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) Destroy(gameObject);
    }
}
