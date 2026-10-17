using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookDeathFadeAuthoring {
const string O="output/death-fade/";
static StorybookDeathFadeAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(O+"build.request"))return;File.Delete(O+"build.request");try{Build();File.WriteAllText(O+"build.txt","PASS");}catch(System.Exception e){File.WriteAllText(O+"build.txt",e.ToString());}};}
public static void Build(){var p=StorybookTransitionAuthoring.Path;var root=PrefabUtility.LoadPrefabContents(p);try{
if(root.GetComponent<StorybookDeathFade>())return;
var ui=root.AddComponent<StorybookDeathFade>();var g=new GameObject("FinalLifePage",typeof(RectTransform),typeof(CanvasGroup),typeof(RawImage));g.layer=5;g.transform.SetParent(root.transform,false);g.transform.SetAsFirstSibling();var r=g.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
ui.finalPage=g.GetComponent<RawImage>();ui.finalPage.raycastTarget=true;ui.pageGroup=g.GetComponent<CanvasGroup>();ui.pageGroup.alpha=0;ui.pageGroup.blocksRaycasts=false;
var mat=new Material(Shader.Find("UI/Storybook Death Page"));mat.SetTexture("_InkTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DarkFairytale/UI/Transition/Art/DeathInkStoryboard.png"));AssetDatabase.CreateAsset(mat,"Assets/DarkFairytale/UI/Transition/DeathPage.mat");ui.finalPage.material=mat;
// Separate from the normal route veil, whose parent group is ordinarily transparent.
var canvas=g.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=29999;g.AddComponent<GraphicRaycaster>();ui.pageGroup.ignoreParentGroups=true;
PrefabUtility.SaveAsPrefabAsset(root,p);
}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
