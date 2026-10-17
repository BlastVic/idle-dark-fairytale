using System;
using System.Collections.Generic;
using Assets.Scripts.Extentions;
using UnityEngine;
using Scripts.Level;

namespace Assets.Scripts.Services
{
    public class SaveManager : MonoSingleton<SaveManager>
    {
        private const String saveFileName = "savedata_v3";
        private const String saveLastBattle = "savedata_lastbattle";

        // Editor validation uses a separate preference key; production always uses savedata_v3.
        static string SaveKey {
            get {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetString("STAT_ALLOCATION_QA_SAVE_KEY", saveFileName);
#else
                return saveFileName;
#endif
            }
        }
        static string LastBattleKey => SaveKey == saveFileName ? saveLastBattle : SaveKey + "_lastbattle";

        [SerializeField]
        public UserData _userData;

        private String _data;
        private String _dataLastBattle;

#if UNITY_EDITOR
        // Keep the no-save decision for the entire Play session, including OnApplicationQuit.
        // Persistence tests using an explicitly isolated key may still opt into writes.
        bool editorValidationSession;
#endif
        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            editorValidationSession = SaveKey == saveFileName &&
                (UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE", false) ||
                 !string.IsNullOrEmpty(UnityEditor.SessionState.GetString("UI_PRODUCTION_FLOW", "")));
#endif
            _userData = new UserData();
        }

        private void Start()
        {
            Load();
            //this checks the dlc for us
        }

        public void ClearPrefs()
        {
            PlayerPrefs.DeleteAll();
        }


