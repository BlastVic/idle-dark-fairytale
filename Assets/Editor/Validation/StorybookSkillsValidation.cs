using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Game.Enum;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookSkillsValidation
{
    public const string Dir="output/skills-ui/";
    static bool running;
    static double deadline;
    static StorybookSkillsValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SKILLS_QA",false)){SessionState.SetBool("SKILLS_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);if(SessionState.GetBool("SKILLS_QA_LOCK",false)){SessionState.SetBool("SKILLS_QA_LOCK",false);EditorApplication.UnlockReloadAssemblies();}running=false;}};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!SessionState.GetBool("EQUIPMENT_QA",false)&&!SessionState.GetBool("BOOK_ENTRY_QA",false)&&!File.Exists("output/equipment-ui/validate.request")&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Skills actual Unity verification; test save persistence disabled\n");
            SessionState.SetBool("SKILLS_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("SKILLS_QA_LOCK",true);EditorApplication.LockReloadAssemblies();running=false;deadline=EditorApplication.timeSinceStartup+600;StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;
        }
        if(SessionState.GetBool("SKILLS_QA",false)&&EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+600;GameManager.single.StartCoroutine(new SkillsProbe().Run());}
        if(EditorApplication.isPlaying&&deadline>0&&SessionState.GetBool("SKILLS_QA",false)&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog timeout");EditorApplication.isPlaying=false;}
    }
    public static void Log(string s)=>File.AppendAllText(Dir+"validation.txt",s+"\n");
}
public sealed class SkillsProbe
{
    StorybookSkillsUI ui;
    int failures,errors;
    void Check(bool value,string name){StorybookSkillsValidation.Log((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
    void Error(string m,string s,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;StorybookSkillsValidation.Log("ERROR "+m+"\n"+s);}}
    void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookSkillsValidation.Dir+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    void Pointer(Button button,string name){var r=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Check(p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,name+" pointer unobstructed");}
    StorybookSkillNode N(int id)=>ui.nodes.First(n=>(int)n.key==id);
    void Fixture(){var s=SkillsController.Instance;s.ResetSkills();s.regenLevel=1;s.angelOnMyShoulderLevel=1;GameManager.single.skillPoints=3;GameManager.single.playerLevel=20;ui.Refresh();}
    IEnumerator WaitForMotion(){float end=Time.realtimeSinceStartup+3;while(ui&&ui.IsAnimating&&Time.realtimeSinceStartup<end)yield return null;}
    public IEnumerator Run()
    {
        Application.logMessageReceived+=Error;
        if(!GameManager.single.HasSeenThis(CursedBookPrologue.SeenKey))GameManager.single.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&!intro.Finished&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;var gm=GameManager.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){var tutorial=TutorialManager.single;tutorial.automatic=false;if(tutorial.view)tutorial.view.Hide();}
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);
        ui=gc.skillsPanel.GetComponent<StorybookSkillsUI>();Check(ui&&ui.isActiveAndEnabled,"existing Skills route opens authored prefab");if(!ui){Finish();yield break;}
        yield return new WaitForEndOfFrame();Shot("current-save-9x16");
        ui.Select(N(3));yield return new WaitForSecondsRealtime(.4f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot("current-save-detail-9x16");ui.CloseDrawer();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();
        Check(ui.nodes.Length==15&&ui.links.Length==16,"15 nodes and all 16 directed dependencies are persisted");
        var legacy=UnityEngine.Object.FindObjectsByType<SkillButton>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(n=>new SerializedObject(n)).ToArray();
        foreach(var node in ui.nodes){var old=legacy.FirstOrDefault(n=>n.FindProperty("skillKey").intValue==(int)node.key);if(old!=null)Check(old.FindProperty("prerequisiteKey").intValue==(int)node.prerequisite&&old.FindProperty("prerequisiteKey2").intValue==(int)node.alternatePrerequisite,node.displayName+" prerequisites match legacy configuration");else Check(new[]{node.prerequisite,node.alternatePrerequisite}.All(k=>k==SkillsTypeEnum.NONE||ui.nodes.Any(n=>n.key==k)),node.displayName+" authored prerequisite references resolve");}
        Check(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"all serialized component references resolve");
        Fixture();
        Check(N(1).available.activeSelf&&N(1).availableSymbol.text=="↑","learned skill with enough points shows upgrade bookmark");
        Check(N(3).available.activeSelf&&N(3).availableSymbol.text=="+","unlearned skill with enough points shows learn bookmark");
        Check(!N(5).available.activeSelf&&N(5).locked.activeSelf,"locked skill hides action bookmark");
        Check(ui.nodes.All(n=>n.availableSymbol&&n.available.GetComponentsInChildren<StorybookSkillMark>(true).Any(m=>m.bookmark)),"all 15 action states have persisted bookmark and symbol");
        gm.skillPoints=0;ui.Refresh();Check(ui.nodes.All(n=>!n.available.activeSelf),"zero points hides all action bookmarks");
        Check(ui.nodes.All(n=>!n.availableBackground||!n.availableBackground.gameObject.activeSelf),"zero points hides any optional background images");
        gm.skillPoints=3;ui.Refresh();Check(N(1).available.activeSelf&&N(3).available.activeSelf,"replenishing points restores action bookmarks");
        Check(ui.nodes.All(n=>!n.availableBackground||n.availableBackground.gameObject.activeSelf==n.available.activeSelf),"optional backgrounds follow bookmark availability");
        int[] widths={941,768,720},heights={1672,1024,1440};string[] ratios={"9x16","3x4","1x2"};
        for(int i=0;i<3;i++){
            StorybookEquipmentValidation.Resolution(widths[i],heights[i]);yield return new WaitForSecondsRealtime(.4f);ui.layout.Apply();ui.treeScroll.verticalNormalizedPosition=1;
            yield return new WaitForEndOfFrame();Shot(ratios[i]+"-tree");
            foreach(var button in ui.navigation.Concat(new[]{ui.back}))Pointer(button,ratios[i]+" "+button.name);
            Pointer(N(1).button,ratios[i]+" root");N(1).button.onClick.Invoke();yield return new WaitForSecondsRealtime(.08f);
            Check(ui.IsAnimating&&ui.drawer.gameObject.activeSelf,ratios[i]+" opening animation has intermediate frame");
            yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot(ratios[i]+"-right-drawer");
            var action=(RectTransform)ui.purchase.transform;
            Check(-action.anchoredPosition.y-ui.cost.rectTransform.rect.height>=14,ratios[i]+" cost and action have breathing room");
            Check(ui.footerInset.y>=20&&ui.purchaseCaption.fontSize<=26,ratios[i]+" action bottom inset and text scale");
            SkillsController.Instance.regenLevel=0;gm.skillPoints=0;ui.Refresh();yield return new WaitForEndOfFrame();Shot(ratios[i]+"-insufficient-points");
            SkillsController.Instance.regenLevel=1;gm.skillPoints=3;ui.Refresh();yield return new WaitForEndOfFrame();
            Check(ui.DrawerOpen&&ui.DrawerOnRight,ratios[i]+" left node opens right drawer");Pointer(ui.purchase,ratios[i]+" upgrade");Pointer(ui.close,ratios[i]+" close");Pointer(N(1).button,ratios[i]+" selected stays visible");
            Check(ui.drawer.rect.height<ui.overlay.rect.height-20,ratios[i]+" content drawer does not fill tree height");
            var corners=new Vector3[4];ui.drawer.GetWorldCorners(corners);Check(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),ratios[i]+" drawer within safe viewport");
            float tall=ui.drawer.rect.height;
            var data=new PointerEventData(EventSystem.current);var feedback=ui.purchase.GetComponent<ShelterButtonFeedback>();var scale=ui.purchase.transform.localScale;feedback.OnPointerDown(data);Check(ui.purchase.transform.localScale.x<scale.x,ratios[i]+" purchase pressed feedback");feedback.OnPointerUp(data);Check(ui.purchase.transform.localScale==scale,ratios[i]+" purchase restores");
            ui.close.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();Check(!ui.DrawerOpen&&!ui.drawerGroup.blocksRaycasts&&!ui.drawer.gameObject.activeSelf,ratios[i]+" hidden drawer does not block rays");
            N(3).button.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot(ratios[i]+"-left-drawer");
            Check(ui.DrawerOpen&&!ui.DrawerOnRight,ratios[i]+" right node opens left drawer");Check(!ui.currentValues.transform.parent.gameObject.activeSelf&&ui.nextValues.transform.parent.gameObject.activeSelf,ratios[i]+" unlearned skill hides entire current section");Pointer(ui.purchase,ratios[i]+" learn");Pointer(N(3).button,ratios[i]+" right selection visible");
            float unlearnedHeight=ui.drawer.rect.height;
            SkillsController.Instance.sinisterStampedeLevel=2;ui.Refresh();yield return new WaitForEndOfFrame();Shot(ratios[i]+"-shadow-detail");
            Check(ui.currentValues.transform.parent.gameObject.activeSelf&&ui.drawer.rect.height>unlearnedHeight+20,ratios[i]+" current section restores and hidden section collapses spacing");
            Check(ui.currentHeading.text.Contains("Lv. 2")&&ui.nextHeading.text.Contains("<color=#8F2018>Lv. 3</color>")&&ui.nextValues.text.Contains("<color=#8F2018>9%</color>"),ratios[i]+" level2 to3 preview keeps target numbers red");
            Check(ui.description.text=="有概率触发范围攻击。\n敌人多于1名时生效。"&&!ui.currentValues.text.Contains("敌人")&&!ui.nextValues.text.Contains("敌人")&&!ui.nextValues.text.Contains("+"),ratios[i]+" common condition appears once and no difference annotation");
            Check(ui.description.color.r<.2f&&ui.currentValues.color.r<.2f&&ui.nextValues.color.r<.2f,ratios[i]+" body and labels use dark ink");
            SkillsController.Instance.sinisterStampedeLevel=0;ui.Refresh();
            ui.Select(N(1));yield return new WaitForSecondsRealtime(.55f);yield return WaitForMotion();Check(ui.DrawerOnRight&&ui.Selected==N(1)&&!ui.IsAnimating,ratios[i]+" switching sides closes then reopens");
            var gesture=ui.drawer.GetComponent<StorybookSkillsGesture>();var drag=new PointerEventData(EventSystem.current){pressPosition=new Vector2(200,500),position=new Vector2(240,500)};gesture.OnBeginDrag(drag);drag.position=new Vector2(400,500);gesture.OnDrag(drag);gesture.OnEndDrag(drag);yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();Check(!ui.DrawerOpen,ratios[i]+" outward swipe dismisses drawer");
            ui.Select(N(6));yield return new WaitForSecondsRealtime(.55f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot(ratios[i]+"-resurrection-detail");
            Check(ui.prerequisites.text.Contains("满足任一项")&&!ui.prerequisites.text.Contains("\n或 "),ratios[i]+" alternative prerequisites have a shared heading and aligned rows");
            Check(ui.prerequisiteRows.Length==2&&ui.prerequisiteRows.All(r=>r.activeSelf)&&ui.prerequisiteIcons[0].sprite==ui.prerequisiteUnmet&&ui.prerequisiteIcons[1].sprite==ui.prerequisiteMet,ratios[i]+" authored prerequisite images show unmet devil and met angel");
            Check(ui.prerequisiteLabels.All(t=>!t.text.Contains("○")&&!t.text.Contains("✓")&&!t.text.Contains("√"))&&ui.prerequisiteIcons.All(im=>im.sprite&&!im.raycastTarget),ratios[i]+" condition indicators use transparent sprites instead of text");
            Check(ui.description.fontSize==22&&ui.description.lineSpacing>1&&ui.description.text.Contains("\n"),ratios[i]+" resurrection description has separate effect lines");
            Pointer(ui.purchase,ratios[i]+" resurrection learn");ui.CloseDrawer();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();
            ui.treeScroll.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();Shot(ratios[i]+"-bottom-tree");
            var last=N(15).Rect;var p=RectTransformUtility.WorldToScreenPoint(null,last.TransformPoint(last.rect.center));Check(RectTransformUtility.RectangleContainsScreenPoint(ui.treeScroll.viewport,p),ratios[i]+" last row reachable by scrolling");
        }
        ui.treeScroll.verticalNormalizedPosition=1;ui.layout.SetValidationSafeArea(new Rect(0,60,Screen.width,Screen.height-110));yield return new WaitForSecondsRealtime(.2f);ui.Select(N(1));yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot("1x2-safe-area");Pointer(ui.purchase,"safe-area purchase");Pointer(ui.navigation[2],"safe-area shelter");var safeCorners=new Vector3[4];ui.layout.core.GetWorldCorners(safeCorners);Check(safeCorners.All(p=>p.y>=59&&p.y<=Screen.height-49),"notch and home indicator respected");ui.CloseDrawer();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();ui.layout.SetValidationSafeArea(null);
        StorybookEquipmentValidation.Resolution(941,1672);yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();Fixture();var skills=SkillsController.Instance;
        ui.Select(N(1));yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();gm.skillPoints=0;ui.Refresh();Check(!ui.purchase.interactable&&ui.purchaseCaption.text=="技能点不足","zero points disables upgrade");ui.Buy();Check(skills.regenLevel==1&&gm.skillPoints==0,"disabled purchase cannot spend points");
        gm.skillPoints=1;ui.Refresh();Check(ui.purchase.interactable,"point replenishment restores button");ui.purchase.onClick.Invoke();Check(skills.regenLevel==2&&gm.skillPoints==0,"purchase increments real skill and spends exactly one point");
        skills.regenLevel=9;gm.skillPoints=1;ui.Refresh();Check(!ui.purchase.interactable&&ui.Cost==2&&!N(1).available.activeSelf,"9 to 10 requires two points and hides bookmark with one point");gm.skillPoints=2;ui.Refresh();Check(N(1).available.activeSelf,"two points restores level9 upgrade bookmark");ui.Buy();Check(skills.regenLevel==10&&gm.skillPoints==0&&!ui.purchase.interactable&&!N(1).available.activeSelf,"level10 cap and exact two-point spend hides bookmark");ui.Buy();Check(skills.regenLevel==10&&gm.skillPoints==0&&!ui.nextValues.transform.parent.gameObject.activeSelf&&ui.currentValues.transform.parent.gameObject.activeSelf,"max level retains current values but cannot preview level11");yield return new WaitForEndOfFrame();Shot("max-level");
        Fixture();Check(N(6).IsUnlocked(skills)&&!N(5).IsUnlocked(skills),"angel alone unlocks resurrection without devil");ui.Select(N(6));yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();ui.Buy();Check(skills.ressurrectionLevel==1&&N(5).button.interactable&&N(9).button.interactable,"resurrection purchase immediately unlocks reverse devil and dragon paths");
        skills.ResetSkills();skills.devilOnMyShoulderLevel=1;ui.Refresh();Check(N(6).IsUnlocked(skills),"devil alone also unlocks resurrection");Check(ui.prerequisiteIcons[0].sprite==ui.prerequisiteMet&&ui.prerequisiteIcons[1].sprite==ui.prerequisiteUnmet,"prerequisite sprites swap when fulfilled route changes");skills.ResetSkills();ui.Refresh();Check(!N(6).IsUnlocked(skills)&&!N(6).button.interactable,"neither prerequisite keeps resurrection locked");int before=gm.skillPoints;ui.Buy();Check(skills.ressurrectionLevel==0&&gm.skillPoints==before,"stale detail cannot bypass prerequisite check");
        Fixture();skills.sinisterStampedeLevel=1;skills.devilOnMyShoulderLevel=1;ui.Refresh();ui.Select(N(5));yield return new WaitForSecondsRealtime(.55f);yield return WaitForMotion();yield return new WaitForEndOfFrame();Shot("multi-effect-drawer");Check(ui.currentValues.text.Contains("暴伤")&&ui.nextValues.text.Contains("暴击"),"all four devil effects included");Pointer(ui.purchase,"long-content action");
        ui.CloseDrawer();yield return new WaitForSecondsRealtime(.3f);yield return WaitForMotion();ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&!ui.gameObject.activeSelf,"return to shelter preserved");
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);ui.navigation[0].onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.INVENTORY&&gc.inventoryPanel.activeSelf&&!ui.gameObject.activeSelf,"skills to equipment navigation");
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);ui.navigation[3].onClick.Invoke();yield return new WaitForSecondsRealtime(3);Check(LevelController.Instance.AssetManager.mapPackageObj.activeSelf&&!ui.gameObject.activeSelf,"skills to map navigation");
        Router.single.SkillsButton();yield return new WaitForSecondsRealtime(2);ui.navigation[4].onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(!ui.gameObject.activeSelf,"skills to shop navigation");
        Check(errors==0,"no unexpected runtime errors");Finish();
    }
    void Finish(){Application.logMessageReceived-=Error;StorybookSkillsValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
