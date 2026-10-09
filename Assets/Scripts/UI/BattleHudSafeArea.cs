using UnityEngine;

/// <summary>Fits an authored 1080x1920 core inside the safe area without stretching artwork.</summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class BattleHudSafeArea : MonoBehaviour
{
    public RectTransform core;
    public Vector2 referenceSize = new Vector2(1080, 1920);
    public bool respectSafeArea = true;
    Rect lastSafe;
    Vector2 lastSize;

    void OnEnable() { Apply(true); }
    void LateUpdate() { Apply(false); }
    void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Apply(false); }

    public void Apply(bool force = true)
    {
        if (!core || referenceSize.x <= 0 || referenceSize.y <= 0) return;
        var root = (RectTransform)transform;
        Vector2 size = root.rect.size;
        if (size.x <= 0 || size.y <= 0) return;
        Rect safe = respectSafeArea && Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        if (!force && size == lastSize && safe == lastSafe) return;
        lastSize = size; lastSafe = safe;
        Rect normalized = NormalizedSafeArea(safe, new Vector2(Screen.width, Screen.height));
        FitCore(core, size, normalized, referenceSize);
    }

    public static Rect NormalizedSafeArea(Rect safe, Vector2 screen)
    {
        if (screen.x <= 0 || screen.y <= 0 || safe.width <= 0 || safe.height <= 0) return new Rect(0, 0, 1, 1);
        return Rect.MinMaxRect(Mathf.Clamp01(safe.xMin / screen.x), Mathf.Clamp01(safe.yMin / screen.y),
            Mathf.Clamp01(safe.xMax / screen.x), Mathf.Clamp01(safe.yMax / screen.y));
    }

    public static void FitCore(RectTransform target, Vector2 rootSize, Rect normalizedSafe, Vector2 designSize)
    {
        Vector2 available = Vector2.Scale(rootSize, normalizedSafe.size);
        float scale = Mathf.Min(available.x / designSize.x, available.y / designSize.y);
        target.anchorMin = target.anchorMax = new Vector2(.5f, .5f);
        target.pivot = new Vector2(.5f, .5f);
        target.sizeDelta = designSize;
        target.anchoredPosition = Vector2.Scale(normalizedSafe.center - new Vector2(.5f, .5f), rootSize);
        target.localScale = Vector3.one * scale;
    }
}
