#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Added only during an explicit Play-mode validation, never saved to a scene or player build.
public sealed class MonsterPreviewRuntimeProbe : MonoBehaviour
{
    public MonsterPreviewHud hud;
    public bool selectorHit;
    public int width, height;
    void LateUpdate()
    {
        if (!hud || !EventSystem.current) return;
        width = Screen.width; height = Screen.height;
        var rect = (RectTransform)hud.catalogButton.transform;
        var hits = new List<RaycastResult>();
        var pointer = new PointerEventData(EventSystem.current) { displayIndex = 0, position = rect.TransformPoint(rect.rect.center) };
        hud.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
        selectorHit = hits.Any(hit => hit.gameObject == hud.catalogButton.gameObject);
    }
}

#endif
