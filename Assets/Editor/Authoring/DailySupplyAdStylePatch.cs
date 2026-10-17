using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class DailySupplyAdStylePatch
{
 const string Out="output/daily-supply-ad-style/";
 static DailySupplyAdStylePatch(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"patch.request"))return;File.Delete(Out+"patch.request");
 const string path="Assets/DarkFairytale/UI/DailyReward/Prefabs/StorybookDailyReminder.prefab";var root=PrefabUtility.LoadPrefabContents(path);
 try{
 var ui=root.GetComponent<StorybookDailyReminder>();var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/Experience/StorybookExperience.prefab").GetComponent<StorybookExperienceUI>();
 var target=ui.advert.GetComponent<Image>();var original=source.advert.GetComponent<Image>();const string borderPath="Assets/DarkFairytale/UI/DailyReward/Sprites/IvoryAdButton.asset";
 var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(borderPath);if(!sprite){var src=original.sprite;sprite=ProductionUiArt.Create(src.texture,src.rect,new Vector2(.5f,.5f),src.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(24,18,24,18));AssetDatabase.CreateAsset(sprite,borderPath);}
 target.sprite=sprite;target.material=original.material;target.color=original.color;target.type=Image.Type.Sliced;
 ui.advert.transition=source.advert.transition;ui.advert.colors=source.advert.colors;
 if(!ui.advert.GetComponent<ShelterButtonFeedback>())ui.advert.gameObject.AddComponent<ShelterButtonFeedback>();
 var old=ui.advert.transform.Find("Film");if(old)Object.DestroyImmediate(old.gameObject);
 var film=Object.Instantiate(source.advert.transform.Find("Film").gameObject,ui.advert.transform).GetComponent<RectTransform>();film.name="Film";film.anchorMin=film.anchorMax=film.pivot=new Vector2(0,.5f);film.anchoredPosition=new Vector2(25,0);film.sizeDelta=new Vector2(34,36);
 var label=ui.adCaption;label.font=source.adCaption.font;label.fontStyle=source.adCaption.fontStyle;label.color=source.adCaption.color;label.fontSize=27;label.resizeTextMinSize=24;label.resizeTextMaxSize=27;
 label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(65,4);label.rectTransform.offsetMax=new Vector2(-15,-4);
 ProductionUiArt.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
}
