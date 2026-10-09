using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services.Vibrations;
using IdleKnightHero.UI;
using UnityEngine;

public class Enemy : AnimationController
{
    public int maxSkins = 3;
    public float expGiven = 1;
    public float waitBetweenAttacks = 2;
    //public float fxpGiven = 1;
    public string bossName = "Bad Boi";
    public string attackAudio = "ATTACK1";
    public string deathAudio = "SLIME_DEATH";
    public List<Item> itemsGiven;

    public Actor_Base ab;
    public GameObject rootPos;
    public int maxAttacks = 1;
    public int chosenIdle = 1;

    public bool IsEntering => GetComponent<DropIn>() is DropIn drop && drop.IsEntering;

    private void OnEnable()
    {
        ab = GetComponent<Actor_Base>();
        tag = "Enemy";//why does tag get stripped off some of these???
    }

    private void Start()
    {
        base.Start();
        maxAttacks = base.GetMaxAttacks();
        chosenIdle = Random.Range(1, base.GetMaxIdles());
        if (!IsEntering) SetAnimation("Idle1", true, false);
        else if (!GetComponent<DropIn>().UsesSpawn) base.SetAnimation("Idle1", true, false);

        #region Randomize the Skin
        skinIndex = Random.Range(1, maxSkins + 1);
        //Debug.Log("SetSkin From Init:" + skinIndex);
        SetSkin(skinIndex.ToString());
        if (IsEntering) GetComponent<DropIn>().HoldSpawnPose();
        //base.skeletonAnimation.skeleton.SetSkin(skinIndex.ToString());
        #endregion
    }

    Coroutine attackRoutine;
    public void StopAttacking()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    public void StartAttacking(bool instant = false)
    {
        if (IsEntering || ab.isDying) return;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }
        attackRoutine = StartCoroutine(WaitAndAttack(instant));
    }

    IEnumerator WaitAndAttack(bool instant)
    {
        //Debug.Log("ienumerator attack");

        if (instant)
        {
            Attack();
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(waitBetweenAttacks * .9f, waitBetweenAttacks * 1.1f));
            Attack();
        }
    }

    //bool isAttacking = false;
    public void Attack()
    {
        //Debug.Log("Attack");
        if (IsEntering)
        {
            return;
        }
        if (ab.isDying) return;//cant attack when dying
        SetAnimation("Attack" + Random.Range(1, maxAttacks), true, false, Random.RandomRange(.9f, 1.1f));
        StartAttacking();
    }

    #region ANIMATION 
    public string currentAnimation = "";
    public override void SetAnimation(string animation, bool isLoop, bool isOnComplete, float speed = 1)
    {
        if (IsEntering) return;
        if (animation == currentAnimation) return;//dont let anything overwrite identical animation
        if (currentAnimation.Contains("Attack") && animation.Contains("Hit"))
        {
            //Debug.Log("Getting hit, while attacking");
        }
        //Debug.Log("SetAnimation Enemy:" + animation);
        currentAnimation = animation;
        base.SetAnimation(animation, isLoop, isOnComplete, speed);
    }

    public override void AnimationOnHit(string animation)
    {
        if (IsEntering || ab.isDying) return;
        if (animation.Contains("Attack"))
        {
            SoundManager.Instance.PlayClip(attackAudio, false, false);
            if (!Player.single.ab.isDying)
            {
                float dmgRandomized = ab.currentStat.dmg * Random.Range(.9f, 1.1f);

                float critChance = Random.Range(0, 100);
                bool isCrit = false;
                if (critChance < ab.currentStat.crit) isCrit = true;
                Player.single.ab.Hit(dmgRandomized, isCrit, ab.currentStat.pierce);
                StartCoroutine(CameraEffects.single.Shake());
            }
            else
            {
                Player.single.GetComponentInChildren<MaterialSwapper>().CallWhiteFlash();
            }
        }
    }

    public override void AnimationComplete(int trackIndex, string animationName)
    {
        if (IsEntering) return;

        if (animationName.Contains("Death"))
        {
            if (!string.IsNullOrEmpty(bossName))
            {
                VibrationsManager.Instance.CallVibe(MoreMountains.NiceVibrations.HapticTypes.Success);
            }

            DestroyMe();
            return;
        }
        //Debug.Log("Anim complete enemy:" + animationName);
        SetAnimation("Idle1", true, false, 1);//idle for now - waiting for attack

        //StartAttacking();
    }
    public void SetSkin(string s)
    {
        base.skeletonAnimation.skeleton.SetSkin(s);

    }
    public int skinIndex = 0;

    #endregion

    public void Drops()
    {
        for (int i = 0; i < itemsGiven.Count; i++)
        {
            float chance = Random.Range(0, 1f);
            if (chance <= itemsGiven[i].m_ChanceDrop)
            {
                Item newItem = InventoryManager.single.CreateItemStats(itemsGiven[i]);
                //for now just keep same name
                newItem.m_ItemFinalTitle = InventoryManager.single.GetRandomName(newItem);
                newItem.uniqueId = Random.Range(0, 999999);
                InventoryManager.single.AddItem(newItem);
                GameplayCanvas.single.ShowItemPopUp(newItem);
            }

        }
    }

    void DestroyMe()
    {
        if (GameManager.single.GetLivingEnemies().Count == 0)
        {
            WaveManager.single.WaveComplete();
        }

        Destroy(gameObject);
    }
}