        public void Save()
        {
#if UNITY_EDITOR
            // Explicit monster integration tests must never persist temporary waves or rewards.
            if (editorValidationSession || UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE", false)) return;
#endif
            LoggerMethods.Log("--- Saved File ---");
            GameManager gm = GameManager.single;

            InventoryManager im = InventoryManager.single;

            //m_UserData.soundLevel = AudioManager.single.soundLevel;
            //m_UserData.musicLevel = AudioManager.single.musicLevel;
            _userData.premiumCurrency = gm.premiumCurrency;

            //inventory
            _userData.accOwned = im.accOwned;
            _userData.armorsOwned = im.armorsOwned;
            _userData.helmetsOwned = im.helmetsOwned;
            _userData.shieldsOwned = im.shieldsOwned;
            _userData.weaponsOwned = im.weaponsOwned;
            _userData.chestsOwned = im.chestsOwned;

            //fusion booster feature
            _userData.isXpBoosterActive = gm.isBoostedXPActive;
            _userData.xpBoostTimerString = gm.xpBoostEndTimeString;

            //inventory worn
            _userData.accEq = im.accEq;
            _userData.armorEq = im.armorEq;
            _userData.helmetEq = im.helmetEq;
            _userData.shieldEq = im.shieldEq;
            _userData.weaponEq = im.weaponEq;

            //inventory bools
            _userData.hasWeapon = im.hasWeapon;
            _userData.hasAcc = im.hasAcc;
            _userData.hasHelmet = im.hasHelmet;
            _userData.hasShield = im.hasShield;
            _userData.hasArmor = im.hasArmor;

            _userData.playerLevel = gm.playerLevel;
            _userData.playerDeathCount = gm.playerDeathCount;
            _userData.rebirthLevel = gm.rebirthLevel;
            _userData.fusionLevel = gm.fusionLevel;
            _userData.hpLevel = gm.hpLevel;
            _userData.dmgLevel = gm.dmgLevel;
            _userData.defLevel = gm.defLevel;
            _userData.atkSpdLevel = gm.atkSpdLevel;
            _userData.critLevel = gm.critLevel;
            _userData.critDmgLevel = gm.critDmgLevel;
            _userData.LastAllocatedStat = gm.lastStatPurchased;

            //skills level
            _userData.regenLevel = SkillsController.Instance.regenLevel;
            _userData.sinisterStampedeLevel = SkillsController.Instance.sinisterStampedeLevel;
            _userData.angelOnMyShoulderLevel = SkillsController.Instance.angelOnMyShoulderLevel;
            _userData.devilOnMyShoulderLevel = SkillsController.Instance.devilOnMyShoulderLevel;
            _userData.pathCleanerLevel = SkillsController.Instance.pathCleanerLevel;
            _userData.luckyBastardLevel = SkillsController.Instance.luckyBastardLevel;
            _userData.ressurrectionLevel = SkillsController.Instance.ressurrectionLevel;
            _userData.twiceTheFunLevel = SkillsController.Instance.twiceTheFunLevel;
            _userData.lastButNotLeastLevel = SkillsController.Instance.lastButNotLeastLevel;
            _userData.cindrelLevel = SkillsController.Instance.cindrelLevel;
            _userData.lightningDemiGodlevel = SkillsController.Instance.lightningDemiGodlevel;
            _userData.bossKillerLevel = SkillsController.Instance.bossKillerLevel;
            _userData.myLuckyStarsLevel = SkillsController.Instance.myLuckyStarsLevel;
            _userData.battlefieldTacticianLevel = SkillsController.Instance.battlefieldTacticianLevel;
            _userData.breathOfHellLevel = SkillsController.Instance.breathOfHellLevel;

            _userData.xpNow = gm.xpNow;
            _userData.xpMax = gm.xpMax;
            _userData.fxpNow = gm.fxpNow;
            _userData.fxpMax = gm.fxpMax;
            _userData.skillPoints = gm.skillPoints;
            _userData.statPoints = gm.statPoints;
            _userData.crystals = gm.crystals;

            _userData.stackedCrystals = gm.stackedCrystals;
            //userData.stackedSkillPoints = gm.stackedSkillPoints;
            //userData.stackedStatPoints = gm.stackedStatPoints;
            _userData.stackedXp = gm.stackedXp;

            _userData.highestWavesReached = WaveManager.single.highestWavesReached;
            _userData.thingsSeenPermanent = gm.thingsSeenPermanent;

            //Debug.Log("Chest:" + InventoryManager.single.chestsOwned[0].rewardTime);
            _data = JsonUtility.ToJson(_userData);
            _dataLastBattle = gm.lastBattleKey;
            CreateJSON();
        }

        public void SaveDeathProgress()
        {
#if UNITY_EDITOR
            if (editorValidationSession || UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE", false)) return;
#endif
            Save();
            PlayerPrefs.Save(); // Persist before the death presentation can be interrupted.
        }

        public void SaveStatAllocation()
        {
#if UNITY_EDITOR
            if (editorValidationSession || UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE", false)) return;
#endif
            Save();
            PlayerPrefs.Save(); // Allocation and its repeat restriction survive immediate shutdown.
        }

        public void Load()
        {
            // Load our UserData into myData 
            LoadJSON();

            GameManager gm = GameManager.single;
            InventoryManager im = InventoryManager.single;
            //Audio
            //AudioManager.single.soundLevel = m_UserData.soundLevel;
            //AudioManager.single.musicLevel = m_UserData.musicLevel;
            gm.premiumCurrency = _userData.premiumCurrency;

           //inventory
           im.accOwned = _userData.accOwned;
            im.weaponsOwned = _userData.weaponsOwned;
            im.shieldsOwned = _userData.shieldsOwned;
            im.helmetsOwned = _userData.helmetsOwned;
            im.armorsOwned = _userData.armorsOwned;
            im.chestsOwned = _userData.chestsOwned;

            //xp booster feature - restore the boost you had
            if (_userData.isXpBoosterActive)
            {
                gm.isBoostedXPActive = _userData.isXpBoosterActive;
                gm.xpBoostEndTimeString = _userData.xpBoostTimerString;

                long restoreDateLong = System.Convert.ToInt64(_userData.xpBoostTimerString);
                //long to date
                gm.xpBoostEndTime = System.DateTime.FromBinary(restoreDateLong);
                GameManager.single.AddXPBoost(gm.xpBoostEndTimeString, true);
            }

            //inventory worn
            im.accEq = _userData.accEq;
            im.weaponEq = _userData.weaponEq;
            im.shieldEq = _userData.shieldEq;
            im.helmetEq = _userData.helmetEq;
            im.armorEq = _userData.armorEq;

            im.hasWeapon = _userData.hasWeapon;
            im.hasAcc = _userData.hasAcc;
            im.hasArmor = _userData.hasArmor;
            im.hasShield = _userData.hasShield;
            im.hasHelmet = _userData.hasHelmet;


            //stat levels
            gm.playerLevel = _userData.playerLevel;
            gm.playerDeathCount = Mathf.Max(0, _userData.playerDeathCount);
            gm.rebirthLevel = _userData.rebirthLevel;
            gm.fusionLevel = _userData.fusionLevel;
            gm.hpLevel = _userData.hpLevel;
            gm.dmgLevel = _userData.dmgLevel;
            gm.defLevel = _userData.defLevel;
            gm.atkSpdLevel = _userData.atkSpdLevel;
            gm.critLevel = _userData.critLevel;
            gm.critDmgLevel = _userData.critDmgLevel;
            gm.lastStatPurchased = _userData.LastAllocatedStat;

            //skill levels
            SkillsController.Instance.regenLevel = _userData.regenLevel;
            SkillsController.Instance.pathCleanerLevel = _userData.pathCleanerLevel;
            SkillsController.Instance.angelOnMyShoulderLevel = _userData.angelOnMyShoulderLevel;
            SkillsController.Instance.devilOnMyShoulderLevel = _userData.devilOnMyShoulderLevel;
            SkillsController.Instance.sinisterStampedeLevel = _userData.sinisterStampedeLevel;
            SkillsController.Instance.luckyBastardLevel = _userData.luckyBastardLevel;
            SkillsController.Instance.ressurrectionLevel = _userData.ressurrectionLevel;
            SkillsController.Instance.twiceTheFunLevel = _userData.twiceTheFunLevel;
            SkillsController.Instance.cindrelLevel = _userData.cindrelLevel;
            SkillsController.Instance.lastButNotLeastLevel = _userData.lastButNotLeastLevel;
            SkillsController.Instance.lightningDemiGodlevel = _userData.lightningDemiGodlevel;
            SkillsController.Instance.bossKillerLevel = _userData.bossKillerLevel;
            SkillsController.Instance.myLuckyStarsLevel = _userData.myLuckyStarsLevel;
            SkillsController.Instance.battlefieldTacticianLevel = _userData.battlefieldTacticianLevel;
            SkillsController.Instance.breathOfHellLevel = _userData.breathOfHellLevel;

            gm.xpNow = _userData.xpNow;
            gm.xpMax = _userData.xpMax;
            gm.fxpNow = _userData.fxpNow;
            gm.fxpMax = _userData.fxpMax;
            gm.skillPoints = _userData.skillPoints;
            gm.statPoints = _userData.statPoints;
            gm.crystals = _userData.crystals;

            gm.stackedXp = _userData.stackedXp;
            //gm.stackedStatPoints = userData.stackedStatPoints;
            //gm.stackedSkillPoints = userData.stackedSkillPoints;
            gm.stackedCrystals = _userData.stackedCrystals;

            WaveManager.single.highestWavesReached = _userData.highestWavesReached;
            gm.thingsSeenPermanent = _userData.thingsSeenPermanent;

            if (!string.IsNullOrEmpty(_dataLastBattle))
            {
                // Retired legacy chapters no longer have monster/map assets.
                // Keep old progression data, but return replay navigation to chapter one.
                gm.lastBattleKey = WaveManager.single.GetWavesetByKey(_dataLastBattle) != null
                    ? _dataLastBattle : "Khorasan Ruins I";
            }

            EventManager.single.CallLoadComplete();

            //GameManager.single.LoadComplete();
        }

        public void DeleteData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            GameManager gm = GameManager.single;

            gm.playerLevel = 1;
            gm.playerDeathCount = 0;
            gm.rebirthLevel = 0;

            ResetFusionAndXp();
            ResetStats();
            ResetInventory();
            SkillsController.Instance.ResetSkills();

            gm.skillPoints = 0;
            gm.statPoints = 0;
            gm.crystals = 0;

            gm.stackedCrystals = 0;
            gm.stackedXp = 0;

            WaveManager.single.highestWavesReached.Clear();
            gm.thingsSeenPermanent.Clear();
            GameplayCanvas.single.RefreshCampUI();
            Save();
            Application.LoadLevel("Gameplay");
        }

        public void ResetFusionAndXp()
        {
            GameManager gm = GameManager.single;
            gm.fusionLevel = 0;
            gm.fxpMax = 75;
            gm.fxpNow = 0;
            gm.xpNow = 0;
            gm.xpMax = 5;
        }

        public void ResetInventory()
        {
            InventoryManager im = InventoryManager.single;

            im.hasWeapon = false;
            im.hasShield = false;
            im.hasHelmet = false;
            im.hasArmor = false;
            im.hasAcc = false;
            im.ClearInventory();

        }

        public void ResetStats()
        {
            GameManager gm = GameManager.single;
            gm.atkSpdLevel = 0;
            gm.critDmgLevel = 0;
            gm.critLevel = 0;
            gm.defLevel = 0;
            gm.dmgLevel = 0;
            gm.hpLevel = 0;
            gm.lastStatPurchased = LevelTypeEnum.NONE;
        }

        /* The following metods came from the referenced URL */
        //string UTF8ByteArrayToString(byte[] characters)
        //{
        //    UTF8Encoding encoding = new UTF8Encoding();
        //    string constructedString = encoding.GetString(characters);
        //    return (constructedString);
        //}

        void CreateJSON()
        {
            PlayerPrefs.SetString(SaveKey, _data);
            PlayerPrefs.SetString(LastBattleKey, _dataLastBattle);
            LoggerMethods.Log("saved to prefs");
        }

        // public UserData userData;
        // JsonUtility's runtime JSON reader does not honor FormerlySerializedAs.
        // Preserve already-learned pet levels from pre-rename saves; a new field wins if both exist.
        public static UserData DeserializeUserData(string json)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(json, @"""cindrelLevel""\s*:"))
                json = System.Text.RegularExpressions.Regex.Replace(json, @"""taironTheDragonLevel""\s*:", "\"cindrelLevel\":");
            return JsonUtility.FromJson<UserData>(json);
        }

        void LoadJSON()
        {
            string _info = "";
            if (PlayerPrefs.HasKey(SaveKey))
            {
                _info = PlayerPrefs.GetString(SaveKey);

                _userData = DeserializeUserData(_info);

                //_data = _info;
                LoggerMethods.Log("File Read Success");
            }
            else
            {
                LoggerMethods.Log("Save File Not Found.");
                EventManager.single.CallLoadComplete();
            }

            if (PlayerPrefs.HasKey(LastBattleKey))
            {
                _dataLastBattle = PlayerPrefs.GetString(LastBattleKey);
            }
        }

        void OnApplicationQuit()
        {
            Save();
        }
    }
    [System.Serializable]
    public class UserData
    {
        //public float soundLevel;
        //public float musicLevel;
        public float currentExp;

