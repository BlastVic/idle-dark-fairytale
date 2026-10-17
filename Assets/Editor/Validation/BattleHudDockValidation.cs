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
        while (intro && intro.gameObject.activeSelf) yield return null;
        var gc = GameplayCanvas.single;
        TutorialManager.single.automatic = false; TutorialManager.single.view.Hide();
        BlackForestMushroomBuilder.RunFirstLevel();
        yield return new WaitForSecondsRealtime(2);
        Time.timeScale = 0;
        var layout = gc.battleHud.GetComponent<BattleHudSafeArea>();
        foreach (var size in new[] {new Vector2Int(1080,1920),new Vector2Int(768,1024),new Vector2Int(720,1440)}) {
            StorybookEquipmentValidation.Resolution(size.x,size.y);
            yield return new WaitForSecondsRealtime(.4f);
            layout.Apply(); Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            var corners = new Vector3[4]; layout.bottomPanel.GetWorldCorners(corners);
            float actual = RectTransformUtility.WorldToScreenPoint(null,corners[0]).y;
            float expected = Screen.safeArea.yMin + layout.bottomInset * layout.core.localScale.y * gc.battleHud.GetComponent<Canvas>().scaleFactor;
            bool ok = Mathf.Abs(actual-expected)<1 && gc.battleHud.gameObject.activeInHierarchy;
            File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+size+" bottom="+actual+" expected="+expected+"\n");
            gc.ToggleBattleUi(false);
            yield return new WaitForEndOfFrame();
            bool noBanner = !System.Array.Exists(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None), t => t.name == "Banner Ad Background");
            File.AppendAllText(Out+"validation.txt",(noBanner && !gc.hpXpLevelUI.activeSelf?"PASS ":"FAIL ")+"No legacy banner or HP after HUD hides "+size+"\n");
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
