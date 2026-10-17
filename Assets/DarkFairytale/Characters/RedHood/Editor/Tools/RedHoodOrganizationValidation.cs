using System;
using System.IO;
using System.Linq;
using UnityEditor;
using Spine.Unity;

// Read-only asset checks and preview render, only after an explicit local request.
[InitializeOnLoad]
public static class RedHoodOrganizationValidation
{
    const string Root="Assets/DarkFairytale/Characters/RedHood/";
    const string Out="output/redhood-organization/";
    static RedHoodOrganizationValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"validate.request"))return;
        File.Delete(Out+"validate.request");
        try {
            var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"Spine/RedHoodBattle_SkeletonData.asset");
            if(!data||data.GetSkeletonData(false).Animations.Count!=15)throw new Exception("Production must retain all 15 animations");
            var guid=AssetDatabase.AssetPathToGUID(Root+"Spine/RedHoodBattle_SkeletonData.asset");
            if(!File.ReadAllText(Root+"Tests/RedHoodAnimationTest.unity").Contains(guid))throw new Exception("Preview does not use production data");
            foreach(var path in new[]{Root+"Prefabs/Player.prefab",Root+"Tests/RedHoodAnimationTest.unity"})
                if(AssetDatabase.GetDependencies(path,true).Any(p=>p.StartsWith("Assets/RedHoodPrototype/")||p.StartsWith("Assets/ArtSources/")))throw new Exception("Invalid runtime dependency: "+path);
            DarkFairytaleValidation.ValidatePlayer();
            File.WriteAllText(Out+"validation.txt","PASS: single production skeleton / 15 animations / preview binding / runtime dependencies / Player prefab / combat and resurrection events / flash materials.\n");
        } catch(Exception e){File.WriteAllText(Out+"validation.txt","FAIL: "+e);UnityEngine.Debug.LogException(e);}
    }
}
