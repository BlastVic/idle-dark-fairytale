using System;
using System.Collections;
using System.IO;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

// Explicit opt-in Play Mode regression; all stats and skill overrides are temporary.
[InitializeOnLoad]
public static class PlayerDeathValidation
{
    const string Output = "output/player-death/";
    const string Key = "PLAYER_DEATH_QA";
    static bool running;
    static double deadline;
    static PlayerDeathValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            running = false;
            SessionState.SetBool(Key, false);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
        };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Output + "run.request"))
        {
            File.Delete(Output + "run.request");
            File.WriteAllText(Output + "validation.txt", "Player death Play Mode regression\n");
            SessionState.SetBool(Key, true);
            SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", true);
            EditorApplication.isPlaying = true;
        }
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Key, false)) return;
        if (!running && GameManager.single && GameplayCanvas.single)
        {
            running = true;
            deadline = EditorApplication.timeSinceStartup + 100;
            GameManager.single.StartCoroutine(GuardedRun());
        }
        if (running && EditorApplication.timeSinceStartup > deadline) Finish("FAIL timeout");
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        File.AppendAllText(Output + "validation.txt", "PASS " + message + "\n");
    }
    static void Finish(string message)
    {
        File.AppendAllText(Output + "validation.txt", message + "\n");
        EditorApplication.isPlaying = false;
    }
    static IEnumerator GuardedRun()
    {
        var test = Run();
        while (true)
        {
            object current;
            try { if (!test.MoveNext()) break; current = test.Current; }
            catch (Exception error) { Finish("FAIL " + error); yield break; }
            yield return current;
        }
        Finish("DONE");
    }
    static IEnumerator Run()
    {
        var intro = UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();
        while (intro && intro.gameObject.activeSelf) yield return null;
        yield return new WaitForSeconds(1);
        var gc = GameplayCanvas.single;
        var gm = GameManager.single;
        var skills = SkillsController.Instance;
        skills.ResetSkills();
        gc.dailyCherriesPop.SetActive(false);
        TutorialManager.single.automatic = false;
        TutorialManager.single.view.Hide();
        gm.xpMax = 999999;
        gm.lastBattleKey = "Black Forest Mushroom Test";
        Router.single.LastBattle();
        while (!Player.single || Player.single.isCampPlayer || WaveManager.single.loadingLevel || gm.GetLivingEnemies(false).Count == 0)
            yield return null;
        yield return new WaitForSeconds(.5f);
        var player = Player.single;
        player.ab.currentStat.hpMax = player.ab.currentStat.hpNow = 100000;
        player.ab.currentStat.dmg = 0;
        player.ab.currentStat.block = 0;
        foreach (var actor in gm.GetLivingEnemies()) actor.currentStat.hpMax = actor.currentStat.hpNow = 100000;
        var enemies = gm.GetLivingEnemies(false).Select(a => a.GetComponent<Enemy>()).ToArray();
        Check(enemies.Length > 0, "live enemies available");
        var resurrection = SkillsDataDescriptions.SkillDataArr.First(s => s.skillsType == SkillsTypeEnum.Ressurrection);
        float originalChance = resurrection.value1[0];
        resurrection.value1[0] = 1;
        int deaths = gm.playerDeathCount;
        foreach (var enemy in enemies) enemy.Attack();
        player.ab.Hit(10000000);
        resurrection.value1[0] = originalChance;
        Check(player.ab.isDying && player.ab.ressurrectionWaiting, "lethal hit enters resurrection death");
        Check(player.skeletonAnimation.AnimationState.GetCurrent(0).Animation.Name == "Death1", "Death1 replaces active attack");
        Check(enemies.All(e => e.currentAnimation == "Idle1"), "all active enemies immediately stop attack animation");
        player.AnimationOnHit("Attack1");
        player.SetAnimation("Idle1", true, false);
        player.ab.Hit(1000);
        foreach (var enemy in enemies) { enemy.StartAttacking(true); enemy.Attack(); enemy.AnimationOnHit("Attack1"); }
        Check(gm.playerDeathCount == deaths + 1 && player.ab.currentStat.hpNow == 0, "late hits cannot damage or count death twice");
        Check(player.currentAnimation == "Death1" && enemies.All(e => e.currentAnimation == "Idle1"), "late callbacks cannot restart combat or replace death");
        yield return new WaitForSeconds(.3f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Output + "death-animation.png");
        while (player.ab.isDying) yield return null;
        Check(!player.ab.ressurrectionWaiting && player.ab.currentStat.hpNow > 0 && !gc.defeatPanel.activeSelf, "real death animation callback resurrects without defeat");
        float hp = player.ab.currentStat.hpNow;
        yield return new WaitForSeconds(4);
        Check(player.ab.currentStat.hpNow < hp, "enemy attacks resume after resurrection");
        resurrection.value1[0] = 0;
        player.ab.Hit(10000000);
        resurrection.value1[0] = originalChance;
        Check(player.ab.isDying && !player.ab.ressurrectionWaiting && player.currentAnimation == "Death1", "second lethal hit plays non-resurrection death");
        while (!gc.defeatPanel.activeSelf)
        {
            Check(gm.GetLivingEnemies(false).All(a => a.GetComponent<Enemy>().currentAnimation == "Idle1"), "enemies remain idle during defeat delay");
            yield return new WaitForSeconds(.5f);
        }
        yield return new WaitForSeconds(1);
        Check(gm.GetLivingEnemies().Count == 0, "defeat cleans up enemies");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Output + "defeat.png");
        yield return Router.single.GoToCampFromBattle();
        yield return new WaitForSeconds(1);
        Check(gc.homeUI.gameObject.activeInHierarchy && !Player.single.ab.isDying, "return to camp resets player");
    }
}
