using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad] public static class StorybookDebugEntryAuthoring{
 static StorybookDebugEntryAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/debug-entry/build.request"))return;File.Delete("output/debug-entry/build.request");try{Build();File.WriteAllText("output/debug-entry/build.txt","PASS");}catch(Exception e){File.WriteAllText("output/debug-entry/build.txt",e.ToString());}};}
 static void Build(){var root=PrefabUtility.LoadPrefabContents(StorybookSettingsBuilder.Path);try{var ui=root.GetComponent<StorybookSettingsUI>();if(ui.developer)return;var template=ui.scroll.content.Find("StartupPreferences");var row=UnityEngine.Object.Instantiate(template.gameObject,ui.scroll.content);row.name="DeveloperTools";for(int i=row.transform.childCount-1;i>=0;i--)if(row.transform.GetChild(i).name!="FineFrame")UnityEngine.Object.DestroyImmediate(row.transform.GetChild(i).gameObject);row.GetComponent<LayoutElement>().preferredHeight=110;
 var bg=row.AddComponent<Image>();bg.color=new Color(1,1,1,.001f);bg.raycastTarget=true;var b=row.AddComponent<Button>();b.transition=Selectable.Transition.None;row.AddComponent<ShelterButtonFeedback>();ui.developer=b;
 Label(row.transform,"Title","开发者调试",22,12,380,38,25);Label(row.transform,"Description","打开调试菜单，调整测试数据",22,56,400,32,17);Label(row.transform,"Arrow","›",437,27,35,50,34);
 PrefabUtility.SaveAsPrefabAsset(root,StorybookSettingsBuilder.Path);AssetDatabase.SaveAssets();}finally{PrefabUtility.UnloadPrefabContents(root);}}
 static void Label(Transform parent,string name,string text,float x,float y,float w,float h,int size){var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);StorybookEquipmentLayout.Box((RectTransform)g.transform,x,y,w,h);var t=g.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");t.fontSize=size;t.text=text;t.color=new Color(.17f,.11f,.075f);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;t.alignment=TextAnchor.MiddleLeft;}
}
