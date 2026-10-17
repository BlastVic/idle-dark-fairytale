using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored return-button sound; registered before page callbacks can disable the button.</summary>
[RequireComponent(typeof(Button)), DisallowMultipleComponent, DefaultExecutionOrder(-100)]
public sealed class StorybookReturnAudio : MonoBehaviour
{
    Button button;
    void Awake(){button=GetComponent<Button>();button.onClick.AddListener(Play);}
    void Play(){if(button&&button.IsActive()&&button.IsInteractable())StorybookNavigation.PlayClick();}
    void OnDestroy(){if(button)button.onClick.RemoveListener(Play);}
}
