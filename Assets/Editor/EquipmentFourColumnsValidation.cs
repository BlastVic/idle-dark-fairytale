using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentFourColumnsValidation
{
    const string Out = "output/equipment-four-columns/";
    static bool running;
    static EquipmentFourColumnsValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("FOUR_COL_QA", false)) return;
            SessionState.SetBool("FOUR_COL_QA", false);
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
            SessionState.SetBool("FOUR_COL_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("FOUR_COL_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static void Check(bool ok,string message)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");
    static IEnumerator Run()
    {
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        var ui=GameplayCanvas.single.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        var fixture=new Item{uniqueId=9900334,m_ItemType=ItemType.WEAPON,m_ItemFinalTitle="测试猎刀",m_IconName="Wep1",baseStat=new Stat{dmg=18,crit=5,atkSpd=8,hpMax=24,def=3},rollStat=new RollStat()};
        InventoryManager.single.weaponsOwned.Add(fixture);ui.SetCategory(ItemType.WEAPON);ui.SetDisplayMode(true);ui.SetSort(1);ui.Select(fixture);
        bool had=PlayerPrefs.HasKey(StorybookEquipmentUI.ColumnsPreference);int previous=PlayerPrefs.GetInt(StorybookEquipmentUI.ColumnsPreference,4);
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);ui.SetColumns(4);yield return null;
            var layout=ui.layout;float density=layout.Density;var header=layout.header.rect.size;var detail=layout.detail.rect.size;var position=layout.detail.anchoredPosition;var viewport=layout.gridViewport.rect.size;
            foreach(int columns in new[]{3,5,4}){
                ui.columnButtons[columns-3].onClick.Invoke();yield return new WaitForEndOfFrame();
                Check(Mathf.Abs(layout.Density-density)<.0001f&&layout.header.rect.size==header&&layout.detail.rect.size==detail&&layout.detail.anchoredPosition==position&&layout.gridViewport.rect.size==viewport,"Surrounding layout unchanged: "+columns+" columns "+size);
                var button=(RectTransform)ui.columnButtons[columns-3].transform;Check(Mathf.Abs(button.rect.width-button.rect.height)<.01f,"Square column button "+columns);
                var cell=System.Linq.Enumerable.First(ui.Cells,c=>c.Item==fixture);
                Check(cell.detailLabels.preferredHeight<=cell.detailLabels.rectTransform.rect.height&&cell.detailValues.preferredHeight<=cell.detailValues.rectTransform.rect.height,"Five attributes fit "+columns+" "+size);
                foreach(var action in new[]{ui.equip,ui.decompose,ui.lockButton}){
                    var corners=new Vector3[4];((RectTransform)action.transform).GetWorldCorners(corners);bool inPanel=true;
                    foreach(var corner in corners){var local=layout.detail.InverseTransformPoint(corner);inPanel&=layout.detail.rect.Contains(local);}
                    Check(inPanel,"Action within panel "+action.name+" "+size);
                    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,action.transform.TransformPoint(((RectTransform)action.transform).rect.center))},hits);
                    Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==action,"Action receives pointer "+action.name);
                }
                var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+"-"+columns+".png",shot.EncodeToPNG());Object.Destroy(shot);
            }
        }
        fixture.baseStat=new Stat{dmg=18,atkSpd=8};ui.RefreshSelection();yield return new WaitForEndOfFrame();
        Check(ui.layout.detailHeight<=266.1f,"Two-row comparison shrinks to 266 design units");
        foreach(var action in new[]{ui.equip,ui.decompose,ui.lockButton}){var corners=new Vector3[4];((RectTransform)action.transform).GetWorldCorners(corners);Check(System.Array.TrueForAll(corners,c=>ui.layout.detail.rect.Contains(ui.layout.detail.InverseTransformPoint(c))),"Compact action remains inside panel "+action.name);}
        var compact=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"compact-two-rows.png",compact.EncodeToPNG());Object.Destroy(compact);
        ui.lockButton.onClick.Invoke();Check(fixture.isLocked&&!ui.decompose.interactable,"Lock disables decomposition");ui.lockButton.onClick.Invoke();Check(!fixture.isLocked&&ui.decompose.interactable,"Unlock restores decomposition");
        ui.equip.onClick.Invoke();Check(ui.Equipped(ItemType.WEAPON).uniqueId==fixture.uniqueId&&!ui.equip.interactable,"Fourth-column equip updates equipped state");
        if(had)PlayerPrefs.SetInt(StorybookEquipmentUI.ColumnsPreference,previous);else PlayerPrefs.DeleteKey(StorybookEquipmentUI.ColumnsPreference);PlayerPrefs.Save();
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
