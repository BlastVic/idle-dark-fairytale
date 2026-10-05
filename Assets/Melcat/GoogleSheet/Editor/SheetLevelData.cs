#if UNITY_EDITOR

using GoogleSheetsForUnity;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using Scripts.Level;

namespace Melcat
{
    public static class SheetLevelData
    {
        [Serializable]
        public class LevelData
        {
            public int levelKey;
            public int exp;
            public int hp;
            public int dmg;
            public int def;
            public int atkSpd;
            public string critLevel;
            public int critDmg;
        }
       
        public const string levelTable = "level";

        public static List<LevelData> GetSheetDataLevel()
        {
            int itemCount = Mathf.Max(
                LevelDataDescriptions.ExpLevelDb.Length,
                LevelDataDescriptions.HpLevelDb.Length,
                LevelDataDescriptions.DmgLevelDb.Length,
                LevelDataDescriptions.AtkSpdLevelDb.Length,
                LevelDataDescriptions.CritLevelDb.Length,
                LevelDataDescriptions.CritDmgLevelDb.Length);
            List<LevelData> levels = new List<LevelData>();
            for(int i =0;i < itemCount; i++)
            {
                LevelData data = new LevelData();
                data.levelKey = i;

                if (LevelDataDescriptions.ExpLevelDb.Length > i)
                    data.exp = LevelDataDescriptions.ExpLevelDb[i];
                else
                    data.exp = -1;

                if (LevelDataDescriptions.HpLevelDb.Length > i)
                    data.hp = LevelDataDescriptions.HpLevelDb[i];
                else
                    data.hp = -1;

                if (LevelDataDescriptions.DmgLevelDb.Length > i)
                    data.dmg = LevelDataDescriptions.DmgLevelDb[i];
                else
                    data.dmg = -1;

                if (LevelDataDescriptions.DefLevelDb.Length > i)
                    data.def = LevelDataDescriptions.DefLevelDb[i];
                else
                    data.def = -1;

                if (LevelDataDescriptions.AtkSpdLevelDb.Length > i)
                    data.atkSpd = LevelDataDescriptions.AtkSpdLevelDb[i];
                else
                    data.atkSpd = -1;

                if (LevelDataDescriptions.CritLevelDb.Length > i)
                    data.critLevel = LevelDataDescriptions.CritLevelDb[i].ToString("#0.00");
                else
                    data.critLevel = "-1";

                if (LevelDataDescriptions.CritDmgLevelDb.Length > i)
                    data.critDmg = LevelDataDescriptions.CritDmgLevelDb[i];
                else
                    data.critDmg = -1;

                levels.Add(data);
            }
            return levels;
        }

        public static void CreateTableLevel()
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback += HandleDriveResponse;

            string[] tableHeaders = new string[] {
                "levelKey",
                "hp",
                "dmg",
                "def",
                "atkSpd",
                "critLevel",
                "critDmg"
            };
            Drive.CreateTable(tableHeaders, levelTable, false);
        }

        public static void UploadLevel(List<LevelData> levelsData, Action callback)
        {
            // Suscribe to Drive event to get the Drive response.
            Drive.responseCallback = (dataContainer) =>
            {
                if (dataContainer.objType == levelTable)
                {
                    callback();
                }
            };
            string jsonData = JsonHelper.ToJson(levelsData.ToArray());
            Drive.RebuildObjects(jsonData, levelTable, false);
        }

        private static void HandleDriveResponse(Drive.DataContainer dataContainer)
        {

        }
    }
}

#endif

