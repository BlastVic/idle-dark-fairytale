using System.Collections;
using Spine.Unity;
using UnityEngine;

// Authored only on the new ground companion. Original flying-dragon assets remain untouched.
public sealed class PetCindrelCompanion : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public Vector2 viewportFeet = new Vector2(.22f, .38f);
    public Vector2 viewportSize = new Vector2(.54f, .17f);
    public float exitSeconds = 1.2f;
    public bool Exiting { get; private set; }
    Vector3 setupScale;
    Bounds setupBounds;
    Camera battleCamera;
    int lastWidth,lastHeight;
    void Awake() {
        skeleton.Initialize(false);skeleton.Skeleton.SetToSetupPose();skeleton.Update(0);skeleton.LateUpdate();
        setupScale=skeleton.transform.localScale;setupBounds=skeleton.GetComponent<MeshFilter>().sharedMesh.bounds;
    }
    void Start(){if(LevelController.Instance && LevelController.Instance.AssetManager)battleCamera=LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>();Place();}
    void LateUpdate(){if(!Exiting && (Screen.width!=lastWidth || Screen.height!=lastHeight))Place();}
    public void Place(){
        if(!battleCamera)return;lastWidth=Screen.width;lastHeight=Screen.height;
        var worldSize=DarkFairytaleBattleStyle.WorldSize(battleCamera);
        float scale=DarkFairytaleBattleStyle.DisplayFitScale(Vector3.Scale(setupBounds.size,setupScale),worldSize,viewportSize);
        // Keep the authored Spine display node at (1,1,1); fit the companion root.
        skeleton.transform.localScale=setupScale;
        transform.localScale=Vector3.one*scale;
        var feet=battleCamera.ViewportToWorldPoint(new Vector3(viewportFeet.x,viewportFeet.y,-battleCamera.transform.position.z));
        transform.position=new Vector3(feet.x-setupBounds.center.x*scale,feet.y-setupBounds.min.y*scale,0);
        skeleton.GetComponent<MeshRenderer>().sortingLayerName="Character";
        skeleton.GetComponent<MeshRenderer>().sortingOrder=Mathf.RoundToInt((1-viewportFeet.y)*100)+1;
    }
    public void Exit(){if(Exiting)return;Exiting=true;StartCoroutine(Leave());}
    IEnumerator Leave(){
        skeleton.Skeleton.ScaleX=-Mathf.Abs(skeleton.Skeleton.ScaleX);
        skeleton.AnimationState.SetAnimation(0,"Exit",true);
        var start=transform.position;float elapsed=0;
        while(elapsed<exitSeconds){elapsed+=Time.deltaTime;transform.position=start+Vector3.left*(elapsed/exitSeconds*8);yield return null;}
        Destroy(gameObject);
    }
}
