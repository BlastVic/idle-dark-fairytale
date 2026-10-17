using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BattleHudDockValidation
{
    const string Out = "output/battle-hud-dock/";
    static bool running;
    static BattleHudDockValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("HUD_DOCK_QA", false)) return;
            SessionState.SetBool("HUD_DOCK_QA", false);
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
            SessionState.SetBool("HUD_DOCK_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && SessionState.GetBool("HUD_DOCK_QA", false) && !running && GameplayCanvas.single && GameManager.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static IEnumerator Run()
    {
        var intro = Object.FindFirstObjectByType<CursedBookIntro>();
        yield return new WaitForSecondsRealtime(2);
        if(intro&&!intro.Finished)intro.SkipStory();yield return new WaitForSecondsRealtime(2);
        var gc = GameplayCanvas.single;
        TutorialManager.single.automatic = false; TutorialManager.single.view.Hide();
        GameManager.single.lastBattleKey="Black Forest Mushroom Test";Router.single.LastBattle();
        yield return new WaitForSecondsRealtime(2);
        Player.single.ab.trainingTarget=true;WaveManager.single.StopAllCoroutines();
        Time.timeScale = 0;
        var layout = gc.battleHud.GetComponent<BattleHudSafeArea>();
        foreach (var size in new[] {new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}) {
            typeof(StorySkipValidation).GetMethod("Size",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{size.x,size.y});
            yield return new WaitForSecondsRealtime(.4f);
            layout.Apply(); Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            var corners = new Vector3[4]; layout.bottomPanel.GetWorldCorners(corners);
            float actual = RectTransformUtility.WorldToScreenPoint(null,corners[0]).y;
            float expected = Screen.safeArea.yMin + layout.bottomInset * layout.core.localScale.y * gc.battleHud.GetComponent<Canvas>().scaleFactor;
            bool ok = Mathf.Abs(actual-expected)<1 && gc.battleHud.gameObject.activeInHierarchy;
            File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+size+" bottom="+actual+" expected="+expected+"\n");
            layout.topPanel.GetWorldCorners(corners);
            float headerTop=RectTransformUtility.WorldToScreenPoint(null,corners[1]).y;
            float expectedTop=Screen.safeArea.yMax-layout.topInset*layout.core.localScale.y*gc.battleHud.GetComponent<Canvas>().scaleFactor;
            File.AppendAllText(Out+"validation.txt",(Mathf.Abs(headerTop-expectedTop)<1?"PASS ":"FAIL ")+"header dock "+size+" actual="+headerTop+" expected="+expectedTop+"\n");
            gc.itemToast.Present(new Item{itemGrade=ItemGrade.UI_ONLY,m_ItemFinalTitle="获得水晶 × 25"});yield return new WaitForSecondsRealtime(.3f);gc.itemToast.Place();
            var toast=new Vector3[4];gc.itemToast.card.GetWorldCorners(toast);File.AppendAllText(Out+"validation.txt",(toast[1].y<corners[0].y?"PASS ":"FAIL ")+"toast below header "+size+"\n");
            yield return new WaitForEndOfFrame();var normal=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"header-"+size.x+"x"+size.y+".png",normal.EncodeToPNG());Object.Destroy(normal);
            var boss=gc.battleHud.bossHud;boss.BeginWave(new Wave{enemies=new[]{"MobRivenfang"}});yield return new WaitForSecondsRealtime(1.6f);boss.Place();gc.itemToast.Place();var bossCorners=new Vector3[4];boss.panel.GetWorldCorners(bossCorners);gc.itemToast.card.GetWorldCorners(toast);
            File.AppendAllText(Out+"validation.txt",(bossCorners[1].y<corners[0].y&&toast[1].y<bossCorners[0].y?"PASS ":"FAIL ")+"header boss toast ordering "+size+"\n");
            yield return new WaitForEndOfFrame();var bossShot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"boss-header-"+size.x+"x"+size.y+".png",bossShot.EncodeToPNG());Object.Destroy(bossShot);boss.Hide();gc.itemToast.gameObject.SetActive(false);
            gc.ToggleBattleUi(false);
            yield return new WaitForEndOfFrame();
            bool noBanner = !System.Array.Exists(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None), t => t.name == "Banner Ad Background");
            File.AppendAllText(Out+"validation.txt",(noBanner && (!gc.hpXpLevelUI || !gc.hpXpLevelUI.activeSelf)?"PASS ":"FAIL ")+"No legacy banner or HP after HUD hides "+size+"\n");
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Out+"no-banner-"+size.x+"x"+size.y+".png",shot.EncodeToPNG()); Object.Destroy(shot);
            gc.ToggleBattleUi(true);
        }
        Time.timeScale = 1;
        StorybookEquipmentValidation.Resolution(1080,1920);
        File.AppendAllText(Out+"validation.txt","DONE\n");
        EditorApplication.isPlaying = false;
    }
}
