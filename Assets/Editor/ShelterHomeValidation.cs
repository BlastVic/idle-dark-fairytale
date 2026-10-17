using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Game.Enum;
using Assets.Scripts.Services.Purchasing;
using Scripts.Level;

/// <summary>Explicit local QA. Uses real Gameplay and disables persistence during fixtures.</summary>
[InitializeOnLoad]
public static class ShelterHomeValidation
{
    const string Dir="output/home-ui/";
    static int stage,ratio,errors,flow;
    static double until,deadline;
    static int points,level;
    static float music,sound;
    static readonly int[] W={720,768,720}, H={1280,1024,1440};
    static readonly string[] Ratios={"9x16","3x4","1x2"};
    static ShelterHomeValidation() { if(SessionState.GetBool("SHELTER_QA_PENDING",false)) { stage=1; until=EditorApplication.timeSinceStartup+3; deadline=EditorApplication.timeSinceStartup+90; SessionState.SetBool("SHELTER_QA_PENDING",false); } EditorApplication.update+=Tick;Application.logMessageReceived+=LogError; }
    static void LogError(string m,string s,LogType t) {if(m=="Logger Methods: Ad is unavailable when trying to ShowRewardedAd") { if(stage>0)Log("KNOWN legacy ad availability diagnostic (no ad service in editor)"); return; } if(stage>0&&(t==LogType.Exception||t==LogType.Error)){errors++;Log("ERROR "+m+"\n"+s);}}
    static void Log(string m){Directory.CreateDirectory(Dir);File.AppendAllText(Dir+"validation.txt",m+"\n");}
    static void Check(bool ok,string m){if(!ok)throw new Exception(m);Log("PASS "+m);}
    static void Next(int n,float wait=.6f){stage=n;until=EditorApplication.timeSinceStartup+wait;deadline=EditorApplication.timeSinceStartup+90;}
    static void Shot(string n){ScreenCapture.CaptureScreenshot(Dir+n+".png");}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try {
            if(stage==0) {
                bool visual=File.Exists(Dir+"visual.request");
                if(!visual && !File.Exists(Dir+"validate.request"))return;
                SessionState.SetBool("SHELTER_QA_VISUAL",visual);if(visual)File.Delete(Dir+"visual.request");
                if(EditorApplication.isPlaying) { File.Delete(Dir+"validate.request"); SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true); errors=ratio=flow=0; Next(1,2); return; }
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(Dir+"validate.request");File.WriteAllText(Dir+"validation.txt","Shelter home actual Unity validation\n");
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true); errors=ratio=flow=0;
                Resolution(720,1280);SessionState.SetBool("SHELTER_QA_PENDING",true);EditorApplication.isPlaying=true;Next(1,3);return;
            }
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout stage "+stage);
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
            var gm=GameManager.single;var gc=GameplayCanvas.single;
            if(!gm||!gc||!Player.single||!gc.homeUI)return;
            var ui=gc.homeUI;
            if(stage==1) {
                if(gm.gameState!=GameStateType.CAMP)return;
                var intro=UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;
                gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);
                if(!StorybookDailyReward.ClaimedToday)gm.thingsSeenPermanent.Add(StorybookDailyReward.ClaimKey(DateTime.Today));gc.dailyCherriesPop.SetActive(false);
                if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
                Check(ui.gameObject.activeInHierarchy,"Camp displays saved home prefab");
                Check(AssetDatabase.LoadAssetAtPath<GameObject>(ShelterHomeBuilder.Path)!=null,"Saved editable prefab exists");
                foreach(var t in ui.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Script binding "+t.name);
                if(SessionState.GetBool("SHELTER_QA_VISUAL",false)) {Resolution(W[ratio],H[ratio]);Next(2,1);return;}
                points=gm.statPoints;level=gm.hpLevel;
                gm.statPoints=0;ui.Refresh();Check(ui.statButtons.All(b=>!b.interactable),"Zero points disables all six upgrade buttons");
                ui.BuyStat(0);Check(gm.statPoints==0&&gm.hpLevel==level,"Disabled upgrade cannot spend points");
                PurchaserManager.Instance.SetLastStatPurchased(LevelTypeEnum.DEF);
                gm.statPoints=3;ui.Refresh();Check(ui.statButtons[0].interactable,"Points enable eligible upgrades");
                ui.statButtons[0].onClick.Invoke();Check(gm.statPoints==2&&gm.hpLevel==level+1,"Upgrade spends exactly one point and increments stat");
                Check(!ui.statButtons[0].interactable,"Last purchased attribute disabled immediately");
                ui.BuyStat(0);Check(gm.statPoints==2,"Repeated click cannot double-spend");
                gm.hpLevel=100;gm.statPoints=1;PurchaserManager.Instance.SetLastStatPurchased(LevelTypeEnum.DEF);ui.Refresh();Check(!ui.statButtons[0].interactable,"Level 100 requires two points");
                gm.statPoints=2;ui.Refresh();Check(ui.statButtons[0].interactable,"Two points permits level 100 purchase");
                gm.hpLevel=level;gm.statPoints=points;Player.single.ab.currentStat=gm.CalcPlayer();Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax;ui.Refresh();
                Resolution(W[ratio],H[ratio]);Next(2,1);
            } else if(stage==2) {
                if(ui.GetComponent<ShelterResponsiveLayout>())ui.GetComponent<ShelterResponsiveLayout>().Apply();else ui.GetComponent<BattleHudSafeArea>().Apply();Canvas.ForceUpdateCanvases();
                var pixel=ui.GetComponent<Canvas>().pixelRect;Check((int)pixel.width==W[ratio]&&(int)pixel.height==H[ratio],"Actual resolution "+Ratios[ratio]);
                foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeInHierarchy)) {
                    var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);
                    Check(corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=pixel.width+1&&c.y<=pixel.height+1),Ratios[ratio]+" control inside screen "+b.name);
                }
                var data=new PointerEventData(EventSystem.current){displayIndex=0,position=RectTransformUtility.WorldToScreenPoint(null,ui.settingsButton.transform.TransformPoint(((RectTransform)ui.settingsButton.transform).rect.center))};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                Log("Raycast hits: "+string.Join(",",hits.Select(h=>h.gameObject.name))+" at "+data.position);
                Shot("home-"+Ratios[ratio]);
                Check(ui.settingsButton.targetGraphic.raycastTarget && ui.settingsButton.targetGraphic.Raycast(data.position,null) && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)ui.settingsButton.transform,data.position),"Settings graphic accepts pointer inside its rectangle");
                ShelterPointerProbe.Run(ui,Dir+"validation.txt");Shot("home-"+Ratios[ratio]);Next(3);
            } else if(stage==3) {
                ratio++;if(ratio<3){Resolution(W[ratio],H[ratio]);Next(2,1);}
                else {Resolution(720,1280);ui.ToggleAttributes();Next(4,1);}
            } else if(stage==4) {
                Check(!ui.attributeBody.blocksRaycasts&&ui.attributeBody.alpha==0&&Mathf.Abs(ui.attributePanel.sizeDelta.y-ui.collapsedHeight)<.1f,"Collapsed attributes hidden and not clickable");Shot("home-collapsed");
                ui.ToggleAttributes();Next(5);
            } else if(stage==5) {
                Check(ui.attributeBody.blocksRaycasts&&ui.attributeBody.alpha==1,"Expanded attributes restore interaction");
                var feedback=ui.settingsButton.GetComponent<ShelterButtonFeedback>();var before=feedback.transform.localScale;
                feedback.OnPointerDown(new PointerEventData(EventSystem.current));Check(feedback.transform.localScale.x<before.x,"Button press scales down");
                feedback.OnPointerUp(new PointerEventData(EventSystem.current));Check(feedback.transform.localScale==before,"Button release restores scale");
                ui.OpenFunction(2);Next(6);
            } else if(stage==6) {
                Check(ui.modal.activeSelf&&ui.modalGroup.alpha==1,"Function details open with fade");Shot("rebirth-details");Next(106);
            } else if(stage==106) {ui.CloseModal();ui.OpenSettings();Next(7);
            } else if(stage==7) {
                Check(ui.settingsUI&&ui.settingsUI.IsOpen,"Settings opens actual audio controls");music=IdleKnightHero.UI.SoundManager.Instance.musicLevel;sound=IdleKnightHero.UI.SoundManager.Instance.soundLevel;
                ui.settingsUI.music.value=.3f;ui.settingsUI.sound.value=.4f;
                Check(Mathf.Abs(IdleKnightHero.UI.SoundManager.Instance.musicLevel-.3f)<.001f&&Mathf.Abs(IdleKnightHero.UI.SoundManager.Instance.soundLevel-.4f)<.001f,"Audio sliders bind to sound manager");
                Shot("settings");Next(107);
            } else if(stage==107) {ui.settingsUI.music.value=music;ui.settingsUI.sound.value=sound;ui.settingsUI.Close();
                if(SessionState.GetBool("SHELTER_QA_VISUAL",false)){Log("PASS final visual + interaction regression; errors="+errors);stage=0;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);return;}
                ui.navigationButtons[0].onClick.Invoke();Next(8,3);
            } else if(stage==8) {
                Check(gm.gameState==GameStateType.INVENTORY&&!ui.gameObject.activeSelf,"Inventory opens and home hides");Router.single.CampButton();Next(9,3);
            } else if(stage==9) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Inventory returns home");ui.navigationButtons[1].onClick.Invoke();Next(10,3);
            } else if(stage==10) {
                Check(gm.gameState==GameStateType.SKILLS&&!ui.gameObject.activeSelf,"Skills opens");Router.single.CampButton();Next(11,3);
            } else if(stage==11) {Check(gm.gameState==GameStateType.CAMP,"Skills returns home");ui.navigationButtons[3].onClick.Invoke();Next(12,3);}
            else if(stage==12) {Check(gm.gameState==GameStateType.MAP&&!ui.gameObject.activeSelf,"Map opens");Router.single.CampButton();Next(13,3);}
            else if(stage==13) {Check(gm.gameState==GameStateType.CAMP,"Map returns home");ui.OpenFunction(1);Next(14,3);}
            else if(stage==14) {Check(gc.experienceUI.gameObject.activeSelf&&ui.gameObject.activeSelf,"Experience page opens over shelter");Router.single.CloseExperience();Next(15,3);}
            else if(stage==15) {Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Experience returns home");ui.battleButton.onClick.Invoke();Next(16,3);}
            else if(stage==16) {if(gm.gameState!=GameStateType.BATTLE)return;Check(!ui.gameObject.activeSelf&&gc.battleHud.gameObject.activeSelf,"Continue battle restores battle HUD");gc.battleHud.backButton.onClick.Invoke();Next(17,3);}
            else if(stage==17) {Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf&&!ui.IsNavigating,"Battle return resets navigation");Shot("return-home");Next(117);
            } else if(stage==117) {ui.navigationButtons[4].onClick.Invoke();Next(18,3);
            } else if(stage==18) {
                Check(gm.gameState==GameStateType.CASH_MALL&&!ui.gameObject.activeSelf,"Shop opens");Router.single.CallCampFromCashMall();Next(19,3);
            } else if(stage==19) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Shop returns home");
                if(InventoryManager.single.chestsOwned.Count>0) {ui.chestButtons[0].onClick.Invoke();Next(20,3);} else Next(21);
            } else if(stage==20) {
                Check(gm.gameState==GameStateType.CHEST_PACKAGE&&!ui.gameObject.activeSelf,"Chest opens original reward details");Router.single.CallCampFromChest(false);Next(21,3);
            } else if(stage==21) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Chest returns home");gm.fxpNow=0;ui.functionButtons[0].onClick.Invoke();Check(!ui.modal.activeSelf,"Fusion skips function details");Next(22,3);
            } else if(stage==22) {
                Check(gm.gameState==GameStateType.FUSION&&!ui.gameObject.activeSelf,"Fusion opens original flow");Router.single.CallCampFromFusion();Next(23,3);
            } else if(stage==23) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Fusion returns home");gm.playerLevel=1;ui.OpenFunction(2);Check(!ui.modalAction.interactable,"Locked rebirth does not allow entry");
                gm.playerLevel=999;ui.Refresh();Check(ui.modalAction.interactable,"Eligible rebirth enables entry");ui.modalAction.onClick.Invoke();Next(24,3);
            } else if(stage==24) {
                Check(gm.gameState==GameStateType.REBIRTH&&!ui.gameObject.activeSelf,"Rebirth opens original flow");Router.single.CallCampFromRebirth();Next(25,3);
            } else if(stage==25) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Rebirth returns home");gm.playerLevel=19;ui.battleButton.onClick.Invoke();Next(26,3);
            } else if(stage==26) {
                if(gm.gameState!=GameStateType.BATTLE)return;Router.single.StartCoroutine(Router.single.GoToDefeat());Next(27,3);
            } else if(stage==27) {
                if(gm.gameState!=GameStateType.DEFEAT)return;Check(!ui.gameObject.activeSelf&&gc.defeatPanel.activeSelf,"Defeat settlement remains visible");gc.GoToCampFromVictoryOrDefeat();Next(28,3);
            } else if(stage==28) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Defeat returns home");ui.battleButton.onClick.Invoke();Next(29,3);
            } else if(stage==29) {
                if(gm.gameState!=GameStateType.BATTLE)return;Router.single.newChest=new Chest{chestMapName=WaveManager.single.currentWaveset.levelName,crystalsMin=0,crystalsMax=0};Router.single.StartCoroutine(Router.single.GoToVictory());Next(30,4);
            } else if(stage==30) {
                if(gm.gameState!=GameStateType.VICTORY)return;Check(!ui.gameObject.activeSelf&&gc.victoryPanel.activeSelf,"Victory settlement remains visible");gc.GoToCampFromVictoryOrDefeat();Next(31,3);
            } else if(stage==31) {
                Check(gm.gameState==GameStateType.CAMP&&ui.gameObject.activeSelf,"Victory returns home");
                var responsive=ui.GetComponent<ShelterResponsiveLayout>();responsive.Apply();
                foreach(float aspect in new[]{.5f,.5625f,.6f,.666667f,.7f,.75f}) {
                    var input=new Vector2(1920*aspect,1920);Check((ShelterResponsiveLayout.SupportedSize(input)-input).magnitude<.01f,"Full viewport across supported aspect "+aspect);
                }
                Check(ShelterResponsiveLayout.SupportedSize(new Vector2(1600,1920))==new Vector2(1440,1920),"Black side margins beyond 3:4");
                Check(ShelterResponsiveLayout.SupportedSize(new Vector2(900,2160))==new Vector2(900,1800),"Black top and bottom beyond 1:2");
                Check(Mathf.Abs(responsive.room.localScale.x-responsive.room.localScale.y)<.0001f,"Background remains uniformly scaled");
                Log(errors==0?"PASS all home checks; unexpected runtime errors=0":"FAIL runtime errors="+errors);stage=0;EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            }
        } catch(Exception e) {Log("FAIL "+e);stage=0;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
    }
    static void Resolution(int w,int h)=>StorybookEquipmentValidation.Resolution(w,h);
}
