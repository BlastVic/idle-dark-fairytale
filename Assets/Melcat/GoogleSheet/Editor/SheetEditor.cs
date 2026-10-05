using System;
using System.Collections.Generic;
using Assets.Scripts.Services;
using Scripts.Level;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayerPrefs = Assets.Scripts.Services.PlayerPrefs;

namespace Melcat
{
    public static class SheetEditor 
    {
        public const string MAIN_SCENE_PATH = "Assets/Scenes/MainScenes/Gameplay.unity";
        public const string LEVEL_DATA_PATH = "Assets/Resources/LevelDataDescriptions.asset";

        [MenuItem("Melcat/Game Data/Create/Wave", false, 1)]
        public static void CreateWaveData()
        {
            SheetWaveData.CreateTableWave();
        }
        [MenuItem("Melcat/Game Data/Create/Wave Enemy", false, 2)]
        public static void CreateWaveEnemyData()
        {
            SheetWaveData.CreateTableWaveEnemy();
        }
        [MenuItem("Melcat/Game Data/Create/Wave Item", false, 3)]
        public static void CreateWaveItemData()
        {
            SheetWaveData.CreateTableWaveItem();
        }
        [MenuItem("Melcat/Game Data/Create/Enemy", false, 4)]
        public static void CreateEnemyData()
        {
            SheetEnemyData.CreateTableEnemy();
        }
        [MenuItem("Melcat/Game Data/Create/Enemy Item Given", false, 5)]
        public static void CreateEnemyItemData()
        {
            SheetEnemyData.CreateTableEnemyItem();
        }
        [MenuItem("Melcat/Game Data/Create/Level", false, 6)]
        public static void CreateLevelData()
        {
            SheetLevelData.CreateTableLevel();
        }

        [MenuItem("Melcat/Game Data/Upload/Wave", false, 1)]
        public static void UploadWave()
        {
            // 打开场景:
            var scene = EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
            GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
            if (goWave != null)
            {
                WaveManager manager = goWave.GetComponent<WaveManager>();
                List<SheetWaveData.WaveData> wavesData = SheetWaveData.GetSheetDataWave(manager.waveDb);
                SheetWaveData.UploadWave(wavesData, () => {
                    Debug.Log("Upload wave data complete!");
                });
            }
        }
        [MenuItem("Melcat/Game Data/Upload/Wave Enemy", false, 2)]
        public static void UploadWaveEnemy()
        {
            // 打开场景:
            var scene = EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
            GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
            if (goWave != null)
            {
                WaveManager manager = goWave.GetComponent<WaveManager>();
                List<SheetWaveData.WaveEnemyData> enemies = SheetWaveData.GetSheetDataWaveEnemy(manager.waveDb);
                SheetWaveData.UploadWaveEnemy(enemies, () =>
                {
                    Debug.Log("Upload wave enemy data complete!");
                });
            }
        }
        [MenuItem("Melcat/Game Data/Upload/Wave Item", false, 3)]
        public static void UploadWaveItem()
        {
            // 打开场景:
            var scene = EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
            GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
            if (goWave != null)
            {
                WaveManager manager = goWave.GetComponent<WaveManager>();
                List<SheetWaveData.WaveItemData> items = SheetWaveData.GetSheetDataWaveItem(manager.waveDb);
                SheetWaveData.UploadWaveItem(items, () =>
                {
                    Debug.Log("Upload wave item data complete!");
                });
            }
        }
        [MenuItem("Melcat/Game Data/Upload/Enemy", false, 4)]
        public static void UploadEnemy()
        {
            //var levelData = AssetDatabase.LoadAssetAtPath<LevelDataDescriptions>(LEVEL_DATA_PATH);
            var enemyDb = LevelDataDescriptions.EnemyDb;

            List<GameObject> enemyObjs = new List<GameObject>();
            foreach(var e in enemyDb)
            {
                string path = AssetDatabase.GUIDToAssetPath(e.assetRef.AssetGUID);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                enemyObjs.Add(obj);
            }

            List<SheetEnemyData.EnemyData> items = SheetEnemyData.GetSheetDataEnemy(enemyObjs);
            SheetEnemyData.UploadEnemy(items, () =>
            {
                Debug.Log("Upload enemy data complete!");
            });
        }
        [MenuItem("Melcat/Game Data/Upload/Enemy Item Given", false, 5)]
        public static void UploadEnemyItem()
        {
            //var levelData = AssetDatabase.LoadAssetAtPath<LevelDataDescriptions>(LEVEL_DATA_PATH);
            var enemyDb = LevelDataDescriptions.EnemyDb;
            List<SheetEnemyData.EnemyItemData> items = new List<SheetEnemyData.EnemyItemData>();
            foreach (var e in enemyDb)
            {
                string path = AssetDatabase.GUIDToAssetPath(e.assetRef.AssetGUID);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Enemy enemy = obj.GetComponent<Enemy>();
                for(int i =0;i < enemy.itemsGiven.Count; i++)
                {
                    items.Add(SheetEnemyData.GetSheetDataEnemyItemFromItem(obj.name, i, enemy.itemsGiven[i]));
                }
            }
            SheetEnemyData.UploadEnemyItem(items, () =>
            {
                Debug.Log("Upload enemy item given data complete!");
            });
        }
        [MenuItem("Melcat/Game Data/Upload/Level", false, 6)]
        public static void UploadLevel()
        {
            List<SheetLevelData.LevelData> items = SheetLevelData.GetSheetDataLevel();
            SheetLevelData.UploadLevel(items, () =>
            {
                Debug.Log("Upload level data complete!");
            });
        }

        [MenuItem("Melcat/Game Data/Import/Wave", false, 1)]
        public static void ImportWaveData()
        {
            // 打开场景:
            var scene = EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
            GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
            if (goWave != null)
            {
                SheetWaveData.ImportWaveSet((list)=> {
                    WaveManager manager = goWave.GetComponent<WaveManager>();
                    manager.waveDb = list;
                    SheetWaveData.ImportWaveEnemy(list,() => {
                        SheetWaveData.ImportWaveItem(list, () => {
                            // 编辑列场景, 设置脏数据:
                            EditorSceneManager.MarkSceneDirty(scene);
                            // 保存场景:
                            EditorSceneManager.SaveScene(scene, MAIN_SCENE_PATH);
                            Debug.Log("Import wave data count:" + list.Count);
                        });
                    });
                });
            }
        }
    }
}

