using System.Collections;
using UnityEngine;

/// <summary>Prefab-authored full-screen ink veil. Legacy route API retained without Animator dependencies.</summary>
public class TransitionCanvas : MonoBehaviour
{
    static TransitionCanvas instance;
    public static TransitionCanvas single => instance ? instance : (instance=FindFirstObjectByType<TransitionCanvas>());
    public CanvasGroup group;
    [Min(.01f)] public float coverDuration=.3f, revealDuration=.4f;
    public bool isBlack;
    Coroutine motion;
    void Awake(){instance=this;group.alpha=isBlack?1:0;group.interactable=false;group.blocksRaycasts=isBlack;}
    public void ToBlack(string from="toShow"){isBlack=true;Fade(1,coverDuration);}
    public void ToClear(string from="toClear"){isBlack=false;Fade(0,revealDuration);}
    public void ForceClear(){isBlack=false;Fade(0,revealDuration);}
    void Fade(float target,float duration){if(motion!=null)StopCoroutine(motion);group.blocksRaycasts=true;motion=StartCoroutine(Animate(target,duration));}
    IEnumerator Animate(float target,float duration){float start=group.alpha;float elapsed=0;while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;group.alpha=Mathf.Lerp(start,target,Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,duration)));yield return null;}group.alpha=target;group.blocksRaycasts=target>0;motion=null;}
    void OnDisable(){if(motion!=null)StopCoroutine(motion);motion=null;if(group){group.alpha=0;group.blocksRaycasts=false;}isBlack=false;}
    void OnDestroy(){if(instance==this)instance=null;}
}
