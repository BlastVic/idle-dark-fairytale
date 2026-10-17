using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class StorybookItemToastValidation
{
 const string Out="output/item-toast/";static bool running;static double started;
 static StorybookItemToastValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("ITEM_TOAST_QA",false)){SessionState.SetBool("ITEM_TOAST_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||StorybookItemToastAuthoring.Busy)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")&&!File.Exists(Out+"build.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("ITEM_TOAST_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("ITEM_TOAST_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;started=EditorApplication.timeSinceStartup;GameManager.single.StartCoroutine(Run());}if(running&&EditorApplication.timeSinceStartup-started>180){Check(false,"timeout");EditorApplication.isPlaying=false;}}
 static void Check(bool ok,string text)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+text+"\n");
 static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Object.Destroy(t);}
 static IEnumerator Run(){
 var intro=Object.FindAnyObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
 var gc=GameplayCanvas.single;var gm=GameManager.single;TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();for(int i=0;i<9;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
 var ui=gc.itemToast;Check(ui&&gc.itemPopUp==ui.gameObject,"Gameplay uses persistent new prefab");if(!ui){EditorApplication.isPlaying=false;yield break;}
 var item=new Item{m_IconName="Wep1",m_ItemFinalTitle="荆棘猎刀",m_ItemType=ItemType.WEAPON,itemGrade=ItemGrade.EPIC,baseStat=new Stat{dmg=18}};
 foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
 StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);gc.ShowItemPopUp(item);yield return new WaitForSecondsRealtime(.5f);Check(ui.group.alpha>.99f&&ui.icon.sprite,"Notification visible with real icon "+size);Check(!ui.group.blocksRaycasts&&!ui.group.interactable,"Does not block game input "+size);
 var corners=new Vector3[4];ui.card.GetWorldCorners(corners);Check(corners[0].x>=0&&corners[2].x<=Screen.width+1&&corners[0].y>=0&&corners[2].y<=Screen.height+1,"Card inside viewport "+size);
 yield return new WaitForEndOfFrame();Shot("equipment-"+size.x+"x"+size.y);yield return new WaitForSecondsRealtime(2.5f);Check(!ui.gameObject.activeSelf,"Auto exit "+size);}
 StorybookEquipmentValidation.Resolution(1080,1920);
 gc.ShowItemPopUp(item);gc.ShowItemPopUp(new Item{itemGrade=ItemGrade.UI_ONLY,m_ItemFinalTitle="属性点 +1"});gc.ShowItemPopUp(new Item{itemGrade=ItemGrade.UI_ONLY,m_ItemFinalTitle="经验 +200"});Check(gc.itemPopUpQueue.Count==2,"Multiple rewards queued without replacement");yield return new WaitForSecondsRealtime(2.9f);Check(ui.itemName.text=="属性点 +1"&&ui.heading.text=="获得奖励","Non-equipment reward localized without false quality");Check(!ui.icon.enabled&&!ui.icon.sprite,"Text reward has no legacy icon dependency");yield return new WaitForEndOfFrame();Shot("stat-reward");yield return new WaitForSecondsRealtime(2.7f);Check(ui.itemName.text=="经验 +200","Queue order preserved");yield return new WaitForSecondsRealtime(2.7f);Check(!ui.Presenting&&gc.itemPopUpQueue.Count==0,"Queue drains and releases presentation");
 item.m_ItemFinalTitle="Wanderer's Ancient Moonlit Thornbreaker Greatsword";gc.ShowItemPopUp(item);yield return new WaitForSecondsRealtime(.5f);Check(ui.itemName.cachedTextGenerator.characterCountVisible>=item.m_ItemFinalTitle.Length,"Long equipment name fits");yield return new WaitForEndOfFrame();Shot("long-name");yield return new WaitForSecondsRealtime(2.5f);
 gc.ShowItemPopUp(item);TransitionCanvas.single.ToBlack();yield return new WaitForSecondsRealtime(3);Check(ui.Presenting&&ui.group.alpha==0,"Transition pauses visible duration");TransitionCanvas.single.ToClear();yield return new WaitForSecondsRealtime(3);Check(!ui.Presenting,"Notification resumes and completes after transition");
 Router.single.LastBattle();yield return new WaitForSecondsRealtime(3);gc.ShowItemPopUp(item);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();Shot("battle-notification");Check(!ui.group.blocksRaycasts,"Battle notification remains click-through");
 File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
 }
}
