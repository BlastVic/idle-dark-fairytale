using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit menu/request only. Never rewrites prefabs on import or entering Play mode.
[InitializeOnLoad]
public static class EquipmentLootAuthoring
{
    public const string Output="output/equipment-loot/";
    public const string ScenePath="Assets/Scenes/Gameplay.unity";
    static EquipmentLootAuthoring() { EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!File.Exists(Output+"command.request"))return;
        string command=File.ReadAllText(Output+"command.request").Trim();File.Delete(Output+"command.request");
        try
        {
            if(command=="apply")Apply();else if(command=="validate")EquipmentLootValidation.Run();
            else if(command=="play")EquipmentLootPlayValidation.Start();else throw new Exception("Unknown command");
            File.WriteAllText(Output+"command-result.txt","PASS "+command);
        }
        catch(Exception e){File.WriteAllText(Output+"command-result.txt","FAIL "+e);Debug.LogException(e);}
    }
    public static void WithScene(Action<WaveManager,InventoryManager> action, bool save=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode before authoring loot");
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty)throw new Exception("Gameplay has unsaved edits; loot authoring stopped");
            var roots=scene.GetRootGameObjects();
            action(roots.SelectMany(r=>r.GetComponentsInChildren<WaveManager>(true)).Single(),roots.SelectMany(r=>r.GetComponentsInChildren<InventoryManager>(true)).Single());
            if(save){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [MenuItem("Tools/Dark Fairytale/Balance/Apply Equipment Loot")]
    public static void Apply()
    {
        Directory.CreateDirectory(Output);EquipmentLoot.ResetCache();var data=EquipmentLoot.Data;
        WithScene((wm,im)=>{
            foreach(var e in data.entries)if(!im.GetSpriteIcon(e.key))throw new Exception("Missing icon "+e.key);
            foreach(var s in data.stages)if(wm.waveDb.Count(w=>w.levelKey==s.key)!=1)throw new Exception("Missing stage "+s.key);
        });
        foreach(var rule in data.monsters)
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(rule.key))?.GetComponent<Enemy>())throw new Exception("Missing monster "+rule.key);
        foreach(var rule in data.monsters)
        {
            string path=PathFor(rule.key);var root=PrefabUtility.LoadPrefabContents(path);
            try{var enemy=root.GetComponent<Enemy>();enemy.equipmentLootKey=rule.key;enemy.itemsGiven.Clear();PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        WithScene((wm,im)=>{
            foreach(var s in data.stages){var w=wm.waveDb.Single(x=>x.levelKey==s.key);w.equipmentLootTier=s.tier;w.chestItemsGiven.Clear();w.chestGoldItemsGiven.Clear();w.chestDiamondItemsGiven.Clear();}
        },true);
        EquipmentLootValidation.Run();
    }
    public static string PathFor(string key)=>"Assets/DarkFairytale/Monsters/"+key+"/"+key+".prefab";
}
