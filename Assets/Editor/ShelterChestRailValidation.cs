using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShelterChestRailValidation
{
    public const string Out="output/chest-rail/";
    static bool started;static double deadline;
    static ShelterChestRailValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("CHEST_RAIL_QA",false)){SessionState.SetBool("CHEST_RAIL_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);started=false;}};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
            File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","Chest rail Unity validation; isolated project, no save writes.\n");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("CHEST_RAIL_QA",true);started=false;deadline=EditorApplication.timeSinceStartup+240;
            StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&SessionState.GetBool("CHEST_RAIL_QA",false)&&!started&&GameManager.single&&GameplayCanvas.single){started=true;deadline=EditorApplication.timeSinceStartup+240;GameManager.single.StartCoroutine(new ChestRailProbe().Run());}
        if(EditorApplication.isPlaying&&SessionState.GetBool("CHEST_RAIL_QA",false)&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog");EditorApplication.isPlaying=false;}
    }
    public static void Log(string s)=>File.AppendAllText(Out+"validation.txt",s+"\n");
}
public sealed class ChestRailProbe
{
    int failures,errors;ShelterChestRail drawer;ShelterHomeUI home;
    void Check(bool value,string name){ShelterChestRailValidation.Log((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
    void Error(string m,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;ShelterChestRailValidation.Log("ERROR "+m+"\n"+stack);}}
    void Shot(string name){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(ShelterChestRailValidation.Out+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}
    List<RaycastResult> Hits(RectTransform target){var hits=new List<RaycastResult>();var pos=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center));EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pos},hits);return hits;}
    void Pointer(Button button,string label){var hits=Hits((RectTransform)button.transform);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,label);}
    void Bounds(RectTransform target,string label){var corners=new Vector3[4];target.GetWorldCorners(corners);Check(corners.All(p=>p.x>=-1&&p.x<=Screen.width+1&&p.y>=-1&&p.y<=Screen.height+1),label);}
    IEnumerator EndMotion(){float end=Time.realtimeSinceStartup+3;while(drawer.IsAnimating&&Time.realtimeSinceStartup<end)yield return null;yield return new WaitForEndOfFrame();Check(!drawer.IsAnimating,"animation completes");}
    IEnumerator Collapse(string tag)
    {
        drawer.collapseButton.onClick.Invoke();drawer.Collapse();drawer.Expand();
        Check(drawer.IsAnimating&&!drawer.railGroup.blocksRaycasts&&!drawer.shortcutGroup.blocksRaycasts,"transition locks chest clicks and ignores repeated requests");
        yield return new WaitForSecondsRealtime(drawer.railDuration*.45f);yield return new WaitForEndOfFrame();
        Check(drawer.RailHidden>0&&drawer.RailHidden<1&&drawer.ShortcutVisible==0&&!drawer.shortcut.gameObject.activeSelf,"rail exits before shortcut appears");if(tag=="9x16")Shot("01-rail-exiting");
        yield return EndMotion();
        Check(drawer.Collapsed&&!drawer.rail.gameObject.activeSelf&&!drawer.railGroup.blocksRaycasts&&drawer.shortcut.gameObject.activeSelf&&drawer.expandButton.IsInteractable(),tag+" collapsed state releases hidden rail rays");
        var first=drawer.layout.functions[0];var second=drawer.layout.functions[1];
        Check(Mathf.Abs((drawer.shortcut.anchoredPosition.y-first.anchoredPosition.y)-(first.anchoredPosition.y-second.anchoredPosition.y))<.1f,tag+" shortcut has identical vertical button spacing");
        Check(Mathf.Abs(drawer.shortcut.anchoredPosition.x-first.anchoredPosition.x)<.1f&&drawer.shortcut.sizeDelta==first.sizeDelta,tag+" shortcut alignment and size match side buttons");
        Bounds(drawer.shortcut,tag+" shortcut fits screen");Pointer(drawer.expandButton,tag+" shortcut receives click");
        Check(Hits(drawer.railSlot).All(h=>!h.gameObject.transform.IsChildOf(drawer.rail)),tag+" hidden chest strip cannot intercept input");Shot(tag+"-collapsed");
    }
    IEnumerator Expand(string tag)
    {
        drawer.expandButton.onClick.Invoke();yield return new WaitForSecondsRealtime(drawer.shortcutDuration*.4f);yield return new WaitForEndOfFrame();
        Check(drawer.RailHidden==1&&drawer.ShortcutVisible<1&&!drawer.rail.gameObject.activeSelf,"reverse starts with shortcut leaving before rail returns");if(tag=="9x16")Shot("03-shortcut-exiting");
        yield return EndMotion();Check(!drawer.Collapsed&&drawer.rail.gameObject.activeSelf&&!drawer.shortcut.gameObject.activeSelf&&drawer.railGroup.blocksRaycasts&&!drawer.shortcutGroup.blocksRaycasts,tag+" expanded state restores rail input only");Pointer(drawer.collapseButton,tag+" collapse control receives click");Shot(tag+"-expanded");
    }
    public IEnumerator Run()
    {
        Application.logMessageReceived+=Error;var gm=GameManager.single;var gc=GameplayCanvas.single;
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        home=gc.homeUI;home.CloseModal();drawer=home.GetComponent<ShelterChestRail>();Check(drawer,"scene inherits persisted chest rail component");if(!drawer){Finish();yield break;}
        int nodeCount=home.GetComponentsInChildren<Transform>(true).Length;
        Check(home.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"no missing scripts");
        int[] widths={941,768,720},heights={1672,1024,1440};string[] names={"9x16","3x4","1x2"};
        for(int i=0;i<3;i++){
            StorybookEquipmentValidation.Resolution(widths[i],heights[i]);yield return new WaitForSecondsRealtime(.4f);drawer.layout.Apply();yield return new WaitForEndOfFrame();
            Bounds(drawer.railSlot,names[i]+" expanded rail fits screen");Pointer(drawer.collapseButton,names[i]+" collapse hit target");Shot(names[i]+"-initial");
            yield return Collapse(names[i]);foreach(var button in home.functionButtons)Pointer(button,names[i]+" existing side button "+button.name);
            yield return Expand(names[i]);
        }
        drawer.Collapse();yield return new WaitForSecondsRealtime(.1f);StorybookEquipmentValidation.Resolution(768,1024);yield return EndMotion();drawer.layout.Apply();yield return new WaitForEndOfFrame();Bounds(drawer.shortcut,"resize during animation ends inside screen");Pointer(drawer.expandButton,"resized shortcut remains clickable");
        var feedback=drawer.expandButton.GetComponent<ShelterButtonFeedback>();var scale=drawer.shortcut.localScale;var evt=new PointerEventData(EventSystem.current);feedback.OnPointerDown(evt);Check(drawer.shortcut.localScale.x<scale.x,"shortcut pressed state");feedback.OnPointerUp(evt);Check(drawer.shortcut.localScale==scale,"shortcut release restores scale");
        home.functionButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(3);Check(FusionPackage.single&&gm.gameState==GameStateType.FUSION,"side fusion navigation preserved when rail collapsed");if(FusionPackage.single)FusionPackage.single.Close();yield return new WaitForSecondsRealtime(2);
        Check(gm.gameState==GameStateType.CAMP&&drawer.Collapsed&&drawer.expandButton.IsInteractable(),"collapsed choice survives leaving and returning home");
        drawer.Expand();yield return new WaitForSecondsRealtime(.06f);home.gameObject.SetActive(false);home.gameObject.SetActive(true);yield return null;
        Check(!drawer.IsAnimating&&!drawer.Collapsed&&drawer.railGroup.blocksRaycasts&&!drawer.shortcut.gameObject.activeSelf,"interrupted reverse settles safely on re-enable");
        // Check original chest ownership bindings are still usable after moving the strip.
        var inventory=InventoryManager.single;
        if(inventory.chestsOwned.Count==0)inventory.chestsOwned.Add(new Chest{uniqueKey="ChestRailQA",rewardTime=DateTime.Now.AddMinutes(5).ToBinary().ToString(),possibleRewards=new List<Item>(),possibleGoldRewards=new List<Item>(),possibleDiamondRewards=new List<Item>()});
        home.Refresh();yield return null;Pointer(home.chestButtons[0],"original chest slot raycast after expansion");home.chestButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(3);
        Check(gm.gameState==GameStateType.CHEST_PACKAGE&&ChestPackage.single,"original chest details still open");Router.single.CallCampFromChest(false);yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&drawer.collapseButton.IsInteractable(),"chest details return restores expanded strip");
        Check(home.GetComponentsInChildren<Transform>(true).Length==nodeCount,"no runtime UI node generation");Check(errors==0,"no unexpected runtime errors");Finish();
    }
    void Finish(){Application.logMessageReceived-=Error;ShelterChestRailValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
