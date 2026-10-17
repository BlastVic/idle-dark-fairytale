using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Game.Enum;
using Scripts.Level;

[InitializeOnLoad]
public static class LocalPrefabPackagingValidation
{
    const string Dir="output/packaging-fix/", Flag="IDF_LOCAL_PACKAGING_QA";
    static bool running; static double deadline; static int errors;
    static LocalPrefabPackagingValidation()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Flag,false)&&(t==LogType.Error||t==LogType.Exception)){errors++;Log("ERROR "+m);}};
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag,false)){
            SessionState.SetBool(Flag,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.EraseInt("SETTINGS_QA_SKIP");running=false;
            var previous=SessionState.GetString(Flag+"_scene","");if(!string.IsNullOrEmpty(previous))EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(previous);
        }};
    }
    static void Log(string s){File.AppendAllText(Dir+"validation.txt",s+"\n");}
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Log("PASS "+s);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"restart.request")){
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
            File.Delete(Dir+"restart.request");SessionState.SetBool(Flag+"_started",false);running=false;
            File.WriteAllText(Dir+"validate.request","");
        }
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"validate.request")){
            File.Delete(Dir+"validate.request");SessionState.SetBool(Flag+"_started",false);Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"validation.txt","");
            try{
                if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
                LocalPrefabPackaging.Prepare();
                SessionState.SetString(Flag+"_scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Flag,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetInt("SETTINGS_QA_SKIP",1);
                EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
            }catch(Exception e){Log("FAIL "+e);}
            return;
        }
        if(!EditorApplication.isPlaying||!SessionState.GetBool(Flag,false))return;
        // Route checks trigger victory/defeat explicitly; prevent live combat racing those transitions.
        if(GameManager.single&&GameManager.single.gameState==GameStateType.BATTLE&&Player.single&&Player.single.ab&&Player.single.ab.currentStat!=null){Player.single.ab.currentStat.hpMax=1000000;Player.single.ab.currentStat.hpNow=1000000;}
        if(!running){running=true;deadline=EditorApplication.timeSinceStartup+180;}
        if(EditorApplication.timeSinceStartup>deadline){Log("FAIL timeout");EditorApplication.isPlaying=false;return;}
        if(!GameManager.single||!GameplayCanvas.single||!LevelController.Instance||GameManager.single.gameState!=GameStateType.CAMP)return;
        var intro=UnityEngine.Object.FindAnyObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;
        if(SessionState.GetBool(Flag+"_started",false))return;
        SessionState.SetBool(Flag+"_started",true);errors=0;
        GameManager.single.StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){Log("FAIL "+e);break;}yield return current;}
        SessionState.SetBool(Flag+"_started",false);EditorApplication.isPlaying=false;
    }
    static IEnumerator Run()
    {
        var gm=GameManager.single;var gc=GameplayCanvas.single;var am=LevelController.Instance.AssetManager;
        var eventSystem=UnityEngine.EventSystems.EventSystem.current;
        Check(eventSystem && eventSystem.currentInputModule is UnityEngine.EventSystems.StandaloneInputModule,"legacy UI input module active");
        // Exercise the same APIs used by navigation, dragging, and Android back handling.
        _ = Input.mousePosition; _ = Input.GetMouseButton(0); _ = Input.GetKeyDown(KeyCode.Escape); _ = Input.touchCount;
        Check(true,"legacy pointer, touch and back-key APIs available");
        if(TutorialManager.single){TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();}
        // Resource-loading checks must not wait for the interactive chapter-ending story.
        if(!gm.HasSeenThis(CursedBookIntro.ChapterEndingSeenKey))gm.thingsSeenPermanent.Add(CursedBookIntro.ChapterEndingSeenKey);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        Check(gc.homeUI&&gc.homeUI.gameObject.activeInHierarchy,"startup reaches shelter through local MapPackage");
        var rows=LevelDataDescriptions.EnemyDb.Concat(LevelDataDescriptions.MapDb).Concat(LevelDataDescriptions.ParticleDb).Concat(LevelDataDescriptions.GeneralObjectsDb).ToArray();
        foreach(var row in rows){var request=Resources.LoadAsync<LocalPrefabEntry>(LocalPrefabLoader.ResourceFolder+"/"+row.assetRef.AssetGUID);yield return request;var entry=request.asset as LocalPrefabEntry;Check(entry&&entry.prefab,"resource key "+row.key);}
        yield return gm.StartCoroutine(Router.single.GoToFusion());Check(am.fusionPackageObj,"fusion loaded");
        yield return gm.StartCoroutine(Router.single.GoToCampFromFusion());Check(gc.homeUI.gameObject.activeInHierarchy,"fusion returns home");
        yield return gm.StartCoroutine(Router.single.GoToRebirth());Check(am.rebirthPackageObj,"rebirth loaded");
        yield return gm.StartCoroutine(Router.single.GoToCampFromRebirth());Check(gc.homeUI.gameObject.activeInHierarchy,"rebirth returns home");
        yield return gm.StartCoroutine(Router.single.GoToCashMall());Check(CashMallPackage.single&&CashMallPackage.single.view,"shop loaded");
        yield return gm.StartCoroutine(Router.single.GoToCampFromCashMall());Check(gc.homeUI.gameObject.activeInHierarchy,"shop returns home");
        yield return gm.StartCoroutine(Router.single.GoToLastBattle());yield return new WaitForSeconds(3);
        var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);Check(enemies.Length>0,"battle spawns enemies");
        if(File.Exists("output/enemy-bar-prefabs/validate.request")){File.Delete("output/enemy-bar-prefabs/validate.request");yield return EnemyBarPrefabAuthoring.ValidateBattle(am);}
        foreach(var row in LevelDataDescriptions.ParticleDb){var handle=LocalPrefabLoader.InstantiateAsync(row.assetRef,Vector3.zero,Quaternion.identity);yield return handle;Check(handle.Status==UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded,"instantiate effect "+row.key);LocalPrefabLoader.ReleaseInstance(handle);}
        WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();gm.BattleCleanup();yield return null;
        yield return gm.StartCoroutine(Router.single.GoToVictory());Check(gc.victoryPanel.activeSelf,"victory flow");
        yield return gm.StartCoroutine(Router.single.GoToCampFromBattle());Check(gc.homeUI.gameObject.activeInHierarchy,"victory returns home");
        yield return gm.StartCoroutine(Router.single.GoToLastBattle());yield return new WaitForSeconds(2);
        WaveManager.single.StopAllCoroutines();WaveManager.single.CancelInvoke();gm.BattleCleanup();yield return null;
        yield return gm.StartCoroutine(Router.single.GoToDefeat());Check(gc.defeatPanel.activeSelf,"defeat flow");
        yield return gm.StartCoroutine(Router.single.GoToCampFromBattle());Check(gc.homeUI.gameObject.activeInHierarchy,"defeat returns home");
        Check(errors==0,"no runtime errors");Log("COMPLETE");
    }
}
