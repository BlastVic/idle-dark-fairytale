using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class StorybookRebirthEntryFix
{
 public const string Out="output/rebirth-entry/";
 static StorybookRebirthEntryFix(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");var root=PrefabUtility.LoadPrefabContents(StorybookRebirthBuilder.Path);try{if(!root.transform.Find("PresentationCamera")){var g=new GameObject("PresentationCamera",typeof(Camera));g.transform.SetParent(root.transform,false);var c=g.GetComponent<Camera>();c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;c.cullingMask=0;c.depth=-100;c.orthographic=true;c.nearClipPlane=.1f;c.farClipPlane=10;c.allowHDR=false;c.allowMSAA=false;}PrefabUtility.SaveAsPrefabAsset(root,StorybookRebirthBuilder.Path);AssetDatabase.SaveAssets();File.WriteAllText(Out+"build.txt","PASS");}finally{PrefabUtility.UnloadPrefabContents(root);}};}
}
