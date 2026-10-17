using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Ads;
using Assets.Scripts.Services.Ads.MediationProxy;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ChestOpeningValidation
{
    const string Out="output/chest-opening/",Flag="CHEST_OPENING_QA";
    static bool running;
    static double deadline;
    static ChestOpeningValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
            File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            SessionState.SetBool(Flag,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying)return;
        if(!running&&GameManager.single&&GameplayCanvas.single){running=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(Run());}
        if(running&&EditorApplication.timeSinceStartup>deadline){Check(false,"timeout");EditorApplication.isPlaying=false;}
    }
    static void Check(bool v,string message)=>File.AppendAllText(Out+"validation.txt",(v?"PASS ":"FAIL ")+message+"\n");
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run(){
        var gm=GameManager.single;var gc=GameplayCanvas.single;var im=InventoryManager.single;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.dailyCherriesPop.SetActive(false);
        var source=im.weaponsOwned.FirstOrDefault()??im.weaponEq;Check(source!=null,"test reward source exists");if(source==null){EditorApplication.isPlaying=false;yield break;}
        int index=0;
        foreach(var sz in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(sz.x,sz.y);
            var item=Utilities.DeepClone(source);item.m_ChanceDrop=1;
            var pool=Enumerable.Repeat(item,index==0?2:index==1?0:6).ToList();
            int rewardCount=pool.Count;
            var grade=new[]{ChestGrade.NORMAL,ChestGrade.GOLD,ChestGrade.DIAMOND}[index];
            var chest=new Chest{uniqueKey="opening-qa-"+index,chestGrade=grade,chestMapName="第一章 · 黑森林",rewardTime=DateTime.Now.AddSeconds(index==0?-5:100).ToBinary().ToString(),crystalsMin=120,crystalsMax=120,possibleRewards=pool,possibleGoldRewards=pool,possibleDiamondRewards=pool};
            im.chestsOwned.Add(chest);Router.single.CallChestFromCamp(chest);yield return new WaitForSecondsRealtime(2);
            var owner=ChestPackage.single;Check(owner&&owner.view.opening,"authored opening prefab loaded");if(!owner){EditorApplication.isPlaying=false;yield break;}
            var opening=owner.view.opening;var fixedScale=opening.skeleton.rectTransform.localScale;var fixedPosition=opening.skeleton.rectTransform.anchoredPosition;bool fixedTransform=true;
            var page=owner.GetComponent<StorybookFusionLayout>().page;var buttonCorners=new Vector3[4];((RectTransform)owner.view.advert.transform).GetWorldCorners(buttonCorners);
            Check(buttonCorners.Min(c=>page.InverseTransformPoint(c).y)+page.rect.height*.5f>=32,"advertisement button has at least 32 design units bottom clearance");
            int before=im.GetTotalItemCount();float money=gm.crystals;
            yield return new WaitForEndOfFrame();Shot(sz.x+"x"+sz.y+"-closed");
            if(index==0){owner.TapOpen();owner.TapOpen();}
            else if(index==1){gm.premiumCurrency=10;owner.cherryCost=3;owner.PayCherriesButton();owner.PayCherriesButton();Check(gm.premiumCurrency==7,"paid opening charges once");}
            else {
                var ad=AdManager.Instance;var field=typeof(AdManager).GetField("_mediationProxy",BindingFlags.Instance|BindingFlags.NonPublic);var previous=field.GetValue(ad);var fake=new FakeAd();field.SetValue(ad,fake);
                fake.RewardedVideoFinished+=success=>typeof(AdManager).GetMethod("OnRewardedVideoFinished",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ad,new object[]{success});
                try{owner.WatchVidButton();fake.Finish(false);Check(owner.Owned&&!owner.Opening,"cancelled ad leaves chest untouched");owner.WatchVidButton();fake.Finish(true);fake.Finish(true);}finally{field.SetValue(ad,previous);}
            }
            Check(owner.view.openingHiddenObjects.All(n=>!n.activeSelf),"header information hidden during opening");
            owner.CloseButton();owner.CloseButtonWithRewards();
            Check(!owner.Leaving&&owner.Opening,"opening cannot be closed or triggered twice");
            Check(im.GetTotalItemCount()==before+rewardCount&&gm.crystals==money+120&&!owner.Owned,"rewards committed exactly once at open");
            int frame=0;float nextFrame=0;
            if(index==0){Directory.CreateDirectory(Out+"frames");File.WriteAllText(Out+"frames/times.txt","");}
            float time=0;bool charge=false,burst=false,overlap=false;while(!opening.Finished&&time<15){
                time+=Time.unscaledDeltaTime;yield return new WaitForEndOfFrame();
                overlap |= opening.featured.gameObject.activeSelf && opening.featured.transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>().Any(img=>img.name=="FlyingReward");
                fixedTransform &= opening.skeleton.rectTransform.localScale==fixedScale && opening.skeleton.rectTransform.anchoredPosition==fixedPosition;
                if(index==0&&time>=nextFrame){Shot("frames/"+frame.ToString("D4"));File.AppendAllText(Out+"frames/times.txt",frame+" "+time.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");frame++;nextFrame=time+.05f;}
                if(!charge&&time>=.95f){charge=true;Shot(sz.x+"x"+sz.y+"-charge");}
                if(!burst&&time>=1.4f){burst=true;Shot(sz.x+"x"+sz.y+"-burst");}
                yield return null;
            }
            if(rewardCount>0)Check(overlap,"next reward reveal overlaps previous reward landing");
            Check(!opening.featured.transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>().Any(img=>img.name=="FlyingReward"),"all reward flights finish before collect");
            Check(fixedTransform,"chest size and position stay fixed throughout opening");
            Check(opening.Finished&&opening.collect.gameObject.activeInHierarchy,"all rewards displayed before collect");
            Check(opening.rewardsScroll.content.GetComponentsInChildren<ChestRewardRow>().Length==rewardCount+1,"crystals and equipment retained in reward list");
            Check(gm.gameState==GameStateType.CHEST_PACKAGE,"stays on chest page until player confirms");
            var corners=new Vector3[4];((RectTransform)opening.collect.transform).GetWorldCorners(corners);Check(corners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),sz+" collect inside viewport");
            yield return new WaitForEndOfFrame();Shot(sz.x+"-summary-"+index);
            opening.collect.onClick.Invoke();opening.collect.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            Check(gm.gameState==GameStateType.CAMP&&!ChestPackage.single,"collect returns to camp");
            Check(im.GetTotalItemCount()==before+rewardCount&&gm.crystals==money+120,"return does not issue rewards again");index++;
        }
        File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
    sealed class FakeAd:IAdMediationProxy{public event Action<bool> RewardedVideoFinished;public void Finish(bool v)=>RewardedVideoFinished?.Invoke(v);public void Initialize(){}public bool IsInterstitialAvailable()=>false;public bool IsRewardedVideoAvailable()=>true;public void ShowBanner(){}public void ShowInterstitial(){}public void ShowRewardedVideo(){}public void ShowTestSuit(){}}

}
