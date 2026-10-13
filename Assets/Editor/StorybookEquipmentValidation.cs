using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class StorybookEquipmentValidation
{
    public const string Dir="output/equipment-ui/";
    static bool running;
    static double deadline;
    static StorybookEquipmentValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("EQUIPMENT_QA",false)){SessionState.SetBool("EQUIPMENT_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);if(SessionState.GetBool("EQUIPMENT_QA_LOCKED",false)){EditorApplication.UnlockReloadAssemblies();SessionState.SetBool("EQUIPMENT_QA_LOCKED",false);}running=false;}};}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"restore-columns.request")){int value;if(int.TryParse(File.ReadAllText(Dir+"restore-columns.request"),out value)){UnityEngine.PlayerPrefs.SetInt(StorybookEquipmentUI.ColumnsPreference,Mathf.Clamp(value,3,5));UnityEngine.PlayerPrefs.Save();}File.Delete(Dir+"restore-columns.request");}
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Equipment actual Unity gameplay verification; test save persistence disabled\n");
            SessionState.SetBool("EQUIPMENT_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("EQUIPMENT_QA_LOCKED",true);EditorApplication.LockReloadAssemblies();running=false;deadline=EditorApplication.timeSinceStartup+240;Resolution(941,1672);EditorApplication.isPlaying=true;
        }
        if(SessionState.GetBool("EQUIPMENT_QA",false)&&EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+240;GameManager.single.StartCoroutine(new EquipmentProbe().Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog timeout");EditorApplication.isPlaying=false;}
    }
    public static void Resolution(int w,int h)=>typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});
    public static void Log(string value)=>File.AppendAllText(Dir+"validation.txt",value+"\n");
}
public sealed class EquipmentProbe
{
    int failures,errors;
    bool hadColumns;int savedColumns;
    StorybookEquipmentUI ui;
    void Check(bool value,string text){StorybookEquipmentValidation.Log((value?"PASS ":"FAIL ")+text);if(!value)failures++;}
    void OnLog(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;StorybookEquipmentValidation.Log("ERROR "+m);}}
    void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookEquipmentValidation.Dir+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    void Pointer(Button b,string name){var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Check(p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,name+" pointer unobstructed");}
    public IEnumerator Run()
    {
        hadColumns=UnityEngine.PlayerPrefs.HasKey(StorybookEquipmentUI.ColumnsPreference);savedColumns=UnityEngine.PlayerPrefs.GetInt(StorybookEquipmentUI.ColumnsPreference,4);
        Application.logMessageReceived+=OnLog;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        ui=GameplayCanvas.single.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        Check(ui&&ui.isActiveAndEnabled,"equipment route installs and opens editable prefab");if(!ui){Finish();yield break;}
        yield return new WaitForEndOfFrame();Shot("9x16-current-save");
        var im=InventoryManager.single;var gm=GameManager.single;
        Item template=ui.Equipped(ItemType.WEAPON)??im.weaponsOwned.FirstOrDefault();
        if(template==null)template=new Item{m_ItemType=ItemType.WEAPON,m_ItemFinalTitle="旧猎刀",m_IconName="Wep1",baseStat=new Stat{dmg=12},rollStat=new RollStat()};
        im.weaponsOwned.Clear();im.hasWeapon=true;im.weaponEq=Utilities.DeepClone(template);im.weaponEq.uniqueId=880000;im.weaponEq.baseStat=new Stat{dmg=12,crit=5,atkSpd=100};im.weaponEq.m_ItemFinalTitle="旧猎刀";im.weaponEq.itemGrade=ItemGrade.ORDINARY;im.weaponEq.isLocked=false;
        var icons=im.m_ItemIcons.Where(p=>p.sprite&&p.spriteName.StartsWith("Wep")).ToArray();
        for(int i=0;i<23;i++){var item=Utilities.DeepClone(template);item.uniqueId=880001+i;item.isLocked=i==5;item.m_ItemFinalTitle=i==0?"荆棘猎刀":"测试装备 "+(i+1);item.baseStat=new Stat{dmg=18+i,crit=8,atkSpd=100};item.itemGrade=(ItemGrade)(i%5);item.fusionExp=3+i;if(icons.Length>0)item.m_IconName=icons[i%icons.Length].spriteName;im.weaponsOwned.Add(item);}
        gm.statPoints=3;gm.CalcPlayer();ui.SetCategory(ItemType.WEAPON);ui.Select(im.weaponsOwned[0]);
        Check(ui.Columns==(hadColumns?Mathf.Clamp(savedColumns,3,5):4),"restores saved columns or defaults to four");
        var choice=ui.Selected;
        foreach(var size in new[]{new Vector2Int(941,1672),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);
            float lastWidth=float.MaxValue;
            for(int columns=3;columns<=5;columns++){
                ui.columnButtons[columns-3].onClick.Invoke();yield return new WaitForEndOfFrame();
                Check(ui.grid.constraintCount==columns&&ui.Selected==choice&&ui.grid.cellSize.x<lastWidth,"column selection keeps item and scales card "+columns+" "+size);
                Check(UnityEngine.PlayerPrefs.GetInt(StorybookEquipmentUI.ColumnsPreference)==columns,"column preference persisted "+columns);
                foreach(var b in ui.columnButtons)Pointer(b,"columns "+b.name);
                Shot("columns-"+columns+"-"+size.x+"x"+size.y);lastWidth=ui.grid.cellSize.x;
            }
        }
        ui.SetColumns(5);
        // Reference fixture for prior design is retained as a utility; new layout is validated with actual item art.
        var selectedStats=ui.Selected.baseStat;var equippedStats=im.weaponEq.baseStat;
        ui.Selected.baseStat=new Stat{dmg=6};im.weaponEq.baseStat=new Stat{dmg=58,atkSpd=13};
        foreach(var size in new[]{new Vector2Int(941,1672),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);
            ui.RefreshSelection();yield return new WaitForEndOfFrame();
            Check(Mathf.Abs(ui.layout.detailHeight-403)<1,"two-row comparison reclaims unused third row "+size);
            Pointer(ui.equip,"compact details equip");Pointer(ui.decompose,"compact details decompose");Shot("two-row-"+size.x+"x"+size.y);
        }
        ui.Selected.baseStat=selectedStats;im.weaponEq.baseStat=equippedStats;ui.RefreshSelection();
        Check(Mathf.Abs(ui.layout.detailHeight-440)<1,"three-row comparison restores authored height");
        int[] widths={941,768,720},heights={1672,1024,1440};string[] ratios={"9x16","3x4","1x2"};
        for(int ratio=0;ratio<3;ratio++){
            StorybookEquipmentValidation.Resolution(widths[ratio],heights[ratio]);yield return new WaitForSecondsRealtime(.4f);ui.layout.Apply();ui.gridScroll.verticalNormalizedPosition=1;
            if(ui.attributesExpanded)ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            Shot(ratios[ratio]+"-collapsed");
            var modeSelection=ui.Selected;float scrollRow=ui.gridScroll.content.anchoredPosition.y/(ui.grid.cellSize.y+ui.grid.spacing.y);
            ui.compactModeButton.onClick.Invoke();yield return new WaitForEndOfFrame();Shot(ratios[ratio]+"-compact");
            Check(!ui.detailedMode&&ui.Cells[0].primary.gameObject.activeSelf&&!ui.Cells[0].detailedBody.activeSelf,ratios[ratio]+" compact hides overlay and shows centered primary");
            ui.detailedModeButton.onClick.Invoke();yield return new WaitForEndOfFrame();
            Check(ui.detailedMode&&ui.Selected==modeSelection&&ui.grid.constraintCount==5&&ui.Cells[0].detailedBody.activeSelf,ratios[ratio]+" detailed mode keeps five columns and selection");
            Check(ui.Cells.Where(c=>c.gameObject.activeSelf).All(c=>c.detailLabels.preferredHeight<=c.detailLabels.rectTransform.rect.height+1&&c.detailValues.preferredHeight<=c.detailValues.rectTransform.rect.height+1),ratios[ratio]+" card attribute text fits overlay");
            Check(ui.comparisonRows[1].current.text=="12"&&ui.comparisonRows[1].selected.text=="18"&&ui.comparisonRows[1].delta.text.Contains("+6"),"three columns bind equipped target and delta");
            Check(!ui.previous.gameObject.activeSelf&&!ui.next.gameObject.activeSelf&&!ui.title.gameObject.activeSelf,"removed title band stays hidden");
            float before=ui.layout.gridViewport.rect.height;var selected=ui.Selected;
            foreach(var b in new[]{ui.equip,ui.decompose,ui.foldButton,ui.sortButton,ui.filterButton,ui.batchButton,ui.back,ui.compactModeButton,ui.detailedModeButton}.Concat(ui.navigationButtons).Concat(ui.categoryButtons))Pointer(b,ratios[ratio]+" "+b.name);
            ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.08f);float intermediate=ui.layout.gridViewport.rect.height;
            Check(intermediate<before&&intermediate>before-(ui.layout.expandedHeight-ui.layout.collapsedHeight)*ui.layout.Density-1,ratios[ratio]+" accordion visibly animates grid height");
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot(ratios[ratio]+"-expanded");
            Check(ui.Selected==selected,ratios[ratio]+" fold preserves selected item");
            Check(ui.layout.gridViewport.rect.height+1>=ui.grid.cellSize.y,ratios[ratio]+" expanded grid retains at least one full row");
            Check(ui.attributeBody.blocksRaycasts&&ui.attributeBody.interactable,ratios[ratio]+" expanded attributes receive input");
            foreach(var b in ui.statButtons)Pointer(b,ratios[ratio]+" "+b.transform.parent.name);
            Check(ui.layout.gridViewport.rect.height<before,ratios[ratio]+" grid absorbs attribute height");
            var e=new PointerEventData(EventSystem.current);var feedback=ui.equip.GetComponent<ShelterButtonFeedback>();var original=ui.equip.transform.localScale;feedback.OnPointerDown(e);Check(ui.equip.transform.localScale.x<original.x,ratios[ratio]+" button presses");feedback.OnPointerUp(e);Check(ui.equip.transform.localScale==original,ratios[ratio]+" button restores");
            ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);Check(!ui.attributeBody.gameObject.activeSelf&&!ui.attributeBody.blocksRaycasts,ratios[ratio]+" collapsed body does not block rays");
        }
        ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);
        ui.layout.SetValidationSafeArea(new Rect(0,60,Screen.width,Screen.height-110));yield return new WaitForEndOfFrame();Shot("1x2-safe-area-expanded");
        foreach(var b in new[]{ui.back,ui.equip,ui.foldButton}.Concat(ui.navigationButtons).Concat(ui.statButtons))Pointer(b,"safe-area "+b.name);
        var bounds=new Vector3[4];ui.layout.core.GetWorldCorners(bounds);Check(bounds.All(p=>p.y>=59&&p.y<=Screen.height-49),"notch and home indicator safe-area respected");
        ui.layout.SetValidationSafeArea(null);ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);
        StorybookEquipmentValidation.Resolution(941,1672);yield return new WaitForSecondsRealtime(.3f);
        // Browse without equipping, then use actual event-handler swipe path.
        ui.Select(im.weaponsOwned[0]);var equippedId=im.weaponEq.uniqueId;var first=ui.Selected;
        var swipe=ui.layout.detailContent.GetComponent<StorybookEquipmentSwipe>();var ev=new PointerEventData(EventSystem.current){pressPosition=new Vector2(650,500),position=new Vector2(620,500)};swipe.OnBeginDrag(ev);ev.position=new Vector2(400,500);swipe.OnDrag(ev);swipe.OnEndDrag(ev);
        Check(ui.Selected!=first&&im.weaponEq.uniqueId==equippedId,"horizontal swipe selects next without equipping");ui.Step(-1);Check(ui.Selected==first,"previous restores neighboring selection");
        ui.gridScroll.content.anchoredPosition=new Vector2(0,400);ui.SetCategory(ItemType.SHIELD);ui.SetCategory(ItemType.WEAPON);yield return null;Check(ui.Selected==first&&Mathf.Abs(ui.gridScroll.content.anchoredPosition.y-400)<2,"per-category scroll and selection remembered");
        ui.SetSort(2);Check(ui.Selected==first,"sorting preserves selection");ui.SetFilter(1);Check(!ui.VisibleItems.Contains(im.weaponEq),"unequipped filter");ui.SetFilter(2);Check(ui.VisibleItems.All(i=>!i.isLocked),"unlocked filter");ui.SetFilter(0);ui.SetSort(0);
        ui.Select(first);ui.ToggleLock();Check(first.isLocked&&!ui.decompose.interactable,"locked item cannot be decomposed");Check(Utilities.DeepClone(first).isLocked,"item lock survives save-compatible serialization");ui.ToggleLock();
        ui.Select(first);ui.EquipSelected();Check(im.weaponEq.uniqueId==first.uniqueId&&!im.weaponsOwned.Any(i=>i.uniqueId==first.uniqueId)&&im.weaponsOwned.Any(i=>i.uniqueId==equippedId),"equip exchanges previous item without loss or duplication");Check(!ui.equip.interactable&&!ui.decompose.interactable,"equipped selection disables destructive/redundant actions");
        ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);gm.statPoints=1;ui.RefreshStats();int level=gm.hpLevel;ui.BuyStat(0);Check(gm.hpLevel==level+1&&gm.statPoints==0&&ui.statButtons.All(b=>!b.interactable),"last stat point updates values and disables all plus buttons");yield return new WaitForEndOfFrame();Shot("attributes-no-points");gm.statPoints=3;ui.RefreshStats();Check(ui.statButtons.Any(b=>b.interactable)&&ui.statButtons.Select((b,i)=>b.interactable==ui.CanBuy(i)).All(b=>b),"stat buttons restore according to point costs and original consecutive-stat restriction");
        var many=im.weaponsOwned[1];many.baseStat=new Stat{hpMax=5,dmg=20,def=2,atkSpd=4,crit=3,critDmg=8,hpPerc=2,dmgPerc=3,defPerc=4};many.m_ItemFinalTitle="名称很长的诅咒森林装备测试";ui.Rebuild();ui.Select(many);Check(ui.Cells.First(c=>c.Item==many).detailLabels.text.Split('\n').Length==9,"detailed cards retain every nonzero attribute");Check(ui.comparisonRows.All(r=>r.root.gameObject.activeSelf)&&ui.comparisonContent.rect.height>ui.comparisonScroll.viewport.rect.height,"all nine effective stat rows available via vertical scrolling");yield return new WaitForEndOfFrame();Shot("long-name-all-stats");
        var locked=im.weaponsOwned.First(i=>i.isLocked);int lockedId=locked.uniqueId;int beforeCount=im.weaponsOwned.Count;float fxp=gm.fxpNow;
        ui.ToggleBatch();ui.EquipSelected();ui.RequestDecompose();Check(ui.modal.activeSelf,"batch destruction requires explicit confirmation");ui.modalCancel.onClick.Invoke();Check(im.weaponsOwned.Count==beforeCount&&gm.fxpNow==fxp,"cancel keeps inventory and fusion experience unchanged");
        ui.RequestDecompose();yield return new WaitForEndOfFrame();Shot("batch-confirmation");Pointer(ui.modalConfirm,"batch confirm");ui.modalConfirm.onClick.Invoke();Check(im.weaponsOwned.Count==1&&im.weaponsOwned[0].uniqueId==lockedId&&gm.fxpNow>fxp&&im.hasWeapon,"batch only removes selected unlocked unequipped items");
        im.helmetsOwned.Clear();im.hasHelmet=false;ui.SetCategory(ItemType.HELMET);Check(ui.VisibleItems.Count==0&&!ui.equip.interactable&&!ui.decompose.interactable&&ui.emptyText.gameObject.activeSelf,"empty category has clear disabled state");yield return new WaitForEndOfFrame();Shot("empty-category");
        ui.SetCategory(ItemType.WEAPON);ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(gm.gameState==GameStateType.CAMP&&!ui.gameObject.activeSelf,"return to shelter works");
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);Check(ui.gameObject.activeSelf&&gm.gameState==GameStateType.INVENTORY,"reopen equipment works");
        ui.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(3);Check(!ui.gameObject.activeSelf&&LevelController.Instance.AssetManager.mapPackageObj.activeSelf,"equipment to map navigation works");
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);ui.navigationButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(2);Check(GameplayCanvas.single.skillsPanel.activeSelf&&!ui.gameObject.activeSelf,"equipment to skills navigation works");
        Check(errors==0,"no unexpected runtime errors");Finish();
    }
    IEnumerator ReferenceCapture(InventoryManager im,GameManager gm)
    {
        // Isolated visual fixture only: same approved art/data, never persisted or used as production item mappings.
        var source=AssetDatabase.LoadAssetAtPath<Texture2D>(StorybookEquipmentBuilder.Root+"/Art/ApprovedEquipment.png");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/ReferenceColour.mat");
        var all=new[]{im.weaponEq}.Concat(im.weaponsOwned).ToArray();var keys=all.Select(i=>i.m_IconName).ToArray();
        int level=gm.playerLevel,skills=gm.skillPoints;float fxp=gm.fxpNow,max=gm.fxpMax;var stats=gm.currentPlayerStat;var grade=all[1].itemGrade;
        var sprites=new List<Sprite>();var mappings=new List<SpritePair>();
        for(int i=0;i<12;i++){
            float x=67+(i%4)*210,y=445+(i/4)*183;
            var sprite=Sprite.Create(source,new Rect(x,source.height-y-98,174,98),Vector2.one*.5f,100);sprites.Add(sprite);
            var pair=new SpritePair{spriteName="QA_REFERENCE_"+i,sprite=sprite};mappings.Add(pair);im.m_ItemIcons.Add(pair);
        }
        for(int i=0;i<all.Length;i++)all[i].m_IconName="QA_REFERENCE_"+(i%12);
        all[1].itemGrade=ItemGrade.VERY_RARE;gm.playerLevel=20;gm.skillPoints=2;gm.fxpMax=100;gm.fxpNow=36;
        gm.currentPlayerStat=new Stat{hpMax=33,dmg=18,def=5,atkSpd=100,crit=8,critDmg=150};
        ui.Rebuild();foreach(var c in ui.Cells)c.icon.material=material;ui.selectedIcon.material=ui.equippedIcon.material=material;
        yield return new WaitForEndOfFrame();Shot("reference-data-9x16-collapsed");ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("reference-data-9x16-expanded");
        ui.ToggleAttributes();yield return new WaitForSecondsRealtime(.3f);
        for(int i=0;i<all.Length;i++)all[i].m_IconName=keys[i];all[1].itemGrade=grade;
        gm.playerLevel=level;gm.skillPoints=skills;gm.fxpNow=fxp;gm.fxpMax=max;gm.currentPlayerStat=stats;
        ui.selectedIcon.material=ui.equippedIcon.material=null;foreach(var c in ui.Cells)c.icon.material=null;
        foreach(var p in mappings)im.m_ItemIcons.Remove(p);ui.Rebuild();foreach(var sprite in sprites)UnityEngine.Object.Destroy(sprite);
        StorybookEquipmentValidation.Log("CAPTURE same-resolution reference-data fixture; separate from current-save render and actual item artwork");
    }
    void Finish(){if(hadColumns)UnityEngine.PlayerPrefs.SetInt(StorybookEquipmentUI.ColumnsPreference,savedColumns);else UnityEngine.PlayerPrefs.DeleteKey(StorybookEquipmentUI.ColumnsPreference);UnityEngine.PlayerPrefs.Save();Application.logMessageReceived-=OnLog;StorybookEquipmentValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
