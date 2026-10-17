using System;
using Assets.Scripts.Services;
using Scripts.Level;
using Scripts.Skills;
using UnityEditor;
using UnityEngine;
using PlayerPrefs = Assets.Scripts.Services.PlayerPrefs;

namespace IdleKnightHero.Editor
{
    public static class MenuExtentions
    {
        [MenuItem("IdleKnightHero/Clear prefs", false, 2)]
        public static void ClearPrefs()
        {
            if (EditorUtility.DisplayDialog("Warning", "Are you sure you want to clear all player prefs?", "Ok", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
            }
        }
        [MenuItem("IdleKnightHero/Data/Game settings", false, 10)]
        public static void OpenGameSettings()
        {
            Selection.activeObject = GameSettings.Instance;
        }
        [MenuItem("IdleKnightHero/Data/Level data descriptions", false, 11)]
        public static void OpenLevelDataDescriptions()
        {
            Selection.activeObject = LevelDataDescriptions.Instance;
        }

        [MenuItem("IdleKnightHero/Data/Skills data descriptions", false, 12)]
        public static void OpenSkillsDataDescriptions()
        {
            Selection.activeObject = SkillsDataDescriptions.Instance;
        }
    }
}
