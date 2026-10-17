using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class StorybookMapLayout : MonoBehaviour
{
    public RectTransform supported,core,header,detail,navigation,viewport,content;
    public ScrollRect scroll;
    public Vector2 referenceSize=new Vector2(941,1672);
    public float headerHeight=151,detailHeight=184,navigationHeight=226;
    public float detailBottom=240,mapTop=149,mapBottom=424;
    public float artworkWidth=941,artworkHeight=1672,initialTop=149,wideInitialTop=400;
    public string positionPreferenceKey="Storybook.Map.Chapter01.Top";
    Vector2 lastSize;Rect lastSafe;
    bool positionLoaded,hasPosition;
    float rememberedTop,lastMapScale;
    void LoadPosition()
    {
        if(!Application.isPlaying||positionLoaded)return;
        positionLoaded=true;hasPosition=PlayerPrefs.HasKey(positionPreferenceKey);
        if(hasPosition)rememberedTop=PlayerPrefs.GetFloat(positionPreferenceKey);
    }
    public void RememberPosition()
    {
        if(!Application.isPlaying||!content||lastMapScale<=0)return;
        rememberedTop=Mathf.Clamp(content.anchoredPosition.y/lastMapScale,0,artworkHeight);
        hasPosition=true;
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false))return;
#endif
        PlayerPrefs.SetFloat(positionPreferenceKey,rememberedTop);PlayerPrefs.Save();
    }
    void OnDisable()=>RememberPosition();
    void OnApplicationPause(bool paused){if(paused)RememberPosition();}
    void OnEnable()=>Apply();
    void LateUpdate()=>Apply(false);
    void OnRectTransformDimensionsChange(){if(isActiveAndEnabled)Apply(false);}
    public void Apply(bool force=true)
    {
        if(!supported||!core||!viewport||!content)return;
        var root=(RectTransform)transform;Vector2 size=root.rect.size;if(size.x<1||size.y<1)return;
        Rect safe=Application.isPlaying?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
        if(!force&&lastSize==size&&lastSafe==safe)return;
        LoadPosition();
        if(Application.isPlaying&&lastMapScale>0){rememberedTop=Mathf.Max(0,content.anchoredPosition.y/lastMapScale);hasPosition=true;}
        lastSize=size;lastSafe=safe;
        Vector2 bounds=ShelterResponsiveLayout.SupportedSize(size);supported.anchorMin=supported.anchorMax=supported.pivot=new Vector2(.5f,.5f);supported.sizeDelta=bounds;supported.anchoredPosition=Vector2.zero;
        var normalized=BattleHudSafeArea.NormalizedSafeArea(safe,new Vector2(Screen.width,Screen.height));
        Vector2 lo=Vector2.Max(Vector2.zero,Vector2.Scale(normalized.min,size)+(bounds-size)*.5f);
        Vector2 hi=Vector2.Min(bounds,Vector2.Scale(normalized.max,size)+(bounds-size)*.5f);
        Vector2 usable=hi-lo;float scale=Mathf.Min(usable.x/referenceSize.x,usable.y/referenceSize.y);if(scale<=0)return;
        Vector2 design=usable/scale;core.anchorMin=core.anchorMax=core.pivot=new Vector2(.5f,.5f);core.sizeDelta=design;core.localScale=Vector3.one*scale;core.anchoredPosition=(lo+hi-bounds)*.5f;
        var sharedNav=navigation.GetComponent<StorybookNavigation>();
        float detailOffset=sharedNav?sharedNav.ReservedHeight(core):detailBottom;
        Box(header,0,0,design.x,headerHeight);Box(detail,8,design.y-detailOffset-detailHeight,design.x-16,detailHeight);
        if(!sharedNav)Box(navigation,8,design.y-navigationHeight,design.x-16,navigationHeight);
        Box(viewport,0,mapTop,design.x,design.y-mapTop-(sharedNav?detailOffset+detailHeight:mapBottom));
        content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.sizeDelta=new Vector2(artworkWidth,artworkHeight);
        float mapScale=design.x/artworkWidth;content.localScale=Vector3.one*mapScale;
        float top=Application.isPlaying&&hasPosition?rememberedTop:Mathf.Lerp(initialTop,wideInitialTop,Mathf.InverseLerp(9f/16,.75f,bounds.x/bounds.y));
        lastMapScale=mapScale;
        content.anchoredPosition=new Vector2(0,Mathf.Clamp(top*mapScale,0,Mathf.Max(0,artworkHeight*mapScale-viewport.rect.height)));if(scroll)scroll.StopMovement();
    }
    static void Box(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
}
