using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class EquipmentCardPolish {
 static EquipmentCardPolish(){EditorApplication.update+=()=>{
 const string request="output/equipment-card-polish/patch.request";
 if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
 File.Delete(request);const string path="Assets/DarkFairytale/UI/Equipment/Prefabs/EquipmentCell.prefab";
 var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponent<StorybookEquipmentCell>();
 ui.quality.rectTransform.sizeDelta=new Vector2(19,23);ui.quality.rectTransform.anchoredPosition=new Vector2(10,-9);
 var ribbon=(RectTransform)ui.stateRibbon.transform;ribbon.anchoredPosition=new Vector2(82,-9);ribbon.sizeDelta=new Vector2(65,22);
 var image=ui.stateRibbon.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/ToolbarButtonFrame.asset");image.type=Image.Type.Sliced;image.fillCenter=true;
 ui.state.rectTransform.anchorMin=Vector2.zero;ui.state.rectTransform.anchorMax=Vector2.one;ui.state.rectTransform.offsetMin=Vector2.zero;ui.state.rectTransform.offsetMax=Vector2.zero;ui.state.fontSize=13;ui.state.alignment=TextAnchor.MiddleCenter;
 ui.detailedIconPose.anchoredPosition=new Vector2(29,-37);ui.detailedIconPose.sizeDelta=new Vector2(100,66);
 var body=(RectTransform)ui.detailedBody.transform;body.offsetMin=new Vector2(8,8);body.offsetMax=new Vector2(-8,-112);body.GetComponent<Image>().color=new Color(.22f,.15f,.085f,.12f);
 foreach(var text in new[]{ui.detailLabels,ui.detailValues}){text.fontSize=16;text.lineSpacing=1.25f;text.color=new Color(.27f,.21f,.15f);text.rectTransform.offsetMin=new Vector2(text==ui.detailLabels?9:62,6);text.rectTransform.offsetMax=new Vector2(text==ui.detailLabels?-64:-9,-9);}
 if(!body.Find("GoldRule")){
 var rule=new GameObject("GoldRule",typeof(RectTransform),typeof(Image));rule.transform.SetParent(body,false);var r=(RectTransform)rule.transform;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(-12,1);r.anchoredPosition=Vector2.zero;var im=rule.GetComponent<Image>();im.color=new Color(.68f,.51f,.29f,.8f);im.raycastTarget=false;
 }
 PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);File.WriteAllText("output/equipment-card-polish/patch.txt","PASS");
 };}
}
