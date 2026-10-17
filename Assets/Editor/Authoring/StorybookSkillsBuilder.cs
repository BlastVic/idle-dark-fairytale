using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring only. No import/Play-mode rebuild and no runtime UI construction.</summary>
[InitializeOnLoad]
public static class StorybookSkillsBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Skills", Path="Assets/DarkFairytale/UI/Skills/Prefabs/StorybookSkills.prefab", Output="output/skills-ui/";
    static Font font,regular;
    static Texture2D source;
    static Sprite paper,red,frame;
    static StorybookSkillsBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Output+"build.request"))return;File.Delete(Output+"build.request");try{Build();File.WriteAllText(Output+"build-result.txt","PASS");}catch(Exception e){File.WriteAllText(Output+"build-result.txt",e.ToString());Debug.LogException(e);}}
    static T Asset<T>(string p,Func<T> make) where T:UnityEngine.Object{var a=AssetDatabase.LoadAssetAtPath<T>(p);if(a)return a;a=make();AssetDatabase.CreateAsset(a,p);return a;}
    static Sprite Slice(string name,float x,float y,float w,float h){return Asset(Root+"/Sprites/"+name+".asset",()=>{var s=ProductionUiArt.Create(source,new Rect(x,source.height-y-h,w,h),Vector2.one*.5f,100);s.name=name;return s;});}
    static RectTransform Node(Transform parent,string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);return r;}
    static Material illustration;
    static void Box(RectTransform r,float x,float y,float w,float h)=>StorybookEquipmentLayout.Box(r,x,y,w,h);
    static void Full(RectTransform r,float l=0,float t=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l,bottom);r.offsetMax=new Vector2(-right,-t);}
    static Image Pic(Transform p,string name,Sprite sprite,float x,float y,float w,float h){var r=Node(p,name);Box(r,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;return im;}
    static Text Text(Transform p,string name,string value,float x,float y,float w,float h,int size){var r=Node(p,name);Box(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.075f,.055f,.035f);t.text=value;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    static Image Border(Transform p,string name,Sprite s){var im=Pic(p,name,s,0,0,100,100);Full(im.rectTransform);im.type=Image.Type.Sliced;im.fillCenter=false;return im;}
    static RectTransform Panel(Transform p,string name,float w,float h){var im=Pic(p,name,paper,0,0,w,h);im.type=Image.Type.Tiled;Border(im.transform,"OriginalArtBorder",frame);return im.rectTransform;}
    static Button Button(Transform p,string name,string caption,float x,float y,float w,float h,bool crimson=false){var im=Pic(p,name,crimson?red:paper,x,y,w,h);im.type=Image.Type.Tiled;im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var c=b.colors;c.pressedColor=new Color(.65f,.57f,.48f);c.disabledColor=c.normalColor;c.fadeDuration=.08f;b.colors=c;im.gameObject.AddComponent<ShelterButtonFeedback>();var state=im.gameObject.AddComponent<StorybookEquipmentButtonState>();state.grayscaleShader=Shader.Find("UI/Storybook Disabled");
        Border(im.transform,"ButtonBorder",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/"+(crimson?"RedButtonFrame":"PaperButtonFrame")+".asset"));var txt=Text(im.transform,"Caption",caption,8,0,w-16,h,32);Full(txt.rectTransform,8,0,8,0);if(crimson)txt.color=new Color(.98f,.94f,.82f);return b;}
    static void Line(Transform p,string name,Vector2 a,Vector2 b,Color color,float width=2){var v=b-a;var im=Pic(p,name,null,a.x,a.y,v.magnitude,width);im.rectTransform.pivot=new Vector2(0,.5f);im.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);im.color=color;}
    static ScrollRect Scroll(Transform p,string name,out RectTransform content){var vp=Node(p,name);Full(vp,12,12,12,12);vp.gameObject.AddComponent<RectMask2D>();var bg=Pic(vp,"DragSurface",null,0,0,100,100);Full(bg.rectTransform);bg.color=new Color(1,1,1,.001f);bg.raycastTarget=true;var sc=vp.gameObject.AddComponent<ScrollRect>();sc.viewport=vp;sc.horizontal=false;sc.vertical=true;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=35;content=Node(vp,"Content");Box(content,0,0,901,1160);content.anchorMax=new Vector2(1,1);content.sizeDelta=new Vector2(0,1160);sc.content=content;return sc;}
    static Material Circle(string name,Sprite s,bool ring=false){return Asset(Root+"/Materials/"+name+".mat",()=>{var m=new Material(Shader.Find("UI/Reference Ink Cutout"));var r=s.rect;m.SetFloat("_Mode",1);m.SetFloat("_Inner",ring?.45f:0);m.SetVector("_SourceRect",new Vector4(r.x/source.width,r.y/source.height,r.width/source.width,r.height/source.height));return m;});}
    static Material CleanIcon(int id,Sprite sprite)
    {
        var m=Asset(Root+"/Materials/CleanIcon"+id+".mat",()=>new Material(Shader.Find("UI/Skill Illustration Cutout")));
        m.SetFloat("_Mode",0);m.SetFloat("_InkOnly",0);m.SetFloat("_Cutoff",.49f);m.SetFloat("_Feather",.10f);
        m.SetFloat("_HideSourceLock",new[]{2,5,8,9,11,12,13,14,15}.Contains(id)?1:0);
        var r=sprite.rect;m.SetVector("_SourceRect",new Vector4(r.x/source.width,r.y/source.height,r.width/source.width,r.height/source.height));EditorUtility.SetDirty(m);return m;
    }
    [MenuItem("Tools/Dark Fairytale/UI/Build Skills UI (Explicit)")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainScenes/Gameplay.unity"){if(scene.isDirty)throw new InvalidOperationException("Save current scene first.");scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScenes/Gameplay.unity");}
        // Read the legacy scene's actual serialized prerequisite rules, never infer logic from a generated diagram.
        var oldNodes=UnityEngine.Object.FindObjectsByType<SkillButton>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var prerequisites=new Dictionary<int,Vector2Int>();
        foreach(var n in oldNodes){var so=new SerializedObject(n);prerequisites[so.FindProperty("skillKey").intValue]=new Vector2Int(so.FindProperty("prerequisiteKey").intValue,so.FindProperty("prerequisiteKey2").intValue);}
        if(prerequisites.Count!=15)throw new InvalidOperationException("Expected original 15 skill definitions; found "+prerequisites.Count);
        Directory.CreateDirectory(Root+"/Sprites");Directory.CreateDirectory(Output);AssetDatabase.Refresh();
        source=ShelterRefinedBuilder.Import("Assets/ArtSources/UI/Skills/Art/ApprovedTree.png");
        illustration=Asset("Assets/DarkFairytale/UI/Skills/Materials/CleanIllustration.mat",()=>{var m=new Material(Shader.Find("UI/Reference Ink Cutout"));m.SetFloat("_Mode",0);m.SetFloat("_InkOnly",0);m.SetFloat("_Cutoff",.69f);m.SetFloat("_Feather",.085f);return m;});
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Black.otf");regular=AssetDatabase.LoadAssetAtPath<Font>("Assets/DarkFairytale/UI/CursedStorybook/Fonts/NotoSerifCJKsc-Regular.otf");
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/CleanPaperV3.asset");red=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Shelter/Reference/Sprites/BlankRedV2.asset");frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/PanelFrameV3.asset");
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(StorybookEquipmentBuilder.Path).GetComponent<StorybookEquipmentUI>();
        var root=Node(null,"StorybookSkills");
        try{
            var cv=root.gameObject.AddComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=100;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(941,1672);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();var ui=root.gameObject.AddComponent<StorybookSkillsUI>();var layout=root.gameObject.AddComponent<StorybookSkillsLayout>();ui.layout=layout;ui.paper=paper;ui.crimson=red;
            var black=Pic(root,"OutsideSafeViewport",null,0,0,941,1672);Full(black.rectTransform);black.color=new Color(.025f,.021f,.019f);black.raycastTarget=true;
            layout.supported=Node(root,"SupportedViewport");layout.supported.gameObject.AddComponent<RectMask2D>();layout.core=Node(layout.supported,"Core_9x16");
            layout.header=UnityEngine.Object.Instantiate(original.layout.header,layout.core);layout.header.name="Header";layout.headerContent=(RectTransform)layout.header.Find("Contents");
            layout.headerContent.Find("Title").GetComponent<Text>().text="技 能";
            ui.back=layout.headerContent.Find("BackToShelter").GetComponent<Button>();ui.playerLevel=layout.headerContent.Find("PlayerLevel").GetComponent<Text>();ui.subjectNumber=layout.headerContent.Find("SubjectNumber").GetComponent<Text>();
            var sharedNav=StorybookNavigationAuthoring.Instantiate(layout.core,root,1);
            layout.navigation=(RectTransform)sharedNav.transform;layout.navigationContent=null;ui.navigation=sharedNav.buttons;
            layout.points=Panel(layout.core,"SkillPoints",925,102);layout.pointsContent=Node(layout.points,"Contents");Box(layout.pointsContent,0,0,925,102);
            var book=UnityEngine.Object.Instantiate(ui.navigation[1].transform.Find("Icon"),layout.pointsContent);book.name="SkillBook";Box((RectTransform)book,262,13,81,76);
            ui.points=Text(layout.pointsContent,"Points","技能点 3",365,21,310,60,38);
            Text(layout.pointsContent,"UnlockRule","箭头指向后续技能 · 任一前置达到1级即可解锁",80,65,765,29,22).gameObject.SetActive(false);
            layout.tree=Panel(layout.core,"SkillTree",925,1200);
            ui.treeScroll=Scroll(layout.tree,"TreeViewport",out var content);layout.treeScroll=ui.treeScroll;
            var background=ui.treeScroll.viewport.Find("DragSurface").gameObject.AddComponent<StorybookSkillsGesture>();background.owner=ui;background.vertical=ui.treeScroll;background.background=true;
            content.sizeDelta=new Vector2(0,1400);
            for(int row=1;row<5;row++)Line(content,"RowDivider",new Vector2(20,row*268-46),new Vector2(882,row*268-46),new Color(.22f,.18f,.13f,.13f),1);
            var pathRoot=Node(content,"DependencyPaths");Box(pathRoot,0,0,901,1400);
            string[] names=Scripts.Skills.SkillDesign.Names;
            string[] desc=Scripts.Skills.SkillDesign.Descriptions;
            string[][] effect=Scripts.Skills.SkillDesign.Labels;
            Vector2[] positions={default,new Vector2(284,78),new Vector2(798,286),new Vector2(630,78),new Vector2(284,286),new Vector2(630,286),new Vector2(457,494),new Vector2(116,286),new Vector2(630,494),new Vector2(457,702),new Vector2(284,494),new Vector2(630,702),new Vector2(798,494),new Vector2(116,494),new Vector2(284,702),new Vector2(457,910)};
            Rect[] crops={default,new Rect(241,272,132,101),new Rect(752,438,132,106),new Rect(578,270,128,106),new Rect(246,438,137,105),new Rect(576,438,132,105),new Rect(403,625,148,109),new Rect(83,438,132,106),new Rect(576,628,132,106),new Rect(409,818,138,110),new Rect(246,628,137,105),new Rect(576,818,132,112),new Rect(752,628,132,106),new Rect(83,628,132,105),new Rect(247,818,135,112),new Rect(408,991,140,106)};
            var lockSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DarkFairytale/UI/Equipment/Sprites/LockGlyph.asset");var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/UI/Shelter/Reference/Materials/ReferenceInk.mat");
            ui.nodes=new StorybookSkillNode[15];
            for(int i=1;i<=15;i++){
                var r=Node(content,"Skill_"+((SkillsTypeEnum)i));var pos=positions[i];int row=Mathf.RoundToInt((pos.y-78)/208);Box(r,pos.x-76,pos.y-62+row*60,152,175);
                var n=r.gameObject.AddComponent<StorybookSkillNode>();n.key=(SkillsTypeEnum)i;n.prerequisite=(SkillsTypeEnum)prerequisites[i].x;n.alternatePrerequisite=(SkillsTypeEnum)prerequisites[i].y;n.displayName=names[i];n.descriptionTemplate=desc[i];n.effectLabels=effect[i];ui.nodes[i-1]=n;
                var hit=r.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,.001f);n.button=r.gameObject.AddComponent<Button>();n.button.targetGraphic=hit;n.button.navigation=new Navigation{mode=Navigation.Mode.None};r.gameObject.AddComponent<ShelterButtonFeedback>();r.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
                var crop=crops[i];var sprite=i==8?AssetDatabase.LoadAssetAtPath<Sprite>(GhostWolfPackAuthoring.IconPath):i==2?AssetDatabase.LoadAssetAtPath<Sprite>(ShadowWolfSkillAuthoring.IconPath):Slice("CleanSkill"+i,crop.x,crop.y,crop.width,crop.height);n.icon=Pic(r,"ApprovedIllustration",sprite,7,2,138,120);n.icon.material=(i==2||i==8)?null:CleanIcon(i,sprite);n.icon.preserveAspect=true;
                var sel=Node(r,"SelectedCrimsonRing");Box(sel,5,0,142,124);var mark=sel.gameObject.AddComponent<StorybookSkillMark>();mark.ring=true;mark.color=new Color(.62f,.035f,.025f);mark.raycastTarget=false;n.selected=sel.gameObject;n.selected.SetActive(false);
                n.caption=Text(r,"Name",names[i],-6,125,164,32,25);n.level=Text(r,"Level","Lv. 0 / 10",-6,156,164,26,21);
                if(i==8||i==9){n.caption.text=i==8?"召唤：\n幽灵狼群":"召唤：\n书脊龙";n.caption.rectTransform.sizeDelta=new Vector2(164,60);n.level.rectTransform.anchoredPosition=new Vector2(-6,-196);}
                var lk=Pic(r,"Lock",lockSprite,124,93,24,31);lk.material=ink;n.locked=lk.gameObject;
                var available=Node(r,"AvailableState");Box(available,0,0,152,175);n.available=available.gameObject;
                var bookmark=Node(available,"ActionBookmark");Box(bookmark,120,0,30,48);
                var bookmarkMark=bookmark.gameObject.AddComponent<StorybookSkillMark>();bookmarkMark.bookmark=true;bookmarkMark.color=new Color(.494f,.098f,.082f);bookmarkMark.raycastTarget=false;
                n.availableSymbol=Text(available,"CanUpgrade","+",120,1,30,38,27);n.availableSymbol.color=new Color(.98f,.925f,.82f);
            }
            var allLinks=new List<StorybookSkillLink>();
            foreach(var n in ui.nodes)foreach(var k in new[]{n.prerequisite,n.alternatePrerequisite})if(k!=SkillsTypeEnum.NONE){var from=ui.nodes[(int)k-1];var a=positions[(int)k];var b=positions[(int)n.key];Vector2[] route;
                // Dedicated lanes for the three cyclic OR prerequisites preserve the original graph.
                if((int)k==6&&((int)n.key==4||(int)n.key==5)){float lane=(int)n.key==4?376:545;route=new[]{new Vector2(a.x+((int)n.key==4?-65:65),a.y),new Vector2(lane,a.y-12),new Vector2(lane,b.y+5),new Vector2(b.x+((int)n.key==4?66:-66),b.y+5)};}
                else if((int)n.key==6){float lane=(int)k==4?412:502;route=new[]{new Vector2(a.x+((int)k==4?66:-66),a.y),new Vector2(lane,a.y),new Vector2(lane,b.y-93),new Vector2(b.x,b.y-93),new Vector2(b.x,b.y-69)};}
                else if(Mathf.Abs(a.x-b.x)>1)route=new[]{new Vector2(a.x+(b.x<a.x?-66:66),a.y),new Vector2(b.x,a.y),new Vector2(b.x,b.y-68)};
                else route=new[]{new Vector2(a.x,a.y+118),new Vector2(b.x,b.y-68)};
                for(int r=0;r<route.Length;r++)route[r].y=SpacedTreeY(route[r].y);
                allLinks.Add(BuildLink(pathRoot,from,n,route));
            }
            ui.links=allLinks.ToArray();
            foreach(var pos in new[]{new Vector2(457,396),new Vector2(376,338),new Vector2(545,338)}){var t=Text(pathRoot,"AnyPrerequisite","或",pos.x-13,SpacedTreeY(pos.y-13),26,26,20);t.color=new Color(.5f,.075f,.045f);}
            for(int i=0;i<5;i++){Text(content,"Row_"+(i+1),(i+1).ToString(),4,45+i*268,28,44,26);}
            Text(content,"Legend","实线：前置已满足     虚线：前置未满足",28,1330,850,40,22);
            ui.overlay=Node(layout.tree,"DetailsOverlay");Full(ui.overlay,12,12,12,12);ui.overlay.gameObject.AddComponent<RectMask2D>();
            BuildDrawer(ui);
            layout.Apply();ProductionUiArt.SaveAsPrefabAsset(root.gameObject,Path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        var gc=UnityEngine.Object.FindFirstObjectByType<GameplayCanvas>(FindObjectsInactive.Include);var old=gc.skillsPanel;
        if(old&&!old.GetComponent<StorybookSkillsUI>()){old.SetActive(false);old.name="LegacySkillsPanel_Reference";}
        var existing=UnityEngine.Object.FindFirstObjectByType<StorybookSkillsUI>(FindObjectsInactive.Include);
        if(!existing){existing=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),scene)).GetComponent<StorybookSkillsUI>();}
        existing.gameObject.SetActive(false);gc.skillsPanel=existing.gameObject;EditorUtility.SetDirty(gc);PrefabUtility.RecordPrefabInstancePropertyModifications(gc);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    // Insert space between rows without stretching the icons or their attachment offsets.
    static float SpacedTreeY(float y)
    {
        float result=y;
        for(int row=0;row<4;row++)result+=60*Mathf.Clamp01((y-(198+208*row))/26);
        return result;
    }
    static StorybookSkillLink BuildLink(Transform parent,StorybookSkillNode from,StorybookSkillNode to,Vector2[] points)
    {
        var r=Node(parent,from.key+"_to_"+to.key);Box(r,0,0,901,1400);var link=r.gameObject.AddComponent<StorybookSkillLink>();link.source=from;link.target=to;
        var solid=Node(r,"SatisfiedSolid");Box(solid,0,0,901,1400);var dashed=Node(r,"UnmetDashed");Box(dashed,0,0,901,1400);link.solid=solid.gameObject;link.dashed=dashed.gameObject;
        for(int i=1;i<points.Length;i++){var a=points[i-1];var b=points[i];Line(solid,"Segment"+i,a,b,link.satisfied,2.7f);float length=(b-a).magnitude;for(float d=0;d<length;d+=14)Line(dashed,"Dash",Vector2.Lerp(a,b,d/length),Vector2.Lerp(a,b,Mathf.Min(d+8,length)/length),link.unmet,2);}
        var end=points[points.Length-1];var v=(end-points[points.Length-2]).normalized;
        var ar=Node(r,"Arrowhead");Box(ar,end.x,end.y,16,14);ar.pivot=new Vector2(1,.5f);ar.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg);
        var arrow=ar.gameObject.AddComponent<StorybookSkillMark>();arrow.raycastTarget=false;link.arrow=arrow;solid.gameObject.SetActive(false);return link;
    }
    static Text FlowText(Transform p,string name,string value,int size){var t=Text(p,name,value,0,0,350,40,size);t.font=font;t.alignment=TextAnchor.MiddleLeft;var le=t.gameObject.AddComponent<LayoutElement>();le.minHeight=32;return t;}
    static void BuildDrawer(StorybookSkillsUI ui)
    {
        ui.drawer=Panel(ui.overlay,"ContentHeightDrawer",392,650);var surface=ui.drawer.GetComponent<Image>();surface.raycastTarget=true;
        ui.drawerGroup=ui.drawer.gameObject.AddComponent<CanvasGroup>();
        ui.title=Text(ui.drawer,"SkillName","生命复苏",50,8,292,51,32);ui.title.rectTransform.anchorMax=Vector2.one;ui.title.rectTransform.anchorMin=new Vector2(0,1);ui.title.rectTransform.sizeDelta=new Vector2(-100,51);
        ui.close=Button(ui.drawer,"Close","×",340,8,44,44,true);var close=(RectTransform)ui.close.transform;close.anchorMin=close.anchorMax=new Vector2(1,1);close.anchoredPosition3D=new Vector3(-70,-16,0);ui.close.GetComponentInChildren<Text>().fontSize=30;ui.close.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleCenter;ui.close.GetComponentInChildren<Text>().text="";
        Line(ui.close.transform,"CrossA",new Vector2(12,12),new Vector2(32,32),new Color(.98f,.94f,.82f),3);
        Line(ui.close.transform,"CrossB",new Vector2(12,32),new Vector2(32,12),new Color(.98f,.94f,.82f),3);
        ui.detailScroll=Scroll(ui.drawer,"DetailViewport",out var body);ui.drawerBody=body;
        var layout=body.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(4,4,6,8);layout.spacing=18;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var head=Node(body,"IconAndLevel");head.gameObject.AddComponent<LayoutElement>().preferredHeight=78;
        ui.detailIcon=Pic(head,"SkillIllustration",null,0,3,72,72);ui.detailIcon.preserveAspect=true;
        ui.level=Text(head,"Level","Lv. 1 → 2",92,9,226,60,26);ui.level.alignment=TextAnchor.MiddleLeft;ui.level.rectTransform.anchorMax=new Vector2(1,1);ui.level.rectTransform.sizeDelta=new Vector2(-92,60);
        ui.description=FlowText(body,"Description","每2秒恢复生命。",22);ui.description.font=regular;ui.description.lineSpacing=1.25f;
        var compare=Node(body,"CurrentAndNext");ui.comparison=compare.gameObject;
        var comp=compare.gameObject.AddComponent<VerticalLayoutGroup>();comp.childControlWidth=true;comp.childControlHeight=true;comp.childForceExpandHeight=false;comp.spacing=7;
        ui.currentValues=FlowText(compare,"Current","当前\n恢复生命 2%",22);ui.currentValues.font=regular;ui.nextValues=FlowText(compare,"Next","升级后\n恢复生命 2.4%",22);ui.nextValues.font=regular;ui.nextValues.color=new Color(.18f,.34f,.18f);
        ui.currentOnlyText=FlowText(body,"MaxLevel","已达到最高等级",22);ui.currentOnlyText.font=regular;ui.currentOnly=ui.currentOnlyText.gameObject;
        ui.prerequisites=FlowText(body,"Prerequisites","无前置要求",20);ui.prerequisites.font=regular;ui.prerequisites.lineSpacing=1.35f;
        StorybookPrerequisiteAuthoring.Build(ui,body,regular);
        ui.footer=Node(ui.drawer,"FixedActionFooter");Box(ui.footer,28,514,336,112);
        ui.cost=Text(ui.footer,"Cost","消耗 1 技能点",0,0,336,30,22);ui.cost.rectTransform.anchorMax=new Vector2(1,1);ui.cost.rectTransform.sizeDelta=new Vector2(0,30);
        ui.purchase=Button(ui.footer,"Purchase","升 级",0,46,336,60,true);var pr=(RectTransform)ui.purchase.transform;pr.anchorMax=new Vector2(1,1);pr.sizeDelta=new Vector2(0,60);
        ui.purchaseSurface=ui.purchase.GetComponent<Image>();ui.purchaseCaption=ui.purchase.GetComponentInChildren<Text>();ui.purchaseCaption.fontSize=26;
        ui.purchase.transform.Find("ButtonBorder").GetComponent<Image>().pixelsPerUnitMultiplier=1.4f;
        var gesture=ui.drawer.gameObject.AddComponent<StorybookSkillsGesture>();gesture.owner=ui;gesture.vertical=ui.detailScroll;
        var bodyGesture=ui.detailScroll.viewport.Find("DragSurface").gameObject.AddComponent<StorybookSkillsGesture>();bodyGesture.owner=ui;bodyGesture.vertical=ui.detailScroll;
        StorybookSkillDetailsAuthoring.Configure(ui);
        ui.drawerGroup.alpha=0;ui.drawerGroup.blocksRaycasts=false;ui.drawerGroup.interactable=false;ui.drawer.gameObject.SetActive(false);
    }
}
