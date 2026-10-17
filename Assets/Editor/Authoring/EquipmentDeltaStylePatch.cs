using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EquipmentDeltaStylePatch
{
    static EquipmentDeltaStylePatch() { EditorApplication.update += Tick; }
    static void Tick()
    {
        const string dir="output/equipment-delta/";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(dir+"patch.request"))return;
        File.Delete(dir+"patch.request");
        const string path="Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var ui=root.GetComponent<StorybookEquipmentUI>();
            var caption=ui.layout.detailContent.Find("ChangeCaption").GetComponent<Text>();
            caption.supportRichText=true;
            caption.text="<color=#9E140E>红色 · 提升</color>\n<color=#1A5729>绿色 · 降低</color>";
            caption.fontStyle=FontStyle.Bold;
            foreach(var row in ui.comparisonRows){
                var arrow=row.root.Find("DeltaArrow");
                if(!arrow){var go=new GameObject("DeltaArrow",typeof(RectTransform),typeof(CanvasRenderer),typeof(EquipmentDeltaArrow));go.transform.SetParent(row.root,false);arrow=go.transform;}
                row.deltaArrow=arrow.GetComponent<EquipmentDeltaArrow>();
                row.deltaArrow.raycastTarget=false;
                StorybookEquipmentLayout.Box((RectTransform)arrow,561,10,15,18);
                var ar=(RectTransform)arrow;ar.pivot=new Vector2(.5f,.5f);ar.anchoredPosition+=new Vector2(7.5f,-9);
                StorybookEquipmentLayout.Box(row.delta.rectTransform,580,0,88,37);
                row.delta.fontStyle=FontStyle.Bold;
                row.delta.fontSize=23;row.delta.resizeTextMaxSize=23;row.delta.resizeTextMinSize=18;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
            File.WriteAllText(dir+"patch.txt","PASS: persistent arrow icons and bold red/green deltas");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
