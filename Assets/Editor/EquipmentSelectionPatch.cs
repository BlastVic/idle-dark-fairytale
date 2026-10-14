using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class EquipmentSelectionPatch
{
    const string Out="output/equipment-selection/";
    static EquipmentSelectionPatch(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"patch.request"))return;
        File.Delete(Out+"patch.request");const string path="Assets/DarkFairytale/UI/Equipment/EquipmentCell.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var cell=root.GetComponent<StorybookEquipmentCell>();cell.cardBackground=root.GetComponent<Image>();cell.normalBackground=Color.white;cell.selectedBackground=new Color(1f,.76f,.66f,1f);
            var badge=cell.multiMark;var old=badge.GetComponent<Text>();if(old)Object.DestroyImmediate(old);
            var rect=(RectTransform)badge.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-7,-7);rect.sizeDelta=new Vector2(33,33);rect.SetAsLastSibling();
            var plate=badge.GetComponent<Image>();if(!plate)plate=badge.AddComponent<Image>();plate.sprite=cell.stateRibbon.GetComponent<Image>().sprite;plate.type=Image.Type.Sliced;plate.color=new Color(.5f,.055f,.04f);plate.raycastTarget=false;
            if(!rect.Find("Check")){
                var go=new GameObject("Check",typeof(RectTransform),typeof(CanvasRenderer),typeof(EquipmentSelectionCheck));var r=(RectTransform)go.transform;r.SetParent(rect,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(6,6);r.offsetMax=new Vector2(-6,-6);var check=go.GetComponent<EquipmentSelectionCheck>();check.color=new Color(1,.94f,.78f);check.raycastTarget=false;
            }
            badge.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
