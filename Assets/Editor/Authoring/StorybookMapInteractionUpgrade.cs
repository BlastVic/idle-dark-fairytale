using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, incremental migration of the authored prefab; preserves existing art and manual edits.</summary>
[InitializeOnLoad]
public static class StorybookMapInteractionUpgrade
{
    static StorybookMapInteractionUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="output/map-ui/upgrade.request";
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        File.Delete(request);
        var root=PrefabUtility.LoadPrefabContents(StorybookMapBuilder.Path);
        try{Apply(root);ProductionUiArt.SaveAsPrefabAsset(root,StorybookMapBuilder.Path);AssetDatabase.SaveAssets();File.WriteAllText("output/map-ui/upgrade-result.txt","PASS");}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    [MenuItem("Tools/Dark Fairytale/UI/Upgrade Map Interactions (Explicit)")]
    static void Request(){Directory.CreateDirectory("output/map-ui");File.WriteAllText("output/map-ui/upgrade.request","");}
    static void CorrectSeal(string name,Rect topRect,ref Sprite sprite,ref Material material)
    {
        string path=StorybookMapBuilder.Root+"/Sprites/"+name+".asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArtSources/UI/Map/Art/ApprovedMap.png");
        var rect=new Rect(topRect.x,texture.height-topRect.y-topRect.height,topRect.width,topRect.height);
        var slice=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(!slice){slice=ProductionUiArt.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);slice.name=name;AssetDatabase.CreateAsset(slice,path);}
        string matPath=StorybookMapBuilder.Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){mat=new Material(material);AssetDatabase.CreateAsset(mat,matPath);}
        mat.SetVector("_SourceRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
        EditorUtility.SetDirty(mat);sprite=slice;material=mat;
    }
    public static void Apply(GameObject root)
    {
        var ui=root.GetComponent<StorybookMapUI>();
        // The old rectangles included the top of their original labels. Correct the atlas
        // rectangles now that labels are separate, centred below each medallion.
        CorrectSeal("CompletedSealClean",new Rect(148,1046,80,78),ref ui.completedSeal,ref ui.completedMaterial);
        CorrectSeal("AvailableSealClean",new Rect(460,698,88,80),ref ui.availableSeal,ref ui.availableMaterial);
        CorrectSeal("LockedSealClean",new Rect(712,592,84,78),ref ui.lockedSeal,ref ui.lockedMaterial);
        foreach(var n in ui.nodes){
            if(!n.customSeal){n.seal.sprite=ui.completedSeal;n.seal.material=ui.completedMaterial;}
            var label=(RectTransform)n.label.transform.parent;var seal=n.seal.rectTransform;
            float center=seal.anchoredPosition.x+seal.rect.width*(.5f-seal.pivot.x);
            label.anchoredPosition=new Vector2(center-label.rect.width*.5f,seal.anchoredPosition.y-seal.rect.height*(1-seal.pivot.y)-4);
            n.labelRoot=label.gameObject;
            n.progress.rectTransform.anchoredPosition=new Vector2(center-n.progress.rectTransform.rect.width*.5f,label.anchoredPosition.y-label.rect.height-3);
        }
        var card=ui.layout.detail;
        var motion=card.GetComponent<StorybookMapCardMotion>();if(!motion)motion=card.gameObject.AddComponent<StorybookMapCardMotion>();
        if(!motion.content){
            var children=card.Cast<Transform>().Where(t=>t.name!="DetailFrame").ToArray();
            var content=new GameObject("AnimatedContent",typeof(RectTransform),typeof(CanvasGroup));content.layer=5;
            var rect=(RectTransform)content.transform;rect.SetParent(card,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            foreach(var child in children)child.SetParent(rect,false);
            motion.content=rect;motion.group=content.GetComponent<CanvasGroup>();
        }
        ui.detailMotion=motion;
    }
}
