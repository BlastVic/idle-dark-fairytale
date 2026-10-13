using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public sealed class ShelterButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = .96f;
    Vector3 restingScale;
    Button button;
    void Awake() { button = GetComponent<Button>(); restingScale = transform.localScale; }
    public void OnPointerDown(PointerEventData e) { if (button.IsInteractable()) transform.localScale = restingScale * pressedScale; }
    public void OnPointerUp(PointerEventData e) { transform.localScale = restingScale; }
    public void OnPointerExit(PointerEventData e) { transform.localScale = restingScale; }
    void OnDisable() { if (button) transform.localScale = restingScale; }
}
