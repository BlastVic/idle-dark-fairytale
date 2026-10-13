using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookNavigationAuthoring
{
    public const string Path="Assets/DarkFairytale/UI/Shared/StorybookNavigation.prefab";
    const string Out="output/shared-navigation/";
    static StorybookNavigationAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}};}
    [MenuItem("Tools/Dark Fairytale/UI/Unify Bottom Navigation (Explicit)")]
    public static void Build(){
        Directory.CreateDirectory("Assets/DarkFairytale/UI/Shared");
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(Path))Create();
        foreach(var path in new[]{ShelterHomeBuilder.Path,StorybookEquipmentBuilder.Path,StorybookSkillsBuilder.Path,StorybookMapBuilder.Path,StorybookShopBuilder.Path})Migrate(path);
        RefineArt();
        AssetDatabase.SaveAssets();
    }
    static void Create(){
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(ShelterHomeBuilder.Path).GetComponent<ShelterHomeUI>();
        var original=source.GetComponent<ShelterResponsiveLayout>().navigation;
        var go=UnityEngine.Object.Instantiate(original.gameObject);go.name="StorybookNavigation";
        try{
            var nav=go.AddComponent<StorybookNavigation>();nav.buttons=source.navigationButtons.Select(b=>go.transform.Find(AnimationUtility.CalculateTransformPath(b.transform,original)).GetComponent<Button>()).ToArray();
            nav.skillBadge=go.transform.Find(AnimationUtility.CalculateTransformPath(source.skillBadge.transform,original)).GetComponent<Text>();
            var template=nav.buttons[2].transform.Find("ReferenceBookmark").gameObject;
            // Use only the frame's border pixels; the original illustration/text live in the discarded center.
            var edge=template.transform.Find("BookmarkEdges").GetComponent<Image>();var old=edge.sprite;
            var border=Sprite.Create(old.texture,old.rect,old.pivot/old.rect.size,old.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(15,41,15,29));border.name="NavigationBookmarkBorder";AssetDatabase.CreateAsset(border,"Assets/DarkFairytale/UI/Shared/NavigationBookmarkBorder.asset");
            edge.sprite=border;edge.gameObject.SetActive(true);edge.fillCenter=false;
            foreach(Transform child in template.transform.Cast<Transform>().ToArray())if(child.name.StartsWith("OriginalBookmark"))UnityEngine.Object.DestroyImmediate(child.gameObject);
            nav.bookmarks=new GameObject[5];nav.captions=new Text[5];
            for(int i=0;i<5;i++){
                var b=nav.buttons[i];b.onClick=new Button.ButtonClickedEvent();b.transition=Selectable.Transition.None;
                var book=i==2?template:UnityEngine.Object.Instantiate(template,b.transform);book.name="SelectedBookmarkArt";book.transform.SetAsFirstSibling();nav.bookmarks[i]=book;
                var icon=b.transform.Find("Icon").GetComponent<Image>();icon.gameObject.SetActive(true);
                if(i==2){icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ShelterReferenceBuilder.Root+"/Sprites/Shelter.asset");icon.material=AssetDatabase.LoadAssetAtPath<Material>(ShelterReferenceBuilder.Root+"/ReferenceColour.mat");}
                nav.captions[i]=b.transform.Find("Caption").GetComponent<Text>();
                // Every page uses the same measured icon and caption baselines.
                var r=icon.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(-50,27)*ShelterReferenceBuilder.S; r.anchoredPosition=new Vector2(r.anchoredPosition.x,-r.anchoredPosition.y);r.sizeDelta=new Vector2(100,98)*ShelterReferenceBuilder.S;
                var c=nav.captions[i].rectTransform;c.anchorMin=c.anchorMax=new Vector2(.5f,1);c.pivot=new Vector2(0,1);c.anchoredPosition=new Vector2(-87.5f,-117)*ShelterReferenceBuilder.S;c.sizeDelta=new Vector2(175,50)*ShelterReferenceBuilder.S;
            }
            nav.Refresh();PrefabUtility.SaveAsPrefabAsset(go,Path);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static Material MaterialAsset(string name,Material source){string path="Assets/DarkFairytale/UI/Shared/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(source);m.name=name;AssetDatabase.CreateAsset(m,path);}return m;}
    static void RefineArt(){
        var root=PrefabUtility.LoadPrefabContents(Path);
        try{
            var nav=root.GetComponent<StorybookNavigation>();
            root.transform.Find("ReferenceFrame").GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ShelterReferenceBuilder.Root+"/Sprites/AttributeBorder.asset");
            var colour=MaterialAsset("CleanNavigationColour",AssetDatabase.LoadAssetAtPath<Material>(ShelterReferenceBuilder.Root+"/ReferenceColour.mat"));colour.SetFloat("_Cutoff",.48f);colour.SetFloat("_Feather",.06f);EditorUtility.SetDirty(colour);
            var tent=MaterialAsset("ShelterCutout",colour);tent.shader=Shader.Find("UI/Shared Navigation Art");tent.SetFloat("_RemoveRed",1);EditorUtility.SetDirty(tent);
            nav.normalFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");
            nav.selectedFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
            nav.shelterOnPaper=tent;nav.shelterOnRed=tent;nav.shelterIcon=nav.buttons[2].transform.Find("Icon").GetComponent<Image>();
            foreach(var b in nav.buttons){var icon=b.transform.Find("Icon").GetComponent<Image>();icon.material=b==nav.buttons[2]?tent:colour;}
            var mapIcon=nav.buttons[3].transform.Find("Icon").GetComponent<Image>();var mapArt=MaterialAsset("MapSilhouette",colour);mapArt.shader=Shader.Find("UI/Shared Navigation Art");mapArt.SetFloat("_MapShape",1);var rect=mapIcon.sprite.rect;var texture=mapIcon.sprite.texture;mapArt.SetVector("_SourceRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));EditorUtility.SetDirty(mapArt);mapIcon.material=mapArt;
            var bookmark=MaterialAsset("BookmarkFill",nav.bookmarks[0].GetComponent<Image>().material);bookmark.shader=Shader.Find("UI/Shared Navigation Art");bookmark.SetFloat("_Bookmark",1);EditorUtility.SetDirty(bookmark);foreach(var b in nav.bookmarks)b.GetComponent<Image>().material=bookmark;
            nav.Refresh();PrefabUtility.SaveAsPrefabAsset(root,Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static StorybookNavigation Instantiate(Transform parent,RectTransform page,int selected){
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),parent);instance.name="Navigation";
        var nav=instance.GetComponent<StorybookNavigation>();nav.pageRoot=page;nav.selectedIndex=selected;PrefabUtility.RecordPrefabInstancePropertyModifications(nav);return nav;
    }
    static void Migrate(string path){
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            if(root.GetComponentInChildren<StorybookNavigation>(true))return;
            var home=root.GetComponent<ShelterHomeUI>();var equipment=root.GetComponent<StorybookEquipmentUI>();var skills=root.GetComponent<StorybookSkillsUI>();var map=root.GetComponent<StorybookMapUI>();var shop=root.GetComponent<StorybookShopUI>();
            RectTransform old=home?home.GetComponent<ShelterResponsiveLayout>().navigation:equipment?equipment.layout.navigation:map?map.layout.navigation:root.GetComponent<StorybookSkillsLayout>().navigation;
            int order=old.GetSiblingIndex();var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),old.parent);instance.name="Navigation";instance.transform.SetSiblingIndex(order);
            var nav=instance.GetComponent<StorybookNavigation>();nav.pageRoot=(RectTransform)root.transform;nav.selectedIndex=home?2:equipment?0:skills?1:map?3:4;
            var r=(RectTransform)instance.transform;
            if(home){home.navigationButtons=nav.buttons;home.skillBadge=nav.skillBadge;home.GetComponent<ShelterResponsiveLayout>().navigation=r;}
            if(equipment){equipment.navigationButtons=nav.buttons;equipment.skillBadge=nav.skillBadge;equipment.layout.navigation=r;equipment.layout.navigationContent=null;}
            if(map){map.navigationButtons=nav.buttons;map.skillBadge=nav.skillBadge;map.layout.navigation=r;}
            if(skills)skills.navigation=nav.buttons;
            if(shop)shop.navigation=nav.buttons;
            if(skills||shop){var layout=root.GetComponent<StorybookSkillsLayout>();layout.navigation=r;layout.navigationContent=null;}
            UnityEngine.Object.DestroyImmediate(old.gameObject);nav.Apply();nav.Refresh();
            PrefabUtility.RecordPrefabInstancePropertyModifications(nav);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
