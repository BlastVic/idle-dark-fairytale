using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class StorySkipAuthoring {
 const string Dir="output/story-skip/";
 static StorySkipAuthoring(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"install.request"))return;File.Delete(Dir+"install.request");Install();}
 static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position){var g=new GameObject(name,typeof(RectTransform));g.layer=5;var r=(RectTransform)g.transform;r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;}
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 static Text Label(Transform parent,string name,string value,Font font,int size,Vector2 dimensions,Vector2 pos){var t=Rect(name,parent,dimensions,pos).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.15f,.11f,.09f);t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;return t;}
 static Button Button(Transform parent,string name,string value,Font font,Vector2 size,Vector2 pos){var r=Rect(name,parent,size,pos);var image=r.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/CursedStorybook/Sprites/PaperButton.asset");image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.96f,.88f);c.pressedColor=new Color(.65f,.57f,.48f);c.disabledColor=Color.gray;b.colors=c;Label(r,"Label",value,font,32,size,Vector2.zero);Style(b);return b;}
 static void Style(Button b){
  // A solid native UI surface stays sharp at every supported resolution.
  var image=b.GetComponent<Image>();image.sprite=null;image.material=null;image.type=Image.Type.Simple;
  image.color=new Color(.14f,.115f,.095f,.98f);
  var edge=b.GetComponent<Outline>();if(!edge)edge=b.gameObject.AddComponent<Outline>();
  edge.effectColor=new Color(.68f,.59f,.43f,1);edge.effectDistance=new Vector2(1.5f,-1.5f);edge.useGraphicAlpha=true;
  foreach(var t in b.GetComponentsInChildren<Text>(true)){t.fontStyle=FontStyle.Normal;t.fontSize=34;t.color=new Color(1f,.95f,.85f);}
  if(!b.GetComponent<ShelterButtonFeedback>())b.gameObject.AddComponent<ShelterButtonFeedback>();
  var state=b.GetComponent<StorybookEquipmentButtonState>();if(!state)state=b.gameObject.AddComponent<StorybookEquipmentButtonState>();
  state.grayscaleShader=Shader.Find("UI/Storybook Disabled");
 }
 static void StylePanel(StorySkipUI ui){
  var image=ui.panel.GetComponent<Image>();image.sprite=null;image.material=null;image.type=Image.Type.Simple;image.color=new Color(.88f,.83f,.72f,1);
  var edge=ui.panel.GetComponent<Outline>();if(!edge)edge=ui.panel.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.38f,.29f,.18f,1);edge.effectDistance=new Vector2(3,-3);
  var sr=(RectTransform)ui.skip.transform;sr.sizeDelta=new Vector2(172,76);sr.anchorMin=sr.anchorMax=new Vector2(1,1);sr.anchoredPosition=new Vector2(-116,-68);
  var label=ui.skip.GetComponentInChildren<Text>();label.rectTransform.sizeDelta=sr.sizeDelta;
 }
 [MenuItem("Tools/Dark Fairytale/Install Story Skip Controls")]
 public static void Install(){
 foreach(string path in new[]{"Assets/DarkFairytale/UI/StorybookIntro/CursedBookIntro.prefab","Assets/DarkFairytale/UI/Chapter01Ending/Chapter01Ending.prefab"}) {
 var root=PrefabUtility.LoadPrefabContents(path);
 try {
  if(root.GetComponent<StorySkipUI>()){
    var existing=root.GetComponent<StorySkipUI>();foreach(var button in new[]{existing.skip,existing.cancel,existing.confirm})Style(button);StylePanel(existing);
    PrefabUtility.SaveAsPrefabAsset(root,path);continue;
  }
  var intro=root.GetComponent<CursedBookIntro>();var ui=root.AddComponent<StorySkipUI>();ui.intro=intro;ui.core=root.GetComponent<CursedBookIntroLayout>().core;
  ui.skip=Button(ui.core,"SkipButton","跳过",intro.prologue.subtitle.font,new Vector2(156,66),new Vector2(-108,-58));var sr=(RectTransform)ui.skip.transform;sr.anchorMin=sr.anchorMax=new Vector2(1,1);
  var modal=Rect("SkipConfirmation",root.transform,Vector2.zero,Vector2.zero);Stretch(modal);modal.gameObject.AddComponent<Image>().color=new Color(0,0,0,.72f);ui.confirmation=modal.gameObject;
  ui.panel=Rect("PaperPanel",modal,new Vector2(870,480),Vector2.zero);var paper=ui.panel.gameObject.AddComponent<Image>();paper.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/CursedStorybook/Sprites/PaperButton.asset");paper.type=Image.Type.Sliced;
  Label(ui.panel,"Title","跳过剧情",intro.prologue.subtitle.font,42,new Vector2(780,70),new Vector2(0,160));
  Label(ui.panel,"Message","你确定要跳过剧情吗？你可以在地图里重新观看剧情。",intro.prologue.subtitle.font,34,new Vector2(740,150),new Vector2(0,35));
  ui.cancel=Button(ui.panel,"CancelButton","继续观看",intro.prologue.subtitle.font,new Vector2(300,86),new Vector2(-180,-145));
  ui.confirm=Button(ui.panel,"ConfirmButton","确认跳过",intro.prologue.subtitle.font,new Vector2(300,86),new Vector2(180,-145));
  StylePanel(ui);modal.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 File.WriteAllText(Dir+"install.txt","Installed saved skip UI on both story prefabs.");
 }
}
