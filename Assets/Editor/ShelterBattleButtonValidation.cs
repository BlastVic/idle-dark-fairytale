using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit local QA only; never rebuilds authored UI or writes player saves.
[InitializeOnLoad]
public static class ShelterBattleButtonValidation
{
    const string Out="output/shelter-button/";
    static bool running;
    static ShelterBattleButtonValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("BATTLE_BUTTON_QA",false)){SessionState.SetBool("BATTLE_BUTTON_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("BATTLE_BUTTON_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("BATTLE_BUTTON_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
    static void Check(bool ok,string message){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Object.Destroy(t);}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        var b=gc.homeUI.battleButton;var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        Check(b.transition==Selectable.Transition.None,"Composite ribbon does not tint its base separately");
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);var scale=b.transform.localScale;
            yield return new WaitForEndOfFrame();Shot(size+"-normal");
            ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Shot(size+"-pressed");Check(Vector3.Distance(b.transform.localScale,scale*.96f)<.001f,"Whole ribbon scales on press "+size);Check(b.targetGraphic.canvasRenderer.GetColor()==Color.white,"No local text-background tint "+size);
            ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return null;Check(b.transform.localScale==scale,"Release restores size "+size);
            b.interactable=false;yield return new WaitForEndOfFrame();Shot(size+"-disabled");b.interactable=true;yield return new WaitForEndOfFrame();Shot(size+"-restored");
        }
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
