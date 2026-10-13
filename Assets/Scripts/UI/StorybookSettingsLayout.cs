using UnityEngine;

[ExecuteAlways]
public sealed class StorybookSettingsLayout:MonoBehaviour
{
    public RectTransform page,viewport,footer;
    public float designWidth=540,headerHeight=124,footerHeight=54;
    Vector2 previous;Rect previousSafe;
#if UNITY_EDITOR
    public Rect? validationSafe;
#endif
    void OnEnable()=>Apply();
    void LateUpdate()=>Apply(false);
    public void Apply(bool force=true){if(!page)return;var size=((RectTransform)transform).rect.size;Rect safe=Screen.safeArea;
#if UNITY_EDITOR
        if(validationSafe.HasValue)safe=validationSafe.Value;
#endif
        if(size.x<1||size.y<1||(!force&&previous==size&&previousSafe==safe))return;previous=size;previousSafe=safe;
        var n=BattleHudSafeArea.NormalizedSafeArea(safe,new Vector2(Screen.width,Screen.height));var lo=Vector2.Scale(n.min,size);var hi=Vector2.Scale(n.max,size);
        float scale=(hi.x-lo.x)/designWidth;float height=(hi.y-lo.y)/scale;
        page.anchorMin=page.anchorMax=page.pivot=Vector2.one*.5f;page.sizeDelta=new Vector2(designWidth,height);page.localScale=Vector3.one*scale;page.anchoredPosition=(lo+hi-size)*.5f;
        StorybookEquipmentLayout.Box(viewport,26,headerHeight,designWidth-52,height-headerHeight-footerHeight);
        StorybookEquipmentLayout.Box(footer,26,height-footerHeight,designWidth-52,footerHeight);
    }
}
