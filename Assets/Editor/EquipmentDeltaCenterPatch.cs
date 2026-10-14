using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class EquipmentDeltaCenterPatch
{
    const string Out="output/equipment-delta-center/";
    static EquipmentDeltaCenterPatch(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"patch.request"))return;
        File.Delete(Out+"patch.request");
        const string path="Assets/DarkFairytale/UI/Equipment/StorybookEquipment.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var ui=root.GetComponent<StorybookEquipmentUI>();
            var caption=ui.layout.detailContent.Find("ChangeCaption");if(caption)Object.DestroyImmediate(caption.gameObject);
            foreach(var row in ui.comparisonRows){
                if(row.root.Find("DeltaValueGroup"))continue;
                var group=new GameObject("DeltaValueGroup",typeof(RectTransform),typeof(HorizontalLayoutGroup));
                var rect=(RectTransform)group.transform;rect.SetParent(row.root,false);
                rect.anchorMin=new Vector2(2f/3f,0);rect.anchorMax=Vector2.one;
                rect.offsetMin=new Vector2(85,0);rect.offsetMax=new Vector2(-8,0);
                var layout=group.GetComponent<HorizontalLayoutGroup>();layout.childAlignment=TextAnchor.MiddleCenter;layout.spacing=7;
                layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
                row.deltaArrow.transform.SetParent(rect,false);var arrow=row.deltaArrow.gameObject.AddComponent<LayoutElement>();arrow.preferredWidth=15;arrow.preferredHeight=18;arrow.flexibleWidth=0;
                row.delta.transform.SetParent(rect,false);row.delta.alignment=TextAnchor.MiddleCenter;
                var value=row.delta.gameObject.AddComponent<LayoutElement>();value.preferredHeight=37;value.flexibleWidth=0;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
