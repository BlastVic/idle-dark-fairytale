using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit request only. Exercises the production equipment UI with all icon keys.
[InitializeOnLoad]
public static class EquipmentIconSetValidation
{
    public const string Dir="output/equipment-fullset/";
    static bool running; static double deadline;
    static EquipmentIconSetValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("EQUIPMENT_SET_QA",false)){
                SessionState.SetBool("EQUIPMENT_SET_QA",false);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false); running=false;
            }
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"unity-validation.txt", "All equipment icon validation\n");
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetBool("EQUIPMENT_SET_QA",true);
            deadline=EditorApplication.timeSinceStartup+240;running=false;EditorApplication.isPlaying=true;
        }
        if(SessionState.GetBool("EQUIPMENT_SET_QA",false)&&EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+240;GameManager.single.StartCoroutine(new EquipmentIconSetProbe().Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Log("FAIL watchdog");EditorApplication.isPlaying=false;}
    }
    public static void Log(string s)=>File.AppendAllText(Dir+"unity-validation.txt",s+"\n");
}
public sealed class EquipmentIconSetProbe
{
    int failures, errors;
    void Check(bool ok,string text){EquipmentIconSetValidation.Log((ok?"PASS ":"FAIL ")+text);if(!ok)failures++;}
    void OnLog(string s,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception){errors++;EquipmentIconSetValidation.Log("ERROR "+s);}}
    public IEnumerator Run()
    {
        Application.logMessageReceived+=OnLog;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
        Router.single.InventoryButton();yield return new WaitForSecondsRealtime(2);
        var ui=GameplayCanvas.single.inventoryPanel.GetComponent<StorybookEquipmentUI>();
        if(!ui){Check(false,"equipment UI exists");Finish();yield break;}
        var im=InventoryManager.single;
        var types=new[]{ItemType.WEAPON,ItemType.HELMET,ItemType.SHIELD,ItemType.ARMOR,ItemType.ACCESSORY};
        var prefixes=new[]{"Wep","Head","Sh","Body","Acc"};var counts=new[]{28,26,25,30,31};
        int id=997000;
        im.hasWeapon=im.hasHelmet=im.hasShield=im.hasArmor=im.hasAcc=false;
        for(int c=0;c<5;c++){
            var list=ui.Owned(types[c]);list.Clear();
            for(int i=1;i<=counts[c];i++){
                string key=prefixes[c]+i;var sprite=im.GetSpriteIcon(key);
                Check(sprite&&sprite.name==key&&sprite.texture.width==256&&sprite.texture.height==256,key+" resolves to same-name 256px sprite");
                Check(im.m_ItemIcons.Count(x=>x.spriteName==key)==1,key+" unique mapping");
                list.Add(new Item{uniqueId=id++,m_ItemType=types[c],m_IconName=key,m_ItemFinalTitle=key,itemGrade=(ItemGrade)Mathf.Min(4,(i-1)*5/counts[c]),baseStat=new Stat{dmg=i+5,def=i+3,hpMax=i+10},rollStat=new RollStat()});
            }
        }
        bool hadColumns=PlayerPrefs.HasKey(StorybookEquipmentUI.ColumnsPreference);int columns=PlayerPrefs.GetInt(StorybookEquipmentUI.ColumnsPreference,4);
        ui.SetColumns(4);
        foreach(var size in new[]{new Vector2Int(941,1672),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
            for(int c=0;c<5;c++){
                ui.SetCategory(types[c]);ui.SetSort(0);ui.SetFilter(0);ui.Select(ui.Owned(types[c])[0]);ui.Rebuild();
                yield return new WaitForEndOfFrame();
                Check(ui.Cells.Where(x=>x.gameObject.activeSelf).All(x=>x.icon.sprite),prefixes[c]+" visible cells have sprites "+size);
                for(int page=0;page<3;page++){
                    ui.gridScroll.verticalNormalizedPosition=1-page*.5f;
                    yield return new WaitForEndOfFrame();
                    var tex=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(EquipmentIconSetValidation.Dir+prefixes[c]+"-"+size.x+"x"+size.y+"-"+page+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
                }
            }
        }
        if(hadColumns)PlayerPrefs.SetInt(StorybookEquipmentUI.ColumnsPreference,columns);else PlayerPrefs.DeleteKey(StorybookEquipmentUI.ColumnsPreference);PlayerPrefs.Save();
        Finish();
    }
    void Finish(){Application.logMessageReceived-=OnLog;EquipmentIconSetValidation.Log((failures==0&&errors==0?"PASS":"FAIL")+" COMPLETE failures="+failures+" errors="+errors);EditorApplication.isPlaying=false;}
}
