using UnityEngine;

/// <summary>Uniformly fits saved 9:16 core nodes inside the safe area. Never rebuilds UI.</summary>
[ExecuteAlways]
public sealed class CursedBookIntroLayout : MonoBehaviour
{
    public RectTransform core;
    public RectTransform prologueSubtitle;
    public float subtitleBottomPadding = 45;
    public Vector2 designSize = new Vector2(1080, 1920);
    void OnEnable() { Apply(); }
    void LateUpdate() { Apply(); }
    public void Apply()
    {
        if (!core || Screen.width < 1 || Screen.height < 1) return;
        var root = (RectTransform)transform;
        var size = root.rect.size;
        var safe = Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        var available = Vector2.Scale(max - min, size);
        var scale = Mathf.Min(available.x / designSize.x, available.y / designSize.y);
        core.localScale = Vector3.one * scale;
        core.anchoredPosition = Vector2.Scale((min + max) * .5f - Vector2.one * .5f, size);
        if (prologueSubtitle) {
            // Book zoom follows viewport height; captions must follow its bottom safe area,
            // not a fixed Y in the narrower 9:16 core (which overlaps paper on tall phones).
            float y = root.rect.yMin + min.y * size.y +
                      (subtitleBottomPadding + prologueSubtitle.rect.height * .5f) * scale;
            var local = core.InverseTransformPoint(root.TransformPoint(new Vector3(0, y, 0)));
            var position = prologueSubtitle.anchoredPosition;
            position.y = local.y; prologueSubtitle.anchoredPosition = position;
        }
    }
}
