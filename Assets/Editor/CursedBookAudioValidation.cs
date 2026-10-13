using System.IO;
using UnityEditor;
using UnityEngine;
using IdleKnightHero.UI;

[InitializeOnLoad]
public static class CursedBookAudioValidation
{
    static CursedBookIntroAudio audio;
    static bool playing,held,done;
    static float maxDrift;
    static CursedBookAudioValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(!EditorApplication.isPlaying){audio=null;playing=held=done=false;maxDrift=0;return;}
        if(!SessionState.GetBool("BOOK_ENTRY_QA",false)||done)return;
        if(!audio)audio=Object.FindFirstObjectByType<CursedBookIntroAudio>(FindObjectsInactive.Include);
        if(!audio)return;
        if(audio.intro.Elapsed>1 && audio.intro.Elapsed<9.5f){
            playing|=audio.foley.isPlaying && audio.atmosphere.isPlaying;
            if(audio.Holding)held=true;
            if(audio.foley.isPlaying)maxDrift=Mathf.Max(maxDrift,Mathf.Abs(audio.foley.time-audio.intro.Elapsed));
        }
        if(!audio.intro.Finished)return;
        bool stopped=!audio.foley.isPlaying&&!audio.atmosphere.isPlaying;
        bool restored=MusicManager.Instance&&!MusicManager.Instance.backgroundMusicSource.mute;
        File.AppendAllText("output/storybook-audio/runtime-validation.txt",$"{(playing&&stopped&&restored&&maxDrift<.2f?"PASS":"FAIL")} ratio={SessionState.GetInt("BOOK_ENTRY_RATIO",0)} playing={playing} held={held} stopped={stopped} musicRestored={restored} maxObservedDrift={maxDrift:F3}s\n");
        done=true;
    }
}
