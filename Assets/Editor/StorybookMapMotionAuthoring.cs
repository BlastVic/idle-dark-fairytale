using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookMapMotionAuthoring
{
 public const string Out="output/map-motion/";
 static StorybookMapMotionAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(System.Exception e){File.WriteAllText(Out+"build.txt",e.ToString());}};}
 [MenuItem("Tools/Dark Fairytale/UI/Update Map Motion (Explicit)")]
 public static void Build(){var root=PrefabUtility.LoadPrefabContents(StorybookMapBuilder.Path);try{var ui=root.GetComponent<StorybookMapUI>();foreach(var n in ui.nodes){var f=n.button.GetComponent<ShelterButtonFeedback>();if(!f)f=n.button.gameObject.AddComponent<ShelterButtonFeedback>();f.animationTarget=n.seal.transform;f.pressedScale=.9f;var seal=n.seal.rectTransform;var delta=Vector2.Scale(new Vector2(.5f,.5f)-seal.pivot,seal.rect.size);seal.pivot=Vector2.one*.5f;seal.anchoredPosition+=delta;}
 var motion=ui.detailMotion;if(!motion.fullCardRise){var card=ui.layout.detail;var wrapper=new GameObject("DetailSlideViewport",typeof(RectTransform),typeof(RectMask2D));wrapper.layer=5;var rect=(RectTransform)wrapper.transform;rect.SetParent(card.parent,false);rect.SetSiblingIndex(card.GetSiblingIndex());rect.anchorMin=card.anchorMin;rect.anchorMax=card.anchorMax;rect.pivot=card.pivot;rect.anchoredPosition=card.anchoredPosition;rect.sizeDelta=card.sizeDelta;rect.localScale=card.localScale;
 if(motion.group){motion.group.alpha=1;motion.group.blocksRaycasts=motion.group.interactable=true;}if(motion.content)motion.content.anchoredPosition=Vector2.zero;
 card.SetParent(rect,false);card.anchorMin=Vector2.zero;card.anchorMax=Vector2.one;card.pivot=Vector2.one*.5f;card.offsetMin=card.offsetMax=Vector2.zero;card.localScale=Vector3.one;motion.content=card;motion.group=card.GetComponent<CanvasGroup>();if(!motion.group)motion.group=card.gameObject.AddComponent<CanvasGroup>();motion.fullCardRise=true;ui.layout.detail=rect;}
 motion.duration=.42f;motion.riseDistance=208;PrefabUtility.SaveAsPrefabAsset(root,StorybookMapBuilder.Path);AssetDatabase.SaveAssets();}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
