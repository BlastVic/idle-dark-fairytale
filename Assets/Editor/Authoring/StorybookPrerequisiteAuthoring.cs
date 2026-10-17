using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit one-time authoring of editable prerequisite image rows.</summary>
[InitializeOnLoad]
public static class StorybookPrerequisiteAuthoring
{
    const string Request="output/skills-ui/prerequisite-images.request";
    public const string Met="Assets/DarkFairytale/UI/Skills/Art/PrerequisiteMet.png", Unmet="Assets/DarkFairytale/UI/Skills/Art/PrerequisiteUnmet.png";
    static StorybookPrerequisiteAuthoring(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        File.Delete(Request);GameObject root=null;
        try{
            Import(Met);Import(Unmet);root=PrefabUtility.LoadPrefabContents(StorybookSkillsBuilder.Path);
            var ui=root.GetComponent<StorybookSkillsUI>();
            if(ui.prerequisiteRows==null||ui.prerequisiteRows.Length==0)Build(ui,ui.drawerBody,ui.prerequisites.font);
            PrefabUtility.SaveAsPrefabAsset(root,StorybookSkillsBuilder.Path);
            File.WriteAllText("output/skills-ui/prerequisite-images-result.txt","PASS");
        }catch(Exception e){File.WriteAllText("output/skills-ui/prerequisite-images-result.txt",e.ToString());Debug.LogException(e);}
        finally{if(root)PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Import(string path){var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
    static RectTransform Node(Transform parent,string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);return r;}
    public static void Build(StorybookSkillsUI ui,RectTransform body,Font font){
        int order=ui.prerequisites.transform.GetSiblingIndex();
        var section=Node(body,"PrerequisiteSection");section.SetSiblingIndex(order);
        var layout=section.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=7;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        ui.prerequisites.transform.SetParent(section,false);
        ui.prerequisiteMet=AssetDatabase.LoadAssetAtPath<Sprite>(Met);ui.prerequisiteUnmet=AssetDatabase.LoadAssetAtPath<Sprite>(Unmet);
        ui.prerequisiteRows=new GameObject[2];ui.prerequisiteIcons=new Image[2];ui.prerequisiteLabels=new Text[2];
        for(int i=0;i<2;i++){
            var row=Node(section,"Condition"+(i+1));row.gameObject.AddComponent<LayoutElement>().preferredHeight=30;ui.prerequisiteRows[i]=row.gameObject;
            var icon=Node(row,"StateImage");StorybookEquipmentLayout.Box(icon,0,3,24,24);var image=icon.gameObject.AddComponent<Image>();image.sprite=ui.prerequisiteUnmet;image.preserveAspect=true;image.raycastTarget=false;ui.prerequisiteIcons[i]=image;
            var label=Node(row,"ConditionText");StorybookEquipmentLayout.Box(label,34,0,250,30);label.anchorMax=new Vector2(1,1);label.sizeDelta=new Vector2(-34,30);
            var text=label.gameObject.AddComponent<Text>();text.font=font;text.fontSize=20;text.text="前置技能 ≥1级";text.color=new Color(.40f,.373f,.333f);text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;ui.prerequisiteLabels[i]=text;
        }
    }
}
