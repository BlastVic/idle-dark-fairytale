using UnityEditor;
using UnityEngine;
using Spine.Unity;

public static class SpineEffectsStageAuthoring
{
    public static void Install()
    {
        var path=SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var test=root.GetComponent<SpineEffectsTest>();
            if(!test.hero) test.hero=Actor(root.transform,"Red Hood",AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/DarkFairytale/Characters/RedHood/Spine/RedHoodBattle_SkeletonData.asset"),new Vector3(-2.3f,-1.6f,0),2.6f,false);
            if(!test.enemy){var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/Monsters/BlackForestMushroom/MobTallowkin/MobTallowkin.prefab").GetComponentInChildren<SkeletonAnimation>(true);test.enemy=Actor(root.transform,"Enemy - MobTallowkin",source.skeletonDataAsset,new Vector3(2.1f,-1.6f,0),2.3f,true);}
            test.widthSlider.minValue=25;test.widthSlider.maxValue=200;test.widthSlider.value=100;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    static SkeletonAnimation Actor(Transform parent,string name,SkeletonDataAsset data,Vector3 feet,float height,bool flip)
    {
        var a=SkeletonAnimation.NewSkeletonAnimationGameObject(data);a.name=name;a.transform.SetParent(parent,false);
        a.AnimationName="Idle1";a.loop=true;a.AnimationState.SetAnimation(0,"Idle1",true);a.Update(0);a.LateUpdate();
        var r=a.GetComponent<MeshRenderer>();var b=r.bounds;float scale=height/b.size.y;
        a.transform.localScale=new Vector3(flip?-scale:scale,scale,scale);
        a.transform.localPosition=feet-new Vector3(b.center.x*(flip?-scale:scale),b.min.y*scale,0);
        r.sortingLayerName="Character";r.sortingOrder=10;
        return a;
    }
}
