using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Game.Enum;

[InitializeOnLoad]
public static class FirstDefeatValidation
{
    const string Dir="output/first-defeat/";
    static bool running;
    static FirstDefeatValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode){running=false;SessionState.SetBool("FIRST_DEFEAT_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}};}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","");SessionState.SetBool("FIRST_DEFEAT_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&!running&&SessionState.GetBool("FIRST_DEFEAT_QA",false)&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}
    }
    static void Check(bool ok,string name)=>File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+name+"\n");
    static IEnumerator Run(){
        yield return new WaitForSecondsRealtime(2);
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)intro.SkipStory();yield return new WaitForSecondsRealtime(2);
        var gm=GameManager.single;var gc=GameplayCanvas.single;var wm=WaveManager.single;var tutorial=TutorialManager.single;
        tutorial.automatic=false;tutorial.view.Hide();gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<9;i++)gm.thingsSeenPermanent.Remove("Tutorial-"+i);
        gm.thingsSeenPermanent.Add("Tutorial-0");gm.thingsSeenPermanent.Add("Tutorial-1");gm.statPoints=2;gm.lastStatPurchased=Scripts.Level.LevelTypeEnum.NONE;
        wm.currentWaveset=wm.GetWavesetByKey("Khorasan Ruins II");Router.single.waitingForBattleCleanup=false;
        gc.ToggleCampUi(false);gc.ToggleBattleUi(false);gm.SetGameState(GameStateType.DEFEAT);gc.defeatPanel.SetActive(true);
        var ui=gc.defeatPanel.GetComponent<StorybookBattleResultUI>();yield return new WaitForSecondsRealtime(.4f);
        Check(ui.RequiresTutorialReturn&&!ui.replay.gameObject.activeSelf&&!ui.replaySmall.gameObject.activeSelf&&ui.home.gameObject.activeInHierarchy,"first defeat exposes only shelter action");
        ui.replay.onClick.Invoke();Check(!ui.IsBusy&&gc.defeatPanel.activeSelf,"hidden replay cannot bypass onboarding");
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            typeof(StorySkipValidation).GetMethod("Size",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{size.x,size.y});yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+size.x+"x"+size.y+".png",tex.EncodeToPNG());Object.Destroy(tex);
            var corners=new Vector3[4];((RectTransform)ui.home.transform).GetWorldCorners(corners);Check(corners[0].x>=0&&corners[0].y>=0&&corners[2].x<=Screen.width&&corners[2].y<=Screen.height,"shelter button bounds "+size);
        }
        gm.thingsSeenPermanent.Add("Tutorial-2");ui.Refresh();Check(!ui.RequiresTutorialReturn&&ui.replay.gameObject.activeSelf,"completed saved tutorial restores retry");
        gm.thingsSeenPermanent.Remove("Tutorial-2");ui.Refresh();tutorial.automatic=true;ui.home.onClick.Invoke();yield return new WaitForSecondsRealtime(4);
        Check(gm.gameState==GameStateType.CAMP&&!gc.defeatPanel.activeSelf&&gc.homeUI.gameObject.activeInHierarchy,"return enters shelter");
        Check(tutorial.ActiveKey==2&&tutorial.view.Visible,"shelter attribute tutorial starts key="+tutorial.ActiveKey+" button="+gc.homeUI.statButtons[0].IsInteractable());
        File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
