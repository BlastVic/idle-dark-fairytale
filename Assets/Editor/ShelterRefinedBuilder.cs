using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit authoring command. Never runs merely because assets were imported or Play was entered.
[InitializeOnLoad]
public static class ShelterRefinedBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shelter/Refined";
    static Font font;
    static Color ink=new Color(.12f,.09f,.07f),ivory=new Color(.97f,.91f,.79f);
    static ShelterRefinedBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("output/home-refined/build.request"))return;File.Delete("output/home-refined/build.request");try{Build();File.WriteAllText("output/home-refined/build-result.txt","PASS");}catch(Exception e){File.WriteAllText("output/home-refined/build-result.txt",e.ToString());Debug.LogException(e);}}
    public static Texture2D Import(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Default;i.isReadable=true;i.mipmapEnabled=false;i.alphaIsTransparency=true;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=4096;i.wrapMode=TextureWrapMode.Clamp;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
    public static Rect Trim(Texture2D t,RectInt cell){var c=t.GetPixels32();int l=cell.xMax,b=cell.yMax,r=cell.x,y=cell.y;for(int j=cell.y;j<cell.yMax;j++)for(int x=cell.x;x<cell.xMax;x++)if(c[j*t.width+x].a>8){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,j);y=Math.Max(y,j);}return new Rect(l,b,Math.Max(1,r-l+1),Math.Max(1,y-b+1));}
    static Sprite[] Sheet(string file,int cols,int rows,Vector4 border){var t=Import(Root+"/"+file+".png");return Enumerable.Range(0,cols*rows).Select(n=>SpriteAsset(t,file+"_"+n,Trim(t,new RectInt(n%cols*t.width/cols,(rows-1-n/cols)*t.height/rows,t.width/cols,t.height/rows)),border)).ToArray();}
    static Sprite SpriteAsset(Texture2D t,string name,Rect rect,Vector4 border){string p=Root+"/Sprites/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(p);if(existing)return existing;var s=Sprite.Create(t,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);s.name=name;AssetDatabase.CreateAsset(s,p);return s;}
    public static RectTransform Node(Transform p,string name){var t=p.Find(name);if(t)return (RectTransform)t;var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(p,false);return (RectTransform)go.transform;}
    public static void Box(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void Right(RectTransform r,float right,float y,float w,float h){Box(r,-right,-0+y,w,h);r.anchorMin=r.anchorMax=new Vector2(1,1);}
    static void Center(RectTransform r,float x,float y,float w,float h){Box(r,x-w*.5f,y,w,h);r.anchorMin=r.anchorMax=new Vector2(.5f,1);}
    static void Stretch(RectTransform r,float l,float top,float right,float bottom){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(l,bottom);r.offsetMax=new Vector2(-right,-top);}
    static void Width(RectTransform r,float l,float y,float right,float h){r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(l,-y-h);r.offsetMax=new Vector2(-right,-y);}
    static Image Pic(Transform p,string name,Sprite s,float x,float y,float w,float h){var r=Node(p,name);Box(r,x,y,w,h);var im=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();im.sprite=s;im.color=Color.white;im.raycastTarget=false;im.type=Image.Type.Simple;return im;}
    static void Frame(Image im,Sprite s){im.sprite=s;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=1.4f;im.color=Color.white;}
    static void TextStyle(Text t,int size,Color? color=null){t.font=font;t.fontStyle=FontStyle.Normal;t.fontSize=size;t.resizeTextForBestFit=false;t.color=color??ink;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;}
    static void Rule(Transform p,string name,float x,float y,float w,float h){var im=Pic(p,name,null,x,y,w,h);im.color=new Color(.14f,.09f,.05f,.32f);}
    static void Fraction(RectTransform r,int i,int count,float top,float height){r.anchorMin=new Vector2((float)i/count,1);r.anchorMax=new Vector2((float)(i+1)/count,1);r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(8,-top-height);r.offsetMax=new Vector2(-8,-top);}
    [MenuItem("Tools/Dark Fairytale/UI/Apply Refined Shelter Design (Explicit)")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Sprites");AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Bold.otf");
        var panels=Sheet("Panels",1,4,new Vector4(48,42,48,42));var icons=Sheet("Icons",4,4,Vector4.zero);var controls=Sheet("Controls",2,2,Vector4.zero);
        var old=Import("Assets/DarkFairytale/UI/Shelter/ShelterIcons.png");var loot=new Sprite[5];for(int i=0;i<5;i++)loot[i]=SpriteAsset(old,"Loot_"+i,Trim(old,new RectInt(i%4*old.width/4,(3-i/4)*old.height/4,old.width/4,old.height/4)),Vector4.zero);
        var roomTex=Import(Root+"/RoomTall.png");var roomSprite=SpriteAsset(roomTex,"RoomTall",new Rect(0,0,roomTex.width,roomTex.height),Vector4.zero);
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
        try{
            var ui=home.GetComponent<ShelterHomeUI>();foreach(var t in home.GetComponentsInChildren<Text>(true))TextStyle(t,t.fontSize,t.color);
            var safe=home.GetComponent<BattleHudSafeArea>();safe.enabled=false;
            var layout=home.GetComponent<ShelterResponsiveLayout>()??home.AddComponent<ShelterResponsiveLayout>();
            var core=(RectTransform)(home.transform.Find("Core_9x16")??home.transform.Find("SupportedViewport/Core_9x16"));
            var viewport=Node(home.transform,"SupportedViewport");if(!viewport.GetComponent<RectMask2D>())viewport.gameObject.AddComponent<RectMask2D>();
            var room=(RectTransform)(core.Find("ShelterIllustration")??viewport.Find("ShelterIllustration"));room.SetParent(viewport,false);room.SetAsFirstSibling();room.GetComponent<Image>().sprite=roomSprite;
            core.SetParent(viewport,false);var mask=core.GetComponent<RectMask2D>();if(mask)UnityEngine.Object.DestroyImmediate(mask);
            var lower=core.Find("LowerBackdrop");if(lower)lower.gameObject.SetActive(false);
            home.transform.Find("FullScreenBackdrop").GetComponent<Image>().color=Color.black;
            layout.viewport=viewport;layout.core=core;layout.room=room;layout.top=(RectTransform)core.Find("TopStatus");layout.chests=(RectTransform)core.Find("ChestRail");layout.attributes=ui.attributePanel;layout.battle=(RectTransform)ui.battleButton.transform;layout.navigation=(RectTransform)core.Find("Navigation");layout.functions=ui.functionButtons.Select(b=>(RectTransform)b.transform).ToArray();
            Frame(layout.top.GetComponent<Image>(),panels[0]);Frame(layout.chests.GetComponent<Image>(),panels[1]);Frame(ui.attributePanel.GetComponent<Image>(),panels[0]);Frame(layout.navigation.GetComponent<Image>(),panels[3]);
            var top=layout.top;
            Box((RectTransform)top.Find("Portrait"),25,17,118,125);Box(ui.numberText.rectTransform,20,141,140,33);TextStyle(ui.numberText,24,new Color(.33f,.28f,.21f));
            Box(ui.levelText.rectTransform,157,51,184,77);TextStyle(ui.levelText,49);
            Width((RectTransform)top.Find("HealthFrame"),350,43,436,44);Width(ui.hpFill.rectTransform,361,52,447,26);Width(ui.hpText.rectTransform,350,44,436,42);TextStyle(ui.hpText,27,ivory);
            Width((RectTransform)top.Find("ExperienceFrame"),350,104,436,36);Width(ui.xpFill.rectTransform,361,113,447,18);Width(ui.xpText.rectTransform,350,102,436,38);TextStyle(ui.xpText,25,ivory);
            var topIcons=top.GetComponentsInChildren<Image>().Where(im=>im.name=="Icon"&&im.transform.parent==top).ToArray();
            topIcons[0].sprite=loot[3];Right(topIcons[0].rectTransform,413,53,43,66);topIcons[1].sprite=loot[4];Right(topIcons[1].rectTransform,254,59,51,51);
            Right(ui.crystalsText.rectTransform,365,52,95,72);TextStyle(ui.crystalsText,30);Right(ui.cherriesText.rectTransform,199,52,87,72);TextStyle(ui.cherriesText,30);
            Right((RectTransform)ui.settingsButton.transform,102,40,88,94);ui.settingsButton.GetComponent<Image>().color=new Color(1,1,1,.01f);var gear=ui.settingsButton.transform.Find("Icon").GetComponent<Image>();gear.sprite=icons[9];Box(gear.rectTransform,14,17,60,60);
            for(int i=0;i<3;i++){var r=Pic(top,"TopRule_"+i,null,0,43,1.5f,88);r.color=new Color(.17f,.11f,.05f,.3f);Right(r.rectTransform,new[]{427,264,108}[i],43,1.5f,88);}
            ui.chestSprites=new[]{loot[0],loot[1],loot[2]};ui.chestStatusPlates=new Image[4];
            for(int i=0;i<4;i++){
                var b=ui.chestButtons[i];Fraction((RectTransform)b.transform,i,4,8,147);b.GetComponent<Image>().sprite=null;b.GetComponent<Image>().color=new Color(1,1,1,.01f);
                Center(ui.chestImages[i].rectTransform,0,5,153,103);ui.chestImages[i].sprite=loot[Math.Min(i,2)];
                var plate=Pic(b.transform,"StatusRibbon",panels[2],0,115,198,29);Center(plate.rectTransform,0,115,198,29);plate.type=Image.Type.Sliced;plate.pixelsPerUnitMultiplier=4;plate.transform.SetSiblingIndex(1);ui.chestStatusPlates[i]=plate;
                Center(ui.chestLabels[i].rectTransform,0,113,210,32);TextStyle(ui.chestLabels[i],25);var wax=ui.chestReady[i].GetComponent<Image>();wax.sprite=controls[3];Center(wax.rectTransform,76,6,39,48);
                if(i<3){var rule=Pic(b.transform,"Divider",null,0,26,1.5f,81);rule.color=new Color(.17f,.11f,.05f,.28f);Right(rule.rectTransform,0,26,1.5f,81);}
            }
            for(int i=0;i<3;i++){var b=ui.functionButtons[i];var im=b.GetComponent<Image>();im.sprite=controls[0];im.type=Image.Type.Simple;im.color=Color.white;var icon=b.transform.Find("Icon").GetComponent<Image>();icon.sprite=icons[6+i];Box(icon.rectTransform,31,20,80,70);var caption=b.transform.Find("Caption").GetComponent<Text>();Box(caption.rectTransform,9,94,124,34);TextStyle(caption,27);}
            Box((RectTransform)ui.rebirthLock.transform,111,8,25,25);
            ui.expandedHeight=300;ui.collapsedHeight=76;ui.attributePanel.sizeDelta=new Vector2(1052,300);
            var title=ui.attributePanel.Find("Title").GetComponent<Text>();Box(title.rectTransform,36,2,145,72);TextStyle(title,43);
            Right(ui.pointsText.rectTransform,570,3,435,70);TextStyle(ui.pointsText,29);ui.pointsText.alignment=TextAnchor.MiddleRight;
            Right((RectTransform)ui.collapseButton.transform,114,8,90,60);ui.collapseText.color=Color.clear;
            ui.collapseArrow=Node(ui.collapseButton.transform,"Chevron");Box(ui.collapseArrow,26,20,38,22);ui.collapseArrow.pivot=new Vector2(.5f,.5f);ui.collapseArrow.anchoredPosition+=new Vector2(19,-11);
            for(int i=0;i<2;i++){var line=Pic(ui.collapseArrow,"Stroke"+i,null,i*15,7,24,4);line.color=ink;line.rectTransform.localEulerAngles=new Vector3(0,0,i==0?-32:32);}
            var body=(RectTransform)ui.attributeBody.transform;Width(body,24,82,24,194);
            foreach(var t in body.GetComponentsInChildren<Image>().Where(im=>im.name=="InkRule").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            for(int i=0;i<6;i++){
                var cell=Node(body,"Cell_"+i);Fraction(cell,i%3,3,i/3*96,94);
                var label=body.Find("StatLabel_"+i)??cell.Find("StatLabel_"+i);label.SetParent(cell,false);Box((RectTransform)label,76,13,75,65);TextStyle(label.GetComponent<Text>(),29);
                ui.statValues[i].transform.SetParent(cell,false);Right(ui.statValues[i].rectTransform,151,13,79,65);TextStyle(ui.statValues[i],30);
                ui.statButtons[i].transform.SetParent(cell,false);Right((RectTransform)ui.statButtons[i].transform,63,21,54,54);var im=ui.statButtons[i].GetComponent<Image>();im.sprite=controls[1];im.type=Image.Type.Simple;im.color=Color.white;var outline=im.GetComponent<Outline>();if(outline)UnityEngine.Object.DestroyImmediate(outline);var plus=ui.statButtons[i].GetComponentInChildren<Text>();Stretch(plus.rectTransform,0,0,0,4);TextStyle(plus,41);
                var statIcon=Pic(cell,"StatIcon",icons[i],13,25,51,45);statIcon.preserveAspect=true;
                if(i<3){var rule=Pic(cell,"RowRule",null,0,94,200,1.5f);Width(rule.rectTransform,0,94,0,1.5f);rule.color=new Color(.17f,.11f,.05f,.27f);}
                if(i%3!=2){var rule=Pic(cell,"ColumnRule",null,0,4,1.5f,86);Right(rule.rectTransform,-7,4,1.5f,86);rule.color=new Color(.17f,.11f,.05f,.27f);}
            }
            Frame(ui.battleButton.GetComponent<Image>(),panels[2]);ui.battleButton.GetComponent<Image>().pixelsPerUnitMultiplier=1;
            var bi=ui.battleButton.GetComponentsInChildren<Image>().Where(im=>im.name=="Icon").ToArray();bi[0].sprite=icons[15];Center(bi[0].rectTransform,-292,15,102,98);bi[1].sprite=controls[3];Right(bi[1].rectTransform,118,-18,130,171);
            var bt=ui.battleButton.transform.Find("Caption").GetComponent<Text>();Center(bt.rectTransform,0,12,600,100);TextStyle(bt,62,ivory);
            for(int i=0;i<5;i++){
                var b=ui.navigationButtons[i];Fraction((RectTransform)b.transform,i,5,9,202);var im=b.GetComponent<Image>();im.sprite=null;im.color=new Color(1,1,1,.01f);
                if(i==2){var bookmark=Pic(b.transform,"SelectedBookmark",controls[2],0,-28,183,249);Center(bookmark.rectTransform,0,-28,183,249);bookmark.transform.SetAsFirstSibling();}
                var icon=b.transform.Find("Icon").GetComponent<Image>();icon.sprite=icons[10+i];Center(icon.rectTransform,0,23,108,111);icon.preserveAspect=true;
                var caption=b.transform.Find("Caption").GetComponent<Text>();Center(caption.rectTransform,0,150,195,46);TextStyle(caption,31,i==2?ivory:ink);
                if(i<4&&i!=2){var rule=Pic(b.transform,"NavDivider",null,0,30,1.5f,133);Right(rule.rectTransform,-6,30,1.5f,133);rule.color=new Color(.17f,.11f,.05f,.27f);}
                if(i==1){var badge=(RectTransform)ui.skillBadge.transform.parent;Center(badge,57,2,40,40);badge.GetComponent<Image>().sprite=controls[3];TextStyle(ui.skillBadge,22,ivory);}
            }
            var modal=(RectTransform)ui.modal.transform;Stretch(modal,0,0,0,0);Stretch((RectTransform)modal.Find("InputBlocker"),0,0,0,0);
            var paper=(RectTransform)modal.Find("DetailsPaper");paper.anchorMin=paper.anchorMax=paper.pivot=new Vector2(.5f,.5f);paper.anchoredPosition=Vector2.zero;Frame(paper.GetComponent<Image>(),panels[0]);
            layout.Apply();PrefabUtility.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(home);}AssetDatabase.SaveAssets();
    }
}
