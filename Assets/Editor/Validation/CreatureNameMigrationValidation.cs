using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Services;
using Scripts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Spine.Unity;

/// <summary>Explicit migration authoring and isolated runtime acceptance. No automatic asset rebuilding.</summary>
[InitializeOnLoad]
public static class CreatureNameMigrationValidation
{
    public const string Dir = "output/creature-display-names-2026-10-16/";
    const string Session = "CREATURE_NAMES_QA";
    static bool running;
    static int errors;
    static double deadline;
    static string mode;
    static readonly int[] Width = {720,768,720}, Height = {1280,1024,1440};
    static CreatureNameMigrationValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, stack, type) => {
            if (running && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) { errors++; Log("ERROR " + message); }
        };
    }
    static void Log(string message) => File.AppendAllText(Dir + mode + ".txt", message + "\n");
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Log("PASS " + message); }
    public static void Resolution(int w, int h) => typeof(MonsterPreviewValidation).GetMethod("SetResolution", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] {w,h});
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(Session + "Return", false)) {
            SessionState.SetBool(Session + "Return", false);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
            EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);
        }
        string request = Dir + "command.request";
        if (File.Exists(request)) {
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try {
                if (command == "author") Author();
                else if (command == "story") {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first");
                    EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
                    Directory.CreateDirectory("output/chapter01-ending/");
                    File.WriteAllText("output/chapter01-ending/validate.request", "");
                }
                else if (command == "stop") EditorApplication.isPlaying = false;
                else Begin(command);
            } catch (Exception e) { File.WriteAllText(Dir + "command-result.txt", "FAIL " + e); Debug.LogException(e); }
        }
        var requested = SessionState.GetString(Session, "");
        if (!running && requested.Length > 0 && EditorApplication.isPlaying) {
            var owner = requested == "preview" ? (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>() : GameManager.single;
            if (!owner) return;
            mode = requested; SessionState.SetString(Session, ""); running = true; errors = 0;
            deadline = EditorApplication.timeSinceStartup + 300;
            File.WriteAllText(Dir + mode + ".txt", "2026-10-16 names; real Unity " + mode + "; temporary runtime data only\n");
            owner.StartCoroutine(Guard(mode == "preview" ? Preview() : Combat()));
        }
        if (running && EditorApplication.timeSinceStartup > deadline) Finish("FAIL timeout");
    }
    static void Begin(string requested)
    {
        if (requested != "preview" && requested != "combat") throw new Exception("Unknown command " + requested);
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new Exception("Save current scene before validation");
        Directory.CreateDirectory(Dir);
        Directory.CreateDirectory("output/wolf-run-entrance/");
        Directory.CreateDirectory("output/monster-entrance-rules/");
        SessionState.SetString(Session, requested);
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
        EditorSceneManager.OpenScene(requested == "preview" ? MonsterTestSceneBuilder.ScenePath : "Assets/Scenes/Gameplay.unity");
        EditorApplication.isPlaying = true;
    }
    static void Finish(string result)
    {
        Log(result); running = false;
        SessionState.SetBool(Session + "Return", true);
        EditorApplication.isPlaying = false;
    }
    static IEnumerator Guard(IEnumerator test)
    {
        while (true) {
            object current;
            try { if (!test.MoveNext()) break; current = test.Current; }
            catch (Exception e) { Finish("FAIL " + e); yield break; }
            yield return current;
        }
        Finish(errors == 0 ? "COMPLETE PASS; runtime errors=0" : "FAIL runtime errors=" + errors);
    }
    [MenuItem("Tools/Dark Fairytale/Monsters/Apply Approved Creature Display Names")]
    public static void Author()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first");
        var ui = PrefabUtility.LoadPrefabContents(MonsterPreviewUiBuilder.HudPath);
        try {
            var hud = ui.GetComponent<MonsterPreviewHud>();
            foreach (var card in hud.cards.Where(c => c && c.data)) {
                card.displayName = CreatureNames.DisplayName(card.data.name);
                card.title.text = card.displayName;
            }
            hud.selectedTitle.text = "红帽菇";
            PrefabUtility.SaveAsPrefabAsset(ui, MonsterPreviewUiBuilder.HudPath);
        } finally { PrefabUtility.UnloadPrefabContents(ui); }
        foreach (var entry in CreatureNames.Entries.Where(e => !e.IsPet)) {
            var guid = LevelDataDescriptions.EnemyDb.Single(e => e.key == entry.Key).assetRef.AssetGUID;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try {
                var bar = prefab.GetComponentInChildren<EnemyLifebar>(true);
                prefab.GetComponent<Enemy>().bossName = bar && (bar.isBoss || bar.isMiniBoss) ? entry.DisplayName : "";
                if (bar && bar.miniBossName) bar.miniBossName.text = entry.DisplayName;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            } finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Dir + "command-result.txt", "PASS authored Chinese display names; GUIDs retained");
    }
    static IEnumerator Preview()
    {
        yield return null;
        Check((int)Scripts.Skills.SkillsTypeEnum.PetCindrel == 9, "Skill serialized enum remains 9");
        var legacy = SaveManager.DeserializeUserData("{\"taironTheDragonLevel\":7}");
        Check(legacy.cindrelLevel == 7, "Legacy pet skill save loads as cindrelLevel=7");
        Check(JsonUtility.FromJson<UserData>(JsonUtility.ToJson(legacy)).cindrelLevel == 7, "Renamed save field roundtrip");
        Check(SaveManager.DeserializeUserData("{\"taironTheDragonLevel\":7,\"cindrelLevel\":3}").cindrelLevel == 3, "New save value wins over legacy field");
        var t = UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
        foreach (var entry in CreatureNames.Entries) {
            var db = (entry.IsPet ? LevelDataDescriptions.GeneralObjectsDb : LevelDataDescriptions.EnemyDb).Where(e => e.key == entry.Key).ToArray();
            Check(db.Length == 1, entry.Key + " unique database key");
            var address = Addressables.LoadAssetAsync<GameObject>(entry.Key);
            var reference = Addressables.LoadAssetAsync<GameObject>(db[0].assetRef.RuntimeKey);
            yield return address; yield return reference;
            Check(address.Status == AsyncOperationStatus.Succeeded && reference.Status == AsyncOperationStatus.Succeeded && address.Result == reference.Result, entry.Key + " address and database resolve same prefab");
            var prefab = address.Result;
            Check(prefab.name == entry.Key, entry.Key + " prefab name");
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) throw new Exception("Missing script " + child.name);
            var skeleton = prefab.GetComponentInChildren<SkeletonAnimation>(true);
            Check(skeleton && skeleton.skeletonDataAsset.name == entry.Key + "_SkeletonData", entry.Key + " skeleton reference");
            Check(skeleton.skeletonDataAsset.atlasAssets.All(a => a && a.Materials.All(m => m && m.mainTexture)), entry.Key + " atlas/material/texture references");
            int index = Array.FindIndex(t.monsters, a => a == skeleton.skeletonDataAsset);
            Check(index >= 0 && t.monsterPrefabs.Contains(prefab), entry.Key + " test scene references");
            var card = t.hud.cards.Single(c => c.data == skeleton.skeletonDataAsset);
            Check(card.displayName == entry.DisplayName && card.title.text == entry.DisplayName && card.portrait.sprite, entry.Key + " authored Chinese card and portrait");
            foreach (var query in new[] {entry.Key, entry.DisplayName}) {
                t.hud.searchField.text = query;
                Check(t.hud.cards.Count(c => c.gameObject.activeSelf) == 1 && card.gameObject.activeSelf, entry.Key + " search " + query);
            }
            card.button.onClick.Invoke(); t.hud.searchField.text = "";
            Check(t.character.valid && t.Error == null && t.hud.selectedTitle.text == entry.DisplayName, entry.Key + " runtime selection and Chinese title");
            for (int a = 0; a < t.AnimationNames.Length; a++) {
                t.Play(a); t.character.Update(.1f); t.character.LateUpdate();
                Check(t.character.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0, entry.Key + " clip " + t.AnimationNames[a]);
            }
            t.Play(Array.IndexOf(t.AnimationNames, "Idle1"));
            if (prefab.GetComponent<DropIn>() && prefab.GetComponent<DropIn>().runFromRight) {
                var runTest = RunPreview(t, entry.Key);
                while (runTest.MoveNext()) yield return runTest.Current;
            }
            if (!entry.IsPet) {
                t.SetSpeed(2); t.TestEntrance();
                if (prefab.GetComponent<DropIn>().spawnInPlace) {
                    Vector3 spawnPosition = t.character.transform.position;
                    Check(t.character.AnimationState.GetCurrent(0).Animation.Name.ToLowerInvariant() == "spawn", entry.Key + " preview plays spawn in place");
                    yield return new WaitForSeconds(.1f); yield return new WaitForEndOfFrame();
                    Check((t.character.transform.position-spawnPosition).sqrMagnitude < .000001f, entry.Key + " spawn has no falling displacement");
                    ScreenCapture.CaptureScreenshot("output/monster-entrance-rules/"+entry.Key+"-spawn-preview.png");
                }
                float until = Time.realtimeSinceStartup + 8;
                while (t.EntranceStatus != "完成 · 待机" && Time.realtimeSinceStartup < until) yield return null;
                Check(t.EntranceStatus == "完成 · 待机", entry.Key + " entrance completes");
                t.SetSpeed(1);
            } else Check(!t.hud.entranceButton.interactable, "Pet entrance button disabled (no enemy DropIn)");
            for (int r = 0; r < 3; r++) {
                Resolution(Width[r], Height[r]); yield return new WaitForSeconds(.5f);
                Canvas.ForceUpdateCanvases(); t.stage.Apply(); yield return new WaitForEndOfFrame();
                CheckVisible((RectTransform)t.hud.selectedTitle.transform, entry.Key + " title");
                CheckVisible((RectTransform)t.hud.entranceButton.transform, entry.Key + " entrance button");
                CheckVisible((RectTransform)t.hud.pauseButton.transform, entry.Key + " playback controls");
                Check(Screen.width == Width[r] && Screen.height == Height[r], entry.Key + " actual ratio " + Screen.width + "x" + Screen.height);
                ScreenCapture.CaptureScreenshot(Dir + entry.Key + "-preview-" + Width[r] + "x" + Height[r] + ".png");
                yield return new WaitForEndOfFrame();
            }
            Addressables.Release(address); Addressables.Release(reference);
        }
    }
    static IEnumerator RunPreview(MonsterAnimationTest t, string key)
    {
        for (int r = 0; r < 3; r++) {
            Resolution(Width[r], Height[r]); yield return new WaitForSeconds(.5f);
            t.SetSpeed(1); t.SetPaused(false); t.Play(Array.IndexOf(t.AnimationNames, "Idle1"));
            yield return new WaitForEndOfFrame();
            Vector3 target = t.character.transform.position;
            t.TestEntrance();
            var renderer = t.character.GetComponent<MeshRenderer>();
            Check(t.stage.previewCamera.WorldToViewportPoint(renderer.bounds.min).x > 1, key + " whole body starts beyond right edge " + Width[r]);
            Check(t.EntranceStatus == "跑步进场" && t.character.AnimationState.GetCurrent(0).Animation.Name == "Run_Battle_00", key + " native run animation");
            float previousX = t.character.transform.position.x;
            yield return new WaitForSeconds(t.monsterPrefabs.First(p => p.name == key).GetComponent<DropIn>().runDuration * .5f); yield return new WaitForEndOfFrame();
            Check(t.character.transform.position.x < previousX && Mathf.Abs(t.character.transform.position.y - target.y) < .001f, key + " runs left on ground");
            ScreenCapture.CaptureScreenshot("output/wolf-run-entrance/" + key + "-running-" + Width[r] + "x" + Height[r] + ".png");
            t.SetPaused(true); yield return new WaitForEndOfFrame();
            Vector3 paused = t.character.transform.position;
            float track = t.character.AnimationState.GetCurrent(0).TrackTime;
            yield return new WaitForSeconds(.2f); yield return new WaitForEndOfFrame();
            Check((t.character.transform.position-paused).sqrMagnitude < .000001f && Mathf.Abs(t.character.AnimationState.GetCurrent(0).TrackTime-track)<.001f, key + " pause freezes run and movement");
            t.SetPaused(false); t.SetSpeed(2); yield return new WaitForSeconds(.7f); yield return new WaitForEndOfFrame();
            Check(t.EntranceStatus == "完成 · 待机" && (t.character.transform.position-target).sqrMagnitude < .000001f, key + " arrives exactly at standing position");
            t.Replay(); Check(t.EntranceStatus == "跑步进场", key + " replay restarts running");
            t.Play(Array.IndexOf(t.AnimationNames, "Idle1")); yield return new WaitForEndOfFrame();
            Check(!t.EntranceMode && (t.character.transform.position-target).sqrMagnitude < .000001f, key + " cancel resets position");
            t.SetSpeed(1);
        }
    }

    static void CheckVisible(RectTransform rect, string label)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Check(corners.All(p => p.x >= -1 && p.x <= Screen.width + 1 && p.y >= -1 && p.y <= Screen.height + 1), label + " within screen");
    }
    static void HoldPlayer()
    {
        var player = Player.single;
        player.StopAllCoroutines(); player.skeletonAnimation.timeScale = 0;
        player.ab.currentStat.hpMax = player.ab.currentStat.hpNow = 100000;
        player.ab.currentStat.block = 0; player.ab.currentStat.def = 0;
        if (PetCindrel.single) { PetCindrel.single.StopAllCoroutines(); }
    }
    static IEnumerator Combat()
    {
        while (!Player.single || Player.single.ab.currentStat == null || !WaveManager.single) yield return null;
        var intro = UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();
        while (intro && !intro.Finished) yield return null;
        SkillsController.Instance.ResetSkills(); SkillsController.Instance.cindrelLevel = 1;
        GameManager.single.xpNow = 0; GameManager.single.xpMax = 1000000;
        var wm = WaveManager.single;
        var entries = CreatureNames.Entries.Where(e => !e.IsPet).ToArray();
        var level = JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));
        level.levelKey = level.levelName = "Creature name migration QA";
        level.mapGraphicKey = "CH1_Node04";
        level.waves = entries.Select(e => new Wave { enemies = new[] { e.Key } }).Concat(new[] {new Wave {enemies = new[] {"MobStillmere","MobMurmlet","MobTallowkin"}}}).ToArray();
        wm.waveDb.Add(level);
        var ui = GameplayCanvas.single;
        LevelController.Instance.AssetManager.HideMapPackage();
        if (ui.homeUI) ui.homeUI.gameObject.SetActive(false);
        ui.ToggleButtonGrid(false); ui.ToggleFusionBarUI(false); ui.victoryPanel.SetActive(false); ui.defeatPanel.SetActive(false); ui.dailyCherriesPop.SetActive(false);
        Resolution(720,1280); wm.CallStartLevel(level.levelKey);
        foreach (var entry in entries) {
            Enemy enemy = null;
            while (!enemy || enemy.ab.currentStat == null) {
                HoldPlayer(); enemy = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).FirstOrDefault(e => e.name == entry.Key + "(Clone)"); yield return null;
            }
            enemy.ab.currentStat.hpMax = enemy.ab.currentStat.hpNow = 100000;
            enemy.ab.currentStat.def = 0; enemy.ab.chanceInterruptAttack = -1;
            bool entryObserved = false;
            var drop = enemy.GetComponent<DropIn>();
            float runY = enemy.skeletonAnimation.transform.position.y;
            float runX = enemy.skeletonAnimation.transform.position.x;
            bool runShot = false;
            Vector3 spawnPosition = enemy.skeletonAnimation.transform.position;
            if (drop.spawnInPlace) Check(drop.Phase == DropIn.EntrancePhase.Spawning && enemy.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name.ToLowerInvariant() == "spawn", entry.Key + " battle starts with stationary spawn");
            if (entry.Key == "MobGloamweb") Check(drop.Phase == DropIn.EntrancePhase.Falling && !drop.runFromRight && !drop.spawnInPlace, "Spider retains falling entrance");
            if (drop.runFromRight) {
                Check(drop.Phase == DropIn.EntrancePhase.Running && enemy.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name == "Run_Battle_00", entry.Key + " battle uses native run");
                Check(Camera.main.WorldToViewportPoint(enemy.skeletonAnimation.GetComponent<MeshRenderer>().bounds.min).x > 1, entry.Key + " battle starts outside right screen");
            }
            while (enemy.IsEntering) {
                HoldPlayer(); enemy.StopAttacking();
                if (drop.spawnInPlace) {
                    if ((enemy.skeletonAnimation.transform.position-spawnPosition).sqrMagnitude > .000001f) throw new Exception(entry.Key+" spawn moved vertically/horizontally");
                    if (!runShot && enemy.skeletonAnimation.AnimationState.GetCurrent(0).TrackTime > .2f) {
                        ScreenCapture.CaptureScreenshot("output/monster-entrance-rules/"+entry.Key+"-spawn-combat.png"); runShot=true;
                    }
                }
                if (drop.runFromRight) {
                    if (Mathf.Abs(enemy.skeletonAnimation.transform.position.y-runY)>.001f || enemy.skeletonAnimation.transform.position.x>runX+.001f) throw new Exception(entry.Key+" run direction/ground height");
                    runX=enemy.skeletonAnimation.transform.position.x;
                    if (enemy.ab.enemyLifebar.transform.localScale != Vector3.zero) throw new Exception("Run lifebar shown before arrival");
                    if (!runShot && Camera.main.WorldToViewportPoint(enemy.skeletonAnimation.GetComponent<MeshRenderer>().bounds.center).x < 1) {
                        ScreenCapture.CaptureScreenshot("output/wolf-run-entrance/"+entry.Key+"-combat-running.png");runShot=true;
                    }
                }
                float hp = enemy.ab.currentStat.hpNow; enemy.ab.Hit(100); enemy.Attack();
                if (enemy.ab.currentStat.hpNow != hp || enemy.currentAnimation.StartsWith("Attack")) throw new Exception(entry.Key + " entrance isolation");
                entryObserved = true; yield return null;
            }
            enemy.StopAttacking(); enemy.currentAnimation = ""; enemy.SetAnimation("Idle1", true, false);
            Check(CreatureNames.DisplayName(enemy.name) == entry.DisplayName && enemy.skeletonAnimation.valid, entry.Key + " actual battle prefab/name/skeleton");
            Check(entryObserved, entry.Key + " actual entrance and isolation");
            var bar = enemy.ab.enemyLifebar;
            Check(enemy.bossName == (bar && (bar.isBoss || bar.isMiniBoss) ? entry.DisplayName : ""), entry.Key + " boss display name and ordinary enemy behavior preserved");
            if (bar && (bar.isBoss || bar.isMiniBoss))
                Check(bar.miniBossName.text == entry.DisplayName, entry.Key + " rendered boss label: " + bar.miniBossName.text);
            while (!PetCindrel.single) { HoldPlayer(); yield return null; }
            var pet = PetCindrel.single;
            pet.StopAllCoroutines(); pet.currentAnimation = ""; pet.SetAnimation("Idle1", true, true);
            Check(pet.name == "PetCindrel(Clone)" && pet.GetComponent<PetCindrelEffects>(), "PetCindrel real skill/general DB spawn and effects");
            HoldPlayer(); float playerHP = Player.single.ab.currentStat.hpNow;
            enemy.ab.currentStat.dmg = 100; enemy.currentAnimation = ""; enemy.SetAnimation("Attack1", false, false);
            yield return new WaitForSeconds(enemy.skeletonAnimation.Skeleton.Data.FindAnimation("Attack1").Duration + .15f);
            Check(Player.single.ab.currentStat.hpNow < playerHP && enemy.currentAnimation == "Idle1", entry.Key + " attack reduces player HP and returns idle");
            enemy.StopAttacking();
            float enemyHP = enemy.ab.currentStat.hpNow;
            pet.StopAllCoroutines(); pet.isAttacking = false; pet.currentAnimation = ""; pet.SetAnimation("Attack1", false, false);
            yield return new WaitForSeconds(pet.skeletonAnimation.Skeleton.Data.FindAnimation("Attack1").Duration + .05f);
            pet.StopAllCoroutines();
            Check(enemy.ab.currentStat.hpNow < enemyHP, "PetCindrel actually damages " + entry.Key);
            enemy.StopAttacking(); enemy.currentAnimation = ""; enemy.SetAnimation("Idle1", true, false);
            enemy.ab.Hit(1); Check(enemy.currentAnimation == "Hit", entry.Key + " real hit reaction");
            yield return new WaitForSeconds(enemy.skeletonAnimation.Skeleton.Data.FindAnimation("Hit").Duration + .1f);
            Check(enemy.currentAnimation == "Idle1", entry.Key + " hit returns idle");
            ScreenCapture.CaptureScreenshot(Dir + entry.Key + "-combat.png"); yield return new WaitForEndOfFrame();
            float xp = GameManager.single.xpNow; enemy.ab.Hit(10000000);
            Check(GameManager.single.xpNow > xp, entry.Key + " actual XP reward");
            while (enemy) { HoldPlayer(); yield return null; }
            Check(true, entry.Key + " death destroys instance; wave advances");
        }
        while (wm.waveNumber < entries.Length || GameManager.single.GetLivingEnemies().Count < 3) { HoldPlayer(); yield return null; }
        var mixed = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        while (mixed.Any(e => e.IsEntering)) { HoldPlayer(); yield return null; }
        Check(mixed.Select(e => e.name).Distinct().Count() == 3, "Mixed wave resolves all three renamed keys");
        foreach (var e in mixed) { e.StopAttacking(); e.ab.currentStat.hpNow = e.ab.currentStat.hpMax = 100000; e.currentAnimation = ""; e.SetAnimation("Idle1",true,false); }
        PetCindrel.single.StopAllCoroutines(); PetCindrel.single.currentAnimation=""; PetCindrel.single.SetAnimation("Idle1",true,true);
        for (int r = 0; r < 3; r++) {
            Resolution(Width[r],Height[r]); yield return new WaitForSeconds(.5f); yield return new WaitForEndOfFrame();
            Check(Screen.width == Width[r] && Screen.height == Height[r], "Battle actual ratio " + Width[r] + "x" + Height[r]);
            ScreenCapture.CaptureScreenshot(Dir + "mixed-combat-" + Width[r] + "x" + Height[r] + ".png"); yield return new WaitForEndOfFrame();
        }
        foreach (var e in mixed) e.ab.Hit(10000000);
        while (!wm.victoryCalled) { HoldPlayer(); yield return null; }
        Check(true, "11 individual waves and mixed wave reach victory");
        GameplayCanvas.single.GoToCampFromVictoryOrDefeat(); yield return new WaitForSeconds(2);
        Check(!PetCindrel.single, "Victory return cleans up PetCindrel");
    }
}
