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
    public Actor_Base ab;
    public GameObject rootPos;

    [Tooltip("Disable for characters with a fixed costume, such as Red Hood. Equipment stats still apply.")]
    public bool useEquipmentAppearance = true;
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
    }

    public void InitPlayer()
    {
        skeletonAnimation.transform.localPosition = originalVisualPosition;
        skeletonAnimation.transform.localScale = originalVisualScale;
        var shadow = transform.Find("Battle Contact Shadow");
        if (shadow) shadow.gameObject.SetActive(false);
        currentAnimation = "";
        isAttacking = false;
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

            if (SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Tairon_The_Dragon) > 0)
            {
                LevelController.Instance.AssetManager.SpawnGeneralObj("PetDragon");
            }


        }

        ab.isDying = false;

        ab.currentStat = GameManager.single.CalcPlayer();
        ab.currentStat.hpNow = ab.currentStat.hpMax;//fill hp
        GameplayCanvas.single.RefreshHp();
        GameplayCanvas.single.FillHp();

        if (isCampPlayer)
        {
            StopAllCoroutines();
            SetAnimation("Idle1", true, false, 1);

            GameplayCanvas.single.RefreshHp();
            GameplayCanvas.single.RefreshXp();
        }
        else
        {
            StopAllCoroutines();

            //boot up our skills
            regenVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Regen), SkillsTypeEnum.Regen)[0];
            if (regenVal > 0)
                StartCoroutine(RegenLoop());

            lightningVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Lightning_Demi_God), SkillsTypeEnum.Lightning_Demi_God)[0];
            if (lightningVal > 0)
                StartCoroutine(LightningLoop());

            myLuckyStarsVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.My_Lucky_Stars), SkillsTypeEnum.My_Lucky_Stars)[0];

            sinisterStampedeChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Sinister_Stampede), SkillsTypeEnum.Sinister_Stampede)[0];
            bossKillerChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Boss_Killer), SkillsTypeEnum.Boss_Killer)[0];

            pathCleanerChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Path_Cleaner), SkillsTypeEnum.Path_Cleaner)[0];

            //start our attack
            Attack();
        }

    }


    public float myLuckyStarsVal;
    public float regenVal;
    public float lightningVal;
    public float sinisterStampedeChance;
    public float pathCleanerChance;
    public float bossKillerChance;

    IEnumerator RegenLoop()
    {
        yield return new WaitForSeconds(2);
        if (!ab.isDying)
        {
            ab.HealByPercent(regenVal);
            StartCoroutine("RegenLoop");
        }
    }

    IEnumerator LightningLoop()
    {
        yield return new WaitForSeconds(Random.Range(1, 3.0f));
        if (!ab.isDying)
        {
            //Debug.Log("Try some lightning");
            foreach (Actor_Base nextAb in GameManager.single.GetLivingEnemies(false))
            {
                if (nextAb.currentStat.hpNow < (nextAb.currentStat.hpMax / 2))
                {
                    //Debug.Log("Cast a lightning");
                    SoundManager.Instance.PlayClip("LIGHTNING");
                    LevelController.Instance.AssetManager.SpawnParticle("Lightning_Particle", nextAb.transform.position);
                    float lightningDmg = Player.single.ab.currentStat.dmg * lightningVal;
                    nextAb.Hit(lightningDmg, false, 0, 150, false, true);
                }
            }


            StartCoroutine("LightningLoop");
        }
    }

    public bool isAttacking = false;
    public int attackIndexer = 0;
    public bool shouldUse3rdAttackBoost = false;

    void Attack()
    {
        if (isCampPlayer) return;//skip the attack we are not in battle

        if (GameManager.single.GetLivingEnemies(false).Count == 0)
        {
            //Debug.Log("No Enemies");
            StartCoroutine(AttackComplete(.2f));
            return;
        }

        #region Last But Not Least
        shouldUse3rdAttackBoost = false;
        attackIndexer++;
        if (SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Last_But_Not_Least) > 0)
        {
            if (Utilities.IsDivisible(attackIndexer, 3))
            {
                shouldUse3rdAttackBoost = true;
            }
        }
        #endregion

        #region Sinister Stampede Override
        if (GameManager.single.GetLivingEnemies(false).Count >= 2)
        {
            if (Random.Range(0, 1f) <= sinisterStampedeChance)
            {
                //Debug.Log("Could do a sinister Stampede!");
                SetAnimation("Skill5", false, false, ab.currentStat.atkSpd * 0.01f);
                SoundManager.Instance.PlayClip("JUMP_SKILL");
                return;
            }
        }
        #endregion

        #region Boss Killer
        if (GameManager.single.GetAllBosses(false).Count >= 1)
        {
            if (Random.Range(0, 1f) <= bossKillerChance)
            {
                Debug.Log("Rolled for boss killer");
                SetAnimation("Skill7", false, false, ab.currentStat.atkSpd * 0.01f);
                SoundManager.Instance.PlayClip("JUMP_SKILL");
                return;
            }
        }
        #endregion


        #region Path Cleaner
        if (Random.Range(0, 1f) <= pathCleanerChance)
        {
            //Debug.Log("Could do a sinister Stampede!");
            SetAnimation("Skill4", false, false, ab.currentStat.atkSpd * 0.01f);
            return;
        }
        #endregion

        if (ab.isDying)
        {
            AttackComplete();
            return;
        }

        int attackNum = Random.Range(1, 3);
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
        if (ab.isDying) return;
        if (isAttacking && !animation.Contains("Death")) return;//dont let anything happen until attack finishes
        if (animation == currentAnimation) return;//dont let anything overwrite identical animation

        currentAnimation = animation;
        base.SetAnimation(animation, isLoop, isOnComplete, speed);
    }

    public override void AnimationOnHit(string animation)
    {
        if (animation.Contains("Attack") || animation.Contains("Skill4")/*path cleaner*/)
        {
            SoundManager.Instance.PlayClip("ATTACK1");
            if (GameManager.single.GetLivingEnemies(false).Count > 0)
            {

                ProcessDamage(1, GameManager.single.GetLivingEnemies(false)[0]);
            }
        }

        if (animation.Contains("Skill5"))
        {
            //sinister stampede
            //SoundManager.Instance.PlayClip("JUMP_SKILL");

            var assets = LevelController.Instance.AssetManager;
            var position = assets.battleStyle
                ? assets.battleStyle.EnemyPosition(assets.battleCampCamera.GetComponent<Camera>(), 0)
                : new Vector3(-3, -5.5f, 0);
            assets.SpawnParticle("AOE_Particle", position);
            ProcessDmg_AOE();
        }

        if (animation.Contains("Skill7"))
        {
            //sinister stampede
            Debug.Log("OnHit Skill 7");
            if (GameManager.single.GetAllBosses(false).Count > 0)
                ProcessDamage(1.5f, GameManager.single.GetAllBosses(false)[0]);
        }
    }

    void ProcessDamage(float perc, Actor_Base targetAb)
    {
        float dmgRandomized = ab.currentStat.dmg * Random.Range(.9f, 1.1f);
        VibrationsManager.Instance.CallVibe(MoreMountains.NiceVibrations.HapticTypes.LightImpact);

        #region LAST BUT NOT LEAST
        if (shouldUse3rdAttackBoost)
        {
            //Debug.Log("3rd hit before:" + dmgRandomized);
            dmgRandomized += dmgRandomized * SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Last_But_Not_Least), SkillsTypeEnum.Last_But_Not_Least)[0];
            //Debug.Log("3rd hit after:" + dmgRandomized);
            shouldUse3rdAttackBoost = false;
        }
        #endregion

        #region BATTLEFIELD TACTICIAN
        float battlefieldVal = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Battlefield_Tactician), SkillsTypeEnum.Battlefield_Tactician)[0];

        if (battlefieldVal > 0 && GameObject.FindGameObjectsWithTag("Enemy").Length >= 2)
        {
            Debug.Log("BT before:" + dmgRandomized);
            dmgRandomized += battlefieldVal * dmgRandomized;
            Debug.Log("BT after:" + dmgRandomized);

        }
        #endregion

        float critChance = Random.Range(0, 100);
        bool isCrit = false;
        if (critChance < ab.currentStat.crit) isCrit = true;

        targetAb.Hit(dmgRandomized, isCrit);

        StartCoroutine(CameraEffects.single.Shake());

    }

    public void ProcessDmg_AOE(float perc = 1)
    {
        //default     public IEnumerator Shake (float duration=.15f, float magnitude=.07f)
        StartCoroutine(CameraEffects.single.Shake(.45f, .2f));
        VibrationsManager.Instance.CallVibe(MoreMountains.NiceVibrations.HapticTypes.MediumImpact);

        foreach (Actor_Base nextAb in GameManager.single.GetLivingEnemies(false))
        {
            //first alter it by our perc param, then randomize it by 10%
            float dmgRandomized = (ab.currentStat.dmg * perc) * Random.Range(.9f, 1.1f);
            float critChance = Random.Range(0, 100);
            bool isCrit = false;
            if (critChance < ab.currentStat.crit) isCrit = true;
            nextAb.Hit(dmgRandomized, isCrit);
        }
    }

    public override void ForceAnimation(string animation, bool isLoop)
    {
        base.ForceAnimation(animation, isLoop);
    }

    public override void AnimationComplete(int trackIndex, string animationName)
    {
        if (animationName.Contains("Death") && ab.ressurrectionWaiting)
        {
            ab.currentStat.hpNow = ab.currentStat.hpMax * .4f;
            LevelController.Instance.AssetManager.SpawnParticle("Res_Particle", rootPos.transform.position);
            GameplayCanvas.single.RefreshHp();
            ab.isDying = false;
            ab.ressurrectionWaiting = false;
            ForceAnimation("Attack1", false);
            currentAnimation = "Idle1";
            isAttacking = false;

        }



        if (animationName.Contains("Attack") || animationName.Contains("Skill"))
        {
            StartCoroutine(AttackComplete());
        }

        SetAnimation("Idle1", true, true, 1);
        //base.AnimationComplete(trackIndex, animationName);
    }

    public IEnumerator AttackComplete(float delay = 0)
    {
        yield return new WaitForSeconds(.2f);
        isAttacking = false;
        Attack();
    }
    #endregion

}
