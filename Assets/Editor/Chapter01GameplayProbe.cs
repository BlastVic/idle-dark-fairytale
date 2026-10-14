using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit live integration capture. QA persistence guard prevents changes to the user's save.
[InitializeOnLoad]
public static class Chapter01GameplayProbe
{
    const string Key="CH1_ENV_QA_STAGE";
    static int stage,node,ratio,errors;
    static string[] route;
    static double until,started;
    static readonly int[] widths={720,768,720},heights={1280,1024,1440};
    static readonly string[] names={"9x16","3x4","1x2"};
    static string Dir=>Chapter01EnvironmentBuilder.Output;
    static Chapter01GameplayProbe(){
        stage=SessionState.GetInt(Key,0);EditorApplication.update+=Tick;
        Application.logMessageReceived+=OnLog;
    }
    static void OnLog(string text,string stack,LogType type){if(stage>0&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)){errors++;Log("ERROR "+text);}}
    [MenuItem("Tools/Dark Fairytale/Environment/Test Chapter 01 in Gameplay")]
    public static void Begin(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty && current.path!="Assets/Scenes/MainScenes/Gameplay.unity")throw new Exception("Cannot switch away from an unsaved scene");
        SessionState.SetString("CH1_ENV_RETURN_SCENE",current.path);
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"/gameplay-validation.txt","Five node live Gameplay / Addressables / environment binding / aspect validation\n");
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        if(current.path!="Assets/Scenes/MainScenes/Gameplay.unity")EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
        Next(1,0);EditorApplication.isPlaying=true;
    }
    static void Log(string s){File.AppendAllText(Dir+"/gameplay-validation.txt",s+"\n");}
    static void Next(int s,double wait){stage=s;SessionState.SetInt(Key,s);until=EditorApplication.timeSinceStartup+wait;}
    static void Resolution(int i){typeof(CursedStorybookUiValidation).GetMethod("SetResolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{widths[i],heights[i]});EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();}
    static void Tick(){
        if(stage==0||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{
            if(!EditorApplication.isPlaying)return;
            if(stage==1){
                if(!Player.single||Player.single.ab.currentStat==null||!WaveManager.single||!GameplayCanvas.single)return;
                started=EditorApplication.timeSinceStartup;node=0;ratio=0;errors=0;Time.timeScale=1;
                GameManager.single.xpNow=0;GameManager.single.xpMax=100000;
                GameplayCanvas.single.CancelInvoke("TryShowingXpBoosterNotification");
                GameplayCanvas.single.xpBoosterNotification.SetActive(false);
                route=WaveManager.single.waveDb.Take(5).Select(w=>w.levelKey).ToArray();
                if(!route.SequenceEqual(Chapter01EnvironmentBuilder.Levels))throw new Exception("Current first five route keys differ from environment bindings: "+string.Join(", ",route));
                Log("Actual first five route keys: "+string.Join(" -> ",route));
                foreach(var key in route){
                    var wave=WaveManager.single.waveDb.First(w=>w.levelKey==key);
                    int i=Array.IndexOf(Chapter01EnvironmentBuilder.Levels,key);
                    var catalog=Resources.Load<ChapterEnvironmentCatalog>("Chapter01Environments");
                    if(!catalog || catalog.Find(wave.mapGraphicKey,key)!=catalog.entries[i].prefab)throw new Exception("Wrong saved node binding "+key);
                    // Only this play-session replaces encounters with a three-enemy layout fixture.
                    wave.waves=new[]{new Wave{enemies=new[]{"MobWaxcap","MobWoodcap","MobDreamcap"}}};
                    wave.isBoss=false;wave.isMiniBoss=false;
                }
                StartNode();return;
            }
            if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Live environment test timeout stage="+stage);
            var assets=LevelController.Instance.AssetManager;
            if(stage==2){
                Player.single.StopAllCoroutines();Player.single.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);
                Player.single.ab.currentStat.hpNow=Player.single.ab.currentStat.hpMax;
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                foreach(var enemy in enemies)if(enemy.ab.currentStat!=null)enemy.ab.currentStat.hpNow=enemy.ab.currentStat.hpMax;
                if(enemies.Length!=3||enemies.Any(e=>e.IsEntering))return;
                var maps=GameObject.FindGameObjectsWithTag("MapGraphics");
                var expected=Resources.Load<ChapterEnvironmentCatalog>("Chapter01Environments").entries[node].prefab.GetComponent<SpriteRenderer>().sprite;
                if(maps.Length!=1||maps[0].GetComponent<SpriteRenderer>().sprite!=expected)throw new Exception("Node did not replace environment exactly once");
                Log("PASS "+Chapter01EnvironmentBuilder.Levels[node]+" -> "+maps[0].name+"; 3 live enemies, one map");
                Time.timeScale=0;ratio=0;Resolution(ratio);Next(3,1);return;
            }
            if(EditorApplication.timeSinceStartup<until)return;
            if(stage==7){
                if(!assets.mapPackageObj || !assets.mapPackageObj.activeInHierarchy)return;
                var mapNode=assets.mapPackageObj.GetComponentsInChildren<MapNode>(true).Single(n=>n.mapFullName==route[node]);
                if(!mapNode.gameObject.activeInHierarchy)throw new Exception("Map node inactive: "+route[node]);
                // The QA session unlocks the button only; its saved click event stays intact.
                mapNode.myButton.interactable=true;
                mapNode.myButton.onClick.Invoke();
                Log("CLICK actual MapNode button: "+mapNode.mapFullName);
                Next(2,0);return;
            }
            if(stage==3){
                var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(Player.single,camera);
                var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).OrderBy(e=>e.name).ToArray();
                for(int i=0;i<3;i++){
                    assets.battleStyle.PlaceEnemy(enemies[i],camera,i);enemies[i].GetComponent<DropIn>().ResetForLayout();
                    var sk=enemies[i].skeletonAnimation;sk.AnimationState.ClearTracks();sk.Skeleton.SetToSetupPose();sk.AnimationState.SetAnimation(0,"Idle1",true);sk.Update(0);sk.LateUpdate();
                }
                var root=GameObject.FindGameObjectWithTag("MapGraphics");
                foreach(var sk in root.GetComponentsInChildren<SkeletonAnimation>()){
                    var track=sk.AnimationState.GetCurrent(0);if(track!=null){track.TrackTime=1;sk.Update(0);sk.LateUpdate();}
                }
                GameplayCanvas.single.battleHud.GetComponent<BattleHudSafeArea>().Apply();Canvas.ForceUpdateCanvases();Next(4,1);
            } else if(stage==4){
                ScreenCapture.CaptureScreenshot(Dir+"/gameplay-node0"+(node+1)+"-"+names[ratio]+".png");
                Log("CAPTURE requested node="+(node+1)+" "+widths[ratio]+"x"+heights[ratio]+"; editor Screen="+Screen.width+"x"+Screen.height);Next(5,.8);
            } else if(stage==5){
                string capture=Dir+"/gameplay-node0"+(node+1)+"-"+names[ratio]+".png";
                var captured=new Texture2D(2,2);captured.LoadImage(File.ReadAllBytes(capture));int w=captured.width,h=captured.height;UnityEngine.Object.DestroyImmediate(captured);
                if(w!=widths[ratio]||h!=heights[ratio])throw new Exception("Captured Game View size mismatch: "+w+"x"+h);
                Log("PASS actual Gameplay PNG "+w+"x"+h);
                ratio++;if(ratio<3){Resolution(ratio);Next(3,1);}
                else{Time.timeScale=1;GameplayCanvas.single.battleHud.backButton.onClick.Invoke();Next(6,2);}
            } else if(stage==6){
                if(GameObject.FindGameObjectsWithTag("MapGraphics").Length!=0)throw new Exception("Environment leaked after return to camp");
                Log("PASS node="+(node+1)+" return to camp destroys environment");node++;
                if(node<5)StartNode();else Finish((errors==0?"PASS":"FAIL")+": five node transitions, 15 actual Gameplay captures, errors="+errors);
            }
        }catch(Exception e){Finish("FAIL: "+e);Debug.LogException(e);}
    }
    static void StartNode(){
        var gc=GameplayCanvas.single;gc.victoryPanel.SetActive(false);gc.defeatPanel.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        var assets=LevelController.Instance.AssetManager;
        assets.HidePlayer();gc.ToggleBattleUi(false);gc.ToggleCampUi(false);
        assets.TurnOnMapPackage();GameManager.single.SetGameState(Assets.Scripts.Game.Enum.GameStateType.MAP);
        Next(7,1.5);
    }
    static void Finish(string message){Log(message);Next(0,0);Time.timeScale=1;
        typeof(CursedStorybookUiValidation).GetMethod("RestoreGameView",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        EditorApplication.isPlaying=false;
        // Save guard stays true until teardown completes, then the request runner releases it.
        SessionState.SetBool("CH1_ENV_CLEANUP",true);
    }
}
