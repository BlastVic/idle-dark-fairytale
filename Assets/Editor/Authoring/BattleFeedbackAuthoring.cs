using System;using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.UI;
[InitializeOnLoad] public static class BattleFeedbackAuthoring {
const string O="output/battle-feedback/";
static BattleFeedbackAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(O+"build.request"))return;File.Delete(O+"build.request");try{Build();File.WriteAllText(O+"build.txt","PASS");}catch(Exception e){File.WriteAllText(O+"build.txt",e.ToString());}};}
static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
static Image Pic(string name,Transform p,float x,float y,float w,float h,string path){var i=Rect(name,p,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);i.raycastTarget=false;return i;}
public static void Build(){const string path="Assets/DarkFairytale/UI/CursedStorybook/Prefabs/CursedStorybookBattleHUD.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{
var s=root.GetComponentInChildren<BattleStatusDetails>(true);var panel=s.notification.parent;UnityEngine.Object.DestroyImmediate(s.notification.gameObject);
s.notificationAnchor.anchoredPosition=new Vector2(32,72);
var r=Rect("PortraitFloat",panel,32,72,460,150);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,.5f);s.notification=r;s.notificationGroup=r.gameObject.AddComponent<CanvasGroup>();s.notificationGroup.alpha=0;s.notificationGroup.blocksRaycasts=false;s.notificationGroup.interactable=false;
var deco=Rect("LevelPaper",r,0,0,460,150);s.levelDecoration=deco.gameObject;
Pic("Paper",deco,0,0,450,142,"Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset");
var frame=Pic("ThornFrame",deco,0,0,460,150,"Assets/DarkFairytale/UI/Equipment/Sprites/PanelFrameV3.asset");frame.type=Image.Type.Sliced;frame.fillCenter=false;
s.waxSeal=Pic("WaxSeal",deco,-185,0,66,66,"Assets/DarkFairytale/UI/Rebirth/Sprites/ReadyWaxSeal.asset").rectTransform;
s.waxSeal.GetComponent<Image>().material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Rebirth/Materials/AuthoredCutout.mat");
var t=Rect("Headline",r,35,19,350,80).gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");t.fontSize=40;t.supportRichText=true;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.text="经验 +25";t.color=new Color(1,.9f,.65f);var o=t.gameObject.AddComponent<Outline>();o.effectColor=new Color(.12f,.055f,.03f,1);o.effectDistance=new Vector2(2,-2);s.notificationText=t;
var xp=UnityEngine.Object.Instantiate(t,r);xp.name="Experience";xp.rectTransform.anchoredPosition=new Vector2(-80,19);xp.rectTransform.sizeDelta=new Vector2(290,90);xp.fontSize=60;s.experienceText=xp;
var reward=Rect("Rewards",r,35,-40,350,42).gameObject.AddComponent<Text>();reward.font=t.font;reward.fontSize=27;reward.alignment=TextAnchor.MiddleCenter;reward.color=new Color(.18f,.13f,.08f);reward.raycastTarget=false;s.rewardText=reward;
s.floatDuration=1.65f;s.levelDuration=2.5f;s.floatDistance=120;
PrefabUtility.SaveAsPrefabAsset(root,path);
}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
