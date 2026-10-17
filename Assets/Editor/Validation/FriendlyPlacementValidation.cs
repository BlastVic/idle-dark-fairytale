using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Spine.Unity;

// Explicit local visual check; never changes production assets or saves the test session.
[InitializeOnLoad]
public static class FriendlyPlacementValidation
{
    const string Dir = "output/friendly-placement/";
    static bool running;
    static FriendlyPlacementValidation() {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s => {
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("FRIENDLY_PLACEMENT_QA", false)) {
                running = false;
                SessionState.SetBool("FRIENDLY_PLACEMENT_QA", false);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
            }
        };
    }
    static void Tick() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir + "validate.request")) {
            File.Delete(Dir + "validate.request");
            File.WriteAllText(Dir + "validation.txt", "Actual gameplay placement; save disabled\n");
            SessionState.SetBool("FRIENDLY_PLACEMENT_QA", true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && running && Player.single) {
            Player.single.StopAllCoroutines(); Player.single.skeletonAnimation.timeScale = 0;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
                enemy.StopAttacking(); if (enemy.ab) enemy.ab.trainingTarget = true;
            }
        }
        if (EditorApplication.isPlaying && !running && SessionState.GetBool("FRIENDLY_PLACEMENT_QA", false) && GameManager.single && GameplayCanvas.single) {
            running = true; GameManager.single.StartCoroutine(Run());
        }
    }
    static void Pose(SkeletonAnimation skeleton, string name, float time = 0) {
        skeleton.timeScale = 0;
        skeleton.AnimationState.ClearTracks(); skeleton.Skeleton.SetToSetupPose();
        var entry = skeleton.AnimationState.SetAnimation(0, name, false);
        entry.TrackTime = time; skeleton.Update(0); skeleton.LateUpdate();
    }
    static void BoundsLog(Camera camera, SkeletonAnimation skeleton, string name) {
        var b = skeleton.GetComponent<MeshRenderer>().bounds;
        var a = camera.WorldToViewportPoint(b.min); var z = camera.WorldToViewportPoint(b.max);
        File.AppendAllText(Dir + "validation.txt", $"{name}: x={a.x:F4}..{z.x:F4}, y={a.y:F4}..{z.y:F4}\n");
    }
    static IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2);
        var intro = Object.FindFirstObjectByType<CursedBookIntro>(); if (intro && !intro.Finished) intro.SkipStory();
        yield return new WaitForSecondsRealtime(6);
        var tutorial = TutorialManager.single; tutorial.automatic = false; tutorial.view.Hide();
        var ui = GameplayCanvas.single;
        ui.CancelInvoke("TryShowingXpBoosterNotification"); ui.xpBoosterNotification.SetActive(false);
        ui.dailyCherriesPop.SetActive(false);
        var wm = WaveManager.single;
        var level = JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        level.levelKey = "FriendlyPlacementQA"; level.levelName = "站位校准"; level.mapGraphicKey = "CH1_Node05";
        level.waves = new[] { new Wave { enemies = new[] { "MobTallowkin" } } }; wm.waveDb.Add(level);
        var assets = LevelController.Instance.AssetManager;
        assets.HideMapPackage(); if (ui.homeUI) ui.homeUI.gameObject.SetActive(false);
        ui.ToggleButtonGrid(false); ui.ToggleFusionBarUI(false); ui.victoryPanel.SetActive(false); ui.defeatPanel.SetActive(false);
        SkillsController.Instance.cindrelLevel = 0; SkillsController.Instance.shadowWolfLevel = 0;
        wm.CallStartLevel(level.levelKey);
        yield return new WaitForSecondsRealtime(3);
        var player = Player.single;
        player.StopAllCoroutines(); player.ab.currentStat.hpNow = 1000000;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
            e.StopAttacking(); e.ab.currentStat.hpNow = e.ab.currentStat.hpMax = 1000000;
            Pose(e.skeletonAnimation, "Idle1");
        }
        assets.SpawnGeneralObj("PetCindrel"); assets.SpawnGeneralObj("PetShadowWolf");
        yield return new WaitForSecondsRealtime(1);
        var dragon = Object.FindFirstObjectByType<PetCindrelCompanion>();
        var wolf = Object.FindFirstObjectByType<WolfPetCompanion>();
        dragon.StopAllCoroutines(); wolf.StopAllCoroutines();
        foreach (var behaviour in dragon.GetComponents<MonoBehaviour>()) if (behaviour != dragon) behaviour.StopAllCoroutines();
        var cam = assets.battleCampCamera.GetComponent<Camera>();
        var original = Object.Instantiate(assets.battleStyle); original.playerFeet.x = .23f; original.playerRetreatLeftMargin = .14f;
        foreach (var size in new[] { new Vector2Int(720,1280), new Vector2Int(768,1024), new Vector2Int(720,1440) }) {
            typeof(MonsterPreviewValidation).GetMethod("SetResolution", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {size.x,size.y});
            yield return new WaitForSecondsRealtime(.6f);
            for (int pass = 0; pass < 2; pass++) {
                var style = pass == 0 ? original : assets.battleStyle;
                style.PlacePlayer(player, cam);
                Pose(player.skeletonAnimation, "Idle1");
                dragon.viewportFeet.x = pass == 0 ? .22f : .18f;
                wolf.viewportFeet.x = pass == 0 ? .28f : .24f;
                dragon.Place(); wolf.Place(); Pose(dragon.skeleton, "Idle1"); Pose(wolf.skeleton, "Idle1");
                int slot = 0; foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) style.PlaceEnemy(e,cam,slot++);
                yield return new WaitForSecondsRealtime(.3f);
                string label = (pass == 0 ? "before-" : "after-") + size.x + "x" + size.y;
                BoundsLog(cam, player.skeletonAnimation, label+" hero"); BoundsLog(cam,dragon.skeleton,label+" dragon"); BoundsLog(cam,wolf.skeleton,label+" wolf");
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Dir+label+".png",shot.EncodeToPNG()); Object.Destroy(shot);
            }
            Pose(player.skeletonAnimation,"Victory",player.skeletonAnimation.Skeleton.Data.FindAnimation("Victory").Duration);
            BoundsLog(cam,player.skeletonAnimation,"victory "+size);
            yield return new WaitForEndOfFrame();
            var victory = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Dir+"victory-"+size.x+"x"+size.y+".png",victory.EncodeToPNG()); Object.Destroy(victory);
        }
        Object.Destroy(original); File.AppendAllText(Dir+"validation.txt","COMPLETE\n"); EditorApplication.isPlaying = false;
    }
}
