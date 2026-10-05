using System.Collections;
using System.Collections.Generic;
using IdleKnightHero.UI;
using Scripts.Skills;
using UnityEngine;
using UnityEngine.UI;

public class Actor_Base : MonoBehaviour
{
    /// <summary>
    /// Stuff that player and enemy commonly do, should go here
    /// </summary>
    [Range(0, 100)]
    public int chanceInterruptAttack = 1;
    public Stat baseStat, currentStat;
    public bool isPlayer = false;
    public bool isDying = false;
    public bool isBoss = false;
    public EnemyLifebar enemyLifebar;

    Enemy enemy;

    private void Start()
    {
        if (!isPlayer)
        {
            enemy = GetComponent<Enemy>();
            enemyLifebar = transform.GetChild(0).GetComponent<EnemyLifebar>();
        }
        InitActor();
    }

    private void InitActor()
    {
        if (!isPlayer) //player uses a different path that calc through GameManager instead
        {
            Calc();
            CallRefreshHp();
        }
    }

    public void Calc()
    {
        currentStat = Utilities.DeepClone(baseStat);
    }

    public void HealByPercent(float perc)
    {
        float healAmt = (currentStat.hpMax * perc);
        //Debug.Log("Healed:" + healAmt);
        currentStat.hpNow += healAmt;
        ClampHp();
        CallRefreshHp();
    }

    public void AddAbsorb(float val)
    {
        //dont have clamp
        LevelController.Instance.AssetManager.SpawnParticle("Absorb_Particle", Player.single.rootPos.transform.position + new Vector3(0, .5f, 0));
        float absorb = currentStat.hpMax * .2f;
        currentStat.absorb += absorb;
        currentStat.hpNow += absorb;

        CallRefreshHp();
    }

    public void ClampHp()
    {
        if (currentStat.absorb > 0) return;//dont bother clamping if we have absrob we're allowed to go over max
        if (currentStat.hpNow > currentStat.hpMax) currentStat.hpNow = currentStat.hpMax;
    }
    public void Hit(float dmg, bool isCrit = false, float incomingPierce = 0, float critDmg = 150, bool isPet = false, bool isLightning = false)
    {

        if (isPlayer)
        {
            #region Chance Block
            if (Random.Range(0, 1f) < currentStat.block)
            {
                LevelController.Instance.AssetManager.SpawnParticle("Shield_Particle", Player.single.rootPos.transform.position + new Vector3(0, 4, 0));
                return;
            }
            #endregion

            //GET DEF - For use with Pierce also Dmg calc-ing
            float currentDef = currentStat.def;

            //CALCULATE PIERCE IF APPLICABLE
            float piercedDmg = 0;
            if (incomingPierce > 0 && currentDef >= dmg)
            {
                piercedDmg = dmg * (incomingPierce * .01f);
            }

            //REMOVE DEF
            dmg = dmg - currentDef;

            //APPLY DMG FLOOR
            if (dmg < 1) dmg = 1;

            //APPLY PIERCE VALUE
            dmg += piercedDmg;

            //CALCULATE CRIT - AFTER DEF
            if (isCrit)
            {
                //Debug.Log("DMG before crit:" + dmg);
                //Debug.Log("CRIT was:" + critDmg);
                dmg *= (critDmg * .01f);
                //Debug.Log("Damage after crit:" + dmg);
                #region LUCKY STARS - Player took a crit
                if (Player.single.myLuckyStarsVal > 0)
                {
                    float rngLuckyStars = Random.Range(0, 1f);
                    if (Player.single.myLuckyStarsVal > rngLuckyStars)
                    {
                        //Debug.Log("Should play lucky stars");
                        AddAbsorb(.2f); // give 20% absorb
                    }
                }
                #endregion
            }

            dmg = Mathf.Round(dmg);

            //Player.single.SetAnimation("Hit", false, false);
            Player.single.GetComponentInChildren<MaterialSwapper>().CallWhiteFlash();
            //play text pop up damage

            LevelController.Instance.AssetManager.SpawnPopUpText(Player.single.rootPos.transform.position, isPlayer, dmg, isCrit, false, isLightning);
        }
        else //we are an enemy
        {
            LevelController.Instance.AssetManager.SpawnParticle("Hit_Particle", enemy.rootPos.transform.position);

            #region INTERRUPT CHANCE
            if (enemy.currentAnimation.Contains("Attack"))
            {
                int randomInterrupt = Random.RandomRange(0, 100);
                if (randomInterrupt <= enemy.ab.chanceInterruptAttack)
                {
                    //Debug.Log("Interrupted Attack");
                    enemy.SetAnimation("Hit", false, false, 1); //play animation cus we interrupted
                }

            }
            else
            {
                //Debug.Log("Could not interrupt - no chance rolled");
                enemy.SetAnimation("Hit", false, false, 1); //play animation always - we aren't attacking
            }
            #endregion
            //enemy.GetComponentInChildren<MaterialSwapper>().CallWhiteFlash();
            dmg = dmg - currentStat.def;
            if (isCrit)
            {
                //Debug.Log("DMG before crit:" + dmg);
                //Debug.Log("CRIT was:" + Player.single.ab.currentStat.critDmg);
                dmg *= (Player.single.ab.currentStat.critDmg * .01f);
                //Debug.Log("Damage after crit:" + dmg);

                //dmg *= (critDmg * .01f);
            }
            dmg = Mathf.Round(dmg);
            if (dmg < 1) dmg = 1;
            //play text pop up damage
            LevelController.Instance.AssetManager.SpawnPopUpText(enemy.rootPos.transform.position, isPlayer, dmg, isCrit, isPet, isLightning);
        }

        currentStat.hpNow -= dmg;
        if (isPlayer)
        {
            if (currentStat.absorb > 0) currentStat.absorb -= dmg;
            if (currentStat.absorb < 0) currentStat.absorb = 0;
        }

        if (currentStat.hpNow < 1)
        {
            currentStat.hpNow = 0;

            Death();
        }
        CallRefreshHp();
        //GetComponent<MaterialSwapper>().CallWhiteFlash();
    }

