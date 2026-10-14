using UnityEngine;

/// <summary>Slides the authored whole card within its clipped layout slot.</summary>
public sealed class StorybookMapCardMotion : MonoBehaviour
{
    public RectTransform content;
    public CanvasGroup group;
    [Min(.01f)] public float duration = .42f;
    public float riseDistance = 208f;
    public bool fullCardRise;
    float elapsed;
    bool playing;
    public void Play(){elapsed=0;playing=true;Apply(0);}
    void Update()
    {
        if(!playing)return;
        elapsed+=Time.unscaledDeltaTime;
        float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));Apply(1-Mathf.Pow(1-t,3));
        if(t>=1)playing=false;
    }
    void Apply(float t)
    {
        float distance=fullCardRise&&content?content.rect.height+24:riseDistance;
        if(content)content.anchoredPosition=new Vector2(0,-distance*(1-t));
        if(group){group.alpha=fullCardRise?1:Mathf.Lerp(.25f,1,t);group.blocksRaycasts=group.interactable=t>=1;}
    }
    void OnDisable(){playing=false;Apply(1);}
}
