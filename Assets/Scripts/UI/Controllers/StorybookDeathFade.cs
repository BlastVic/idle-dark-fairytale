using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Freezes the final death pose, drains its colour, then reveals the authored defeat UI.</summary>
public sealed class StorybookDeathFade : MonoBehaviour
{
    public RawImage finalPage;
    public CanvasGroup pageGroup;
    [Min(0)] public float stillDuration=0;
    [Min(.01f)] public float colourDuration=1.1f, revealDuration=.35f;
    [Min(.01f)] public float inkDuration=1.1f;
    [Min(0)] public float inkHoldDuration=.15f;
    public float InkProgress {get;private set;}
    Texture2D captured;
    Material instance;
    public bool Presenting {get;private set;}
    void Awake(){ResetPage();}
    public IEnumerator CloseLife(System.Action onColourDrained = null)
    {
        ResetPage();
        yield return new WaitForEndOfFrame();
        captured=ScreenCapture.CaptureScreenshotAsTexture();
        instance=new Material(finalPage.material);
        instance.SetFloat("_InkProgress",0);instance.SetFloat("_Progress",0);finalPage.material=instance;finalPage.texture=captured;
        pageGroup.alpha=1;pageGroup.blocksRaycasts=true;Presenting=true;
        if(stillDuration>0)yield return new WaitForSecondsRealtime(stillDuration);
        float time=0;
        while(time<colourDuration){time+=Time.unscaledDeltaTime;instance.SetFloat("_Progress",Mathf.Clamp01(time/colourDuration));yield return null;}
        instance.SetFloat("_Progress",1);
        onColourDrained?.Invoke();
        yield return new WaitForSecondsRealtime(.15f);
        time=0;
        while(time<inkDuration){time+=Time.unscaledDeltaTime;InkProgress=Mathf.Clamp01(time/inkDuration);instance.SetFloat("_InkProgress",InkProgress);yield return null;}
        InkProgress=1;instance.SetFloat("_InkProgress",1);
        yield return new WaitForSecondsRealtime(inkHoldDuration);
    }

    public IEnumerator RevealDefeat()
    {
        // Keep the final page until the destination has actually rendered beneath it.
        yield return new WaitForEndOfFrame();
        float time=0;
        while(time<revealDuration){time+=Time.unscaledDeltaTime;pageGroup.alpha=1-Mathf.SmoothStep(0,1,time/revealDuration);yield return null;}
        ResetPage();
    }
    void ResetPage(){
        Presenting=false;InkProgress=0;
        if(pageGroup){pageGroup.alpha=0;pageGroup.blocksRaycasts=false;pageGroup.interactable=false;}
        if(finalPage)finalPage.texture=null;
        if(captured)Destroy(captured);captured=null;
        if(instance){if(finalPage)finalPage.material=originalMaterial;Destroy(instance);instance=null;}
    }
    Material originalMaterial;
    void OnEnable(){if(finalPage)originalMaterial=finalPage.material;}
    void OnDisable(){ResetPage();}
}
