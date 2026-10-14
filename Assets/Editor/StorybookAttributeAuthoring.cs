using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit extraction and installation. Does not run on import or in gameplay.</summary>
[InitializeOnLoad] public static class StorybookAttributeAuthoring
{
 public const string Path="Assets/DarkFairytale/UI/Shared/StorybookAttributePanel.prefab",Home="Assets/DarkFairytale/UI/Shelter/ShelterHome.prefab",Out="output/shared-attributes/";
 static StorybookAttributeAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}};}
 static T Map<T>(T source,Transform oldRoot,Transform newRoot) where T:Component=>newRoot.Find(AnimationUtility.CalculateTransformPath(source.transform,oldRoot)).GetComponent<T>();
 [MenuItem("Tools/Dark Fairytale/UI/Extract Shared Attributes (Explicit)")]
 public static void Build(){Directory.CreateDirectory(Out);var eq=PrefabUtility.LoadPrefabContents(StorybookEquipmentBuilder.Path);float expanded,collapsed;
 try{var ui=eq.GetComponent<StorybookEquipmentUI>();expanded=ui.layout.expandedHeight;collapsed=ui.layout.collapsedHeight;
 if(!AssetDatabase.LoadAssetAtPath<GameObject>(Path)){
 var old=ui.layout.attributes;var panel=UnityEngine.Object.Instantiate(old.gameObject);panel.name="StorybookAttributePanel";var rt=(RectTransform)panel.transform;StorybookEquipmentLayout.Box(rt,0,0,925,expanded);rt.localScale=Vector3.one;
 var v=panel.AddComponent<StorybookAttributePanel>();v.contents=Map(ui.layout.attributeContent,old,rt);v.body=Map(ui.attributeBody,old,rt);v.fold=Map(ui.foldButton,old,rt);v.arrow=Map(ui.foldArrow,old,rt);v.points=Map(ui.points,old,rt);v.rebirth=Map(ui.rebirthText,old,rt);v.add=new Button[6];v.values=new Text[6];for(int i=0;i<6;i++){v.add[i]=Map(ui.statButtons[i],old,rt);v.values[i]=Map(ui.statValues[i],old,rt);}
 v.contents.sizeDelta=new Vector2(925,expanded);v.contents.localScale=Vector3.one;v.SetFold(1);PrefabUtility.SaveAsPrefabAsset(panel,Path);UnityEngine.Object.DestroyImmediate(panel);
 }
 if(!ui.sharedAttributes){var old=ui.layout.attributes;var v=Instantiate(old);ui.sharedAttributes=v;ui.layout.attributes=(RectTransform)v.transform;ui.layout.attributeContent=v.contents;ui.attributeBody=v.body;ui.foldButton=v.fold;ui.foldArrow=v.arrow;ui.points=v.points;ui.rebirthText=v.rebirth;ui.statButtons=v.add;ui.statValues=v.values;UnityEngine.Object.DestroyImmediate(old.gameObject);ui.layout.Apply();PrefabUtility.SaveAsPrefabAsset(eq,StorybookEquipmentBuilder.Path);}
 }finally{PrefabUtility.UnloadPrefabContents(eq);}
 var home=PrefabUtility.LoadPrefabContents(Home);try{var ui=home.GetComponent<ShelterHomeUI>();if(!ui.sharedAttributes){var old=ui.attributePanel;var v=Instantiate(old);v.fitHomeContents=true;ui.sharedAttributes=v;ui.attributePanel=(RectTransform)v.transform;ui.attributeBody=v.body;ui.collapseButton=v.fold;ui.collapseArrow=v.arrow;ui.collapseText=null;ui.pointsText=v.points;ui.statButtons=v.add;ui.statValues=v.values;ui.expandedHeight=expanded*v.homeScale;ui.collapsedHeight=collapsed*v.homeScale;home.GetComponent<ShelterResponsiveLayout>().attributes=ui.attributePanel;UnityEngine.Object.DestroyImmediate(old.gameObject);ui.attributePanel.sizeDelta=new Vector2(1052,ui.attributesExpanded?ui.expandedHeight:ui.collapsedHeight);home.GetComponent<ShelterResponsiveLayout>().Apply();v.Fit();v.SetFold(ui.attributesExpanded?1:0);PrefabUtility.RecordPrefabInstancePropertyModifications(v);PrefabUtility.SaveAsPrefabAsset(home,Home);}}finally{PrefabUtility.UnloadPrefabContents(home);}AssetDatabase.SaveAssets();}
 static StorybookAttributePanel Instantiate(RectTransform old){var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),old.parent);obj.transform.SetSiblingIndex(old.GetSiblingIndex());var r=(RectTransform)obj.transform;r.anchorMin=old.anchorMin;r.anchorMax=old.anchorMax;r.pivot=old.pivot;r.sizeDelta=old.sizeDelta;r.anchoredPosition=old.anchoredPosition;r.localScale=old.localScale;return obj.GetComponent<StorybookAttributePanel>();}
}
