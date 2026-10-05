using System;
using System.Collections.Generic;
using Assets.Scripts.Extentions.ResourceAsset;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Scripts.Level
{
    public class LevelDataDescriptions : SingletonResourcesAsset<LevelDataDescriptions>
    {
        [SerializeField]
        private Int32[] _expLevelDb;
        [SerializeField]
        private FusionLevel[] _fusionLevelDb;
        [SerializeField]
        private RebirthLevel[] _rebirthLevelDb;
        [SerializeField]
        private String[] _rebirthOneLiners;

        [Header("Ignore element zero on stat ladders")]
        [SerializeField]
        private Int32[] _hpLevelDb;
        [SerializeField]
        public Int32[] _dmgLevelDb;
        [SerializeField]
        public Int32[] _defLevelDb;
        [SerializeField]
        public Int32[] _atkSpdLevelDb;
        [SerializeField]
        public float[] _critLevelDb;
        [SerializeField]
        public Int32[] _critDmgLevelDb;

        [Header("Assest Addressable")]
        [SerializeField]
        private AddressableStringPair[] _enemyDb;
        [SerializeField]
        private AddressableStringPair[] _mapDb;
        [SerializeField]
        private AddressableStringPair[] _particleDb;
        [SerializeField]
        private AddressableStringPair[] _generalObjectsDb;


        public static Int32[] ExpLevelDb => Instance._expLevelDb;

        public static FusionLevel[] FusionLevelDb => Instance._fusionLevelDb;

        public static RebirthLevel[] RebirthLevelDb => Instance._rebirthLevelDb;

        public static String[] RebirthOneLiners => Instance._rebirthOneLiners;

        //Ignore element zero on stat ladders
        public static Int32[] HpLevelDb => Instance._hpLevelDb;

        public static Int32[] DmgLevelDb => Instance._dmgLevelDb;

        public static Int32[] DefLevelDb => Instance._defLevelDb;

        public static Int32[] AtkSpdLevelDb => Instance._atkSpdLevelDb;

        public static float[] CritLevelDb => Instance._critLevelDb;

        public static Int32[] CritDmgLevelDb => Instance._critDmgLevelDb;

        //Assest Addressable
        public static AddressableStringPair[] EnemyDb => Instance._enemyDb;
        public static AddressableStringPair[] MapDb => Instance._mapDb;
        public static AddressableStringPair[] ParticleDb => Instance._particleDb;
        public static AddressableStringPair[] GeneralObjectsDb => Instance._generalObjectsDb;

        public static void Create()
        {
        }
    }
    [Serializable]
    public class FusionLevel
    {
        public float requirement;
        public int statPointsGiven;
        public int skillPointsGiven;
        public float expGiven;
        public List<Item> itemsGiven;
    }

    [Serializable]
    public class RebirthLevel
    {
        public float minLevel = 100;
        public int statPointsGiven;
        public int skillPointsGiven;
        public float expGiven;
        public float crystalsGiven;
    }
    [Serializable]
    public class AddressableStringPair
    {
        public string key;
        public AssetReference assetRef;
    }
}