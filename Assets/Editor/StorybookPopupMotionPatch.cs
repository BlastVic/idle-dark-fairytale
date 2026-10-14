using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookPopupMotionPatch
{
 const string Out="output/popup-motion/";
 static StorybookPopupMotionPatch(){EditorApplication.update+=Tick;}
 static void Install(GameObject go){
 if(!go)return;
 var existing=go.GetComponent<StorybookPopupMotion>();
 if(existing){var paper=go.GetComponent<Image>();if(existing.dimmers.Length==0&&paper&&paper.color.a>0){var copy=existing.motion.gameObject.AddComponent<Image>();EditorUtility.CopySerialized(paper,copy);copy.raycastTarget=false;paper.color=Color.clear;}return;}
 var fx=go.AddComponent<StorybookPopupMotion>();var input=go.GetComponent<CanvasGroup>();if(!input)input=go.AddComponent<CanvasGroup>();fx.input=input;input.alpha=1;
 var dims=go.GetComponentsInChildren<Image>(true).Where(i=>!i.sprite&&i.color.a>0&&i.color.a<.9f&&i.color.r<.15f&&i.color.g<.15f&&i.color.b<.15f).Cast<Graphic>().ToArray();fx.dimmers=dims;
 var children=go.transform.Cast<Transform>().ToArray();var wrapper=new GameObject("PopupMotion",typeof(RectTransform),typeof(CanvasGroup));var r=(RectTransform)wrapper.transform;r.SetParent(go.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;fx.motion=r;fx.content=wrapper.GetComponent<CanvasGroup>();
 foreach(var child in children){if(dims.Any(d=>d.transform==child))continue;child.SetParent(r,false);}
 }
 static void Patch(string path,System.Action<GameObject> apply){var root=PrefabUtility.LoadPrefabContents(path);try{apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"patch.request"))return;File.Delete(Out+"patch.request");
 Patch("Assets/DarkFairytale/UI/Shelter/ShelterHome.prefab",g=>Install(g.GetComponent<ShelterHomeUI>().modal));
 Patch("Assets/DarkFairytale/UI/Equipment/StorybookEquipment.prefab",g=>{var u=g.GetComponent<StorybookEquipmentUI>();Install(u.modal);Install(u.options);});
 Patch("Assets/DarkFairytale/UI/DailyReward/StorybookDailyReminder.prefab",Install);
 Patch("Assets/Prefab/Addressable Prefabs/CashMallPackage.prefab",g=>Install(g.GetComponent<CashMallPackage>().view.reward));
 Patch("Assets/Prefab/Addressable Prefabs/RebirthPackage.prefab",g=>Install(g.GetComponentInChildren<StorybookRebirthUI>(true).confirmation));
 Patch("Assets/DarkFairytale/UI/Developer/DeveloperOverlay.prefab",g=>Install(g.GetComponent<DeveloperOverlayUI>().confirmation));
 Patch("Assets/DarkFairytale/UI/Tutorial/StorybookTutorial.prefab",Install);
 File.WriteAllText(Out+"patch.txt","PASS: eight modal roots");}
}
