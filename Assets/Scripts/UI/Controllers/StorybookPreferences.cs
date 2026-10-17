using UnityEngine;

public static class StorybookPreferences
{
    public const string SkipLoadingKey="Storybook.SkipLoadingAnimation";
    static string StorageKey {
        get {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetString("SETTINGS_QA_PREF_KEY",SkipLoadingKey);
#else
            return SkipLoadingKey;
#endif
        }
    }
    public static bool SkipLoadingAnimation
    {
        get {
#if UNITY_EDITOR
            int test=UnityEditor.SessionState.GetInt("SETTINGS_QA_SKIP",-1);
            if(test>=0)return test==1;
#endif
            return PlayerPrefs.GetInt(StorageKey,0)==1;
        }
        set {
#if UNITY_EDITOR
            if(UnityEditor.SessionState.GetBool("MONSTER_QA_DISABLE_SAVE",false)&&StorageKey==SkipLoadingKey){UnityEditor.SessionState.SetInt("SETTINGS_QA_SKIP",value?1:0);return;}
#endif
            PlayerPrefs.SetInt(StorageKey,value?1:0);PlayerPrefs.Save();
        }
    }
}
