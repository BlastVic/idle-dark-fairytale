using System;
using System.Collections.Generic;
using UnityEngine;

// Names follow the current artwork key, including items already present in older saves.
public static class EquipmentNames
{
    [Serializable] public sealed class Entry { public string key, name; }
    [Serializable] public sealed class Catalog { public Entry[] entries; }
    static Dictionary<string,string> names;

    public static string ForIcon(string key)
    {
        if(string.IsNullOrEmpty(key)) return null;
        if(names==null) {
            var asset=Resources.Load<TextAsset>("Localization/EquipmentNames");
            if(!asset) throw new InvalidOperationException("Missing equipment name catalog");
            names=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var entry in JsonUtility.FromJson<Catalog>(asset.text).entries)
                names.Add(entry.key,entry.name);
        }
        return names.TryGetValue(key,out var name)?name:null;
    }

    public static string Display(Item item)
    {
        if(item==null) return string.Empty;
        // Non-equipment rewards must retain their own text.
        var name=item.itemGrade==ItemGrade.UI_ONLY?null:ForIcon(item.m_IconName);
        return name ?? (string.IsNullOrWhiteSpace(item.m_ItemFinalTitle)?item.m_ItemBasicTitle:item.m_ItemFinalTitle);
    }
}
