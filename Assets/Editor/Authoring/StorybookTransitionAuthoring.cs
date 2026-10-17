using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookTransitionAuthoring{
 public const string Path="Assets/DarkFairytale/UI/Transition/Prefabs/StorybookTransition.prefab",Out="output/transition-ui/";
 static StorybookTransitionAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(System.Exception e){File.WriteAllText(Out+"build.txt",e.ToString());}};}
 [MenuItem("Tools/Dark Fairytale/UI/Create Transition (Explicit)")]
 public static void Build(){
 if(!File.Exists(Path))throw new System.InvalidOperationException("Restore the authored TurningStoryLeaf prefab; do not regenerate a legacy black veil.");
 Debug.Log("Transition prefab already authored. Existing Inspector adjustments preserved.");
 }
}
