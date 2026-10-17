using System.Collections;
using Assets.Scripts.Game.Enum;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Animates authored chest-rail and shortcut nodes; never creates or rebuilds UI.</summary>
[DefaultExecutionOrder(100)]
public sealed class ShelterChestRail : MonoBehaviour
{
    public ShelterHomeUI home;
    public ShelterResponsiveLayout layout;
    public RectTransform railSlot, rail, shortcut;
    public CanvasGroup railGroup, shortcutGroup;
    public Button collapseButton, expandButton;
    [Min(.01f)] public float railDuration = .3f, shortcutDuration = .22f;
    [Min(0)] public float outsidePadding = 24;
    [SerializeField] bool collapsed;
    public bool Collapsed => collapsed;
    public bool IsAnimating { get; private set; }
    public float RailHidden { get; private set; }
    public float ShortcutVisible { get; private set; }
    bool targetCollapsed;
    Coroutine motion;
    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        collapseButton.onClick.AddListener(Collapse);
        expandButton.onClick.AddListener(Expand);
    }
    void OnEnable()
    {
        targetCollapsed = collapsed;
        Snap();
    }
    void OnDisable()
    {
        if (motion != null) StopCoroutine(motion);
        motion = null;
        // Finish to the requested stable state if navigation interrupts a transition.
        collapsed = targetCollapsed;
        Snap();
    }
    void LateUpdate() { ApplyPose(); ApplyInteraction(); }
    bool CanToggle => !IsAnimating && home && !home.IsNavigating && GameManager.single && GameManager.single.gameState == GameStateType.CAMP;
    public void Collapse() { if (CanToggle && !collapsed) Begin(true); }
    public void Expand() { if (CanToggle && collapsed) Begin(false); }
    void Begin(bool hide)
    {
        targetCollapsed = hide; IsAnimating = true;
        SoundManager.Instance.PlayClip("CLICK");
        ApplyInteraction();
        motion = StartCoroutine(Animate(hide));
    }
    IEnumerator Animate(bool hide)
    {
        if (hide)
        {
            // The rail leaves completely before the shortcut starts entering.
            yield return Slide(true, 0, 1, railDuration);
            rail.gameObject.SetActive(false);
            shortcut.gameObject.SetActive(true);
            yield return Slide(false, 0, 1, shortcutDuration);
        }
        else
        {
            yield return Slide(false, 1, 0, shortcutDuration);
            shortcut.gameObject.SetActive(false);
            rail.gameObject.SetActive(true);
            yield return Slide(true, 1, 0, railDuration);
        }
        collapsed = hide; IsAnimating = false; motion = null;
        ApplyPose(); ApplyInteraction();
    }
    IEnumerator Slide(bool movingRail, float from, float to, float duration)
    {
        for (float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
        {
            float value = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed/Mathf.Max(.01f,duration)));
            if (movingRail) RailHidden = value; else ShortcutVisible = value;
            ApplyPose(); yield return null;
        }
        if (movingRail) RailHidden = to; else ShortcutVisible = to;
        ApplyPose();
    }
    void Snap()
    {
        IsAnimating = false;
        RailHidden = collapsed ? 1 : 0;
        ShortcutVisible = collapsed ? 1 : 0;
        if (rail) rail.gameObject.SetActive(!collapsed);
        if (shortcut) shortcut.gameObject.SetActive(collapsed);
        ApplyPose(); ApplyInteraction();
    }
    void ApplyPose()
    {
        if (!layout || !layout.core || !layout.viewport || !rail || !railSlot || !shortcut || layout.functions.Length == 0) return;
        // The viewport mask is the visible right edge, including at wide aspect ratios and safe areas.
        layout.viewport.GetWorldCorners(corners);
        float right = layout.core.InverseTransformPoint(corners[2]).x-layout.core.rect.xMin+outsidePadding;
        var first = layout.functions[0];
        shortcut.anchorMin = shortcut.anchorMax = shortcut.pivot = new Vector2(0,1);
        shortcut.sizeDelta = first.sizeDelta;
        shortcut.anchoredPosition = new Vector2(Mathf.Lerp(right,first.anchoredPosition.x,ShortcutVisible),first.anchoredPosition.y+layout.functionGap);
        rail.anchoredPosition = new Vector2(Mathf.Max(0,right-railSlot.anchoredPosition.x)*RailHidden,0);
    }
    void ApplyInteraction()
    {
        if (!railGroup || !shortcutGroup || !home) return;
        bool available = !IsAnimating && !home.IsNavigating;
        railGroup.interactable = railGroup.blocksRaycasts = available && !collapsed;
        shortcutGroup.interactable = shortcutGroup.blocksRaycasts = available && collapsed;
        collapseButton.interactable = available && !collapsed;
        expandButton.interactable = available && collapsed;
    }
}
