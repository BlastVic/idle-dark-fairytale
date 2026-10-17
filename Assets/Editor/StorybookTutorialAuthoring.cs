using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookTutorialAuthoring
{
    public const string Root="Assets/DarkFairytale/UI/Tutorial/",Path=Root+"StorybookTutorial.prefab";
    static StorybookTutorialAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/tutorial-ui/build.request"))return;File.Delete("output/tutorial-ui/build.request");try{if(AssetDatabase.LoadAssetAtPath<GameObject>(Path))Refine();else Build();File.WriteAllText("output/tutorial-ui/build.txt","PASS");}catch(Exception e){File.WriteAllText("output/tutorial-ui/build.txt",e.ToString());Debug.LogException(e);}};}
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Image Pic(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite,Color colour,bool hit=false){var r=Rect(name,parent,pos,size);var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=colour;i.raycastTarget=hit;return i;}
    static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string value,int fontSize,Color colour,bool bold=false){var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-"+(bold?"Black":"Regular")+".otf");t.text=value;t.fontSize=fontSize;t.color=colour;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Sprite Import(string name){var path=Root+name+".png";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();int x0=tex.width,y0=tex.height,x1=0,y1=0;for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>20){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}var s=ProductionUiArt.Create(tex,new UnityEngine.Rect(x0,y0,x1-x0+1,y1-y0+1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(s,Root+name+".asset");return s;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Tutorial (Explicit)")]
    public static void Build(){
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Path))throw new InvalidOperationException("Prefab already exists; edit it directly instead of overwriting manual adjustments.");
        var panel=Import("GuidePanel");var arrow=Import("GuideArrow");
        var root=new GameObject("StorybookTutorial",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=12000;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var ui=root.AddComponent<StorybookTutorialUI>();ui.presentation=root.GetComponent<CanvasGroup>();ui.dimmers=new Image[4];for(int i=0;i<4;i++)ui.dimmers[i]=Pic("Dimmer"+i,root.transform,Vector2.zero,Vector2.one,null,new Color(0,0,0,.53f),true);
        ui.highlight=Rect("TargetHighlight",root.transform,Vector2.zero,new Vector2(220,220));ui.glow=ui.highlight.gameObject.AddComponent<CanvasGroup>();ui.glow.blocksRaycasts=false;
        Color gold=new Color(.94f,.73f,.35f);for(int i=0;i<4;i++){var line=Pic("GoldEdge"+i,ui.highlight,Vector2.zero,Vector2.zero,null,gold).rectTransform;line.anchorMin=i<2?new Vector2(0,i):new Vector2(i-2,0);line.anchorMax=i<2?new Vector2(1,i):new Vector2(i-2,1);line.sizeDelta=i<2?new Vector2(0,2):new Vector2(2,0);}
        ui.arrow=Pic("DirectionArrow",root.transform,Vector2.zero,new Vector2(85,155),arrow,Color.white).rectTransform;
        ui.card=Pic("GuideCard",root.transform,Vector2.zero,ui.cardSize,panel,Color.white,true).rectTransform;
        var ink=new Color(.13f,.105f,.075f);var red=new Color(.42f,.07f,.06f);
        Label("Header",ui.card,new Vector2(0,163),new Vector2(350,58),"旅途指引",32,ink,true);
        ui.heading=Label("Heading",ui.card,new Vector2(0,78),new Vector2(810,84),"启程，前往黑森林",53,red,true);
        ui.body=Label("Body",ui.card,new Vector2(0,-32),new Vector2(810,104),"打开地图，选择第一个可挑战节点，\n开始你的探索。",32,ink);
        ui.cue=Label("Cue",ui.card,new Vector2(0,-149),new Vector2(750,58),"点击下方「地图」",31,ink,true);
        foreach(float y in new[]{127f,-108f})Pic("Divider",ui.card,new Vector2(0,y),new Vector2(715,1),null,new Color(.38f,.29f,.17f,.45f));
        var close=Pic("Close",ui.card,new Vector2(397,164),new Vector2(72,72),null,Color.clear,true);ui.close=close.gameObject.AddComponent<Button>();ui.close.transition=Selectable.Transition.None;close.gameObject.AddComponent<ShelterButtonFeedback>();Label("Caption",close.transform,Vector2.zero,new Vector2(65,65),"×",46,ink);
        ui.steps=new[]{Step("启程，前往黑森林","打开地图，选择第一个可挑战节点，\n开始你的探索。","点击下方「地图」"),Step("选择探索节点","沿着林间小路，\n从第一个可挑战节点开始。","点击高亮的节点"),Step("让生命更加坚韧","使用获得的属性点，\n提升生命，为下一次探索做准备。","点击生命旁的「＋」"),Step("查看旅途的收获","背包里有新的装备。\n挑选合适的装备，提升战斗能力。","点击下方「装备」"),Step("穿戴新的装备","比较装备属性与换装变化，\n确认后将它装备到身上。","点击「装备」按钮"),Step("分解多余的装备","分解未穿戴、未锁定的装备，\n可积累融合进度。确认前仍可取消。","点击「分解」查看确认信息"),Step("打开探索的宝箱","战斗带回了新的宝箱。\n打开宝箱，查看旅途的奖励。","点击高亮的宝箱"),Step("融合之力已苏醒","融合进度已满，\n前往融合页面领取成长奖励。","点击右侧「融合」"),Step("前往商店查看","血月结晶不足时，\n可前往商店了解获取方式。","点击下方「商店」")};
        ui.presentation.alpha=0;ui.presentation.blocksRaycasts=false;root.SetActive(false);ProductionUiArt.SaveAsPrefabAsset(root,Path);UnityEngine.Object.DestroyImmediate(root);
        var manager=UnityEngine.Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);if(!manager)throw new InvalidOperationException("Open Gameplay scene first.");var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),manager.transform);manager.view=instance.GetComponent<StorybookTutorialUI>();foreach(var old in manager.tutorials)if(old)old.SetActive(false);manager.overlay.SetActive(false);manager.tutorialCharacter.SetActive(false);EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void Refine(){var go=PrefabUtility.LoadPrefabContents(Path);try{
        var ui=go.GetComponent<StorybookTutorialUI>();foreach(var t in go.GetComponentsInChildren<Text>(true))t.verticalOverflow=VerticalWrapMode.Overflow;
        ui.cardGap=225;ui.card.GetComponent<Image>().preserveAspect=true;ui.heading.fontSize=64;ui.body.font=ui.heading.font;ui.body.fontSize=32;ui.cue.color=ui.heading.color;
        if(!ui.card.Find("QuillBookmark")){
            string path="Assets/ArtSources/UI/Tutorial/GuideReference.png";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite=ProductionUiArt.Create(tex,new UnityEngine.Rect(108,737,72,102),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
            // Exact approved bookmark pixels; polygon follows the pointed lower edge.
            sprite.OverrideGeometry(new[]{new Vector2(-.36f,.51f),new Vector2(.36f,.51f),new Vector2(.36f,-.51f),new Vector2(0,-.24f),new Vector2(-.36f,-.51f)},new ushort[]{0,1,3,1,2,3,0,3,4});AssetDatabase.CreateAsset(sprite,Root+"QuillBookmark.asset");Pic("QuillBookmark",ui.card,new Vector2(-375,173),new Vector2(80,114),sprite,Color.white);
            var star=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/RuleCross.asset");var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/ReferenceInk.mat");
            for(int i=0;i<4;i++){var image=Pic("CornerStar"+i,ui.highlight,Vector2.zero,new Vector2(25,25),star,new Color(1,.8f,.4f));image.material=ink;image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(i%2,i/2);}
            foreach(float x in new[]{-125f,125f}){var image=Pic("HeaderOrnament",ui.card,new Vector2(x,163),new Vector2(30,30),star,Color.black);image.material=ink;}
        }
        ProductionUiArt.SaveAsPrefabAsset(go,Path);
    }finally{PrefabUtility.UnloadPrefabContents(go);}}
    static StorybookTutorialUI.Step Step(string title,string body,string cue)=>new StorybookTutorialUI.Step{title=title,body=body,cue=cue};
}
