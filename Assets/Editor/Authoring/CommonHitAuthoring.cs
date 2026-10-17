using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

public static class CommonHitAuthoring
{
    const string Folder="Assets/DarkFairytale/Effects/Common/LiubeiHit";
    static float BattleHeight()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Gameplay.unity");
        try{var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.name=="Main Camera - Battle - Camp - (KEEP OFF)");return DarkFairytaleBattleStyle.WorldSize(camera).y;}
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        AssetDatabase.Refresh();
        string texture=Folder+"/liubei_atk_beaten_1.png";
        var ti=(TextureImporter)AssetImporter.GetAtPath(texture);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Hit.mat");
        if(!material){material=new Material(Shader.Find("DarkFairytale/Yellow White Hit"));AssetDatabase.CreateAsset(material,Folder+"/Hit.mat");}
        material.shader=Shader.Find("DarkFairytale/Yellow White Hit");
        material.SetFloat("_WhiteOpacity",.15f);material.SetFloat("_YellowOpacity",.45f);
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);material.SetFloat("_StraightAlphaInput",1);material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(material);
        var atlas=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Folder+"/Hit_Atlas.asset");if(!atlas){atlas=ScriptableObject.CreateInstance<SpineAtlasAsset>();AssetDatabase.CreateAsset(atlas,Folder+"/Hit_Atlas.asset");}
        atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Hit.atlas.txt");atlas.materials=new[]{material};atlas.Clear();EditorUtility.SetDirty(atlas);
        var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Folder+"/Hit_SkeletonData.asset");if(!data){data=ScriptableObject.CreateInstance<SkeletonDataAsset>();AssetDatabase.CreateAsset(data,Folder+"/Hit_SkeletonData.asset");}
        data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Hit.json");data.atlasAssets=new AtlasAssetBase[]{atlas};data.scale=.01f;data.Clear();EditorUtility.SetDirty(data);
        var sd=data.GetSkeletonData(false);if(sd==null)throw new Exception("Hit skeleton failed to load");
        var path=CommonBattleEffectsAuthoring.PathAt(0);var root=PrefabUtility.LoadPrefabContents(path);
        try {
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            for(int i=root.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach(var c in root.GetComponents<Component>())if(c&&!(c is Transform))UnityEngine.Object.DestroyImmediate(c);
            root.transform.localScale=Vector3.one*3f;root.transform.localPosition=Vector3.zero;
            var skeleton=SkeletonAnimation.NewSkeletonAnimationGameObject(data);skeleton.name="liubei_atk_beaten_1";skeleton.transform.SetParent(root.transform,false);
            // Ground origin is shared by gameplay and the preview.
            skeleton.transform.localPosition=Vector3.zero;
            skeleton.transform.localScale=new Vector3(-1,1,1);
            skeleton.AnimationName="texiao";skeleton.loop=false;skeleton.Initialize(false);
            var renderer=skeleton.GetComponent<MeshRenderer>();renderer.sortingLayerName="Character";renderer.sortingOrder=70;
            var effect=root.AddComponent<CommonSpineBattleEffect>();effect.targetEnemy=true;effect.centerOnBody=true;effect.skeleton=skeleton;effect.duration=sd.FindAnimation("texiao").Duration;
            bool any=false;Bounds bounds=new Bounds();
            for(int i=0;i<=60;i++){effect.Sample(effect.duration*i/60f);foreach(var v in skeleton.GetComponent<MeshFilter>().sharedMesh.vertices){var p=skeleton.transform.TransformPoint(v);if(!any){bounds=new Bounds(p,Vector3.zero);any=true;}else bounds.Encapsulate(p);}}
            effect.motionBounds=bounds;skeleton.timeScale=1;effect.Sample(0);skeleton.timeScale=1;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        var rigPath=SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab";var rig=PrefabUtility.LoadPrefabContents(rigPath);
        try{var t=rig.GetComponent<SpineEffectsTest>();t.battleStyle=AssetDatabase.LoadAssetAtPath<DarkFairytaleBattleStyle>("Assets/DarkFairytale/Battle/Settings/BattleStyle.asset");t.battleWorldHeight=BattleHeight();var fx=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<CommonSpineBattleEffect>();t.particleDurations[0]=fx.duration;t.particleBounds[0]=fx.motionBounds;PrefabUtility.SaveAsPrefabAsset(rig,rigPath);}finally{PrefabUtility.UnloadPrefabContents(rig);}
        CommonBattleEffectsAuthoring.RefreshHitPreview();
        AssetDatabase.SaveAssets();
    }
}
