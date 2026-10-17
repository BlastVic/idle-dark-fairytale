using System;

/// <summary>Stable English resource keys and independently authored Chinese display names.</summary>
public static class CreatureNames
{
    public readonly struct Entry
    {
        public readonly string Key, DisplayName;
        public bool IsPet => Key.StartsWith("Pet", StringComparison.Ordinal);
        public Entry(string key, string displayName) { Key = key; DisplayName = displayName; }
    }

    public static readonly Entry[] Entries = {
        new Entry("MobTallowkin", "红帽菇"),
        new Entry("MobGnarlo", "木头菇"),
        new Entry("MobDrowseed", "瞌睡菇"),
        new Entry("MobHexmaw", "红眼狼"),
        new Entry("MobVesperling", "枯叶菇"),
        new Entry("MobBramblet", "刺帽菇"),
        new Entry("MobSplinterjaw", "疯松鼠"),
        new Entry("MobGloamweb", "花背蛛"),
        new Entry("MobRivenfang", "霜牙狼王"),
        new Entry("MobMurmlet", "水史莱姆"),
        new Entry("MobStillmere", "史莱姆之王"),
        new Entry("PetCindrel", "书脊龙")
    };

    public static string DisplayName(string assetName)
    {
        string key = assetName.Replace("_SkeletonData", "").Replace("(Clone)", "");
        foreach (var entry in Entries) if (entry.Key == key) return entry.DisplayName;
        return key;
    }
}
