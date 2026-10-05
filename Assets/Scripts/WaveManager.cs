using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.Enum;
using IdleKnightHero.UI;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private static WaveManager _instance;
    public static WaveManager single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<WaveManager>();
            return _instance;
        }
    }
    public List<Waveset> waveDb = new List<Waveset>();
    public List<WaveReached> highestWavesReached = new List<WaveReached>();

    public int waveNumber = 0;
    public Waveset currentWaveset;
    public bool loadingLevel = false;

    public void CallStartLevel(string levelKey, System.Action callback = null)
    {
        GameManager.single.SetGameState(GameStateType.BATTLE);
        //Debug.Log("Started:" + levelKey);
        victoryCalled = false;
        GameplayCanvas.single.replayButtonCool = true;
        StartCoroutine(StartLevelChain(levelKey, callback));

    }

    IEnumerator StartLevelChain(string levelKey,System.Action callback)
    {
        Debug.Log("Key for level:" + levelKey);
        waveNumber = 0;
        loadingLevel = true;

        LevelController.Instance.AssetManager.TurnOnPlayer();
        currentWaveset = GetWavesetByKey(levelKey);
        //music chooser
        if (currentWaveset.isMiniBoss)
        {
            MusicManager.Instance.PlayMusicByClipNumber(1);
        }
        else if (currentWaveset.isBoss)
        {
            MusicManager.Instance.PlayMusicByClipNumber(2);
        }
        else
        {
            MusicManager.Instance.PlayBattleNormalMusic();// (1);
        }
        GameplayCanvas.single.ToggleHpXpLevelUi(true);
        LevelController.Instance.AssetManager.SpawnMap(currentWaveset.mapGraphicKey);
        //WAIT UNTIL MAP GRAPHICS ARE LOADED - to continue
        while (LevelController.Instance.AssetManager.spawningMap)
        {
            yield return null;
        }
        //Debug.Log("screen.width:" + Screen.width);

        float aspectRatio = (float)Screen.height / (float)Screen.width;
        aspectRatio = (float)System.Math.Round(aspectRatio, 1);
        //Debug.Log("aspect Ratio:" + System.Math.Round(aspectRatio, 1));

        //simple rounded way of checking for 4:3 ipad aspect ratio
        if (aspectRatio == 1.3f && !LevelController.Instance.AssetManager.battleStyle)
        {
            //Debug.Log("Better add clouds");
            LevelController.Instance.AssetManager.SpawnMap("Clouds");
        }
        else
        {
            //Debug.Log("Aspect ratio was:" + System.Math.Round(aspectRatio, 1));
        }

        GameplayCanvas.single.ToggleBattleUi(true);
        GameplayCanvas.single.ToggleCampUi(false);
        if (LevelController.Instance.AssetManager.battleStyle || GameManager.single.thingsSeenPermanent.Contains("Cutscene-0"))
        {
            LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
            LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>().enabled = true;
            CallNextWave();
        }
        else
        {
            LevelController.Instance.AssetManager.battleCampCamera.SetActive(false);
        }

        while (GameObject.FindGameObjectWithTag("MapGraphics") == null)
        {
            //Debug.Log("mapgraphics null");
            yield return null; //wait and stay black till map is loaded
        }

        TransitionCanvas.single.ToClear("WaveManager 30");

        if (callback != null)
        {
            callback();
        }
    }

    public bool victoryCalled = false;
    public void WaveComplete()
    {
        if (victoryCalled) return;
        if (waveNumber + 1 >= currentWaveset.waves.Count())
        {
            //Debug.Log("VICTORY!");
            victoryCalled = true;

            //string rewardTime = PlayerPrefs.SetString("sysString", System.DateTime.Now.ToBinary().ToString());
            System.DateTime rewardTime = System.DateTime.Now.AddSeconds(currentWaveset.chestSeconds);
            string rewardTimeString = rewardTime.ToBinary().ToString();

            //find out what kind of chest to drop

            float gradeRand = Random.Range(0, 100);
            Debug.Log("WAVE MANAGER WAVE COMPLETE - Victory rand:" + gradeRand);
            //gradeRand = 75;//hardset gold
            ChestGrade chestGradeChosen = ChestGrade.NORMAL;
            float crystMin = currentWaveset.crystalsMin;
            float crystMax = currentWaveset.crystalsMax;

            if (gradeRand >= 70 && gradeRand < 95)
            {
                //turn gold
                //Debug.Log("turn gold");
                chestGradeChosen = ChestGrade.GOLD;
                //set reward time diff
                //string back to a long
                rewardTime = System.DateTime.Now.AddSeconds(currentWaveset.chestSeconds * 2);
                rewardTimeString = rewardTime.ToBinary().ToString();

                crystMin *= 3;
                crystMax *= 3;
                Debug.Log("Gold Chest min crystal:" + crystMin + " max crystal:" + crystMax);
            }
            if (gradeRand >= 95)
            {
                //turn diamond
                chestGradeChosen = ChestGrade.DIAMOND;
                //set reward time diff
                rewardTime = System.DateTime.Now.AddSeconds(currentWaveset.chestSeconds * 3);
                rewardTimeString = rewardTime.ToBinary().ToString();

                crystMin *= 10;
                crystMax *= 10;
            }

            LevelController.Instance.AssetManager.nextDropInGrade = chestGradeChosen;

            //create and try add a chest
            Chest newChest = new Chest
            {
                chestMapName = currentWaveset.levelName,
                chestGrade = chestGradeChosen,
                possibleRewards = currentWaveset.chestItemsGiven,
                possibleGoldRewards = currentWaveset.chestGoldItemsGiven,
                possibleDiamondRewards = currentWaveset.chestDiamondItemsGiven,
                rewardTime = rewardTimeString,
                crystalsMax = crystMax,
                crystalsMin = crystMin
            };

            Router.single.newChest = newChest;

            InventoryManager.single.TryAddChest(newChest);


            Router.single.StartCoroutine(Router.single.GoToVictory());
        }
        else
        {
            waveNumber++;
            CallNextWave();
        }
    }

    public void CallNextWave()
    {
        GameplayCanvas.single.RefreshWaveText();
        StartCoroutine(SpawnWave(currentWaveset.waves[waveNumber]));
    }
    public IEnumerator SpawnWave(Wave w)
    {
        foreach (string s in w.enemies)
        {

            //Debug.Log("Spawn :" + s);
            Vector3 position = LevelController.Instance.AssetManager.GetNextPosition();
            LevelController.Instance.AssetManager.SpawnEnemy(s, position);

            while (LevelController.Instance.AssetManager.spawningEnemy == true)  //WAIT UNTIL ENEMY LOADED
            {
                yield return null;
            }
        }
        loadingLevel = false;
    }

    public Waveset GetWavesetByKey(string key)
    {
        return waveDb.Where(x => x.levelKey == key).FirstOrDefault();
    }


    public int GetWaveseReachedByKey(string key)
    {
        WaveReached wr = highestWavesReached.Where(x => x.levelKey == key).FirstOrDefault();
        if (wr != null) return wr.waveReached;
        return 0;
    }

    public void SetWaveseReachedByKey(string key, int reached)
    {
        WaveReached wr = highestWavesReached.Where(x => x.levelKey == key).FirstOrDefault();
        if (wr != null)
        {
            if (reached > wr.waveReached) wr.waveReached = reached;
        }
        else
        {
            highestWavesReached.Add(new WaveReached { levelKey = key, waveReached = reached });
        }
    }
}

[System.Serializable]
public class Waveset
{
    public string levelKey;
    public string levelName;
    public string mapGraphicKey;
    public bool isMiniBoss;
    public bool isBoss;
    public Wave[] waves;
    public float crystalsMin, crystalsMax = 20;
    public ChestGrade chestGrade;
    public int chestSeconds = 10;
    public List<Item> chestItemsGiven;
    public List<Item> chestGoldItemsGiven;
    public List<Item> chestDiamondItemsGiven;
    public string position;
    public string precondition;
    public string icon;
    public string color;

}

[System.Serializable]
public class Wave
{
    public string[] enemies;
}

[System.Serializable]
public class WaveReached
{
    public string levelKey;
    public int waveReached;
}
