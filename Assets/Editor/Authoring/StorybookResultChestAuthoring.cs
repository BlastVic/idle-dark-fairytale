using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookResultChestAuthoring
{
    const string Root=StorybookBattleResultBuilder.Root;
    static StorybookResultChestAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/battle-result/chest-patch.request"))return;File.Delete("output/battle-result/chest-patch.request");Patch();};}
    [MenuItem("Tools/Dark Fairytale/UI/Patch Result Chest Binding (Explicit)")]
    public static void Patch(){
        AssetDatabase.Refresh();
        foreach(var name in new[]{"StorybookVictory","StorybookDefeat"}){
            var root=PrefabUtility.LoadPrefabContents(Root+"/"+name+".prefab");
            try{Apply(root);PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText("output/battle-result/chest-patch.txt","Both result prefabs share one panel; lantern is an independent editable image.");
    }
    static Sprite Import(string name){
        string file=Root+"/"+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(file);
    }
    static Sprite ChestSprite(string name){
        string file=Root+"/"+name+".png";
        Import(name);
        var importer=(TextureImporter)AssetImporter.GetAtPath(file);
        importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(file);
        var pixels=texture.GetPixels32();int left=texture.width,bottom=texture.height,right=0,top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>5){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        left=Mathf.Max(0,left-8);bottom=Mathf.Max(0,bottom-8);right=Mathf.Min(texture.width-1,right+8);top=Mathf.Min(texture.height-1,top+8);
        string path=Root+"/"+name+".asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(!sprite){sprite=Sprite.Create(texture,new Rect(left,bottom,right-left+1,top-bottom+1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);}
        return sprite;
    }
    public static void Apply(GameObject root){
        var ui=root.GetComponent<StorybookBattleResultUI>();var core=root.transform.Find("Core_9x16");
        core.Find("ApprovedArtwork_NoText").GetComponent<Image>().sprite=Import("ResultPanelShared");
        if(!ui.victory){
            var child=core.Find("DefeatLantern");
            if(!child){child=new GameObject("DefeatLantern",typeof(RectTransform),typeof(Image)).transform;child.SetParent(core,false);}
            var image=child.GetComponent<Image>();image.sprite=Import("DefeatLantern");image.preserveAspect=true;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(139,-492);rect.sizeDelta=new Vector2(416,277);child.SetSiblingIndex(1);
            return;
        }
        ui.normalChest=ChestSprite("RewardChestNormal");ui.goldChest=ChestSprite("RewardChestGold");ui.diamondChest=ChestSprite("RewardChestDiamond");
        if(!ui.chestImage){var go=new GameObject("RewardChest_DynamicQuality",typeof(RectTransform),typeof(Image));go.transform.SetParent(core,false);ui.chestImage=go.GetComponent<Image>();var rect=ui.chestImage.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(191,-531);rect.sizeDelta=new Vector2(310,210);}
        ui.chestImage.sprite=ui.normalChest;ui.chestImage.material=null;ui.chestImage.color=Color.white;ui.chestImage.preserveAspect=true;ui.chestImage.raycastTarget=false;
        ui.rewardTitle.text="普通宝箱";
    }
}
