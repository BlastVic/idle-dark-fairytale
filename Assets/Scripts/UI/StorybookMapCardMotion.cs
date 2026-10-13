using UnityEngine;

/// <summary>Animates only the saved card content, leaving responsive layout in charge of its frame.</summary>
public sealed class StorybookMapCardMotion : MonoBehaviour
{
    public RectTransform content;
    public CanvasGroup group;
    [Min(.01f)] public float duration = .24f;
    public float riseDistance = 18f;
    float elapsed;
    bool playing;
    public void Play() { elapsed=0;playing=true;Apply(0); }
    void Update()
    {
        if(!playing)return;
        elapsed+=Time.unscaledDeltaTime;
        float t=Mathf.Clamp01(elapsed/duration);Apply(1-Mathf.Pow(1-t,3));
        if(t>=1)playing=false;
    }
    void Apply(float t)
    {
        if(content)content.anchoredPosition=new Vector2(0,-riseDistance*(1-t));
        if(group)group.alpha=Mathf.Lerp(.25f,1,t);
    }
    void OnDisable(){playing=false;Apply(1);}
}
