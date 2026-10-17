using IdleKnightHero.UI;
using UnityEngine;

/// <summary>Triggered by Skill2's raised-scythe OnHit; survives the visual runners.</summary>
public sealed class GhostWolfPackAudio : MonoBehaviour
{
    AudioSource source;
    AudioClip charge;
    Player owner;
    SoundManager sound;
    public bool PlayingCharge { get; private set; }

    public static GhostWolfPackAudio Play(Player player)
    {
        var howl = Resources.Load<AudioClip>("Audio/Skills/GhostWolfHowl");
        var pack = Resources.Load<AudioClip>("Audio/Skills/GhostWolfPack");
        if (!player || !howl || !pack) return null;
        var node = new GameObject("Ghost wolf howl then pack audio");
        node.transform.SetParent(player.transform, false);
        var audio = node.AddComponent<GhostWolfPackAudio>();
        audio.owner = player;
        audio.sound = FindFirstObjectByType<SoundManager>();
        audio.charge = pack;
        audio.source = node.AddComponent<AudioSource>();
        audio.source.playOnAwake = false;
        audio.source.loop = false;
        audio.source.spatialBlend = 0;
        audio.source.clip = howl;
        audio.ApplySettings();
        audio.source.Play();
        return audio;
    }

    public static void StopAll(Player player)
    {
        if (!player) return;
        foreach (var audio in player.GetComponentsInChildren<GhostWolfPackAudio>(true)) {
            audio.source?.Stop();
            audio.enabled = false;
            Destroy(audio.gameObject);
        }
    }

    void ApplySettings()
    {
        float level = sound ? sound.getSFXVolume() : 1;
        source.volume = level * level; // Same gain as the battle SoundManager.
        source.pitch = Time.timeScale;
    }

    void Update()
    {
        if (!owner || owner.isCampPlayer || owner.ab.isDying || owner.IsVictoryRelaxing) {
            Destroy(gameObject);
            return;
        }
        ApplySettings();
        if (Time.timeScale <= 0 || AudioListener.pause || source.isPlaying) return;
        if (PlayingCharge) { Destroy(gameObject); return; }
        // Wait for actual playback completion, rather than an animation-length timer.
        PlayingCharge = true;
        source.clip = charge;
        source.Play();
    }

    void OnDisable() { if (source) source.Stop(); }
}
