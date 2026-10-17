using UnityEngine;

/// <summary>Cover the supported portrait region without stretching the background art.</summary>
[ExecuteAlways]
public sealed class StorybookResultBackdrop : MonoBehaviour
{
    public RectTransform supported,background;
    public Vector2 artworkSize;
    void OnEnable()=>Apply();
    void LateUpdate()=>Apply();
    void Apply(){
        if(!supported||!background||artworkSize.x<=0||artworkSize.y<=0)return;
        var size=ShelterResponsiveLayout.SupportedSize(((RectTransform)transform).rect.size);
        supported.anchorMin=supported.anchorMax=supported.pivot=new Vector2(.5f,.5f);supported.anchoredPosition=Vector2.zero;supported.sizeDelta=size;
        background.anchorMin=background.anchorMax=background.pivot=new Vector2(.5f,.5f);background.anchoredPosition=Vector2.zero;background.sizeDelta=artworkSize;
        background.localScale=Vector3.one*Mathf.Max(size.x/artworkSize.x,size.y/artworkSize.y);
    }
}
