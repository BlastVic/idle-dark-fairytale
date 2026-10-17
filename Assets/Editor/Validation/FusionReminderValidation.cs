using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FusionReminderValidation
{
    const string Out = "output/fusion-reminder/";
    static bool running;
    static FusionReminderValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("FUSION_REMINDER_QA", false)) return;
            SessionState.SetBool("FUSION_REMINDER_QA", false);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
            running = false;
        };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Out + "validate.request")) {
            File.Delete(Out + "validate.request");
            File.WriteAllText(Out + "validation.txt", "");
            SessionState.SetBool("FUSION_REMINDER_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("FUSION_REMINDER_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static void Check(bool ok,string message)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");
    static IEnumerator Run()
    {
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        var gm=GameManager.single;var gc=GameplayCanvas.single;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        for(int i=0;i<9;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        gc.dailyCherriesPop.SetActive(false);gc.CancelInvoke("TryShowingXpBoosterNotification");
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.inventoryPanel.GetComponent<StorybookEquipmentUI>();var reminder=ui.fusionReminder;
        Check(reminder&&!reminder.Presenting&&!ui.layout.fusion,"No permanent fusion rail or startup popup");
        var item=new Item{uniqueId=9988123,m_ItemType=ItemType.WEAPON,m_ItemFinalTitle="测试猎刀",m_IconName="Wep1",baseStat=new Stat{dmg=12},rollStat=new RollStat(),fusionExp=10};
        InventoryManager.single.weaponsOwned.Add(item);ui.SetCategory(ItemType.WEAPON);ui.Select(item);
        gm.fxpMax=100;gm.fxpNow=25;
        ui.RequestDecompose();ui.modalConfirm.onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);
        Check(reminder.Presenting&&gm.fxpNow==35&&!InventoryManager.single.weaponsOwned.Contains(item),"Actual decomposition grants XP and opens reminder");
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            var corners=new Vector3[4];reminder.card.GetWorldCorners(corners);bool inside=true;
            foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(null,corner);inside&=p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height;}
            Check(inside&&reminder.Presenting&&(corners[2]-corners[0]).magnitude>100,"Reminder visible size and inside screen "+size);
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,reminder.open.transform.position)},hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==reminder.open,"Fusion button receives pointer "+size);
            var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
        }
        reminder.close.onClick.Invoke();Check(!reminder.Presenting&&!reminder.group.blocksRaycasts,"Close releases pointer blocking");
        reminder.lifetime=.5f;reminder.Notify(5);yield return new WaitForSecondsRealtime(.7f);Check(!reminder.Presenting&&!reminder.group.blocksRaycasts,"Auto dismiss releases pointer blocking");
        reminder.lifetime=8;ui.modal.SetActive(true);reminder.Notify(5);yield return null;Check(!reminder.Presenting,"Confirmation overlay defers reminder");ui.CloseOverlays();yield return new WaitForSecondsRealtime(.3f);Check(reminder.Presenting,"Deferred notification resumes");
        reminder.Notify(7);yield return null;Check(reminder.heading.text.Contains("12"),"Repeated gains merge into same card");
        ui.gridScroll.StopMovement();ui.gridScroll.content.anchoredPosition=new Vector2(0,150);
        var category=ui.Category;int? selected=ui.Selected?.uniqueId;
        float scroll=ui.gridScroll.content.anchoredPosition.y;
        reminder.open.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(FusionPackage.single&&!reminder.gameObject.activeInHierarchy,"Go to fusion opens current fusion UI and hides equipment popup");
        FusionPackage.single.view.Close();yield return new WaitForSecondsRealtime(2.5f);
        Check(ui.gameObject.activeInHierarchy&&GameManager.single.gameState==Assets.Scripts.Game.Enum.GameStateType.INVENTORY,"Fusion returns to equipment instead of shelter");
        Check(ui.Category==category&&ui.Selected?.uniqueId==selected&&Mathf.Abs(ui.gridScroll.content.anchoredPosition.y-scroll)<1,"Equipment category selection and scroll restored");
        Router.single.CampButton();yield return new WaitForSecondsRealtime(2);
        Router.single.CallFusionFromCamp();yield return new WaitForSecondsRealtime(2);
        Check(!Router.single.FusionReturnsToEquipment,"Shelter entry clears old equipment destination");
        FusionPackage.single.view.Close();yield return new WaitForSecondsRealtime(2);
        Check(GameManager.single.gameState==Assets.Scripts.Game.Enum.GameStateType.CAMP&&gc.homeUI.gameObject.activeInHierarchy,"Shelter fusion still returns to shelter");
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
