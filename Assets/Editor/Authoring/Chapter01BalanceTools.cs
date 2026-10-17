using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.Level;

// Only an explicit menu action or request applies the design. Imports never apply it.
[InitializeOnLoad]
public static class Chapter01BalanceTools
{
    const string Design = "Docs/Balance/chapter01.json";
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    const string Output = "output/chapter01-balance/";
    const string SmokeKey = "CH1_BALANCE_SMOKE";
    [Serializable] public class Config { public int version; public string[] notes; public Monster[] monsters; public Node[] nodes; }
    [Serializable] public class Monster
    {
        public string key, path, dropSource, bossName;
        public float hp, damage, interval, experience, crit, defense, pierce;
        public bool boss, miniBoss;
        public int interruptChance;
        public float critDamage, hpPercent, damagePercent, defensePercent, attackSpeed, block, absorb;
        public System.Collections.Generic.List<Item> drops;
    }
    [Serializable] public class Node
    {
        public string key;
        public int index;
        public bool miniBoss, boss;
        public Wave[] waves;
        public Rewards rewards;
    }
    [Serializable] public class Rewards
    {
        public float crystalsMin, crystalsMax;
        public int chestSeconds;
        public ChestGrade chestGrade;
        public System.Collections.Generic.List<Item> normal, gold, diamond;
    }
    static Stat ExpectedStat(Monster m) => new Stat { hpNow=m.hp, hpMax=m.hp, dmg=m.damage, def=m.defense,
        crit=m.crit, critDmg=m.critDamage, pierce=m.pierce, hpPerc=m.hpPercent, dmgPerc=m.damagePercent,
        defPerc=m.defensePercent, atkSpd=m.attackSpeed, block=m.block, absorb=m.absorb };
    static void CheckItems(System.Collections.Generic.List<Item> items, string label)
    {
        Require(items != null && items.Count > 0, "Missing reward pool " + label);
        foreach (var item in items)
            Require(item.m_ChanceDrop > 0 && item.m_ChanceDrop <= 1 && !string.IsNullOrEmpty(item.m_IconName)
                && item.baseStat != null && item.rollStat != null, "Incomplete item " + label);
    }
    static bool SameItems(System.Collections.Generic.List<Item> a, System.Collections.Generic.List<Item> b) =>
        a != null && b != null && a.Count == b.Count && a.Zip(b, (x,y) => JsonUtility.ToJson(x)==JsonUtility.ToJson(y)).All(x=>x);
    // One-time explicit migration: snapshot source item definitions into the editable design.
    static void CompleteDetails()
    {
        var config=Read();
        foreach(var m in config.monsters)
            m.drops=Utilities.DeepClone(AssetDatabase.LoadAssetAtPath<GameObject>(m.dropSource).GetComponent<Enemy>().itemsGiven.Where(i=>i.m_ChanceDrop>0).ToList());
        WithScene(wm=>{
            foreach(var n in config.nodes) {
                var source=wm.waveDb.Single(w=>w.levelKey==(n.index==5?"The Great Cave Entrance":n.key));
                n.rewards.normal=Utilities.DeepClone(source.chestItemsGiven);
                n.rewards.gold=Utilities.DeepClone(source.chestGoldItemsGiven);
                n.rewards.diamond=Utilities.DeepClone(source.chestDiamondItemsGiven);
            }
        },false);
        File.WriteAllText(Design,JsonUtility.ToJson(config,true)+"\n");
        Apply();
    }
    static Config Read() => JsonUtility.FromJson<Config>(File.ReadAllText(Design));
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Log(string message) => File.AppendAllText(Output + "validation.txt", message + "\n");
    static Chapter01BalanceTools()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(SmokeKey, false))
            {
                running = false;
                stopping = false;
                SessionState.SetBool(SmokeKey, false);
                SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
                var previous = SessionState.GetString("CH1_BALANCE_RETURN", "");
                if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                    EditorSceneManager.OpenScene(previous);
            }
        };
    }
    static void WithScene(Action<WaveManager> action, bool save)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Require(!scene.isDirty, "Gameplay contains unsaved changes; balance was not applied.");
            var wm = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<WaveManager>(true)).Single();
            action(wm);
            if (save) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
    [MenuItem("Tools/Dark Fairytale/Balance/Apply Chapter 01 Design")]
    public static void Apply()
    {
        Directory.CreateDirectory(Output);
        var config = Read();
        // Resolve every dependency and check scene state before modifying any assets.
        WithScene(wm => {
            Require(wm.waveDb.Take(5).Select(w => w.levelKey).SequenceEqual(config.nodes.Select(n => n.key)), "Chapter route differs.");
            foreach (var m in config.monsters)
            {
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(m.path)?.GetComponent<Actor_Base>(), "Missing actor " + m.key);
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(m.dropSource)?.GetComponent<Enemy>(), "Missing drop source " + m.key);
                CheckItems(m.drops,m.key);
                Require(m.critDamage==150 && m.hpPercent==0 && m.damagePercent==0 && m.defensePercent==0 &&
                    m.attackSpeed==0 && m.block==0 && m.absorb==0, "Unsupported enemy stat configuration " + m.key);
            }
            foreach(var n in config.nodes) {
                Require(n.rewards!=null && n.rewards.crystalsMin>=0 && n.rewards.crystalsMax>=n.rewards.crystalsMin && n.rewards.chestSeconds>0,"Missing node rewards "+n.key);
                CheckItems(n.rewards.normal,n.key+" normal");CheckItems(n.rewards.gold,n.key+" gold");CheckItems(n.rewards.diamond,n.key+" diamond");
            }
        }, false);
        foreach (var m in config.monsters)
        {
            var root = PrefabUtility.LoadPrefabContents(m.path);
            try
            {
                var ab = root.GetComponent<Actor_Base>();
                ab.baseStat = ExpectedStat(m);
                ab.currentStat = Utilities.DeepClone(ab.baseStat);
                ab.isBoss = m.boss;
                ab.chanceInterruptAttack = m.interruptChance;
                var enemy = root.GetComponent<Enemy>();
                enemy.expGiven = m.experience;
                enemy.waitBetweenAttacks = m.interval;
                enemy.bossName = m.bossName;
                enemy.itemsGiven = Utilities.DeepClone(m.drops);
                var bar = root.GetComponentInChildren<EnemyLifebar>(true);
                if (m.boss && !bar.miniBossName)
                {
                    var oldBar = bar;
                    var template = AssetDatabase.LoadAssetAtPath<EnemyLifebar>("Assets/DarkFairytale/UI/CursedStorybook/Prefabs/CursedStorybookEnemyBar.prefab");
                    bar = UnityEngine.Object.Instantiate(template, root.transform, false);
                    bar.name = "CursedStorybookEnemyBar";
                    bar.transform.SetSiblingIndex(0);
                    ab.enemyLifebar = bar;
                    UnityEngine.Object.DestroyImmediate(oldBar.gameObject);
                }
                bar.isMiniBoss = m.miniBoss;
                bar.isBoss = m.boss && !m.miniBoss;
                if (bar.miniBossName) bar.miniBossName.text = m.bossName;
                PrefabUtility.SaveAsPrefabAsset(root, m.path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        WithScene(wm => {
            foreach (var n in config.nodes)
            {
                var w = wm.waveDb.Single(x => x.levelKey == n.key);
                w.waves = n.waves;
                w.isMiniBoss = n.miniBoss;
                w.isBoss = n.boss;
                w.crystalsMin=n.rewards.crystalsMin;w.crystalsMax=n.rewards.crystalsMax;
                w.chestSeconds=n.rewards.chestSeconds;w.chestGrade=n.rewards.chestGrade;
                w.chestItemsGiven=Utilities.DeepClone(n.rewards.normal);
                w.chestGoldItemsGiven=Utilities.DeepClone(n.rewards.gold);
                w.chestDiamondItemsGiven=Utilities.DeepClone(n.rewards.diamond);
            }
        }, true);
        Validate();
    }
    [MenuItem("Tools/Dark Fairytale/Balance/Validate Chapter 01 Design")]
    public static void Validate()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "validation.txt", "Chapter 01 persistent asset validation\n");
        var config = Read();
        foreach (var m in config.monsters)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(m.path);
            var ab = go.GetComponent<Actor_Base>(); var enemy = go.GetComponent<Enemy>();
            Require(JsonUtility.ToJson(ab.baseStat)==JsonUtility.ToJson(ExpectedStat(m)),"Full stat mismatch "+m.key);
            Require(ab.baseStat.hpNow == m.hp && ab.baseStat.hpMax == m.hp && ab.baseStat.dmg == m.damage &&
                ab.baseStat.def == m.defense && ab.baseStat.crit == m.crit && ab.baseStat.pierce == m.pierce, "Stats differ " + m.key);
            Require(enemy.expGiven == m.experience && enemy.waitBetweenAttacks == m.interval, "Timing/rewards differ " + m.key);
            Require(ab.isBoss == m.boss, "Boss skill targeting differs " + m.key);
            Require(ab.chanceInterruptAttack == m.interruptChance, "Interrupt chance differs " + m.key);
            var bar = go.GetComponentInChildren<EnemyLifebar>(true);
            Require(bar.isMiniBoss == m.miniBoss && bar.isBoss == (m.boss && !m.miniBoss), "Boss bar differs " + m.key);
            Require(!m.boss || bar.miniBossName, "Missing boss label " + m.key);
            CheckItems(m.drops,m.key);
            Require(SameItems(enemy.itemsGiven,m.drops), "Drops differ " + m.key);
            var db = LevelDataDescriptions.EnemyDb.Single(x => x.key == m.key);
            Require(db.assetRef.AssetGUID == AssetDatabase.AssetPathToGUID(m.path), "Wrong addressable " + m.key);
            var data = enemy.skeletonAnimation.skeletonDataAsset.GetSkeletonData(false);
            Require(data.FindAnimation("Attack1") != null && data.FindAnimation("Death1") != null, "Missing clips " + m.key);
            Log("PASS " + m.key + " HP=" + m.hp + " ATK=" + m.damage + " interval=" + m.interval + " XP=" + m.experience);
        }
        WithScene(wm => {
            foreach (var n in config.nodes)
            {
                var w = wm.waveDb.Single(x => x.levelKey == n.key);
                Require(w.isBoss == n.boss && w.isMiniBoss == n.miniBoss && w.waves.Length == n.waves.Length, "Node differs " + n.key);
                for (int i = 0; i < w.waves.Length; i++)
                    Require(w.waves[i].enemies.SequenceEqual(n.waves[i].enemies), "Wave differs " + n.key + " / " + i);
                Require(w.crystalsMin==n.rewards.crystalsMin && w.crystalsMax==n.rewards.crystalsMax && w.chestSeconds==n.rewards.chestSeconds &&
                    w.chestGrade==n.rewards.chestGrade && SameItems(w.chestItemsGiven,n.rewards.normal) &&
                    SameItems(w.chestGoldItemsGiven,n.rewards.gold) && SameItems(w.chestDiamondItemsGiven,n.rewards.diamond),"Node rewards differ "+n.key);
                Log("PASS node " + n.index + " " + n.key + " waves=" + w.waves.Length);
            }
        }, false);
        Log("PASS persistent configuration, addressable registrations, drops, clips and boss flags");
    }
    // Art probes must follow the saved gameplay drop policy, which is no longer empty.
    public static void ValidateConfiguredDrops(Enemy enemy)
    {
        var m = Read().monsters.Single(x => x.key == enemy.name.Replace("(Clone)", ""));
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(m.path).GetComponent<Enemy>();
        Require(enemy.itemsGiven.Count == saved.itemsGiven.Count && enemy.itemsGiven.Zip(saved.itemsGiven,
            (a, b) => JsonUtility.ToJson(a) == JsonUtility.ToJson(b)).All(x => x), "Runtime drops differ from saved prefab " + m.key);
    }
    static bool running, stopping;
    static int node, wave = -1, errors, stage;
    static double started, waveReady;
    static Config smoke;
    static int captureIndex;
    static bool capturePending;
    static bool layoutNeeded;
    static double captureAt;
    static readonly int[] Widths = { 720, 768, 720 }, Heights = { 1280, 1024, 1440 };
    static void Resolution(int index)
    {
        typeof(CursedStorybookUiValidation).GetMethod("SetResolution", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { Widths[index], Heights[index] });
    }
    [MenuItem("Tools/Dark Fairytale/Balance/Smoke Test Chapter 01")]
    public static void Smoke()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play first.");
        running = false; stopping = false;
        Require(!Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty), "Unsaved scene; smoke test not started.");
        SessionState.SetString("CH1_BALANCE_RETURN", SceneManager.GetActiveScene().path);
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
        SessionState.SetBool(SmokeKey, true);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }
    static void BeginNode()
    {
        wave = -1;
        var gc = GameplayCanvas.single;
        gc.victoryPanel.SetActive(false); gc.defeatPanel.SetActive(false); gc.dailyCherriesPop.SetActive(false);
        var assets = LevelController.Instance.AssetManager;
         assets.HideMapPackage();
        if (gc.homeUI) gc.homeUI.gameObject.SetActive(false);
        gc.ToggleButtonGrid(false); gc.ToggleFusionBarUI(false);
        WaveManager.single.CallStartLevel(smoke.nodes[node].key);
        stage = 0;
    }
    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors++; Log("ERROR " + message); }
    }
    static int ItemCount()
    {
        var inventory=InventoryManager.single;
        return inventory.weaponsOwned.Count+inventory.shieldsOwned.Count+inventory.helmetsOwned.Count+inventory.armorsOwned.Count+inventory.accOwned.Count;
    }
    static void Finish(string message)
    {
        Log(message); running = false; stopping = true; Application.logMessageReceived -= OnLog;
        typeof(CursedStorybookUiValidation).GetMethod("RestoreGameView", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        Time.timeScale = 1; EditorApplication.isPlaying = false;
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string request = Output + "command.request";
        if (File.Exists(request))
        {
            var command = File.ReadAllText(request).Trim(); File.Delete(request);
            try {
                if (command == "complete") CompleteDetails();
                else if (command == "apply") Apply(); else if (command == "validate") Validate();
                else if (command == "smoke") Smoke(); else throw new Exception("Unknown command " + command);
                File.WriteAllText(Output + "command-result.txt", "PASS " + command);
            } catch (Exception e) { File.WriteAllText(Output + "command-result.txt", "FAIL " + e); }
        }
        if (stopping || !EditorApplication.isPlaying || !SessionState.GetBool(SmokeKey, false)) return;
        try
        {
            if (!running)
            {
                if (!Player.single || Player.single.ab.currentStat == null || !WaveManager.single || !GameplayCanvas.single) return;
                var intro = UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();
                if (intro && !intro.Finished) return;
                smoke = Read(); node = 0; errors = 0; started = EditorApplication.timeSinceStartup; running = true;
                Application.logMessageReceived += OnLog;
                Log("SMOKE: real saved waves; player held alive and enemies killed after observing attacks. This is integration, not balance playtesting.");
                GameManager.single.xpNow = 0; GameManager.single.xpMax = 100000;
                Time.timeScale = 3; BeginNode();
            }
            Require(EditorApplication.timeSinceStartup - started < 240, "Smoke timeout");
            Player.single.StopAllCoroutines();
            Player.single.ForceAnimation("Idle1", true);
            Player.single.ab.currentStat.hpNow = Player.single.ab.currentStat.hpMax = 100000;
            if (PetCindrel.single) PetCindrel.single.StopAllCoroutines();
            var wm = WaveManager.single;
            if (stage == 1)
            {
                if (EditorApplication.timeSinceStartup < waveReady) return;
                Require(GameObject.FindGameObjectsWithTag("MapGraphics").Length == 0, "Map leaked on return to camp");
                Require(GameObject.FindGameObjectsWithTag("ChestDropIn").Length == 0, "Victory chest leaked on return to camp");
                Log("PASS node " + (node + 1) + " victory and return to camp");
                node++; if (node == smoke.nodes.Length) { Finish(errors == 0 ? "PASS all five nodes / all saved waves / victory / camp; errors=0" : "FAIL runtime errors=" + errors); return; }
                BeginNode(); return;
            }
            if (wm.victoryCalled && GameplayCanvas.single.victoryPanel.activeInHierarchy &&
                GameManager.single.gameState == Assets.Scripts.Game.Enum.GameStateType.VICTORY &&
                !LevelController.Instance.AssetManager.spawningGeneralObj && GameplayCanvas.single.victoryPanel.GetComponent<StorybookBattleResultUI>())
            {
                var reward=smoke.nodes[node].rewards;
                var chest=Router.single.newChest;
                Require(chest!=null && SameItems(chest.possibleRewards,reward.normal) && SameItems(chest.possibleGoldRewards,reward.gold) &&
                    SameItems(chest.possibleDiamondRewards,reward.diamond),"Actual victory chest pools differ");
                float factor=chest.chestGrade==ChestGrade.DIAMOND?10:chest.chestGrade==ChestGrade.GOLD?3:1;
                Require(chest.crystalsMin==reward.crystalsMin*factor && chest.crystalsMax==reward.crystalsMax*factor,"Actual chest crystal bounds differ");
                Log("PASS runtime reward pools and crystal bounds node="+(node+1));
                GameplayCanvas.single.GoToCampFromVictoryOrDefeat();
                waveReady = EditorApplication.timeSinceStartup + 2; stage = 1; return;
            }
            if (wm.loadingLevel || wm.currentWaveset == null) return;
            var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => !e.ab.isDying).ToArray();
            if (enemies.Length == 0 || enemies.Any(e => e.IsEntering || e.ab.currentStat == null)) return;
            if (wave != wm.waveNumber)
            {
                var expected = smoke.nodes[node].waves[wm.waveNumber].enemies;
                Require(enemies.Select(e => e.name.Replace("(Clone)", "")).OrderBy(x => x).SequenceEqual(expected.OrderBy(x => x)), "Actual spawn differs");
                foreach (var enemy in enemies)
                {
                    var m = smoke.monsters.Single(x => x.key == enemy.name.Replace("(Clone)", ""));
                    Require(enemy.ab.currentStat.hpMax == m.hp && enemy.ab.currentStat.dmg == m.damage, "Runtime stats differ " + m.key);
                }
                wave = wm.waveNumber; waveReady = EditorApplication.timeSinceStartup + 1.5;
                captureIndex = 3;
                if (enemies.Any(e => e.ab.isBoss))
                {
                    captureIndex = 0; capturePending = false; layoutNeeded = true; Resolution(0);
                    captureAt = EditorApplication.timeSinceStartup + 1;
                }
                Log("PASS runtime node=" + (node + 1) + " wave=" + (wave + 1) + " " + string.Join(",", expected));
            }
            if (captureIndex < 3)
            {
                if (EditorApplication.timeSinceStartup < captureAt) return;
                if (layoutNeeded)
                {
                    var assets = LevelController.Instance.AssetManager;
                    var camera = assets.battleCampCamera.GetComponent<Camera>();
                    assets.battleStyle.PlacePlayer(Player.single, camera);
                    for (int i = 0; i < enemies.Length; i++) assets.battleStyle.PlaceEnemy(enemies[i], camera, i);
                    GameplayCanvas.single.itemPopUpQueue.Clear();
                    GameplayCanvas.single.itemPopUp.SetActive(false);
                    layoutNeeded = false; captureAt = EditorApplication.timeSinceStartup + 1.5; return;
                }
                if (!capturePending)
                {
                    ScreenCapture.CaptureScreenshot(Output + "node" + (node + 1) + "-" + Widths[captureIndex] + "x" + Heights[captureIndex] + ".png");
                    capturePending = true; captureAt = EditorApplication.timeSinceStartup + .5; return;
                }
                captureIndex++;
                if (captureIndex < 3) { Resolution(captureIndex); capturePending = false; layoutNeeded = true; captureAt = EditorApplication.timeSinceStartup + 1; return; }
            }
            if (EditorApplication.timeSinceStartup >= waveReady)
            {
                float beforeXp=GameManager.single.xpNow, expectedXp=enemies.Sum(e=>e.expGiven);
                int beforeItems=ItemCount(), guaranteed=enemies.Sum(e=>e.itemsGiven.Count(i=>i.m_ChanceDrop==1));
                foreach (var enemy in enemies) enemy.ab.Hit(1000000);
                Require(GameManager.single.xpNow+.001f>=beforeXp+expectedXp,"Missing actual death XP");
                Require(ItemCount()>=beforeItems+guaranteed,"Missing guaranteed equipment drop");
                Log("PASS death rewards node="+(node+1)+" wave="+(wave+1)+" baseXP="+expectedXp+" awardedXP="+(GameManager.single.xpNow-beforeXp)+" items="+(ItemCount()-beforeItems));
            }
        }
        catch (Exception e) { Finish("FAIL " + e); }
    }
}
