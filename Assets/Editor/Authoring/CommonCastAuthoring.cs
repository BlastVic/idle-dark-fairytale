using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

public static class CommonCastAuthoring
{
    const string Folder="Assets/DarkFairytale/Effects/Common/DongzhuoCast";
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
        string texture=Folder+"/dongzhuo_atk_tx_1.png";
        var ti=(TextureImporter)AssetImporter.GetAtPath(texture);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Cast.mat");
        if(!material){material=new Material(Shader.Find("DarkFairytale/Golden Spine"));AssetDatabase.CreateAsset(material,Folder+"/Cast.mat");}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);material.SetFloat("_StraightAlphaInput",1);material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(material);
        var atlas=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Folder+"/Cast_Atlas.asset");if(!atlas){atlas=ScriptableObject.CreateInstance<SpineAtlasAsset>();AssetDatabase.CreateAsset(atlas,Folder+"/Cast_Atlas.asset");}
        atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Cast.atlas.txt");atlas.materials=new[]{material};atlas.Clear();EditorUtility.SetDirty(atlas);
        var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Folder+"/Cast_SkeletonData.asset");if(!data){data=ScriptableObject.CreateInstance<SkeletonDataAsset>();AssetDatabase.CreateAsset(data,Folder+"/Cast_SkeletonData.asset");}
        data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Cast.json");data.atlasAssets=new AtlasAssetBase[]{atlas};data.scale=.01f;data.Clear();EditorUtility.SetDirty(data);
        var sd=data.GetSkeletonData(false);if(sd==null)throw new Exception("Cast skeleton failed to load");
        var path="Assets/Resources/Effects/DongzhuoCast.prefab";System.IO.Directory.CreateDirectory("Assets/Resources/Effects");var root=new GameObject("DongzhuoCast");
        try {
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            for(int i=root.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach(var c in root.GetComponents<Component>())if(c&&!(c is Transform))UnityEngine.Object.DestroyImmediate(c);
            root.transform.localScale=Vector3.one*2.5f;root.transform.localPosition=Vector3.zero;
            var skeleton=SkeletonAnimation.NewSkeletonAnimationGameObject(data);skeleton.name="dongzhuo_atk_tx_1";skeleton.transform.SetParent(root.transform,false);
            // Ground origin is shared by gameplay and the preview.
            skeleton.transform.localPosition=Vector3.zero;
            skeleton.AnimationName="texiao";skeleton.loop=false;skeleton.Initialize(false);
            var renderer=skeleton.GetComponent<MeshRenderer>();renderer.sortingLayerName="Character";renderer.sortingOrder=70;
            var effect=root.AddComponent<CommonSpineBattleEffect>();effect.startTime=.3f;effect.keepSpawnPosition=true;effect.skeleton=skeleton;effect.duration=sd.FindAnimation("texiao").Duration-effect.startTime;
            bool any=false;Bounds bounds=new Bounds();
            for(int i=0;i<=60;i++){effect.Sample(effect.duration*i/60f);foreach(var v in skeleton.GetComponent<MeshFilter>().sharedMesh.vertices){var p=skeleton.transform.TransformPoint(v);if(!any){bounds=new Bounds(p,Vector3.zero);any=true;}else bounds.Encapsulate(p);}}
            effect.motionBounds=bounds;skeleton.timeScale=1;effect.Sample(0);skeleton.timeScale=1;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();
    }
}
