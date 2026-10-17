using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Spine.Unity;

// Explicit local requests only. Never rebuilds monster assets or UI.
[InitializeOnLoad]
public static class MonsterNameMigrationValidation
{
    const string Dir = "output/monster-name-migration/";
    static readonly string[] Ids = { "MobTallowkin", "MobGnarlo", "MobDrowseed", "MobGloamweb", "MobHexmaw", "MobRivenfang", "MobVesperling", "MobBramblet", "MobSplinterjaw" };
    static int index, phase, ratio, errors;
    static double next, started;
    static AsyncOperationHandle<GameObject> addressLoad, databaseLoad;
    static readonly int[] Width = {720,768,720}, Height = {1280,1024,1440};
    static MonsterNameMigrationValidation() { EditorApplication.update += Tick; Application.logMessageReceived += LogError; }
    static void LogError(string message, string stack, LogType type) { if (phase > 0 && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) { errors++; Log("ERROR " + message); } }
    static void Log(string message) { File.AppendAllText(Dir + "validation.txt", message + "\n"); }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Log("PASS " + message); }
    static void Resolution(int w,int h) { typeof(MonsterPreviewValidation).GetMethod("SetResolution", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h}); }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try {
            string request = Dir + "command.request";
            if (File.Exists(request)) {
                string cmd = File.ReadAllText(request).Trim();
                if (cmd == "lock" && EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
                File.Delete(request);
                if (cmd == "lock") {
                    Check(EditorSceneManager.SaveOpenScenes(), "Open scenes saved before rename"); AssetDatabase.SaveAssets();
                    EditorApplication.LockReloadAssemblies(); AssetDatabase.DisallowAutoRefresh();
                    File.WriteAllText(Dir+"command-result.txt", "LOCKED");
                } else if (cmd == "unlock") {
                    AssetDatabase.AllowAutoRefresh(); EditorApplication.UnlockReloadAssemblies();
                    File.WriteAllText(Dir+"command-result.txt", "UNLOCKED"); AssetDatabase.Refresh();
                } else if (cmd == "validate") {
                    if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first");
                    var ui=PrefabUtility.LoadPrefabContents("Assets/Tests/MonsterAnimation/UI/MonsterPreviewHUD.prefab");
                    try {
                        var hud=ui.GetComponent<MonsterPreviewHud>();
                        foreach(var card in hud.cards.Where(c=>c.data && Ids.Contains(c.data.name.Replace("_SkeletonData","")))) {
                            card.displayName=MonsterPreviewUiBuilder.DisplayName(card.data.name);card.title.text=card.displayName;
                            card.title.resizeTextForBestFit=true;card.title.resizeTextMinSize=10;card.title.resizeTextMaxSize=20;
                        }
                        hud.selectedTitle.resizeTextForBestFit=true;hud.selectedTitle.resizeTextMinSize=12;hud.selectedTitle.resizeTextMaxSize=24;
                        PrefabUtility.SaveAsPrefabAsset(ui,"Assets/Tests/MonsterAnimation/UI/MonsterPreviewHUD.prefab");
                    } finally {UnityEngine.Object.DestroyImmediate(ui);}
                    EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);
                    File.WriteAllText(Dir+"validation.txt", "Monster name migration: actual runtime address/database/catalog checks\n");
                    errors=0; index=0; ratio=0;
                    SessionState.SetBool("MONSTER_NAME_VALIDATION_START",true);
                    EditorApplication.isPlaying=true;
                } else throw new Exception("Unknown command " + cmd);
            }
            if (SessionState.GetBool("MONSTER_NAME_VALIDATION_START",false) && EditorApplication.isPlaying) {
                SessionState.SetBool("MONSTER_NAME_VALIDATION_START",false);phase=1;index=0;ratio=0;errors=0;started=EditorApplication.timeSinceStartup;next=started+2;
            }
            if (phase==0 || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next) return;
            if (EditorApplication.timeSinceStartup-started>180) throw new Exception("Validation timeout");
            var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
            if(!t || !t.hud)return;
            string id=Ids[index];
            if(phase==1) {
                var entries=Scripts.Level.LevelDataDescriptions.EnemyDb.Where(e=>e.key==id).ToArray();
                Check(entries.Length==1,id+" unique database key");
                addressLoad=Addressables.LoadAssetAsync<GameObject>(id);
                databaseLoad=Addressables.LoadAssetAsync<GameObject>(entries[0].assetRef.RuntimeKey);
                phase=2; return;
            }
            if(phase==2) {
                if(!addressLoad.IsDone || !databaseLoad.IsDone)return;
                Check(addressLoad.Status==AsyncOperationStatus.Succeeded && databaseLoad.Status==AsyncOperationStatus.Succeeded && addressLoad.Result==databaseLoad.Result && addressLoad.Result.name==id,id+" exact address and database load same prefab");
                var prefab=addressLoad.Result;
                Check(prefab.GetComponent<Enemy>() && prefab.GetComponent<Actor_Base>(),id+" enemy components");
                foreach(var tr in prefab.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)==0,id+" script "+tr.name);
                var sa=prefab.GetComponentInChildren<SkeletonAnimation>(true); var data=sa.skeletonDataAsset.GetSkeletonData(false);
                Check(data!=null && new[]{"Idle1","Attack1","Hit","Death1"}.All(a=>data.FindAnimation(a)!=null),id+" Spine and four game animations");
                Check(sa.skeletonDataAsset.atlasAssets.All(a=>a!=null && a.Materials.All(m=>m && m.mainTexture)),id+" atlas/material/texture references");
                int ix=Array.FindIndex(t.monsters,d=>d.name==id+"_SkeletonData"); Check(ix>=0,id+" catalog data");
                var card=t.hud.cards.Single(c=>c.data==t.monsters[ix]);
                Check(card.portrait.sprite && card.displayName == CreatureNames.DisplayName(id),id+" persistent portrait and name");
                t.hud.searchField.text=id.ToLowerInvariant(); Check(t.hud.cards.Count(c=>c.gameObject.activeSelf)==1,id+" exact search");
                card.button.onClick.Invoke();
                Check(t.SelectedMonster==ix && t.character.valid && string.IsNullOrEmpty(t.Error),id+" real preview selection");
                t.hud.searchField.text="";
                Addressables.Release(addressLoad);Addressables.Release(databaseLoad);
                ratio=0;Resolution(Width[ratio],Height[ratio]);next=EditorApplication.timeSinceStartup+1;phase=3;return;
            }
            if(phase==3) {
                Check(t.hud.selectedTitle.text == CreatureNames.DisplayName(id),id+" selected label at "+Width[ratio]+"x"+Height[ratio]);
                ScreenCapture.CaptureScreenshot(Dir+id+"-"+Width[ratio]+"x"+Height[ratio]+".png");
                phase=5;next=EditorApplication.timeSinceStartup+.7;return;
            }
            if(phase==5) {
                if(++ratio<3){Resolution(Width[ratio],Height[ratio]);phase=3;next=EditorApplication.timeSinceStartup+1;return;}
                if(++index<Ids.Length){phase=1;next=EditorApplication.timeSinceStartup+.2;return;}
                index=Ids.Length-1;ratio=0;Resolution(Width[ratio],Height[ratio]);t.hud.SetCatalogOpen(true);t.hud.searchField.text="Mob";t.hud.catalogScroll.verticalNormalizedPosition=0;phase=4;next=EditorApplication.timeSinceStartup+1;return;
            }
            if(phase==4) {
                ScreenCapture.CaptureScreenshot(Dir+"catalog-"+Width[ratio]+"x"+Height[ratio]+".png");
                phase=6;next=EditorApplication.timeSinceStartup+.7;return;
            }
            if(phase==6) {
                if(++ratio<3){Resolution(Width[ratio],Height[ratio]);phase=4;next=EditorApplication.timeSinceStartup+1;return;}
                t.hud.searchField.text="";t.hud.SetCatalogOpen(false);Resolution(720,1280);
                Log((errors==0?"PASS":"FAIL")+" complete; nine monsters; errors="+errors);phase=0;
            }
        } catch(Exception e) {
            if(addressLoad.IsValid())Addressables.Release(addressLoad); if(databaseLoad.IsValid())Addressables.Release(databaseLoad);
            phase=0;Log("FAIL "+e);Debug.LogException(e);
        }
    }
}
