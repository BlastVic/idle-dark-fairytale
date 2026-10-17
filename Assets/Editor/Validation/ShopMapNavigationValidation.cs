using System.Collections;
using System.IO;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;

// Runs only after an explicit local validation request; never edits authored UI.
[InitializeOnLoad]
public static class ShopMapNavigationValidation
{
    const string Out = "output/shop-map-navigation/";
    const string Key = "SHOP_MAP_NAVIGATION_QA";
    static bool running;
    static int failures, errors, cameraGaps;
    static bool monitorCameras;
    static double deadline;
    static ShopMapNavigationValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
            Application.logMessageReceived -= Error;
            running = false;
        };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Out + "validate.request")) {
            File.Delete(Out + "validate.request");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            File.WriteAllText(Out + "validation.txt", "Shop -> map route regression; persistence disabled.\n");
            SessionState.SetBool(Key, true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (!running && GameManager.single && GameplayCanvas.single) {
            running = true; failures = errors = 0;
            deadline = EditorApplication.timeSinceStartup + 120;
            Application.logMessageReceived += Error;
            GameManager.single.StartCoroutine(Run());
        }
        if (running && EditorApplication.timeSinceStartup > deadline) {
            Check(false, "watchdog timeout"); Finish();
        }
    }
    static void Error(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        if (message.Contains("Ad is unavailable")) return;
        errors++; File.AppendAllText(Out + "validation.txt", "ERROR " + message + "\n");
    }
    static void Check(bool ok, string message)
    {
        if (!ok) failures++;
        File.AppendAllText(Out + "validation.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
    }
    static IEnumerator OpenShop()
    {
        Router.single.CashMallButton();
        float end = Time.realtimeSinceStartup + 15;
        while ((!CashMallPackage.single || GameManager.single.gameState != GameStateType.CASH_MALL) && Time.realtimeSinceStartup < end) yield return null;
        yield return new WaitForSecondsRealtime(.6f);
        Check(CashMallPackage.single, "shop opens");
    }
    static IEnumerator MonitorCameras()
    {
        while (monitorCameras) {
            yield return new WaitForEndOfFrame();
            bool rendering = false;
            foreach (var camera in Camera.allCameras)
                rendering |= camera.isActiveAndEnabled && camera.targetDisplay == 0 && camera.targetTexture == null;
            if (!rendering) cameraGaps++;
        }
    }
    static IEnumerator Run()
    {
        var gm = GameManager.single; var gc = GameplayCanvas.single;
        if (!gm.HasSeenThis(CursedBookPrologue.SeenKey)) gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        var intro = Object.FindFirstObjectByType<CursedBookIntro>();
        while (intro && intro.gameObject.activeSelf) yield return null;
        TutorialManager.single.automatic = false; TutorialManager.single.view.Hide();
        Check(!GameObject.Find("MenuPackageCam"), "Retired menu camera removed from loaded scene");
        cameraGaps=0; monitorCameras=true; gm.StartCoroutine(MonitorCameras());
        for (int i = 0; i < 20; i++) if (!gm.HasSeenThis("Tutorial-" + i)) gm.thingsSeenPermanent.Add("Tutorial-" + i);
        gc.CancelInvoke("TryShowingXpBoosterNotification"); gc.dailyCherriesPop.SetActive(false);
        foreach (var size in new[] { new Vector2Int(720,1280), new Vector2Int(768,1024), new Vector2Int(720,1440) }) {
            StorybookEquipmentValidation.Resolution(size.x, size.y);
            yield return OpenShop();
            if (!CashMallPackage.single) { Finish(); yield break; }
            var ui = CashMallPackage.single.view;
            ui.SetBusy(true, "validation"); ui.Navigate(3);
            Check(gm.gameState == GameStateType.CASH_MALL && !TransitionCanvas.single.isBlack, "purchase busy blocks map navigation");
            ui.SetBusy(false, "");
            ui.navigation[3].onClick.Invoke();
            ui.Navigate(3); ui.Navigate(2); // repeated clicks must not queue a return to camp
            Check(!ui.navigation[3].interactable && !ui.back.interactable, "navigation locks immediately");
            bool campSeen = false; int frames = 0;
            float end = Time.realtimeSinceStartup + 3;
            while (Time.realtimeSinceStartup < end) {
                campSeen |= gm.gameState == GameStateType.CAMP || (gc.homeUI && gc.homeUI.gameObject.activeInHierarchy);
                frames++; yield return null;
            }
            Check(!campSeen, size + " no intermediate camp state or home activation across " + frames + " frames");
            Check(gm.gameState == GameStateType.MAP && LevelController.Instance.AssetManager.mapPackageObj.activeInHierarchy, size + " reaches map");
            Check(!CashMallPackage.single && !Object.FindFirstObjectByType<StorybookShopUI>(), "shop destroyed without leftover UI");
            Check(!TransitionCanvas.single.group.blocksRaycasts, "transition releases input");
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Out + size.x + "x" + size.y + "-map.png", shot.EncodeToPNG()); Object.Destroy(shot);
            yield return OpenShop();
            Check(gm.gameState == GameStateType.CASH_MALL, "map to shop reaches shop");
            yield return new WaitForEndOfFrame();
            var shopShot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Out + size.x + "x" + size.y + "-shop.png", shopShot.EncodeToPNG()); Object.Destroy(shopShot);
            CashMallPackage.single.view.navigation[3].onClick.Invoke();
            yield return new WaitForSecondsRealtime(2);
            Router.single.CampButton(); yield return new WaitForSecondsRealtime(2);
            Check(gm.gameState == GameStateType.CAMP && gc.homeUI.gameObject.activeInHierarchy, "map return to camp preserved");
        }
        yield return OpenShop();
        if (CashMallPackage.single) CashMallPackage.single.view.back.onClick.Invoke();
        yield return new WaitForSecondsRealtime(2);
        Check(gm.gameState == GameStateType.CAMP && !CashMallPackage.single, "shop back still returns to camp");
        Finish();
    }
    static void Finish()
    {
        monitorCameras=false;
        Check(cameraGaps==0,"Every rendered transition frame has a display camera; gaps="+cameraGaps);
        File.AppendAllText(Out + "validation.txt", (failures == 0 && errors == 0 ? "PASS" : "FAIL") + " COMPLETE failures=" + failures + " errors=" + errors + "\n");
        Application.logMessageReceived -= Error;
        EditorApplication.isPlaying = false;
    }
}
