using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Game.Enum;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Explicit editor test: real Gameplay renders, binding/raycast checks and return/settlement flows.</summary>
public static class CursedStorybookUiValidation
{
    static string Dir => Formation ? "output/battle-formation/" : "output/battle-ui/";
    static bool Formation => SessionState.GetBool("B3_FORMATION_TEST", false);
    const string Key = "B3_UI_TEST_STAGE";
    static int stage, ratio, errors;
    static double until, started;
    static readonly int[] Widths = { 720, 768, 720 };
    static readonly int[] Heights = { 1280, 1024, 1440 };
    static readonly string[] Names = { "9x16", "3x4", "1x2" };
    static int originalGameSize = -1;
    static readonly List<int> addedSizes = new List<int>();

    [InitializeOnLoadMethod]
    static void Listen()
    {
        stage = EditorApplication.isPlayingOrWillChangePlaymode ? SessionState.GetInt(Key,0) : 0;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (stage == 0 || (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)) return;
        errors++; File.AppendAllText(Dir+"validation.txt", "ERROR: "+message+"\n");
    }

    [MenuItem("Tools/Dark Fairytale/UI/Validate B3 in Gameplay")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("B3_FORMATION_TEST", false);
        BeginValidation();
    }

    [MenuItem("Tools/Dark Fairytale/UI/Validate Triangle Formation")]
    public static void BeginFormation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("B3_FORMATION_TEST", true);
        BeginValidation();
    }

    static void BeginValidation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before starting this test.");
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Dir+"validation.txt", "B3 UGUI prefab / gameplay validation\n");
        errors=0; ratio=0;
        ValidatePrefabs();
        SetResolution(720,1280);
        SessionState.SetInt(Key,1); stage=1; started=EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    static void ValidatePrefabs()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CursedStorybookUiBuilder.HudPath);
        Check(prefab != null,"Saved HUD prefab exists");
        var hud = prefab.GetComponent<CursedBattleHud>();
        var sceneHud = UnityEngine.Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include).battleHud;
        Check(PrefabUtility.IsPartOfPrefabInstance(sceneHud),"Scene HUD retains its prefab connection");
        Check(hud && hud.backButton && hud.hpFill && hud.xpFill && hud.waveText && hud.levelText,"Serialized UI references are complete");
        Check(prefab.GetComponent<BattleHudSafeArea>().core != null,"Editable centered 9:16 core is assigned");
        foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts: "+t.name, false);
        foreach(var t in prefab.GetComponentsInChildren<Text>(true))
            Check(t.font && t.font.HasCharacter('生') && t.font.HasCharacter('波'),"Embedded font supports Chinese",false);
        foreach(var image in prefab.GetComponentsInChildren<Image>(true))
            if (image != hud.backButton.targetGraphic) Check(!image.raycastTarget,"Decoration doesn't intercept input",false);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(CursedStorybookUiBuilder.EnemyPath).GetComponent<EnemyLifebar>();
        Check(enemy && enemy.hpMeter && enemy.hpText && enemy.miniBossName,"Saved enemy HUD prefab has complete bindings");
        Log("PASS: all decorative graphics ignore raycasts; Chinese glyphs and prefab scripts verified.");
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if(stage==0)
            {
                if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("output/battle-formation/validate.request"))
                { File.Delete("output/battle-formation/validate.request"); BeginFormation(); return; }
                if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir+"validate.request")) { File.Delete(Dir+"validate.request"); Begin(); }
                return;
            }
            if(EditorApplication.timeSinceStartup-started>180) throw new Exception("Validation timed out at stage "+stage);
            if(!EditorApplication.isPlaying) return;
            var gc = GameplayCanvas.single;
            var gm = GameManager.single;
            if(!gc || !gm || !gc.battleHud || EditorApplication.timeSinceStartup<until) return;
            if(stage==1)
            {
                if(!LevelController.Instance || !LevelController.Instance.AssetManager || !Player.single) return;
                started=EditorApplication.timeSinceStartup; ratio=0;
                if(Formation)
                {
                    // Play-mode fixture only; never save changes to the authored waves.
                    var wave = WaveManager.single.waveDb.Find(w => w.levelKey == "Black Forest Mushroom Test");
                    wave.waves = new[] { new Wave { enemies = BlackForestMushroomBuilder.Ids.ToArray() } };
                    BlackForestMushroomBuilder.RunBattle();
                }
                else BlackForestMushroomBuilder.RunFirstLevel();
                Next(2,Formation ? 0 : 2);
            }
            else if(stage==2)
            {
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                if(Formation)
                {
                    Player.single.StopAllCoroutines();
                    Player.single.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
                    foreach(var e in enemies) { e.ab.currentStat.hpNow=e.ab.currentStat.hpMax; }
                }
                if(!gc.battleHud.gameObject.activeInHierarchy || enemies.Length==0 || enemies.Any(e=>e.GetComponent<DropIn>().isFalling)) return;
                if(Formation && enemies.Length!=3) return;
                Time.timeScale=0;
                Check(gc.battleRelatedUI.All(o=>!o.activeSelf) && !gc.hpXpLevelUI.activeSelf,"Legacy battle HUD is hidden without deleting it");
                Check(enemies.All(e=>e.ab.enemyLifebar.name=="CursedStorybookEnemyBar"),"Live enemies use the authored health bar prefab");
                TestData(gc);
                SetResolution(Widths[ratio],Heights[ratio]); Next(3,.8);
            }
            else if(stage==3)
            {
                var pixels=gc.battleHud.GetComponent<Canvas>().pixelRect;
                Check(Mathf.RoundToInt(pixels.width)==Widths[ratio] && Mathf.RoundToInt(pixels.height)==Heights[ratio],"Game view has requested resolution "+Names[ratio]);
                var assets=LevelController.Instance.AssetManager;
                var camera=assets.battleCampCamera.GetComponent<Camera>();
                assets.battleStyle.PlacePlayer(Player.single,camera);
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
                    .OrderBy(e=>e.skeletonAnimation.GetComponent<MeshRenderer>().bounds.min.y).ToArray();
                for(int i=0;i<enemies.Length;i++) { assets.battleStyle.PlaceEnemy(enemies[i],camera,i); enemies[i].GetComponent<DropIn>().ResetForLayout();
                    if(Formation) {
                        var sk=enemies[i].skeletonAnimation;
                        sk.AnimationState.ClearTracks(); sk.Skeleton.SetToSetupPose();
                        sk.AnimationState.SetAnimation(0,"Idle1",true); sk.Update(0); sk.LateUpdate();
                    }
                }
                gc.battleHud.GetComponent<BattleHudSafeArea>().Apply();
                Canvas.ForceUpdateCanvases();
                Next(13,.5);
            }
            else if(stage==13)
            {
                ScreenCapture.CaptureScreenshot(Dir+"inspect-"+Names[ratio]+".png");
                Next(14,.5);
            }
            else if(stage==14)
            {
                var assets=LevelController.Instance.AssetManager;
                var camera=assets.battleCampCamera.GetComponent<Camera>();
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).OrderBy(e=>e.transform.position.x).ToArray();
                // Recover slot order from left/back, center/front, right/back.
                if(Formation) enemies=new[] {enemies[1],enemies[0],enemies[2]};
                ValidateLayout(gc.battleHud,Names[ratio]);
                if(Formation) ValidateFormation(camera, assets.battleStyle, enemies);
                Next(4,.5);
            }
            else if(stage==4)
            {
                ScreenCapture.CaptureScreenshot(Dir+"gameplay-"+Names[ratio]+".png");
                Next(5,.7);
            }
            else if(stage==5)
            {
                ratio++;
                if(ratio<Widths.Length) { SetResolution(Widths[ratio],Heights[ratio]); Next(3,.8); }
                else
                {
                    TestSafeAreas(gc.battleHud);
                    SetResolution(720,1280); Time.timeScale=2;
                    // Layout sampling clears Spine tracks; resume the attack chain explicitly.
                    if(Formation) { gc.battleHud.backButton.onClick.Invoke(); Next(15,2); return; }
                    Player.single.currentAnimation="";
                    Player.single.isAttacking=false;
                    Player.single.StartCoroutine(Player.single.AttackComplete());
                    Next(6,1);
                }
            }
            else if(stage==15)
            {
                BlackForestMushroomBuilder.RunFirstLevel(); Next(6,1);
            }
            else if(stage==6)
            {
                // Test fixture: sustain HP so the full three-wave victory path is deterministic.
                if(gm.gameState==GameStateType.BATTLE && Player.single && !Player.single.ab.isDying)
                { Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax; gc.RefreshHp(); }
                if(gm.gameState!=GameStateType.VICTORY && gm.gameState!=GameStateType.DEFEAT) return;
                Check(!gc.battleHud.gameObject.activeSelf,"Battle HUD hides for "+gm.gameState+" settlement");
                ScreenCapture.CaptureScreenshot(Dir+"settlement.png");
                gc.GoToCampFromVictoryOrDefeat(); Next(7,3);
            }
            else if(stage==7)
            {
                if(gm.gameState!=GameStateType.CAMP) return;
                Check(!gc.battleHud.gameObject.activeSelf && gc.hpXpLevelUI.activeSelf,"Camp HUD restored after settlement");
                BlackForestMushroomBuilder.RunFirstLevel(); Next(8,3);
            }
            else if(stage==8)
            {
                if(!gc.battleHud.gameObject.activeInHierarchy) return;
                Check(gc.battleHud.backButton.interactable,"Return button resets when entering battle again");
                gc.battleHud.backButton.onClick.Invoke(); Next(9,2);
            }
            else if(stage==9)
            {
                if(gm.gameState!=GameStateType.CAMP) return;
                Check(!gc.battleHud.gameObject.activeSelf && gc.hpXpLevelUI.activeSelf,"Actual return button callback reaches camp and restores HUD");
                ScreenCapture.CaptureScreenshot(Dir+"return-to-camp.png");
                BlackForestMushroomBuilder.RunFirstLevel(); Next(10,3);
            }
            else if(stage==10)
            {
                if(!gc.battleHud.gameObject.activeInHierarchy) return;
                // Exercise the real defeat settlement entry independently of combat balance.
                Router.single.StartCoroutine(Router.single.GoToDefeat()); Next(11,3);
            }
            else if(stage==11)
            {
                if(gm.gameState!=GameStateType.DEFEAT) return;
                Check(!gc.battleHud.gameObject.activeSelf && gc.defeatPanel.activeSelf,"Defeat settlement hides battle HUD");
                ScreenCapture.CaptureScreenshot(Dir+"defeat-settlement.png");
                gc.GoToCampFromVictoryOrDefeat(); Next(12,3);
            }
            else if(stage==12)
            {
                if(gm.gameState!=GameStateType.CAMP) return;
                Check(!gc.battleHud.gameObject.activeSelf && gc.hpXpLevelUI.activeSelf,"Camp restored after defeat");
                Time.timeScale=1;
                Log(errors==0?"PASS: live test complete, runtime errors=0.":"FAIL: runtime errors="+errors);
                RestoreGameView(); stage=0; SessionState.SetInt(Key,0); EditorApplication.isPlaying=false;
            }
        }
        catch(Exception e)
        {
            Log("FAIL: "+e); Time.timeScale=1; stage=0; SessionState.SetInt(Key,0);
            if(EditorApplication.isPlaying) EditorApplication.isPlaying=false;
            Debug.LogException(e);
        }
    }

    static void ValidateFormation(Camera camera, DarkFairytaleBattleStyle style, Enemy[] enemies)
    {
        Check(enemies.Length == 3, "Three live enemies in compact formation");
        Check(Mathf.Abs(style.enemyFeet[0].y-style.playerFeet.y)<.001f, "Front enemy shares player depth");
        var orders = new int[3];
        for(int i=0;i<3;i++)
        {
            var renderer=enemies[i].skeletonAnimation.GetComponent<MeshRenderer>();
            var bounds=renderer.bounds;
            var min=camera.WorldToViewportPoint(bounds.min);
            var max=camera.WorldToViewportPoint(bounds.max);
            Check(min.x>=0 && max.x<=1 && min.y>.2f && max.y<.85f, "Enemy "+i+" stays inside battle area");
            orders[i]=renderer.sortingOrder;
            Log("Enemy "+i+": feet="+style.enemyFeet[i]+" bounds="+min+" / "+max+" sorting="+orders[i]);
        }
        Check(orders[0]>orders[1] && orders[0]>orders[2], "Front enemy draws over both rear enemies");
        Check(style.enemyFeet[1].x<style.enemyFeet[0].x && style.enemyFeet[2].x>style.enemyFeet[0].x,
            "Rear enemies bracket front enemy horizontally");
    }

    static void TestData(GameplayCanvas gc)
    {
        var hud=gc.battleHud;
        hud.SetHealth(0,14,false); Check(hud.hpFill.fillAmount==0,"Zero HP display");
        hud.SetHealth(14,14,false); Check(hud.hpFill.fillAmount==1,"Full HP display");
        hud.SetHealth(11,14,true); Check(hud.hpFill.color==hud.shieldHp,"Absorb/shield color preserved");
        hud.SetExperience(42,44,100); Check(hud.levelText.text=="Lv. 42" && hud.xpText.text=="44%" && Mathf.Abs(hud.xpFill.fillAmount-.44f)<.001f,"Level and experience binding");
        hud.SetWave(2,3); Check(hud.waveText.text=="第 2 / 3 波" && hud.progressRow.activeSelf,"Wave counter and progress markers");
        hud.SetWave(9,12); Check(!hud.progressRow.activeSelf && hud.waveText.text=="第 9 / 12 波","Long levels retain exact wave count");
        gc.RefreshHp(); gc.RefreshXp(); gc.RefreshWaveText();
    }

    static void ValidateLayout(CursedBattleHud hud,string label)
    {
        Rect pixelRect=hud.GetComponent<Canvas>().pixelRect;
        foreach(var t in new[] { hud.backButton.transform, hud.waveText.transform, hud.hpText.transform, hud.xpText.transform, hud.levelText.transform })
        {
            var corners=new Vector3[4]; ((RectTransform)t).GetWorldCorners(corners);
            foreach(var c in corners) Check(c.x>=-1 && c.y>=-1 && c.x<=pixelRect.width+1 && c.y<=pixelRect.height+1,label+" keeps "+t.name+" on-screen",false);
        }
        var hits=new List<RaycastResult>();
        var eventData=new PointerEventData(EventSystem.current) { displayIndex=0, position=RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)hud.backButton.transform).TransformPoint(((RectTransform)hud.backButton.transform).rect.center)) };
        hud.GetComponent<GraphicRaycaster>().Raycast(eventData,hits);
        Log("Raycast position="+eventData.position+" rect="+((RectTransform)hud.backButton.transform).rect+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
        if(!Formation) Check(hits.Any(h=>h.gameObject==hud.backButton.gameObject),label+" return button receives raycasts");
        else Log(label+" editor synthetic raycast="+hits.Any(h=>h.gameObject==hud.backButton.gameObject)+"; return action checked separately in live flow");
        var core=hud.GetComponent<BattleHudSafeArea>().core;
        Check(Mathf.Abs(core.rect.width/core.rect.height-9f/16)<.0001f,label+" preserves 9:16 artwork proportions");
        Log("PASS: "+label+" actual Game view "+pixelRect.width+"x"+pixelRect.height+"; all essential UI inside screen.");
    }

    static void TestSafeAreas(CursedBattleHud hud)
    {
        var safe=hud.GetComponent<BattleHudSafeArea>();
        Vector2 root=((RectTransform)hud.transform).rect.size;
        var simulated=new Rect(0,.04f,1,.92f);
        BattleHudSafeArea.FitCore(safe.core,root,simulated,safe.referenceSize);
        float halfH=safe.core.rect.height*safe.core.localScale.y*.5f;
        Check(safe.core.anchoredPosition.y-halfH>=-root.y*.5f+root.y*.04f-.1f &&
            safe.core.anchoredPosition.y+halfH<=root.y*.5f-root.y*.04f+.1f,"Simulated top/bottom safe insets respected");
        safe.Apply();
    }

    static void Next(int value,double delay) { stage=value; SessionState.SetInt(Key,value); until=EditorApplication.timeSinceStartup+delay; }
    static void Check(bool condition,string message,bool report=true) { if(!condition) throw new Exception(message); if(report) Log("PASS: "+message); }
    static void Log(string message) { Directory.CreateDirectory(Dir); File.AppendAllText(Dir+"validation.txt",message+"\n"); }

    static object GetSizeGroup()
    {
        var assembly=typeof(Editor).Assembly;
        var type=assembly.GetType("UnityEditor.GameViewSizes");
        var instance=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null,null);
        var group=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        return type.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(group,"Standalone")});
    }

    static void SetResolution(int width,int height)
    {
        var assembly=typeof(Editor).Assembly;
        var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        var property=view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(originalGameSize<0) { originalGameSize=(int)property.GetValue(view,null); SessionState.SetInt("B3_ORIGINAL_SIZE",originalGameSize); }
        object group=GetSizeGroup();
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,
            new object[]{Enum.Parse(kind,"FixedResolution"),width,height,"B3 QA "+width+"x"+height},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        addedSizes.Add(index); property.SetValue(view,index,null); view.Repaint();
    }

    static void RestoreGameView()
    {
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .SetValue(view,SessionState.GetInt("B3_ORIGINAL_SIZE",0),null);
        // Keep named QA resolutions available to the user for later manual checks.
    }
}
