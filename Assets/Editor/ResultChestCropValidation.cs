using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit local QA only; never rebuilds authored UI or writes player saves.
[InitializeOnLoad]
public static class ResultChestCropValidation
{
    const string Out="output/result-chest-crop/";
    static bool running;
    static ResultChestCropValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("CHEST_CROP_QA",false)){SessionState.SetBool("CHEST_CROP_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("CHEST_CROP_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("CHEST_CROP_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
    static void Check(bool ok,string message){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Object.Destroy(t);}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        gc.homeUI.gameObject.SetActive(false);gc.ToggleBattleUi(false);gc.ToggleButtonGrid(false);gc.ToggleCampUi(false);
        var wm=WaveManager.single;wm.currentWaveset=wm.GetWavesetByKey("Khorasan Ruins II");
        Router.single.waitingForBattleCleanup=false;Router.single.newChest=new Chest();
        var ui=gc.victoryPanel.GetComponent<StorybookBattleResultUI>();gc.victoryPanel.SetActive(true);
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
            foreach(var grade in new[]{ChestGrade.NORMAL,ChestGrade.GOLD,ChestGrade.DIAMOND}){
                Router.single.newChest.chestGrade=grade;ui.Refresh();yield return new WaitForEndOfFrame();Shot(grade+"-"+size.x+"x"+size.y);
                var sprite=ui.chestImage.sprite;
                Check(sprite==ui.ChestSprite(grade)&&ui.chestImage.preserveAspect,"Correct quality and aspect "+grade+" "+size);
                Check(sprite.texture.GetPixel(0,0).a==0,"Transparent isolated source "+grade);
                var corners=new Vector3[4];ui.chestImage.rectTransform.GetWorldCorners(corners);bool inside=true;foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(null,c);inside &= p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height;}Check(inside,"Chest fully inside viewport "+size);
            }
        }
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
