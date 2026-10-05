#if UNITY_EDITOR

using GoogleSheetsForUnity;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace Melcat
{
    public static class SheetEnemyData
    {
        public const string enemyTable = "enemy";
        public const string enemyItemTable = "enemy_item_given";

        [Serializable]
        public class EnemyData
        {
            public string enemyKey;
            public float expGiven;
            public string waitBetweenAttacks;
            public int chanceInterruptAttack;
            public float hpNow;
            public float hpMax;
            public float hpPerc;
            public float dmg;
            public float dmgPerc;
            public float def;
            public float defPerc;
            public float block;
            public float atkSpd;
            public float crit;
            public float critDmg;
            public float pierce;
            public float absorb;
        }
        [Serializable]
        public class EnemyItemData
        {
            public string enemyKey;
            public int itemIndex;
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


        public static List<EnemyData> GetSheetDataEnemy(List<GameObject> enemyObjects)
        {
            List<EnemyData> enemiesData = new List<EnemyData>();
            foreach (var obj in enemyObjects)
            {
                EnemyData data = new EnemyData();
                Enemy objEnemy = obj.GetComponent<Enemy>();
                Actor_Base objActor = obj.GetComponent<Actor_Base>();

                data.enemyKey = obj.name;
                data.expGiven = objEnemy.expGiven;
                data.waitBetweenAttacks = objEnemy.waitBetweenAttacks.ToString("#0.00");
                data.chanceInterruptAttack = objActor.chanceInterruptAttack;
                data.hpNow = objActor.baseStat.hpNow;
                data.hpMax = objActor.baseStat.hpMax;
                data.hpPerc = objActor.baseStat.hpPerc;
                data.dmg = objActor.baseStat.dmg;
                data.dmgPerc = objActor.baseStat.dmgPerc;
                data.def = objActor.baseStat.def;
                data.defPerc = objActor.baseStat.defPerc;
                data.block = objActor.baseStat.block;
                data.atkSpd = objActor.baseStat.atkSpd;
                data.crit = objActor.baseStat.crit;
                data.critDmg = objActor.baseStat.critDmg;
                data.pierce = objActor.baseStat.pierce;
                data.absorb = objActor.baseStat.absorb;
                enemiesData.Add(data);
            }

            return enemiesData;
        }
        public static EnemyItemData GetSheetDataEnemyItemFromItem(string key, int itemIndex, Item item)
        {
            EnemyItemData data = new EnemyItemData();
            data.enemyKey = key;
            data.itemIndex = itemIndex;
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

        // Creates a new localization table on the cloud.
        public static void CreateTableEnemy()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "enemyKey",
                "expGiven",
                "waitBetweenAttacks",
                "chanceInterruptAttack",
                "hpNow",
                "hpMax",
                "hpPerc",
                "dmg",
                "dmgPerc",
                "def",
                "defPerc",
                "block",
                "atkSpd",
                "crit",
                "critDmg",
                "pierce",
                "absorb"
            };
            Drive.CreateTable(tableHeaders, enemyTable, false);
        }
        public static void CreateTableEnemyItem()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "enemyKey",
                "itemIndex",
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
            Drive.CreateTable(tableHeaders, enemyItemTable, false);
        }

        public static void UploadEnemy(List<EnemyData> enemiesData, Action callback)
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback = (dataContainer) =>
            {
                if (dataContainer.objType == enemyTable)
                {
                    callback();
                }
            };

            string jsonData = JsonHelper.ToJson(enemiesData.ToArray());
            Drive.RebuildObjects(jsonData, enemyTable, false);
        }
        public static void UploadEnemyItem(List<EnemyItemData> enemyItems, Action callback)
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback = (dataContainer) => {
                if (dataContainer.objType == enemyItemTable)
                {
                    callback();
                }
            };

            string jsonData = JsonHelper.ToJson(enemyItems.ToArray());
            Drive.RebuildObjects(jsonData, enemyItemTable, false);
        }


        // Processes the data received from the cloud.
        private static void HandleDriveResponse(Drive.DataContainer dataContainer)
        {
            
        }
    }
}

#endif

