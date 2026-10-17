using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LegacySceneRemnantsCleanup
{
    const string Dir="output/legacy-scene-cleanup/";
    static LegacySceneRemnantsCleanup(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"cleanup.request"))return;
        File.Delete(Dir+"cleanup.request");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Gameplay.unity"){File.WriteAllText(Dir+"result.txt","FAIL: Gameplay scene required");return;}
        var gc=Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);
        if(!gc||!gc.homeUI||!gc.battleHud||!gc.inventoryPanel.GetComponent<StorybookEquipmentUI>()||!gc.skillsPanel.GetComponent<StorybookSkillsUI>()){File.WriteAllText(Dir+"result.txt","FAIL: replacement UI bindings required");return;}
        File.Copy(scene.path,Dir+"Gameplay-before.unity",true);
        string[] names={"HP and XP Bars","TopBar","Camp UI","Bottom Nav (KEEP ON)","Inventory Panel","TutorialBubblesParent","DailyCherriesPop","Wave Label","Wave Text","BackButton_Battle","XR CUTOUT"};
        var nodes=gc.transform.Cast<Transform>().Where(t=>names.Contains(t.name)).Select(t=>t.gameObject).ToArray();
        var candidates=new HashSet<string>();
        foreach(var dep in EditorUtility.CollectDependencies(nodes)){var p=AssetDatabase.GetAssetPath(dep);if(p.StartsWith("Assets/"))candidates.Add(p);}
        if(gc.chestPrefab){foreach(var p in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(gc.chestPrefab),true))candidates.Add(p);gc.chestPrefab=null;}
        File.WriteAllLines(Dir+"candidate-assets.txt",candidates.OrderBy(x=>x));
        var removed=nodes.SelectMany(n=>n.GetComponentsInChildren<Transform>(true)).Select(t=>t.name).ToArray();
        foreach(var node in nodes)Undo.DestroyObjectImmediate(node);
        gc.battleRelatedUI=new GameObject[0];gc.campRelatedUI=new GameObject[0];gc.notifiers=new GameObject[0];gc.bottomNavButtons=new GameObject[0];
        // Clear broken serialized links on surviving objects, including old optional compatibility fields.
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true)){
            if(!component)continue;var so=new SerializedObject(component);var p=so.GetIterator();bool changed=false;
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==null&&p.propertyPath!="m_Script"){p.objectReferenceValue=null;changed=true;}
            if(changed)so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(gc);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Dir+"removed-nodes.txt",removed);
        File.WriteAllText(Dir+"result.txt","PASS: removed "+nodes.Length+" obsolete roots / "+removed.Length+" nodes. New prefab bindings retained.");
    }
}
