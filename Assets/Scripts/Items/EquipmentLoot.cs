using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>One authored catalog for equipment progression, rolls and reward sources.</summary>
public static class EquipmentLoot
{
    [Serializable] public sealed class Range { public string stat; public int min, max; }
    [Serializable] public sealed class Entry
    {
        public string key, profile;
        public ItemType type;
        public int tier;
        public Range primary, signature;
        public Range[] bonuses;
    }
    [Serializable] public sealed class Stage { public int tier; public string key, name; }
    [Serializable] public sealed class MonsterRule
    {
        public string key;
        public int tier, count;
        public float chance;
        public ChestGrade quality;
    }
    [Serializable] public sealed class Quality
    {
        public ChestGrade grade;
        public int count;
        public float[] rarityWeights;
    }
    [Serializable] public sealed class Catalog
    {
        public int version;
        public Stage[] stages;
        public MonsterRule[] monsters;
        public Quality[] qualities;
        public float[] categoryWeights, rarityMultipliers;
        public int[] extraAffixes, fusionExperience;
        public Entry[] entries;
    }
    static Catalog catalog;
    public static Catalog Data
    {
        get
        {
            if (catalog == null)
            {
                var asset = Resources.Load<TextAsset>("Data/Equipment/EquipmentLoot");
                if (!asset) throw new InvalidOperationException("Missing equipment loot catalog");
                catalog = JsonUtility.FromJson<Catalog>(asset.text);
                Validate(catalog);
            }
            return catalog;
        }
    }
    public static void ResetCache() => catalog = null;
    public static void Validate(Catalog c)
    {
        void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void CheckRange(Range r) { Require(r != null && r.min > 0 && r.max >= r.min &&
            new[]{"dmg","def","hpMax","atkSpd","crit","critDmg","dmgPerc","defPerc","hpPerc"}.Contains(r.stat), "Invalid equipment stat range"); }
        Require(c != null && c.version == 1, "Unsupported equipment catalog");
        Require(c.categoryWeights.Length == 5 && c.categoryWeights.All(w => w > 0), "Missing category weights");
        Require(c.rarityMultipliers.Length == 5 && c.rarityMultipliers.All(w => w >= 1) && c.extraAffixes.Length == 5 && c.extraAffixes.All(x=>x>=0&&x<=3), "Invalid rarity settings");
        Require(c.fusionExperience.Length==5 && c.fusionExperience.All(x=>x>0), "Invalid fusion experience");
        Require(c.stages.Length == 5 && c.stages.Select(s=>s.tier).OrderBy(x=>x).SequenceEqual(Enumerable.Range(1,5)) && c.stages.Select(s=>s.key).Distinct().Count()==5, "Invalid stages");
        Require(c.entries.Select(e=>e.key).Distinct().Count() == c.entries.Length, "Duplicate equipment key");
        foreach (var e in c.entries)
        {
            Require(e.tier >= 1 && e.tier <= 5 && (int)e.type>=0 && (int)e.type<5 && EquipmentNames.ForIcon(e.key) != null, "Unknown equipment " + e.key);
            CheckRange(e.primary); CheckRange(e.signature);
            Require(e.primary.stat == (e.type==ItemType.WEAPON?"dmg":e.type==ItemType.ACCESSORY?"hpMax":"def"), "Wrong primary stat " + e.key);
            Require(e.bonuses.Length == 3, "Missing affixes " + e.key);
            foreach(var r in e.bonuses) CheckRange(r);
            Require(new[]{e.primary.stat,e.signature.stat}.Concat(e.bonuses.Select(r=>r.stat)).Distinct().Count()==5, "Duplicate affix " + e.key);
        }
        for(int tier=1;tier<=5;tier++) for(int type=0;type<5;type++)
            Require(c.entries.Any(e=>e.tier==tier&&(int)e.type==type), "Empty equipment category");
        Require(c.qualities.Length==3 && c.qualities.Select(q=>(int)q.grade).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,3)), "Invalid chest grades");
        foreach(var q in c.qualities) Require(q.count>=1&&q.count<=5&&q.rarityWeights.Length==5&&q.rarityWeights.All(w=>w>=0)&&q.rarityWeights.Sum()>0, "Invalid chest quality");
        Require(c.monsters.Select(m=>m.key).Distinct().Count()==c.monsters.Length,"Duplicate monster rule");
        foreach(var m in c.monsters) Require(m.tier>=1&&m.tier<=5&&m.count>0&&m.count<=5&&m.chance>0&&m.chance<=1&&c.qualities.Any(q=>q.grade==m.quality),"Invalid monster rule");
    }
    static int Weighted(float[] weights)
    {
        float value = Random.value * weights.Sum();
        for(int i=0;i<weights.Length;i++) { value -= weights[i]; if(value < 0) return i; }
        return Array.FindLastIndex(weights, w=>w>0);
    }
    static ItemGrade RollGrade(int tier, ChestGrade quality)
    {
        var weights = (float[])Data.qualities.Single(q=>q.grade==quality).rarityWeights.Clone();
        // Legendary gear belongs to the last two progression bands.
        if(tier<4) { weights[3]+=weights[4]; weights[4]=0; }
        return (ItemGrade)Weighted(weights);
    }
    public static Item Roll(Entry entry, ItemGrade grade)
    {
        if((int)grade<0||(int)grade>4) throw new ArgumentOutOfRangeException(nameof(grade));
        var item = new Item { m_IconName=entry.key, m_ItemBasicTitle=EquipmentNames.ForIcon(entry.key),
            m_ItemFinalTitle=EquipmentNames.ForIcon(entry.key), m_ItemType=entry.type, itemGrade=grade,
            m_ChanceDrop=1, baseStat=new Stat(), rollStat=new RollStat(),
            fusionExp=Mathf.RoundToInt(Data.fusionExperience[entry.tier-1]*Data.rarityMultipliers[(int)grade]), uniqueId=Random.Range(1,int.MaxValue) };
        // Combat uses one shared RedHood skeleton; equipment art remains UI-only.
        float mult = Data.rarityMultipliers[(int)grade];
        Apply(item.baseStat,entry.primary,mult); Apply(item.baseStat,entry.signature,mult);
        var extras = entry.bonuses.ToList();
        for(int i=0;i<Data.extraAffixes[(int)grade];i++)
        {
            int at=Random.Range(0,extras.Count); Apply(item.baseStat,extras[at],mult); extras.RemoveAt(at);
        }
        return item;
    }
    static void Apply(Stat s, Range r, float multiplier)
    {
        // Explicit inclusive integer ranges; never truncate a float roll to a lower band.
        float value=Mathf.Max(1,Mathf.RoundToInt(Random.Range(r.min,r.max+1)*multiplier));
        switch(r.stat)
        {
            case "dmg": s.dmg=value; break; case "def": s.def=value; break;
            case "hpMax": s.hpMax=value; break; case "atkSpd": s.atkSpd=value; break;
            case "crit": s.crit=value; break; case "critDmg": s.critDmg=value; break;
            case "dmgPerc": s.dmgPerc=value; break; case "defPerc": s.defPerc=value; break;
            case "hpPerc": s.hpPerc=value; break;
            default: throw new InvalidOperationException("Unknown stat "+r.stat);
        }
    }
    static Item Pick(int tier, ItemType type, ChestGrade quality, HashSet<string> excluded)
    {
        var pool=Data.entries.Where(e=>e.tier==tier&&e.type==type&&!excluded.Contains(e.key)).ToArray();
        if(pool.Length==0) throw new InvalidOperationException("Equipment pool exhausted");
        var entry=pool[Random.Range(0,pool.Length)]; excluded.Add(entry.key);
        return Roll(entry,RollGrade(tier,quality));
    }
    public static List<Item> RollMonster(string key)
    {
        var rule=Data.monsters.Single(m=>m.key==key);
        var result=new List<Item>();
        if(Random.value>=rule.chance) return result;
        var used=new HashSet<string>();
        for(int i=0;i<rule.count;i++) result.Add(Pick(rule.tier,(ItemType)Weighted(Data.categoryWeights),rule.quality,used));
        return result;
    }
    public static int ChestTier(Chest chest)
    {
        if(chest.equipmentLootTier>0) return chest.equipmentLootTier;
        // Pre-catalog saves store the source level name, not the source key.
        var stage=Data.stages.FirstOrDefault(s=>s.key==chest.chestMapName||s.name==chest.chestMapName);
        return stage==null?0:stage.tier;
    }
    public static List<Item> RollChest(Chest chest, InventoryManager inventory)
    {
        int tier=ChestTier(chest);
        var result=new List<Item>();
        if(tier>0)
        {
            int count=Data.qualities.Single(q=>q.grade==chest.chestGrade).count;
            var types=new List<ItemType>{ItemType.SHIELD,ItemType.HELMET,ItemType.ARMOR,ItemType.ACCESSORY};
            var used=new HashSet<string>();
            result.Add(Pick(tier,ItemType.WEAPON,chest.chestGrade,used));
            for(int i=1;i<count;i++)
            {
                int at=Random.Range(0,types.Count);result.Add(Pick(tier,types[at],chest.chestGrade,used));types.RemoveAt(at);
            }
            return result;
        }
        // Unrelated test/future legacy chests retain their explicitly authored rewards.
        var pool=chest.chestGrade==ChestGrade.DIAMOND?chest.possibleDiamondRewards:chest.chestGrade==ChestGrade.GOLD?chest.possibleGoldRewards:chest.possibleRewards;
        if(pool!=null) foreach(var candidate in pool)
        {
            if(Random.value>=candidate.m_ChanceDrop) continue;
            var item=inventory.CreateItemStats(candidate); item.m_ItemFinalTitle=inventory.GetRandomName(item);
            item.uniqueId=Random.Range(1,int.MaxValue); result.Add(item);
        }
        return result;
    }
}
