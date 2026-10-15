using System;
using System.Collections;
using System.IO;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FusionReturnRegression
{
    const string Dir="output/fusion-return/",Key="FUSION_RETURN_QA";
    static bool running;static double deadline;static int exceptions;
    static FusionReturnRegression(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);Application.logMessageReceived-=Error;running=false;}};}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            SessionState.SetBool(Key,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(!running&&GameManager.single&&GameplayCanvas.single){running=true;exceptions=0;deadline=EditorApplication.timeSinceStartup+120;Application.logMessageReceived+=Error;GameManager.single.StartCoroutine(Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Check(false,"timeout");EditorApplication.isPlaying=false;}
    }
    static void Error(string msg,string trace,LogType type){if(type==LogType.Exception&&trace.Contains("StorybookFusionUI")){exceptions++;File.AppendAllText(Dir+"validation.txt",msg+"\n"+trace+"\n");}}
    static void Check(bool ok,string s)=>File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+s+"\n");
    static IEnumerator Open(){Router.single.CallFusionFromCamp();yield return new WaitForSecondsRealtime(2);}
    static IEnumerator Run(){
        var gm=GameManager.single;var gc=GameplayCanvas.single;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.dailyCherriesPop.SetActive(false);
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);
            gm.fusionLevel=6;gm.fxpMax=310;gm.fxpNow=180;gm.crystals=130;
            yield return Open();var ui=FusionPackage.single.view;
            Check(!ui.hint,"current prefab deliberately has no optional hint");
            ui.Refresh();ui.action.onClick.Invoke();ui.Close();
            Check(ui.Busy&&!ui.Leaving,"fill blocks leaving without exception");
            yield return new WaitForSecondsRealtime(1.5f);
            Check(!ui.Busy&&ui.Ready&&gm.crystals==0,"fill completes and unlocks navigation");
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+size.x+"x"+size.y+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            Check(!FusionPackage.single&&gm.gameState==GameStateType.CAMP,"back returns after filling");
            yield return Open();ui=FusionPackage.single.view;ui.returnToShelter.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            Check(!FusionPackage.single&&gm.gameState==GameStateType.CAMP,"bottom return button works");
            yield return Open();ui=FusionPackage.single.view;int before=gm.fusionLevel;ui.action.onClick.Invoke();
            yield return new WaitForSecondsRealtime(3);
            Check(gm.fusionLevel==before+1&&!FusionPackage.single&&gm.gameState==GameStateType.CAMP,"upgrade succeeds and returns with absent hint");
        }
        Check(exceptions==0,"no fusion exceptions");File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
