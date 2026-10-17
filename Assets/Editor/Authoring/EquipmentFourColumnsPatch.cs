using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class EquipmentFourColumnsPatch {
 static void Box(Transform t,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box((RectTransform)t,x,y,w,h);
 static EquipmentFourColumnsPatch(){EditorApplication.update+=()=>{
 const string request="output/equipment-four-columns/patch.request";
 if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
 File.Delete(request);const string path="Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab";
 var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponent<StorybookEquipmentUI>();var l=ui.layout;var p=l.detailContent;ui.sideActions=true;l.detailHeight=377;
 Box(p.Find("CurrentHeading"),23,14,211,36);Box(p.Find("TargetHeading"),253,14,211,36);Box(p.Find("DeltaHeading"),483,14,211,36);
 foreach(var name in new[]{"CurrentHeading","TargetHeading","DeltaHeading"})p.Find(name).GetComponent<Text>().fontSize=27;
 Box(ui.equippedIcon.transform,88,52,82,64);Box(ui.selectedIcon.transform,318,52,82,64);
 Box(ui.equippedName.transform,24,118,210,30);Box(ui.selectedName.transform,254,118,210,30);
 foreach(var t in new[]{ui.equippedName,ui.selectedName}){t.resizeTextMaxSize=20;t.resizeTextMinSize=13;}
 Box(ui.equippedGrade.transform,24,148,210,24);Box(ui.grade.transform,254,148,210,24);
 p.Find("ChangeSymbol").gameObject.SetActive(false);var caption=p.Find("ChangeCaption").GetComponent<Text>();caption.text="红色 · 提升\n绿色 · 降低";caption.fontSize=19;caption.color=new Color(.38f,.30f,.22f);Box(caption.transform,483,73,211,64);
 var dividers=p.Cast<Transform>().Where(t=>t.name=="InkRule").ToArray();for(int i=0;i<dividers.Length;i++)Box(dividers[i],243+i*230,18,1,343);
 if(dividers.Length<3){var g=Object.Instantiate(dividers[0].gameObject,p);g.name="InkRule";Box(g.transform,713,18,1,343);}
 Box(ui.comparisonScroll.viewport,23,178,681,185);
 for(int i=0;i<ui.comparisonRows.Length;i++){
 var r=ui.comparisonRows[i];Box(r.root,0,i*37,681,37);
 for(int col=0;col<3;col++){Box(r.root.Find("StatIcon"+col),col*230+2,9,19,19);var label=r.root.Find("Label"+col).GetComponent<Text>();Box(label.transform,col*230+27,0,74,37);label.fontSize=19;}
 var rules=r.root.Cast<Transform>().Where(t=>t.name=="InkRule").ToArray();for(int j=0;j<rules.Length;j++)Box(rules[j],j*230+2,36,209,1);
 Box(r.current.transform,99,0,109,37);Box(r.selected.transform,329,0,109,37);Box(r.delta.transform,553,0,115,37);
 foreach(var t in new[]{r.current,r.selected,r.delta}){t.fontSize=21;t.resizeTextForBestFit=true;t.resizeTextMinSize=14;t.resizeTextMaxSize=21;}
 }
 Box(ui.equip.transform,738,83,162,58);Box(ui.decompose.transform,738,153,162,58);
 foreach(var b in new[]{ui.equip,ui.decompose}){
 foreach(RectTransform c in b.transform){if(c.anchorMin==c.anchorMax){c.anchorMin=Vector2.zero;c.anchorMax=Vector2.one;c.offsetMin=c.offsetMax=Vector2.zero;}}
 b.GetComponentInChildren<Text>().fontSize=26;
 }
 Box(ui.lockButton.transform,770,222,98,27);Box(ui.lockCaption.transform,0,0,98,27);
 if(!p.Find("ActionHeading")){var label=Object.Instantiate(p.Find("CurrentHeading").gameObject,p);label.name="ActionHeading";label.GetComponent<Text>().text="操作";Box(label.transform,738,14,162,36);}
 Box(p.Find("DetailsSwipeSurface"),17,17,688,346);
 PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);File.WriteAllText("output/equipment-four-columns/patch.txt","PASS");
 };}
}
