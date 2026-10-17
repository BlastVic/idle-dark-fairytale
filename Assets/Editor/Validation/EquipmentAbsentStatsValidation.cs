using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentAbsentStatsValidation
{
    const string Out = "output/equipment-absent-stats/";
    static bool running;
    static EquipmentAbsentStatsValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("ABSENT_STATS_QA", false)) return;
            SessionState.SetBool("ABSENT_STATS_QA", false);
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
            SessionState.SetBool("ABSENT_STATS_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("ABSENT_STATS_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
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
        if(current==null){current=new Item{uniqueId=9900335,m_ItemType=ItemType.WEAPON,m_ItemFinalTitle="Useless Dust Club",m_IconName="Wep1",baseStat=new Stat{dmg=3},rollStat=new RollStat()};InventoryManager.single.hasWeapon=true;InventoryManager.single.weaponEq=current;}
        var original=current.baseStat;var originalName=current.m_ItemFinalTitle;var originalGrade=current.itemGrade;
        current.m_ItemFinalTitle="Useless Dust Club";current.itemGrade=ItemGrade.ORDINARY;
        current.baseStat=new Stat{dmg=12,crit=8,atkSpd=8,hpMax=10,def=6};
        ui.RefreshSelection();
        foreach(int statCount in new[]{5,2}){
        fixture.m_ItemFinalTitle=statCount==5?"五属性测试武器 1":"Wanderer Club";
        fixture.m_IconName=current.m_IconName;fixture.itemGrade=statCount==5?ItemGrade.LEGENDARY:ItemGrade.ORDINARY;fixture.isLocked=true;
        fixture.baseStat=statCount==5?new Stat{dmg=8,crit=3,atkSpd=5,hpMax=10,def=2}:new Stat{dmg=1,hpMax=1};
        current.baseStat=statCount==5?new Stat{dmg=3}:new Stat{dmg=12,crit=5,atkSpd=8,hpMax=15,def=3};
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.5f);
            ui.RefreshSelection();yield return new WaitForEndOfFrame();
            foreach(var label in new[]{ui.equippedName,ui.selectedName}) {
                Check(label.preferredWidth<=label.rectTransform.rect.width+1,"Single-line quality/title fits "+size);
                Check(label.text.Contains(" · "),"Quality and title combined");
            }
            Check(ui.layout.detailHeight==303,"Adaptive height "+statCount+" stats "+size);
            Check(ui.layout.detailContent.Find("ReplacementDivider").GetComponent<EquipmentReplacementDivider>(),"Continuous notched divider");
            Check(!ui.layout.detailContent.Find("ReplaceDirection"),"Standalone arrow removed");
            Check(ui.selectedIcon.transform.parent.parent==ui.comparisonScroll.viewport,"Weapon behind attribute rows");
            Check(!ui.grade.gameObject.activeSelf&&!ui.equippedGrade.gameObject.activeSelf,"Separate quality rows removed");
            var absent=ui.comparisonRows[4];int missingColumn=statCount==5?0:1;
            Check(!absent.root.Find("Label"+missingColumn).gameObject.activeSelf&&!absent.root.Find("StatIcon"+missingColumn).gameObject.activeSelf,"Absent stat label/icon hidden");
            Check((missingColumn==0?absent.current:absent.selected).text=="","Absent value blank");
            Check(absent.root.Find("Label"+(1-missingColumn)).gameObject.activeSelf,"Present stat restored when comparison changes");
            Check(absent.delta.text==(statCount==5?"+3%":"-5%"),"Gained/lost stat delta retained");
            var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+statCount+"stats-"+size.x+"x"+size.y+".png",shot.EncodeToPNG());
            var corners=new Vector3[4];ui.layout.detail.GetWorldCorners(corners);
            var low=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var high=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            int x=Mathf.Clamp(Mathf.FloorToInt(low.x),0,shot.width-1),y=Mathf.Clamp(Mathf.FloorToInt(low.y),0,shot.height-1);
            int w=Mathf.Clamp(Mathf.CeilToInt(high.x)-x,1,shot.width-x),h=Mathf.Clamp(Mathf.CeilToInt(high.y)-y,1,shot.height-y);
            var crop=new Texture2D(w,h,TextureFormat.RGB24,false);crop.SetPixels(shot.GetPixels(x,y,w,h));crop.Apply();
            File.WriteAllBytes(Out+statCount+"stats-panel-"+size.x+"x"+size.y+".png",crop.EncodeToPNG());Object.Destroy(crop);Object.Destroy(shot);
        }
        }
        fixture.m_ItemFinalTitle="非常非常长的装备名称用于单行省略验证 Long Equipment Name";ui.RefreshSelection();yield return null;
        Check(ui.selectedName.text.EndsWith("…"),"Long title ellipsis");
        ui.Rebuild();ui.Select(current);Check(!ui.equip.interactable&&!ui.decompose.interactable,"Equipped item actions disabled");
        fixture.isLocked=false;ui.Select(fixture);Check(ui.equip.interactable&&ui.decompose.interactable,"Unlocked target actions available");
        fixture.isLocked=true;ui.RefreshSelection();Check(ui.equip.interactable&&!ui.decompose.interactable,"Locked target cannot decompose");
        current.baseStat=original;current.m_ItemFinalTitle=originalName;current.itemGrade=originalGrade;
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
