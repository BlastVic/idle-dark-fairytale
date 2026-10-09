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
    const string Dir = "output/battle-ui/";
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
                if(!EditorApplication.isPlaying && File.Exists(Dir+"validate.request")) { File.Delete(Dir+"validate.request"); Begin(); }
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
                BlackForestMushroomBuilder.RunFirstLevel();
                Next(2,2);
            }
            else if(stage==2)
            {
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                if(!gc.battleHud.gameObject.activeInHierarchy || enemies.Length==0 || enemies.Any(e=>e.GetComponent<DropIn>().isFalling)) return;
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
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).OrderBy(e=>e.transform.position.x).ToArray();
                for(int i=0;i<enemies.Length;i++) { assets.battleStyle.PlaceEnemy(enemies[i],camera,i); enemies[i].GetComponent<DropIn>().ResetForLayout(); }
                gc.battleHud.GetComponent<BattleHudSafeArea>().Apply();
                Canvas.ForceUpdateCanvases();
                ValidateLayout(gc.battleHud,Names[ratio]);
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
                    Player.single.currentAnimation="";
                    Player.single.isAttacking=false;
                    Player.single.StartCoroutine(Player.single.AttackComplete());
                    Next(6,1);
                }
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
        var eventData=new PointerEventData(EventSystem.current) { position=hud.backButton.transform.position };
        hud.GetComponent<GraphicRaycaster>().Raycast(eventData,hits);
        Check(hits.Any(h=>h.gameObject==hud.backButton.gameObject),label+" return button receives raycasts");
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
