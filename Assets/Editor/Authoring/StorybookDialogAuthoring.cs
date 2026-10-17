using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit migration only. Never rebuilds authored dialogs on import or play.</summary>
[InitializeOnLoad]
public static class StorybookDialogAuthoring
{
    const string Root="Assets/DarkFairytale/UI/Shared/Dialogs/";
    const string Out="output/common-dialogs/";
    static StorybookDialogAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"install.request"))return;File.Delete(Out+"install.request");try{Install();File.WriteAllText(Out+"install.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"install.txt",e.ToString());Debug.LogException(e);}};}
    static RectTransform Node(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));g.layer=5;var r=(RectTransform)g.transform;r.SetParent(parent,false);return r;}
    static void Full(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Sprite Import(string path,Texture2D tex,Vector4 border){File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spriteBorder=border;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=256;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static void Art(){
        Directory.CreateDirectory(Root+"Textures");Directory.CreateDirectory(Root+"Prefabs");
        var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes("Assets/DarkFairytale/UI/Tutorial/GuidePanel.png"));
        // Compact nine-slice: retain actual guide corners, sample narrow edge strips, omit the centre.
        var frame=new Texture2D(96,96,TextureFormat.RGBA32,false);int corner=32;
        for(int y=0;y<96;y++)for(int x=0;x<96;x++){
            bool center=x>=corner&&x<96-corner&&y>=corner&&y<96-corner;
            int sx=x<corner?48+x*2:x>=64?48+1680-(96-x)*2:48+800+(x-32)*2;
            int sy=y<corner?67+y*2:y>=64?67+748-(96-y)*2:67+350+(y-32)*2;
            frame.SetPixel(x,y,center?Color.clear:source.GetPixel(sx,sy));
        }
        frame.Apply();Import(Root+"Textures/GuideDialogFrame.png",frame,new Vector4(32,32,32,32));
        var paper=new Texture2D(64,64,TextureFormat.RGBA32,false);paper.SetPixels(source.GetPixels(850,450,64,64));paper.Apply();Import(Root+"Textures/GuideDialogPaper.png",paper,Vector4.zero);UnityEngine.Object.DestroyImmediate(source);
    }
    static void Build(){
        var root=Node("CommonDialogChrome",null);root.sizeDelta=new Vector2(870,480);var ui=root.gameObject.AddComponent<StorybookDialogChrome>();
        var paper=Node("Paper",root);Full(paper);paper.offsetMin=new Vector2(18,18);paper.offsetMax=new Vector2(-18,-18);var img=paper.gameObject.AddComponent<Image>();img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Textures/GuideDialogPaper.png");img.type=Image.Type.Tiled;img.raycastTarget=false;
        var frame=Node("GuideFrame",root);Full(frame);img=frame.gameObject.AddComponent<Image>();img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Textures/GuideDialogFrame.png");img.type=Image.Type.Tiled;img.fillCenter=false;img.raycastTarget=false;
        var equipment=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab").GetComponent<StorybookEquipmentUI>();
        var back=UnityEngine.Object.Instantiate(equipment.back.gameObject,root);back.name="Back";ui.back=back.GetComponent<Button>();ui.back.onClick=new Button.ButtonClickedEvent();
        var r=(RectTransform)back.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(28,-8);r.sizeDelta=new Vector2(78,125);r.localScale=Vector3.one;
        if(!back.GetComponent<StorybookReturnAudio>())back.AddComponent<StorybookReturnAudio>();
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"Prefabs/CommonDialogChrome.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
    }
    public static void Apply(RectTransform panel,Button cancel,Button oldClose=null){
        var title=panel.Cast<Transform>().FirstOrDefault(t=>t.name=="Title");
        if(title&&title.GetComponent<Text>())title.GetComponent<Text>().font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Bold.otf");
        if(panel.GetComponentInChildren<StorybookDialogChrome>(true))return;
        var image=panel.GetComponent<Image>();if(image){image.sprite=null;image.material=null;image.color=Color.clear;}
        foreach(var outline in panel.GetComponents<Outline>())UnityEngine.Object.DestroyImmediate(outline);
        foreach(var t in panel.Cast<Transform>().ToArray())if(t.GetComponent<Image>()&&!t.GetComponent<Button>()&&t.GetComponentInChildren<Text>(true)==null)t.gameObject.SetActive(false);
        var shell=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/CommonDialogChrome.prefab"),panel);Full((RectTransform)shell.transform);shell.transform.SetAsFirstSibling();shell.GetComponent<StorybookDialogChrome>().cancelAction=cancel;
        if(oldClose)oldClose.gameObject.SetActive(false);
    }
    static void Patch(string path,Action<GameObject> edit){var root=PrefabUtility.LoadPrefabContents(path);try{edit(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
    [MenuItem("Tools/Dark Fairytale/Unify Common Dialogs")]
    public static void Install(){Art();Build();
        foreach(var path in new[]{"Assets/DarkFairytale/UI/StorybookIntro/Prefabs/CursedBookIntro.prefab","Assets/DarkFairytale/UI/Chapter01Ending/Prefabs/Chapter01Ending.prefab"})Patch(path,g=>{var ui=g.GetComponent<StorySkipUI>();Apply(ui.panel,ui.cancel);});
        Patch("Assets/DarkFairytale/UI/Shelter/Prefabs/ShelterHome.prefab",g=>{var ui=g.GetComponent<ShelterHomeUI>();Apply((RectTransform)ui.modalTitle.transform.parent,ui.modalClose,ui.modalClose);});
        Patch("Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab",g=>{var ui=g.GetComponent<StorybookEquipmentUI>();Apply((RectTransform)ui.modalTitle.transform.parent,ui.modalCancel);});
        Patch("Assets/Prefab/Addressable Prefabs/Packages/RebirthPackage.prefab",g=>{var ui=g.GetComponentInChildren<StorybookRebirthUI>(true);Apply((RectTransform)ui.confirmBody.transform.parent,ui.cancel);});
    }
}
