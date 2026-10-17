using UnityEngine;
using UnityEngine.EventSystems;
public sealed class DeveloperOverlayDrag : MonoBehaviour, IDragHandler
{
 public DeveloperOverlayUI view;
 public void OnDrag(PointerEventData e){view.panel.anchoredPosition+=e.delta/view.GetComponent<Canvas>().scaleFactor;view.Clamp();}
}
