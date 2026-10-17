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
    private string fairytaleMapKey;
    private ChapterEnvironmentCatalog environmentCatalog;
    public GameObject[] enemyPositions;
    public GameObject popUpText, notifierText;

    [Header("Load Once Object Refs")]
    public GameObject rebirthPackageObj;
    public GameObject mapPackageObj;
    public bool IsShelterReady => GameplayCanvas.single && GameplayCanvas.single.homeUI;
    public GameObject fusionPackageObj;
    public GameObject battleCampCamera;
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
        var next = Instantiate(popUpText).GetComponent<StorybookCombatFloat>();
        next.Present(pos, battleCampCamera.GetComponent<Camera>(), hitPlayer, isCrit, val, isPurple, isBlue);
    }

    public void SpawnNotifierText(Vector3 pos)
    {
        // Battle HUD displays the actual XP gain above the portrait, including bonuses.
        var hud=GameplayCanvas.single?GameplayCanvas.single.battleHud:null;
        if(hud&&hud.isActiveAndEnabled&&hud.statusDetails)return;
        var next = Instantiate(notifierText).GetComponent<StorybookCombatFloat>();
        next.Present(pos, battleCampCamera.GetComponent<Camera>());
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


    public void HideMapPackage()
    {
        if (mapPackageObj != null) mapPackageObj.SetActive(false);
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
        LocalPrefabLoader.InstantiateAsync(nextParticle, position, Quaternion.identity).Completed += LocalPrefabLoader.Release;
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
        LocalPrefabLoader.InstantiateAsync(nextEnemy, position, Quaternion.identity).Completed += SpawnEnemy_Complete;
    }

    private void SpawnEnemy_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        spawningEnemy = false;
        var instance = obj.Result;
        LocalPrefabLoader.Release(obj);
        if (!instance) return;
        Enemy nextEnemy = instance.GetComponent<Enemy>();
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
            string levelKey = WaveManager.single && WaveManager.single.currentWaveset != null ? WaveManager.single.currentWaveset.levelKey : null;
            string environmentKey = levelKey + "|" + assetKey;
            if (!fairytaleBackdrop || fairytaleMapKey != environmentKey)
            {
                // Release effects before creating the replacement. Deferred destruction
                // must not re-enable effects owned by the new backdrop.
                if (fairytaleBackdrop)
                {
                    fairytaleBackdrop.GetComponent<DarkFairytaleBackdrop>().ReleaseCameraEffects();
                    fairytaleBackdrop.SetActive(false);
                    Destroy(fairytaleBackdrop);
                }
                if (!environmentCatalog)
                    environmentCatalog = Resources.Load<ChapterEnvironmentCatalog>("Environment/Chapter01Environments");
                var prefab = environmentCatalog ? environmentCatalog.Find(assetKey, levelKey) : null;
                var camera = battleCampCamera.GetComponent<Camera>();
                if (prefab)
                {
                    fairytaleBackdrop = Instantiate(prefab);
                    fairytaleBackdrop.GetComponent<DarkFairytaleBackdrop>().Initialize(camera);
                }
                else fairytaleBackdrop = battleStyle.CreateBackdrop(camera);
                fairytaleMapKey = environmentKey;
            }
            spawningMap = false;
            return;
        }
        spawningMap = true;
        nextMap = GetAssetByKey(AssetType.MAP, assetKey);
        LocalPrefabLoader.InstantiateAsync(nextMap, Vector3.zero, Quaternion.identity).Completed += SpawnMap_Complete;
    }

    private void SpawnMap_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        spawningMap = false;
        var instance = obj.Result;
        LocalPrefabLoader.Release(obj);
        if (instance) instance.tag = "MapGraphics";
    }

    #endregion


    #region SPAWN General Object
    private AssetReference nextGeneralObj;
    public bool spawningGeneralObj = false;
    public void SpawnGeneralObj(string assetKey)
    {
        //Debug.Log(assetKey);
        spawningGeneralObj = true;
        nextGeneralObj = GetAssetByKey(AssetType.GENERAL, assetKey);
        LocalPrefabLoader.InstantiateAsync(nextGeneralObj, Vector3.zero, Quaternion.identity).Completed += result => SpawnGeneral_Complete(result, assetKey);
    }



    private void SpawnGeneral_Complete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj, string lastGeneralAsset)
    {
        var instance = obj.Result;
        LocalPrefabLoader.Release(obj);
        spawningGeneralObj = false;
        if (!instance) return;
        Debug.Log("Last general asset:" + lastGeneralAsset);
        spawningGeneralObj = false;
        if (lastGeneralAsset == "RebirthPackage") rebirthPackageObj = instance;

        if (lastGeneralAsset == "MapPackage") mapPackageObj = instance;
        if (lastGeneralAsset == "ChestPackage") ChestPackage.single.SetMe(GameplayCanvas.single.lastChestClicked);
        if (lastGeneralAsset == "FusionPackage") fusionPackageObj = instance;


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
