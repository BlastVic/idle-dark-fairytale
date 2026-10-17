using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[InitializeOnLoad]
public static class CommonDialogValidation
{
    const string Dir="output/common-dialogs/";static bool running;
    static CommonDialogValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("DIALOG_QA",false)){SessionState.SetBool("DIALOG_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.SetBool("PROLOGUE_QA_FORCE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","");SessionState.SetBool("DIALOG_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("PROLOGUE_QA_FORCE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&!running&&SessionState.GetBool("DIALOG_QA",false)&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
    static void Check(bool ok,string s)=>File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+s+"\n");
    static IEnumerator Capture(GameObject modal,string name){
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            typeof(StorySkipValidation).GetMethod("Size",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{size.x,size.y});yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
            var chrome=modal.GetComponentInChildren<StorybookDialogChrome>();Check(chrome,"shared authored chrome "+name);if(!chrome)yield break;
            var corners=new Vector3[4];((RectTransform)chrome.transform).GetWorldCorners(corners);Check(corners[0].x>=0&&corners[0].y>=0&&corners[2].x<=Screen.width&&corners[2].y<=Screen.height,"bounds "+name+size);
            var b=chrome.back;var pointer=new PointerEventData(EventSystem.current){position=b.transform.TransformPoint(((RectTransform)b.transform).rect.center)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==b,"return raycast "+name+size);
            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+name+"-"+size.x+"x"+size.y+".png",tex.EncodeToPNG());Object.Destroy(tex);
            bool enabled=chrome.cancelAction.interactable;chrome.cancelAction.interactable=false;yield return new WaitForSecondsRealtime(.1f);
            Check(!b.IsInteractable()&&b.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>g.material.HasProperty("_DisabledUI")&&g.material.GetFloat("_DisabledUI")>0),"whole return disabled "+name+size+" interactable="+b.interactable+" graphics="+string.Join(",",b.GetComponentsInChildren<UnityEngine.UI.Graphic>().Select(g=>g.name+":"+(g.material.HasProperty("_DisabledUI")?g.material.GetFloat("_DisabledUI"):-1))));
            chrome.cancelAction.interactable=enabled;yield return null;yield return null;Check(b.interactable==enabled,"return restored "+name+size);
            var feedback=b.GetComponent<ShelterButtonFeedback>();var original=b.transform.localScale;feedback.OnPointerDown(pointer);Check(b.transform.localScale.x<original.x,"return pressed "+name+size);feedback.OnPointerUp(pointer);Check(b.transform.localScale==original,"return released "+name+size);
        }
    }
    static IEnumerator Run(){
        yield return new WaitForSecondsRealtime(3);var intro=Object.FindAnyObjectByType<CursedBookIntro>();float deadline=Time.realtimeSinceStartup+30;while(!intro.prologue.IsPlaying&&Time.realtimeSinceStartup<deadline)yield return null;
        var skip=intro.GetComponent<StorySkipUI>();skip.Open();yield return Capture(skip.confirmation,"skip");skip.confirmation.GetComponentInChildren<StorybookDialogChrome>().back.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(!skip.IsConfirming&&!intro.prologue.Paused&&!intro.Finished,"story back resumes without skipping");intro.SkipStory();yield return new WaitForSecondsRealtime(2);
        var gc=GameplayCanvas.single;var gm=GameManager.single;TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();for(int i=0;i<9;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);gm.playerLevel=1;
        gc.homeUI.OpenFunction(2);yield return Capture(gc.homeUI.modal,"rebirth-locked");gc.homeUI.modal.GetComponentInChildren<StorybookDialogChrome>().back.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(!gc.homeUI.modal.activeSelf,"shelter dialog returns");
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);var equipment=gc.inventoryPanel.GetComponent<StorybookEquipmentUI>();equipment.modalTitle.text="确认分解";equipment.modalMessage.text="分解 3 件装备\n获得 120 融合经验\n已装备及锁定装备不会被分解";StorybookPopupMotion.Show(equipment.modal);yield return Capture(equipment.modal,"equipment-confirm");equipment.modal.GetComponentInChildren<StorybookDialogChrome>().back.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(!equipment.modal.activeSelf,"equipment returns without decomposing");equipment.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        Router.single.CallRebirthFromCamp();yield return new WaitForSecondsRealtime(3);var rebirth=RebirthPackage.single;gm.playerLevel=60;rebirth.view.Refresh();rebirth.ToggleAreYouSure(true);yield return Capture(rebirth.view.confirmation,"rebirth-confirm");rebirth.view.confirmation.GetComponentInChildren<StorybookDialogChrome>().back.onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(!rebirth.view.confirmation.activeSelf,"rebirth returns without reset");
        File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
