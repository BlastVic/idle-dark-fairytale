using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scripts.Skills;

[InitializeOnLoad]
public static class ScytheAudioValidation
{
    const string Flag="SCYTHE_AUDIO_QA", Out="output/scythe-skill-check/runtime.txt";
    static double deadline;
    static SkillTest test;
    static int phase, sample;
    static readonly float[] Speeds={100,200,75};
    static bool slash,spin;
    static float castTime,spinTime;
    static ScytheAudioValidation(){EditorApplication.update+=Tick;}
    public static void Begin()
    {
        Directory.CreateDirectory("output/scythe-skill-check");File.WriteAllText(Out,"");
        SessionState.SetBool(Flag,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        EditorSceneManager.OpenScene("Assets/Tests/Skills/SkillTest.unity");
        EditorApplication.isPlaying=true;
    }
    static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);File.AppendAllText(Out,"PASS "+msg+"\n");}
    static void Finish(string result,int code)
    {
        File.AppendAllText(Out,result+"\n");SessionState.SetBool(Flag,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
        EditorApplication.isPlaying=false;EditorApplication.Exit(code);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+120;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout");
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(!test)test=UnityEngine.Object.FindFirstObjectByType<SkillTest>();
            if(!test||!test.Ready)return;
            if(phase==0){
                test.ResetTest();test.Select(SkillsTypeEnum.Sinister_Stampede);
                Player.single.ab.currentStat.atkSpd=Speeds[sample];
                slash=spin=false;phase=2;return;
            }
            if(phase==2){
                if(!test.castButton.interactable||!Player.single.manualControl)return;
                castTime=Time.time;phase=1;test.Cast();
                Check(Player.single.currentAnimation=="Skill5","Skill5 cast at attack speed "+Speeds[sample]);
            }
            foreach(var a in UnityEngine.Object.FindObjectsByType<SkillTimedAudio>(FindObjectsSortMode.None)){
                var s=a.GetComponent<AudioSource>();
                if(s.clip.name=="ScytheSlash"&&!slash){
                    slash=true;float track=Player.single.skeletonAnimation.AnimationState.GetCurrent(0).TrackTime;
                    Check(track>=.46f&&track<.70f,"slash follows animation time: "+track);
                    Check(Mathf.Abs(s.pitch-Speeds[sample]/100)<.02f,"slash pitch follows attack speed");
                }
                if(s.clip.name=="ScytheSpin"&&!spin){
                    spin=true;spinTime=Time.time;
                    Check(s.transform.parent.GetComponent<CommonSpineBattleEffect>()!=null,"spin belongs to actual effect instance");
                    Check(Mathf.Abs(s.clip.length-1.2667f)<.002f&&Mathf.Abs(s.pitch-1)<.02f,"spin lasts 1.2667s at fixed speed");
                    Check(s.playOnAwake==false&&s.spatialBlend==0,"explicit 2D one-shot");
                }
            }
            if(phase==1&&spin&&Time.time-spinTime>1.5f){
                Check(slash&&spin,"both authored clips triggered");
                Check(!UnityEngine.Object.FindObjectsByType<SkillTimedAudio>(FindObjectsSortMode.None).Any(),"audio sources cleaned up");
                sample++;phase=0;
                if(sample==Speeds.Length)Finish("PASS all 3 attack speeds; no runtime exceptions observed by validator",0);
            }
            if(Time.time-castTime>8&&phase==1)throw new Exception("missing cue");
        }catch(Exception e){Finish("FAIL "+e,1);}
    }
}
