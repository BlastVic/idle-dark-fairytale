using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared, unscaled popup transition over authored UGUI nodes.</summary>
public sealed class StorybookPopupMotion : MonoBehaviour
{
    public RectTransform motion;
    public CanvasGroup content, input;
    public Graphic[] dimmers;
    public float shadeDuration=.12f, windowDuration=.2f, travel=18;
    public bool Closing {get;private set;}
    public bool Animating {get;private set;}
    Color[] colors;
    Coroutine routine;
    float shade,window;
    void Awake(){colors=new Color[dimmers.Length];for(int i=0;i<colors.Length;i++)colors[i]=dimmers[i].color;}
    void OnEnable(){shade=window=0;Closing=false;Apply();routine=StartCoroutine(Animate(true));}
    void OnDisable(){if(routine!=null)StopCoroutine(routine);routine=null;Animating=Closing=false;if(input){input.blocksRaycasts=false;input.interactable=false;}if(motion)motion.anchoredPosition=Vector2.zero;}
    void Apply(){
        for(int i=0;i<dimmers.Length;i++){var c=colors[i];c.a*=shade;dimmers[i].color=c;}
        content.alpha=window;motion.anchoredPosition=new Vector2(0,-travel*(1-window));
    }
    IEnumerator Phase(bool background,float target,float duration){float start=background?shade:window;for(float t=0;t<duration;){t+=Time.unscaledDeltaTime;float value=Mathf.Lerp(start,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/duration)));if(background)shade=value;else window=value;Apply();yield return null;}if(background)shade=target;else window=target;Apply();}
    IEnumerator Animate(bool opening){
        Animating=true;input.alpha=1;input.blocksRaycasts=true;input.interactable=false;
        if(opening){yield return Phase(true,1,dimmers.Length>0?shadeDuration:0);yield return Phase(false,1,windowDuration);input.interactable=true;}
        else {yield return Phase(false,0,windowDuration);yield return Phase(true,0,dimmers.Length>0?shadeDuration:0);input.blocksRaycasts=false;gameObject.SetActive(false);}
        Animating=false;routine=null;
    }
    public static void Show(GameObject target){if(!target)return;var fx=target.GetComponent<StorybookPopupMotion>();if(!target.activeSelf){target.SetActive(true);return;}if(!fx)return;if(fx.routine!=null)fx.StopCoroutine(fx.routine);fx.Closing=false;fx.routine=fx.StartCoroutine(fx.Animate(true));}
    public static void Hide(GameObject target,bool immediate=false){if(!target)return;var fx=target.GetComponent<StorybookPopupMotion>();if(immediate||!fx||!target.activeInHierarchy){target.SetActive(false);return;}if(fx.Closing)return;fx.Closing=true;if(fx.routine!=null)fx.StopCoroutine(fx.routine);fx.routine=fx.StartCoroutine(fx.Animate(false));}
}
