using System.Linq;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Explicit local QA only; never rebuilds authored UI or writes player saves.
[InitializeOnLoad]
public static class StorybookNavigationValidation
{
    const string Out="output/shared-navigation/";
    static bool running;
    static StorybookNavigationValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SHARED_NAV_QA",false)){SessionState.SetBool("SHARED_NAV_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");SessionState.SetBool("SHARED_NAV_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying&&SessionState.GetBool("SHARED_NAV_QA",false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}}
    static void Check(bool ok,string message){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    static void Shot(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Object.Destroy(t);}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);while(intro&&intro.gameObject.activeSelf)yield return null;yield return new WaitForSecondsRealtime(1);
        var gc=GameplayCanvas.single;gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!GameManager.single.HasSeenThis("Tutorial-"+i))GameManager.single.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.overlay.SetActive(false);TutorialManager.single.tutorialCharacter.SetActive(false);foreach(var t in TutorialManager.single.tutorials)t.SetActive(false);}
        var sizes=new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)};
        foreach(var size in sizes){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.5f);
            Vector2 baseline=Vector2.zero;float height=0;
            foreach(int page in new[]{2,0,1,3,4}){
                var nav=Object.FindObjectsByType<StorybookNavigation>(FindObjectsSortMode.None).FirstOrDefault(n=>n.selectedIndex==page&&n.gameObject.activeInHierarchy);
                Check(nav!=null,"Active shared navigation page "+page);if(!nav){EditorApplication.isPlaying=false;yield break;}
                yield return new WaitForEndOfFrame();Shot(page+"-"+size.x+"x"+size.y);
                var corners=new Vector3[4];((RectTransform)nav.transform).GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[1]);
                if(page==2){baseline=a;height=b.y-a.y;}else Check(Vector2.Distance(a,baseline)<1&&Mathf.Abs((b.y-a.y)-height)<1,"Identical menu bounds on page "+page+" "+size);
                Check(nav.bookmarks.Count(x=>x.activeSelf)==1&&nav.bookmarks[page].activeSelf,"Single correct selected bookmark");
                foreach(var button in nav.buttons){var center=RectTransformUtility.WorldToScreenPoint(null,button.transform.TransformPoint(((RectTransform)button.transform).rect.center));var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Raycast "+button.name);}
                int target=page==2?0:page==0?1:page==1?3:page==3?4:2;
                int count=StorybookNavigation.ClickSoundCount;nav.buttons[target].onClick.Invoke();Check(StorybookNavigation.ClickSoundCount==count+1,"One click sound "+page+" to "+target);
                yield return new WaitForSecondsRealtime(page==4?3:2);
            }
        }
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
