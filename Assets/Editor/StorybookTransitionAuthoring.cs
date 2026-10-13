using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookTransitionAuthoring{
 public const string Path="Assets/DarkFairytale/UI/Transition/StorybookTransition.prefab",Out="output/transition-ui/";
 static StorybookTransitionAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(System.Exception e){File.WriteAllText(Out+"build.txt",e.ToString());}};}
 [MenuItem("Tools/Dark Fairytale/UI/Create Transition (Explicit)")]
 public static void Build(){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));if(!File.Exists(Path)){var go=new GameObject("StorybookTransition",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup),typeof(TransitionCanvas));try{go.layer=5;go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=30000;var ui=go.GetComponent<TransitionCanvas>();ui.group=go.GetComponent<CanvasGroup>();ui.group.alpha=0;ui.group.blocksRaycasts=ui.group.interactable=false;var veil=new GameObject("InkVeil",typeof(RectTransform),typeof(Image));veil.layer=5;veil.transform.SetParent(go.transform,false);var rt=(RectTransform)veil.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;veil.GetComponent<Image>().color=new Color(.018f,.014f,.018f,1);PrefabUtility.SaveAsPrefabAsset(go,Path);}finally{Object.DestroyImmediate(go);}}
 EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");var old=Object.FindFirstObjectByType<TransitionCanvas>(FindObjectsInactive.Include);if(old&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old)==Path)return;var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));if(old)Object.DestroyImmediate(old.gameObject);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();}
}
