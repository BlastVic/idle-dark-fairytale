#if UNITY_EDITOR

using GoogleSheetsForUnity;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace Melcat
{
    public static class SheetWaveData
    {
        [Serializable]
        public class WaveData
        {
            public string levelKey;
            public string levelName;
            public string mapGraphicKey;
            public bool isMiniBoss;
            public bool isBoss;
            public float crystalsMin;
            public float crystalsMax;
            public string chestGrade;
            public int chestSeconds;
            public string position;
            public string precondition;
            public string icon;
            public string color;
        }
        [Serializable]
        public class WaveEnemyData
        {
            public string levelKey;
            public int index;
            public string enemies;
        }
        [Serializable]
        public class WaveItemData
        {
            public string levelKey;
            public string chestItem;
            public string itemBasicTitle;
            public string itemFinalTitle;
            public int uniqueId;
            public string chanceDrop;
            public string iconName;
            public string itemType;
            public string skinIfApplicable;
            public string attachmentIfApplicable;
            public string itemGrade;
            public float fusionExp;
            public float state_hpNow = 0;
            public float state_hpMax = 0;
            public float state_hpPerc = 0;
            public float state_dmg = 0;
            public float state_dmgPerc = 0;
            public float state_def = 0;
            public float state_defPerc = 0;
            public float state_block = 0;
            public float state_atkSpd = 0;
            public float state_crit = 0;
            public float state_critDmg = 0;
            public float state_pierce = 0;
            public float state_absorb = 0;

            public float rollStat_dmgMin;
            public float rollStat_dmgMax;
            public float rollStat_defMin;
            public float rollStat_defMax;
            public float rollStat_atkSpdMin;
            public float rollStat_atkSpdMax;
            public float rollStat_critMin;
            public float rollStat_critMax;
            public float rollStat_critDmgMin;
            public float rollStat_critDmgMax;
            public float rollStat_hpMin;
            public float rollStat_hpMax_rolled;
            public float rollStat_dmgPercMin;
            public float rollStat_dmgPercMax;
            public float rollStat_hpPercMin;
            public float rollStat_hpPercMax;
            public float rollStat_defPercMin;
            public float rollStat_defPercMax;
            public string rollStat_percChanceBonus1;
            public string rollStat_percChanceBonus2;
            public string rollStat_percChanceBonus3;
            public string rollStat_percChanceBonus4;
        }

        public const string waveTable = "wave_test";
        public const string waveEnemyTable = "wave_enemy_test";
        public const string waveItemTable = "wave_item_test";

        public static List<WaveData> GetSheetDataWave(List<Waveset> waveSets)
        {
            List<WaveData> wavesData = new List<WaveData>();
            foreach (var wave in waveSets)
            {
                WaveData data = new WaveData();
                data.levelKey = wave.levelKey;
                data.levelName = wave.levelName;
                data.mapGraphicKey = wave.mapGraphicKey;
                data.isMiniBoss = wave.isMiniBoss;
                data.isBoss = wave.isBoss;
                data.crystalsMin = wave.crystalsMin;
                data.crystalsMax = wave.crystalsMax;
                data.chestGrade = wave.chestGrade.ToString();
                data.chestSeconds = wave.chestSeconds;
                data.position = wave.position;
                data.precondition = wave.precondition;
                data.icon = wave.icon;
                data.color = wave.color;
                wavesData.Add(data);
            }

            return wavesData;
        }
        public static List<WaveEnemyData> GetSheetDataWaveEnemy(List<Waveset> waveSets)
        {
            List<WaveEnemyData> enemies = new List<WaveEnemyData>();
            foreach (var wave in waveSets)
            {
                for (int i = 0; i < wave.waves.Length; i++)
                {
                    WaveEnemyData data = new WaveEnemyData();
                    data.levelKey = wave.levelKey;
                    data.index = i;

                    StringBuilder sb = new StringBuilder();
                    if (wave.waves[i].enemies != null && wave.waves[i].enemies.Length > 0)
                    {
                        for (int j = 0; j < wave.waves[i].enemies.Length; j++)
                        {
                            if (j < wave.waves[i].enemies.Length - 1)
                            {
                                sb.Append(wave.waves[i].enemies[j] + ",");
                            }
                            else
                            {
                                sb.Append(wave.waves[i].enemies[j]);
                            }
                        }
                    }
                    data.enemies = sb.ToString();
                    enemies.Add(data);
                }
            }
            return enemies;
        }
        public static List<WaveItemData> GetSheetDataWaveItem(List<Waveset> waveSets)
        {
            List<WaveItemData> items = new List<WaveItemData>();
            foreach (var wave in waveSets)
            {
                for (int i = 0; i < wave.chestItemsGiven.Count; i++)
                {
                    items.Add(GetSheetDataWaveItemFromItem(wave.levelKey, "chestItemsGiven", wave.chestItemsGiven[i]));
                }

                for (int i = 0; i < wave.chestGoldItemsGiven.Count; i++)
                {
                    items.Add(GetSheetDataWaveItemFromItem(wave.levelKey, "chestGoldItemsGiven", wave.chestGoldItemsGiven[i]));
                }

                for (int i = 0; i < wave.chestDiamondItemsGiven.Count; i++)
                {
                    items.Add(GetSheetDataWaveItemFromItem(wave.levelKey, "chestDiamondItemsGiven", wave.chestDiamondItemsGiven[i]));
                }
            }
            return items;
        }
        private static WaveItemData GetSheetDataWaveItemFromItem(string key,string chestItem,Item item)
        {
            WaveItemData data = new WaveItemData();
            data.levelKey = key;
            data.chestItem = chestItem;
            data.itemBasicTitle = item.m_ItemBasicTitle;
            data.itemFinalTitle = item.m_ItemFinalTitle;
            data.uniqueId = item.uniqueId;
            data.chanceDrop = item.m_ChanceDrop.ToString("#0.00");
            data.iconName = item.m_IconName;
            data.itemType = item.m_ItemType.ToString();
            data.skinIfApplicable = item.m_SkinIfApplicable;
            data.attachmentIfApplicable = item.m_AttachmentIfApplicable;
            data.itemGrade = item.itemGrade.ToString();
            data.fusionExp = item.fusionExp;

            data.state_hpNow = item.baseStat.hpNow;
            data.state_hpMax = item.baseStat.hpMax;
            data.state_hpPerc = item.baseStat.hpPerc;
            data.state_dmg = item.baseStat.dmg;
            data.state_dmgPerc = item.baseStat.dmgPerc;
            data.state_def = item.baseStat.def;
            data.state_defPerc = item.baseStat.defPerc;
            data.state_block = item.baseStat.block;
            data.state_atkSpd = item.baseStat.atkSpd;
            data.state_crit = item.baseStat.crit;
            data.state_critDmg = item.baseStat.critDmg;
            data.state_pierce = item.baseStat.pierce;
            data.state_absorb = item.baseStat.absorb;

            data.rollStat_dmgMin = item.rollStat.dmgMin;
            data.rollStat_dmgMax = item.rollStat.dmgMax;
            data.rollStat_defMin = item.rollStat.defMin;
            data.rollStat_defMax = item.rollStat.defMax;
            data.rollStat_atkSpdMin = item.rollStat.atkSpdMin;
            data.rollStat_atkSpdMax = item.rollStat.atkSpdMax;
            data.rollStat_critMin = item.rollStat.critMin;
            data.rollStat_critMax = item.rollStat.critMax;
            data.rollStat_critDmgMin = item.rollStat.critDmgMin;
            data.rollStat_critDmgMax = item.rollStat.critDmgMax;
            data.rollStat_hpMin = item.rollStat.hpMin;
            data.rollStat_hpMax_rolled = item.rollStat.hpMax_rolled;
            data.rollStat_dmgPercMin = item.rollStat.dmgPercMin;
            data.rollStat_dmgPercMax = item.rollStat.dmgPercMax;
            data.rollStat_hpPercMin = item.rollStat.hpPercMin;
            data.rollStat_hpPercMax = item.rollStat.hpPercMax;
            data.rollStat_defPercMin = item.rollStat.defPercMin;
            data.rollStat_defPercMax = item.rollStat.defPercMax;
            data.rollStat_percChanceBonus1 = item.rollStat.percChanceBonus1.ToString("#0.00");
            data.rollStat_percChanceBonus2 = item.rollStat.percChanceBonus2.ToString("#0.00");
            data.rollStat_percChanceBonus3 = item.rollStat.percChanceBonus3.ToString("#0.00");
            data.rollStat_percChanceBonus4 = item.rollStat.percChanceBonus4.ToString("#0.00");

            return data;
        }
        public static List<Waveset> GetGameDataWaveset(List<WaveData> waves)
        {
            List<Waveset> waveSets = new List<Waveset>();
            foreach (var wave in waves)
            {
                Waveset set = new Waveset();
                set.levelKey = wave.levelKey;
                set.levelName = wave.levelName;
                set.mapGraphicKey = wave.mapGraphicKey;
                set.isMiniBoss = wave.isMiniBoss;
                set.isBoss = wave.isBoss;
                set.crystalsMin = wave.crystalsMin;
                set.crystalsMax = wave.crystalsMax;
                set.chestGrade = (ChestGrade)Enum.Parse(typeof(ChestGrade),wave.chestGrade);
                set.chestSeconds = wave.chestSeconds;
                set.position = wave.position;
                set.precondition = wave.precondition;
                set.icon = wave.icon;
                set.color = wave.color;
                waveSets.Add(set);
            }

            return waveSets;
        }
        public static List<Item> GetGameDataWaveItem(List<WaveItemData> items,string levelKey,string chestItem)
        {
            List<Item> waveItems = new List<Item>();
            foreach (var waveItem in items)
            {
                if(waveItem.levelKey == levelKey && waveItem.chestItem == chestItem)
                {
                    Item item = new Item();
                    item.m_ItemBasicTitle = waveItem.itemBasicTitle;
                    item.m_ItemFinalTitle = waveItem.itemFinalTitle;
                    item.uniqueId = waveItem.uniqueId;
                    item.m_ChanceDrop = float.Parse(waveItem.chanceDrop);
                    item.m_IconName = waveItem.iconName;
                    item.m_ItemType = (ItemType)Enum.Parse(typeof(ItemType), waveItem.itemType);
                    item.m_SkinIfApplicable = waveItem.skinIfApplicable;
                    item.m_AttachmentIfApplicable = waveItem.attachmentIfApplicable;
                    item.itemGrade = (ItemGrade)Enum.Parse(typeof(ItemGrade), waveItem.itemGrade);

                    item.baseStat = new Stat();
                    item.baseStat.hpNow = waveItem.state_hpNow;
                    item.baseStat.hpMax = waveItem.state_hpMax;
                    item.baseStat.hpPerc = waveItem.state_hpPerc;
                    item.baseStat.dmg = waveItem.state_dmg;
                    item.baseStat.dmgPerc = waveItem.state_dmgPerc;
                    item.baseStat.def = waveItem.state_def;
                    item.baseStat.defPerc = waveItem.state_defPerc;
                    item.baseStat.block = waveItem.state_block;
                    item.baseStat.atkSpd = waveItem.state_atkSpd;
                    item.baseStat.crit = waveItem.state_crit;
                    item.baseStat.critDmg = waveItem.state_critDmg;
                    item.baseStat.pierce = waveItem.state_pierce;
                    item.baseStat.absorb = waveItem.state_absorb;

                    item.rollStat = new RollStat();
                    item.rollStat.dmgMin = waveItem.rollStat_dmgMin;
                    item.rollStat.dmgMax = waveItem.rollStat_dmgMax;
                    item.rollStat.defMin = waveItem.rollStat_defMin;
                    item.rollStat.defMax = waveItem.rollStat_defMax;
                    item.rollStat.atkSpdMin = waveItem.rollStat_atkSpdMin;
                    item.rollStat.atkSpdMax = waveItem.rollStat_atkSpdMax;
                    item.rollStat.critMin = waveItem.rollStat_critMin;
                    item.rollStat.critMax = waveItem.rollStat_critMax;
                    item.rollStat.critDmgMin = waveItem.rollStat_critDmgMin;
                    item.rollStat.critDmgMax = waveItem.rollStat_critDmgMax;
                    item.rollStat.hpMin = waveItem.rollStat_hpMin;
                    item.rollStat.hpMax_rolled = waveItem.rollStat_hpMax_rolled;
                    item.rollStat.dmgPercMin = waveItem.rollStat_dmgPercMin;
                    item.rollStat.dmgPercMax = waveItem.rollStat_dmgPercMax;
                    item.rollStat.hpPercMin = waveItem.rollStat_hpPercMin;
                    item.rollStat.hpPercMax = waveItem.rollStat_hpPercMax;
                    item.rollStat.defPercMin = waveItem.rollStat_defPercMin;
                    item.rollStat.defPercMax = waveItem.rollStat_defPercMax;
                    item.rollStat.percChanceBonus1 = float.Parse(waveItem.rollStat_percChanceBonus1);
                    item.rollStat.percChanceBonus2 = float.Parse(waveItem.rollStat_percChanceBonus2);
                    item.rollStat.percChanceBonus3 = float.Parse(waveItem.rollStat_percChanceBonus3);
                    item.rollStat.percChanceBonus4 = float.Parse(waveItem.rollStat_percChanceBonus4);

                    waveItems.Add(item);
                }
            }

            return waveItems;
        }

        public static void CreateTableWave()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "levelKey",
                "levelName",
                "mapGraphicKey",
                "isMiniBoss",
                "isBoss",
                "crystalsMin",
                "crystalsMax",
                "chestGrade",
                "chestSeconds",
                "position",
                "precondition",
                "icon",
                "color"
            };
            Drive.CreateTable(tableHeaders, waveEnemyTable, false);
        }
        public static void CreateTableWaveEnemy()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "levelKey",
                "index",
                "enemies"
            };
            Drive.CreateTable(tableHeaders, waveEnemyTable, false);
        }
        public static void CreateTableWaveItem()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "levelKey",
                "chestItem",
                "itemBasicTitle",
                "itemFinalTitle",
                "uniqueId",
                "chanceDrop",
                "iconName",
                "itemType",
                "skinIfApplicable",
                "attachmentIfApplicable",
                "itemGrade",
                "fusionExp",
                "state_hpNow",
                "state_hpMax",
                "state_hpPerc",
                "state_dmg",
                "state_dmgPerc",
                "state_def",
                "state_defPerc",
                "state_block",
                "state_atkSpd",
                "state_crit",
                "state_critDmg",
                "state_pierce",
                "state_absorb",
                "rollStat_dmgMin",
                "rollStat_dmgMax",
                "rollStat_defMin",
                "rollStat_defMax",
                "rollStat_atkSpdMin",
                "rollStat_atkSpdMax",
                "rollStat_critMin",
                "rollStat_critMax",
                "rollStat_critDmgMin",
                "rollStat_critDmgMax",
                "rollStat_hpMin",
                "rollStat_hpMax_rolled",
                "rollStat_dmgPercMin",
                "rollStat_dmgPercMax",
                "rollStat_hpPercMin",
                "rollStat_hpPercMax",
                "rollStat_defPercMin",
                "rollStat_defPercMax",
                "rollStat_percChanceBonus1",
                "rollStat_percChanceBonus2",
                "rollStat_percChanceBonus3",
                "rollStat_percChanceBonus4"
            };
            Drive.CreateTable(tableHeaders, waveItemTable, false);
        }

        public static void UploadWave(List<WaveData> wavesData,Action callback)
        {
            Drive.responseCallback = (dataContainer) => 
            {
                if (dataContainer.objType == waveTable)
                {
                    callback();
                }
            };
            string jsonData = JsonHelper.ToJson(wavesData.ToArray());
            Drive.RebuildObjects(jsonData, waveTable, false);
        }
        public static void UploadWaveEnemy(List<WaveEnemyData> waveEnemies, Action callback)
        {
            Drive.responseCallback = (dataContainer) => 
            {
                if (dataContainer.objType == waveEnemyTable)
                {
                    callback();
                }
            };
            string jsonData = JsonHelper.ToJson(waveEnemies.ToArray());
            Drive.RebuildObjects(jsonData, waveEnemyTable, false);
        }
        public static void UploadWaveItem(List<WaveItemData> waveItems, Action callback)
        {
            Drive.responseCallback = (dataContainer) => {
                if (dataContainer.objType == waveItemTable)
                {
                    callback();
                }
            };
            string jsonData = JsonHelper.ToJson(waveItems.ToArray());
            Drive.RebuildObjects(jsonData, waveItemTable, false);
        }

        public static void ImportWaveSet(Action<List<Waveset>> callback)
        {
            // Suscribe for catching cloud responses.
            Drive.responseCallback = (dataContainer) => {

                if (dataContainer.objType != waveTable)
                    return;

                if (dataContainer.QueryType == Drive.QueryType.getTable)
                {
                    string rawJSon = dataContainer.payload;
                    Debug.Log("Data from Google Drive received.");
                    WaveData[] waves = JsonHelper.ArrayFromJson<WaveData>(rawJSon);
                    List<WaveData> wavesData = new List<WaveData>(waves);
                    callback(GetGameDataWaveset(wavesData));
                }

                if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
                {
                    Debug.Log(dataContainer.msg);
                }
            };

            // Make the query.
            Drive.GetTable(waveTable, false);
        }
        public static void ImportWaveEnemy(List<Waveset> waves,Action callback)
        {
            Drive.responseCallback = (dataContainer) => {

                if (dataContainer.objType != waveEnemyTable)
                    return;

                if (dataContainer.QueryType == Drive.QueryType.getTable)
                {
                    string rawJSon = dataContainer.payload;
                    WaveEnemyData[] waveEnemies = JsonHelper.ArrayFromJson<WaveEnemyData>(rawJSon);

                    for(int i=0;i < waves.Count; i++)
                    {
                        Waveset wave = waves[i];
                        List<WaveEnemyData> tempEnemies = new List<WaveEnemyData>();
                        for(int j=0;j < waveEnemies.Length; j++)
                        {
                            if(wave.levelKey == waveEnemies[j].levelKey)
                            {
                                tempEnemies.Add(waveEnemies[j]);
                            }
                        }
                        wave.waves = new Wave[tempEnemies.Count];
                        for (int k = 0; k < tempEnemies.Count; k++)
                        {
                            wave.waves[k] = new Wave();
                            wave.waves[k].enemies = tempEnemies[k].enemies.Split(',');
                        }
                    }
                    callback();
                }
                if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
                {
                    Debug.Log(dataContainer.msg);
                }
            };
            Drive.GetTable(waveEnemyTable, false);
        }
        public static void ImportWaveItem(List<Waveset> waves, Action callback)
        {
            Drive.responseCallback = (dataContainer) => {

                if (dataContainer.objType != waveItemTable)
                    return;

                if (dataContainer.QueryType == Drive.QueryType.getTable)
                {
                    string rawJSon = dataContainer.payload;
                    WaveItemData[] waveItems = JsonHelper.ArrayFromJson<WaveItemData>(rawJSon);
                    for (int i = 0; i < waves.Count; i++)
                    {
                        Waveset wave = waves[i];
                        List<WaveItemData> tempItems = new List<WaveItemData>();
                        for (int j = 0; j < waveItems.Length; j++)
                        {
                            if (wave.levelKey == waveItems[j].levelKey)
                            {
                                tempItems.Add(waveItems[j]);
                            }
                        }
                        wave.chestItemsGiven = GetGameDataWaveItem(tempItems, wave.levelKey, "chestItemsGiven");
                        wave.chestGoldItemsGiven = GetGameDataWaveItem(tempItems, wave.levelKey, "chestGoldItemsGiven");
                        wave.chestDiamondItemsGiven = GetGameDataWaveItem(tempItems, wave.levelKey, "chestDiamondItemsGiven");
                    }
                    callback();
                }

                if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
                {
                    Debug.Log(dataContainer.msg);
                }
            };
            Drive.GetTable(waveItemTable, false);
        }

        private static void HandleDriveResponse(Drive.DataContainer dataContainer)
        {
            
        }
    }
}

#endif

