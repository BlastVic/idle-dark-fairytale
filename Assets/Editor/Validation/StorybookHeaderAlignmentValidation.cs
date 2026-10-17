using Assets.Scripts.Game.Enum;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StorybookHeaderAlignmentValidation
{
    const string Out = "output/header-alignment/";
    static bool running;
    static StorybookHeaderAlignmentValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("HEADER_ALIGNMENT_QA", false)) return;
            SessionState.SetBool("HEADER_ALIGNMENT_QA", false);
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
            SessionState.SetBool("HEADER_ALIGNMENT_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("HEADER_ALIGNMENT_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static void Check(bool ok,string message)=>File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");
    static IEnumerator Run()
    {
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        foreach(var page in new[]{"Equipment","Skills","Shop"}){
            if(page=="Equipment")Router.single.InventoryButton();
            else if(page=="Skills")Router.single.SkillsButton();
            else Router.single.CashMallButton();
            yield return new WaitForSecondsRealtime(2);
            GameObject root=page=="Equipment"?GameplayCanvas.single.inventoryPanel:page=="Skills"?GameplayCanvas.single.skillsPanel:Object.FindFirstObjectByType<StorybookShopUI>().gameObject;
            var eq=root.GetComponent<StorybookEquipmentLayout>();var sk=root.GetComponent<StorybookSkillsLayout>();
            var panel=eq?eq.header:sk.header;var content=eq?eq.headerContent:sk.headerContent;
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
                StorybookEquipmentValidation.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.4f);
                if(eq)eq.Apply();else sk.Apply();yield return new WaitForEndOfFrame();
                Check(Mathf.Abs(content.rect.width*content.localScale.x-panel.rect.width)<.1f,page+" content spans frame "+size);
                var back=(RectTransform)content.Find("BackToShelter");var title=(RectTransform)content.Find("Title");var level=(RectTransform)content.Find("PlayerLevel");
                Check(Mathf.Abs(back.anchoredPosition.x-19)<.1f&&back.anchorMin.x==0,page+" back aligned to left "+size);
                Check(level.anchorMin.x==1&&Mathf.Abs(level.anchoredPosition.x+155)<.1f,page+" player info aligned to right "+size);
                var center=panel.InverseTransformPoint(title.TransformPoint(title.rect.center));
                Check(Mathf.Abs(center.x-panel.rect.center.x)<.1f,page+" title centered "+size);
                foreach(var name in new[]{"BackToShelter","Portrait","PlayerLevel","SubjectNumber","Title"}){
                    var r=(RectTransform)content.Find(name);var corners=new Vector3[4];r.GetWorldCorners(corners);
                    Check(System.Array.TrueForAll(corners,c=>{var p=panel.InverseTransformPoint(c);return p.x>=panel.rect.xMin-.1f&&p.x<=panel.rect.xMax+.1f;}),page+" header element fits "+name+" "+size);
                    Check(Mathf.Abs(r.lossyScale.x-r.lossyScale.y)<.001f,page+" no artwork stretch "+name);
                }
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+page+"-"+size.x+"x"+size.y+".png",image.EncodeToPNG());Object.Destroy(image);
                var safe=new Rect(32,50,size.x-80,size.y-105);
                if(eq)eq.SetValidationSafeArea(safe);else sk.SetValidationSafeArea(safe);yield return new WaitForEndOfFrame();
                var pts=new Vector3[4];panel.GetWorldCorners(pts);
                Check(System.Array.TrueForAll(pts,p=>{var q=RectTransformUtility.WorldToScreenPoint(null,p);return q.x>=safe.xMin-.1f&&q.x<=safe.xMax+.1f&&q.y<=safe.yMax+.1f;}),page+" respects asymmetric safe area "+size);
                if(eq)eq.SetValidationSafeArea(null);else sk.SetValidationSafeArea(null);
            }
            content.Find("BackToShelter").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            Check(GameManager.single.gameState==GameStateType.CAMP,page+" back returns to shelter");
        }
        StorybookEquipmentValidation.Resolution(1080,1920);File.AppendAllText(Out+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}
