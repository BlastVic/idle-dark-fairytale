using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit incremental authoring; never replaces the tree or rebuilds on import/play.</summary>
[InitializeOnLoad]
public static class StorybookSkillDetailsAuthoring
{
    const string Request="output/skills-ui/details-layout.request";
    static readonly Color Ink=new Color(.075f,.055f,.035f);
    public static readonly string[] Descriptions={"", "每2秒恢复生命。", "有概率发动快速三连击。", "有概率触发范围攻击。\n敌人多于1名时生效。", "提升生命与防御。", "提升攻击与攻速，\n增加暴击与暴伤。", "阵亡时有概率复活。\n复活后恢复40%生命。", "击败敌人时，有概率恢复20%生命。", "每第三次攻击造成额外伤害。", "幼龙伙伴间歇造成攻击伤害。", "击败敌人时，有概率获得双倍经验。", "生命低于50%的敌人受到雷击，造成攻击伤害。", "对首领有概率发动六连击，造成150%伤害。", "受到暴击时，有概率获得10%生命吸收。", "面对多个敌人时，增加伤害。", "幼龙有概率发动二连或三连攻击。"};
    static StorybookSkillDetailsAuthoring(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        File.Delete(Request);GameObject root=null;
        try{
            root=PrefabUtility.LoadPrefabContents(StorybookSkillsBuilder.Path);
            Configure(root.GetComponent<StorybookSkillsUI>());
            PrefabUtility.SaveAsPrefabAsset(root,StorybookSkillsBuilder.Path);
            File.WriteAllText("output/skills-ui/details-layout-result.txt","PASS");
        }catch(Exception e){File.WriteAllText("output/skills-ui/details-layout-result.txt",e.ToString());Debug.LogException(e);}
        finally{if(root)PrefabUtility.UnloadPrefabContents(root);}
    }
    static RectTransform Section(Transform parent,string name){
        var existing=parent.Find(name);if(existing)return (RectTransform)existing;
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);
        var layout=r.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;return r;
    }
    static Text Heading(Transform parent,string name,string caption,Font font){
        var found=parent.Find(name);if(found)return found.GetComponent<Text>();
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);r.gameObject.AddComponent<LayoutElement>().minHeight=30;
        var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=21;t.color=Ink;t.text=caption;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=true;return t;
    }
    static void Divider(Transform parent){
        if(parent.Find("SectionDivider"))return;
        var r=new GameObject("SectionDivider",typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);
        var e=r.gameObject.AddComponent<LayoutElement>();e.minHeight=1;e.preferredHeight=1;
        var image=r.gameObject.AddComponent<Image>();image.color=new Color(.48f,.35f,.22f,.45f);image.raycastTarget=false;
    }
    public static void Configure(StorybookSkillsUI ui){
        var bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Bold.otf");
        foreach(var n in ui.nodes)n.descriptionTemplate=Descriptions[(int)n.key];
        ui.title.font=bold;ui.title.fontSize=28;ui.title.alignment=TextAnchor.MiddleLeft;
        var title=ui.title.rectTransform;StorybookEquipmentLayout.Box(title,88,10,244,54);title.anchorMax=new Vector2(1,1);title.sizeDelta=new Vector2(-148,54);
        ui.detailIcon.transform.SetParent(ui.drawer,false);StorybookEquipmentLayout.Box(ui.detailIcon.rectTransform,24,11,54,54);
        ui.level.transform.parent.gameObject.SetActive(false);
        var body=ui.drawerBody;body.GetComponent<VerticalLayoutGroup>().spacing=18;
        var description=Section(body,"DescriptionSection");description.SetSiblingIndex(0);
        Heading(description,"DescriptionHeading","技能描述",bold).transform.SetSiblingIndex(0);
        ui.description.transform.SetParent(description,false);ui.description.color=Ink;ui.description.transform.SetSiblingIndex(1);
        ui.prerequisites.transform.parent.SetParent(description,false);ui.prerequisites.color=Ink;
        foreach(var label in ui.prerequisiteLabels)label.color=Ink;
        Divider(description);
        ui.comparison.transform.SetSiblingIndex(1);
        ui.comparison.GetComponent<VerticalLayoutGroup>().spacing=18;
        var current=Section(ui.comparison.transform,"CurrentSection");
        ui.currentHeading=Heading(current,"CurrentHeading","当前技能等级   Lv. 2",bold);
        ui.currentValues.transform.SetParent(current,false);ui.currentValues.transform.SetSiblingIndex(1);ui.currentValues.color=Ink;ui.currentValues.lineSpacing=1.2f;Divider(current);
        var next=Section(ui.comparison.transform,"TargetSection");
        ui.nextHeading=Heading(next,"TargetHeading","目标技能等级   <color=#8F2018>Lv. 3</color>",bold);
        ui.nextValues.transform.SetParent(next,false);ui.nextValues.transform.SetSiblingIndex(1);ui.nextValues.color=Ink;ui.nextValues.lineSpacing=1.2f;Divider(next);
        foreach(var text in new[]{ui.description,ui.currentValues,ui.nextValues,ui.currentOnlyText}){text.font=bold;text.color=Ink;text.supportRichText=true;}
        ui.currentValues.text="触发概率  <color=#8F2018>7.5%</color>";ui.nextValues.text="触发概率  <color=#8F2018>9%</color>";
        ui.currentOnlyText.color=Ink;ui.currentOnly.transform.SetSiblingIndex(2);
        ui.cost.color=Ink;ui.cost.alignment=TextAnchor.MiddleLeft;ui.cost.text="技能点消耗   <color=#861A12>1 点</color>";
    }
}
