using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit opt-in initial authoring. Never rebuilds on import or entering Play.</summary>
[InitializeOnLoad]
public static class StorybookEquipmentBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Equipment", Path="Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab", CellPath="Assets/DarkFairytale/UI/Equipment/Prefabs/EquipmentCell.prefab", Output="output/equipment-ui/";
    static Texture2D source,expanded;
    static Font font,regular;
    static Material ink,colour;
    static Sprite paper,red,frame;
    static StorybookEquipmentBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Output+"build.request"))return;File.Delete(Output+"build.request");try{Build();File.WriteAllText(Output+"build-result.txt","PASS");}catch(Exception e){File.WriteAllText(Output+"build-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string path,Func<T> make) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a)return a;a=make();AssetDatabase.CreateAsset(a,path);return a;}
    static Sprite Slice(string n,float x,float y,float w,float h,Vector4 border=default,bool exp=false){var tex=exp?expanded:source;return Asset(Root+"/Sprites/"+n+".asset",()=>{var s=ProductionUiArt.Create(Root+"/Sprites/"+n+".asset",tex,new Rect(x,tex.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);s.name=n;return s;});}
    static RectTransform Node(Transform p,string n){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(p,false);return r;}
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static void Full(RectTransform r,float l=0,float t=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l,bottom);r.offsetMax=new Vector2(-right,-t);}
    static Image Pic(Transform p,string n,Sprite s,float x,float y,float w,float h,Material m=null){var r=Node(p,n);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.material=m;im.raycastTarget=false;return im;}
    static Text Txt(Transform p,string n,string value,float x,float y,float w,float h,int size,Color? color=null){var r=Node(p,n);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=color??Color.black;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Material Surface(string name,Sprite s,float w,float h){return Asset(Root+"/Materials/"+name+".mat",()=>{var m=new Material(Shader.Find("UI/Reference Ink Cutout"));var r=s.rect;var t=s.texture;m.SetFloat("_Mode",2);m.SetVector("_SourceRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));m.SetVector("_Tiles",new Vector4(w/r.width,h/r.height,0,0));return m;});}
    static Image Fill(Transform p,string n,float x,float y,float w,float h,bool crimson=false){var s=crimson?red:paper;var im=Pic(p,n,s,x,y,w,h);im.type=Image.Type.Tiled;return im;}
    static void Frame(Transform p,string n,Sprite s){var im=Pic(p,n,s,0,0,100,100);Full(im.rectTransform);im.type=Image.Type.Sliced;im.fillCenter=false;}
    static RectTransform Panel(Transform p,string n,float height,Sprite custom=null){var im=Fill(p,n,8,0,925,height);Frame(im.transform,"OriginalArtBorder",custom?custom:frame);return im.rectTransform;}
    static RectTransform Content(Transform p,string n,float h){var r=Node(p,n);Box(r,0,0,925,h);return r;}
    static void Rule(Transform p,float x,float y,float w,float h=1){var im=Pic(p,"InkRule",null,x,y,w,h);im.color=new Color(.2f,.16f,.11f,.4f);}
    static Button Hit(Transform p,string n,float x,float y,float w,float h){var im=Pic(p,n,null,x,y,w,h);im.color=new Color(1,1,1,.001f);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.95f,.85f);c.pressedColor=new Color(.68f,.59f,.48f);c.disabledColor=c.normalColor;c.fadeDuration=.08f;b.colors=c;b.gameObject.AddComponent<ShelterButtonFeedback>();b.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");return b;}
    static Button Button(Transform p,string n,string caption,float x,float y,float w,float h,bool crimson=false,int size=27){var b=Hit(p,n,x,y,w,h);var im=b.GetComponent<Image>();var s=crimson?red:paper;im.color=Color.white;im.sprite=s;im.type=Image.Type.Tiled;Frame(b.transform,"OriginalButtonBorder",Slice(crimson?"RedButtonFrame":"PaperButtonFrame",crimson?134:571,1282,crimson?361:232,66,new Vector4(22,14,22,14)));Txt(b.transform,"Caption",caption,8,0,w-16,h,size,crimson?new Color(.98f,.94f,.82f):Color.black);return b;}
    static void Bookmark(Transform p,float w,float h){var fill=Fill(p,"BookmarkPaper",3,3,w-6,h-24,true);Frame(p,"BookmarkEdge",Slice("CategoryBookmarkEdge",29,162,172,148,new Vector4(9,23,9,10)));}
    static ScrollRect Scroll(Transform p,string n,float x,float y,float w,float h,out RectTransform viewport,out RectTransform content){viewport=Node(p,n);Box(viewport,x,y,w,h);viewport.gameObject.AddComponent<RectMask2D>();var bg=Pic(viewport,"DragSurface",null,0,0,w,h);Full(bg.rectTransform);bg.color=new Color(1,1,1,.001f);bg.raycastTarget=true;var sc=viewport.gameObject.AddComponent<ScrollRect>();sc.horizontal=false;sc.vertical=true;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=35;sc.viewport=viewport;content=Node(viewport,"Content");Box(content,0,0,w,h);sc.content=content;return sc;}
    [MenuItem("Tools/Dark Fairytale/UI/Build Equipment UI (Explicit)")]
    public static void Build()
    {
        if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode before authoring equipment assets.");
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Output);AssetDatabase.Refresh();
        source=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Equipment/Art/ApprovedEquipment.png");expanded=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Equipment/Art/ApprovedExpanded.png");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");regular=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
        ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/Materials/ReferenceInk.mat");colour=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/Materials/ReferenceColour.mat");
        paper=Slice("CleanPaperV3",370,345,40,35);red=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/BlankRedV2.asset");
        frame=Slice("PanelFrameV3",8,950,925,414,new Vector4(23,10,23,15));BuildCell();
        var root=Node(null,"StorybookEquipment");
        try{
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();var ui=root.gameObject.AddComponent<StorybookEquipmentUI>();var l=root.gameObject.AddComponent<StorybookEquipmentLayout>();ui.layout=l;
            l.categoriesHeight=140;l.detailHeight=440;l.toolbarHeight=142;l.collapsedHeight=64;l.expandedHeight=256;l.fusionHeight=60;l.navigationHeight=164;l.gap=6;
            var black=Pic(root,"OutsideSupportedArea",null,0,0,941,1672);black.color=Color.black;black.raycastTarget=true;Full(black.rectTransform);
            l.supported=Node(root,"SupportedViewport");l.supported.gameObject.AddComponent<RectMask2D>();l.core=Node(l.supported,"Core_9x16");
            var backdrop=Pic(l.core,"DarkBindingBackground",Slice("DarkBinding",0,0,941,12),0,0,941,1672,Surface("BindingTile",Slice("DarkBinding",0,0,941,12),941,1672));Full(backdrop.rectTransform);
            l.header=Panel(l.core,"Header",151,frame);l.headerContent=Content(l.header,"Contents",151);
            ui.back=Hit(l.headerContent,"BackToShelter",19,7,78,141);Pic(ui.back.transform,"BackBookmark",Slice("BackBookmark",30,14,67,143),0,0,67,143);
            Pic(l.headerContent,"ForestInk",Slice("ForestInk",119,54,152,77),105,41,152,77,ink);
            Txt(l.headerContent,"Title","装 备",323,30,260,92,51);
            Pic(l.headerContent,"Portrait",Slice("Portrait",662,38,108,109),651,25,108,109);
            ui.level=Txt(l.headerContent,"PlayerLevel","Lv. 20",770,46,144,58,40);ui.number=Txt(l.headerContent,"SubjectNumber","No. 017",770,101,144,32,22);
            foreach(var name in new[]{"Portrait","PlayerLevel","SubjectNumber"}){
                var r=(RectTransform)l.headerContent.Find(name);var pos=r.anchoredPosition;
                r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(pos.x-925,pos.y);
            }
            var headerTitle=(RectTransform)l.headerContent.Find("Title");
            headerTitle.anchorMin=headerTitle.anchorMax=new Vector2(.5f,1);headerTitle.pivot=new Vector2(.5f,1);headerTitle.anchoredPosition=new Vector2(0,-30);

            l.categories=Panel(l.core,"Categories",140,frame);l.categoryContent=Content(l.categories,"Contents",140);
            ui.categoryButtons=new Button[5];ui.categoryIcons=new Image[5];ui.categoryBackgrounds=new Image[5];ui.categoryBadges=new GameObject[5];
            string[] cats={"武器","头盔","盾牌","护甲","饰品"};Rect[] catRects={new Rect(91,179,71,56),new Rect(271,175,60,71),new Rect(441,177,61,69),new Rect(615,175,70,70),new Rect(790,174,69,72)};
            for(int i=0;i<5;i++){
                var b=Hit(l.categoryContent,"Category_"+cats[i],15+i*179,4,177,133);ui.categoryButtons[i]=b;
                var selected=Pic(b.transform,"SelectedBookmark",null,0,0,177,133);selected.color=Color.clear;Bookmark(selected.transform,177,133);ui.categoryBackgrounds[i]=selected;
                var c=catRects[i];ui.categoryIcons[i]=Pic(b.transform,"CategoryIcon",Slice("CategoryIconV4"+i,c.x,c.y,c.width,c.height),54,13,67,69,colour);
                ui.categoryIcons[i].preserveAspect=true;
                if(i==0){var light=Asset("Assets/DarkFairytale/UI/Equipment/Materials/WhiteWeaponInk.mat",()=>{var m=new Material(Shader.Find("UI/Equipment Light Ink"));m.SetFloat("_Mode",6);return m;});ui.categoryIcons[i].material=light;}
                Txt(b.transform,"Caption",cats[i],3,83,171,45,32);
                var dot=Pic(b.transform,"NewItem",Slice("NewDot",866,166,32,34),143,4,25,26);ui.categoryBadges[i]=dot.gameObject;
                if(i<4)Rule(b.transform,176,15,1,100);
            }
            l.backpack=Panel(l.core,"Backpack",648);l.toolbar=Content(l.backpack,"Toolbar",142);
            Pic(l.toolbar,"BagIcon",Slice("BackpackIcon",70,326,55,66),53,15,55,66,colour);ui.count=Txt(l.toolbar,"InventoryCount","背包 24件",118,14,290,65,38);ui.count.alignment=TextAnchor.MiddleLeft;
            ui.sortButton=Button(l.toolbar,"Sort","排序",418,28,148,50,false,25);ui.sortCaption=ui.sortButton.GetComponentInChildren<Text>();
            ui.filterButton=Button(l.toolbar,"Filter","筛选",580,28,115,50,false,25);ui.filterCaption=ui.filterButton.GetComponentInChildren<Text>();
            ui.batchButton=Button(l.toolbar,"Batch","批量分解",706,28,174,50,false,25);ui.batchCaption=ui.batchButton.GetComponentInChildren<Text>();Rule(l.toolbar,37,89,850);
            foreach(var b in new[]{ui.sortButton,ui.filterButton,ui.batchButton})b.transform.Find("OriginalButtonBorder").GetComponent<Image>().sprite=Slice("ToolbarButtonFrame",426,334,149,51,new Vector4(5,5,5,5));
            ui.columnButtons=new Button[3];
            for(int n=0;n<3;n++){
                int columns=n+3;var b=Button(l.toolbar,"Columns"+columns,"",43+n*50,91,42,42,false,20);ui.columnButtons[n]=b;
                b.transform.Find("OriginalButtonBorder").GetComponent<Image>().sprite=Slice("ToolbarButtonFrame",426,334,149,51,new Vector4(5,5,5,5));
                float w=(26-(columns-1)*2)/(float)columns;
                for(int row=0;row<2;row++)for(int col=0;col<columns;col++){var mark=Pic(b.transform,"GridGlyph"+row+"_"+col,null,8+col*(w+2),10+row*12,w,10);mark.color=Color.black;}
            }
            ui.compactModeButton=Button(l.toolbar,"CompactMode","精简",647,91,116,42,false,23);
            ui.detailedModeButton=Button(l.toolbar,"DetailedMode","详细",764,91,116,42,false,23);
            foreach(var mode in new[]{ui.compactModeButton,ui.detailedModeButton})mode.transform.Find("OriginalButtonBorder").GetComponent<Image>().sprite=Slice("ToolbarButtonFrame",426,334,149,51,new Vector4(5,5,5,5));
            ui.gridScroll=Scroll(l.backpack,"EquipmentViewport",43,142,839,487,out var viewport,out var content);l.gridViewport=viewport;l.gridScroll=ui.gridScroll;
            ui.grid=content.gameObject.AddComponent<GridLayoutGroup>();ui.grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;ui.grid.constraintCount=4;ui.grid.cellSize=new Vector2(200,277.0013f);ui.grid.spacing=new Vector2(13,12);ui.grid.childAlignment=TextAnchor.UpperLeft;
            ui.cellPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CellPath).GetComponent<StorybookEquipmentCell>();
            for(int i=0;i<15;i++){var cell=(GameObject)PrefabUtility.InstantiatePrefab(ui.cellPrefab.gameObject,content);cell.name="EquipmentSlot_"+i;}
            ui.emptyText=Txt(viewport,"EmptyInventory","暂无此类装备",25,35,789,100,30);ui.emptyText.gameObject.SetActive(false);
            // A slim persistent scrollbar remains outside item hit regions.
            var track=Pic(l.backpack,"ScrollbarTrack",null,897,100,4,490);track.color=new Color(.35f,.27f,.18f,.25f);track.rectTransform.anchorMin=new Vector2(1,0);track.rectTransform.anchorMax=Vector2.one;track.rectTransform.offsetMin=new Vector2(-31,25);track.rectTransform.offsetMax=new Vector2(-27,-100);
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;var handle=Pic(track.transform,"Handle",null,0,0,4,80);handle.color=new Color(.24f,.18f,.11f,.7f);bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;ui.gridScroll.verticalScrollbar=bar;ui.gridScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            l.detail=Panel(l.core,"FixedEquipmentDetails",440,Slice("DetailsFrameV2",8,950,925,414,new Vector4(23,10,23,15)));l.detailContent=Content(l.detail,"Contents",440);
            var swipe=l.detailContent.gameObject.AddComponent<StorybookEquipmentSwipe>();swipe.owner=ui;
            var touch=Pic(l.detailContent,"DetailsSwipeSurface",null,17,17,888,310);touch.raycastTarget=true;touch.color=new Color(1,1,1,.001f);
            // Compatibility references are saved but invisible: the approved compact layout has no title/navigation band.
            ui.previous=Button(l.detailContent,"Previous","‹",0,0,1,1);ui.previous.gameObject.SetActive(false);
            ui.next=Button(l.detailContent,"Next","›",0,0,1,1);ui.next.gameObject.SetActive(false);
            ui.title=Txt(l.detailContent,"SelectedTitle","",0,0,1,1,1);ui.title.gameObject.SetActive(false);
            Txt(l.detailContent,"CurrentHeading","当前装备",38,15,268,39,30);
            Txt(l.detailContent,"TargetHeading","目标装备",328,15,268,39,30,new Color(.5f,.1f,.08f));
            Txt(l.detailContent,"DeltaHeading","换装变化",618,15,268,39,30);
            Rule(l.detailContent,313,19,1,306);Rule(l.detailContent,603,19,1,306);
            ui.equippedIcon=Pic(l.detailContent,"EquippedItem",null,115,58,112,92);ui.equippedIcon.preserveAspect=true;
            ui.selectedIcon=Pic(l.detailContent,"SelectedItem",null,405,58,112,92);ui.selectedIcon.preserveAspect=true;
            ui.equippedName=Txt(l.detailContent,"EquippedName","旧猎刀",40,150,260,32,24);
            ui.selectedName=Txt(l.detailContent,"SelectedName","荆棘猎刀",330,150,260,32,24);
            foreach(var t in new[]{ui.equippedName,ui.selectedName}){t.verticalOverflow=VerticalWrapMode.Truncate;t.resizeTextForBestFit=true;t.resizeTextMinSize=15;t.resizeTextMaxSize=24;}
            ui.equippedGrade=Txt(l.detailContent,"EquippedGrade","普通",42,182,258,27,20);
            ui.grade=Txt(l.detailContent,"SelectedGrade","普通",332,182,258,27,20);
            Pic(l.detailContent,"ChangeSymbol",ChangeSymbol(),711,71,78,74,ink);
            Txt(l.detailContent,"ChangeCaption","装备属性变化",626,167,250,32,23);
            ui.lockButton=Hit(l.detailContent,"ToggleItemLock",513,117,72,30);ui.lockCaption=Txt(ui.lockButton.transform,"Caption","锁定",0,0,72,30,18,new Color(.3f,.26f,.2f));
            ui.comparisonScroll=Scroll(l.detailContent,"ComparisonViewport",38,215,850,111,out var cv,out var cc);ui.comparisonContent=cc;
            var swipeTable=cv.gameObject.AddComponent<StorybookEquipmentSwipe>();swipeTable.owner=ui;
            string[] stats={"生命","攻击","防御","攻速","暴击","暴伤","生命%","攻击%","防御%"};string[] icons={"Heart","Swords","Shield","Speed","Critical","CriticalDamage","Heart","Swords","Shield"};
            ui.comparisonRows=new StorybookEquipmentUI.ComparisonRow[9];
            for(int i=0;i<9;i++){
                var r=Node(cc,"Compare_"+stats[i]);Box(r,0,i*37,850,37);var row=new StorybookEquipmentUI.ComparisonRow();row.root=r;
                for(int col=0;col<3;col++){
                    float x=col*290;
                    var icon=Pic(r,"StatIcon"+col,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/"+icons[i]+".asset"),x+4,7,23,23,ink);icon.preserveAspect=true;
                    var label=Txt(r,"Label"+col,stats[i],x+31,0,85,37,21);label.alignment=TextAnchor.MiddleLeft;
                    if(col==0){row.icon=icon;row.label=label;}
                    Rule(r,x+3,36,254);
                }
                row.current=Txt(r,"Current","12",129,0,122,37,23);row.current.alignment=TextAnchor.MiddleRight;
                row.selected=Txt(r,"Selected","18",419,0,122,37,23);row.selected.alignment=TextAnchor.MiddleRight;
                row.delta=Txt(r,"Delta","↑ +6",690,0,150,37,23);row.delta.alignment=TextAnchor.MiddleRight;
                ui.comparisonRows[i]=row;
            }
            ui.equip=Button(l.detailContent,"Equip","装 备",126,346,361,66,true,34);ui.equipCaption=ui.equip.GetComponentInChildren<Text>();
            ui.decompose=Button(l.detailContent,"Decompose","分 解",563,346,232,66,false,32);ui.decomposeCaption=ui.decompose.GetComponentInChildren<Text>();
            l.attributes=Panel(l.core,"CharacterAttributes",64,Slice("AttributesFrameV2",8,1196,925,263,new Vector4(23,8,23,16),true));l.attributeContent=Content(l.attributes,"Contents",256);
            Pic(l.attributeContent,"HeartHeading",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/Heart.asset"),48,16,42,39,ink);
            Txt(l.attributeContent,"Heading","角色属性",100,2,287,60,39);ui.points=Txt(l.attributeContent,"Points","可用点数 3",633,0,170,62,25);
            ui.foldButton=Hit(l.attributeContent,"ToggleAttributes",18,0,887,64);
            var arrow=Pic(ui.foldButton.transform,"Arrow",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/FoldChevron.asset"),816,23,33,23,ink);ui.foldArrow=arrow.rectTransform;ui.foldArrow.pivot=Vector2.one*.5f;ui.foldArrow.anchoredPosition+=new Vector2(16.5f,-11.5f);
            var ab=Node(l.attributeContent,"ExpandableAttributeBody");Box(ab,24,67,877,183);ui.attributeBody=ab.gameObject.AddComponent<CanvasGroup>();
            Rule(ab,7,0,864);ui.statButtons=new Button[6];ui.statValues=new Text[6];
            for(int i=0;i<6;i++){
                int c=i%3,r=i/3;var cell=Node(ab,"Stat_"+stats[i]);Box(cell,13+c*288,11+r*73,279,69);
                var border=Pic(cell,"CellFrame",Slice("StatCellFrame",47,1274,280,66,new Vector4(4,4,4,4),true),0,0,279,69);border.type=Image.Type.Sliced;border.fillCenter=false;
                Pic(cell,"StatIcon",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/"+icons[i]+".asset"),13,12,45,43,ink);
                Txt(cell,"Label",stats[i],65,4,64,62,26);ui.statValues[i]=Txt(cell,"Value","33",123,4,94,62,27);
                ui.statButtons[i]=Button(cell,"AddStat","+",225,11,43,45,false,31);
                ui.statButtons[i].transform.Find("OriginalButtonBorder").GetComponent<Image>().sprite=Slice("PlusBorder",272,1285,42,43,new Vector4(4,4,4,4),true);
            }
            Txt(ab,"TotalsCaption","当前角色总属性",211,157,460,26,20,new Color(.39f,.34f,.27f));
            ui.rebirthText=Txt(ab,"Rebirth","",12,159,180,24,18);
            l.fusion=Panel(l.core,"FusionProgress",60,frame);l.fusionContent=Content(l.fusion,"Contents",60);
            Pic(l.fusionContent,"FusionGlyph",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/Fusion.asset"),57,8,56,45,ink);
            Txt(l.fusionContent,"Caption","融合进度",133,8,138,43,25);
            var f=Pic(l.fusionContent,"ProgressDark",null,280,18,301,25);f.color=new Color(.1f,.105f,.1f);
            ui.fusionFill=Pic(l.fusionContent,"ProgressFill",red,280,18,301,25,Surface("FusionRed",red,301,25));ui.fusionFill.type=Image.Type.Filled;ui.fusionFill.fillMethod=Image.FillMethod.Horizontal;
            var fb=Pic(l.fusionContent,"ProgressFrame",Slice("FusionBarFrame",278,1449,315,32,new Vector4(13,7,13,7)),269,14,315,32);fb.type=Image.Type.Sliced;fb.fillCenter=false;
            ui.fusionText=Txt(l.fusionContent,"ProgressText","36%",474,11,100,40,24,new Color(.98f,.95f,.87f));Txt(l.fusionContent,"Hint","分解积累经验",663,8,225,43,25);
            l.navigation=Panel(l.core,"Navigation",164,frame);l.navigationContent=Content(l.navigation,"Contents",164);
            string[] nav={"装备","技能","庇护所","地图","商店"};string[] navIcons={"Equipment","SkillsTight","Shelter","Map","Shop"};ui.navigationButtons=new Button[5];
            for(int i=0;i<5;i++){
                var b=Hit(l.navigationContent,"Nav_"+nav[i],i*185,0,185,164);ui.navigationButtons[i]=b;
                if(i==0){Fill(b.transform,"SelectedPaper",19,0,153,140,true);Frame(b.transform,"SelectedBookmark",Slice("NavBookmark",20,1503,178,164,new Vector4(12,24,12,12)));}
                var icon=i==1?SkillGlyph():i==3?Slice("NavigationMapV3",606,1535,97,65):AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Map/Sprites/"+navIcons[i]+".asset");var pic=Pic(b.transform,"Icon",icon,42,19,100,86,colour);pic.preserveAspect=true;pic.useSpriteMesh=true;
                if(i==1){pic.material=Asset("Assets/DarkFairytale/UI/Equipment/Materials/SkillBookCutout.mat",()=>{var m=new Material(Shader.Find("UI/Equipment Light Ink"));m.SetFloat("_Mode",7);var r=icon.rect;m.SetVector("_SourceRect",new Vector4(r.x/source.width,r.y/source.height,r.width/source.width,r.height/source.height));return m;});}
                Txt(b.transform,"Caption",nav[i],0,109,185,47,31,i==0?new Color(.98f,.94f,.81f):Color.black);if(i<4)Rule(b.transform,183,18,1,124);
                if(i==1){var badge=Pic(b.transform,"SkillBadge",Slice("SkillBadgeV4",320,1514,42,43),125,11,36,38);var badgeMat=Asset("Assets/DarkFairytale/UI/Equipment/Materials/EquipmentBadgeRing.mat",()=>{var m=new Material(Shader.Find("UI/Reference Ink Cutout"));var r=badge.sprite.rect;m.SetFloat("_Mode",1);m.SetFloat("_Inner",.39f);m.SetVector("_SourceRect",new Vector4(r.x/source.width,r.y/source.height,r.width/source.width,r.height/source.height));return m;});badge.material=badgeMat;var fill=Fill(badge.transform,"BadgeInterior",6,6,24,25,true);ui.skillBadge=Txt(badge.transform,"Count","3",0,0,36,37,24,new Color(.98f,.94f,.82f));}
            }
            BuildOverlays(ui,l.core);StorybookDialogAuthoring.Apply((RectTransform)ui.modalTitle.transform.parent,ui.modalCancel);l.Apply();ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        Attach();AssetDatabase.SaveAssets();
    }
    static Sprite ChangeSymbol()
    {
        var texture=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Equipment/Art/ApprovedCompactDetails.png");
        return Asset(Root+"/Sprites/ChangeSymbolV4.asset",()=>{var s=ProductionUiArt.Create(texture,new Rect(1486,texture.height-316,145,139),Vector2.one*.5f,100);s.name="ChangeSymbolV4";return s;});
    }
    static Sprite SkillGlyph()
    {
        // Keep the complete source book, excluding the adjacent notification badge without repainting it.
        var s=Slice("NavigationSkillBookV4",239,1524,92,84);
        Vector2[] pixels={new Vector2(0,0),new Vector2(65,0),new Vector2(65,36),new Vector2(92,36),new Vector2(92,84),new Vector2(0,84)};
        s.OverrideGeometry(pixels.Select(p=>new Vector2(p.x-46,42-p.y)/100).ToArray(),new ushort[]{0,1,2,0,2,5,2,3,4,2,4,5});EditorUtility.SetDirty(s);return s;
    }
    static void BuildCell()
    {
        var root=Node(null,"EquipmentCell");Box(root,0,0,157.4f,218);
        try{
            var ui=root.gameObject.AddComponent<StorybookEquipmentCell>();var im=root.gameObject.AddComponent<Image>();im.sprite=paper;im.type=Image.Type.Tiled;im.raycastTarget=true;
            var visual=Node(root,"Visuals");Box(visual,0,0,157.4f,218);ui.visual=visual;
            Frame(visual,"NormalFrame",Slice("CellFrame",55,407,196,170,new Vector4(5,5,5,5)));
            ui.button=root.gameObject.AddComponent<Button>();ui.button.targetGraphic=im;ui.button.navigation=new Navigation{mode=Navigation.Mode.None};var c=ui.button.colors;c.pressedColor=new Color(.72f,.63f,.51f);c.disabledColor=c.normalColor;ui.button.colors=c;root.gameObject.AddComponent<ShelterButtonFeedback>();root.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
            ui.selectedFrame=Pic(visual,"SelectedFrame",Slice("SelectedCellFrame",264,406,199,175,new Vector4(6,6,6,6)),0,0,200,172);Full(ui.selectedFrame.rectTransform);ui.selectedFrame.type=Image.Type.Sliced;ui.selectedFrame.fillCenter=false;
            ui.icon=Pic(visual,"ItemArtwork",null,29,32,100,70);ui.icon.preserveAspect=true;
            ui.compactIconPose=Node(visual,"CompactIconPose");Box(ui.compactIconPose,19,41,120,131);
            ui.detailedIconPose=Node(visual,"DetailedIconPose");Box(ui.detailedIconPose,19,41,120,131);
            var overlay=Pic(visual,"DetailedAttributes",null,4,108,149.4f,106);overlay.color=new Color(0,0,0,.82f);ui.detailedBody=overlay.gameObject;
            overlay.rectTransform.anchorMin=Vector2.zero;overlay.rectTransform.anchorMax=Vector2.one;overlay.rectTransform.offsetMin=new Vector2(4,4);overlay.rectTransform.offsetMax=new Vector2(-4,-40);
            ui.detailLabels=Txt(overlay.transform,"Labels","攻击\n暴击\n攻速",9,3,64,100,19,new Color(.98f,.94f,.84f));
            ui.detailValues=Txt(overlay.transform,"Values","17\n6%\n100%",70,3,70,100,19,new Color(.98f,.94f,.84f));
            foreach(var t in new[]{ui.detailLabels,ui.detailValues}){t.alignment=t==ui.detailLabels?TextAnchor.UpperLeft:TextAnchor.UpperRight;t.font=regular;t.lineSpacing=.95f;Full(t.rectTransform,t==ui.detailLabels?9:70,3,t==ui.detailLabels?76:8,3);}

            ui.primary=Txt(visual,"PrimaryStat","攻击 18",5,182,147.4f,29,22);
            ui.quality=Pic(visual,"QualityDiamond",Slice("GradeDiamond",62,414,29,34),6,6,24,28,colour);
            var ribbon=Pic(visual,"StateRibbon",null,65,4,88,26);ui.stateRibbon=ribbon.gameObject;ui.state=Txt(ribbon.transform,"State","已装备",0,0,88,26,18,new Color(.98f,.94f,.84f));
            ui.lockIcon=Pic(visual,"Locked",Slice("LockGlyph",626,687,32,41),130,76,20,26,ink).gameObject;
            ui.multiMark=Txt(visual,"BatchCheck","✓",115,66,38,38,30,new Color(.6f,.08f,.06f)).gameObject;
            ui.multiMark.SetActive(false);ui.lockIcon.SetActive(false);ui.selectedFrame.gameObject.SetActive(false);ui.stateRibbon.SetActive(false);
            ProductionUiArt.SaveAsPrefabAsset(root.gameObject,CellPath);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
    static void BuildOverlays(StorybookEquipmentUI ui,Transform parent)
    {
        var m=Pic(parent,"DecomposeConfirmation",null,0,0,941,1672);Full(m.rectTransform);m.color=new Color(0,0,0,.73f);m.raycastTarget=true;ui.modal=m.gameObject;
        var panel=Panel(m.transform,"ConfirmationPaper",380);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.sizeDelta=new Vector2(740,380);panel.anchoredPosition=Vector2.zero;
        ui.modalTitle=Txt(panel,"Title","确认分解",35,20,670,70,39);ui.modalMessage=Txt(panel,"Message","",44,92,652,152,27);
        ui.modalCancel=Button(panel,"Cancel","取消",62,282,245,64);ui.modalConfirm=Button(panel,"Confirm","确认分解",380,282,285,64,true);ui.modal.SetActive(false);
        var o=Pic(parent,"InventoryOptions",null,0,0,941,1672);Full(o.rectTransform);o.color=new Color(0,0,0,.73f);o.raycastTarget=true;ui.options=o.gameObject;
        var op=Panel(o.transform,"OptionsPaper",420);op.anchorMin=op.anchorMax=op.pivot=Vector2.one*.5f;op.sizeDelta=new Vector2(560,420);op.anchoredPosition=Vector2.zero;
        ui.optionsTitle=Txt(op,"Title","装备排序",30,10,500,65,35);ui.optionButtons=new Button[4];ui.optionCaptions=new Text[4];
        for(int i=0;i<4;i++){ui.optionButtons[i]=Button(op,"Option_"+i,"",54,85+i*77,452,62);ui.optionCaptions[i]=ui.optionButtons[i].GetComponentInChildren<Text>();}ui.options.SetActive(false);
    }
    static void Attach()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainScenes/Gameplay.unity") {
            if(scene.isDirty)throw new InvalidOperationException("Save current scene before installing equipment UI.");
            scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");
        }
        var gc=UnityEngine.Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);
        if(gc.inventoryPanel&&!gc.inventoryPanel.GetComponent<StorybookEquipmentUI>())gc.inventoryPanel.SetActive(false);
        var existing=UnityEngine.Object.FindFirstObjectByType<StorybookEquipmentUI>(FindObjectsInactive.Include);
        if(!existing){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),scene);existing=go.GetComponent<StorybookEquipmentUI>();}
        gc.inventoryPanel=existing.gameObject;existing.gameObject.SetActive(false);EditorUtility.SetDirty(gc);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
