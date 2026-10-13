using System;
using UnityEngine;

/// <summary>Saved node-to-environment bindings; prefab children remain editable.</summary>
[CreateAssetMenu(menuName = "Dark Fairytale/Chapter Environment Catalog")]
public sealed class ChapterEnvironmentCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string mapKey;
        public string levelKey;
        public string displayName;
        public GameObject prefab;
    }

    public Entry[] entries = Array.Empty<Entry>();

    public GameObject Find(string mapKey, string levelKey = null)
    {
        foreach (var entry in entries)
            if (!string.IsNullOrEmpty(levelKey) && entry.levelKey == levelKey) return entry.prefab;
        foreach (var entry in entries)
            if (entry.mapKey == mapKey) return entry.prefab;
        return null;
    }
}
