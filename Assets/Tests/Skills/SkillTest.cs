using System.Collections;
using System.Linq;
using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

// Controls are persistent UGUI nodes in SkillTestHUD.prefab. Only data and interaction live here.
public sealed class SkillTest : MonoBehaviour
{
    public Button[] skillButtons;
    public Button castButton, resetButton, levelDown, levelUp, loopButton, attackButton;
    public Text heading, detail, status, levelText, loopText;
    public RectTransform safeRoot;
    public Button catalogButton;
    public Text catalogLabel;
    public CanvasGroup catalogGroup;
    public bool CatalogOpen {get;private set;}
    public void SetCatalog(bool open){CatalogOpen=open;catalogGroup.blocksRaycasts=open;catalogGroup.interactable=open;if(open)catalogGroup.gameObject.SetActive(true);}

    [System.Serializable] public sealed class Definition { public SkillsTypeEnum key; public string displayName; public string[] effectLabels; }
    public Definition[] definitions;
    public SkillTestStage stage;
    public SkillsTypeEnum Selected {get;private set;}=SkillsTypeEnum.Sinister_Stampede;
    public int Level {get;private set;}=1;
    public bool Ready {get;private set;}
    public bool Loop {get;private set;}
    public int HitCount {get;private set;}
    public float TotalDamage {get;private set;}
    float nextCast; string result="正在准备测试场景…"; int width,height;
    readonly System.Collections.Generic.HashSet<Actor_Base> targets=new System.Collections.Generic.HashSet<Actor_Base>();
    IEnumerator Start()
    {
        catalogButton.onClick.AddListener(()=>SetCatalog(!CatalogOpen));
        for(int i=0;i<skillButtons.Length;i++){int key=i+1;skillButtons[i].onClick.AddListener(()=>Select((SkillsTypeEnum)key));}
        castButton.onClick.AddListener(Cast);resetButton.onClick.AddListener(ResetTest);
        levelDown.onClick.AddListener(()=>SetLevel(Level-1));levelUp.onClick.AddListener(()=>SetLevel(Level+1));
        loopButton.onClick.AddListener(()=>{Loop=!Loop;nextCast=0;});attackButton.onClick.AddListener(()=>Player.single.PerformManualAction(SkillsTypeEnum.NONE));
        yield return null; // Let the authored actors bind their animation events.
        stage.Place();
        Ready=true;ResetTest();
    }
    void Update()
    {
        catalogGroup.alpha=Mathf.MoveTowards(catalogGroup.alpha,CatalogOpen?1:0,Time.unscaledDeltaTime/.12f);
        if(!CatalogOpen&&catalogGroup.alpha==0)catalogGroup.gameObject.SetActive(false);
        catalogLabel.text=definitions.First(n=>n.key==Selected).displayName.Replace("\n","")+"     /     "+(CatalogOpen?"收起 −":"全部技能 +");
        var safe=Screen.safeArea;safeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);safeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
        if(Player.single&&!Player.single.isCampPlayer){Player.single.manualControl=true;}
        foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){
            enemy.StopAttacking();if(enemy.currentAnimation.StartsWith("Attack"))enemy.SetAnimation("Idle1",true,false);var actor=enemy.ab;actor.trainingTarget=true;actor.currentStat.hpMax=100;actor.currentStat.hpNow=40;actor.currentStat.def=0;
            if(actor.enemyLifebar)actor.enemyLifebar.gameObject.SetActive(false);
            if(targets.Add(actor))actor.DamageReceived+=RecordHit;
        }
        if(!Ready){castButton.interactable=resetButton.interactable=attackButton.interactable=false;return;}
        if((width!=Screen.width||height!=Screen.height)&&!Player.single.isAttacking){width=Screen.width;height=Screen.height;stage.Place();}
        bool busy=Player.single.isAttacking||Player.single.ab.isDying||FindFirstObjectByType<GhostWolfPack>();
        castButton.interactable=!Player.single.ab.isDying&&(PlayerSkillScheduler.AnimationFor(Selected)!=null||!busy);
        resetButton.interactable=!busy;attackButton.interactable=!busy;
        levelDown.interactable=Level>1;levelUp.interactable=Level<10;
        loopText.text=Loop?"循环：开":"循环：关";
        if(Loop&&!busy&&Time.unscaledTime>=nextCast){Cast();nextCast=Time.unscaledTime+3;}
        var p=Player.single;status.text=$"命中 {HitCount} 次  ·  累计伤害 {TotalDamage:0}\n{result}\n角色生命 {p.ab.currentStat.hpNow:0}/{p.ab.currentStat.hpMax:0}  护盾 {p.ab.currentStat.absorb:0}  队列 {p.SkillScheduler.PendingCount}";
    }
    void RecordHit(float damage){HitCount++;TotalDamage+=damage;}
    public void Select(SkillsTypeEnum key){Selected=key;Refresh();SetCatalog(false);}
    public void SetLevel(int value){Level=Mathf.Clamp(value,1,10);Refresh();}
    void Refresh(){var node=definitions.First(n=>n.key==Selected);heading.text=node.displayName.Replace("\n","");levelText.text="Lv. "+Level;var values=SkillsController.Instance.GetSkillValues(Level,Selected);
        detail.text=Hint()+"\n"+string.Join(" / ",node.effectLabels.Select((label,i)=>label+" "+(values[i]*100).ToString("0.#")+"%"));
        for(int i=0;i<skillButtons.Length;i++)skillButtons[i].GetComponent<Image>().color=i+1==(int)Selected?new Color(.38f,.085f,.085f):new Color(.10f,.12f,.14f);}
    string Hint(){switch(Selected){
        case SkillsTypeEnum.Regen:return "测试：降低角色生命，观察每 2 秒回复。";
        case SkillsTypeEnum.Ressurrection:return "测试：确保触发复生，播放完整死亡与起身。";
        case SkillsTypeEnum.Lucky_Bastard:case SkillsTypeEnum.Twice_The_Fun:return "测试：模拟击杀触发；怪物不死亡，不发放经验。";
        case SkillsTypeEnum.My_Lucky_Stars:return "测试：模拟暴击并确保触发护佑。";
        case SkillsTypeEnum.BeastPackLeader:return "测试：召唤影狼和书脊龙，狼群也享受增伤。";
        case SkillsTypeEnum.Angel_On_My_Shoulder:case SkillsTypeEnum.Devil_On_My_Shoulder:return "测试：应用属性加成并显示属性数值。";
        default:return "点击测试或开启循环；重置可清空已启用技能。";
    }}
    public void ResetTest(){
        Loop=false;foreach(var pet in FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None))Destroy(pet.gameObject);
        if(PetCindrel.single)Destroy(PetCindrel.single.gameObject);
        var p=Player.single;GhostWolfPackAudio.StopAll(p);p.StopAllCoroutines();p.SkillScheduler.Clear();p.isAttacking=false;p.ab.isDying=false;p.ab.forceSkillTriggers=false;
        p.currentAnimation="";p.SetAnimation("Idle1",true,false);SkillsController.Instance.ResetSkills();
        p.ab.currentStat=GameManager.single.CalcPlayer();p.ab.currentStat.dmg=100;p.ab.currentStat.atkSpd=100;p.ab.currentStat.hpMax=1000;p.ab.currentStat.hpNow=600;p.ab.currentStat.def=0;p.ab.currentStat.block=0;p.ab.currentStat.crit=0;
        p.lightningVal=p.regenVal=p.myLuckyStarsVal=p.sinisterStampedeChance=p.bossKillerChance=0;
        foreach(var target in targets)if(target)target.isBoss=false;
        HitCount=0;TotalDamage=0;result="重置完成 · 已启用技能清空";Refresh();
    }
    public void Cast(){
        if(!Ready||!castButton.interactable)return;
        var s=SkillsController.Instance;
        // Set exactly the selected rank, including when decreasing the level.
        var fields=new[]{"", "regenLevel","shadowWolfLevel","sinisterStampedeLevel","angelOnMyShoulderLevel","devilOnMyShoulderLevel","ressurrectionLevel","luckyBastardLevel","ghostWolfPackLevel","cindrelLevel","twiceTheFunLevel","lightningDemiGodlevel","bossKillerLevel","myLuckyStarsLevel","battlefieldTacticianLevel","beastPackLeaderLevel"};
        typeof(SkillsController).GetField(fields[(int)Selected]).SetValue(s,Level);
        var p=Player.single;float value=s.GetSkillValues(Level,Selected)[0];result=heading.text+" · Lv. "+Level;
        if(PlayerSkillScheduler.AnimationFor(Selected)!=null){p.lightningVal=s.GetSkillValues(s.lightningDemiGodlevel,SkillsTypeEnum.Lightning_Demi_God)[0];if(Selected==SkillsTypeEnum.Boss_Killer)targets.First(t=>t&&t.gameObject.activeInHierarchy).isBoss=true;p.PerformManualAction(Selected);return;}
        switch(Selected){
            case SkillsTypeEnum.PetShadowWolf:SpawnWolf();break;
            case SkillsTypeEnum.PetCindrel:SpawnDragon();break;
            case SkillsTypeEnum.BeastPackLeader:s.shadowWolfLevel=Mathf.Max(1,s.shadowWolfLevel);s.cindrelLevel=Mathf.Max(1,s.cindrelLevel);SpawnWolf();SpawnDragon();result+=$" · 召唤伤害 +{value*100:0}%";break;
            case SkillsTypeEnum.Regen:p.ab.currentStat.hpNow=p.ab.currentStat.hpMax*.3f;p.regenVal=value;p.StopCoroutine("RegenLoop");p.StartCoroutine("RegenLoop");break;
            case SkillsTypeEnum.My_Lucky_Stars:p.ab.currentStat.block=0;p.myLuckyStarsVal=value;p.ab.forceSkillTriggers=true;p.ab.Hit(10,true);p.ab.forceSkillTriggers=false;break;
            case SkillsTypeEnum.Ressurrection:p.ab.forceSkillTriggers=true;p.ab.Death();p.ab.forceSkillTriggers=false;break;
            case SkillsTypeEnum.Lucky_Bastard:case SkillsTypeEnum.Twice_The_Fun:p.ab.currentStat.hpNow=p.ab.currentStat.hpMax*.3f;targets.First().ApplyKillSkillRewards(true,false);result+=" · 已模拟击杀触发";break;
            case SkillsTypeEnum.Boss_Killer:targets.First().isBoss=true;p.PerformManualAction(Selected);break;
            case SkillsTypeEnum.Battlefield_Tactician:p.PerformManualAction(SkillsTypeEnum.NONE);result+=$" · 三目标，攻击伤害 +{value*100:0}%";break;
            default:var before=p.ab.currentStat;p.ab.currentStat=GameManager.single.CalcPlayer();p.ab.currentStat.hpNow=p.ab.currentStat.hpMax;result=$"属性已应用：攻击 {p.ab.currentStat.dmg:0} / 防御 {p.ab.currentStat.def:0}\n攻速 {p.ab.currentStat.atkSpd:0} / 暴击 {p.ab.currentStat.crit:0.#}% / 格挡 {p.ab.currentStat.block*100:0.#}%";break;
        }
    }
    void SpawnWolf(){foreach(var pet in FindObjectsByType<WolfPetCompanion>(FindObjectsSortMode.None)){pet.gameObject.SetActive(false);Destroy(pet.gameObject);}LevelController.Instance.AssetManager.SpawnGeneralObj("PetShadowWolf");}
    void SpawnDragon(){if(PetCindrel.single){PetCindrel.single.gameObject.SetActive(false);Destroy(PetCindrel.single.gameObject);}LevelController.Instance.AssetManager.SpawnGeneralObj("PetCindrel");}
}
