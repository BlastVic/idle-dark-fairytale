using UnityEngine;

/// <summary>Uniformly fits the authored 9:16 page inside the supported viewport and safe area.</summary>
[ExecuteAlways]
public sealed class StorybookFusionLayout : MonoBehaviour
{
    public RectTransform page;
    public Vector2 designSize = new Vector2(595, 1056);
    public float margin = 8;
    Vector2 previousSize;
    Rect previousSafe;
#if UNITY_EDITOR
    Rect? validationSafe;
    public void SetValidationSafeArea(Rect? rect) { validationSafe = rect; Apply(); }
#endif
    void OnEnable() => Apply();
    void LateUpdate() => Apply(false);
    public void Apply(bool force = true)
    {
        if (!page) return;
        var size = ((RectTransform)transform).rect.size;
        if (size.x <= 0 || size.y <= 0) return;
        Rect safe = Screen.safeArea;
#if UNITY_EDITOR
        if (validationSafe.HasValue) safe = validationSafe.Value;
#endif
        if (!force && size == previousSize && safe == previousSafe) return;
        previousSize = size; previousSafe = safe;
        var normalized = BattleHudSafeArea.NormalizedSafeArea(safe, new Vector2(Screen.width, Screen.height));
        Vector2 lo = Vector2.Scale(normalized.min, size) + Vector2.one * margin;
        Vector2 hi = Vector2.Scale(normalized.max, size) - Vector2.one * margin;
        float scale = Mathf.Max(.01f, Mathf.Min((hi.x-lo.x)/designSize.x, (hi.y-lo.y)/designSize.y));
        page.anchorMin = page.anchorMax = page.pivot = Vector2.one * .5f;
        page.sizeDelta = designSize;
        page.localScale = Vector3.one * scale;
        page.anchoredPosition = (lo+hi-size)*.5f;
    }
}
