using System;
using Assets.Scripts.Extentions.ResourceAsset;
using UnityEngine;

namespace Scripts.Skills
{
    public class SkillsDataDescriptions : SingletonResourcesAsset<SkillsDataDescriptions>
    {
        [Header("- Skills Data -")]
        [SerializeField]
        private SkillData[] _skillDataArr;

        public static SkillData[] SkillDataArr => Instance._skillDataArr;
    }
    //each skill level
    [Serializable]
    public class SkillData
    {
        public string key;
        public SkillsTypeEnum skillsType;
        public string desc;
        public Sprite icon, iconGray;
        public float[] value1;
        public float[] value2;
        public float[] value3;
        public float[] value4;
        public float[] value5;

    }
}