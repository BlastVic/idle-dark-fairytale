using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Game.Enum;

[InitializeOnLoad]
public static class BlackForestCampValidation
{
    const string Dir=BlackForestCampBuilder.Out;
    static bool running;
    static BlackForestCampValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){if(!running&&SessionState.GetBool("FOREST_CAMP_QA",false)&&GameManager.single){running=true;GameManager.single.StartCoroutine(Run());}return;}
        if(!File.Exists(Dir+"validate.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Actual Unity forest camp validation\n");
        SessionState.SetBool("FOREST_CAMP_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        Resolution(720,1280);EditorApplication.isPlaying=true;
    }
    static void Resolution(int w,int h){typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});}
    static void Check(bool b,string s){File.AppendAllText(Dir+"validation.txt",(b?"PASS ":"FAIL ")+s+"\n");}
    static void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run(){
        yield return null;yield return null;
        var wm=WaveManager.single;var gm=GameManager.single;
        gm.thingsSeenPermanent.Add("Tutorial-0");gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        wm.highestWavesReached.RemoveAll(x=>x.levelKey=="Mount Zion");
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);if(intro&&intro.prologue)intro.prologue.previewInEditor=false;
        float deadline=Time.realtimeSinceStartup+120;bool bookShot=false;
        while(intro&&!intro.Finished&&Time.realtimeSinceStartup<deadline){yield return new WaitForEndOfFrame();if(intro.RevealProgress>.98f&&!bookShot){Shot("opening-book-forest");bookShot=true;}}
        var ui=GameplayCanvas.single.homeUI;var select=ui.GetComponent<ShelterChapterBackdrop>();Check(select!=null,"saved chapter backdrop component");
        if(!select){Finish();yield break;}
        select.Refresh();Check(!select.IsCabin&&select.forest.gameObject.activeInHierarchy&&!select.cabin.gameObject.activeSelf,"before chapter 1 forest only");
        var character=select.forest.Find("ForestRedHood").GetComponent<SkeletonGraphic>();var fire=select.forest.Find("ForestCampfire").GetComponent<SkeletonGraphic>();var random=fire.GetComponent<SpineCampfireRandom>();
        Check(character.IsValid&&fire.IsValid,"both Spine rigs load");Check(!character.raycastTarget&&!fire.raycastTarget,"no UI input interception");
        for(int a=0;a<3;a++)Check(fire.Skeleton.Data.FindAnimation("Burn"+(char)('A'+a))!=null,"authored burn clip "+a);
        int[] widths={720,768,720},heights={1280,1024,1440};string[] ratios={"9x16","3x4","1x2"};
        for(int i=0;i<3;i++){
            Resolution(widths[i],heights[i]);yield return new WaitForSecondsRealtime(.5f);
            select.layout.introCompositionSize=Vector2.zero;select.layout.SelectScene(select.forest,select.forestSize,select.forestFocalBottom);yield return new WaitForEndOfFrame();
            Shot(ratios[i]+"-forest");Check(Screen.width==widths[i]&&Screen.height==heights[i],ratios[i]+" actual resolution");
            var corners=new Vector3[4];select.layout.attributes.GetWorldCorners(corners);float panelTop=corners[1].y;
            var feet=select.forest.TransformPoint(new Vector3(305f/941*1080-540,-827f/1672*1920,0));
            // The authored room pivots at its top center.
            var pit=select.forest.TransformPoint(new Vector3(596f/941*1080-540,-1015f/1672*1920,0));
            Check(feet.y>panelTop&&pit.y>panelTop,ratios[i]+" heroine and fire clear expanded attributes");
            select.layout.chests.GetWorldCorners(corners);var head=select.forest.TransformPoint(new Vector3(305f/941*1080-540,-434f/1672*1920,0));Check(head.y<corners[0].y,ratios[i]+" heroine head below chest strip");
        }
        Resolution(720,1280);yield return new WaitForSecondsRealtime(.5f);select.layout.SelectScene(select.forest,select.forestSize,select.forestFocalBottom);
        character.timeScale=0;character.AnimationState.ClearTracks();var track=character.AnimationState.SetAnimation(0,"Idle",true);track.MixDuration=0;track.TrackTime=0;character.Update(0);yield return new WaitForEndOfFrame();Shot("eyes-open");
        track.TrackTime=2.2f;character.Update(0);yield return new WaitForEndOfFrame();Shot("eyes-closed");Check(character.Skeleton.FindSlot("eyes").A>.99f,"closed blink frame");
        track.TrackTime=2.4f;character.Update(0);Check(character.Skeleton.FindSlot("eyes").A<.01f,"eyes reopen");character.timeScale=1;
        string last=null;int transitions=random.Transitions;int changes=0;bool noRepeat=true;float end=Time.realtimeSinceStartup+24,nextShot=0;int frame=0;
        Directory.CreateDirectory(Dir+"frames");
        while(Time.realtimeSinceStartup<end){yield return new WaitForEndOfFrame();if(random.Transitions!=transitions){if(last!=null){changes++;noRepeat &= random.CurrentClip!=last;}last=random.CurrentClip;transitions=random.Transitions;}if(Time.realtimeSinceStartup>nextShot){Shot("frames/idle-"+frame.ToString("0000"));frame++;nextShot=Time.realtimeSinceStartup+.1f;}}
        Check(changes>=4&&random.PlayedMask==7,"all three random clips played; changes="+changes);Check(noRepeat,"no repeated consecutive clip transitions observed");
        var level=wm.GetWavesetByKey("Mount Zion");wm.SetWaveseReachedByKey("Mount Zion",level.waves.Length-1);select.Refresh();Check(!select.IsCabin,"reaching final wave does not unlock cabin");
        wm.SetWaveseReachedByKey("Mount Zion",level.waves.Length);select.Refresh();yield return new WaitForEndOfFrame();Shot("chapter1-cleared-cabin");Check(select.IsCabin&&!select.forest.gameObject.activeSelf&&select.cabin.gameObject.activeInHierarchy,"complete chapter switches to original cabin");
        ui.gameObject.SetActive(false);ui.gameObject.SetActive(true);select.Refresh();Check(select.IsCabin,"reopening shelter keeps cabin progression");
        wm.highestWavesReached.RemoveAll(x=>x.levelKey=="Mount Zion");select.Refresh();yield return new WaitForEndOfFrame();
        ui.battleButton.onClick.Invoke();deadline=Time.realtimeSinceStartup+12;while(gm.gameState!=GameStateType.BATTLE&&Time.realtimeSinceStartup<deadline)yield return null;Check(gm.gameState==GameStateType.BATTLE,"battle entry");
        GameplayCanvas.single.battleHud.backButton.onClick.Invoke();deadline=Time.realtimeSinceStartup+12;while(gm.gameState!=GameStateType.CAMP&&Time.realtimeSinceStartup<deadline)yield return null;Check(gm.gameState==GameStateType.CAMP,"return to shelter");
        Finish();
    }
    static void Finish(){File.AppendAllText(Dir+"validation.txt","FINISHED (save disabled)\n");SessionState.SetBool("FOREST_CAMP_QA",false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}
}
