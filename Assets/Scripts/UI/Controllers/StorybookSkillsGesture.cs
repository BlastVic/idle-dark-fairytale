using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Swipe to the origin edge closes details; vertical gestures still scroll their content.</summary>
public sealed class StorybookSkillsGesture : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
{
    public StorybookSkillsUI owner;
    public ScrollRect vertical;
    public bool background;
    bool horizontal, dragged;
    Vector2 start;
    public void OnPointerDown(PointerEventData e){dragged=false;}
    public void OnBeginDrag(PointerEventData e){start=e.pressPosition;dragged=true;horizontal=!background&&Mathf.Abs(e.position.x-start.x)>Mathf.Abs(e.position.y-start.y);if(background)owner.CloseDrawer();if(!horizontal&&vertical)vertical.OnBeginDrag(e);}
    public void OnDrag(PointerEventData e){if(!horizontal&&vertical)vertical.OnDrag(e);}
    public void OnEndDrag(PointerEventData e){if(!horizontal&&vertical)vertical.OnEndDrag(e);float dx=e.position.x-start.x;if(horizontal&&Mathf.Abs(dx)>Screen.width*.06f&&((dx>0)==owner.DrawerOnRight))owner.CloseDrawer();}
    public void OnPointerClick(PointerEventData e){if(background&&!dragged)owner.CloseDrawer();dragged=false;}
}
