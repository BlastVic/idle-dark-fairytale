using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class DailySupplyPatch
{
 const string Out="output/daily-supply/";
 static DailySupplyPatch(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"patch.request"))return;File.Delete(Out+"patch.request");
 const string path="Assets/DarkFairytale/UI/DailyReward/StorybookDailyReminder.prefab";var root=PrefabUtility.LoadPrefabContents(path);
 try{var ui=root.GetComponent<StorybookDailyReminder>();var page=ui.open.transform.parent;
 if(!ui.advert)ui.advert=Object.Instantiate(ui.open,page);ui.advert.name="ClaimDoubleWithAd";ui.open.name="ClaimDirect";
 StorybookEquipmentLayout.Box((RectTransform)ui.advert.transform,154,989,308,96);StorybookEquipmentLayout.Box((RectTransform)ui.open.transform,480,989,308,96);
 const string spritePath="Assets/DarkFairytale/UI/DailyReward/Sprites/ClaimButton.asset";
 var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);if(!sprite){var source=ui.open.GetComponent<Image>().sprite;sprite=ProductionUiArt.Create(source.texture,source.rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(32,25,32,25));AssetDatabase.CreateAsset(sprite,spritePath);}
 foreach(var button in new[]{ui.open,ui.advert}){var image=button.GetComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;}
 ui.adCaption=ui.advert.GetComponentInChildren<Text>();ui.claimCaption=ui.open.GetComponentInChildren<Text>();
 foreach(var text in new[]{ui.adCaption,ui.claimCaption}){var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,4);r.offsetMax=new Vector2(-12,-4);text.fontSize=31;text.resizeTextForBestFit=true;text.resizeTextMinSize=24;text.resizeTextMaxSize=31;}
 ui.adCaption.text="看广告领双倍";ui.claimCaption.text="直接领取";
 ui.notice=page.Find("Footer").GetComponent<Text>();StorybookEquipmentLayout.Box(ui.notice.rectTransform,158,1103,625,42);ui.notice.fontSize=24;
 ProductionUiArt.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
