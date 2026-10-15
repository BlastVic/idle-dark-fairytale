using UnityEngine;
using UnityEngine.UI;

// HUD nodes are authored in the prefab. Supports both Spine and production particle assets.
public sealed class SpineEffectsTest : MonoBehaviour
{
    public BookPetSpineEffect[] effects;
    public GameObject[] particleEffects;
    public Bounds[] particleBounds;
    public float[] particleDurations;
    public string[] displayNames;
    public Button[] selectionButtons;
    public Button replayButton, pauseButton, stepButton, loopButton, backgroundButton;
    public Slider widthSlider, speedSlider, timeSlider;
    public Text title, status, widthText, speedText, pauseText, loopText;
    public Camera previewCamera;
    public SpriteRenderer backdrop;
    public RectTransform safeRoot;
    public Transform effectAnchor;
    public BookPetSpineEffect Current { get; private set; }
    public GameObject CurrentParticle { get; private set; }
    public int Selected { get; private set; }
    public bool Paused { get; private set; }
    public bool Loop { get; private set; } = true;
    public float TimePosition { get; private set; }
    public int Count => effects.Length+(particleEffects==null?0:particleEffects.Length);
    public float Duration => Current?Current.duration:CurrentParticle?particleDurations[Selected-effects.Length]:1;
    public Bounds MotionBounds => Current?Current.motionBounds:particleBounds[Selected-effects.Length];
    public Transform Visual => Current?Current.skeleton.transform:CurrentParticle?CurrentParticle.transform:null;
    ParticleSystem[] systems;
    float clock;
    void Start()
    {
        for(int i=0;i<selectionButtons.Length;i++){int index=i;selectionButtons[i].onClick.AddListener(()=>Select(index));}
        replayButton.onClick.AddListener(Replay);
        pauseButton.onClick.AddListener(()=>SetPaused(!Paused));
        stepButton.onClick.AddListener(()=>Seek(TimePosition+1f/30));
        loopButton.onClick.AddListener(()=>{Loop=!Loop;RefreshLabels();});
        backgroundButton.onClick.AddListener(()=>backdrop.gameObject.SetActive(!backdrop.gameObject.activeSelf));
        widthSlider.onValueChanged.AddListener(_=>ApplySize());
        speedSlider.onValueChanged.AddListener(_=>RefreshLabels());
        timeSlider.onValueChanged.AddListener(Seek);
        Select(0);
    }
    public void Select(int index)
    {
        if(index<0||index>=Count)return;
        if(Current){Current.gameObject.SetActive(false);Destroy(Current.gameObject);}
        if(CurrentParticle){CurrentParticle.SetActive(false);Destroy(CurrentParticle);}
        Current=null;CurrentParticle=null;systems=null;Selected=index;
        if(index<effects.Length){Current=Instantiate(effects[index],effectAnchor);Current.transform.localPosition=Vector3.zero;Current.Play(1,10,0);}
        else{
            CurrentParticle=Instantiate(particleEffects[index-effects.Length],effectAnchor);
            foreach(var timer in CurrentParticle.GetComponentsInChildren<DisableDestroyOnTimer>(true)){timer.CancelInvoke();timer.enabled=false;}
            systems=CurrentParticle.GetComponentsInChildren<ParticleSystem>(true);
            for(int i=0;i<systems.Length;i++){var ps=systems[i];ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(123+i);}
        }
        timeSlider.maxValue=Duration;ApplySize();Replay();
        for(int i=0;i<selectionButtons.Length;i++)selectionButtons[i].GetComponent<Image>().color=i==index?new Color(.38f,.16f,.11f):new Color(.14f,.17f,.20f);
    }
    public void Replay(){clock=0;Paused=false;Sample(0);RefreshLabels();}
    public void SetPaused(bool value){Paused=value;RefreshLabels();}
    public void Seek(float seconds){if(!Visual)return;Paused=true;clock=Mathf.Clamp(seconds,0,Duration);Sample(clock);RefreshLabels();}
    void Update()
    {
        var safe=Screen.safeArea;safeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);safeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
        previewCamera.orthographicSize=5/Mathf.Min(previewCamera.aspect,9f/16);
        if(backdrop&&backdrop.sprite){float h=previewCamera.orthographicSize*2;backdrop.transform.localScale=Vector3.one*Mathf.Max(h/backdrop.sprite.bounds.size.y,h*previewCamera.aspect/backdrop.sprite.bounds.size.x);}
        if(!Visual||Paused)return;clock+=Time.unscaledDeltaTime*speedSlider.value;
        if(Loop&&clock>Duration+.35f)clock=0;Sample(Mathf.Min(clock,Duration));
    }
    void ApplySize()
    {
        if(!Visual)return;float fit=10*widthSlider.value/100/Mathf.Max(.01f,MotionBounds.size.x);
        Visual.localScale=Vector3.one*fit;Visual.localPosition=-MotionBounds.center*fit;
        if(CurrentParticle)Sample(TimePosition);RefreshLabels();
    }
    void Sample(float seconds)
    {
        TimePosition=seconds;
        if(Current){var sk=Current.skeleton;sk.Skeleton.SetToSetupPose();sk.AnimationState.SetAnimation(0,"texiao",false).TrackTime=seconds;sk.AnimationState.Apply(sk.Skeleton);sk.Skeleton.UpdateWorldTransform();sk.LateUpdate();}
        else if(systems!=null)foreach(var ps in systems){ps.Simulate(seconds,false,true,true);ps.Pause(false);}
        timeSlider.SetValueWithoutNotify(seconds);
        status.text=$"{seconds:0.000} / {Duration:0.000} s · "+(Current?"Spine":"Particle System")+"\n"+(Current?"独立特效预览 · 不含宠物动作与战斗弹道":"通用战斗特效 · 与正式战斗共用 Prefab");
    }
    void RefreshLabels()
    {
        title.text=displayNames[Selected]+" / "+(Selected<effects.Length?effects[Selected].name:particleEffects[Selected-effects.Length].name);
        widthText.text=$"宽度 {widthSlider.value:0}%（9:16 核心区）";speedText.text=$"速度 {speedSlider.value:0.00}×";
        pauseText.text=Paused?"继续":"暂停";loopText.text=Loop?"循环：开":"循环：关";
    }
}
