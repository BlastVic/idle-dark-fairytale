using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class StorybookButtonStateRepair
{
    const string Dir="output/button-state/";
    static StorybookButtonStateRepair(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"repair.request"))return;
        File.Delete(Dir+"repair.request");Repair();}
    public static void Repair(){Directory.CreateDirectory(Dir);int count=0;
        foreach(var path in new[]{"Equipment/StorybookEquipment.prefab","Equipment/EquipmentCell.prefab","Map/Chapter01Map.prefab","Skills/StorybookSkills.prefab","Shelter/ShelterHome.prefab","CursedStorybook/CursedStorybookBattleHUD.prefab"}){
            var full="Assets/DarkFairytale/UI/"+path;var root=PrefabUtility.LoadPrefabContents(full);
            try{foreach(var b in root.GetComponentsInChildren<Button>(true)){
                var c=b.colors;c.disabledColor=c.normalColor;b.colors=c;
                var state=b.GetComponent<StorybookEquipmentButtonState>();if(!state)state=b.gameObject.AddComponent<StorybookEquipmentButtonState>();
                state.disabledAlpha=1;state.grayscaleShader=Shader.Find("UI/Storybook Disabled");count++;
            }PrefabUtility.SaveAsPrefabAsset(root,full);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Dir+"repair.txt","Patched buttons: "+count);
    }
}
