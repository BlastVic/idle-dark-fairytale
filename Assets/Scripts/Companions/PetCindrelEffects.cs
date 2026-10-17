using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

// Presentation only: PetCindrel remains the sole authority for hit timing and damage.
public sealed class PetCindrelEffects : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public PetCindrelSpineEffect emberBolt, emberImpact, clawArc, ashDust;
    [Tooltip("Socket offset in imported skeleton units; follows the animated jaw.")]
    public Vector2 mouthOffset = new Vector2(.42f, .08f);
    [Range(.02f,.4f)] public float boltWidth=.18f;
    [Range(.03f,.6f)] public float impactWidth=.54f;
    [Range(.03f,1f)] public float clawWidth=.72f;
    [Range(.45f,.65f)] public float lobLaunchTime=.50f;
    [Range(.1f,.4f)] public float lobApexHeight=.22f;
    [Tooltip("Offset from front-paw contact midpoint, as a fraction of core screen width.")]
    public Vector2 dustContactOffset=new Vector2(.04f,.05f);
    public bool previewOnly;
    public Camera previewCamera;
    public int LaunchedCount {get;private set;}
    public int ImpactCount {get;private set;}
    public int ActiveCount => live.Count;
    sealed class Live {public PetCindrelSpineEffect fx;public float expires;}
    readonly List<Live> live=new List<Live>();
    readonly List<PetCindrelSpineEffect> travelling=new List<PetCindrelSpineEffect>();
    TrackEntry track;
    int loopIndex;
    string trackedAnimation;
    float previousTrackTime;
    Bone mouth, head;
    Actor_Base aim;
    Vector3 aimPoint, startPoint;
    float launchTime, hitTime;
    bool launched,hit,dustLaunched;
    Camera battleCamera;
    PetCindrelCompanion companion;
    public const float DustWidthFraction=.39f;
    public static float CoreWorldWidth(Camera camera){var size=DarkFairytaleBattleStyle.WorldSize(camera);return Mathf.Min(size.x,size.y*9f/16f);}
    public float EffectWorldWidth(PetCindrelSpineEffect effect,Camera camera)
    {
        float fraction=effect==emberBolt?boltWidth:effect==emberImpact?impactWidth:effect==clawArc?clawWidth:effect==ashDust?DustWidthFraction:0;
        return CoreWorldWidth(camera)*fraction;
    }
    float ScreenWorldWidth {
        get {
            if(previewOnly&&previewCamera)return CoreWorldWidth(previewCamera);
            if(battleCamera)return CoreWorldWidth(battleCamera);
            return Mathf.Max(.5f,skeleton.GetComponent<MeshRenderer>().bounds.size.x*2f);
        }
    }
    void Start(){Initialize(skeleton);}
    public void Initialize(SkeletonAnimation actor){
        if(skeleton)skeleton.AnimationState.Event-=OnEvent;
        skeleton=actor;if(!skeleton)return;skeleton.Initialize(false);
        mouth=skeleton.Skeleton.FindBone("Front_Mouth_02");head=skeleton.Skeleton.FindBone("Head01");
        companion=GetComponent<PetCindrelCompanion>();
        if(!previewOnly&&LevelController.Instance&&LevelController.Instance.AssetManager)battleCamera=LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>();
        skeleton.AnimationState.Event+=OnEvent;track=null;
    }
    public void CopyFrom(PetCindrelEffects source,SkeletonAnimation actor){
        emberBolt=source.emberBolt;emberImpact=source.emberImpact;clawArc=source.clawArc;ashDust=source.ashDust;
        mouthOffset=source.mouthOffset;boltWidth=source.boltWidth;impactWidth=source.impactWidth;clawWidth=source.clawWidth;
        dustContactOffset=source.dustContactOffset;lobLaunchTime=source.lobLaunchTime;lobApexHeight=source.lobApexHeight;
        previewOnly=true;Initialize(actor);
    }
    void OnEvent(TrackEntry entry,Spine.Event e){if(previewOnly&&e.Data.Name=="OnHit")Hit(entry.Animation.Name,null);}
    public Vector3 MouthPoint(){
        if(mouth==null)return skeleton.transform.position;
        float x=mouth.WorldX+mouth.A*mouthOffset.x+mouth.B*mouthOffset.y;
        float y=mouth.WorldY+mouth.C*mouthOffset.x+mouth.D*mouthOffset.y;
        return skeleton.transform.TransformPoint(new Vector3(x,y,0));
    }
    public Vector3 LobMouthPoint(){
        if(head==null||mouth==null)return MouthPoint();
        // Midpoint between the upper lip and lower jaw tip, in imported skeleton units.
        head.LocalToWorld(.60f,-.06f,out float ux,out float uy);
        mouth.LocalToWorld(.53f,.06f,out float lx,out float ly);
        return skeleton.transform.TransformPoint(new Vector3((ux+lx)*.5f,(uy+ly)*.5f,0));
    }
    public static float HitTime(Spine.Animation animation){
        foreach(var timeline in animation.Timelines)
            if(timeline is EventTimeline events)
                foreach(var e in events.Events)if(e.Data.Name=="OnHit")return e.Time;
        return -1;
    }
    Vector3 PreviewAim(Vector3 from){
        if(!previewCamera)return from+Vector3.right*ScreenWorldWidth*.21f;
        var point=previewCamera.WorldToViewportPoint(from);point.x=.84f;point.y=Mathf.Clamp(point.y-.06f,.31f,.72f);
        return previewCamera.ViewportToWorldPoint(point);
    }
    static Vector3 TargetPoint(Actor_Base target){var sk=target.GetComponentInChildren<SkeletonAnimation>();return sk?sk.GetComponent<MeshRenderer>().bounds.center:target.transform.position;}
    void LateUpdate(){
        for(int i=live.Count-1;i>=0;i--)if(!live[i].fx||Time.time>=live[i].expires){if(live[i].fx)Destroy(live[i].fx.gameObject);live.RemoveAt(i);}
        if(!skeleton||!skeleton.valid)return;
        if(companion&&companion.Exiting){Clear();return;}
        var current=skeleton.AnimationState.GetCurrent(0);
        if(current!=track || (current!=null && (current.Animation.Name!=trackedAnimation || current.TrackTime<previousTrackTime))){
            if(!hit)Clear();
            track=current;loopIndex=0;launched=hit=dustLaunched=false;aim=null;
            if(track==null)return;
            string name=track.Animation.Name;trackedAnimation=name;hitTime=(name=="Attack1"||name=="Attack2")?HitTime(track.Animation):-1;
            launchTime=name=="Attack1"?lobLaunchTime:hitTime-.26f;
        }
        previousTrackTime=current!=null?current.TrackTime:0;
        if(hitTime<0||track==null)return;
        float trackTime=track.TrackTime;
        if(track.Loop&&track.Animation.Duration>0){int cycle=Mathf.FloorToInt(trackTime/track.Animation.Duration);trackTime%=track.Animation.Duration;if(cycle!=loopIndex){loopIndex=cycle;launched=hit=dustLaunched=false;aim=null;travelling.Clear();}}
        if(track.Animation.Name=="Attack2"&&!dustLaunched&&trackTime>=1.0333f&&trackTime<hitTime)SpawnFootDust();
        if(hit)return;
        if(!launched&&trackTime>=launchTime&&trackTime<hitTime){
            if(!previewOnly){var list=GameManager.single?GameManager.single.GetLivingEnemies(false):null;if(list==null||list.Count==0)return;aim=list[0];}
            startPoint=track.Animation.Name=="Attack1"?LobMouthPoint():MouthPoint();aimPoint=aim?TargetPoint(aim):PreviewAim(startPoint);
            launched=true;LaunchedCount++;
            if(track.Animation.Name=="Attack1")travelling.Add(SpawnProjectile(startPoint,1));
        }
        if(launched){
            if(!previewOnly&&(!aim||aim.isDying)){
                var remaining=GameManager.single.GetLivingEnemies(false);
                if(remaining.Count==0){Clear();hit=true;return;}
                aim=remaining[0];
            }
            if(aim&&!aim.isDying)aimPoint=TargetPoint(aim);
            float t=Mathf.Clamp01((trackTime-launchTime)/(hitTime-launchTime));
            Vector3 from=startPoint;
            for(int i=0;i<travelling.Count;i++){
                var fx=travelling[i];if(!fx)continue;fx.skeleton.timeScale=skeleton.timeScale;
                // Single fireball follows the retained lob trajectory.
                float apex=Mathf.Max(from.y,aimPoint.y)+ScreenWorldWidth*lobApexHeight;
                float rise=apex-from.y,fall=apex-aimPoint.y;
                float v=2*(rise+Mathf.Sqrt(rise*fall));
                float gravity=2*(v+from.y-aimPoint.y);
                fx.transform.position=new Vector3(Mathf.Lerp(from.x,aimPoint.x,t),from.y+v*t-.5f*gravity*t*t,from.z);
                var delta=new Vector3(aimPoint.x-from.x,v-gravity*t,0);
                fx.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            }
        }
    }
    PetCindrelSpineEffect SpawnProjectile(Vector3 origin,float size){
        var fx=Spawn(emberBolt,origin,ScreenWorldWidth*boltWidth*size,1,0);
        if(!fx)return null;
        var core=fx.skeleton.Skeleton.FindBone("qiu");
        if(core!=null)fx.skeleton.transform.localPosition=-new Vector3(core.WorldX,core.WorldY,0)*fx.skeleton.transform.localScale.x;
        live[live.Count-1].expires=float.PositiveInfinity;
        fx.skeleton.AnimationState.GetCurrent(0).Loop=true;
        return fx;
    }
    public void Hit(string animation,Actor_Base target){
        if(companion&&companion.Exiting)return;
        if(animation!="Attack1"&&animation!="Attack2")return;
        if(!previewOnly&&!target){Clear();return;}
        if(!launched){startPoint=MouthPoint();aimPoint=target?TargetPoint(target):PreviewAim(startPoint);}
        else if(target)aimPoint=TargetPoint(target);
        hit=true;ImpactCount++;
        foreach(var fx in travelling)if(fx)Destroy(fx.gameObject);travelling.Clear();
        if(animation=="Attack1")Spawn(emberImpact,aimPoint,ScreenWorldWidth*impactWidth,.65f,0);
        else if(animation=="Attack2"){
            Spawn(clawArc,aimPoint,ScreenWorldWidth*clawWidth,.7f,0);
            if(!dustLaunched)SpawnFootDust();
        }
    }
    void SpawnFootDust(){
        dustLaunched=true;
        var left=skeleton.Skeleton.FindBone("Front_Hand_L");
        var right=skeleton.Skeleton.FindBone("Front_Hand_R");
        Vector3 point=skeleton.transform.position;
        if(left!=null&&right!=null){
            left.LocalToWorld(left.Data.Length*.65f,0,out float lx,out float ly);
            right.LocalToWorld(right.Data.Length*.65f,0,out float rx,out float ry);
            point=skeleton.transform.TransformPoint(new Vector3((lx+rx)*.5f,(ly+ry)*.5f,0));
        }
        point+=new Vector3(dustContactOffset.x,dustContactOffset.y,0)*ScreenWorldWidth;
        var dust=Spawn(ashDust,point,ScreenWorldWidth*DustWidthFraction,.75f,.80f);
        // Ground dust belongs to the companion's depth and sits beneath its body.
        if(dust)dust.skeleton.GetComponent<MeshRenderer>().sortingOrder=skeleton.GetComponent<MeshRenderer>().sortingOrder-1;
    }
    PetCindrelSpineEffect Spawn(PetCindrelSpineEffect prefab,Vector3 position,float width,float speed,float seek){
        if(!prefab)return null;
        var fx=Instantiate(prefab,position,Quaternion.identity);fx.transform.SetParent(transform,true);
        int order=skeleton.GetComponent<MeshRenderer>().sortingOrder+2;
        if(aim){var r=aim.GetComponentInChildren<MeshRenderer>();if(r)order=Mathf.Max(order,r.sortingOrder+2);}
        fx.Play(width,order,speed);fx.skeleton.GetComponent<MeshRenderer>().sortingLayerID=skeleton.GetComponent<MeshRenderer>().sortingLayerID;if(seek>0){fx.skeleton.AnimationState.GetCurrent(0).TrackTime=seek;fx.skeleton.Update(0);fx.skeleton.LateUpdate();}
        live.Add(new Live{fx=fx,expires=Time.time+Mathf.Max(.05f,(fx.duration-seek)/speed)});return fx;
    }
    public void Clear(){foreach(var f in live)if(f.fx)Destroy(f.fx.gameObject);live.Clear();travelling.Clear();}
    void OnDisable(){Clear();}
    void OnDestroy(){if(skeleton&&skeleton.AnimationState!=null)skeleton.AnimationState.Event-=OnEvent;}
}
