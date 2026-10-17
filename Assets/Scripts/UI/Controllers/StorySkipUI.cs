using UnityEngine;
using UnityEngine.UI;

/// <summary>Bindings for authored skip controls; never constructs UI at runtime.</summary>
[DefaultExecutionOrder(100)]
public sealed class StorySkipUI : MonoBehaviour
{
    public CursedBookIntro intro;
    public RectTransform core, panel;
    public Button skip, cancel, confirm;
    public GameObject confirmation;
    public Vector2 normalSkipPosition=new Vector2(-116,-68);
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
        rect.anchorMin=rect.anchorMax=new Vector2(1,1);
        // Position in screen space: the book core can be offset by its presentation layout.
        // Keep controls inside the safe area independently of the illustrated page.
        var offset=IsReplay?replaySkipPosition:normalSkipPosition;
        var safe=Screen.safeArea;
        var scale=rect.lossyScale;
        var half=Vector2.Scale(rect.rect.size, new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)))*.5f;
        var inset=new Vector2(Mathf.Max(Mathf.Abs(offset.x)*Mathf.Abs(scale.x),half.x+12),
                              Mathf.Max(Mathf.Abs(offset.y)*Mathf.Abs(scale.y),half.y+12));
        var screen=new Vector2(safe.xMax-inset.x,safe.yMax-inset.y);
        var canvas=skip.GetComponentInParent<Canvas>().rootCanvas;
        Vector3 world;
        if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent,screen,
            canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out world)) rect.position=world;
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
