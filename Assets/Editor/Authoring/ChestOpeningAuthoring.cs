using System;
using System.IO;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// Explicit requests only: never rebuilds on import or play.
[InitializeOnLoad]
public static class ChestOpeningAuthoring
{
    const string Root="Assets/DarkFairytale/UI/Chest/Spine",Out="output/chest-opening/";
    static ChestOpeningAuthoring(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Out+"build.request"))return;
        File.Delete(Out+"build.request");try{if(File.Exists(Out+"layout.request")){File.Delete(Out+"layout.request");RepairLayout();}else if(File.Exists(Out+"effects.request")){File.Delete(Out+"effects.request");RepairEffects();}else if(File.Exists(Out+"repair.request")){File.Delete(Out+"repair.request");Repair();}else Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}
    };}
    static T Asset<T>(string name,Func<T> make)where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(Root+"/"+name);if(!a){a=make();AssetDatabase.CreateAsset(a,Root+"/"+name);}return a;}
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Pic(Transform parent,string name,float x,float y,float w,float h){var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.raycastTarget=false;return i;}
    static Text Txt(Transform p,string n,string value,float x,float y,float w,float h,Font font,int size){var t=Rect(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.12f,.09f,.06f);t.text=value;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Transform Find(Transform p,string name)=>p.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    static void RepairLayout(){
        var root=PrefabUtility.LoadPrefabContents(StorybookChestBuilder.Path);
        try{
            var opening=root.GetComponent<StorybookChestOpening>();var ui=root.GetComponent<StorybookChestUI>();var page=root.GetComponent<StorybookFusionLayout>().page;
            opening.skeleton.rectTransform.anchoredPosition=new Vector2(0,-330);opening.skeleton.rectTransform.localScale=Vector3.one*.9f;
            ((RectTransform)Find(page,"ChestGroundShadow")).anchoredPosition=new Vector2(0,-445);
            // Existing preview children use top-left coordinates.
            void Box(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
            var preview=opening.preview.transform;Box(preview,38,465,464,170);
            Box(Find(preview,"PreviewTitle"),35,5,394,30);
            Box(Find(preview,"CrystalIllustration"),58,55,65,78);Box(Find(preview,"EquipmentIllustration"),258,55,72,78);
            Box(Find(preview,"CrystalCaption"),125,76,100,36);Box(Find(preview,"EquipmentCaption"),327,76,124,36);
            Box(Find(preview,"InkDivider"),230,55,3,80);
            Box(ui.waiting.transform,35,660,470,80);Box(ui.timer.rectTransform,55,29,360,49);ui.timer.fontSize=38;
            Box(ui.paidActions.transform,35,760,470,127);Box(ui.advert.transform,0,72,470,55);
            Box(ui.freeActions.transform,35,760,470,127);Box(ui.ready.transform,70,680,400,55);
            ui.openingHiddenObjects=new[]{Find(page,"PageTitle").gameObject,ui.title.gameObject,ui.source.gameObject,Find(page,"WalletCrystal").gameObject,ui.balance.gameObject,ui.back.gameObject};
            Box(ui.notice.rectTransform,44,638,452,20);
            var summary=(RectTransform)opening.summary.transform;summary.anchoredPosition=new Vector2(0,-665);summary.sizeDelta=new Vector2(464,340);
            opening.rewardsScroll.viewport.anchoredPosition=new Vector2(0,-188);opening.rewardsScroll.viewport.sizeDelta=new Vector2(424,248);
            opening.featuredName.rectTransform.anchoredPosition=new Vector2(0,-470);
            ((RectTransform)opening.collect.transform).anchoredPosition=new Vector2(0,-884);
            PrefabUtility.SaveAsPrefabAsset(root,StorybookChestBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
    }
    static void RepairEffects(){
        var light=Asset("ChestInterior.mat",()=>new Material(Shader.Find("UI/Chest Interior Light")));
        light.SetFloat("_Mode",0);EditorUtility.SetDirty(light);
        var seam=Asset("ChestSeam.mat",()=>new Material(Shader.Find("UI/Chest Interior Light")));
        seam.SetFloat("_Mode",1);EditorUtility.SetDirty(seam);
        var root=PrefabUtility.LoadPrefabContents(StorybookChestBuilder.Path);
        try{
            var opening=root.GetComponent<StorybookChestOpening>();
            if(opening.rays)UnityEngine.Object.DestroyImmediate(opening.rays.gameObject);opening.rays=null;
            if(opening.flash)UnityEngine.Object.DestroyImmediate(opening.flash.gameObject);opening.flash=null;
            opening.glow.name="ChestInteriorLight";opening.glow.material=light;opening.glow.color=new Color(1,1,1,0);
            opening.seam.material=seam;opening.seam.color=new Color(1,1,1,0);
            foreach(var img in new[]{opening.glow,opening.seam}){
                var r=img.rectTransform;r.SetParent(opening.skeleton.transform,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,0);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(340,260);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
            }
            var sparks=(RectTransform)opening.motes[0].parent;sparks.name="ChestInteriorSparks";sparks.SetParent(opening.skeleton.transform,false);sparks.anchorMin=sparks.anchorMax=sparks.pivot=new Vector2(.5f,.5f);sparks.anchoredPosition=Vector2.zero;
            foreach(var m in opening.motes){m.sizeDelta=new Vector2(9,9);m.anchorMin=m.anchorMax=new Vector2(.5f,.5f);var img=m.GetComponent<Image>();img.color=new Color(1,.87f,.42f,0);}
            PrefabUtility.SaveAsPrefabAsset(root,StorybookChestBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
    }
    static void Repair(){
        var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Root+"/Chest_SkeletonData.asset");data.scale=.01f;data.Clear();EditorUtility.SetDirty(data);
        var root=PrefabUtility.LoadPrefabContents(StorybookChestBuilder.Path);
        try{var opening=root.GetComponent<StorybookChestOpening>();
            var oldGround=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="InkChestGround");if(oldGround)UnityEngine.Object.DestroyImmediate(oldGround.gameObject);
            var page=root.GetComponent<StorybookFusionLayout>().page;
            if(!page.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ChestGroundShadow")){
                var shadow=Pic(page,"ChestGroundShadow",0,473,300,24);shadow.material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/ChestGlow.mat");shadow.color=new Color(.13f,.08f,.02f,.5f);shadow.transform.SetSiblingIndex(opening.skeleton.transform.GetSiblingIndex());
            }
            var frame=Find(opening.summary.transform,"RewardFrame");foreach(var c in frame.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(c.gameObject);PrefabUtility.SaveAsPrefabAsset(root,StorybookChestBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
    }
    public static void Build(){
        var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/ChestParts.png");ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ChestParts.png");
        var mat=Asset("Chest_UI.mat",()=>new Material(Shader.Find("Spine/SkeletonGraphic")));mat.mainTexture=texture;mat.SetFloat("_StraightAlphaInput",1);mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(mat);
        var atlas=Asset("Chest_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Chest.atlas.txt");atlas.materials=new[]{mat};atlas.Clear();EditorUtility.SetDirty(atlas);
        var data=Asset("Chest_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Chest.json");data.atlasAssets=new AtlasAssetBase[]{atlas};data.scale=.01f;data.Clear();if(data.GetSkeletonData(false)==null)throw new Exception("Spine chest parse failed");EditorUtility.SetDirty(data);
        var glowMat=Asset("ChestGlow.mat",()=>new Material(Shader.Find("UI/Chest Opening Glow")));
        var root=PrefabUtility.LoadPrefabContents(StorybookChestBuilder.Path);
        try{
            var ui=root.GetComponent<StorybookChestUI>();if(ui.opening)throw new Exception("Opening nodes already authored; edit prefab instead of rebuilding.");
            var page=root.GetComponent<StorybookFusionLayout>().page;var font=ui.title.font;
            var opening=root.AddComponent<StorybookChestOpening>();ui.opening=opening;
            ui.chest.gameObject.SetActive(false);Find(page,"InkChestGround").gameObject.SetActive(false);
            var sg=Rect(page,"ChestSpine",0,348,370,330).gameObject.AddComponent<SkeletonGraphic>();sg.skeletonDataAsset=data;sg.material=mat;sg.initialSkinName="bronze";sg.startingAnimation="idle";sg.startingLoop=true;sg.unscaledTime=true;sg.raycastTarget=false;sg.Initialize(true);opening.skeleton=sg;
            opening.glow=Pic(page,"ChestGoldenLight",0,305,430,340);opening.glow.material=glowMat;opening.glow.color=new Color(1,.7f,.15f,0);opening.glow.transform.SetSiblingIndex(sg.transform.GetSiblingIndex());
            opening.flash=Pic(page,"BurstFlash",0,310,520,490);opening.flash.material=glowMat;opening.flash.color=new Color(1,.95f,.65f,0);
            opening.motes=new RectTransform[18];var motes=Rect(page,"BurstMotes",0,320,1,1);
            for(int i=0;i<opening.motes.Length;i++){var m=Pic(motes,"GoldMote_"+i,0,0,9,9);m.material=glowMat;m.color=new Color(1,.7f,.2f);m.gameObject.SetActive(false);opening.motes[i]=m.rectTransform;}
            opening.featured=Pic(page,"EmergingReward",0,300,105,105);opening.featured.preserveAspect=true;opening.featured.gameObject.SetActive(false);
            opening.featuredName=Txt(page,"EmergingRewardName","",0,455,452,40,font,23);opening.featuredName.resizeTextForBestFit=true;opening.featuredName.resizeTextMinSize=15;opening.featuredName.resizeTextMaxSize=23;
            opening.preview=Find(page,"RewardPreviewFrame").gameObject;
            var cr=Find(page,"CrystalIllustration").GetComponent<Image>();opening.crystal=cr.sprite;opening.crystalMaterial=cr.material;
            var summary=Rect(page,"EarnedRewards",0,666,464,330);opening.summary=summary.gameObject.AddComponent<CanvasGroup>();
            var frame=UnityEngine.Object.Instantiate(opening.preview.GetComponent<Image>(),summary);frame.name="RewardFrame";foreach(Transform ch in frame.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(ch.gameObject);var fr=frame.rectTransform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;frame.raycastTarget=false;
            opening.progress=Txt(summary,"RewardProgress","",0,25,430,34,font,23);
            var viewport=Rect(summary,"RewardViewport",0,184,424,244);viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,.001f);
            var content=Rect(viewport,"RewardRows",0,0,424,244);content.pivot=new Vector2(.5f,1);
            var scroll=summary.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;opening.rewardsScroll=scroll;
            var template=Rect(page,"RewardRowTemplate",0,0,424,58);var row=template.gameObject.AddComponent<ChestRewardRow>();row.group=template.gameObject.AddComponent<CanvasGroup>();template.pivot=new Vector2(.5f,1);
            var background=Pic(template,"Parchment",0,29,424,58);background.color=new Color(.65f,.51f,.30f,.12f);
            row.icon=Pic(template,"ItemIcon",-173,29,48,48);row.icon.preserveAspect=true;
            row.caption=Txt(template,"ItemName","",30,29,334,52,font,21);row.caption.alignment=TextAnchor.MiddleLeft;row.caption.resizeTextForBestFit=true;row.caption.resizeTextMinSize=15;row.caption.resizeTextMaxSize=21;
            string rp="Assets/DarkFairytale/UI/Chest/Prefabs/ChestRewardRow.prefab";PrefabUtility.SaveAsPrefabAsset(template.gameObject,rp);opening.rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(rp).GetComponent<ChestRewardRow>();UnityEngine.Object.DestroyImmediate(template.gameObject);
            var button=UnityEngine.Object.Instantiate(ui.free,page);button.name="CollectAllRewards";var br=(RectTransform)button.transform;br.anchorMin=br.anchorMax=new Vector2(.5f,1);br.pivot=Vector2.one*.5f;br.anchoredPosition=new Vector2(0,-884);br.sizeDelta=new Vector2(420,65);button.GetComponentInChildren<Text>().text="收下战利品";button.gameObject.SetActive(false);opening.collect=button;
            summary.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,StorybookChestBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
