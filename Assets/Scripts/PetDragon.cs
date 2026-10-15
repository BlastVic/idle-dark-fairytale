using IdleKnightHero.UI;
using Scripts.Skills;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetDragon : AnimationController
{
    private static PetDragon _instance;
    public static PetDragon single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<PetDragon>();
            return _instance;
        }
    }

    BookSpineCompanion groundCompanion;
    public void FlyOff()
    {
        if (groundCompanion) { StopAllCoroutines(); isAttacking=false; groundCompanion.Exit(); return; }
        SetAnimation("Exit", false, false, 1);
    }

    public GameObject rootPos;
    public float breathOfHellChance = 0;
    private void Start()
    {
        base.Start();
        groundCompanion=GetComponent<BookSpineCompanion>();
        breathOfHellChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Breath_Of_Hell), SkillsTypeEnum.Breath_Of_Hell)[0];

        myDmg = Player.single.ab.currentStat.dmg * SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Tairon_The_Dragon), SkillsTypeEnum.Tairon_The_Dragon)[0];
        if (myDmg < 1) myDmg = 1;
        Attack();
    }

    public bool isAttacking = false;

    void Attack()
    {
        if (groundCompanion) {
            if (groundCompanion.Exiting || !Player.single || Player.single.ab.isDying) return;
            if (GameManager.single.GetLivingEnemies(false).Count == 0) {
                SetAnimation("Idle1",true,true); StartCoroutine(AttackComplete()); return;
            }
        }
        #region Breath Of Hell

        if (Random.Range(0, 1f) <= breathOfHellChance)
        {

            if (Random.Range(0, 100) < 50)
            {
                //Debug.Log("Could do Breath of Hell 2!");

                SetAnimation("Attack2", false, false, 1);
            }
            else
            {
                //Debug.Log("Could do Breath of Hell 3!");

                SetAnimation("Attack3", false, false, 1);
            }
            return;
        }

        #endregion


        SetAnimation("Attack1", false, false, 1);
    }

    #region ANIMATION 
    public string currentAnimation = "";
    public override void SetAnimation(string animation, bool isLoop, bool isOnComplete, float speed = 1)
    {
        if (groundCompanion && groundCompanion.Exiting) return;
        if (!Player.single || Player.single.ab.isDying) return;
        if (isAttacking) return;//dont let anything happen until attack finishes
        if (animation == currentAnimation) return;//dont let anything overwrite identical animation

        currentAnimation = animation;
        base.SetAnimation(animation, isLoop, isOnComplete, speed);
    }

    public override void AnimationOnHit(string animation)
    {
        if (groundCompanion && (groundCompanion.Exiting || !Player.single || Player.single.ab.isDying)) return;
        if (animation.Contains("Attack") || animation.Contains("Skill4")/*path cleaner*/)
        {
            SoundManager.Instance.PlayClip(groundCompanion && animation == "Attack2" ? "ATTACK1" : "FIRE");
            if (GameManager.single.GetLivingEnemies(false).Count > 0)
            {

                var target = GameManager.single.GetLivingEnemies(false)[0];
                var effects = GetComponent<BookSpinePetEffects>();
                if (effects) effects.Hit(animation, target);
                ProcessDamage(1, target);
            }
        }


    }

    public float myDmg = 1;
    void ProcessDamage(float perc, Actor_Base targetAb)
    {
        float dmgRandomized = myDmg * Random.Range(.9f, 1.1f);



        bool isCrit = false;

        targetAb.Hit(dmgRandomized, isCrit, 0, 150, true, suppressDefaultHitVfx: GetComponent<BookSpinePetEffects>() != null);

    }



    public override void ForceAnimation(string animation, bool isLoop)
    {
        base.ForceAnimation(animation, isLoop);
    }

    public override void AnimationComplete(int trackIndex, string animationName)
    {
        if (groundCompanion && groundCompanion.Exiting) return;


        if (animationName.Contains("Attack") || animationName.Contains("Skill"))
        {
            StartCoroutine(AttackComplete());
        }

        SetAnimation("Idle1", true, true, 1);
        //base.AnimationComplete(trackIndex, animationName);
    }

    public IEnumerator AttackComplete(float delay = 0)
    {
        yield return new WaitForSeconds(.3f);
        isAttacking = false;
        Attack();
    }
    #endregion

}
