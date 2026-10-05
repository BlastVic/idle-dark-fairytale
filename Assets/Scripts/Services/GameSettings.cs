using System;
using UnityEngine;
using Assets.Scripts.Extentions.ResourceAsset;

namespace Assets.Scripts.Services
{
    [ExecuteInEditMode]
    public class GameSettings : SingletonResourcesAsset<GameSettings>
    {
        public static SystemLanguage Language
        {
            get => PlayerPrefs.HasKey("Language") ? (SystemLanguage)PlayerPrefs.GetInt("Language") : Application.systemLanguage;
            set => PlayerPrefs.SetInt("Language", (Int32)value);
        }
    }
}
