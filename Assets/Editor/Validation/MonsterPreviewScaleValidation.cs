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
public static class MonsterPreviewScaleValidation
{
    const string Dir = "output/monster-preview-scale/";
    static readonly string[] Ids = { "MobTallowkin", "MobGnarlo", "MobDrowseed", "MobGloamweb", "MobHexmaw", "MobRivenfang", "MobVesperling", "MobBramblet", "MobSplinterjaw", "MobMurmlet", "MobStillmere" };
    static string[] ActiveIds => string.IsNullOrEmpty(SessionState.GetString("MONSTER_SCALE_TARGET", "")) ? Ids : new[]{SessionState.GetString("MONSTER_SCALE_TARGET", "")};
    static int index, phase, ratio, errors;
    static double next, started;
    static AsyncOperationHandle<GameObject> addressLoad, databaseLoad;
    static readonly int[] Width = {720,768,720}, Height = {1280,1024,1440};
    static MonsterPreviewScaleValidation() { EditorApplication.update += Tick; Application.logMessageReceived += LogError; }
    static void LogError(string message, string stack, LogType type) { if (phase > 0 && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) { errors++; Log("ERROR " + message); } }
    static void Log(string message) { File.AppendAllText(Dir + "validation.txt", message + "\n"); }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Log("PASS " + message); }
    static void Resolution(int w,int h) { typeof(MonsterPreviewValidation).GetMethod("SetResolution", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h}); }
    static float BattleRatio(GameObject prefab, MonsterAnimationTest tester)
    {
        var parent = new GameObject("Inactive battle-fit verification");parent.SetActive(false);
        try {
            var enemy = UnityEngine.Object.Instantiate(prefab,parent.transform);
            var player = UnityEngine.Object.Instantiate(tester.stage.battlePlayerPrefab,parent.transform);
            var fit=typeof(DarkFairytaleBattleStyle).GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Static);
            var style=tester.stage.battleStyle;
            var e=enemy.GetComponentInChildren<SkeletonAnimation>(true);var p=player.GetComponentInChildren<SkeletonAnimation>(true);
            fit.Invoke(null,new object[]{player.transform,p,tester.previewCamera,style.playerFeet,style.playerSize});
            int slot=tester.stage.battleEnemySlot;
            var bar=enemy.GetComponentInChildren<EnemyLifebar>(true);
            fit.Invoke(null,new object[]{enemy.transform,e,tester.previewCamera,style.enemyFeet[slot],style.EnemyDisplaySize(slot,bar && (bar.isBoss||bar.isMiniBoss))*Mathf.Max(.1f,enemy.GetComponent<Enemy>().displaySizeMultiplier)});
            return e.transform.lossyScale.x/p.transform.lossyScale.x;
        } finally {UnityEngine.Object.DestroyImmediate(parent);}
    }
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
                } else if (cmd == "validate" || cmd.StartsWith("validate ", StringComparison.Ordinal)) {
                    if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play first");
                    string target=cmd.Length>8 ? cmd.Substring(9).Trim() : "";
                    if(target.Length>0 && !Ids.Contains(target)) throw new Exception("Unknown monster " + target);
                    SessionState.SetString("MONSTER_SCALE_TARGET",target);
                    var scene = EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);
                    var configured = UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
                    if (!configured.stage.battleStyle) configured.stage.battleStyle = AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");
                    if (!configured.stage.battlePlayerPrefab) configured.stage.battlePlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Characters/RedHood/Prefabs/Player.prefab");
                    EditorUtility.SetDirty(configured.stage); EditorSceneManager.SaveScene(scene);
                    File.WriteAllText(Dir+"validation.txt", "Monster preview: fixed animation scale and battle-relative size checks\n");
                    errors=0; index=0; ratio=0;
                    SessionState.SetBool("MONSTER_SCALE_VALIDATION_START",true);
                    EditorApplication.isPlaying=true;
                } else throw new Exception("Unknown command " + cmd);
            }
            if (SessionState.GetBool("MONSTER_SCALE_VALIDATION_START",false) && EditorApplication.isPlaying) {
                SessionState.SetBool("MONSTER_SCALE_VALIDATION_START",false);phase=1;index=0;ratio=0;errors=0;started=EditorApplication.timeSinceStartup;next=started+2;
            }
            if (phase==0 || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next) return;
            if (EditorApplication.timeSinceStartup-started>300) throw new Exception("Validation timeout");
            var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();
            if(!t || !t.hud)return;
            string id=ActiveIds[index];
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
                Check(Mathf.Approximately(t.CombatSizeMultiplier, prefab.GetComponent<Enemy>().displaySizeMultiplier), id+" preview uses prefab size multiplier="+t.CombatSizeMultiplier);
                t.stage.InitializePreview();t.stage.Apply();
                var originalScale=t.character.transform.lossyScale;var originalPosition=t.character.transform.position;
                var playerSource=t.stage.battlePlayerPrefab.GetComponentInChildren<SkeletonAnimation>(true);
                var expectedRatio=BattleRatio(prefab,t);
                Check(Mathf.Abs(t.character.transform.lossyScale.x/t.stage.redHood.transform.lossyScale.x-expectedRatio)<.0001f,id+" actual battle Fit relative scale including parent transforms");
                foreach(var clip in t.AnimationNames) {
                    t.Play(Array.IndexOf(t.AnimationNames,clip));
                    t.character.Update(t.character.Skeleton.Data.FindAnimation(clip).Duration*.5f);t.character.LateUpdate();t.stage.Apply();
                    Check(Vector3.Distance(t.character.transform.lossyScale,originalScale)<.0001f && Vector3.Distance(t.character.transform.position,originalPosition)<.0001f,id+" fixed scale and anchor: "+clip);
                }
                t.SetZoom(1.5f);t.stage.Apply();
                Check(Mathf.Abs(t.character.transform.lossyScale.x/t.stage.redHood.transform.lossyScale.x-expectedRatio)<.0001f,id+" zoom preserves battle ratio");
                t.FitCamera();t.stage.Apply();
                Check(Vector3.Distance(t.character.transform.lossyScale,originalScale)<.0001f && Vector3.Distance(t.character.transform.position,originalPosition)<.0001f,id+" reset from last animation restores same scale and anchor");
                t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));
                Addressables.Release(addressLoad);Addressables.Release(databaseLoad);
                ratio=0;Resolution(Width[ratio],Height[ratio]);next=EditorApplication.timeSinceStartup+1;phase=3;return;
            }
            if(phase==3) {
                Check(t.hud.selectedTitle.text == CreatureNames.DisplayName(id),id+" selected label at "+Width[ratio]+"x"+Height[ratio]);
                ScreenCapture.CaptureScreenshot(Dir+id+"-"+Width[ratio]+"x"+Height[ratio]+".png");
                phase=7;next=EditorApplication.timeSinceStartup+.7;return;
            }
            if(phase==7) {
                t.Play(Array.IndexOf(t.AnimationNames,"Attack1"));t.SetPaused(true);
                t.character.AnimationState.Update(t.character.Skeleton.Data.FindAnimation("Attack1").Duration*.5f);
                t.character.AnimationState.Apply(t.character.Skeleton);t.character.Skeleton.UpdateWorldTransform();t.character.LateUpdate();
                phase=8;next=EditorApplication.timeSinceStartup+.5;return;
            }
            if(phase==8) {
                ScreenCapture.CaptureScreenshot(Dir+id+"-attack-"+Width[ratio]+"x"+Height[ratio]+".png");
                phase=5;next=EditorApplication.timeSinceStartup+.7;return;
            }
            if(phase==5) {
                t.SetPaused(false);t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));
                if(++ratio<3){Resolution(Width[ratio],Height[ratio]);phase=3;next=EditorApplication.timeSinceStartup+1;return;}
                if(++index<ActiveIds.Length){phase=1;next=EditorApplication.timeSinceStartup+.2;return;}
                index=ActiveIds.Length-1;ratio=0;Resolution(Width[ratio],Height[ratio]);t.hud.SetCatalogOpen(true);t.hud.searchField.text="Mob";t.hud.catalogScroll.verticalNormalizedPosition=0;phase=4;next=EditorApplication.timeSinceStartup+1;return;
            }
            if(phase==4) {
                ScreenCapture.CaptureScreenshot(Dir+"catalog-"+Width[ratio]+"x"+Height[ratio]+".png");
                phase=6;next=EditorApplication.timeSinceStartup+.7;return;
            }
            if(phase==6) {
                if(++ratio<3){Resolution(Width[ratio],Height[ratio]);phase=4;next=EditorApplication.timeSinceStartup+1;return;}
                t.hud.searchField.text="";t.hud.SetCatalogOpen(false);Resolution(720,1280);
                Log((errors==0?"PASS":"FAIL")+" complete; monsters="+ActiveIds.Length+"; errors="+errors);phase=0;
            }
        } catch(Exception e) {
            if(addressLoad.IsValid())Addressables.Release(addressLoad); if(databaseLoad.IsValid())Addressables.Release(databaseLoad);
            phase=0;Log("FAIL "+e);Debug.LogException(e);
        }
    }
}
