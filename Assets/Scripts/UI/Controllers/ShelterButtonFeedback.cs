using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public sealed class ShelterButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = .96f;
    public Transform animationTarget;
    Transform Target => animationTarget ? animationTarget : transform;
    Vector3 restingScale;
    Button button;
    void Awake() { button = GetComponent<Button>(); restingScale = Target.localScale; }
    public void OnPointerDown(PointerEventData e) { if (button.IsInteractable()) Target.localScale = restingScale * pressedScale; }
    public void OnPointerUp(PointerEventData e) { Target.localScale = restingScale; }
    public void OnPointerExit(PointerEventData e) { Target.localScale = restingScale; }
    void OnDisable() { if (button) Target.localScale = restingScale; }
}