    public void Resurrect()
    {

    }

    public bool ressurrectionWaiting = false;
    public void Death()
    {
        if (isDying) return;
        if (isPlayer)
        {
            Player.single.StopAllCoroutines();
            SoundManager.Instance.PlayClip("PLAYER_DEATH", false, false);
            if (PetDragon.single != null) PetDragon.single.FlyOff();
            #region ROLL FOR RESSURRECTIOn
            float resChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Ressurrection), SkillsTypeEnum.Ressurrection)[0];
            float rand = Random.Range(0, 1f);
            //Debug.Log("res:" + resChance + " roll:" + rand);
            if (resChance > rand)
            {
                SoundManager.Instance.PlayClip("RESSURRECTION", false, false);
                ressurrectionWaiting = true;
            }
            else
            {
                ressurrectionWaiting = false;
                Router.single.StartCoroutine(Router.single.GoToDefeat());
            }
            #endregion


            Player.single.SetAnimation("Death1", false, false);
            isDying = true;

        }
        else
        {
            isDying = true;
            tag = "DyingEnemy";
            SoundManager.Instance.PlayClip(enemy.deathAudio, false, false);
            enemy.SetAnimation("Death1", false, false);
            //GameManager.single.IncFusion(enemy.fxpGiven);
            enemy.Drops();


            #region ROLL FOR TWICE THE FUN
            float twiceChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Twice_The_Fun), SkillsTypeEnum.Twice_The_Fun)[0];
            float twiceRand = Random.Range(0, 1f);
            //Debug.Log("lucky:" + luckyChance + " roll:" + rand);
            if (twiceChance > twiceRand)
            {
                LevelController.Instance.AssetManager.SpawnNotifierText(Player.single.rootPos.transform.position + new Vector3(0, 1.7f, 0));
                GameManager.single.IncXp(enemy.expGiven * 2);

            }
            else
            {
                GameManager.single.IncXp(enemy.expGiven);//once the fun - normal xp ;)
            }
            #endregion

            #region ROLL FOR LUCKY BASTARD
            float luckyChance = SkillsController.Instance.GetSkillValues(SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Lucky_Bastard), SkillsTypeEnum.Lucky_Bastard)[0];
            float rand = Random.Range(0, 1f);
            //Debug.Log("lucky:" + luckyChance + " roll:" + rand);
            if (luckyChance > rand && !Player.single.ab.isDying)
            {
                SoundManager.Instance.PlayClip("HEAL", false, false);
                Player.single.ab.currentStat.hpNow += Player.single.ab.currentStat.hpMax * .2f;//add 20 % hp for a kill that passed lucky bastard RNG
                GameplayCanvas.single.RefreshHp();
                LevelController.Instance.AssetManager.SpawnParticle("Heal_Particle", Player.single.rootPos.transform.position + new Vector3(0, 1.7f, 0));

            }
            #endregion
        }

    }

    public void CallRefreshHp()
    {


        float perc = currentStat.hpNow / currentStat.hpMax;
        if (isPlayer)
        {
            //Debug.Log("Player.single.ab.current.absorb:" + Player.single.ab.currentStat.absorb);

            GameplayCanvas.single.RefreshHp();
            if (Player.single.ab.currentStat.absorb > 0)
            {
                //turn life purple
                GameplayCanvas.single.hpMeter.GetComponent<Image>().color = GameplayCanvas.single.cPurple;
            }
            else
            {
                GameplayCanvas.single.hpMeter.GetComponent<Image>().color = GameplayCanvas.single.cRed;
            }
        }
        else
        {
            enemyLifebar.RefreshHp();
        }
    }

}

[System.Serializable]
public class Stat
{
    public float hpNow = 0;
    public float hpMax = 0;
    public float hpPerc = 0;
    public float dmg = 0;
    public float dmgPerc = 0;
    public float def = 0;
    public float defPerc = 0;
    public float block = 0;
    public float atkSpd = 0;
    public float crit = 0;
    public float critDmg = 0;
    public float pierce = 0;
    public float absorb = 0;

}

[System.Serializable]
public class RollStat
{
    public float dmgMin, dmgMax;
    public float defMin, defMax;
    public float atkSpdMin, atkSpdMax;
    public float critMin, critMax;
    public float critDmgMin, critDmgMax;
    public float hpMin, hpMax_rolled;
    public float dmgPercMin, dmgPercMax;
    public float hpPercMin, hpPercMax;
    public float defPercMin, defPercMax;
    public float percChanceBonus1 = 0;
    public float percChanceBonus2 = 0;
    public float percChanceBonus3 = 0;
    public float percChanceBonus4 = 0;
}

