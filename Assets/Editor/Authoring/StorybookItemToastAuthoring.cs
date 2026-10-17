using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookItemToastAuthoring
{
    public const string Path="Assets/DarkFairytale/UI/Notifications/StorybookItemToast.prefab";
    const string Out="output/item-toast/";
    public static bool Busy;
    static StorybookItemToastAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Busy=true;Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}finally{Busy=false;}};}
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.sizeDelta=size;r.anchoredPosition=pos;return r;}
    static Image Pic(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite,Color color){var r=Rect(name,parent,pos,size);var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;return i;}
    static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,bool bold,Color color){var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-"+(bold?"Black":"Regular")+".otf");t.fontSize=fontSize;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    [MenuItem("Tools/Dark Fairytale/UI/Create Item Notification (Explicit)")]
    public static void Build(){
        var gc=UnityEngine.Object.FindAnyObjectByType<GameplayCanvas>(FindObjectsInactive.Include);if(!gc)throw new Exception("Open Gameplay first.");
        if(gc.itemToast){gc.itemToast.transform.SetParent(null,false);EditorSceneManager.MarkSceneDirty(gc.gameObject.scene);EditorSceneManager.SaveScene(gc.gameObject.scene);return;}
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Path)){Install(gc);return;}
        Directory.CreateDirectory("Assets/DarkFairytale/UI/Notifications");
        var root=new GameObject("StorybookItemToast",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=11000;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var ui=root.AddComponent<StorybookItemToast>();ui.group=root.GetComponent<CanvasGroup>();ui.group.blocksRaycasts=false;ui.group.interactable=false;ui.card=Rect("PaperNotification",root.transform,Vector2.zero,ui.designSize);
        var frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/PanelFrameV3.asset");
        var paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset");
        var shadow=Pic("Shadow",ui.card,new Vector2(0,-7),ui.designSize,frame,new Color(0,0,0,.45f));shadow.type=Image.Type.Sliced;
        Pic("Paper",ui.card,Vector2.zero,ui.designSize-new Vector2(22,16),paper,Color.white);
        var border=Pic("ThornFrame",ui.card,Vector2.zero,ui.designSize,frame,Color.white);border.type=Image.Type.Sliced;border.fillCenter=false;
        ui.qualityAccent=Pic("QualityAccent",ui.card,new Vector2(-367,0),new Vector2(5,126),null,new Color(.55f,.12f,.08f));
        ui.icon=Pic("ItemIcon",ui.card,new Vector2(-280,0),new Vector2(126,126),null,Color.white);ui.icon.preserveAspect=true;
        Pic("Divider",ui.card,new Vector2(-196,0),new Vector2(1,124),null,new Color(.3f,.2f,.1f,.35f));
        ui.heading=Label("Heading",ui.card,new Vector2(75,61),new Vector2(490,40),23,false,new Color(.48f,.1f,.065f));ui.heading.text="获得装备";
        ui.itemName=Label("ItemName",ui.card,new Vector2(75,4),new Vector2(490,80),34,true,new Color(.12f,.1f,.07f));ui.itemName.text="荆棘猎刀";ui.itemName.resizeTextForBestFit=true;ui.itemName.resizeTextMinSize=24;ui.itemName.resizeTextMaxSize=34;
        ui.detail=Label("QualityAndPrimary",ui.card,new Vector2(75,-60),new Vector2(490,40),24,false,new Color(.3f,.25f,.19f));ui.detail.text="精良  ·  攻击 18";
        root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);UnityEngine.Object.DestroyImmediate(root);
        Install(gc);
    }
    static void Install(GameplayCanvas gc){
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
        gc.itemPopUp.SetActive(false);gc.itemPopUp.name="LegacyItemPopUp (Inactive)";gc.itemPopUp=go;gc.itemToast=go.GetComponent<StorybookItemToast>();EditorUtility.SetDirty(gc);EditorSceneManager.MarkSceneDirty(gc.gameObject.scene);EditorSceneManager.SaveScene(gc.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
