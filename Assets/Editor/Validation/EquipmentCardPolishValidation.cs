using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentCardPolishValidation
{
    const string Out = "output/equipment-card-polish/";
    static bool running;
    static EquipmentCardPolishValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("CARD_POLISH_QA", false)) return;
            SessionState.SetBool("CARD_POLISH_QA", false);
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
            SessionState.SetBool("CARD_POLISH_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("CARD_POLISH_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
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
                var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+"-"+columns+".png",shot.EncodeToPNG());Object.Destroy(shot);
            }
        }
        if(had)PlayerPrefs.SetInt(StorybookEquipmentUI.ColumnsPreference,previous);else PlayerPrefs.DeleteKey(StorybookEquipmentUI.ColumnsPreference);PlayerPrefs.Save();
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
