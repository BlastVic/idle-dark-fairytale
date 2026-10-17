using UnityEngine;
using UnityEngine.EventSystems;

public sealed class StorybookMapDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public StorybookMapUI map;
    public void OnBeginDrag(PointerEventData e)=>map.BeginDrag();
    public void OnEndDrag(PointerEventData e)=>map.EndDrag();
    void OnDisable(){if(map)map.EndDrag();}
}
