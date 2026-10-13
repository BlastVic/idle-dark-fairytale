using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[InitializeOnLoad]
public static class StorybookBattleResultValidation
{
    const string Dir="output/battle-result/";
    static bool running;
    static double deadline;
    static StorybookBattleResultValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("RESULT_QA",false)){SessionState.SetBool("RESULT_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","");SessionState.SetBool("RESULT_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
        if(EditorApplication.isPlaying&&SessionState.GetBool("RESULT_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Log(false,"Timeout");EditorApplication.isPlaying=false;}
    }
    static void Log(bool ok,string s)=>File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+s+"\n");
    static void Shot(string s){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+s+".png",t.EncodeToPNG());Object.Destroy(t);}
    static void Pointer(Button b){var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Log(p.x>=0&&p.x<Screen.width&&p.y>=0&&p.y<Screen.height&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,b.name+" pointer in safe bounds");}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;
        yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;var gm=GameManager.single;var wm=WaveManager.single;
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        gc.homeUI.gameObject.SetActive(false);gc.ToggleBattleUi(false);gc.ToggleButtonGrid(false);gc.ToggleCampUi(false);
        wm.currentWaveset=wm.GetWavesetByKey("Khorasan Ruins II");wm.SetWaveseReachedByKey(wm.currentWaveset.levelKey,wm.currentWaveset.waves.Length);
        Router.single.newChest=new Chest{crystalsMin=10,crystalsMax=20};Router.single.waitingForBattleCleanup=false;
        var victory=gc.victoryPanel.GetComponent<StorybookBattleResultUI>();var defeat=gc.defeatPanel.GetComponent<StorybookBattleResultUI>();
        var shared= victory.transform.Find("Core_9x16/ApprovedArtwork_NoText").GetComponent<Image>().sprite;
        Log(shared&&shared==defeat.transform.Find("Core_9x16/ApprovedArtwork_NoText").GetComponent<Image>().sprite,"Victory and defeat reference the same panel sprite");
        Log(defeat.transform.Find("Core_9x16/DefeatLantern").GetComponent<Image>().sprite!=null,"Defeat lantern is a separate persisted Image");
        gc.victoryPanel.SetActive(true);yield return new WaitForSecondsRealtime(.4f);
        StorybookEquipmentValidation.Resolution(1080,1920);yield return new WaitForSecondsRealtime(.3f);
        foreach(var grade in new[]{ChestGrade.NORMAL,ChestGrade.GOLD,ChestGrade.DIAMOND}){
            Router.single.newChest.chestGrade=grade;victory.Refresh();yield return new WaitForEndOfFrame();
            Log(victory.chestImage&&victory.chestImage.enabled&&victory.chestImage.sprite==victory.ChestSprite(grade)&&victory.rewardTitle.text==StorybookBattleResultUI.ChestName(grade),"Actual drop binds chest image and name: "+grade);
            Shot("chest-"+grade);
        }
        Router.single.newChest.chestGrade=ChestGrade.TURD;victory.Refresh();Log(!victory.chestImage.enabled,"Unsupported chest grade cannot show previous reward image");
        var drop=Router.single.newChest;Router.single.newChest=null;victory.Refresh();Log(!victory.chestImage.enabled,"Missing drop clears previous chest image");Router.single.newChest=drop;drop.chestGrade=ChestGrade.NORMAL;victory.Refresh();
        Log(victory.NextKey()=="test01","Next node uses available map sequence");
        foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            var transition=TransitionCanvas.single;
            transition.ToBlack("Transition icon regression");yield return new WaitForSecondsRealtime(.7f);yield return new WaitForEndOfFrame();
            var group=transition.group;
            Log(transition.GetComponentsInChildren<Image>(true).Length==1&&group.alpha>.99f&&group.blocksRaycasts,"Plain black transition with input blocked: "+size);
            Shot("transition-black-"+size.x+"x"+size.y);
            transition.ToClear("Transition icon regression");yield return new WaitForSecondsRealtime(.7f);yield return new WaitForEndOfFrame();
            Log(group.alpha<.01f&&!group.blocksRaycasts,"Transition clears and releases input: "+size);
            Shot("victory-next-"+size.x+"x"+size.y);Pointer(victory.next);Pointer(victory.homeSmall);Pointer(victory.currency);
            gc.victoryPanel.SetActive(false);gc.defeatPanel.SetActive(true);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("defeat-"+size.x+"x"+size.y);Pointer(defeat.replay);Pointer(defeat.home);Log(!defeat.nextActions.activeSelf,"Defeat hides next node");
            gc.defeatPanel.SetActive(false);gc.victoryPanel.SetActive(true);yield return new WaitForSecondsRealtime(.3f);
        }
        wm.currentWaveset=wm.GetWavesetByKey("Mount Zion");wm.SetWaveseReachedByKey(wm.currentWaveset.levelKey,wm.currentWaveset.waves.Length);victory.Refresh();Log(string.IsNullOrEmpty(victory.NextKey())&&!victory.nextActions.activeSelf,"Chapter end has no unreleased next node");
        yield return new WaitForEndOfFrame();Shot("victory-last-node");
        float before=gm.crystals;victory.ClaimReward();float after=gm.crystals;victory.ClaimReward();Log(after>before&&gm.crystals==after,"Reward granted exactly once");Log(!victory.rewardOptions.activeSelf,"Claimed reward hides open actions");
        gc.victoryPanel.SetActive(false);gc.victoryPanel.SetActive(true);yield return new WaitForSecondsRealtime(.3f);
        gm.premiumCurrency=100;float currencyBefore=gm.premiumCurrency;victory.currency.onClick.Invoke();victory.currency.onClick.Invoke();Log(gm.premiumCurrency==currencyBefore-gm.ChestPremiumOpenCost&&victory.RewardClaimed,"Paid opening charges once");
        gc.victoryPanel.SetActive(false);gc.victoryPanel.SetActive(true);yield return new WaitForSecondsRealtime(.3f);gm.premiumCurrency=0;float crystalsBefore=gm.crystals;victory.currency.onClick.Invoke();Log(gm.crystals==crystalsBefore&&!victory.RewardClaimed,"Insufficient currency grants nothing");yield return new WaitForSecondsRealtime(1.6f);
        var ad=Assets.Scripts.Services.Ads.AdManager.Instance;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(Assets.Scripts.Services.Ads.AdManager).GetField("_adTag",flags).SetValue(ad,"BattleResult");
        typeof(Assets.Scripts.Services.Ads.AdManager).GetField("_adFinishedCallback",flags).SetValue(ad,(System.Action)victory.ClaimReward);
        typeof(Assets.Scripts.Services.Ads.AdManager).GetMethod("OnRewardedVideoFinished",flags).Invoke(ad,new object[]{true});float adReward=gm.crystals;Log(adReward>crystalsBefore&&victory.RewardClaimed,"Simulated ad completion uses new reward callback");
        typeof(Assets.Scripts.Services.Ads.AdManager).GetMethod("OnRewardedVideoFinished",flags).Invoke(ad,new object[]{true});Log(gm.crystals==adReward,"Repeated ad completion cannot duplicate reward");
        Router.single.waitingForBattleCleanup=true;victory.Refresh();Log(!victory.replay.interactable,"Cleanup disables navigation");Router.single.waitingForBattleCleanup=false;
        wm.currentWaveset=wm.GetWavesetByKey("Khorasan Ruins II");victory.Refresh();victory.next.onClick.Invoke();Log(victory.IsBusy,"Next click locks duplicate input");yield return new WaitForSecondsRealtime(3);
        Log(wm.currentWaveset.levelKey=="test01"&&!gc.victoryPanel.activeSelf,"Next starts correct battle and closes results");
        wm.StopAllCoroutines();wm.CancelInvoke();gm.BattleCleanup();yield return null;
        yield return GameManager.single.StartCoroutine(Router.single.GoToVictory());yield return new WaitForSecondsRealtime(.4f);Log(gc.victoryPanel.activeSelf,"Real victory route opens prefab");victory.replay.onClick.Invoke();yield return new WaitForSecondsRealtime(3);Log(!gc.victoryPanel.activeSelf&&wm.currentWaveset.levelKey=="test01","Victory replay re-enters same node");
        wm.StopAllCoroutines();wm.CancelInvoke();gm.BattleCleanup();yield return null;
        yield return GameManager.single.StartCoroutine(Router.single.GoToDefeat());yield return new WaitForSecondsRealtime(.4f);Log(gc.defeatPanel.activeSelf,"Real defeat route opens prefab");defeat.home.onClick.Invoke();float timeout=Time.realtimeSinceStartup+8;while(Time.realtimeSinceStartup<timeout&&(!gc.homeUI.gameObject.activeSelf||gc.defeatPanel.activeSelf))yield return null;Log(gc.homeUI.gameObject.activeSelf&&!gc.defeatPanel.activeSelf,"Defeat returns to shelter (state="+gm.gameState+", busy="+defeat.IsBusy+", cleanup="+Router.single.waitingForBattleCleanup+")");
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
