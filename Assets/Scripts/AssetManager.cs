using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.Enum;
using IdleKnightHero.UI;
using Scripts.Level;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class AssetManager : MonoBehaviour
{
    [Header("Lookup Info")]
    public Color[] _colorsDb;

    [Header("Normal Prefabs")]
    public GameObject playerPrefab;
    public DarkFairytaleBattleStyle battleStyle;
    private int nextEnemySlot;
    private GameObject fairytaleBackdrop;
    public GameObject[] enemyPositions;
    public GameObject popUpText, notifierText;

    [Header("Load Once Object Refs")]
    public GameObject rebirthPackageObj;
    public GameObject mapPackageObj;
    public GameObject campPackageObj;
    public GameObject fusionPackageObj;
    public GameObject battleCampCamera, menuCamera;
    public GameObject playerObj;

    public Vector3 GetNextPosition(bool isMini = false)
    {
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Count();
        nextEnemySlot = enemyCount;
        if (battleStyle) return battleStyle.EnemyPosition(battleCampCamera.GetComponent<Camera>(), enemyCount);
        if (enemyCount >= enemyPositions.Count()) enemyCount = enemyPositions.Count() - 1;
        return enemyPositions[enemyCount].transform.position;
    }

    public void SpawnPopUpText(Vector3 pos, bool hitPlayer, float val, bool isCrit, bool isPurple = false, bool isBlue = false)
    {
        GameObject next = GameObject.Instantiate(popUpText);
        next.transform.position = pos + new Vector3(Random.Range(-.3f, .3f), Random.Range(-.3f, .3f), 0);
        next.GetComponent<PopUpText>().SetMe(hitPlayer, isCrit, val);
        if (isPurple) next.GetComponent<PopUpText>().TurnPurple();
        if (isBlue) next.GetComponent<PopUpText>().TurnBlue();

    }

    public void SpawnNotifierText(Vector3 pos)
    {
        GameObject next = GameObject.Instantiate(notifierText);
        next.transform.position = pos + new Vector3(Random.Range(-.3f, .3f), Random.Range(-.3f, .3f), 0);
    }

    bool IsPositionInback()
    {
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Count();
        if (enemyCount == 3) return true; //this guy is middle back
        return false;
    }


    public void TurnOnPlayer(bool isCampPlayer = false)
    {
        if (playerObj == null)
        {
            GameObject obj = GameObject.Instantiate(playerPrefab);
            playerObj = obj;
        }
        playerObj.SetActive(true);
        playerObj.GetComponent<Player>().isCampPlayer = isCampPlayer;
        playerObj.GetComponent<Player>().InitPlayer();
    }


    public void TurnOnMapPackage()
    {
        if (mapPackageObj == null)
        {
            SpawnGeneralObj("MapPackage");
        }
        else
        {
            mapPackageObj.SetActive(true);
        }
    }

    public void TurnOnCampPackage(float targScale)
    {
        if (campPackageObj == null)
        {
            SpawnGeneralObj("CampPackage");
        }
        else
        {
            campPackageObj.SetActive(true);
            campPackageObj.transform.localScale = new Vector3(targScale, targScale, targScale);
        }
    }

    public void HideMapPackage()
    {
        if (mapPackageObj != null) mapPackageObj.SetActive(false);
    }

    public void HideCampPackage()
    {
        if (campPackageObj != null) campPackageObj.SetActive(false);
    }

    public void HidePlayer()
    {
        if (playerObj != null)
        {
            Player.single.SetAnimation("Idle1", true, false, 1);
            playerObj.SetActive(false);
        }
    }

    #region SPAWN PARTICLE
    private AssetReference nextParticle;
    public void SpawnParticle(string assetKey, Vector3 position)
    {
        nextParticle = GetAssetByKey(AssetType.PARTICLE, assetKey);
        Addressables.InstantiateAsync(nextParticle, position, Quaternion.identity);
    }

    //private void SpawnEnemy_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    //{
    //    spawningEnemy = false;
    //    if (IsPositionInback())
    //    {
    //        Debug.Log("This guy is a back row guy");
    //        obj.Result.gameObject.transform.localScale = new Vector3(.85f, .85f, .85f);
    //    }
    //}
    #endregion



    #region SPAWN ENEMY
    private AssetReference nextEnemy;
    public bool spawningEnemy = false;
    public bool isFrontEnemy = true;
    public void SpawnEnemy(string assetKey, Vector3 position)
    {
        spawningEnemy = true;
        nextEnemy = GetAssetByKey(AssetType.ENEMY, assetKey);
        Addressables.InstantiateAsync(nextEnemy, position, Quaternion.identity).Completed += SpawnEnemy_Complete;
    }

    private void SpawnEnemy_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        spawningEnemy = false;
        Enemy nextEnemy = obj.Result.GetComponent<Enemy>();
        if (battleStyle)
        {
            battleStyle.PlaceEnemy(nextEnemy, battleCampCamera.GetComponent<Camera>(), nextEnemySlot);
            return;
        }
        bool overrideInBack = false;

        if (nextEnemy.ab != null && nextEnemy.ab.enemyLifebar != null && nextEnemy.ab.enemyLifebar.isMiniBoss)
        {
            Debug.Log("Mini boss reset pos");
            overrideInBack = true;
            nextEnemy.ab.transform.position = LevelController.Instance.AssetManager.enemyPositions[3].transform.position; //mini boss always getting same position
        }
        if (IsPositionInback() || overrideInBack)
        {
            var body = nextEnemy.skeletonAnimation.transform;
            // Preserve the prefab's authored facing when reducing back-row size.
            body.localScale = new Vector3(Mathf.Sign(body.localScale.x) * .85f, .85f, .85f);

        }
    }
    #endregion

    #region SPAWN MAP
    private AssetReference nextMap;
    public bool spawningMap = false;
    public void SpawnMap(string assetKey)
    {
        if (battleStyle)
        {
            if (!fairytaleBackdrop)
                fairytaleBackdrop = battleStyle.CreateBackdrop(battleCampCamera.GetComponent<Camera>());
            spawningMap = false;
            return;
        }
        spawningMap = true;
        nextMap = GetAssetByKey(AssetType.MAP, assetKey);
        Addressables.InstantiateAsync(nextMap, Vector3.zero, Quaternion.identity).Completed += SpawnMap_Complete;
    }

    private void SpawnMap_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        spawningMap = false;
        obj.Result.tag = "MapGraphics";
    }

    #endregion


    #region SPAWN General Object
    private AssetReference nextGeneralObj;
    public bool spawningGeneralObj = false;
    private string lastGeneralAsset;
    public GameObject chestDropInObj;
    public ChestGrade nextDropInGrade;
    public void SpawnGeneralObj(string assetKey)
    {
        //Debug.Log(assetKey);
        lastGeneralAsset = assetKey;
        spawningGeneralObj = true;
        nextGeneralObj = GetAssetByKey(AssetType.GENERAL, assetKey);
        Addressables.InstantiateAsync(nextGeneralObj, Vector3.zero, Quaternion.identity).Completed += SpawnGeneral_Complete;
    }



    private void SpawnGeneral_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        Debug.Log("Last general asset:" + lastGeneralAsset);
        spawningGeneralObj = false;
        if (lastGeneralAsset == "RebirthPackage") rebirthPackageObj = obj.Result;

        if (lastGeneralAsset == "MapPackage") mapPackageObj = obj.Result;
        if (lastGeneralAsset == "CampPackage")
        {
            campPackageObj = obj.Result;
        }
        if (lastGeneralAsset == "ChestDropIn")
        {
            chestDropInObj = obj.Result;

            chestDropInObj.GetComponent<ChestDropInSwapper>().SetMe(nextDropInGrade);

        }
        if (lastGeneralAsset == "CherriesChestDropIn")
        {
            chestDropInObj = obj.Result;
            chestDropInObj.transform.localPosition = new Vector3(0, 0, 0);
            SoundManager.Instance.PlayClip("CHERRY_CHEST");
            //premium chests will use cherries particles

        }



        if (lastGeneralAsset == "ChestPackage") ChestPackage.single.SetMe(GameplayCanvas.single.lastChestClicked);
        if (lastGeneralAsset == "FusionPackage") fusionPackageObj = obj.Result;


    }
    #endregion

    private AssetReference GetAssetByKey(AssetType assetType, string key)
    {
        switch (assetType)
        {
            case AssetType.ENEMY:
                return LevelDataDescriptions.EnemyDb.Where(x => x.key == key).FirstOrDefault().assetRef; //get the enemy
            case AssetType.MAP:
                return LevelDataDescriptions.MapDb.Where(x => x.key == key).FirstOrDefault().assetRef; //get the enemy
            case AssetType.PARTICLE:
                return LevelDataDescriptions.ParticleDb.Where(x => x.key == key).FirstOrDefault().assetRef; //get the enemy
            case AssetType.GENERAL:
                return LevelDataDescriptions.GeneralObjectsDb.Where(x => x.key == key).FirstOrDefault().assetRef; //get the enemy
        }
        return null;
    }
}
