using System.Collections;
using Spine.Unity;
using Scripts.Skills;
using UnityEngine;

// Shared behaviour for independent wolf pets; animation data remains identical to MobHexmaw.
public sealed class WolfPetCompanion : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public Vector2 viewportFeet = new Vector2(.28f,.38f);
    public Vector2 viewportSize = new Vector2(.432f,.144f);
    [Tooltip("NONE keeps the authored multiplier for independent preview/other wolf pets.")]
    public SkillsTypeEnum damageSkill = SkillsTypeEnum.NONE;
    public float damageMultiplier = 1f;
    public float attackDelay = .8f;
    public bool Exiting { get; private set; }
    Bounds setupBounds;
    Camera battleCamera;
    int width,height;
    bool pendingHit;
    void Awake() {
        skeleton.Initialize(false); skeleton.Skeleton.SetToSetupPose(); skeleton.Update(0); skeleton.LateUpdate();
        setupBounds=skeleton.GetComponent<MeshFilter>().sharedMesh.bounds;
        skeleton.AnimationState.Event+=OnEvent;
    }
    IEnumerator Start() {
        if(LevelController.Instance && LevelController.Instance.AssetManager)
            battleCamera=LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>();
        if (damageSkill != SkillsTypeEnum.NONE)
            damageMultiplier = SkillsController.Instance.GetSkillValues(
                SkillsController.Instance.GetSkillLevel(damageSkill), damageSkill)[0];
        Place();
        while(!Exiting) {
            if(Player.single && (Player.single.ab.isDying || Player.single.isCampPlayer)){Exit();yield break;}
            if(Player.single && GameManager.single && GameManager.single.GetLivingEnemies(false).Count>0) {
                pendingHit=true;
                var entry=skeleton.AnimationState.SetAnimation(0,"Attack1",false);
                yield return new WaitForSeconds(entry.Animation.Duration);
                pendingHit=false;
            }
            if(Exiting)yield break;
            skeleton.AnimationState.SetAnimation(0,"Idle1",true);
            yield return new WaitForSeconds(attackDelay);
        }
    }
    void LateUpdate(){if(!Exiting && Player.single && (Player.single.ab.isDying || Player.single.isCampPlayer))Exit();if(!Exiting && (width!=Screen.width||height!=Screen.height))Place();}
    public void Place(){
        if(!battleCamera)return;width=Screen.width;height=Screen.height;
        float scale=DarkFairytaleBattleStyle.DisplayFitScale(setupBounds.size,DarkFairytaleBattleStyle.WorldSize(battleCamera),viewportSize);
        transform.localScale=Vector3.one*scale;
        var feet=battleCamera.ViewportToWorldPoint(new Vector3(viewportFeet.x,viewportFeet.y,-battleCamera.transform.position.z));
        transform.position=new Vector3(feet.x-setupBounds.center.x*scale,feet.y-setupBounds.min.y*scale,0);
        var renderer=skeleton.GetComponent<MeshRenderer>();renderer.sortingLayerName="Character";renderer.sortingOrder=Mathf.RoundToInt((1-viewportFeet.y)*100)+1;
    }
    void OnEvent(Spine.TrackEntry entry,Spine.Event e){
        if(e.Data.Name!="OnHit" || entry.Animation.Name!="Attack1" || !pendingHit || Exiting)return;
        pendingHit=false;
        if(!Player.single || Player.single.ab.isDying || Player.single.isCampPlayer || damageMultiplier <= 0 || !GameManager.single)return;
        var enemies=GameManager.single.GetLivingEnemies(false);if(enemies.Count==0)return;
        enemies[0].Hit(Mathf.Max(1,Player.single.ab.currentStat.dmg*damageMultiplier*SkillsController.Instance.GetSummonDamageMultiplier(damageSkill))*Random.Range(.9f,1.1f),false,0,150,true);
    }
    public void Exit(){if(Exiting)return;Exiting=true;pendingHit=false;StopAllCoroutines();StartCoroutine(Leave());}
    IEnumerator Leave(){
        skeleton.Skeleton.ScaleX=-Mathf.Abs(skeleton.Skeleton.ScaleX);skeleton.AnimationState.SetAnimation(0,"Run_Battle_00",true);
        var start=transform.position;float t=0;while(t<1.2f){t+=Time.deltaTime;transform.position=start+Vector3.left*(t/1.2f*8);yield return null;}Destroy(gameObject);
    }
    void OnDestroy(){if(skeleton && skeleton.AnimationState!=null)skeleton.AnimationState.Event-=OnEvent;}
}
