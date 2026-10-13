using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Horizontal details browsing coexists with vertical scrolling of long stat lists.</summary>
public sealed class StorybookEquipmentSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public StorybookEquipmentUI owner;
    public ScrollRect vertical;
    Vector2 start;
    bool horizontal;
    public void OnBeginDrag(PointerEventData e){start=e.pressPosition;horizontal=Mathf.Abs(e.position.x-start.x)>Mathf.Abs(e.position.y-start.y);if(vertical&&!horizontal)vertical.OnBeginDrag(e);}
    public void OnDrag(PointerEventData e){if(vertical&&!horizontal)vertical.OnDrag(e);}
    public void OnEndDrag(PointerEventData e){if(vertical&&!horizontal)vertical.OnEndDrag(e);var delta=e.position-start;if(horizontal&&Mathf.Abs(delta.x)>Screen.width*.08f&&Mathf.Abs(delta.x)>Mathf.Abs(delta.y)*1.5f)owner.Step(delta.x<0?1:-1);}
}
