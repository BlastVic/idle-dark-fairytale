using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StoryMapNodeSetup
{
    public const string Dir="output/story-map/";
    static StoryMapNodeSetup(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Dir+"install.request"))return;
        File.Delete(Dir+"install.request");
        try{Install();File.WriteAllText(Dir+"install-result.txt","PASS");}catch(Exception e){File.WriteAllText(Dir+"install-result.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Dark Fairytale/UI/Add Prologue Map Node (Explicit)")]
    public static void Install(){
        const string root="Assets/DarkFairytale/UI/Map";
        string iconPath=root+"/Art/StoryBookCover.png";
        if(!File.Exists(iconPath))File.Copy("ArtDirection/Storybook/SpineSource/images/cover.png",iconPath);
        AssetDatabase.ImportAsset(iconPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(iconPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        var player=AssetDatabase.LoadAssetAtPath<GameObject>(CursedBookIntroBuilder.Prefab).GetComponent<CursedBookIntro>();
        string storyPath=root+"/PrologueStory.asset";
        var story=AssetDatabase.LoadAssetAtPath<StorySequence>(storyPath);
        if(!story){story=ScriptableObject.CreateInstance<StorySequence>();story.pages=player.prologue.pages;AssetDatabase.CreateAsset(story,storyPath);}
        var go=PrefabUtility.LoadPrefabContents(StorybookMapBuilder.Path);
        try {
            var ui=go.GetComponent<StorybookMapUI>();ui.storyPlayerPrefab=player;
            if(!ui.nodes.Any(n=>n.IsStory)){
                var first=ui.nodes[0];var node=UnityEngine.Object.Instantiate(first,first.transform.parent);
                node.name="Story_Prologue";node.kind=StorybookMapNode.NodeKind.Story;node.story=story;node.levelKey="";node.displayName="序章 · 逃离祭祀";
                var r=(RectTransform)node.transform;r.anchoredPosition+=new Vector2(-20,-205);
                node.customSeal=icon;node.customMaterial=null;node.seal.sprite=icon;node.seal.material=null;node.seal.preserveAspect=true;
                node.seal.rectTransform.sizeDelta=new Vector2(88,110);
                node.label.text="序章 · 逃离祭祀";node.label.fontSize=19;
                node.labelRoot.transform.localPosition+=new Vector3(0,-22,0);node.progress.rectTransform.anchoredPosition+=new Vector2(0,-22);
                node.incomingRoute=Array.Empty<Image>();node.preview=icon;node.progress.text="剧情 · 可重播";node.locator.SetActive(false);
                ui.nodes=new[]{node}.Concat(ui.nodes).ToArray();
                var dots=new System.Collections.Generic.List<Image>();
                for(int i=0;i<5;i++){
                    var dot=new GameObject("PrologueRoute_"+i,typeof(RectTransform),typeof(Image));dot.layer=5;dot.transform.SetParent(first.transform.parent,false);
                    var rt=(RectTransform)dot.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
                    rt.anchoredPosition=((RectTransform)first.transform).anchoredPosition+new Vector2(90,-105-i*17);rt.sizeDelta=new Vector2(9,9);
                    var im=dot.GetComponent<Image>();im.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");im.raycastTarget=false;dots.Add(im);
                    dot.transform.SetSiblingIndex(first.transform.GetSiblingIndex());
                }
                first.incomingRoute=dots.ToArray();
            }
            var storyNode=ui.nodes.First(n=>n.IsStory);
            string previewPath=root+"/ProloguePreview.asset";
            var preview=AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
            if(!preview){
                var texture=story.pages[0].illustration;float h=texture.width*132f/218;
                preview=Sprite.Create(texture,new Rect(0,Mathf.Clamp(texture.height*.58f-h*.5f,0,texture.height-h),texture.width,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);preview.name="ProloguePreview";AssetDatabase.CreateAsset(preview,previewPath);
            }
            if(storyNode.preview==icon)storyNode.preview=preview;
            if(!storyNode.seal.transform.Find("StoryMark")){
                var mark=UnityEngine.Object.Instantiate(storyNode.label,storyNode.seal.transform);mark.name="StoryMark";mark.text="序";mark.fontSize=34;mark.color=new Color(.93f,.83f,.62f);mark.raycastTarget=false;
                var rt=mark.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            }
            ui.battleDetailGlyph=ui.layout.detail.GetComponentsInChildren<Transform>(true).First(t=>t.name=="CrossedSwords").gameObject;
            if(!ui.storyDetailGlyph){
                var glyph=UnityEngine.Object.Instantiate(ui.battleDetailGlyph,ui.battleDetailGlyph.transform.parent);glyph.name="StoryBookGlyph";
                var im=glyph.GetComponent<Image>();im.sprite=icon;im.material=null;im.preserveAspect=true;glyph.SetActive(false);ui.storyDetailGlyph=glyph;
            }
            PrefabUtility.SaveAsPrefabAsset(go,StorybookMapBuilder.Path);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
}
