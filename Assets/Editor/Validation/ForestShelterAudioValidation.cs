using System.Collections;
using System.IO;
using IdleKnightHero.UI;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Game.Enum;

// Explicit request only; never changes saved progression or rebuilds UI.
[InitializeOnLoad]
public static class ForestShelterAudioValidation
{
    const string Dir = "output/forest-shelter/";
    const string Flag = "FOREST_AUDIO_QA";
    static bool running;
    static ForestShelterAudioValidation() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlaying) {
            if (!running && SessionState.GetBool(Flag, false) && GameManager.single && GameplayCanvas.single && GameplayCanvas.single.homeUI) {
                running = true; GameManager.single.StartCoroutine(Run());
            }
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (running) { running=false; SessionState.SetBool(Flag,false); SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false); SessionState.SetBool("PROLOGUE_QA_FORCE",false); }
        if (!File.Exists(Dir+"integrate-validate.request")) return;
        File.Delete(Dir+"integrate-validate.request");
        File.WriteAllText(Dir+"runtime-validation.txt", "Forest shelter audio validation\n");
        SessionState.SetBool(Flag,true); SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true); SessionState.SetBool("PROLOGUE_QA_FORCE",true);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        EditorApplication.isPlaying=true;
    }
    static void Check(bool ok,string message) { File.AppendAllText(Dir+"runtime-validation.txt",(ok?"PASS ":"FAIL ")+message+"\n"); }
    static IEnumerator Run()
    {
        var gm=GameManager.single; var wm=WaveManager.single;
        for(int i=0;i<20;i++) if(!gm.HasSeenThis("Tutorial-"+i)) gm.thingsSeenPermanent.Add("Tutorial-"+i);
        wm.highestWavesReached.RemoveAll(x=>x.levelKey=="Mount Zion");
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        float deadline=Time.realtimeSinceStartup+45;
        while(intro&&!intro.prologue.IsPlaying&&Time.realtimeSinceStartup<deadline) yield return null;
        var music=MusicManager.Instance; var source=music.backgroundMusicSource;
        Check(intro&&intro.prologue.IsPlaying&&source.mute,"shelter music muted during prologue");
        if(intro&&intro.prologue.IsPlaying) intro.prologue.narration.time=intro.prologue.fullNarration.length-.5f;
        deadline=Time.realtimeSinceStartup+30;
        while(intro&&!intro.Finished&&Time.realtimeSinceStartup<deadline) yield return null;
        yield return new WaitForSecondsRealtime(2);
        Check(intro&&intro.Finished,"prologue transitions to home");
        Check(source.clip==music.forestShelterAmbience&&source.isPlaying&&source.loop&&!source.mute,"forest clip playing and looping after prologue");
        Check(Mathf.Abs(source.clip.length-90)<.01f,"90 second PCM clip imported");
        source.time=source.clip.length-.6f;
        yield return new WaitForSecondsRealtime(1.5f);
        Check(source.isPlaying&&source.time<3,"actual playback wraps at loop boundary");
        var home=GameplayCanvas.single.homeUI;
        home.battleButton.onClick.Invoke();
        deadline=Time.realtimeSinceStartup+15;
        while(gm.gameState!=GameStateType.BATTLE&&Time.realtimeSinceStartup<deadline)yield return null;
        yield return new WaitForSecondsRealtime(2);
        Check(gm.gameState==GameStateType.BATTLE&&source.clip!=music.forestShelterAmbience,"battle replaces forest ambience");
        GameplayCanvas.single.battleHud.backButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(4);
        Check(gm.gameState==GameStateType.CAMP&&source.clip==music.forestShelterAmbience&&source.isPlaying,"return to forest restores ambience");
        var level=wm.GetWavesetByKey("Mount Zion");
        wm.SetWaveseReachedByKey("Mount Zion",level.waves.Length);
        music.PlayMusicByClipNumber(0);
        yield return new WaitForSecondsRealtime(2);
        Check(source.clip==music.audioClips[0]&&source.clip.name=="HunterCabinLoop"&&source.loop,"unlocked cabin retains original looping ambience");
        File.AppendAllText(Dir+"runtime-validation.txt","FINISHED (save disabled)\n");
        EditorApplication.isPlaying=false;
    }
}
