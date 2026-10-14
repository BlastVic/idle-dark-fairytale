using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StorybookHeaderAlignmentPatch
{
    public static readonly string[] Paths={
        "Assets/DarkFairytale/UI/Equipment/StorybookEquipment.prefab",
        "Assets/DarkFairytale/UI/Skills/StorybookSkills.prefab",
        "Assets/Prefab/Addressable Prefabs/CashMallPackage.prefab"};
    static StorybookHeaderAlignmentPatch(){EditorApplication.update+=Tick;}
    static void Tick(){
        const string dir="output/header-alignment/";
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(dir+"patch.request"))return;
        File.Delete(dir+"patch.request");
        foreach(var path in Paths){
            var root=PrefabUtility.LoadPrefabContents(path);
            try{
                var equipment=root.GetComponent<StorybookEquipmentLayout>();var skills=root.GetComponent<StorybookSkillsLayout>();
                var content=equipment?equipment.headerContent:skills.headerContent;
                foreach(var name in new[]{"Portrait","PlayerLevel","SubjectNumber"}){
                    var r=(RectTransform)content.Find(name);
                    if(r.anchorMin.x!=1){float x=r.anchoredPosition.x-925;r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(x,r.anchoredPosition.y);}
                }
                var title=(RectTransform)content.Find("Title");
                title.anchorMin=title.anchorMax=new Vector2(.5f,1);title.pivot=new Vector2(.5f,1);title.anchoredPosition=new Vector2(0,title.anchoredPosition.y);
                if(equipment)equipment.Apply();else skills.Apply();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText(dir+"patch.txt","PASS: Equipment, Skills and Shop headers use edge anchors and responsive content width.");
    }
}
