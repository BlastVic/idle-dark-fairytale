using Assets.Scripts.Services.Vibrations;
using IdleKnightHero.UI;
using Scripts.Skills;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : AnimationController
{
    private static Player _instance;
    public static Player single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<Player>();
            return _instance;
        }
    }

    public bool isCampPlayer = false;
    [System.NonSerialized] public bool manualControl;
    public bool PerformManualAction(SkillsTypeEnum skill)
    {
        if (!manualControl || ab.isDying) return false;
        if (PlayerSkillScheduler.AnimationFor(skill) != null) { var added=QueueSkill(skill); if (!isAttacking) TryBeginQueuedSkill(); return added; }
        if (isAttacking) return false;
        SetAnimation(skill == SkillsTypeEnum.Boss_Killer ? "Skill7" : "Attack1", false, false, Mathf.Max(.01f,ab.currentStat.atkSpd*.01f));
        return true;
    }
    public Actor_Base ab;
    public GameObject rootPos;

    [Tooltip("Disable for characters with a fixed costume, such as Red Hood. Equipment stats still apply.")]
    public bool useEquipmentAppearance = true;
    public bool IsVictoryRelaxing { get; private set; }
    private Vector3 originalVisualPosition, originalVisualScale;

    private void Awake()
    {
        originalVisualPosition = skeletonAnimation.transform.localPosition;
        originalVisualScale = skeletonAnimation.transform.localScale;
    }

    private void OnEnable()
    {
        ab = GetComponent<Actor_Base>();
    }

    private void Start()
    {
        base.Start();
        skeletonAnimation.AnimationState.Complete += OnTrackComplete;
    }

    public void InitPlayer()
    {
        if (IsVictoryRelaxing && skeletonAnimation && skeletonAnimation.Skeleton != null) {
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
        }
        IsVictoryRelaxing = false;
        skeletonAnimation.transform.localPosition = originalVisualPosition;
        skeletonAnimation.transform.localScale = originalVisualScale;
        var shadow = transform.Find("Battle Contact Shadow");
        if (shadow) shadow.gameObject.SetActive(false);
        currentAnimation = "";
        isAttacking = false;
        attackIndexer = 0;
        ResetCombatActions();
        SetSkinsAndAttachments();


        if (isCampPlayer)
        {
            transform.position = new Vector3(0, 2.62f, 0);
            ab.currentStat.absorb = 0;
        }
        else
        {
            transform.position = new Vector3(0, .62f, 0);
            var assets = LevelController.Instance.AssetManager;
            if (assets.battleStyle)
                assets.battleStyle.PlacePlayer(this, assets.battleCampCamera.GetComponent<Camera>());
        }

        ab.isDying = false;
        ab.ressurrectionWaiting = false;

        ab.currentStat = GameManager.single.CalcPlayer();
        ab.currentStat.hpNow = ab.currentStat.hpMax;//fill hp
        GameplayCanvas.single?.RefreshHp();
        GameplayCanvas.single?.FillHp();

        if (isCampPlayer)
        {
            StopAllCoroutines();
            SetAnimation("Idle1", true, false, 1);

            GameplayCanvas.single?.RefreshHp();
            GameplayCanvas.single?.RefreshXp();
        }
        else
        {
            StopAllCoroutines();

            SummonLearnedCompanions();

            //boot up our skills
            regenVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Regen), SkillsTypeEnum.Regen)[0];
            if (regenVal > 0)
                StartCoroutine(RegenLoop());

            lightningVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Lightning_Demi_God), SkillsTypeEnum.Lightning_Demi_God)[0];
            if (lightningVal > 0)
                StartCoroutine(LightningLoop());

            myLuckyStarsVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.My_Lucky_Stars), SkillsTypeEnum.My_Lucky_Stars)[0];

            sinisterStampedeChance = SkillsController.Instance.sinisterStampedeLevel > 0 ? SkillDesign.SweepChance : 0;
            bossKillerChance = SkillsController.Instance.bossKillerLevel > 0 ? SkillDesign.BossChance : 0;

            //start our attack
            Attack();
        }

    }


    void SummonLearnedCompanions()
    {
        var skills = SkillsController.Instance;
        var assets = LevelController.Instance.AssetManager;
        if (skills.GetSkillLevel(SkillsTypeEnum.PetCindrel) > 0)
            assets.SpawnGeneralObj("PetCindrel");
        if (skills.GetSkillLevel(SkillsTypeEnum.PetShadowWolf) > 0)
            assets.SpawnGeneralObj("PetShadowWolf");
    }

    public float myLuckyStarsVal;
    public float regenVal;
    public float lightningVal;
    public float sinisterStampedeChance;
    public float bossKillerChance;

    float regenNext, lightningNext, lightningInterval;
    public float RegenRemaining => Mathf.Clamp01((regenNext-Time.time)/2f);
    public float LightningRemaining => lightningInterval>0?Mathf.Clamp01((lightningNext-Time.time)/lightningInterval):0;

    IEnumerator RegenLoop()
    {
        regenNext=Time.time+2;
        yield return new WaitForSeconds(2);
        if (!ab.isDying)
        {
            ab.HealByPercent(regenVal);
            StartCoroutine("RegenLoop");
        }
    }

    public const float ActionRecovery = .2f;
    public PlayerSkillScheduler SkillScheduler { get; } = new PlayerSkillScheduler();
    readonly List<Actor_Base> lightningTargets = new List<Actor_Base>();
    Coroutine nextAction;
    float castAttackSpeed;

    void OnTrackComplete(Spine.TrackEntry entry)
    {
        if (entry.TrackIndex == 0 && entry == skeletonAnimation.AnimationState.GetCurrent(0) &&
            (entry.Animation.Name.StartsWith("Attack") || entry.Animation.Name.StartsWith("Skill")))
            AnimationComplete(0, entry.Animation.Name);
    }

    void ResetCombatActions()
    {
        GhostWolfPackAudio.StopAll(this);
        if (nextAction != null) StopCoroutine(nextAction);
        nextAction = null;
        SkillScheduler.Clear(); lightningTargets.Clear(); isAttacking = false;
    }

    public bool QueueSkill(SkillsTypeEnum skill)
    {
        if (isCampPlayer || ab.isDying || IsVictoryRelaxing) return false;
        return SkillScheduler.Enqueue(skill);
    }

    bool CanLightningHit(Actor_Base target)
    {
        return target && target.gameObject.activeInHierarchy && !target.isDying &&
            target.currentStat.hpNow < target.currentStat.hpMax * .5f &&
            (!target.TryGetComponent<DropIn>(out var drop) || !drop.IsEntering);
    }

    IEnumerator LightningLoop()
    {
        while (!isCampPlayer && !ab.isDying && !IsVictoryRelaxing)
        {
            lightningInterval = SkillDesign.LightningInterval;
            lightningNext = Time.time + lightningInterval;
            yield return new WaitForSeconds(lightningInterval);
            if (isCampPlayer || ab.isDying || IsVictoryRelaxing) yield break;
            foreach (var target in GameManager.single.GetLivingEnemies(false))
                if (CanLightningHit(target)) { QueueSkill(SkillsTypeEnum.Lightning_Demi_God); break; }
            // Attack() consumes this request between actions, never interrupting another skill.
            while (SkillScheduler.Contains(SkillsTypeEnum.Lightning_Demi_God)) yield return null;
        }
    }

    bool CanCastSkill(SkillsTypeEnum skill)
    {
        if (SkillsController.Instance.GetSkillLevel(skill) <= 0) return false;
        var targets = GameManager.single.GetLivingEnemies(false);
        if (skill == SkillsTypeEnum.Lightning_Demi_God) return targets.Exists(CanLightningHit);
        if (skill == SkillsTypeEnum.Boss_Killer) return GameManager.single.GetAllBosses(false).Count > 0;
        return targets.Count > 0;
    }

    public float SkillCompensation(string animation, bool includeExpectedCritical = true)
    {
        float speed = Mathf.Max(.01f, ab.currentStat.atkSpd * .01f);
        float skillRate = (animation == "Skill2" || animation == "Skill3") ? 1.2f : 1f;
        float attacks = (skeletonAnimation.Skeleton.Data.FindAnimation(animation).Duration / (speed * skillRate) + ActionRecovery) /
            ((skeletonAnimation.Skeleton.Data.FindAnimation("Attack1").Duration + skeletonAnimation.Skeleton.Data.FindAnimation("Attack2").Duration) * .5f / speed + ActionRecovery);
        float damage = ab.currentStat.dmg;
        if (GameManager.single.GetLivingEnemies(false).Count >= 2)
            damage *= 1 + SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Battlefield_Tactician), SkillsTypeEnum.Battlefield_Tactician)[0];
        if (includeExpectedCritical)
            damage *= 1 + Mathf.Clamp01(ab.currentStat.crit * .01f) * (ab.currentStat.critDmg * .01f - 1);
        return damage * attacks;
    }

    bool TryBeginQueuedSkill()
    {
        if (!SkillScheduler.TryBegin(CanCastSkill, out var skill)) return false;
        string animation = PlayerSkillScheduler.AnimationFor(skill);
        castAttackSpeed = Mathf.Max(.01f, ab.currentStat.atkSpd * .01f);
        SkillScheduler.SetCompensation(SkillCompensation(animation, skill != SkillsTypeEnum.Sinister_Stampede && skill != SkillsTypeEnum.Boss_Killer));
        lightningTargets.Clear();
        if (skill == SkillsTypeEnum.Lightning_Demi_God)
            lightningTargets.AddRange(GameManager.single.GetLivingEnemies(false).FindAll(CanLightningHit));
        SetAnimation(animation, false, false, castAttackSpeed);
        if (skill == SkillsTypeEnum.Sinister_Stampede) StartCoroutine(PlayScytheSlash());
        return true;
    }

    IEnumerator PlayScytheSlash()
    {
        var entry = skeletonAnimation.AnimationState.GetCurrent(0);
        if (entry == null || entry.Animation.Name != "Skill5") yield break;
        while (entry.TrackTime < .46f)
        {
            if (ab.isDying || IsVictoryRelaxing || skeletonAnimation.AnimationState.GetCurrent(0) != entry) yield break;
            yield return null;
        }
        if (ab.isDying || IsVictoryRelaxing || skeletonAnimation.AnimationState.GetCurrent(0) != entry) yield break;
        SkillTimedAudio.Play(transform, Resources.Load<AudioClip>("Audio/Skills/ScytheSlash"), entry.TimeScale * skeletonAnimation.timeScale);
    }

    void ReleaseLightning()
    {
        var targets = lightningTargets.FindAll(CanLightningHit).ToArray();
        float bonus = targets.Length > 0 ? SkillScheduler.Compensation / targets.Length : 0;
        lightningTargets.Clear();
        bool sounded = false;
        foreach (var target in targets)
        {
            // Snapshot targets prevent a delayed cast spilling into a new wave.
            if (!CanLightningHit(target)) continue;
            if (!sounded) { SoundManager.Instance.PlayClip("LIGHTNING"); sounded = true; }
            LevelController.Instance.AssetManager.SpawnParticle("Lightning_Particle", target.transform.position);
            target.Hit(ab.currentStat.dmg * lightningVal + bonus, false, 0, 150, false, true);
        }
    }

    public bool isAttacking = false;
    public int attackIndexer = 0;

    void Attack()
    {
        if (isCampPlayer || ab.isDying || IsVictoryRelaxing || isAttacking) return;
        if (GameManager.single.GetLivingEnemies(false).Count == 0) {
            SkillScheduler.Clear(); ScheduleNextAction(); return;
        }
        // Drain outstanding requests before rolling a new attack opportunity.
        if (TryBeginQueuedSkill()) return;
        if (manualControl) return;
        attackIndexer++;
        if (Random.Range(0, 1f) < sinisterStampedeChance)
            QueueSkill(SkillsTypeEnum.Sinister_Stampede);
        if (SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.GhostWolfPack) > 0 && attackIndexer % 3 == 0)
            QueueSkill(SkillsTypeEnum.GhostWolfPack);
        // Both rolls are retained when they become ready together.
        if (TryBeginQueuedSkill()) return;

        #region Boss Killer
        if (GameManager.single.GetAllBosses(false).Count >= 1)
        {
            if (Random.Range(0, 1f) < bossKillerChance)
            {
                Debug.Log("Rolled for boss killer");
                QueueSkill(SkillsTypeEnum.Boss_Killer);
                TryBeginQueuedSkill();
                SoundManager.Instance.PlayClip("JUMP_SKILL");
                return;
            }
        }
        #endregion

        int attackNum = Random.Range(0, 100) < 70 ? 1 : 2; // Normal attacks: 70% Attack1, 30% Attack2.
        //Debug.Log("attackspeed formatted:" + (ab.currentStat.atkSpd * .01f));
        SetAnimation("Attack" + attackNum, false, false, ab.currentStat.atkSpd * 0.01f);
    }


    public void SetSkin(string skin)
    {
        if (!useEquipmentAppearance) return;
        if (base.skeletonAnimation == null) base.Start();

        base.skeletonAnimation.skeleton.SetSkin(skin);
    }

    public SpineAttachmentSet GetAllAttachments() //whether empty or not
    {

        SpineAttachmentSet set = new SpineAttachmentSet();
        InventoryManager im = InventoryManager.single;

        if (im.hasWeapon)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "weapon1", attachment = im.weaponEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.WEAPON));
        }

        if (im.hasShield)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "shield1", attachment = im.shieldEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.SHIELD));
        }

        if (im.hasHelmet)
        {
            set.attachments.Add(new SpineAttachmentPair { slot = "Head/Head1", attachment = im.helmetEq.m_AttachmentIfApplicable });
        }
        else
        {
            set.attachments.Add(GetEmptyAttachment(ItemType.HELMET));
        }
        return set;
    }

    public SpineAttachmentPair GetEmptyAttachment(ItemType itemType)
    {
        SpineAttachmentSet sap = new SpineAttachmentSet();

        if (itemType == ItemType.WEAPON)
        {
            return new SpineAttachmentPair { slot = "weapon1", attachment = "Weapons/Wep0" };
            //return new SpineAttachmentPair { slot = "weapon1", attachment = "Empty" };
        }

        if (itemType == ItemType.HELMET)
        {
            return new SpineAttachmentPair { slot = "Head/Head1", attachment = "Empty" };
        }

        if (itemType == ItemType.SHIELD)
        {
            return new SpineAttachmentPair { slot = "shield1", attachment = "Empty" };
            //return new SpineAttachmentPair { slot = "shield1", attachment = "Empty" };
        }

        return null;
    }

    public void SetSkinsAndAttachments()
    {

        SetSkinsAndAttachments(null);//this is the default behavior if we want to reset the char looks
    }
    public void SetSkinsAndAttachments(SpineAttachmentSet sas = null)
    {
        if (!useEquipmentAppearance) return;
        //Debug.Log("Setting attachments on player");
        if (skeletonAnimation == null || skeletonAnimation.skeleton == null)
        {
            Debug.Log("You didnt have a skeleton");
            return;
        }

        sas = GetAllAttachments();

        foreach (SpineAttachmentPair sp in sas.attachments)
        {
            skeletonAnimation.skeleton.SetAttachment(sp.slot, sp.attachment);
        }

        if (InventoryManager.single.hasArmor)
        {
            SetSkin(InventoryManager.single.armorEq.m_SkinIfApplicable);
        }
        else
        {
            SetSkin("0");
        }
    }


    #region ANIMATION 
    public string currentAnimation = "";
    public override void SetAnimation(string animation, bool isLoop, bool isOnComplete, float speed = 1)
    {
        if (IsVictoryRelaxing && !animation.Contains("Death")) return;
        if (ab.isDying && !animation.Contains("Death")) return;
        if (isAttacking && !animation.Contains("Death")) return;//dont let anything happen until attack finishes
        if (animation == currentAnimation) return;//dont let anything overwrite identical animation

        if (animation.Contains("Death")) ResetCombatActions();
        currentAnimation = animation;
        // Scale the complete performance, including holds, recovery and events.
        base.SetAnimation(animation, isLoop, isOnComplete, (animation == "Death1" || animation == "Revive" || animation == "Skill2" || animation == "Skill3") ? speed * 1.2f : speed);
        if (animation.StartsWith("Attack") || animation.StartsWith("Skill")) isAttacking = true;
    }

    public override void AnimationOnHit(string animation)
    {
        if (isCampPlayer || ab.isDying || IsVictoryRelaxing) return;
        if (!isAttacking || animation != currentAnimation) return;
        if (animation == "Skill2" || animation == "Skill3" || animation == "Skill5" || animation == "Skill7") {
            if (!SkillScheduler.ConsumeHit(animation)) return;
        }
        if (animation == "Skill3") { ReleaseLightning(); return; }
        if (animation.Contains("Attack"))
        {
            SoundManager.Instance.PlayClip("ATTACK1");
            if (GameManager.single.GetLivingEnemies(false).Count > 0)
            {

                ProcessDamage(1, GameManager.single.GetLivingEnemies(false)[0]);
            }
        }

        if (animation == "Skill2")
        {
            GhostWolfPackAudio.Play(this);
            GhostWolfPack.Cast(this, SkillScheduler.Compensation);
            return;
        }

        if (animation == "Skill5")
        {
            // 回旋镰斩: keep damage routed through the skill, not normal Attack2.
            CursedSweepCast.Spawn(skeletonAnimation.GetComponent<Renderer>());
            //SoundManager.Instance.PlayClip("JUMP_SKILL");

            var assets = LevelController.Instance.AssetManager;
            var position = assets.battleStyle
                ? assets.battleStyle.EnemyPosition(assets.battleCampCamera.GetComponent<Camera>(), 0)
                : new Vector3(-3, -5.5f, 0);
            assets.SpawnParticle("AOE_Particle", position);
            ProcessDmg_AOE(SkillsController.Instance.GetSkillValues(SkillsController.Instance.sinisterStampedeLevel, SkillsTypeEnum.Sinister_Stampede)[0], SkillScheduler.Compensation);
        }

        if (animation.Contains("Skill7"))
        {
            //sinister stampede
            Debug.Log("OnHit Skill 7");
            if (GameManager.single.GetAllBosses(false).Count > 0)
                ProcessDamage(SkillsController.Instance.GetSkillValues(SkillsController.Instance.bossKillerLevel, SkillsTypeEnum.Boss_Killer)[0], GameManager.single.GetAllBosses(false)[0], SkillScheduler.Compensation);
        }
    }

    void ProcessDamage(float perc, Actor_Base targetAb, float compensation = 0)
    {
        float dmgRandomized = ab.currentStat.dmg * Random.Range(.9f, 1.1f);
        VibrationsManager.Instance.CallVibe(MoreMountains.NiceVibrations.HapticTypes.LightImpact);



        #region BATTLEFIELD TACTICIAN
        float battlefieldVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Battlefield_Tactician), SkillsTypeEnum.Battlefield_Tactician)[0];

        if (battlefieldVal > 0 && GameObject.FindGameObjectsWithTag("Enemy").Length >= 2)
        {
            Debug.Log("BT before:" + dmgRandomized);
            dmgRandomized += battlefieldVal * dmgRandomized;
            Debug.Log("BT after:" + dmgRandomized);

        }
        #endregion

        dmgRandomized += compensation * Random.Range(.9f, 1.1f);
        float critChance = Random.Range(0, 100);
        bool isCrit = false;
        if (critChance < ab.currentStat.crit) isCrit = true;

        targetAb.Hit(dmgRandomized, isCrit);

        StartCoroutine(CameraEffects.single.Shake());

    }

    public void ProcessDmg_AOE(float perc = 1, float compensation = 0)
    {
        if (ab.isDying) return;
        //default     public IEnumerator Shake (float duration=.15f, float magnitude=.07f)
        StartCoroutine(CameraEffects.single.Shake(.45f, .2f));
        VibrationsManager.Instance.CallVibe(MoreMountains.NiceVibrations.HapticTypes.MediumImpact);

        var targets = GameManager.single.GetLivingEnemies(false);
        float bonus = targets.Count > 0 ? compensation / targets.Count : 0;
        foreach (Actor_Base nextAb in targets)
        {
            //first alter it by our perc param, then randomize it by 10%
            float dmgRandomized = (ab.currentStat.dmg * perc + bonus) * Random.Range(.9f, 1.1f);
            float critChance = Random.Range(0, 100);
            bool isCrit = false;
            if (critChance < ab.currentStat.crit) isCrit = true;
            nextAb.Hit(dmgRandomized, isCrit);
        }
    }

    public override void ForceAnimation(string animation, bool isLoop)
    {
        // Forced transitions still respect the combat lock; death remains an interruption.
        SetAnimation(animation, isLoop, false);
    }

    public override void AnimationComplete(int trackIndex, string animationName)
    {
        if (IsVictoryRelaxing) return;
        if (animationName.Contains("Death") && ab.ressurrectionWaiting &&
            skeletonAnimation.Skeleton.Data.FindAnimation("Revive") != null)
        {
            // Keep combat locked until the character has risen and planted her feet.
            currentAnimation = "Revive";
            skeletonAnimation.timeScale = 1;
            var revive = skeletonAnimation.AnimationState.SetAnimation(0, "Revive", false);
            revive.TimeScale = 1.2f; // Match the direct collapse in reverse.
            revive.MixDuration = 0; // Revive starts at the exact seated death pose.
            LevelController.Instance.AssetManager.SpawnParticle("Res_Particle", rootPos.transform.position);
            return;
        }
        if ((animationName == "Revive" || animationName.Contains("Death")) && ab.ressurrectionWaiting)
        {
            ab.currentStat.hpNow = ab.currentStat.hpMax * SkillsController.Instance.GetSkillValues(SkillsController.Instance.ressurrectionLevel, SkillsTypeEnum.Ressurrection)[0];
            if (animationName != "Revive")
                LevelController.Instance.AssetManager.SpawnParticle("Res_Particle", rootPos.transform.position);
            GameplayCanvas.single?.RefreshHp();
            ab.isDying = false;
            ab.ressurrectionWaiting = false;
            SummonLearnedCompanions();
            isAttacking = false;
            // Death fades weapon slots; restore them even when no target is available.
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            currentAnimation = "";
            SetAnimation("Idle1", true, false);
            if (regenVal > 0) StartCoroutine(RegenLoop());
            if (lightningVal > 0) StartCoroutine(LightningLoop());
            foreach (var actor in GameManager.single.GetLivingEnemies())
                actor.GetComponent<Enemy>().StartAttacking();
            Attack();
            return;
        }

        if (ab.isDying) return;

        if (animationName.StartsWith("Attack") || animationName.StartsWith("Skill"))
        {
            var track = skeletonAnimation.AnimationState.GetCurrent(0);
            if (!isAttacking || currentAnimation != animationName || track == null ||
                track.Animation.Name != animationName || track.TrackTime + .0001f < track.Animation.Duration) return;
            isAttacking = false;
            SkillScheduler.Complete(); lightningTargets.Clear();
            SetAnimation("Idle1", true, true, 1);
            ScheduleNextAction();
            return;
        }
        SetAnimation("Idle1", true, true, 1);
    }

    void ScheduleNextAction()
    {
        if (nextAction != null) StopCoroutine(nextAction);
        nextAction = StartCoroutine(AttackComplete());
    }

    public IEnumerator AttackComplete(float delay = ActionRecovery)
    {
        yield return new WaitForSeconds(delay);
        nextAction = null;
        Attack();
    }

    // One-shot relief followed by a low-weapon rest, independent of attack speed.
    public float BeginVictoryRelaxation()
    {
        if (isCampPlayer || ab.isDying || !skeletonAnimation) return 0;
        skeletonAnimation.Initialize(false);
        var data=skeletonAnimation.Skeleton.Data;
        var victory=data.FindAnimation("Victory");
        if (victory==null || data.FindAnimation("VictoryIdle")==null) return 0;
        StopAllCoroutines();
        ResetCombatActions();
        IsVictoryRelaxing=true;
        isAttacking=false;
        currentAnimation="Victory";
        skeletonAnimation.timeScale=1;
        var entry=skeletonAnimation.AnimationState.SetAnimation(0,"Victory",false);
        entry.MixDuration=.20f;
        var rest=skeletonAnimation.AnimationState.AddAnimation(0,"VictoryIdle",true,victory.Duration);
        rest.MixDuration=.12f;
        return victory.Duration;
    }
    #endregion

}
