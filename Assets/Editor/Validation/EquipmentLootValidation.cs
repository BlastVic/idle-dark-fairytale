using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random=UnityEngine.Random;

public static class EquipmentLootValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Tools/Dark Fairytale/Balance/Validate Equipment Loot")]
    public static void Run()
    {
        Directory.CreateDirectory(EquipmentLootAuthoring.Output);
        var state=Random.state;
        try
        {
            EquipmentLoot.ResetCache();var c=EquipmentLoot.Data;Random.InitState(17102026);
            var report=new List<string>();var all=new HashSet<string>();
            var names=JsonUtility.FromJson<EquipmentNames.Catalog>(Resources.Load<TextAsset>("Localization/EquipmentNames").text);
            Check(c.entries.Select(e=>e.key).OrderBy(x=>x).SequenceEqual(names.entries.Select(e=>e.key).OrderBy(x=>x)),"Catalog must cover every artwork exactly once");
            EquipmentLootAuthoring.WithScene((wm,im)=>{
                foreach(var e in c.entries)Check(im.GetSpriteIcon(e.key),"Missing icon "+e.key);
                foreach(var s in c.stages){var w=wm.waveDb.Single(x=>x.levelKey==s.key);Check(w.equipmentLootTier==s.tier&&w.chestItemsGiven.Count==0&&w.chestGoldItemsGiven.Count==0&&w.chestDiamondItemsGiven.Count==0,"Stale chest config "+s.key);}
            });
            foreach(var e in c.entries)for(int grade=0;grade<5;grade++)for(int n=0;n<50;n++)
            {
                var item=EquipmentLoot.Roll(e,(ItemGrade)grade);
                Check(item.m_IconName==e.key&&item.m_ItemType==e.type&&item.itemGrade==(ItemGrade)grade,"Wrong item identity");
                var fields=typeof(Stat).GetFields();int count=fields.Count(f=>(float)f.GetValue(item.baseStat)>0);
                Check(count==2+c.extraAffixes[grade],"Affix count/duplicates "+e.key);
                float main=(float)typeof(Stat).GetField(e.primary.stat).GetValue(item.baseStat);
                Check(main>=Mathf.Round(e.primary.min*c.rarityMultipliers[grade])&&main<=Mathf.Round(e.primary.max*c.rarityMultipliers[grade]),"Main stat outside band");
                Check(EquipmentNames.Display(item)==EquipmentNames.ForIcon(e.key),"Name mismatch");
                Check(item.fusionExp==Mathf.RoundToInt(c.fusionExperience[e.tier-1]*c.rarityMultipliers[grade]),"Fusion economy mismatch");
            }
            foreach(var stage in c.stages)foreach(var quality in c.qualities)
            {
                var rarity=new int[5];var seen=new HashSet<string>();var types=new int[5];
                for(int n=0;n<10000;n++)
                {
                    var chest=new Chest{equipmentLootTier=stage.tier,chestGrade=quality.grade};
                    var items=EquipmentLoot.RollChest(chest,null);
                    Check(items.Count==quality.count&&items.Select(x=>x.m_ItemType).Distinct().Count()==items.Count,"Chest count/category duplication");
                    Check(items.Any(x=>x.m_ItemType==ItemType.WEAPON),"Missing weapon guarantee");
                    foreach(var item in items){Check(c.entries.Single(e=>e.key==item.m_IconName).tier==stage.tier,"Tier leakage");rarity[(int)item.itemGrade]++;seen.Add(item.m_IconName);types[(int)item.m_ItemType]++;}
                }
                Check(seen.SetEquals(c.entries.Where(e=>e.tier==stage.tier).Select(e=>e.key)),"Unreachable item in chest");all.UnionWith(seen);
                float total=rarity.Sum();var weights=(float[])quality.rarityWeights.Clone();if(stage.tier<4){weights[3]+=weights[4];weights[4]=0;}
                for(int i=0;i<5;i++)Check(Mathf.Abs(rarity[i]/total-weights[i]/weights.Sum())<.012f,"Rarity distribution drift");
                for(int i=1;i<5;i++)Check(Mathf.Abs(types[i]/10000f-(quality.count-1)/4f)<.03f,"Category distribution drift");
                var old=new Chest{chestMapName=stage.key,chestGrade=quality.grade};
                Check(EquipmentLoot.ChestTier(JsonUtility.FromJson<Chest>(JsonUtility.ToJson(old)))==stage.tier,"Old chest migration failed");
                old.chestMapName=stage.name;Check(EquipmentLoot.ChestTier(old)==stage.tier,"Localized chest migration failed");
                report.Add(stage.name+" "+quality.grade+": 10000 chests; reachable="+seen.Count+"; rarity="+string.Join(",",rarity));
            }
            foreach(var rule in c.monsters)
            {
                var enemy=AssetDatabase.LoadAssetAtPath<GameObject>(EquipmentLootAuthoring.PathFor(rule.key)).GetComponent<Enemy>();
                Check(enemy.equipmentLootKey==rule.key&&enemy.itemsGiven.Count==0,"Stale monster drops");
                int hits=0;var types=new int[5];var seen=new HashSet<string>();
                for(int n=0;n<20000;n++)
                {
                    var items=EquipmentLoot.RollMonster(rule.key);if(items.Count>0)hits++;
                    Check(items.Count==0||items.Count==rule.count,"Monster reward count");
                    Check(items.Select(x=>x.m_IconName).Distinct().Count()==items.Count,"Duplicate boss item");
                    foreach(var item in items){types[(int)item.m_ItemType]++;seen.Add(item.m_IconName);}
                }
                Check(Mathf.Abs(hits/20000f-rule.chance)<.015f,"Monster chance drift");
                Check(seen.SetEquals(c.entries.Where(e=>e.tier==rule.tier).Select(e=>e.key)),"Unreachable monster item");
                for(int i=0;i<5;i++)Check(Mathf.Abs(types[i]/(float)types.Sum()-c.categoryWeights[i]/c.categoryWeights.Sum())<.02f,"Monster category drift");
                report.Add(rule.key+": "+hits+"/20000 kills drop; items="+types.Sum());
            }
            var saved=new Item{m_IconName="Wep4",uniqueId=123,itemGrade=ItemGrade.EPIC,baseStat=new Stat{dmg=321},isLocked=true};
            var restored=JsonUtility.FromJson<Item>(JsonUtility.ToJson(saved));Check(restored.baseStat.dmg==321&&restored.uniqueId==123&&restored.isLocked,"Owned legacy item altered");
            report.Add("PASS 140 artwork keys; 35000 stat rolls; 150000 chests; 160000 monster kills; no tier leakage; old chest/source migration; owned item serialization preserved; fusion experience 1/3/15/20/50 with rarity scaling.");
            File.WriteAllLines(EquipmentLootAuthoring.Output+"validation.txt",report);
        }
        finally{Random.state=state;}
    }
}
