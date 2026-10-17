using Assets.Scripts.Game.Enum;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Chapter01EndingValidation
{
    const string Dir="output/chapter01-ending/"; static bool running;
    static Chapter01EndingValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("CH1_QA",false))Reset();};}
    static void Reset(){running=false;SessionState.SetBool("CH1_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.SetInt("SETTINGS_QA_SKIP",-1);}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){if(!running&&SessionState.GetBool("CH1_QA",false)&&GameManager.single){running=true;GameManager.single.StartCoroutine(Run());}return;}
        if(!File.Exists(Dir+"validate.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool("CH1_QA_FULL",File.ReadAllText(Dir+"validate.request").Trim()=="full");
        File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Chapter 1 ending integration\n");
        SessionState.SetBool("CH1_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("PROLOGUE_QA_FORCE",false);SessionState.SetInt("SETTINGS_QA_SKIP",1);
        Size(720,1280);EditorApplication.isPlaying=true;
    }
    static void Size(int w,int h){CreatureNameMigrationValidation.Resolution(w,h);}
    static void Check(bool b,string msg){File.AppendAllText(Dir+"validation.txt",(b?"PASS ":"FAIL ")+msg+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run(){
        yield return null;var gm=GameManager.single;
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        float deadline=Time.realtimeSinceStartup+45;
        while(UnityEngine.Object.FindObjectsByType<CursedBookIntro>(FindObjectsSortMode.None).Any(x=>!x.Finished)&&Time.realtimeSinceStartup<deadline)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var router=Router.single;var gc=GameplayCanvas.single;var waves=WaveManager.single;
        gm.thingsSeenPermanent.Remove(CursedBookIntro.ChapterEndingSeenKey);
        waves.currentWaveset=waves.GetWavesetByKey("Mount Zion");waves.SetWaveseReachedByKey("Mount Zion",waves.currentWaveset.waves.Length);
        gm.SetGameState(GameStateType.CAMP);Check(!router.ShouldPlayChapterEnding(),"camp does not trigger ending");
        gm.SetGameState(GameStateType.DEFEAT);Check(!router.ShouldPlayChapterEnding(),"defeat does not trigger ending");
        gm.SetGameState(GameStateType.VICTORY);Check(router.ShouldPlayChapterEnding(),"chapter 1 section 5 victory eligible");
        var final=waves.currentWaveset;waves.currentWaveset=waves.waveDb.First(x=>x.levelKey!="Mount Zion");Check(!router.ShouldPlayChapterEnding(),"other levels do not trigger ending");waves.currentWaveset=final;
        gc.victoryPanel.SetActive(true);gc.GoToCampFromVictoryOrDefeat();
        deadline=Time.realtimeSinceStartup+30;
        while(!router.ActiveChapterEnding&&Time.realtimeSinceStartup<deadline)yield return null;
        var intro=router.ActiveChapterEnding;Check(intro,"real victory return handler spawned saved ending prefab");
        if(!intro){EditorApplication.isPlaying=false;yield break;}
        var p=intro.prologue;var layout=gc.homeUI.GetComponent<ShelterResponsiveLayout>();
        int errors=0;Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception){errors++;File.AppendAllText(Dir+"validation.txt",m+"\n");}};Application.logMessageReceived+=log;
        bool paperFade=false;bool handoff=false;bool blank=false;bool[] pages=new bool[5],turns=new bool[4];deadline=Time.realtimeSinceStartup+150;
        while(intro&&!intro.Finished&&Time.realtimeSinceStartup<deadline){
            yield return new WaitForEndOfFrame();
            if(intro.HomeReady&&!blank&&!p.IsPlaying){blank=true;Shot("blank-open-page");Check(intro.book.AnimationState.GetCurrent(0).Animation.Name=="OpenFirstPage"&&intro.book.AnimationState.GetCurrent(0).TrackTime>=intro.book.AnimationState.GetCurrent(0).Animation.Duration,"starts directly on opened blank page");}
            if(intro.ControlsProgress>.98f&&!handoff){handoff=true;Shot("before-live-handoff");Check(intro.homePage.material.GetFloat("_PaperBlend")<.001f,"paper effect fully gone before home controls");}
            if(intro.PushProgress>.45f&&intro.PushProgress<.9f&&!paperFade){paperFade=true;Shot("home-paper-fading");float blend=intro.homePage.material.GetFloat("_PaperBlend");Check(!p.narration.isPlaying&&blend>0&&blend<.55f,"paper fades gradually during camera push after narration");}
            if(p.PageIndex>=0&&p.PageIndex<4&&p.TurningPage&&!turns[p.PageIndex]){turns[p.PageIndex]=true;float ambienceTime=p.continuousAmbience.time;yield return new WaitForSecondsRealtime(.35f);Check(p.continuousAmbience.isPlaying && Mathf.Abs(p.continuousAmbience.time-ambienceTime)>.1f,"ambience advances through turn "+p.PageIndex);yield return new WaitForEndOfFrame();Shot("turn-"+p.PageIndex);Check(p.pageSurface.IsFollowing,"illustration follows turning leaf "+p.PageIndex);}
            if(p.PageIndex>=0&&p.Reading&&p.PageReadTime>.7f&&!pages[p.PageIndex]){
                int n=p.PageIndex;pages[n]=true;
                if(n==0){p.SetPaused(true);float pausedAt=p.continuousAmbience.time;yield return new WaitForSecondsRealtime(.2f);Check(Mathf.Abs(p.continuousAmbience.time-pausedAt)<.05f,"ambience pauses with confirmation");p.SetPaused(false);yield return new WaitForSecondsRealtime(.2f);Check(p.continuousAmbience.isPlaying,"ambience resumes after confirmation");}Shot("page-"+(n+1));
                Check(p.narration.clip==intro.chapterEnding.pages[n].narrationAndAmbience,"approved voice/action clip on page "+(n+1));
                Check(layout.presentationControls.alpha==0,"home controls hidden during narration "+(n+1));
                if(n==4){Check(Mathf.Abs(intro.homePage.material.GetFloat("_PaperBlend")-.55f)<.001f,"live home uses full paper effect throughout narration");Check(intro.homePage.texture==intro.HomeTexture&&gc.homeUI.GetComponent<ShelterChapterBackdrop>().IsCabin,"final illustration is live cabin home");}
                if(n==0||n==4){
                    int[] w={720,768,720},h={1280,1024,1440};string[] labels={"9x16","3x4","1x2"};
                    for(int j=0;j<3;j++){Size(w[j],h[j]);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();Shot("page"+(n+1)+"-"+labels[j]);var c=new Vector3[4];p.subtitle.rectTransform.GetWorldCorners(c);Check(c.All(v=>v.x>=-1&&v.x<=Screen.width+1&&v.y>=-1&&v.y<=Screen.height+1),"subtitle bounds "+labels[j]);}
                    Size(720,1280);
                }
                if(!SessionState.GetBool("CH1_QA_FULL",false))p.narration.time=p.narration.clip.length-.15f;
            }
        }
        Check(pages.All(x=>x),"all five pages narrated");Check(p.TurnsCompleted==4,"four single-page turns before home");
        Check(gm.HasSeenThis(CursedBookIntro.ChapterEndingSeenKey),"ending marked complete after final narration");
        yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();Shot("home-controls-restored");
        Check(!router.ActiveChapterEnding&&layout.presentationControls.alpha>.99f,"home controls and route restored");
        Check(!IdleKnightHero.UI.MusicManager.Instance.backgroundMusicSource.mute,"home music restored");
        int[] widths={720,768,720},heights={1280,1024,1440};string[] names={"9x16","3x4","1x2"};
        for(int j=0;j<3;j++){Size(widths[j],heights[j]);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("home-"+names[j]);}
        Size(720,1280);
        gc.homeUI.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(2f);Check(UnityEngine.Object.FindAnyObjectByType<StorybookMapUI>(),"map navigation works after ending");
        gm.SetGameState(GameStateType.VICTORY);Check(!router.ShouldPlayChapterEnding(),"completed ending does not replay on repeated wins");gm.SetGameState(GameStateType.CAMP);
        Check(errors==0,"no runtime errors");Application.logMessageReceived-=log;File.AppendAllText(Dir+"validation.txt","FINISHED\n");EditorApplication.isPlaying=false;
    }
}
