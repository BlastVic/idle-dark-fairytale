using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentCompactComparisonValidation
{
    const string Out = "output/equipment-compact-comparison/";
    static bool running;
    static EquipmentCompactComparisonValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("COMPACT_COMPARISON_QA", false)) return;
            SessionState.SetBool("COMPACT_COMPARISON_QA", false);
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
            SessionState.SetBool("COMPACT_COMPARISON_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("COMPACT_COMPARISON_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static void Check(bool ok,string message)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");
    static IEnumerator Run()
    {
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        var developer=Object.FindFirstObjectByType<DeveloperOverlayUI>();if(developer)developer.gameObject.SetActive(false);
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        var ui=GameplayCanvas.single.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        var fixture=new Item{uniqueId=9900334,m_ItemType=ItemType.WEAPON,m_ItemFinalTitle="测试猎刀·非常长的装备名称测试单行省略",m_IconName="Wep1",baseStat=new Stat{dmg=18,crit=5,atkSpd=8,hpMax=24,def=3},rollStat=new RollStat()};
        InventoryManager.single.weaponsOwned.Add(fixture);ui.SetCategory(ItemType.WEAPON);ui.SetDisplayMode(true);ui.SetSort(1);ui.Select(fixture);
        var current=ui.Equipped(ItemType.WEAPON);
        var original=current.baseStat;
        current.baseStat=new Stat{dmg=12,crit=8,atkSpd=8,hpMax=10,def=6};
        ui.RefreshSelection();
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.5f);
            ui.RefreshSelection();yield return new WaitForEndOfFrame();
            foreach(var label in new[]{ui.equippedName,ui.selectedName}) {
                Check(label.preferredWidth<=label.rectTransform.rect.width+1,"Single-line title fits "+size);
                Check(label.horizontalOverflow==UnityEngine.HorizontalWrapMode.Overflow,"No wrapping "+size);
            }
            Check(ui.layout.detailHeight==325,"Compact five-stat panel "+size);
            Check(ui.layout.detailContent.Find("ReplaceDirection"),"Replacement arrow saved in prefab");
            var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
        }
        fixture.baseStat=new Stat{dmg=2};current.baseStat=new Stat{dmg=1};ui.RefreshSelection();
        yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
        Check(ui.layout.detailHeight==214,"Compact minimum two-row height");
        var shortShot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"two-rows.png",shortShot.EncodeToPNG());Object.Destroy(shortShot);
        current.baseStat=original;
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
