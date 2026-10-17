using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class EquipmentColumnsPatch {
 static EquipmentColumnsPatch(){EditorApplication.update+=()=>{
 const string request="output/equipment-columns/patch.request";
 if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
 File.Delete(request);
 const string path="Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab";
 var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponent<StorybookEquipmentUI>();
 for(int n=0;n<3;n++){
 var button=(RectTransform)ui.columnButtons[n].transform;button.sizeDelta=new Vector2(42,42);button.anchoredPosition=new Vector2(43+n*50,-91);
 int columns=n+3;float width=(26-(columns-1)*2)/(float)columns;
 foreach(RectTransform child in button){
 if(child.name.StartsWith("GridGlyph")){var indices=child.name.Substring(9).Split('_');int row=int.Parse(indices[0]),col=int.Parse(indices[1]);child.anchoredPosition=new Vector2(8+col*(width+2),-10-row*12);child.sizeDelta=new Vector2(width,10);}
 else if(child.anchorMin==child.anchorMax)child.sizeDelta=new Vector2(42,42);
 }
 }
 PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);File.WriteAllText("output/equipment-columns/patch.txt","PASS");
 };}
}
