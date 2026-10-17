using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookBossHUDAuthoring
{
    public const string Path="Assets/DarkFairytale/UI/BossHUD/Prefabs/StorybookBossHUD.prefab";
    const string Hud="Assets/DarkFairytale/UI/CursedStorybook/Prefabs/CursedStorybookBattleHUD.prefab";
    static StorybookBossHUDAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("output/boss-hud/build.request"))return;File.Delete("output/boss-hud/build.request");try{Build();File.WriteAllText("output/boss-hud/build.txt","PASS");}catch(Exception e){File.WriteAllText("output/boss-hud/build.txt",e.ToString());}};}
    [MenuItem("Tools/Dark Fairytale/Repair Boss HUD Production Frames")]
    public static void RepairProductionFrames()
    {
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            if (ProductionUiArt.FinalizePrefab(root)) PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    static RectTransform Node(Transform parent,string name,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Image Pic(Transform p,string n,Sprite s,Vector2 pos,Vector2 size,Color c){var r=Node(p,n,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.color=c;im.raycastTarget=false;return im;}
    static Text Text(Transform p,string n,string value,Vector2 pos,Vector2 size,int fontSize,Color c){var r=Node(p,n,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Bold.otf");t.fontSize=fontSize;t.text=value;t.color=c;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
    static void Width(RectTransform r,float left,float right){r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(left,r.offsetMin.y);r.offsetMax=new Vector2(-right,r.offsetMax.y);}
    static Sprite Sprite(string p)=>AssetDatabase.LoadAssetAtPath<Sprite>(p);
    public static void Build(){
        foreach(var key in new[]{"MobRivenfang","MobStillmere"}){string path="Assets/DarkFairytale/UI/BossHUD/Textures/"+key+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(Path)){
            var root=Node(null,"StorybookBossHUD",Vector2.zero,new Vector2(1080,1920));root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var ui=root.gameObject.AddComponent<StorybookBossHUD>();ui.group=root.gameObject.AddComponent<CanvasGroup>();ui.group.alpha=0;ui.group.blocksRaycasts=ui.group.interactable=false;
            ui.panel=Node(root,"BossBanner",new Vector2(18,-260),new Vector2(1044,170));
            var frame=Sprite("Assets/DarkFairytale/UI/Equipment/Sprites/PanelFrameV3.asset");var paper=Sprite("Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset");
            var bg=Pic(ui.panel,"BloodPaper",paper,Vector2.zero,new Vector2(1044,170),new Color(.23f,.045f,.045f));bg.type=Image.Type.Tiled;Width(bg.rectTransform,0,0);
            var border=Pic(ui.panel,"BlackGoldThorns",frame,Vector2.zero,new Vector2(1044,170),Color.white);border.type=Image.Type.Sliced;border.fillCenter=false;Width(border.rectTransform,0,0);
            ui.portraitRoot=Node(ui.panel,"PortraitSeal",new Vector2(24,-25),new Vector2(120,120));
            Pic(ui.portraitRoot,"PortraitDarkPaper",paper,new Vector2(9,-9),new Vector2(102,102),new Color(.12f,.1f,.1f)).type=Image.Type.Tiled;
            var pf=Pic(ui.portraitRoot,"PortraitFrame",frame,new Vector2(4,-4),new Vector2(112,112),Color.white);pf.type=Image.Type.Sliced;pf.fillCenter=false;
            ui.portrait=Pic(ui.portraitRoot,"BossPortrait",null,new Vector2(0,6),new Vector2(120,120),Color.white);ui.portrait.preserveAspect=true;
            ui.eyebrow=Text(ui.panel,"BossLabel","首领降临",new Vector2(160,-14),new Vector2(220,25),20,new Color(.88f,.67f,.4f));
            ui.bossName=Text(ui.panel,"BossName","霜牙狼王",new Vector2(160,-40),new Vector2(600,48),36,new Color(1,.92f,.75f));Width(ui.bossName.rectTransform,160,170);
            ui.health=Text(ui.panel,"Health","100%",new Vector2(700,-51),new Vector2(300,32),24,new Color(1,.91f,.72f));ui.health.alignment=TextAnchor.MiddleRight;ui.health.rectTransform.anchorMin=ui.health.rectTransform.anchorMax=new Vector2(1,1);ui.health.rectTransform.pivot=new Vector2(1,1);ui.health.rectTransform.anchoredPosition=new Vector2(-34,-51);
            var track=Pic(ui.panel,"BloodTrack",paper,new Vector2(160,-100),new Vector2(806,38),new Color(.07f,.055f,.05f));track.type=Image.Type.Tiled;Width(track.rectTransform,160,34);ui.fillTrack=track.rectTransform;
            ui.fill=Pic(track.transform,"BloodCharge",Sprite("Assets/DarkFairytale/UI/CursedStorybook/Sprites/HealthInk.asset"),Vector2.zero,new Vector2(806,38),new Color(.88f,.19f,.12f));ui.fill.rectTransform.anchorMin=Vector2.zero;ui.fill.rectTransform.anchorMax=Vector2.one;ui.fill.rectTransform.offsetMin=ui.fill.rectTransform.offsetMax=Vector2.zero;ui.fill.type=Image.Type.Filled;ui.fill.fillMethod=Image.FillMethod.Horizontal;ui.fill.fillOrigin=0;
            var mf=Pic(track.transform,"MeterFrame",Sprite("Assets/DarkFairytale/UI/BattleRefresh/Sprites/MeterTransparent.asset"),new Vector2(-8,7),new Vector2(822,52),Color.white);mf.type=Image.Type.Sliced;mf.fillCenter=false;Width(mf.rectTransform,-8,-8);mf.pixelsPerUnitMultiplier=3;
            ui.chargeTip=Pic(track.transform,"ChargeLeadingLight",null,Vector2.zero,new Vector2(4,30),new Color(1,.86f,.48f)).rectTransform;ui.chargeTip.pivot=Vector2.one*.5f;
            var flash=Pic(track.transform,"FullChargeFlash",ui.fill.sprite,Vector2.zero,new Vector2(806,38),new Color(1,.85f,.46f));flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;flash.rectTransform.offsetMin=flash.rectTransform.offsetMax=Vector2.zero;ui.impact=flash.gameObject.AddComponent<CanvasGroup>();ui.impact.alpha=0;ui.impact.blocksRaycasts=false;
            ui.portraits=new[]{new StorybookBossHUD.PortraitEntry{key="MobRivenfang",displayName="霜牙狼王",portrait=Sprite("Assets/DarkFairytale/UI/BossHUD/Textures/MobRivenfang.png")},new StorybookBossHUD.PortraitEntry{key="MobStillmere",displayName="史莱姆之王",portrait=Sprite("Assets/DarkFairytale/UI/BossHUD/Textures/MobStillmere.png")}};
            ProductionUiArt.FinalizePrefab(root.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Path);UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
        var hud=PrefabUtility.LoadPrefabContents(Hud);try{var controller=hud.GetComponent<CursedBattleHud>();if(!controller.bossHud){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),hud.transform);controller.bossHud=go.GetComponent<StorybookBossHUD>();controller.bossHud.header=hud.GetComponent<BattleHudSafeArea>().core.Find("ChapterHeader") as RectTransform;PrefabUtility.SaveAsPrefabAsset(hud,Hud);}}finally{PrefabUtility.UnloadPrefabContents(hud);}
        foreach(var key in new[]{"MobRivenfang","MobStillmere"}){string path="Assets/DarkFairytale/Monsters/"+key+"/"+key+".prefab";var enemy=PrefabUtility.LoadPrefabContents(path);try{enemy.GetComponent<Actor_Base>().enemyLifebar.GetComponent<Canvas>().enabled=false;PrefabUtility.SaveAsPrefabAsset(enemy,path);}finally{PrefabUtility.UnloadPrefabContents(enemy);}}
        AssetDatabase.SaveAssets();
    }
}
