using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Spine.Unity;
using Scripts.Level;

// Explicit migration only; never rewrites prefabs during import or play.
[InitializeOnLoad]
public static class EnemyBarPrefabAuthoring
{
    const string Dir="output/enemy-bar-prefabs/";
    static EnemyBarPrefabAuthoring(){EditorApplication.update+=()=>{
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists(Dir+"preview.request")){File.Delete(Dir+"preview.request");CapturePrefab();}
        if(!File.Exists(Dir+"apply.request"))return;
        File.Delete(Dir+"apply.request");try{Apply();}catch(Exception e){File.WriteAllText(Dir+"result.txt",e.ToString());Debug.LogException(e);}
    };}
    public static void Apply(){
        Directory.CreateDirectory(Dir);
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Gameplay.unity");
        try{
            var am=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AssetManager>(true)).First();
            var camera=am.battleCampCamera.GetComponent<Camera>();camera.aspect=9f/16;
            var style=am.battleStyle;var template=style.enemyHealthBarPrefab;
            float height=camera.orthographicSize*2,width=height*camera.aspect;
            foreach(var row in LevelDataDescriptions.EnemyDb){
                var path=AssetDatabase.GUIDToAssetPath(row.assetRef.AssetGUID);
                var root=PrefabUtility.LoadPrefabContents(path);
                try{
                    var ab=root.GetComponent<Actor_Base>();var enemy=root.GetComponent<Enemy>();
                    var old=root.GetComponentInChildren<EnemyLifebar>(true);
                    bool boss=old.isBoss,mini=old.isMiniBoss;
                    var bar=((GameObject)PrefabUtility.InstantiatePrefab(template.gameObject,root.transform)).GetComponent<EnemyLifebar>();
                    bar.name="CursedStorybookEnemyBar";bar.transform.SetSiblingIndex(0);
                    bar.isBoss=boss;bar.isMiniBoss=mini;bar.miniBossName.text=boss||mini?enemy.bossName:"";
                    ab.enemyLifebar=bar;UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var skeleton=root.GetComponentInChildren<SkeletonAnimation>(true);skeleton.Initialize(false);skeleton.Skeleton.SetToSetupPose();skeleton.Update(0);skeleton.LateUpdate();
                    var bounds=skeleton.GetComponent<MeshRenderer>().bounds;
                    float fit=DarkFairytaleBattleStyle.DisplayFitScale(bounds.size,new Vector2(width,height),style.EnemyDisplaySize(0,boss||mini)*Mathf.Max(.1f,enemy.displaySizeMultiplier));
                    bar.transform.position=new Vector3(bounds.center.x,boss||mini?bounds.max.y+.28f/fit:bounds.min.y-height*.023f/fit,0);
                    float targetWidth=width*(boss||mini?.30f:.23f)/fit;
                    bar.transform.localScale=Vector3.one*(targetWidth/360f/Mathf.Abs(root.transform.lossyScale.x));
                    bar.hpText.color=new Color(.88f,.84f,.73f);
                    foreach(var outline in bar.GetComponentsInChildren<UnityEngine.UI.Outline>(true))outline.effectColor=new Color(.07f,.075f,.105f,.85f);
                    foreach(var graphic in bar.GetComponentsInChildren<UnityEngine.UI.Image>(true))graphic.raycastTarget=false;
                    bar.hpText.text=((int)ab.baseStat.hpMax)+"/"+((int)ab.baseStat.hpMax);
                    bar.GetComponent<Canvas>().worldCamera=null;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                    File.AppendAllText(Dir+"result.txt","PASS authored "+path+"\n");
                }finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();File.AppendAllText(Dir+"result.txt","COMPLETE\n");
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static System.Collections.IEnumerator ValidateBattle(AssetManager am){
        foreach(var row in LevelDataDescriptions.EnemyDb){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(row.assetRef.AssetGUID));
            var bars=prefab.GetComponentsInChildren<EnemyLifebar>(true);
            if(bars.Length!=1||prefab.GetComponent<Actor_Base>().enemyLifebar!=bars[0]||bars[0].name!="CursedStorybookEnemyBar")throw new Exception("Authored bar binding: "+row.key);
        }
        am.SpawnEnemy("MobRivenfang",Vector3.zero);
        while(am.spawningEnemy)yield return null;
        yield return new WaitForSecondsRealtime(2);
        var wolf=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).First(e=>e.name.StartsWith("MobRivenfang"));
        var bar=wolf.ab.enemyLifebar;
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);
            yield return new WaitForSecondsRealtime(.5f);
            am.battleStyle.PlaceEnemy(wolf,am.battleCampCamera.GetComponent<Camera>(),0);
            yield return new WaitForSecondsRealtime(2);
            if(wolf.ab.enemyLifebar!=bar||wolf.GetComponentsInChildren<EnemyLifebar>(true).Length!=1)throw new Exception("Runtime replaced the authored bar");
            wolf.ab.currentStat.hpNow=wolf.ab.currentStat.hpMax*.5f;bar.RefreshHp();
            yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+"battle-"+size.x+"x"+size.y+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
            File.AppendAllText(Dir+"validation.txt","PASS "+size+" authored bar retained; HP refresh; battle screenshot\n");
        }
        UnityEngine.Object.Destroy(wolf.gameObject);
    }

    static void CapturePrefab(){
        var scene=EditorSceneManager.NewPreviewScene();
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Monsters/MobHexmaw/MobHexmaw.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        var go=new GameObject("Prefab proof camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,root.scene);
        var camera=go.AddComponent<Camera>();camera.scene=root.scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.17f,.20f);
        RenderTexture rt=null;Texture2D tex=null;
        try{
            var sk=root.GetComponentInChildren<SkeletonAnimation>();sk.Initialize(false);sk.Update(0);sk.LateUpdate();
            var bounds=sk.GetComponent<MeshRenderer>().bounds;camera.orthographicSize=Mathf.Max(bounds.size.y*.9f,bounds.size.x*.55f);camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-20);
            var bar=root.GetComponentInChildren<EnemyLifebar>();bar.GetComponent<Canvas>().worldCamera=camera;Canvas.ForceUpdateCanvases();
            rt=new RenderTexture(900,700,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            tex=new Texture2D(900,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,700),0,0);tex.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Dir+"prefab-MobHexmaw.png",tex.EncodeToPNG());
        }finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);if(rt)UnityEngine.Object.DestroyImmediate(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }

}
