using UnityEngine;
using UnityEngine.UI;

/// <summary>Resizes saved panels; never creates or rebuilds artwork or controls.</summary>
[ExecuteAlways]
public sealed class StorybookEquipmentLayout : MonoBehaviour
{
    public RectTransform supported, core, header, categories, backpack, detail, attributes, fusion, navigation;
    public RectTransform headerContent, categoryContent, toolbar, detailContent, attributeContent, fusionContent, navigationContent;
    public RectTransform gridViewport;
    public ScrollRect gridScroll;
    public bool responsiveContents;
    public float designWidth=941, headerHeight=151, categoriesHeight=145, toolbarHeight=94;
    public float detailHeight=410, collapsedHeight=70, expandedHeight=262, fusionHeight=66, navigationHeight=174, gap=8;
    [Range(0,1)] public float expansion;
    public float FoldHeight=>Mathf.Lerp(collapsedHeight,expandedHeight,expansion);
    public float Density {get;private set;}=1;
    Vector2 previous; Rect lastSafe; float lastExpansion=-1;
    bool applying;
#if UNITY_EDITOR
    Rect? validationSafeArea;
    public void SetValidationSafeArea(Rect? value){validationSafeArea=value;Apply();}
#endif
    void OnEnable()=>Apply();
    void LateUpdate()=>Apply(false);
    public void Apply(bool force=true)
    {
        if(applying||!supported||!core||!gridViewport)return;
        var size=((RectTransform)transform).rect.size;
        if(size.x<1||size.y<1)return;
        Rect safe=Screen.safeArea;
#if UNITY_EDITOR
        if(validationSafeArea.HasValue)safe=validationSafeArea.Value;
#endif
        if(!force&&size==previous&&safe==lastSafe&&lastExpansion==expansion)return;
        applying=true; previous=size;lastSafe=safe;lastExpansion=expansion;
        Vector2 bounds=ShelterResponsiveLayout.SupportedSize(size);
        supported.anchorMin=supported.anchorMax=supported.pivot=Vector2.one*.5f;
        supported.sizeDelta=bounds;supported.anchoredPosition=Vector2.zero;
        var normalized=BattleHudSafeArea.NormalizedSafeArea(safe,new Vector2(Screen.width,Screen.height));
        Vector2 lo=Vector2.Max(Vector2.zero,Vector2.Scale(normalized.min,size)+(bounds-size)*.5f);
        Vector2 hi=Vector2.Min(bounds,Vector2.Scale(normalized.max,size)+(bounds-size)*.5f);
        Vector2 usable=hi-lo;float scale=usable.x/designWidth;
        if(scale<=0){applying=false;return;}
        float h=usable.y/scale;
        core.anchorMin=core.anchorMax=core.pivot=Vector2.one*.5f;
        core.sizeDelta=new Vector2(designWidth,h);core.localScale=Vector3.one*scale;core.anchoredPosition=(lo+hi-bounds)*.5f;
        // At wide aspect ratios the fixed panels compact uniformly, without distorting artwork.
        // Remaining height always belongs to the grid, including every intermediate fold frame.
        Density=Mathf.Clamp(h/1672f,.70f,1);
        // Fixed reservation: column count and item stat count must never resize surrounding panels.
        var grid=gridScroll?gridScroll.content.GetComponent<GridLayoutGroup>():null;
        if(grid){float fixedHeight=headerHeight+categoriesHeight+detailHeight+FoldHeight+navigationHeight+toolbarHeight+gap*5;
            Density=Mathf.Min(Density,Mathf.Clamp((h-218-17)/fixedHeight,.60f,1));}
        float d=Density, g=gap*d, y=0;
        Place(header,headerContent,y,headerHeight,d);FitHeaderContent(header,headerContent,d);y+=headerHeight*d+g;
        Place(categories,categoryContent,y,categoriesHeight,d);y+=categoriesHeight*d+g;
        float top=y, bottom=h;
        var sharedNav=navigation.GetComponent<StorybookNavigation>();
        if(sharedNav)bottom-=sharedNav.ReservedHeight(core);
        else {bottom-=navigationHeight*d;Place(navigation,navigationContent,bottom,navigationHeight,d);}
        bottom-=g;

        bottom-=FoldHeight*d;Place(attributes,attributeContent,bottom,FoldHeight,d);bottom-=g;
        bottom-=detailHeight*d;Place(detail,detailContent,bottom,detailHeight,d);bottom-=g;
        Box(backpack,8,top,925,Mathf.Max(toolbarHeight*d+90,bottom-top));
        Content(toolbar,toolbarHeight,d,responsiveContents?backpack.rect.width/d:925);
        gridViewport.anchorMin=Vector2.zero;gridViewport.anchorMax=Vector2.one;gridViewport.pivot=new Vector2(0,1);
        float gridInset=responsiveContents?43*d:43;
        gridViewport.offsetMin=new Vector2(gridInset,17);gridViewport.offsetMax=new Vector2(-gridInset,-toolbarHeight*d);
        if(gridScroll && gridScroll.content) {
            float max=Mathf.Max(0,gridScroll.content.rect.height-gridViewport.rect.height);
            var p=gridScroll.content.anchoredPosition;p.y=Mathf.Clamp(p.y,0,max);gridScroll.content.anchoredPosition=p;
        }
        applying=false;
    }
    void Place(RectTransform r,RectTransform contents,float y,float height,float density){Box(r,8,y,925,height*density);Content(contents,height,density,responsiveContents?r.rect.width/density:925);}
    static void Content(RectTransform r,float height,float d,float width=925){if(!r)return;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(width,height);r.anchoredPosition=Vector2.zero;r.localScale=Vector3.one*d;}
    // Expand the coordinate space, not the artwork. Anchored edge groups stay at the panel edges.
    public static void FitHeaderContent(RectTransform panel,RectTransform content,float density)
    {
        if(!panel||!content||density<=0)return;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,panel.rect.width/density);
    }
    public static void Box(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
}
