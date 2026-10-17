using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentDeltaValidation
{
    const string Out = "output/equipment-delta/";
    static bool running;
    static EquipmentDeltaValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("DELTA_STYLE_QA", false)) return;
            SessionState.SetBool("DELTA_STYLE_QA", false);
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
            SessionState.SetBool("DELTA_STYLE_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("DELTA_STYLE_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
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
        var current=ui.Equipped(ItemType.WEAPON);
        var original=current.baseStat;
        current.baseStat=new Stat{dmg=12,crit=8,atkSpd=8,hpMax=10,def=6};
        ui.RefreshSelection();
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.5f);
            ui.RefreshSelection();yield return new WaitForEndOfFrame();
            var up=ui.comparisonRows[1];var down=ui.comparisonRows[4];var same=ui.comparisonRows[3];
            Check(up.deltaArrow.gameObject.activeSelf && up.delta.color.r>up.delta.color.g && up.deltaArrow.color==up.delta.color && Mathf.Abs(up.deltaArrow.rectTransform.localEulerAngles.z)<.1f,"Increase: solid up arrow + red matching value "+size);
            Check(down.deltaArrow.gameObject.activeSelf && down.delta.color.g>down.delta.color.r && down.deltaArrow.color==down.delta.color && Mathf.Abs(down.deltaArrow.rectTransform.localEulerAngles.z-180)<.1f,"Decrease: solid down arrow + green matching value "+size);
            Check(!same.deltaArrow.gameObject.activeSelf && same.delta.text=="—","Unchanged: no arrow "+size);
            foreach(var row in ui.comparisonRows)if(row.root.gameObject.activeSelf){
                var local=row.root.InverseTransformPoint(row.deltaArrow.transform.TransformPoint(row.deltaArrow.rectTransform.rect.center));
                Check(Mathf.Abs(local.y+18.5f)<1,"Arrow centered in its row "+size);
                Check(row.delta.fontStyle==UnityEngine.FontStyle.Bold && row.delta.preferredWidth<=row.delta.rectTransform.rect.width,"Bold value fits "+row.delta.text+" "+size);
            }
            var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
        }
        current.baseStat=original;
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
