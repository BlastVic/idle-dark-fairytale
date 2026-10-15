using UnityEngine;
using UnityEngine.UI;

/// <summary>Bindings for authored skip controls; never constructs UI at runtime.</summary>
public sealed class StorySkipUI : MonoBehaviour
{
    public CursedBookIntro intro;
    public RectTransform core, panel;
    public Button skip, cancel, confirm;
    public GameObject confirmation;
    public Vector2 normalSkipPosition=new Vector2(-116,68);
    public Vector2 replaySkipPosition=new Vector2(-116,-68);
    public bool IsReplay => intro.mapStory || intro.returnToMapAfterEnding;
    public bool IsConfirming => confirmation.activeSelf;
    void Awake() {
        skip.onClick.AddListener(Open); cancel.onClick.AddListener(Cancel); confirm.onClick.AddListener(Confirm);
        confirmation.SetActive(false); skip.gameObject.SetActive(false);
    }
    void LateUpdate() {
        skip.gameObject.SetActive(!intro.Finished && intro.prologue && intro.prologue.IsPlaying && !IsConfirming);
        panel.localScale=core.localScale;
        var rect=(RectTransform)skip.transform;
        rect.anchorMin=rect.anchorMax=new Vector2(1,IsReplay?1:0);
        rect.anchoredPosition=IsReplay?replaySkipPosition:normalSkipPosition;
    }
    public void Open() {
        if(intro.Finished || IsConfirming)return;
        if(IsReplay){intro.SkipStory();return;}
        intro.prologue.SetPaused(true);confirmation.SetActive(true);skip.gameObject.SetActive(false);
    }
    public void Cancel() { confirmation.SetActive(false);intro.prologue.SetPaused(false); }
    public void Confirm() { confirmation.SetActive(false);intro.SkipStory(); }
    void OnDisable() { if(intro && intro.prologue && intro.prologue.enabled)intro.prologue.SetPaused(false); }
}
