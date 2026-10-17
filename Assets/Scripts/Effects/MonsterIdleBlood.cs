using UnityEngine;
using Spine;
using Spine.Unity;

// Cosmetic only: follows the skinned blade, never changes Spine timelines or dispatches combat events.
[DefaultExecutionOrder(200)]
public sealed class MonsterIdleBlood : MonoBehaviour
{
    public SkeletonAnimation skeleton;
    public Material material;
    public string weaponSlot = "wepon";
    public Vector2 bladeUv = new Vector2(.98f,.75f);
    [Min(.1f)] public float interval = 2.2f;
    [Min(.01f)] public float dropletSize = .055f;
    [Min(.05f)] public float lifetime = .65f;
    public Color bloodColor = new Color(.45f,.025f,.035f,1);
    public int EmittedCount { get; private set; }
    public int ActiveCount { get { int count=0;foreach(var d in drops)if(d!=null && d.active)count++;return count; } }
    public Vector3 LastEmission { get; private set; }
    sealed class Drop { public GameObject go; public MeshRenderer renderer; public MaterialPropertyBlock color = new MaterialPropertyBlock(); public Vector3 pos; public float age,scale; public bool active; }
    readonly Drop[] drops=new Drop[4];
    Mesh mesh; float clock; float[] vertices; Slot slot; Spine.MeshAttachment attachment; int tip=-1;
    void Start(){if(!skeleton)skeleton=GetComponent<SkeletonAnimation>();}
    public void CopySettingsFrom(MonsterIdleBlood source) {
        material=source.material;weaponSlot=source.weaponSlot;bladeUv=source.bladeUv;interval=source.interval;dropletSize=source.dropletSize;lifetime=source.lifetime;bloodColor=source.bloodColor;
    }
    public bool TryGetTip(out Vector3 position) {
        position=transform.position;
        if(!skeleton||skeleton.Skeleton==null)return false;
        slot=skeleton.Skeleton.FindSlot(weaponSlot);
        if(slot==null || slot.A<=0 || !(slot.Attachment is Spine.MeshAttachment current))return false;
        if(attachment!=current) {
            attachment=current;tip=-1;float best=float.MaxValue;
            for(int i=0;i<current.RegionUVs.Length;i+=2){float dist=(new Vector2(current.RegionUVs[i],current.RegionUVs[i+1])-bladeUv).sqrMagnitude;if(dist<best){best=dist;tip=i;}}
            vertices=new float[current.WorldVerticesLength];
        }
        if(tip<0)return false;
        current.ComputeWorldVertices(slot,0,current.WorldVerticesLength,vertices,0,2);
        position=skeleton.transform.TransformPoint(new Vector3(vertices[tip],vertices[tip+1],0));return true;
    }
    void LateUpdate(){
        if(!Application.isPlaying || !skeleton || !skeleton.valid || !material)return;
        var enemy=skeleton.GetComponentInParent<Enemy>();
        if(enemy && (enemy.IsEntering || (enemy.ab && enemy.ab.isDying))){Clear();return;}
        float dt=Time.deltaTime*Mathf.Max(0,skeleton.timeScale);
        var entry=skeleton.AnimationState.GetCurrent(0);string clip=entry?.Animation?.Name;
        if(clip=="Death1"||clip=="Death_00"){Clear();return;}
        for(int i=0;i<drops.Length;i++) {
            var d=drops[i];if(d==null||!d.active)continue;
            d.age+=dt;if(d.age>=lifetime){d.active=false;d.go.SetActive(false);continue;}
            d.go.transform.position=d.pos+Vector3.down*(d.age*d.age*1.8f*d.scale);
            d.color.SetColor("_Color",new Color(bloodColor.r,bloodColor.g,bloodColor.b,bloodColor.a*(1-d.age/lifetime)));
            d.renderer.SetPropertyBlock(d.color);
        }
        if(clip!="Idle1" && clip!="Idle_Battle_00"){clock=0;return;}
        clock+=dt;if(clock<interval)return;
        clock=0;if(TryGetTip(out var pos))Emit(pos);
    }
    void Emit(Vector3 position){
        int index=EmittedCount%drops.Length;var d=drops[index];
        if(d==null){
            if(!mesh){mesh=new Mesh{name="Blood drop silhouette"};mesh.vertices=new[]{new Vector3(0,.8f,0),new Vector3(-.38f,-.05f,0),new Vector3(-.28f,-.38f,0),new Vector3(0,-.5f,0),new Vector3(.28f,-.38f,0),new Vector3(.38f,-.05f,0)};mesh.triangles=new[]{0,1,2,0,2,3,0,3,4,0,4,5};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white,Color.white,Color.white};mesh.RecalculateBounds();}
            d=new Drop();d.go=new GameObject("Idle blood drop");d.go.transform.SetParent(transform,false);d.go.AddComponent<MeshFilter>().sharedMesh=mesh;d.renderer=d.go.AddComponent<MeshRenderer>();d.renderer.sharedMaterial=material;drops[index]=d;
        }
        var sr=skeleton.GetComponent<MeshRenderer>();d.renderer.sortingLayerID=sr.sortingLayerID;d.renderer.sortingOrder=sr.sortingOrder+1;
        d.scale=Mathf.Abs(skeleton.transform.lossyScale.x);d.age=0;d.active=true;d.pos=position;
        d.go.SetActive(true);d.go.transform.position=position;d.go.transform.rotation=Quaternion.identity;
        // Counter parent mirroring: detached drops always point up and fall in world-space gravity.
        var ps=transform.lossyScale;d.go.transform.localScale=new Vector3(dropletSize*d.scale/ps.x,dropletSize*d.scale/ps.y,1);
        d.color.SetColor("_Color",bloodColor);d.renderer.SetPropertyBlock(d.color);LastEmission=position;EmittedCount++;
    }
    void Clear(){clock=0;foreach(var d in drops)if(d!=null){d.active=false;if(d.go)d.go.SetActive(false);}}
    void OnDisable(){Clear();}
    void OnDestroy(){if(mesh)Destroy(mesh);}
}
