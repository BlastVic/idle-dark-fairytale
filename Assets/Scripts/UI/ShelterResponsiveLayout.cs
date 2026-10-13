using UnityEngine;

/// <summary>Only adapts saved nodes. Supported portrait viewport: 1:2 through 3:4.</summary>
[ExecuteAlways]
public sealed class ShelterResponsiveLayout : MonoBehaviour
{
    public RectTransform viewport, core, room;
    public RectTransform top, chests, attributes, battle, navigation;
    public RectTransform[] functions;
    public Vector2 roomSize=new Vector2(1080,2160);
    public Vector2 referenceSize=new Vector2(1080,1920);
    public float minAspect=.5f,maxAspect=.75f;
    public float rightMargin=14;
    public float margin=14,topHeight=188,chestHeight=164,navHeight=224;
    public float attributeBottom=450,battleBottom=286;
    public float topInset=24,chestTop=222,bottomInset=24,battleSide=54,battleHeight=132;
    public float functionTop=438,functionGap=155,functionSize=142,functionRight=34,functionHeight=142;
    public float feetInArtwork=1065,feetClearance=80,wideFeetClearance=20;
    public bool respectSafeArea=true;
    public CanvasGroup presentationControls;
    [System.NonSerialized] public Vector2 introCompositionSize;
    bool storyFramingLocked;
    float storyScalePerHeight, storyOffsetPerHeight;
    public void LockStoryFraming() {
        float h=viewport.rect.height;
        storyScalePerHeight=room.localScale.x/h;
        storyOffsetPerHeight=room.anchoredPosition.y/h;
        storyFramingLocked=true;
    }
    Vector2 lastSize;Rect lastSafe;
    void OnEnable(){Apply();}
    void LateUpdate(){Apply(false);}
    void OnRectTransformDimensionsChange(){if(isActiveAndEnabled)Apply(false);}
    public void Apply(bool force=true)
    {
        var ui=GetComponent<ShelterHomeUI>();
        if(!ui||!viewport||!core||!room||!attributes||!top||!chests||!battle||!navigation)return;
        var root=(RectTransform)transform;var size=root.rect.size;if(size.x<1||size.y<1)return;
        Rect safe=respectSafeArea&&Application.isPlaying?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
        if(!force&&lastSize==size&&lastSafe==safe)return;lastSize=size;lastSafe=safe;
        var captureBounds=SupportedSize(size,minAspect,maxAspect);
        if(introCompositionSize.sqrMagnitude>0) size=introCompositionSize;
        var bounds=SupportedSize(size,minAspect,maxAspect);
        viewport.anchorMin=viewport.anchorMax=viewport.pivot=new Vector2(.5f,.5f);
        viewport.sizeDelta=captureBounds;viewport.anchoredPosition=Vector2.zero;viewport.localScale=Vector3.one;
        var normalized=BattleHudSafeArea.NormalizedSafeArea(safe,new Vector2(Screen.width,Screen.height));
        var safeMin=Vector2.Scale(normalized.min,size)+(bounds-size)*.5f;
        var safeMax=Vector2.Scale(normalized.max,size)+(bounds-size)*.5f;
        safeMin=Vector2.Max(Vector2.zero,safeMin);safeMax=Vector2.Min(bounds,safeMax);
        Vector2 usable=safeMax-safeMin;float scale=Mathf.Min(usable.x/referenceSize.x,usable.y/referenceSize.y);
        if(scale<=0)return;
        Vector2 design=usable/scale;
        core.anchorMin=core.anchorMax=core.pivot=new Vector2(.5f,.5f);core.sizeDelta=design;
        core.anchoredPosition=(safeMin+safeMax-bounds)*.5f;core.localScale=Vector3.one*scale;
        // Frames expand horizontally; fonts, borders and icons retain uniform scale.
        TopLeft(top,margin,topInset,design.x-margin-rightMargin,topHeight);
        TopLeft(chests,margin,chestTop,design.x-margin-rightMargin,chestHeight);
        attributes.anchorMin=attributes.anchorMax=new Vector2(0,0);attributes.pivot=new Vector2(0,0);
        attributes.anchoredPosition=new Vector2(margin,attributeBottom);attributes.sizeDelta=new Vector2(design.x-margin-rightMargin,attributes.sizeDelta.y);
        TopLeft(battle,battleSide,design.y-battleBottom-battleHeight,design.x-battleSide*2,battleHeight);
        if(!navigation.GetComponent<StorybookNavigation>())TopLeft(navigation,margin,design.y-navHeight-bottomInset,design.x-margin-rightMargin,navHeight);
        for(int i=0;i<functions.Length;i++)TopLeft(functions[i],design.x-functionRight-functionSize,functionTop+i*functionGap,functionSize,functionHeight);
        // Preserve the entire scene's spatial relationship: character and fire share this transform.
        float clearance=Mathf.Lerp(feetClearance,wideFeetClearance,Mathf.InverseLerp(referenceSize.x/referenceSize.y,maxAspect,bounds.x/bounds.y));
        float sceneBottom=(design.y-attributeBottom-ui.expandedHeight-clearance)*scale;
        float roomScale=Mathf.Max(bounds.x/roomSize.x,bounds.y/roomSize.y,sceneBottom/feetInArtwork);
        float offset=Mathf.Min(0,sceneBottom-feetInArtwork*roomScale);
        offset=Mathf.Max(bounds.y-roomSize.y*roomScale,offset);
        room.anchorMin=room.anchorMax=new Vector2(.5f,1);room.pivot=new Vector2(.5f,1);
        room.sizeDelta=roomSize;
        room.localScale=Vector3.one*(storyFramingLocked ? storyScalePerHeight*captureBounds.y : roomScale);
        room.anchoredPosition=new Vector2(0,storyFramingLocked ? storyOffsetPerHeight*captureBounds.y : -offset);
    }
    public static Vector2 SupportedSize(Vector2 size,float min=.5f,float max=.75f)
    {
        float aspect=size.x/size.y;
        if(aspect>max)return new Vector2(size.y*max,size.y);
        if(aspect<min)return new Vector2(size.x,size.x/min);
        return size;
    }
    static void TopLeft(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
}
