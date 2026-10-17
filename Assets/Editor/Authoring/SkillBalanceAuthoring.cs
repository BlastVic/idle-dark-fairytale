using System;
using System.IO;
using System.Linq;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SkillBalanceAuthoring
{
    static SkillBalanceAuthoring(){EditorApplication.update+=Tick;}
    static void Tick(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("output/skill-balance/ui.request")){File.Delete("output/skill-balance/ui.request");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");File.WriteAllText("output/skills-ui/validate.request","");return;}const string file="output/skill-balance/apply.request";if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(file))return;File.Delete(file);try{Apply();File.WriteAllText("output/skill-balance/apply.txt","PASS");}catch(Exception e){File.WriteAllText("output/skill-balance/apply.txt",e.ToString());}}
    [MenuItem("Tools/Dark Fairytale/Skills/Apply Balanced Skill Text")]
    public static void Apply(){
        var root=PrefabUtility.LoadPrefabContents(StorybookSkillsBuilder.Path);
        try{foreach(var node in root.GetComponentsInChildren<StorybookSkillNode>(true)){
            int key=(int)node.key;node.displayName=SkillDesign.Names[key];node.descriptionTemplate=SkillDesign.Descriptions[key];node.effectLabels=SkillDesign.Labels[key];
            node.caption.text=key==8?"召唤：\n幽灵狼群":key==9?"召唤：\n书脊龙":node.displayName;
        }PrefabUtility.SaveAsPrefabAsset(root,StorybookSkillsBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        root=PrefabUtility.LoadPrefabContents("Assets/Tests/Skills/SkillTestHUD.prefab");
        try{var test=root.GetComponent<SkillTest>();foreach(var definition in test.definitions){int key=(int)definition.key;definition.displayName=SkillDesign.Names[key];definition.effectLabels=SkillDesign.Labels[key];}
            for(int i=0;i<test.skillButtons.Length;i++)test.skillButtons[i].GetComponentInChildren<UnityEngine.UI.Text>(true).text=SkillDesign.Names[i+1];
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Tests/Skills/SkillTestHUD.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=SkillsDataDescriptions.Instance;var so=new SerializedObject(data);var entries=so.FindProperty("_skillDataArr");
        for(int i=0;i<entries.arraySize;i++){var entry=entries.GetArrayElementAtIndex(i);int key=entry.FindPropertyRelative("skillsType").intValue;entry.FindPropertyRelative("desc").stringValue=SkillDesign.Descriptions[key]+"\n"+string.Join("\n",SkillDesign.Labels[key].Select((label,n)=>label+" [VAL"+(n+1)+"]%"));}
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
    }
}