        //inventory
        public List<Item> weaponsOwned = new List<Item>();
        public List<Item> shieldsOwned = new List<Item>();
        public List<Item> helmetsOwned = new List<Item>();
        public List<Item> armorsOwned = new List<Item>();
        public List<Item> accOwned = new List<Item>();
        public List<Chest> chestsOwned = new List<Chest>();

        //xp booster feature
        public bool isXpBoosterActive;//= GameManager.single.isBoostedXPActive;
        public string xpBoostTimerString;// = GameManager.single.xpBoostEndTimeString;

        //inventory worn
        public Item weaponEq, shieldEq, helmetEq, armorEq, accEq;

        //bools for inv wearing
        public bool hasWeapon = false;
        public bool hasShield = false;
        public bool hasHelmet = false;
        public bool hasArmor = false;
        public bool hasAcc = false;

        public int playerLevel = 1;
        public int playerDeathCount; // Missing in older saves means No. 001.
        public int rebirthLevel = 0;
        public int fusionLevel;
        public int hpLevel;
        public int dmgLevel;
        public int defLevel;
        public int atkSpdLevel;
        public int critLevel;
        public int critDmgLevel;
        // Zero is deliberately NONE for old JSON saves that have no allocation history.
        // IDs 1..6 map to HP, DMG, DEF, ATK_SPD, CRIT, CRIT_DMG.
        public int lastAllocatedStatId;
        public LevelTypeEnum LastAllocatedStat {
            get => lastAllocatedStatId >= 1 && lastAllocatedStatId <= 6 ? (LevelTypeEnum)(lastAllocatedStatId - 1) : LevelTypeEnum.NONE;
            set => lastAllocatedStatId = value >= LevelTypeEnum.HP && value <= LevelTypeEnum.CRIT_DMG ? (int)value + 1 : 0;
        }
        public int regenLevel = 0;
        public int pathCleanerLevel = 0;
        public int sinisterStampedeLevel = 0;
        public int devilOnMyShoulderLevel = 0;
        public int angelOnMyShoulderLevel = 0;
        public int luckyBastardLevel = 0;
        public int ressurrectionLevel = 0;
        public int twiceTheFunLevel = 0;
        [UnityEngine.Serialization.FormerlySerializedAs("taironTheDragonLevel")]
        public int cindrelLevel = 0;
        public int lastButNotLeastLevel = 0;
        public int lightningDemiGodlevel = 0;
        public int bossKillerLevel = 0;
        public int myLuckyStarsLevel = 0;
        public int battlefieldTacticianLevel = 0;
        public int breathOfHellLevel = 0;

        public float xpNow;
        public float xpMax;
        public float fxpNow;
        public float fxpMax = 75;
        public int skillPoints;
        public int statPoints;
        public float crystals;
        public float premiumCurrency;

        //stacked
        public float stackedXp;
        //public float stackedStatPoints;
        //public float stackedSkillPoints;
        public float stackedCrystals;

        public List<string> thingsSeenPermanent = new List<string>();
        public List<WaveReached> highestWavesReached = new List<WaveReached>();

    }

    [System.Serializable]
    public class CustomDictionaryPair
    {
        public string m_Key;
        public int m_Val;
    }
}
