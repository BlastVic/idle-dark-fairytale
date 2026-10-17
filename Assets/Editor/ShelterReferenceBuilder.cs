using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring from measured approved artwork. No import/play-time prefab rebuilding.</summary>
[InitializeOnLoad]
public static class ShelterReferenceBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shelter/Reference";
    public const float S=1080f/941;
    static Texture2D source;static Font chinese,latin;static Material ink,colour;static Sprite paper,redPaper;
    static ShelterReferenceBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("output/home-fidelity/build.request"))return;File.Delete("output/home-fidelity/build.request");try{Build();File.WriteAllText("output/home-fidelity/build-result.txt","PASS");}catch(Exception e){File.WriteAllText("output/home-fidelity/build-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string path,Func<T> create)where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(!a){a=create();AssetDatabase.CreateAsset(a,path);}return a;}
    // The source texture is unchanged. Rects and meshes select its original pixels.
    static Sprite Slice(string name,float x,float y,float w,float h,Vector4 border=default){return Asset(Root+"/Sprites/"+name+".asset",()=>{var s=ProductionUiArt.Create(source,new Rect(x,source.height-y-h,w,h),new Vector2(.5f,.5f),100/S,0,SpriteMeshType.FullRect,border);s.name=name;return s;});}
    static Sprite SkillBook(){var s=Slice("Skills",238,1473,95,100);TrimSkillBook(s);return s;}
    public static void TrimSkillBook(Sprite s){
        // Source-space silhouette excludes the adjacent badge, retaining the original book pixels.
        Vector2[] points={new Vector2(0,0),new Vector2(73,0),new Vector2(85,42),new Vector2(95,42),new Vector2(95,100),new Vector2(0,100)};
        s.OverrideGeometry(points.Select(p=>new Vector2(p.x-47.5f,50-p.y)/s.pixelsPerUnit).ToArray(),new ushort[]{5,0,1,5,1,2,5,2,3,5,3,4});EditorUtility.SetDirty(s);
    }
    static Sprite Ring(string name,float x,float y,float w,float h,float inner){var s=Slice(name,x,y,w,h);int n=64;var vertices=new Vector2[n*2];var tris=new ushort[n*6];for(int i=0;i<n;i++){float a=(float)i/n*Mathf.PI*2;var d=new Vector2(Mathf.Cos(a)*w,Mathf.Sin(a)*h)/(100/S);vertices[i]=d*.492f;vertices[n+i]=d*inner;int j=(i+1)%n;int k=i*6;tris[k]=(ushort)i;tris[k+1]=(ushort)j;tris[k+2]=(ushort)(n+i);tris[k+3]=(ushort)j;tris[k+4]=(ushort)(n+j);tris[k+5]=(ushort)(n+i);}s.OverrideGeometry(vertices,tris);EditorUtility.SetDirty(s);return s;}
    static RectTransform Node(Transform p,string n)=>ShelterRefinedBuilder.Node(p,n);
    static void Box(RectTransform r,float x,float y,float w,float h)=>ShelterRefinedBuilder.Box(r,x*S,y*S,w*S,h*S);
    static void Center(RectTransform r,float x,float y,float w,float h){Box(r,x-w*.5f,y,w,h);r.anchorMin=r.anchorMax=new Vector2(.5f,1);}
    static void Right(RectTransform r,float x,float y,float w,float h){Box(r,-x,y,w,h);r.anchorMin=r.anchorMax=new Vector2(1,1);}
    static void Full(RectTransform r,float l=0,float t=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l*S,bottom*S);r.offsetMax=new Vector2(-right*S,-t*S);}
    static void Width(RectTransform r,float l,float y,float right,float h){r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(l*S,(-y-h)*S);r.offsetMax=new Vector2(-right*S,-y*S);}
    static void Fraction(RectTransform r,int i,int count,float top,float h){r.anchorMin=new Vector2((float)i/count,1);r.anchorMax=new Vector2((float)(i+1)/count,1);r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(0,(-top-h)*S);r.offsetMax=new Vector2(0,-top*S);}
    static Image Pic(Transform p,string n,Sprite sprite,Material mat=null){var r=Node(p,n);r.gameObject.SetActive(true);var im=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();im.sprite=sprite;im.material=mat;im.type=Image.Type.Simple;im.color=Color.white;im.raycastTarget=false;im.preserveAspect=false;im.useSpriteMesh=true;im.fillCenter=true;im.pixelsPerUnitMultiplier=1;return im;}
    static void Style(Text t,float size,bool numbers=false,Color? c=null){t.font=numbers?latin:chinese;t.fontStyle=numbers?FontStyle.Bold:FontStyle.Normal;t.fontSize=Mathf.RoundToInt(size*S);t.color=c??Color.black;t.resizeTextForBestFit=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.alignment=TextAnchor.MiddleCenter;foreach(var edge in t.GetComponents<Shadow>())UnityEngine.Object.DestroyImmediate(edge);}
    static Material Surface(string key,Sprite sprite,float mode,float w,float h,float inner=0){var m=Asset(Root+"/Surface_"+key+".mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));var r=sprite.rect;var t=sprite.texture;m.SetFloat("_Mode",mode);m.SetFloat("_Inner",inner);m.SetVector("_SourceRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));m.SetVector("_Tiles",new Vector4(w/r.width,h/r.height,0,0));EditorUtility.SetDirty(m);return m;}
    static void Paper(Image im,Sprite fill,float w,float h,string key,bool circle=false){im.sprite=fill;im.type=Image.Type.Simple;im.useSpriteMesh=false;im.material=Surface(key,fill,circle?3:2,w,h);}
    static void Panel(RectTransform r,Sprite frame,Sprite fill=null){var im=r.GetComponent<Image>();Paper(im,fill??paper,923,r.sizeDelta.y/S,r.name);im.color=Color.white;var rim=Pic(r,"ReferenceFrame",frame);Full(rim.rectTransform);rim.type=Image.Type.Sliced;rim.fillCenter=false;rim.transform.SetAsFirstSibling();}
    static void Disable(Transform p,string n){var t=p.Find(n);if(t)t.gameObject.SetActive(false);}
    static void Line(Transform p,string n,float y,float left,float right){var im=Pic(p,n,null);im.color=new Color(.10f,.075f,.04f,.65f);Width(im.rectTransform,left,y,right,.7f);}
    static void Cross(Transform p,string n,float x,float y){var im=Pic(p,n,Slice("RuleCross",316,1085,19,18),ink);Box(im.rectTransform,x-9.5f,y-9,19,18);}
    [MenuItem("Tools/Dark Fairytale/UI/Apply Measured Reference Fidelity (Explicit)")]
    public static void Build(){
        Directory.CreateDirectory(Root+"/Sprites");source=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Shelter/Reference/ApprovedHome.png");
        chinese=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");latin=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/Tinos-Bold.ttf");if(!latin)latin=chinese;
        ink=Asset(Root+"/ReferenceInk.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));ink.SetFloat("_InkOnly",0);ink.SetFloat("_Cutoff",.40f);ink.SetFloat("_Feather",.17f);EditorUtility.SetDirty(ink);
        colour=Asset(Root+"/ReferenceColour.mat",()=>new Material(Shader.Find("UI/Reference Ink Cutout")));colour.SetFloat("_InkOnly",0);colour.SetFloat("_Cutoff",.58f);colour.SetFloat("_Feather",.16f);EditorUtility.SetDirty(colour);
        paper=Slice("BlankPaper",165,1032,260,54);redPaper=Slice("BlankRedV2",48,29,37,37);
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
        try{
            var ui=home.GetComponent<ShelterHomeUI>();var l=home.GetComponent<ShelterResponsiveLayout>();var core=l.core;
            l.margin=8*S;l.rightMargin=10*S;l.topInset=22*S;l.topHeight=162*S;l.chestTop=189*S;l.chestHeight=144*S;l.navHeight=190*S;l.bottomInset=34*S;
            l.attributeBottom=(1672-1279)*S;l.battleBottom=(1672-1417)*S;l.battleHeight=126*S;l.battleSide=59*S;
            l.functionTop=375*S;l.functionGap=135*S;l.functionSize=116*S;l.functionHeight=124*S;l.functionRight=37*S;
            ui.expandedHeight=262*S;ui.collapsedHeight=77*S;l.roomSize=new Vector2(1080,1920);l.feetInArtwork=1091;l.feetClearance=75;l.wideFeetClearance=20;
            var rt=ShelterRefinedBuilder.Import(Root+"/RoomReference.png");var roomSprite=Asset(Root+"/Sprites/RoomReference.asset",()=>ProductionUiArt.Create(rt,new Rect(0,0,rt.width,rt.height),new Vector2(.5f,1),100));l.room.GetComponent<Image>().sprite=roomSprite;
            var actor=(RectTransform)l.room.Find("RedHoodShelter");actor.localScale=Vector3.one*.92f;actor.anchoredPosition=new Vector2(426,-1110);
            var fire=(RectTransform)l.room.Find("Hearth");fire.localScale=Vector3.one*.85f;fire.anchoredPosition=new Vector2(115,-923);
            var outer=Pic(core,"BookOuterFrame",Slice("OuterFrameV2",0,0,941,1672,new Vector4(10,17,10,9)));Full(outer.rectTransform);outer.type=Image.Type.Sliced;outer.fillCenter=false;outer.transform.SetAsFirstSibling();
            Panel(l.top,Slice("HeaderBorder",8,22,923,162,new Vector4(24,17,24,17)));
            var top=l.top;var portrait=top.Find("Portrait").GetComponent<Image>();portrait.sprite=Slice("Portrait",105,51,101,100);portrait.material=null;Box(portrait.rectTransform,19,29,101,100);
            Box(ui.numberText.rectTransform,13,127,114,30);Style(ui.numberText,23,true);
            Box(ui.levelText.rectTransform,132,54,196,74);Style(ui.levelText,50,true);
            var hp=top.Find("HealthFrame").GetComponent<Image>();hp.sprite=Slice("HealthFrame",353,75,225,40,new Vector4(17,7,17,7));hp.type=Image.Type.Sliced;hp.fillCenter=false;hp.pixelsPerUnitMultiplier=1;hp.color=Color.white;hp.material=Surface("HealthOutline",hp.sprite,4,225,40);Width(hp.rectTransform,345,53,353,40);
            ui.hpFill.sprite=Slice("HealthFill",373,84,42,23);ui.hpFill.material=Surface("HealthFill",ui.hpFill.sprite,5,213,28);ui.hpFill.color=Color.white;Width(ui.hpFill.rectTransform,351,59,359,28);ui.hpFill.transform.SetSiblingIndex(hp.transform.GetSiblingIndex());var hpDark=Pic(top,"HealthDark",Slice("ExperienceDark",408,124,62,22));Width(hpDark.rectTransform,351,59,359,28);hpDark.material=Surface("HealthDark",hpDark.sprite,5,213,28);hpDark.transform.SetSiblingIndex(ui.hpFill.transform.GetSiblingIndex());Width(ui.hpText.rectTransform,345,51,353,40);ui.hpText.rectTransform.anchoredPosition+=new Vector2(7,-2)*S;Style(ui.hpText,31,true,new Color(.98f,.94f,.84f));
            var xp=top.Find("ExperienceFrame").GetComponent<Image>();xp.sprite=Slice("ExperienceFrame",353,116,225,37,new Vector4(16,6,16,6));xp.type=Image.Type.Sliced;xp.fillCenter=false;xp.pixelsPerUnitMultiplier=1;xp.material=Surface("ExperienceOutline",xp.sprite,4,225,37);Width(xp.rectTransform,345,94,353,37);
            var xpBack=Pic(top,"ExperienceDark",Slice("ExperienceDark",408,124,62,22));Width(xpBack.rectTransform,351,100,359,25);xpBack.material=Surface("ExperienceDark",xpBack.sprite,5,213,25);xpBack.transform.SetSiblingIndex(xp.transform.GetSiblingIndex());
            ui.xpFill.sprite=Slice("ExperienceBlue",363,123,16,23);ui.xpFill.material=Surface("ExperienceBlue",ui.xpFill.sprite,5,213,25);ui.xpFill.color=Color.white;Width(ui.xpFill.rectTransform,351,100,359,25);Width(ui.xpText.rectTransform,349,95,365,33);Style(ui.xpText,26,true,new Color(.98f,.94f,.84f));ui.xpText.alignment=TextAnchor.MiddleRight;hpDark.transform.SetAsLastSibling();ui.hpFill.transform.SetAsLastSibling();hp.transform.SetAsLastSibling();ui.hpText.transform.SetAsLastSibling();xpBack.transform.SetAsLastSibling();ui.xpFill.transform.SetAsLastSibling();xp.transform.SetAsLastSibling();ui.xpText.transform.SetAsLastSibling();
            var currencies=top.GetComponentsInChildren<Image>().Where(im=>im.name=="Icon"&&im.transform.parent==top).ToArray();currencies[0].sprite=Slice("Crystal",600,81,39,56);currencies[0].material=colour;Right(currencies[0].rectTransform,332,59,39,56);currencies[1].sprite=Slice("Cherries",719,80,47,53);currencies[1].material=colour;Right(currencies[1].rectTransform,213,58,47,53);
            Right(ui.crystalsText.rectTransform,292,64,79,47);Style(ui.crystalsText,27,true);ui.crystalsText.alignment=TextAnchor.MiddleLeft;Right(ui.cherriesText.rectTransform,160,64,53,47);Style(ui.cherriesText,27,true);ui.cherriesText.alignment=TextAnchor.MiddleLeft;
            Right((RectTransform)ui.settingsButton.transform,105,29,89,111);var gear=ui.settingsButton.transform.Find("Icon").GetComponent<Image>();gear.sprite=Slice("Settings",841,80,58,57);gear.material=ink;Box(gear.rectTransform,17,29,58,57);ui.settingsButton.GetComponent<Image>().color=Color.clear;
            for(int i=0;i<3;i++)Disable(top,"TopRule_"+i);
            var vertical=Slice("HeaderSeparator",582,68,14,89);for(int i=0;i<3;i++){var im=Pic(top,"ExactSeparator_"+i,vertical,ink);Right(im.rectTransform,new[]{349,230,108}[i],46,14,89);}
            Disable(top,"ExactSeparator_2");var ornateSettings=Pic(top,"SettingsDivider",Slice("SettingsDivider",814,29,26,146),ink);Right(ornateSettings.rectTransform,117,7,26,146);
            var portraitRule=Pic(top,"PortraitDivider",Slice("PortraitDividerV2",207,32,14,147),ink);Box(portraitRule.rectTransform,122,10,14,147);
            Panel(l.chests,Slice("ChestBorder",8,189,923,144,new Vector4(24,15,24,15)));
            var slots=Node(l.chests,"ReferenceSlots");Full(slots,22,0,21,0);ui.chestSprites=new[]{Slice("ChestBronze",75,207,134,74),Slice("ChestGold",290,206,141,75),Slice("ChestBlack",513,206,133,75)};
            ui.chestReadyPlate=Slice("ReadyPlate",61,275,160,38,new Vector4(18,7,18,7));ui.chestTimerPlate=Slice("TimerPlate",282,280,154,31,new Vector4(12,5,12,5));ui.chestEmptyFrames=new GameObject[4];ui.chestReadyInterior=redPaper;ui.chestTimerInterior=Slice("TimerInterior",300,286,14,16);ui.chestReadyFillMaterial=Surface("ChestReady",redPaper,2,132,24);ui.chestTimerFillMaterial=Surface("ChestTimer",ui.chestTimerInterior,2,126,17);ui.chestReadyPlateSize=new Vector2(160,38)*S;ui.chestTimerPlateSize=new Vector2(154,31)*S;ui.chestReadyPlateTop=86*S;ui.chestTimerPlateTop=91*S;
            for(int i=0;i<4;i++){
                var b=ui.chestButtons[i];b.transform.SetParent(slots,false);Fraction((RectTransform)b.transform,i,4,0,140);b.GetComponent<Image>().color=new Color(1,1,1,.001f);Disable(b.transform,"Divider");
                var ci=ui.chestImages[i];ci.material=colour;ci.sprite=ui.chestSprites[Math.Min(i,2)];Center(ci.rectTransform,0,17,141,75);ci.preserveAspect=true;
                var plate=ui.chestStatusPlates[i];plate.sprite=ui.chestReadyPlate;plate.type=Image.Type.Sliced;plate.fillCenter=false;plate.pixelsPerUnitMultiplier=1;plate.color=Color.white;Center(plate.rectTransform,0,86,160,38);
                var back=Pic(plate.transform,"PlainPlateInterior",redPaper);Full(back.rectTransform,14,7,14,7);Paper(back,redPaper,132,24,"ChestReady");back.color=Color.white;back.transform.SetAsFirstSibling();
                Center(ui.chestLabels[i].rectTransform,0,87,155,36);Style(ui.chestLabels[i],23,false,new Color(.98f,.94f,.84f));ui.chestLabels[i].font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Bold.otf");
                var dot=ui.chestReady[i].GetComponent<Image>();dot.sprite=Ring("ReadyDot",188,198,36,36,0);dot.useSpriteMesh=false;dot.material=Surface("ReadyDot",dot.sprite,1,36,36);Center(dot.rectTransform,68,8,36,36);
                var empty=Node(b.transform,"EmptySlot");Full(empty);ui.chestEmptyFrames[i]=empty.gameObject;var outline=Pic(empty,"DashedOutline",Slice("EmptyDashedBorder",710,207,192,109,new Vector4(3,3,3,3)));Center(outline.rectTransform,0,18,192,109);outline.type=Image.Type.Sliced;outline.fillCenter=false;
                var emptyIcon=Pic(empty,"EmptyChest",Slice("EmptyChest",758,218,95,62),ink);Center(emptyIcon.rectTransform,0,29,95,62);emptyIcon.material=null;emptyIcon.color=Color.white;empty.SetAsFirstSibling();
                if(i<3){var divider=Pic(b.transform,"InkDivider",Slice("ChestDivider",239,196,20,124),ink);Right(divider.rectTransform,10,6,20,124);}
            }
            for(int i=0;i<3;i++){
                var b=ui.functionButtons[i];var im=b.GetComponent<Image>();im.sprite=Ring("UtilityRing"+i,788,375+i*135,116,124,.422f);im.material=Surface("UtilityRing"+i,im.sprite,1,116,124,.437f);im.type=Image.Type.Simple;im.useSpriteMesh=false;
                var fill=Pic(b.transform,"ReferenceCirclePaper",Ring("CirclePaper",165,1032,128,54,0));Center(fill.rectTransform,0,8,104,111);Paper(fill,paper,104,111,"CircleFill",true);fill.transform.SetAsFirstSibling();
                var icon=b.transform.Find("Icon").GetComponent<Image>();icon.sprite=i==0?Slice("Fusion",815,394,66,61):i==1?Slice("ExperienceV2",815,533,66,54):Slice("RebirthV2",815,660,42,63);icon.material=ink;var ir=icon.sprite.rect;Box(icon.rectTransform,ir.x-788,source.height-ir.yMax-(375+i*135),ir.width,ir.height);icon.preserveAspect=true;icon.gameObject.SetActive(true);
                var caption=b.transform.Find("Caption").GetComponent<Text>();Box(caption.rectTransform,12,80,96,34);Style(caption,24);
            }
            ui.rebirthLock.GetComponent<Text>().color=Color.clear;var lockImage=Pic(ui.rebirthLock.transform,"LockGlyph",Slice("Lock",856,687,25,33),ink);Box(lockImage.rectTransform,0,0,25,33);Box((RectTransform)ui.rebirthLock.transform,68,42,25,33);
            Panel(ui.attributePanel,Slice("AttributeBorder",8,1017,923,262,new Vector4(28,20,28,17)));
            var title=ui.attributePanel.Find("Title").GetComponent<Text>();Box(title.rectTransform,41,10,95,60);Style(title,43);title.rectTransform.anchoredPosition+=Vector2.down*2*S;
            Right(ui.pointsText.rectTransform,481,7,370,64);Style(ui.pointsText,26);ui.pointsText.alignment=TextAnchor.MiddleLeft;
            Right((RectTransform)ui.collapseButton.transform,99,23,61,45);ui.collapseButton.GetComponent<Image>().color=Color.clear;foreach(Transform child in ui.collapseArrow)child.gameObject.SetActive(false);var arrow=Pic(ui.collapseArrow,"ReferenceArrow",Slice("FoldChevron",843,1050,38,28),ink);Full(arrow.rectTransform);Box(ui.collapseArrow,15,7,32,23);ui.collapseArrow.pivot=new Vector2(.5f,.5f);ui.collapseArrow.anchoredPosition+=new Vector2(16*S,-11.5f*S);
            var body=(RectTransform)ui.attributeBody.transform;Width(body,25,76,24,174);var rowMask=body.GetComponent<RectMask2D>();if(rowMask)UnityEngine.Object.DestroyImmediate(rowMask);
            var statSprites=new[]{Slice("Heart",44,1109,57,60),Slice("Swords",341,1106,58,62),Slice("Shield",635,1107,56,61),Slice("Speed",45,1190,56,63),Slice("Critical",338,1187,62,67),Slice("CriticalDamage",630,1187,68,66)};
            for(int i=0;i<6;i++){
                var cell=(RectTransform)body.Find("Cell_"+i);Fraction(cell,i%3,3,i/3*82,82);Disable(cell,"RowRule");Disable(cell,"ColumnRule");
                var label=cell.Find("StatLabel_"+i).GetComponent<Text>();Box(label.rectTransform,68,17,69,47);Style(label,27);
                Right(ui.statValues[i].rectTransform,new[]{151f,135.5f,132f,149f,133f,146f}[i],20,70,47);Style(ui.statValues[i],30,true);if(i==0||i==3||i==5)ui.statValues[i].alignment=TextAnchor.MiddleRight;
                Right((RectTransform)ui.statButtons[i].transform,61,17,45,50);var add=ui.statButtons[i].GetComponent<Image>();add.sprite=Slice("AddButton",264,1110,47,53,new Vector4(9,9,9,9));add.type=Image.Type.Sliced;add.fillCenter=false;add.pixelsPerUnitMultiplier=1;add.color=Color.white;var bg=Pic(add.transform,"AddPaper",Slice("AddPaper",274,1115,29,7));Full(bg.rectTransform,6,6,6,6);bg.transform.SetAsFirstSibling();
                var plus=ui.statButtons[i].GetComponentInChildren<Text>();Full(plus.rectTransform);Style(plus,44,true);plus.font=chinese;plus.fontStyle=FontStyle.Bold;plus.fontSize=Mathf.RoundToInt(51*S);plus.rectTransform.anchoredPosition+=Vector2.down*S;
                var icon=cell.Find("StatIcon").GetComponent<Image>();icon.sprite=statSprites[i];icon.material=ink;var sr=icon.sprite.rect;Box(icon.rectTransform,sr.x-(33+i%3*874f/3),source.height-sr.yMax-(1093+i/3*82),sr.width,sr.height);icon.preserveAspect=true;
            }
            for(int row=0;row<3;row++)Line(body,"ReferenceRow"+row,row*82,0,0);
            for(int c=0;c<4;c++){var line=Pic(body,"ReferenceColumn"+c,null);line.color=new Color(.10f,.075f,.04f,.65f);line.rectTransform.anchorMin=new Vector2(c/3f,0);line.rectTransform.anchorMax=new Vector2(c/3f,1);line.rectTransform.sizeDelta=new Vector2(1*S,-10*S);line.rectTransform.anchoredPosition=new Vector2(0,5*S);if(c==0||c==3)line.color=Color.clear;for(int row=0;row<3;row++){var star=Pic(body,"Intersection"+c+"_"+row,Slice("RuleCross",316,1085,19,18),ink);Box(star.rectTransform,-9.5f,row*82-9,19,18);star.rectTransform.anchorMin=star.rectTransform.anchorMax=new Vector2(c/3f,1);}}
            // The ribbon is assembled from several graphics; tinting only its base exposes a rectangular patch.
            ui.battleButton.transition=Selectable.Transition.None;
            var battle=ui.battleButton.GetComponent<Image>();Paper(battle,redPaper,823,126,"Battle");battle.color=Color.white;
            var br=Pic(battle.transform,"ReferenceRibbon",Slice("BattleRibbon",59,1291,823,126,new Vector4(125,35,104,17)));Full(br.rectTransform);br.type=Image.Type.Sliced;br.fillCenter=false;br.transform.SetAsFirstSibling();br.gameObject.SetActive(false);
            var ribbonLeft=Pic(battle.transform,"OriginalRibbonLeft",Slice("RibbonLeft",59,1291,292,126));Box(ribbonLeft.rectTransform,0,0,292,126);var ribbonRight=Pic(battle.transform,"OriginalRibbonRight",Slice("RibbonRight",631,1291,251,126));Right(ribbonRight.rectTransform,251,0,251,126);var ribbonTop=Pic(battle.transform,"OriginalRibbonTop",Slice("RibbonTop",351,1291,280,19));Width(ribbonTop.rectTransform,292,0,251,19);var ribbonBottom=Pic(battle.transform,"OriginalRibbonBottom",Slice("RibbonBottom",351,1399,280,18));Width(ribbonBottom.rectTransform,292,108,251,18);
            battle.transform.Find("Caption").SetAsLastSibling();foreach(Transform ornament in battle.transform)if(ornament.name=="Icon")ornament.SetAsLastSibling();
            foreach(var oldIcon in battle.GetComponentsInChildren<Image>(true).Where(im=>im.name=="Icon"))oldIcon.gameObject.SetActive(false);
            var wax=Pic(battle.transform,"OriginalWax",Slice("BattleWax",784,1286,97,152));Right(wax.rectTransform,97,-5,97,152);wax.transform.SetAsLastSibling();
            var bt=battle.transform.Find("Caption").GetComponent<Text>();Center(bt.rectTransform,12,13,408,98);Style(bt,58,false,new Color(.99f,.91f,.73f));bt.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
            Panel(l.navigation,Slice("NavigationBorder",8,1447,923,190,new Vector4(28,20,28,18)));
            var navSlots=Node(l.navigation,"ReferenceTabs");Full(navSlots,10,0,10,0);
            var navSprites=new[]{Slice("Equipment",59,1474,100,97),SkillBook(),Slice("Shelter",423,1466,103,95),Slice("Map",595,1482,99,88),Slice("Shop",780,1475,96,94)};
            for(int i=0;i<5;i++){
                var b=ui.navigationButtons[i];b.transform.SetParent(navSlots,false);Fraction((RectTransform)b.transform,i,5,0,185);Disable(b.transform,"NavDivider");Disable(b.transform,"SelectedBookmark");
                if(i==2){var book=Pic(b.transform,"ReferenceBookmark",redPaper);Center(book.rectTransform,0,-9,150,216);Paper(book,redPaper,150,216,"Bookmark");var edges=Pic(book.transform,"BookmarkEdges",Slice("Bookmark",397,1438,150,216,new Vector4(15,29,15,13)));Full(edges.rectTransform);edges.type=Image.Type.Sliced;edges.fillCenter=false;edges.gameObject.SetActive(false);var a=Pic(book.transform,"OriginalBookmarkTop",Slice("BookmarkTop",397,1438,150,124));Box(a.rectTransform,0,0,150,124);var z=Pic(book.transform,"OriginalBookmarkBottom",Slice("BookmarkBottom",397,1613,150,41));Box(z.rectTransform,0,175,150,41);var left=Pic(book.transform,"OriginalBookmarkLeft",Slice("BookmarkLeft",397,1562,15,51));Box(left.rectTransform,0,124,15,51);var right=Pic(book.transform,"OriginalBookmarkRight",Slice("BookmarkRight",533,1562,14,51));Box(right.rectTransform,136,124,14,51);book.transform.SetAsFirstSibling();}
                var icon=b.transform.Find("Icon").GetComponent<Image>();icon.sprite=navSprites[i];icon.useSpriteMesh=i==1;icon.material=i==2?null:colour;Center(icon.rectTransform,0,i==2?19:27,100,98);icon.preserveAspect=true;icon.gameObject.SetActive(i!=2);
                var caption=b.transform.Find("Caption").GetComponent<Text>();Center(caption.rectTransform,0,i==2?115:117,175,50);Style(caption,i==2?37:29,false,i==2?new Color(.98f,.92f,.81f):Color.black);if(i==2)caption.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
                if(i!=4){var rule=Pic(b.transform,"ReferenceDivider",Slice("NavDivider",186,1459,24,167),ink);Right(rule.rectTransform,12,12,24,167);}
                if(i==1){var badge=(RectTransform)ui.skillBadge.transform.parent;badge.GetComponent<Image>().sprite=Ring("SkillBadge",317,1463,42,44,0);badge.GetComponent<Image>().useSpriteMesh=false;badge.GetComponent<Image>().material=Surface("BadgeRing",badge.GetComponent<Image>().sprite,1,42,44,.42f);var badgeFill=Pic(badge,"BadgeFill",redPaper);Full(badgeFill.rectTransform,3,3,3,3);Paper(badgeFill,redPaper,36,38,"BadgeFill",true);badgeFill.transform.SetAsFirstSibling();Center(badge,54,17,42,44);Style(ui.skillBadge,29,true,new Color(.99f,.96f,.88f));Full(ui.skillBadge.rectTransform);}
            }
            foreach(var modalText in ui.modal.GetComponentsInChildren<Text>(true))Style(modalText,modalText.fontSize/S);
            ui.numberText.transform.SetAsLastSibling();l.Apply();ProductionUiArt.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(home);}AssetDatabase.SaveAssets();
    }
}
