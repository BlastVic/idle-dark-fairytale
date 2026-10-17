using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class ChinesePrologueVoiceValidation {
    const string Dir="output/prologue-zh-integrated/";static bool running;
    static ChinesePrologueVoiceValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(File.Exists(Dir+"cancel.request")){
            File.Delete(Dir+"cancel.request");if(File.Exists(Dir+"validate.request"))File.Delete(Dir+"validate.request");
            SessionState.SetBool("ZH_VOICE_QA",false);SessionState.SetBool("PROLOGUE_QA_FORCE",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;
            return;
        }
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){if(!running&&SessionState.GetBool("ZH_VOICE_QA",false)&&GameManager.single){running=true;GameManager.single.StartCoroutine(Run());}return;}
        if(!File.Exists(Dir+"validate.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool("ZH_VOICE_QUICK",File.ReadAllText(Dir+"validate.request").Trim()=="quick");File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt", "Chinese voiced prologue\n");
        SessionState.SetBool("ZH_VOICE_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("PROLOGUE_QA_FORCE",true);Size(720,1280);EditorApplication.isPlaying=true;
    }
    static void Size(int w,int h){typeof(CursedBookIntroValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});}
    static void Check(bool ok,string msg){File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+msg+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run(){
        yield return null;var gm=GameManager.single;gm.thingsSeenPermanent.Add("Tutorial-0");
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);var p=intro.prologue;
        int[] firstCaptions={5,10,14,18,24,29};
        for(int i=0;i<6;i++)Check(p.voiceCaptions[firstCaptions[i]].start-p.pageRevealTimes[i]-p.pageTurnDuration-p.pageInkDuration>=.8f,"page "+(i+1)+" settles at least 0.8 seconds before narration");
        float deadline=Time.realtimeSinceStartup+210;bool closed=false,opening=false,pushed=false,held=false,portrait=false,forest=false,newEnding=false;float initialScale=0;bool[] pages=new bool[6];bool[] turnShots=new bool[6];bool quiet=true;int errors=0;
        Application.LogCallback handler=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error){errors++;File.AppendAllText(Dir+"validation.txt",m+"\n");}};Application.logMessageReceived+=handler;
        while(!intro.Finished&&Time.realtimeSinceStartup<deadline){
            yield return new WaitForEndOfFrame();
            if(!p.IsPlaying)continue;
            var audio=intro.GetComponent<CursedBookIntroAudio>();quiet &= !audio.foley.isPlaying&&!audio.atmosphere.isPlaying;
            if(p.VoiceTime>4&&!closed){closed=true;initialScale=intro.cameraRig.localScale.x;Shot("closed-book-narration");Check(p.PageIndex==-1&&!p.openingText.gameObject.activeSelf&&p.subtitle.text.Length>0,"opening uses only bottom captions");Check(intro.book.AnimationState.GetCurrent(0).TrackTime==0 && intro.book.Skeleton.FindSlot("turning_page_7").A<.01f,"closed cover with no exposed turning leaf");if(SessionState.GetBool("ZH_VOICE_QUICK",false))p.narration.time=9f;}
            if(p.VoiceTime>9f&&!pushed){pushed=true;Shot("early-camera-push");Check(intro.cameraRig.localScale.x>initialScale && intro.book.AnimationState.GetCurrent(0).TrackTime==0,"camera advances while cover stays closed");Check(Mathf.Abs(p.openingBookEnd-p.openingBookStart-3.7f)<.01f,"cover finishes just before portrait reveal");if(SessionState.GetBool("ZH_VOICE_QUICK",false))p.narration.time=10.72f;}
            if(p.VoiceTime>10.72f&&!held){held=true;float scale=intro.cameraRig.localScale.x;yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Shot("camera-arrived-cover-held");Check(Mathf.Abs(intro.cameraRig.localScale.x-scale)<.001f && intro.book.AnimationState.GetCurrent(0).TrackTime==0,"camera stationary at target before cover opens");Check(Mathf.Abs(p.openingBookStart-p.openingCameraEnd-.3f)<.01f,"explicit 0.3 second hold between camera and cover");if(SessionState.GetBool("ZH_VOICE_QUICK",false))p.narration.time=12f;}
            if(p.VoiceTime>13&&!opening){opening=true;Shot("late-cover-opening");Check(!p.coverOpenSound&&!p.pageFoley.isPlaying,"cover opens without foley");Check(Mathf.Abs((10.7f-8.32f)/(p.openingCameraEnd-p.openingCameraStart)-.7f)<.001f,"camera speed is 70 percent with unchanged endpoint");Check(intro.book.AnimationState.GetCurrent(0).TrackTime>0,"cover opens at the scheduled cue");}
            if(p.VoiceTime>17.05f&&!portrait){portrait=true;Shot("sketch-portrait-only");Check(p.PageIndex==-1&&p.illustration.texture==p.openingSketch&&p.illustration.material.GetFloat("_PortraitReveal")>.99f&&p.illustration.material.GetFloat("_ForestReveal")==0,"portrait appears alone before forest cue");}
            if(p.VoiceTime>21&&!forest){forest=true;Shot("sketch-both-visible");Check(p.illustration.material.GetFloat("_ForestReveal")>.99f,"forest revealed after portrait on existing paper");
                int[] sw={720,768,720},sh={1280,1024,1440};string[] sn={"9x16","3x4","1x2"};
                for(int k=0;k<3;k++){Size(sw[k],sh[k]);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();Shot("sketch-"+sn[k]);}
                Size(720,1280);
            }
            if(p.PageIndex==5&&p.VoiceTime>150&&!newEnding){newEnding=true;Shot("new-ending-wolf-caption");Check(p.subtitle.text.Contains("狼的嚎叫"),"new ending captions match wolf narration");if(SessionState.GetBool("ZH_VOICE_QUICK",false))p.narration.time=p.fullNarration.length-1;}
            if(p.PageIndex>=0&&p.TurningPage&&!turnShots[p.PageIndex]&&p.VoiceTime-p.pageRevealTimes[p.PageIndex]>.55f){
                turnShots[p.PageIndex]=true;Shot("attached-turn-"+p.PageIndex);
                Check(p.pageSurface.IsFollowing&&p.illustration.color.a>.99f,"illustration stays opaque on moving leaf "+p.PageIndex);
            }
            if(p.PageIndex>=0&&p.Reading&&!pages[p.PageIndex]){
                pages[p.PageIndex]=true;Shot("page-"+(p.PageIndex+1));Check(p.illustration.texture==p.pages[p.PageIndex].illustration,"page "+(p.PageIndex+1)+" audio/image alignment");
                if(p.PageIndex==0){
                    int[] w={720,768,720},h={1280,1024,1440};string[] r={"9x16","3x4","1x2"};
                    for(int i=0;i<3;i++){Size(w[i],h[i]);yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();Shot(r[i]);var corners=new Vector3[4];p.subtitle.rectTransform.GetWorldCorners(corners);Check(corners.All(c=>c.x>=0&&c.x<=Screen.width+1&&c.y>=0&&c.y<=Screen.height+1),r[i]+" captions in viewport");}
                    Size(720,1280);
                }
                if(SessionState.GetBool("ZH_VOICE_QUICK",false))p.narration.time=p.PageIndex<5?p.pageRevealTimes[p.PageIndex+1]-.4f:149f;
            }
        }
        Check(intro.Finished&&p.Completed&&pages.All(x=>x),"full narration and six illustrations complete");Check(p.TurnsCompleted==7,"seven single-page turns including sketch and final transition");Check(quiet,"no old opening soundtrack overlaps");Check(!intro.group.blocksRaycasts,"home input restored");Check(errors==0,"no runtime errors");
        // Exercise the real map replay entry, including the new closed-cover opening.
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        GameplayCanvas.single.homeUI.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        var map=UnityEngine.Object.FindFirstObjectByType<StorybookMapUI>();
        Check(map!=null,"map accessible after prologue");
        if(map){map.SelectNode(0);map.Refresh();map.EnterSelected();yield return new WaitForSecondsRealtime(4);
            Check(map.ActiveStory && map.ActiveStory.prologue.narration.isPlaying,"map replay uses voiced prologue");
            if(map.ActiveStory){var replay=map.ActiveStory.prologue;Shot("map-replay-opening");Check(!replay.openingText.gameObject.activeSelf && replay.PageIndex==-1,"replay also begins closed with bottom captions");replay.narration.time=replay.fullNarration.length-.3f;}
            yield return new WaitForSecondsRealtime(3);Check(!map.ActiveStory&&!map.IsNavigating&&map.controls.blocksRaycasts,"replay returns control to map");
        }
        Application.logMessageReceived-=handler;SessionState.SetBool("ZH_VOICE_QA",false);SessionState.SetBool("PROLOGUE_QA_FORCE",false);File.AppendAllText(Dir+"validation.txt","FINISHED\n");EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;
    }
}
