using UnityEngine;
using Spine.Unity;
using UnityEngine.UI;

// HUD nodes are authored in the prefab. Supports both Spine and production particle assets.
public sealed class SpineEffectsTest : MonoBehaviour
{
    public PetCindrelSpineEffect[] effects;
    public PetCindrelEffects petBattleEffects;
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
    public SkeletonAnimation hero, enemy;
    public DarkFairytaleBattleStyle battleStyle;
    public float battleWorldHeight=19.48223f;
    public string sweepHeroAnimation="Skill5";
    public float sweepCastTime=.56f;
    public float sweepHitTime=.69f;
    public float sweepFlyDelay=.62f;
    public float sweepFlySpeed=26f;
    public Vector2 sweepForwardOffset=new Vector2(1.8f,0);
    public Vector2 sweepCastOffset=new Vector2(.25f,-.2f);
    Bounds restingHeroBounds;
    bool SweepPreview=>Selected==effects.Length+2;
    float layoutAspect;
    void MatchBattleScale()
    {
        if(!battleStyle||!hero||!enemy)return;
        var world=DarkFairytaleBattleStyle.WorldSize(previewCamera);
        FitActor(hero,battleStyle.playerSize,new Vector2(.23f,.40f),world);
        FitActor(enemy,battleStyle.enemySize[0],new Vector2(.71f,.40f),world);
        restingHeroBounds=hero.GetComponent<Renderer>().bounds;
    }
    void FitActor(SkeletonAnimation actor,Vector2 size,Vector2 feet,Vector2 world)
    {
        actor.AnimationState.ClearTracks();actor.Skeleton.SetToSetupPose();actor.Update(0);actor.LateUpdate();
        var renderer=actor.GetComponent<Renderer>();
        actor.transform.localScale*=DarkFairytaleBattleStyle.DisplayFitScale(renderer.bounds.size,world,size);
        var b=renderer.bounds;var target=DarkFairytaleBattleStyle.ViewportPoint(previewCamera,feet);
        actor.transform.position+=target-new Vector3(b.center.x,b.min.y,0);
        actor.AnimationState.SetAnimation(0,"Idle1",true);
    }
    public float[] effectWidths={1.2f,1.7f,2.2f,2.4f,3.4f,1.2f,2.1f,3.8f,1.6f,3.0f,2.0f,1.5f};
    public float BaseWidth => Selected<effects.Length?petBattleEffects.EffectWorldWidth(effects[Selected],previewCamera):effectWidths[Selected];
    public bool OriginalHit => Selected==effects.Length;
    Vector3 TargetPosition()
    {
        if(!hero||!enemy)return effectAnchor.position;
        var h=hero.GetComponent<Renderer>().bounds;var e=enemy.GetComponent<Renderer>().bounds;
        switch(Selected){
            case 0: return Vector3.Lerp(h.center,e.center,Mathf.Clamp01(TimePosition/Mathf.Max(.01f,Duration)));
            case 3: case 7: return new Vector3(e.center.x,e.min.y+.25f,0);
            case 8: return new Vector3(h.center.x,h.min.y,0);
            case 6: case 9: case 11: return h.center;
            case 10: return new Vector3(e.center.x,e.center.y+.65f,0);
            default: return e.center;
        }
    }
    public PetCindrelSpineEffect Current { get; private set; }
    public GameObject CurrentParticle { get; private set; }
    public int Selected { get; private set; }
    public bool Paused { get; private set; }
    public bool Loop { get; private set; } = true;
    public float TimePosition { get; private set; }
    public int Count => effects.Length+(particleEffects==null?0:particleEffects.Length);
    public float Duration => Current?Current.duration:CurrentParticle?(SweepPreview?2.3f:particleDurations[Selected-effects.Length]):1;
    public Bounds MotionBounds => Current?Current.motionBounds:particleBounds[Selected-effects.Length];
    public Transform Visual => Current?Current.skeleton.transform:CurrentParticle?CurrentParticle.transform:null;
    public CommonSpineBattleEffect CommonSpine {get;private set;}
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
    public CommonSpineBattleEffect CastPreview {get;private set;}
    public void Select(int index)
    {
        if(index<0||index>=Count)return;
        if(Current){Current.gameObject.SetActive(false);Destroy(Current.gameObject);}
        if(CurrentParticle){CurrentParticle.SetActive(false);Destroy(CurrentParticle);}
        if(CastPreview){CastPreview.gameObject.SetActive(false);Destroy(CastPreview.gameObject);CastPreview=null;}
        Current=null;CurrentParticle=null;CommonSpine=null;systems=null;Selected=index;
        if(index<effects.Length){Current=Instantiate(effects[index],effectAnchor);Current.transform.localPosition=Vector3.zero;Current.Play(BaseWidth,20,0);}
        else{
            CurrentParticle=Instantiate(particleEffects[index-effects.Length],effectAnchor);
            CommonSpine=CurrentParticle.GetComponent<CommonSpineBattleEffect>();if(CommonSpine){CommonSpine.preview=true;CommonSpine.skeleton.timeScale=0;if(SweepPreview)CommonSpine.skeleton.UpdateLocal+=StabilizeSweepBones;}
            foreach(var timer in CurrentParticle.GetComponentsInChildren<DisableDestroyOnTimer>(true)){timer.CancelInvoke();timer.enabled=false;}
            var depth=CurrentParticle.GetComponent<CursedBattleParticleLayer>();if(depth)depth.enabled=false;
            if(!OriginalHit)foreach(var r in CurrentParticle.GetComponentsInChildren<ParticleSystemRenderer>(true)){r.sortingLayerName="Character";r.sortingOrder=20;}
            systems=CurrentParticle.GetComponentsInChildren<ParticleSystem>(true);
            for(int i=0;i<systems.Length;i++){var ps=systems[i];ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(123+i);}
        }
        if(hero){hero.timeScale=SweepPreview?0:1;hero.AnimationState.ClearTracks();hero.Skeleton.SetToSetupPose();hero.AnimationState.SetAnimation(0,"Idle1",true);hero.Update(0);hero.LateUpdate();restingHeroBounds=hero.GetComponent<Renderer>().bounds;}
        if(index==7&&hero)CastPreview=CursedSweepCast.Spawn(hero.GetComponent<Renderer>(),true);
        timeSlider.maxValue=Duration;ApplySize();Replay();
        for(int i=0;i<selectionButtons.Length;i++)selectionButtons[i].GetComponent<Image>().color=i==index?new Color(.38f,.16f,.11f):new Color(.14f,.17f,.20f);
    }
    public void Replay(){clock=0;Paused=false;Sample(0);RefreshLabels();}
    public void SetPaused(bool value){Paused=value;RefreshLabels();}
    public void Seek(float seconds){if(!Visual)return;Paused=true;clock=Mathf.Clamp(seconds,0,Duration);Sample(clock);RefreshLabels();}
    void Update()
    {
        var safe=Screen.safeArea;safeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);safeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
        previewCamera.orthographicSize=battleStyle?battleWorldHeight*.5f:5/Mathf.Min(previewCamera.aspect,9f/16);
        if(battleStyle&&!Mathf.Approximately(layoutAspect,previewCamera.aspect)){layoutAspect=previewCamera.aspect;MatchBattleScale();if(Visual)ApplySize();}
        if(backdrop&&backdrop.sprite){float h=previewCamera.orthographicSize*2;backdrop.transform.localScale=Vector3.one*Mathf.Max(h/backdrop.sprite.bounds.size.y,h*previewCamera.aspect/backdrop.sprite.bounds.size.x);}
        if(!Visual||Paused)return;clock+=Time.unscaledDeltaTime*speedSlider.value;
        if(Loop&&clock>Duration+.35f)clock=0;Sample(Mathf.Min(clock,Duration));
    }
    void ApplySize()
    {
        if(!Visual)return;float fit=BaseWidth*widthSlider.value/100/Mathf.Max(.01f,MotionBounds.size.x);
        if(CurrentParticle){
            // Keep the original prefab transform and child particle scales. Do not normalize its bounds.
            Visual.localScale=particleEffects[Selected-effects.Length].transform.localScale*(widthSlider.value/100);
            Visual.localPosition=Vector3.zero;
        }else{Visual.localScale=Vector3.one*fit;Visual.localPosition=-MotionBounds.center*fit;}
        if(CurrentParticle)Sample(TimePosition);RefreshLabels();
    }
    void SampleHero(float seconds)
    {
        if(!SweepPreview||!hero)return;
        hero.timeScale=0;hero.AnimationState.ClearTracks();hero.Skeleton.SetToSetupPose();
        var attack=hero.Skeleton.Data.FindAnimation(sweepHeroAnimation);
        bool finished=attack!=null&&seconds>=attack.Duration;
        hero.AnimationState.SetAnimation(0,finished?"Idle1":sweepHeroAnimation,finished).TrackTime=finished?seconds-attack.Duration:seconds;
        hero.AnimationState.Apply(hero.Skeleton);hero.Skeleton.UpdateWorldTransform();hero.LateUpdate();
    }
    Vector3 ScytheCenter()
    {
        var bone=hero.Skeleton.FindBone("scythe_projection")??hero.Skeleton.FindBone("weapon");
        float x,y;bone.LocalToWorld(.325f,-.51f,out x,out y);
        return hero.transform.TransformPoint(new Vector3(x,y,0));
    }
    void StabilizeSweepBones(ISkeletonAnimation animated)
    {
        if(!SweepPreview||!CommonSpine)return;
        var skeleton=CommonSpine.skeleton.Skeleton;
        var ring=skeleton.FindBone("xg1");ring.X=-94f*.01f;ring.Y=60f*.01f;
        foreach(var slot in skeleton.Slots)if(slot.Data.Name.StartsWith("yw1"))slot.Attachment=null;
    }
    void SampleSweep(float seconds,Vector3 emission)
    {
        float time=seconds-sweepHitTime;
        var sk=CommonSpine.skeleton;
        sk.transform.localPosition=Vector3.zero;
        sk.gameObject.SetActive(time>=0);
        float sample=time<sweepFlyDelay?Mathf.Max(0,time):.5667f+Mathf.Repeat(time-sweepFlyDelay,1f/3f);
        CommonSpine.Sample(sample);
        // The source places its spin on the right of the formation. Keep both at the same origin.
        StabilizeSweepBones(sk);
        sk.Skeleton.UpdateWorldTransform();sk.LateUpdate();
        float scale=CurrentParticle.transform.localScale.x;
        Vector3 origin=emission+(Vector3)sweepForwardOffset-new Vector3(-94*.0048f,60*.0048f,0)*scale;
        effectAnchor.position=origin+Vector3.right*(Mathf.Max(0,time-sweepFlyDelay)*sweepFlySpeed);
        var renderer=sk.GetComponent<Renderer>();renderer.sortingLayerName="Character";renderer.sortingOrder=70;
        // Stop only after the entire rotating blade has crossed the right edge.
        float right=previewCamera.ViewportToWorldPoint(new Vector3(1,.5f,-previewCamera.transform.position.z)).x;
        if(time>=sweepFlyDelay&&renderer.bounds.min.x>right)sk.gameObject.SetActive(false);
    }
    void Sample(float seconds)
    {
        TimePosition=seconds;
        Vector3 emission=Vector3.zero;
        if(SweepPreview){SampleHero(sweepHitTime);emission=ScytheCenter();}
        SampleHero(seconds);
        if(CastPreview){
            float castTime=seconds-sweepCastTime;
            CastPreview.gameObject.SetActive(castTime>=0&&castTime<=CastPreview.duration);
            CursedSweepCast.Place(CastPreview,hero.GetComponent<Renderer>());
            CastPreview.transform.position=ScytheCenter()+(Vector3)sweepCastOffset-CastPreview.motionBounds.center*(widthSlider.value/100);
            CastPreview.transform.localScale=Resources.Load<CommonSpineBattleEffect>("Effects/DongzhuoCast").transform.localScale*(widthSlider.value/100);
            CastPreview.Sample(Mathf.Max(0,castTime));
        }
        effectAnchor.position=TargetPosition();
        if(CurrentParticle){var layer=CurrentParticle.GetComponent<CursedBattleParticleLayer>();if(layer&&layer.placement!=CursedBattleParticleLayer.Placement.Authored){var actor=layer.alignToPlayer?hero:enemy;if(actor)effectAnchor.position=layer.PositionFor(actor.GetComponent<Renderer>().bounds);}}
        if(CommonSpine&&!SweepPreview){var actor=CommonSpine.targetEnemy?enemy:hero;if(actor)effectAnchor.position=CommonSpine.PositionFor(actor.GetComponent<Renderer>().bounds);}
        if(Current){var sk=Current.skeleton;sk.Skeleton.SetToSetupPose();sk.AnimationState.SetAnimation(0,"texiao",false).TrackTime=seconds;sk.AnimationState.Apply(sk.Skeleton);sk.Skeleton.UpdateWorldTransform();sk.LateUpdate();}
        else if(CommonSpine){
            if(SweepPreview)SampleSweep(seconds,emission);
            else {CommonSpine.skeleton.gameObject.SetActive(true);CommonSpine.Sample(seconds);}
        }
        else if(systems!=null)foreach(var ps in systems){ps.Simulate(seconds,false,true,true);ps.Pause(false);}
        timeSlider.SetValueWithoutNotify(seconds);
        status.text=$"{seconds:0.000} / {Duration:0.000} s · "+(Current||CommonSpine?"Spine":"Particle System")+"\n"+(Current?"角色位置预览 · 火球由小红帽一侧飞向敌人":"角色位置预览 · 与正式战斗共用 Prefab");
    }
    void RefreshLabels()
    {
        title.text=displayNames[Selected]+" / "+(Selected<effects.Length?effects[Selected].name:particleEffects[Selected-effects.Length].name);
        widthText.text=$"特效大小 {widthSlider.value:0}%";speedText.text=$"速度 {speedSlider.value:0.00}×";
        pauseText.text=Paused?"继续":"暂停";loopText.text=Loop?"循环：开":"循环：关";
    }
}
