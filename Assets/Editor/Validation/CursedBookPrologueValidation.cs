using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CursedBookPrologueValidation
{
    public const string Dir = "output/prologue-cn/";
    static bool running;
    static CursedBookPrologueValidation() {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("PROLOGUE_QA", false)) return;
            if (SessionState.GetBool("PROLOGUE_QA_LOCKED",false)) { EditorApplication.UnlockReloadAssemblies(); SessionState.SetBool("PROLOGUE_QA_LOCKED",false); }
            SessionState.SetInt("SETTINGS_QA_SKIP",SessionState.GetInt("PROLOGUE_QA_OLD_SKIP",-1));
            running = false;
            SessionState.SetInt("PROLOGUE_QA_INDEX", SessionState.GetInt("PROLOGUE_QA_INDEX",0)+1);
            EditorApplication.delayCall += Begin;
        };
    }
    static void Tick() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlaying) {
            if (!running && SessionState.GetBool("PROLOGUE_QA",false) && GameManager.single) {
                running = true; GameManager.single.StartCoroutine(Run());
            }
            return;
        }
        if(File.Exists(Dir+"returning.request") && !EditorApplication.isPlayingOrWillChangePlaymode) {
            File.Delete(Dir+"returning.request");
            SessionState.SetInt("PROLOGUE_QA_INDEX",3);SessionState.SetBool("PROLOGUE_QA",true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);Begin();return;
        }
        if (!File.Exists(Dir+"validate.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("PROLOGUE_QA_VISUAL", File.ReadAllText(Dir+"validate.request").Trim()=="visual");
        File.Delete(Dir+"validate.request"); File.WriteAllText(Dir+"validation.txt","Prologue actual Unity play-mode validation\n");
        SessionState.SetInt("PROLOGUE_QA_INDEX",0); SessionState.SetBool("PROLOGUE_QA",true);
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true); Begin();
    }
    static void Begin() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Begin; return; }
        int i=SessionState.GetInt("PROLOGUE_QA_INDEX",0);
        if (i>=4) { SessionState.SetBool("PROLOGUE_QA",false); SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false); File.AppendAllText(Dir+"validation.txt","ALL RUNS FINISHED\n"); return; }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path!="Assets/Scenes/Gameplay.unity") {
            if(scene.isDirty) { File.AppendAllText(Dir+"validation.txt","BLOCKED: current scene has unsaved changes\n");SessionState.SetBool("PROLOGUE_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);return; }
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        }
        var method=typeof(CursedBookIntroValidation).GetMethod("Resolution",BindingFlags.NonPublic|BindingFlags.Static);
        method.Invoke(null,new object[]{new[]{720,768,720,720}[i],new[]{1280,1024,1440,1280}[i]});
        SessionState.SetBool("PROLOGUE_QA_FORCE",i<3);
        SessionState.SetInt("PROLOGUE_QA_OLD_SKIP",SessionState.GetInt("SETTINGS_QA_SKIP",-1));
        SessionState.SetInt("SETTINGS_QA_SKIP",0);
        EditorApplication.LockReloadAssemblies(); SessionState.SetBool("PROLOGUE_QA_LOCKED",true);
        EditorApplication.isPlaying=true;
    }
    static void Log(string s) { File.AppendAllText(Dir+"validation.txt",s+"\n"); }
    static void Check(bool b,string s) { Log((b?"PASS ":"FAIL ")+s); }
    static void Shot(string name) {
        var t=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Dir+name+".png",t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
    }
    static IEnumerator Run() {
        int index=SessionState.GetInt("PROLOGUE_QA_INDEX",0);
        string ratio=new[]{"9x16","3x4","1x2","returning"}[index];
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        if(!intro || !intro.prologue) { Check(false,"missing prologue prefab"); EditorApplication.isPlaying=false;yield break; }
        // Wait until normal SaveManager.Start has loaded, then emulate new/returning saves in memory only.
        yield return null;
        var gm=GameManager.single;
        gm.thingsSeenPermanent.Remove(CursedBookPrologue.SeenKey);gm.thingsSeenPermanent.Remove("Tutorial-0");
        if(index==3)gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        typeof(CursedBookIntroValidation).GetMethod("Resolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new[]{720,768,720,720}[index],new[]{1280,1024,1440,1280}[index]});
        var p=intro.prologue; p.previewInEditor=false;
        Check(p.ShouldPlay()==(index<3),ratio+" new/returning selection");
        if(index>0 && index<3) {
            p.openingDuration=2.5f;p.openingCaptions=new[]{new CursedBookPrologue.Caption{start=0,end=2.5f,text=p.openingCaptions[0].text}};
            foreach(var page in p.pages){page.silentDuration=2.5f;page.captions=new[]{new CursedBookPrologue.Caption{start=0,end=2.5f,text=page.captions[0].text}};}
        }
        Vector3 lockedScale=Vector3.zero; Vector2 lockedPosition=Vector2.zero;
        bool[] read=new bool[6]; bool turnShot=false,homeShot=false,gate=true,stationary=true,speechAfterInk=true;
        float deadline=Time.realtimeSinceStartup+300;
        bool openingShot=false; bool silent=true; float nextFrame=0; int frame=0;
        if(index==0){Directory.CreateDirectory(Dir+"frames");File.WriteAllText(Dir+"frame-times.txt", "");}
        int errors=0; int turnFrame=0;
        Application.LogCallback callback=(m,s,t)=>{if(t==LogType.Exception || (t==LogType.Error&&!m.Contains("Ad is unavailable"))) {errors++;Log("ERROR "+m);} };
        Application.logMessageReceived+=callback;
        while(!intro.Finished && Time.realtimeSinceStartup<deadline) {
            yield return new WaitForEndOfFrame();
            if(index==0 && Time.realtimeSinceStartup>=nextFrame){
                var capture=ScreenCapture.CaptureScreenshotAsTexture();string name="frame-"+(frame++).ToString("0000")+".jpg";
                File.WriteAllBytes(Dir+"frames/"+name,capture.EncodeToJPG(85));UnityEngine.Object.Destroy(capture);
                File.AppendAllText(Dir+"frame-times.txt",name+" "+Time.realtimeSinceStartup.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");nextFrame=Time.realtimeSinceStartup+.25f;
            }
            if(p.IsPlaying) {
                if(lockedScale==Vector3.zero) {lockedScale=intro.cameraRig.localScale;lockedPosition=intro.cameraRig.anchoredPosition;}
                // Hide unrelated first-use tutorial after exercising the real new-player selection.
                if(!gm.HasSeenThis("Tutorial-0"))gm.thingsSeenPermanent.Add("Tutorial-0");
                gate &= intro.ControlsProgress==0 && intro.homePage.color.a==0 && intro.group.blocksRaycasts;
                stationary &= intro.PushProgress==0;
                stationary &= Vector3.Distance(intro.cameraRig.localScale,lockedScale)<.001f && Vector2.Distance(intro.cameraRig.anchoredPosition,lockedPosition)<.01f;
                silent &= !p.narration.isPlaying && !p.pageFoley.isPlaying;
                if(p.Opening && !openingShot && p.PageReadTime>1){SessionState.SetBool("PROLOGUE_QA_FORCE",false);p.ShouldPlay();openingShot=true;Shot(ratio+"-opening");Check(intro.book.AnimationState.GetCurrent(0).Animation.Name=="OpenFirstPage",ratio+" first-page opening without eight turns");}
                if(p.Reading && !p.Opening) {
                    speechAfterInk &= p.InkProgress>=.999f;
                    int page=p.PageIndex;
                    if(!read[page] && p.PageReadTime>1) {
                        Check(Screen.width==new[]{720,768,720,720}[index] && Screen.height==new[]{1280,1024,1440,1280}[index],ratio+" actual capture resolution");
                        read[page]=true; Shot(ratio+"-page-"+(page+1));
                        Check(p.illustration.texture==p.pages[page].illustration,ratio+" page "+(page+1)+" selected texture");
                        var rect=p.illustration.rectTransform.rect;
                        Check(Mathf.Abs(rect.width/rect.height-.75f)<.002f,ratio+" illustration fills page width");
                        var crop=p.SourceUVRect;
                        float sampledAspect=(float)p.pages[page].illustration.width*crop.width/(p.pages[page].illustration.height*crop.height);
                        Check(Mathf.Abs(sampledAspect-rect.width/rect.height)<.002f,ratio+" image not stretched");
                        Vector3[] corners=new Vector3[4];p.illustration.rectTransform.GetWorldCorners(corners);
                        Check(Mathf.Abs((corners[1].y-corners[0].y)-Mathf.Min(Screen.height,Screen.width*16f/9f)*intro.pageHoldViewportFraction)<2,ratio+" prologue retains opened-book reading distance");
                        Vector3[] homeCorners=new Vector3[4];intro.homePage.rectTransform.GetWorldCorners(homeCorners);
                        Check(Enumerable.Range(0,4).All(c=>Vector3.Distance(corners[c],homeCorners[c])<1),ratio+" illustration and home page have identical bounds");
                        p.subtitle.rectTransform.GetWorldCorners(corners);
                        Check(corners.All(c=>c.x>=-1&&c.x<=Screen.width+1&&c.y>=-1&&c.y<=Screen.height+1),ratio+" subtitle within viewport");
                        if(SessionState.GetBool("PROLOGUE_QA_VISUAL",false) && !p.silentPreview) p.narration.time=p.narration.clip.length-.05f;
                    }
                }
                if(index==0 && p.TurningPage && p.PageIndex==0) {
                    float time=intro.book.AnimationState.GetCurrent(0).TrackTime;
                    if(time>=turnFrame*.05f) {Shot("turn-frame-"+turnFrame.ToString("00"));turnFrame++;}
                }
                if(p.TurningPage && !turnShot && intro.book.AnimationState.GetCurrent(0).TrackTime>.15f) {
                    turnShot=true; Shot(ratio+"-single-turn");
                    Check(intro.book.AnimationState.GetCurrent(0).Animation.Name=="TurnPage",ratio+" one paper leaf turns with camera fixed");
                    Check(p.illustration.gameObject.activeSelf && p.pageSurface && p.pageSurface.IsFollowing && !p.narration.isPlaying,ratio+" illustration follows paper after speech ends");
                }
            }
            if(p.Completed && intro.RevealProgress>.99f && !homeShot) { homeShot=true;Shot(ratio+"-home-page"); }
        }
        Check(intro.Finished,ratio+" startup completes");
        if(index<3) {
            Check(openingShot&&silent,ratio+" opening captions and silent story");
            Check(read.All(x=>x),ratio+" all six pages narrated");
            Check(p.Completed && p.TurnsCompleted==6,ratio+" six single-leaf transitions including final turn");
            Check(gate&&stationary&&speechAfterInk,ratio+" stationary reveal then narration; home hidden until final turn");
            Check(gm.HasSeenThis(CursedBookPrologue.SeenKey),ratio+" completion recorded in save model");
            Check(!p.ShouldPlay(),ratio+" completed prologue will not replay");
        }else Check(p.PageIndex==-1 && p.TurnsCompleted==0,ratio+" returning player bypasses six pages");
        yield return new WaitForEndOfFrame();Shot(ratio+"-live-home");
        Check(!intro.group.blocksRaycasts && !intro.gameObject.activeSelf,ratio+" intro releases input");
        Check(GameplayCanvas.single.homeUI.GetComponent<GraphicRaycaster>().enabled,ratio+" home buttons restored");
        Check(gm.gameState==GameStateType.CAMP,ratio+" camp entered");
        if(index==2) {
            GameplayCanvas.single.homeUI.battleButton.onClick.Invoke();
            float battleDeadline=Time.realtimeSinceStartup+10;
            while(gm.gameState!=GameStateType.BATTLE && Time.realtimeSinceStartup<battleDeadline)yield return null;
            Check(gm.gameState==GameStateType.BATTLE,ratio+" battle entry works");
            GameplayCanvas.single.battleHud.backButton.onClick.Invoke();
            float campDeadline=Time.realtimeSinceStartup+10;
            while(gm.gameState!=GameStateType.CAMP && Time.realtimeSinceStartup<campDeadline)yield return null;
            Check(gm.gameState==GameStateType.CAMP,ratio+" return to camp works");
        }
        Check(errors==0,ratio+" no runtime errors");Application.logMessageReceived-=callback;
        Log(ratio+" COMPLETE");EditorApplication.isPlaying=false;
    }
}
