using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Animates authored Spine/UGUI nodes. Reward ownership is committed separately.</summary>
public sealed class StorybookChestOpening : MonoBehaviour
{
    public SkeletonGraphic skeleton;
    public Image glow, flash, featured;
    public Image rays, seam;
    public Text featuredName, progress;
    public CanvasGroup summary;
    public ScrollRect rewardsScroll;
    public ChestRewardRow rowPrefab;
    public Button collect;
    public Sprite crystal;
    public Material crystalMaterial;
    public RectTransform[] motes;
    public GameObject preview;
    public bool Finished {get;private set;}
    readonly List<ChestRewardRow> rows=new List<ChestRewardRow>();
    ChestPackage owner;
    int activeFlights;
    public void Bind(ChestPackage value)
    {
        owner=value;Finished=false;chestBone=null;sparkAge=0;
        skeleton.Initialize(false);
        string skin=value.CurrentChest.chestGrade==ChestGrade.DIAMOND?"diamond":value.CurrentChest.chestGrade==ChestGrade.GOLD?"gold":"bronze";
        skeleton.Skeleton.SetSkin(skin);skeleton.Skeleton.SetSlotsToSetupPose();
        skeleton.AnimationState.SetAnimation(0,"idle",true);
        skeleton.Update(0);
        summary.gameObject.SetActive(false);collect.gameObject.SetActive(false);
        featured.gameObject.SetActive(false);featuredName.text="";progress.text="";
        SetAlpha(glow,0);SetAlpha(flash,0);SetAlpha(rays,0);SetAlpha(seam,0);foreach(var m in motes)m.gameObject.SetActive(false);
        collect.onClick.RemoveAllListeners();collect.onClick.AddListener(()=>{if(Finished)owner.CloseButtonWithRewards();});
    }
    public void Play(){StartCoroutine(Present());}
    static void SetAlpha(Graphic g,float value){if(!g)return;var c=g.color;c.a=value;g.color=c;}
    Spine.Bone chestBone;
    float sparkAge;
    void LateUpdate()
    {
        if(!owner||skeleton.Skeleton==null)return;
        if(chestBone==null)chestBone=skeleton.Skeleton.FindBone("chest");
        float unit=skeleton.canvas?skeleton.canvas.referencePixelsPerUnit:100;
        foreach(var effect in new[]{glow,seam})if(effect){
            effect.rectTransform.anchoredPosition=new Vector2(chestBone.WorldX,chestBone.WorldY)*unit;
            effect.rectTransform.localEulerAngles=new Vector3(0,0,chestBone.WorldRotationX);
            effect.rectTransform.localScale=new Vector3(chestBone.WorldScaleX,chestBone.WorldScaleY,1);
        }
        if(owner.Opening&&sparkAge>=.95f){sparkAge+=Time.unscaledDeltaTime;AnimateSparks(sparkAge,true);}
    }
    void AnimateSparks(float age,bool visible)
    {
        sparkAge=age;
        for(int i=0;i<motes.Length;i++){
            var m=motes[i];m.gameObject.SetActive(visible);if(!visible)continue;
            float t=age<.95f?Mathf.Clamp01(age/(.7f+(i%4)*.12f)):Mathf.Repeat(age*.42f+i*.173f,1);
            float source=(i*37%181)-90;
            m.anchoredPosition=new Vector2(source+Mathf.Sin(i*2.4f+t*3)*15*t,40+t*(75+(i%5)*18));
            m.localScale=Vector3.one*(.35f+(i%4)*.14f);
            SetAlpha(m.GetComponent<Image>(),Mathf.Sin(t*Mathf.PI)*(Finished?.5f:1));
        }
    }
    IEnumerator Present()
    {
        preview.SetActive(false);owner.view.source.gameObject.SetActive(false);
        skeleton.AnimationState.SetAnimation(0,"open",false);
        float elapsed=0;
        bool burst=false;
        while(elapsed<2.2f){
            elapsed+=Time.unscaledDeltaTime;
            float charge=Mathf.Clamp01(elapsed/1.2f);
            float afterBurst=Mathf.Max(0,elapsed-1.25f);
            SetAlpha(glow,elapsed<1.25f?0:Mathf.Lerp(1,.72f,afterBurst/.95f));
            SetAlpha(rays,0);
            SetAlpha(seam,elapsed<1.25f?charge*(.8f+.2f*Mathf.Sin(elapsed*30)):Mathf.Max(0,1-afterBurst/.12f));
            if(!burst&&elapsed>=1.25f){burst=true;SoundManager.Instance.PlayClip("CHEST_DROP_CRYSTALS");}
            SetAlpha(flash,0);
            AnimateSparks(afterBurst,elapsed>=1.25f);
            yield return null;
        }
        SetAlpha(flash,0);
        SetAlpha(seam,0);
        skeleton.AnimationState.SetAnimation(0,"opened",true);
        summary.gameObject.SetActive(true);summary.alpha=1;
        int total=owner.RewardItems.Count+1;
        for(int i=0;i<total;i++){
            Item item=i==0?null:owner.RewardItems[i-1];
            Sprite sprite=item==null?crystal:InventoryManager.single.GetSpriteIcon(item.m_IconName);
            string label=item==null?"水晶 × "+owner.RewardAmount:StorybookEquipmentUI.GradeName(item.itemGrade)+" · "+item.m_ItemFinalTitle;
            progress.text="战利品 "+(i+1)+" / "+total;
            featured.sprite=sprite;featured.material=item==null?crystalMaterial:null;
            featured.gameObject.SetActive(sprite);featuredName.text=label;SetAlpha(featuredName,1);
            var row=Instantiate(rowPrefab,rewardsScroll.content);row.gameObject.SetActive(true);
            row.icon.sprite=sprite;row.icon.material=item==null?crystalMaterial:null;row.icon.enabled=sprite;
            row.caption.text=label;row.group.alpha=0;rows.Add(row);
            var rr=(RectTransform)row.transform;rr.anchoredPosition=new Vector2(0,-i*62);
            rewardsScroll.content.sizeDelta=new Vector2(0,Mathf.Max(rewardsScroll.viewport.rect.height,(i+1)*62));
            Canvas.ForceUpdateCanvases();rewardsScroll.verticalNormalizedPosition=0;
            SoundManager.Instance.PlayClip(i==0?"STAT_BUY":"ITEM_DROP");
            float t=0;while(t<.4f){t+=Time.unscaledDeltaTime;float k=Mathf.Clamp01(t/.4f);featured.rectTransform.anchoredPosition=new Vector2(0,-295+85*Mathf.Sin(k*Mathf.PI*.5f));featured.rectTransform.localScale=Vector3.one*Mathf.Lerp(.3f,1,k);SetAlpha(featured,k);yield return null;}
            yield return new WaitForSecondsRealtime(item!=null&&(int)item.itemGrade>=3?.75f:.4f);
            // Let the next reveal begin while this authored icon copy flies to its row.
            var flying=Instantiate(featured,featured.transform.parent);
            flying.name="FlyingReward";
            flying.transform.position=featured.transform.position;
            activeFlights++;
            StartCoroutine(LandReward(flying,row));
        }
        featured.gameObject.SetActive(false);featuredName.text="";
        while(activeFlights>0)yield return null;
        progress.text="战利品已全部获得";rewardsScroll.verticalNormalizedPosition=1;
        Finished=true;collect.gameObject.SetActive(true);collect.interactable=true;
        SetAlpha(rays,0);SetAlpha(glow,.48f);
        owner.view.Refresh();
    }
    IEnumerator LandReward(Image flying,ChestRewardRow row)
    {
        Vector3 from=flying.transform.position;
        float t=0;
        while(t<.3f){
            t+=Time.unscaledDeltaTime;float k=Mathf.Clamp01(t/.3f);
            // A later reward can scroll the list; follow the live destination.
            flying.transform.position=Vector3.Lerp(from,row.icon.transform.position,k);
            flying.rectTransform.localScale=Vector3.one*(1-.6f*k);
            SetAlpha(flying,1-k);row.group.alpha=k;
            yield return null;
        }
        row.group.alpha=1;flying.gameObject.SetActive(false);Destroy(flying.gameObject);activeFlights--;
    }

}
