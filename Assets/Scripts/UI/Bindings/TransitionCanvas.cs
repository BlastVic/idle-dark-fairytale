using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Saved paper overlay: turn the outgoing UI with the story book mesh, then develop the destination through wet ink.</summary>
public class TransitionCanvas : MonoBehaviour
{
    static TransitionCanvas instance;
    public static TransitionCanvas single => instance ? instance : (instance=FindFirstObjectByType<TransitionCanvas>());
    public CanvasGroup group;
    public RawImage page, leafImage;
    public StorybookTransitionLeaf leaf;
    [Min(.01f)] public float coverDuration=.65f, revealDuration=.6f;
    public bool playPageTurn;
    public bool isBlack;
    public bool IsCovered {get;private set;}
    Material material, original, leafMaterial, leafOriginal;
    Texture2D captured;
    Coroutine motion;
    bool revealRequested, covering;
    void Awake(){
        instance=this;
        original=page.material;material=new Material(original);page.material=material;
        leafOriginal=leafImage.material;leafMaterial=new Material(leafOriginal);leafImage.material=leafMaterial;leafImage.enabled=false;
        Set(1,0);group.alpha=isBlack?1:0;IsCovered=isBlack;
        group.interactable=false;group.blocksRaycasts=isBlack;
    }
    // Keep existing route entry points; "black" now means an opaque paper surface.
    public void ToBlack(string from="toShow"){
        revealRequested=false;isBlack=true;group.blocksRaycasts=true;
        if(IsCovered){if(motion!=null)StopCoroutine(motion);motion=null;Set(1,0);group.alpha=1;return;}
        if(covering)return;
        if(motion!=null)StopCoroutine(motion);
        motion=StartCoroutine(playPageTurn?Cover():CaptureCurrentView());
    }
    public void ToClear(string from="toClear"){
        isBlack=false;
        if(motion!=null&&!IsCovered){revealRequested=true;return;}
        if(group.alpha==0){group.blocksRaycasts=false;return;}
        if(motion!=null)StopCoroutine(motion);
        motion=StartCoroutine(Reveal());
    }
    public void ForceClear(){ToClear();}
    public IEnumerator WaitUntilCovered(){while(covering)yield return null;}
    void Set(float turn,float reveal){
        material.SetFloat("_PaperBlend",!playPageTurn&&captured?0:1);material.SetFloat("_Reveal",reveal);
        material.SetFloat("_Aspect",(float)Screen.width/Mathf.Max(1,Screen.height));
        if(leaf.Following){leaf.Sample(turn);leafMaterial.SetFloat("_PaperBlend",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.52f,turn)));}
    }
    IEnumerator CaptureCurrentView(){
        covering=true;IsCovered=false;
        // Keep the outgoing UI visible until its rendered frame can cover the route change.
        yield return new WaitForEndOfFrame();
        var next=ScreenCapture.CaptureScreenshotAsTexture();
        ReleaseCapture();captured=next;page.texture=captured;
        Set(1,0);group.alpha=1;IsCovered=true;covering=false;motion=null;
        if(revealRequested){revealRequested=false;motion=StartCoroutine(Reveal());}
    }
    IEnumerator Cover(){
        covering=true;
        // Capture the outgoing rendered UI before covering it. No scene or UI objects are rebuilt.
        yield return new WaitForEndOfFrame();
        var previous=captured;captured=ScreenCapture.CaptureScreenshotAsTexture();
        leafImage.texture=captured;if(previous)Destroy(previous);
        leafMaterial.SetFloat("_Reveal",0);leafMaterial.SetFloat("_Aspect",(float)Screen.width/Mathf.Max(1,Screen.height));
        leafImage.enabled=true;leaf.Begin();
        group.alpha=1;Set(0,0);
        for(float t=0;t<coverDuration;t+=Time.unscaledDeltaTime){Set(Mathf.SmoothStep(0,1,t/coverDuration),0);yield return null;}
        Set(1,0);leaf.End();leafImage.enabled=false;IsCovered=true;covering=false;motion=null;
        if(revealRequested){revealRequested=false;motion=StartCoroutine(Reveal());}
    }
    IEnumerator Reveal(){
        group.blocksRaycasts=true;
        // Keep the destination hidden until its first complete rendered frame exists.
        yield return new WaitForEndOfFrame();
        for(float t=0;t<revealDuration;t+=Time.unscaledDeltaTime){Set(1,Mathf.SmoothStep(0,1,t/revealDuration));yield return null;}
        Set(1,1);group.alpha=0;group.blocksRaycasts=false;IsCovered=false;motion=null;ReleaseCapture();
    }
    void ReleaseCapture(){if(page)page.texture=null;if(leaf)leaf.End();if(leafImage){leafImage.enabled=false;leafImage.texture=null;}if(captured)Destroy(captured);captured=null;}
    void OnDisable(){if(motion!=null)StopCoroutine(motion);motion=null;revealRequested=covering=false;ReleaseCapture();if(group){group.alpha=0;group.blocksRaycasts=false;}isBlack=IsCovered=false;}
    void OnDestroy(){if(page)page.material=original;if(material)Destroy(material);if(leafImage)leafImage.material=leafOriginal;if(leafMaterial)Destroy(leafMaterial);if(instance==this)instance=null;}
}
