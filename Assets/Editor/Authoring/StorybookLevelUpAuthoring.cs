using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookLevelUpAuthoring
{
 public const string Path="Assets/DarkFairytale/UI/Notifications/StorybookLevelUp.prefab";
 public static bool Busy;
 static StorybookLevelUpAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/level-up/build.request"))return;File.Delete("output/level-up/build.request");try{Busy=true;Build();File.WriteAllText("output/level-up/build.txt","PASS");}catch(Exception e){File.WriteAllText("output/level-up/build.txt",e.ToString());Debug.LogException(e);}finally{Busy=false;}};}
 static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,Color color){var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var t=g.GetComponent<Text>();t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=size;t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.verticalOverflow=VerticalWrapMode.Overflow;t.color=color;t.raycastTarget=false;return t;}
 [MenuItem("Tools/Dark Fairytale/UI/Create Level Up Notification (Explicit)")]
 public static void Build(){
 var gc=UnityEngine.Object.FindAnyObjectByType<GameplayCanvas>(FindObjectsInactive.Include);if(!gc)throw new Exception("Open Gameplay first.");if(gc.levelUpUI)throw new Exception("Already installed; edit prefab directly.");
 if(!AssetDatabase.LoadAssetAtPath<GameObject>(Path)){
 var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StorybookItemToastAuthoring.Path));root.name="StorybookLevelUp";var old=root.GetComponent<StorybookItemToast>();var card=old.card;var group=old.group;
 for(int n=card.childCount-1;n>=0;n--){var child=card.GetChild(n);if(child.name!="Paper"&&child.name!="Shadow"&&child.name!="ThornFrame")UnityEngine.Object.DestroyImmediate(child.gameObject);}
 UnityEngine.Object.DestroyImmediate(old);var ui=root.AddComponent<StorybookLevelUpUI>();ui.card=card;ui.group=group;root.GetComponent<Canvas>().sortingOrder=11500;
 foreach(Transform child in card){var r=(RectTransform)child;r.sizeDelta=child.name=="Paper"?ui.designSize-new Vector2(22,16):ui.designSize;}
 var red=new Color(.45f,.075f,.045f);var ink=new Color(.14f,.11f,.075f);
 ui.heading=Label("Heading",card,new Vector2(0,72),new Vector2(430,48),29,red);ui.heading.text="等级提升";
 ui.level=Label("Level",card,new Vector2(0,5),new Vector2(670,90),56,ink);ui.level.text="Lv. 4  →  5";
 ui.rewards=Label("EarnedPoints",card,new Vector2(0,-69),new Vector2(690,52),27,red);ui.rewards.text="属性点 +1    ·    技能点 +1";
 foreach(float x in new[]{-295f,295f})Label("InkOrnament",card,new Vector2(x,69),new Vector2(70,50),29,ink).text="✦";
 ui.sparks=new Graphic[10];for(int i=0;i<10;i++){float x=(i%2==0?-1:1)*(300+(i/2%2)*54);float y=-85+(i/2)*39;var star=Label("GoldMote"+i,card,new Vector2(x,y),new Vector2(40,40),i%3==0?28:16,new Color(.93f,.68f,.29f,0));star.text="✦";ui.sparks[i]=star;}
 root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);UnityEngine.Object.DestroyImmediate(root);
 }
 var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));gc.levelUpUI=instance.GetComponent<StorybookLevelUpUI>();EditorUtility.SetDirty(gc);EditorSceneManager.MarkSceneDirty(gc.gameObject.scene);EditorSceneManager.SaveScene(gc.gameObject.scene);AssetDatabase.SaveAssets();
 }
}
