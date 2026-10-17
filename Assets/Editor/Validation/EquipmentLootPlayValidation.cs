using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentLootPlayValidation
{
    const string Key="EQUIPMENT_LOOT_QA";
    static bool running;static double started;
    static EquipmentLootPlayValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)){
                SessionState.SetBool(Key,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;
                if(SessionState.GetBool(Key+"_LOCK",false)){SessionState.SetBool(Key+"_LOCK",false);EditorApplication.UnlockReloadAssemblies();}
                string previous=SessionState.GetString(Key+"_SCENE","");
                if(!string.IsNullOrEmpty(previous)&&previous!=EquipmentLootAuthoring.ScenePath)EditorSceneManager.OpenScene(previous);
                typeof(CursedStorybookUiValidation).GetMethod("RestoreGameView",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            }
        };
    }
    public static void Start()
    {
        EquipmentLootAuthoring.WithScene((w,i)=>{});
        if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene; validation not started");
        SessionState.SetString(Key+"_SCENE",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene(EquipmentLootAuthoring.ScenePath);
        File.WriteAllText(EquipmentLootAuthoring.Output+"play-validation.txt","Equipment loot production flow\n");
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool(Key,true);started=EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(!running&&GameManager.single&&GameplayCanvas.single){running=true;started=EditorApplication.timeSinceStartup;EditorApplication.LockReloadAssemblies();SessionState.SetBool(Key+"_LOCK",true);GameManager.single.StartCoroutine(Run());}
        if(running&&EditorApplication.timeSinceStartup-started>180){Log("FAIL timeout");EditorApplication.isPlaying=false;}
    }
    static void Log(string text)=>File.AppendAllText(EquipmentLootAuthoring.Output+"play-validation.txt",text+"\n");
    static int failures,errors;
    static void Check(bool ok,string text){Log((ok?"PASS ":"FAIL ")+text);if(!ok)failures++;}
    static void OnLog(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;Log("ERROR "+text);}}
    static IEnumerator Run()
    {
        failures=errors=0;Application.logMessageReceived+=OnLog;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        for(int n=0;n<20;n++)if(!GameManager.single.HasSeenThis("Tutorial-"+n))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+n);
        if(TutorialManager.single){TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();}
        var im=InventoryManager.single;var gc=GameplayCanvas.single;
        im.ClearInventory();im.hasWeapon=im.hasShield=im.hasHelmet=im.hasArmor=im.hasAcc=false;
        // Invoke the production Enemy.Drops entry point without starting an actor or a battle.
        var fixture=new GameObject("Loot QA");fixture.SetActive(false);var enemy=fixture.AddComponent<Enemy>();enemy.equipmentLootKey="MobRivenfang";
        int before=im.GetTotalItemCount();enemy.Drops();Check(im.GetTotalItemCount()==before+3,"Boss Drops delivers three actual inventory items");UnityEngine.Object.Destroy(fixture);
        foreach(var entry in EquipmentLoot.Data.entries)im.AddItem(EquipmentLoot.Roll(entry,ItemGrade.RARE));
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        var ui=gc.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        Check(ui,"Production equipment screen loaded");
        foreach(ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            ui.SetCategory(type);ui.SetFilter(0);var item=ui.Owned(type).First(i=>EquipmentLoot.Data.entries.Single(e=>e.key==i.m_IconName).tier==1);
            ui.Select(item);ui.EquipSelected();Check(ui.Equipped(type)?.m_IconName==item.m_IconName,"Equip generated "+type);
        }
        var sizes=new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)};
        yield return new WaitForSecondsRealtime(4);
        for(int i=0;i<sizes.Length;i++)
        {
            var size=sizes[i];StorybookEquipmentValidation.Resolution(size.x,size.y);ui.SetCategory(ItemType.WEAPON);ui.SetFilter(0);ui.SetSort(0);ui.Rebuild();
            yield return new WaitForSecondsRealtime(.7f);ui.layout.Apply();Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();Shot("equipment-"+size.x+"x"+size.y);
        }
        // Return through the authored navigation before opening three chest qualities.
        ui.back.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        for(int i=0;i<3;i++)
        {
            var size=sizes[i];StorybookEquipmentValidation.Resolution(size.x,size.y);
            var chest=new Chest{uniqueKey="loot-qa-"+i,chestGrade=(ChestGrade)i,chestMapName="Khorasan Ruins I",
                rewardTime=DateTime.Now.AddSeconds(-5).ToBinary().ToString(),crystalsMin=10,crystalsMax=10};
            // Deliberately zero tier: exercise old-save migration via real Claim().
            im.chestsOwned.Add(chest);Router.single.CallChestFromCamp(chest);yield return new WaitForSecondsRealtime(2);
            var owner=ChestPackage.single;Check(owner,"Chest page loaded");if(!owner)break;
            before=im.GetTotalItemCount();float money=GameManager.single.crystals;
            owner.TapOpen();owner.TapOpen();
            Check(im.GetTotalItemCount()==before+3+i&&GameManager.single.crystals==money+10&&!owner.Owned,"Legacy "+chest.chestGrade+" commits once");
            Check(owner.RewardItems.Select(x=>x.m_ItemType).Distinct().Count()==3+i,"Distinct chest slots");
            float deadline=Time.realtimeSinceStartup+20;while(!owner.view.opening.Finished&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(owner.view.opening.Finished,"Opening animation completes");
            yield return new WaitForEndOfFrame();Shot("chest-"+chest.chestGrade+"-"+size.x+"x"+size.y);
            var scroll=owner.view.opening.rewardsScroll;
            Check(scroll.content.GetComponentsInChildren<ChestRewardRow>().Length==owner.RewardItems.Count+1,"Every equipment and currency reward has an authored row");
            scroll.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();
            Shot("chest-"+chest.chestGrade+"-"+size.x+"x"+size.y+"-bottom");
            owner.CloseButtonWithRewards();deadline=Time.realtimeSinceStartup+10;while(owner&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1);Check(!owner,"Returns to camp");
        }
        Application.logMessageReceived-=OnLog;Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);
        EditorApplication.isPlaying=false;
    }
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(EquipmentLootAuthoring.Output+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
}
