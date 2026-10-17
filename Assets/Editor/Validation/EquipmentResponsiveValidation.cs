using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentResponsiveValidation
{
    const string Out = "output/equipment-responsive/";
    static bool running;
    static EquipmentResponsiveValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("RESPONSIVE_EQUIPMENT_QA", false)) return;
            SessionState.SetBool("RESPONSIVE_EQUIPMENT_QA", false);
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
            SessionState.SetBool("RESPONSIVE_EQUIPMENT_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("RESPONSIVE_EQUIPMENT_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
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
                foreach(var pair in new[]{new[]{layout.categories,layout.categoryContent},new[]{layout.backpack,layout.toolbar},new[]{layout.detail,layout.detailContent},new[]{layout.attributes,layout.attributeContent}})
                    Check(Mathf.Abs(pair[0].rect.width-pair[1].rect.width*pair[1].localScale.x)<.1f,"Contents fills panel: "+pair[0].name+" "+size);
                Check(Mathf.Abs(ui.grid.cellSize.x*columns+ui.grid.spacing.x*(columns-1)-layout.gridViewport.rect.width)<.1f,"Grid fills available width "+size);
                Check(((RectTransform)ui.batchButton.transform).anchorMin.x==1&&((RectTransform)ui.detailedModeButton.transform).anchorMin.x==1,"Toolbar controls track right edge "+size);
                foreach(var row in ui.comparisonRows)if(row.root.gameObject.activeSelf)
                    Check(Mathf.Abs(row.root.rect.width-ui.comparisonScroll.viewport.rect.width)<.1f,"Comparison row fills viewport "+size);
                foreach(var name in new[]{"CurrentHeading","TargetHeading","DeltaHeading","SelectedName"}){
                    var t=layout.detailContent.Find(name).GetComponent<UnityEngine.UI.Text>();
                    File.AppendAllText(Out+"validation.txt",name+" rect="+t.rectTransform.rect+" preferred="+t.preferredHeight+" cull="+t.canvasRenderer.cull+" alpha="+t.color.a+" vertices="+t.cachedTextGenerator.vertexCount+"\n");
                }
                var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+"-"+columns+".png",shot.EncodeToPNG());Object.Destroy(shot);
            }
            ui.foldButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
            Check(ui.attributeBody.interactable&&ui.attributeBody.blocksRaycasts,"Expanded attributes receive clicks "+size);
            foreach(var b in ui.statButtons){
                var rect=(RectTransform)b.transform;var bounds=new Vector3[4];rect.GetWorldCorners(bounds);
                Check(System.Array.TrueForAll(bounds,p=>{var local=layout.attributes.InverseTransformPoint(p);return layout.attributes.rect.Contains(local);}),"Stat button within full-width attributes "+size);
            }
            yield return new WaitForEndOfFrame();var expanded=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+"-expanded.png",expanded.EncodeToPNG());Object.Destroy(expanded);
            ui.foldButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
            Check(!ui.attributeBody.gameObject.activeSelf&&!ui.attributeBody.blocksRaycasts,"Collapsed attributes do not block clicks "+size);
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
