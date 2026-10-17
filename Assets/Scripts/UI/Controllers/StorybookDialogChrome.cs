using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared authored dialog frame; forwards return to each dialog's existing cancel action.</summary>
public sealed class StorybookDialogChrome : MonoBehaviour
{
    public Button back;
    public Button cancelAction;
    void Awake(){back.onClick.AddListener(Return);}
    void LateUpdate(){back.interactable=cancelAction&&cancelAction.interactable;}
    void Return(){if(cancelAction&&cancelAction.interactable)cancelAction.onClick.Invoke();}
}
