using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class StorybookSkillsLayout : MonoBehaviour
{
    public RectTransform supported, core, header, points, tree, navigation;
    public RectTransform headerContent, pointsContent, navigationContent;
    public ScrollRect treeScroll;
    public float designWidth = 941, headerHeight = 151, pointsHeight = 102, navigationHeight = 164, gap = 7;
    Vector2 previous;
    Rect previousSafe;
#if UNITY_EDITOR
    Rect? validationSafe;
    public void SetValidationSafeArea(Rect? value) { validationSafe=value; Apply(); }
#endif
    void OnEnable() => Apply();
    void LateUpdate() => Apply(false);
    public void Apply(bool force=true)
    {
        if (!core || !tree || !supported) return;
        Vector2 size=((RectTransform)transform).rect.size;
        if(size.x<1 || size.y<1)return;
        Rect safe=Screen.safeArea;
#if UNITY_EDITOR
        if(validationSafe.HasValue)safe=validationSafe.Value;
#endif
        if(!force && size==previous && safe==previousSafe)return;
        previous=size; previousSafe=safe;
        Vector2 bounds=ShelterResponsiveLayout.SupportedSize(size);
        supported.anchorMin=supported.anchorMax=supported.pivot=Vector2.one*.5f;
        supported.sizeDelta=bounds; supported.anchoredPosition=Vector2.zero;
        Rect n=BattleHudSafeArea.NormalizedSafeArea(safe,new Vector2(Screen.width,Screen.height));
        Vector2 lo=Vector2.Max(Vector2.zero,Vector2.Scale(n.min,size)+(bounds-size)*.5f);
        Vector2 hi=Vector2.Min(bounds,Vector2.Scale(n.max,size)+(bounds-size)*.5f);
        float scale=(hi.x-lo.x)/designWidth;
        if(scale<=0)return;
        float h=(hi.y-lo.y)/scale;
        core.anchorMin=core.anchorMax=core.pivot=Vector2.one*.5f;
        core.sizeDelta=new Vector2(designWidth,h);core.localScale=Vector3.one*scale;core.anchoredPosition=(lo+hi-bounds)*.5f;
        // Artwork is uniformly scaled only. A shorter screen scrolls the tree instead of squeezing it.
        float density=Mathf.Clamp(h/1672f,.85f,1);
        Place(header,headerContent,0,headerHeight,density);
        Place(points,pointsContent,headerHeight*density+gap,pointsHeight,density);
        var sharedNav=navigation.GetComponent<StorybookNavigation>();
        float navSpace=sharedNav?sharedNav.ReservedHeight(core):navigationHeight*density;
        if(!sharedNav)Place(navigation,navigationContent,h-navSpace,navigationHeight,density);
        float top=(headerHeight+pointsHeight)*density+gap*2;
        StorybookEquipmentLayout.Box(tree,8,top,925,h-top-navSpace-gap);
        if(treeScroll && treeScroll.content){var p=treeScroll.content.anchoredPosition;p.y=Mathf.Clamp(p.y,0,Mathf.Max(0,treeScroll.content.rect.height-treeScroll.viewport.rect.height));treeScroll.content.anchoredPosition=p;}
    }
    static void Place(RectTransform panel,RectTransform contents,float y,float height,float density)
    {
        StorybookEquipmentLayout.Box(panel,8,y,925,height*density);
        contents.anchorMin=contents.anchorMax=new Vector2(.5f,1);contents.pivot=new Vector2(.5f,1);
        contents.anchoredPosition=Vector2.zero;contents.sizeDelta=new Vector2(925,height);contents.localScale=Vector3.one*density;
    }
}
