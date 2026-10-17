using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookDailyAuthoring
{
 public const string Root="Assets/DarkFairytale/UI/DailyReward",Path="Assets/DarkFairytale/UI/DailyReward/Prefabs/StorybookDailyReminder.prefab",Out="output/daily-reward/";
 static Texture2D art;static Font font;
 static StorybookDailyAuthoring(){EditorApplication.update+=PollBackPatch;EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());}};}
 static RectTransform Node(Transform p,string n,float x,float y,float w,float h){var g=new GameObject(n,typeof(RectTransform));g.layer=5;g.transform.SetParent(p,false);var r=(RectTransform)g.transform;StorybookEquipmentLayout.Box(r,x,y,w,h);return r;}
 static Sprite Slice(string n,int x,int y,int w,int h){string p=Root+"/Sprites/"+n+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(p);if(s)return s;s=ProductionUiArt.Create(art,new Rect(x,art.height-y-h,w,h),Vector2.one*.5f,100);AssetDatabase.CreateAsset(s,p);return s;}
 static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h){var i=Node(p,n,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=s;i.raycastTarget=false;return i;}
 static Text Txt(Transform p,string n,string value,float x,float y,float w,float h,int size){var t=Node(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.04f,.025f,.015f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
 static Button Btn(Transform p,string n,Sprite s,float x,float y,float w,float h,string value,int size,out Text label){var i=Pic(p,n,s,x,y,w,h);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.62f,.55f,.5f);c.disabledColor=Color.white;b.colors=c;i.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");label=Txt(i.transform,"Caption",value,0,0,w,h,size);label.color=new Color(.99f,.94f,.82f);return b;}
 [MenuItem("Tools/Dark Fairytale/UI/Build Daily Reward (Explicit)")]
 public static void Build(){Directory.CreateDirectory(Root+"/Sprites");AssetDatabase.Refresh();art=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/DailyReward/Art/BlankDaily.png");font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
 var root=Node(null,"StorybookDailyReminder",0,0,941,1672);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10800;root.gameObject.AddComponent<GraphicRaycaster>();var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
 var ui=root.gameObject.AddComponent<StorybookDailyReminder>();ui.group=root.gameObject.AddComponent<CanvasGroup>();var dim=Pic(root,"DimAndBlock",null,0,0,941,1672);dim.color=new Color(0,0,0,.65f);dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
 var page=Node(root,"Core_9x16",0,0,941,1672);var layout=root.gameObject.AddComponent<StorybookFusionLayout>();layout.page=page;layout.designSize=new Vector2(941,1672);layout.margin=0;
 var blank=art;art=ShelterRefinedBuilder.Import(Root+"/Art/DailyBasePlate.png");
 var panel=Pic(page,"ApprovedPaper",Slice("BasePaper",108,449,725,730),108,449,725,730);art=blank;
 var illustration=Pic(page,"ApprovedCrystalIllustration",Slice("Crystal",274,660,409,220),274,660,409,220);illustration.material=Ink();

 Txt(page,"Title","每日补给",328,489,295,80,54);Txt(page,"Subtitle","为下一段旅途，备好补给",230,602,485,49,30);
 Txt(page,"Currency","血月结晶 ×",300,895,228,70,40);ui.amount=Txt(page,"Amount","10",530,874,118,90,78);ui.amount.color=new Color(.5f,.04f,.025f);
 ui.open=Btn(page,"GoToShop",Slice("Button",226,982,491,111),226,982,491,111,"前往领取",49,out _);ui.open.GetComponent<Image>().material=Ink();
 ui.close=Btn(page,"BackBookmark",null,119,448,56,132,"",1,out _);ConfigureBack(ui);
 Txt(page,"Footer","每日可领取一次",379,1103,239,42,25);root.gameObject.SetActive(false);ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);UnityEngine.Object.DestroyImmediate(root.gameObject);
 var gc=UnityEngine.Object.FindAnyObjectByType<GameplayCanvas>(FindObjectsInactive.Include);var controller=gc.GetComponent<StorybookDailyReward>();if(!controller)controller=gc.gameObject.AddComponent<StorybookDailyReward>();if(controller.reminder)UnityEngine.Object.DestroyImmediate(controller.reminder.gameObject);var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));controller.reminder=instance.GetComponent<StorybookDailyReminder>();if(gc.dailyCherriesPop)gc.dailyCherriesPop.SetActive(false);gc.dailyCherriesPop=instance;gc.cherriesDailyText=controller.reminder.amount;EditorUtility.SetDirty(gc);EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(gc.gameObject.scene);EditorSceneManager.SaveScene(gc.gameObject.scene);
 PatchShop();AssetDatabase.SaveAssets();}
 static void ConfigureBack(StorybookDailyReminder ui){
 var b=ui.close;b.name="BackBookmark";StorybookEquipmentLayout.Box((RectTransform)b.transform,119,448,56,132);
 var image=b.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Fusion/Sprites/BackRibbon.asset");image.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Fusion/Materials/InkCutout.mat");image.preserveAspect=true;
 foreach(var t in b.GetComponentsInChildren<Text>(true))UnityEngine.Object.DestroyImmediate(t.gameObject);
 var old=b.transform.parent.Find("CloseRibbon");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 }
 static void PollBackPatch(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"back-patch.request"))return;File.Delete(Out+"back-patch.request");try{PatchBack();File.WriteAllText(Out+"back-patch.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"back-patch.txt",e.ToString());}}
 [MenuItem("Tools/Dark Fairytale/UI/Update Daily Return Button (Explicit)")]
 public static void PatchBack(){var root=PrefabUtility.LoadPrefabContents(Path);try{ConfigureBack(root.GetComponent<StorybookDailyReminder>());ProductionUiArt.SaveAsPrefabAsset(root,Path);}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();}
 static Material Ink(){var p="Assets/DarkFairytale/UI/DailyReward/Materials/Ink.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("UI/Chest Authored Cutout"));AssetDatabase.CreateAsset(m,p);}m.SetFloat("_Mode",0);m.SetFloat("_InkOnly",0);m.SetFloat("_Cutoff",.57f);m.SetFloat("_Feather",.16f);return m;}
 static void PatchShop(){var root=PrefabUtility.LoadPrefabContents(StorybookShopBuilder.Path);try{var ui=root.GetComponent<StorybookShopUI>();var content=ui.scroll.content;if(content.Find("DailySupply")){ui.dailyClaim.GetComponent<Image>().material=Ink();content.Find("DailySupply/ApprovedCrystal").GetComponent<Image>().material=Ink();ProductionUiArt.SaveAsPrefabAsset(root,StorybookShopBuilder.Path);return;}
 foreach(string name in new[]{"Pouch80","Chest500","Status"}){var r=(RectTransform)content.Find(name);r.anchoredPosition+=Vector2.down*235;}content.sizeDelta+=new Vector2(0,235);
 var card=Pic(content,"DailySupply",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset"),20,245,855,215);card.type=Image.Type.Tiled;var border=Pic(card.transform,"Frame",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shop/Sprites/CardFrame.asset"),0,0,855,215);border.type=Image.Type.Sliced;border.fillCenter=false;
 var crystal=Pic(card.transform,"ApprovedCrystal",Slice("Crystal",274,660,409,220),20,30,210,113);crystal.preserveAspect=true;crystal.material=Ink();Txt(card.transform,"Title","每日补给",220,18,285,55,35);ui.dailyAmount=Txt(card.transform,"Amount","血月结晶 ×10",220,80,285,53,29);Txt(card.transform,"Hint","每日可领取一次",220,146,285,36,22);
 ui.dailyClaim=Btn(card.transform,"Claim",Slice("Button",226,982,491,111),531,65,287,78,"免费领取",31,out ui.dailyCaption);ui.dailyClaim.GetComponent<Image>().preserveAspect=true;ui.dailyClaim.GetComponent<Image>().material=Ink();ProductionUiArt.SaveAsPrefabAsset(root,StorybookShopBuilder.Path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
}
