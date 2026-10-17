using System;
using System.Collections;
using System.IO;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleKnightHero.UI;

[InitializeOnLoad]
public static class GhostWolfAudioValidation
{
    const string Dir = "output/ghost-wolf-audio/";
    static bool running;
    static double deadline;
    static GhostWolfAudioValidation() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isPlaying && File.Exists(Dir + "run.request")) {
            File.Delete(Dir + "run.request");
            SessionState.SetBool("GhostWolfAudioQA", true);
            EditorSceneManager.OpenScene(SkillTestBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && !running && SessionState.GetBool("GhostWolfAudioQA", false)) {
            var test = UnityEngine.Object.FindFirstObjectByType<SkillTest>();
            if (!test || !test.Ready) return;
            SessionState.SetBool("GhostWolfAudioQA", false);
            running = true; deadline = EditorApplication.timeSinceStartup + 60;
            File.WriteAllText(Dir + "validation.txt", "");
            test.StartCoroutine(Guard(Run(test)));
        }
        if (running && EditorApplication.timeSinceStartup > deadline) Finish("FAIL timeout");
    }
    static void Check(bool ok, string message) {
        if (!ok) throw new Exception(message);
        File.AppendAllText(Dir + "validation.txt", "PASS " + message + "\n");
    }
    static void Finish(string result) {
        Time.timeScale = 1;
        File.AppendAllText(Dir + "validation.txt", result + "\n");
        running = false; EditorApplication.isPlaying = false;
    }
    static IEnumerator Guard(IEnumerator work) {
        while (true) {
            object next;
            try { if (!work.MoveNext()) break; next = work.Current; }
            catch (Exception e) { Finish("FAIL " + e); yield break; }
            yield return next;
        }
    }
    static IEnumerator Run(SkillTest test) {
        test.ResetTest(); test.Select(SkillsTypeEnum.GhostWolfPack); test.SetLevel(1); yield return null; test.Cast();
        Check(Player.single.isAttacking, "Skill2 cast started");
        yield return new WaitForSeconds(.3f);
        Check(!UnityEngine.Object.FindFirstObjectByType<GhostWolfPackAudio>(), "no audio before raised-scythe event");
        GhostWolfPackAudio audio;
        do { yield return null; audio = UnityEngine.Object.FindFirstObjectByType<GhostWolfPackAudio>(); } while (!audio);
        var source = audio.GetComponent<AudioSource>();
        Check(!audio.PlayingCharge && source.isPlaying && source.clip.name == "GhostWolfHowl", "howl starts at Skill2 OnHit");
        Check(UnityEngine.Object.FindObjectsByType<GhostWolfPackAudio>(FindObjectsSortMode.None).Length == 1, "one audio sequence per cast");
        yield return new WaitForSeconds(.5f);
        Time.timeScale = 0; yield return null;
        Check(source.pitch == 0 && !audio.PlayingCharge, "pause retains howl phase");
        yield return new WaitForSecondsRealtime(.3f); Time.timeScale = 1;
        var sound = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
        var volume = sound.soundLevel; sound.soundLevel = 0; yield return null;
        Check(source.volume == 0, "mute follows SFX volume");
        sound.soundLevel = volume; yield return null;
        Check(Mathf.Approximately(source.volume, volume * volume), "SFX volume restored");
        while (!audio.PlayingCharge) yield return null;
        Check(source.isPlaying && source.clip.name == "GhostWolfPack", "provided clip begins after howl finishes");
        Check(source.clip.length > 9.9f && source.clip.length < 10.2f, "complete ten-second user clip retained");
        Check(test.HitCount == 5, "five original crossing impacts preserved");
        Check(!UnityEngine.Object.FindFirstObjectByType<GhostWolfPack>(), "audio survives runners leaving screen");
        while (audio) yield return null;
        Check(!UnityEngine.Object.FindFirstObjectByType<GhostWolfPackAudio>(), "sequence cleans up after complete playback");
        audio = GhostWolfPackAudio.Play(Player.single); yield return null; test.ResetTest(); yield return null;
        Check(!audio, "test reset cancels playback");
        audio = GhostWolfPackAudio.Play(Player.single); Player.single.ab.isDying = true; yield return null; yield return null;
        Check(!audio, "death cancels playback");
        Finish("PASS ghost wolf audio sequence");
    }
}
