using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class InventoryManager : MonoBehaviour
{

    // [Header("-Master Database-")]
    public List<SpritePair> m_ItemIcons = new List<SpritePair>();

    [Header("- Master Adjectives -")]
    public string[] m_OrdinaryAdjectives;
    public string[] m_RareAdjectives;
    public string[] m_VeryRareAdjectives;
    public string[] m_EpicAdjectives;
    public string[] m_LegendaryAdjectives;

    [Header("- Master Titles -")]
    public string[] m_OrdinaryTitles;
    public string[] m_RareTitles;
    public string[] m_VeryRareTitles;
    public string[] m_EpicTitles;
    public string[] m_LegendaryTitles;

    [Header("- Master Grades Colors -")]
    public Color m_OrdinaryColor;
    public Color m_RareColor;
    public Color m_VeryRareColor;
    public Color m_EpicColor;
    public Color m_LegendaryColor;
    public Color m_UIOnlyColor;

    [Header("-Player Owned-")]
    public List<Item> weaponsOwned = new List<Item>();
    public List<Item> shieldsOwned = new List<Item>();
    public List<Item> helmetsOwned = new List<Item>();
    public List<Item> armorsOwned = new List<Item>();
    public List<Item> accOwned = new List<Item>();
    public Chest lastChest;
    public List<Chest> chestsOwned = new List<Chest>();

    [Header("-New Item Flags -")]
    public bool weaponFlag;
    public bool armorFlag, accFlag, helmetFlag, shieldFlag;

    //Rare Rates
    public float m_RareRate = .77f;
    public float m_VeryRareRate = .85f;
    public float m_EpicRate = .99f;
    public float m_LegendaryRate = .999f;

    [Header("-Default Icons")]
    public Sprite m_DefaultWeapon, m_DefaultHead, m_DefaultBody, m_DefaultGem;

    public bool hasWeapon = false;
    public bool hasShield = false;
    public bool hasHelmet = false;
    public bool hasArmor = false;
    public bool hasAcc = false;

    public Item weaponEq, shieldEq, helmetEq, armorEq, accEq;

    private static InventoryManager _instance;

    public static InventoryManager single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<InventoryManager>();
            return _instance;
        }
    }

    public Sprite[] gradeIcons;
    public Sprite GetGradeIcon(ItemGrade itemGrade)
    {
        Sprite sprite = null;
        switch (itemGrade)
        {
            case ItemGrade.LEGENDARY:
                sprite = gradeIcons[0];
                break;
            case ItemGrade.EPIC:
                sprite = gradeIcons[1];
                break;
            case ItemGrade.VERY_RARE:
                sprite = gradeIcons[2];
                break;
            case ItemGrade.RARE:
                sprite = gradeIcons[3];
                break;
            case ItemGrade.ORDINARY:
                sprite = gradeIcons[4];
                break;
        }
        return sprite;
    }

    public Color GetGradeColor(ItemGrade itemGrade)
    {
        Color color = Color.white;
        switch (itemGrade)
        {
            case ItemGrade.LEGENDARY:
                color = m_LegendaryColor;
                break;
            case ItemGrade.EPIC:
                color = m_EpicColor;
                break;
            case ItemGrade.VERY_RARE:
                color = m_VeryRareColor;
                break;
            case ItemGrade.RARE:
                color = m_RareColor;
                break;
            case ItemGrade.ORDINARY:
                color = m_OrdinaryColor;
                break;
        }
        return color;
    }

    public int GetTotalItemCount()
    {
        int total = weaponsOwned.Count + shieldsOwned.Count + helmetsOwned.Count + armorsOwned.Count + accOwned.Count;
        //Debug.Log("TotalItemCount:" + total);
        return total;
    }

    public string GetRandomName(Item nextItem)
    {
        string adj = GetAdjective(nextItem.itemGrade);
        //string adj = InventoryManager.single.GetAdjective(nextItem.m_ItemGrade);

        if (Random.Range(0, 2) == 1) adj = "";
        string spacer = " ";
        string nameGenerated = "";
        nameGenerated = adj + spacer + GetTitle(nextItem.itemGrade) + spacer + nextItem.m_ItemBasicTitle;
        nextItem.m_ItemFinalTitle = nameGenerated;
        return nameGenerated;
    }

    public ItemGrade GetGradeRoll()
    {
        float roll = Random.Range(0, 1f);
        ItemGrade grade = ItemGrade.ORDINARY;

        if (roll >= m_LegendaryRate)
        {
            grade = ItemGrade.LEGENDARY;
            return grade;
        }
        if (roll >= m_EpicRate)
        {
            grade = ItemGrade.EPIC;
            return grade;
        }
        if (roll >= m_VeryRareRate)
        {
            grade = ItemGrade.VERY_RARE;
            return grade;
        }
        if (roll >= m_RareRate)
        {
            grade = ItemGrade.RARE;
            return grade;
        }
        grade = ItemGrade.ORDINARY;
        return grade;
    }

    public double GetGradeCostMult(ItemGrade grade)
    {
        double mult = 3;
        if (grade == ItemGrade.LEGENDARY)
        {
            mult = 52;
        }
        if (grade == ItemGrade.EPIC)
        {
            mult = 42;
        }
        if (grade == ItemGrade.VERY_RARE)
        {
            mult = 30;
        }
        if (grade == ItemGrade.RARE)
        {
            mult = 25;
        }
        return mult;
    }


    private void Awake()
    {
        InventoryManager[] foundObjects = FindObjectsOfType<InventoryManager>();
        if (foundObjects.Length > 1)
        {
            Destroy(foundObjects[1].gameObject);
        }
    }

    public string GetAdjective(ItemGrade itemGrade)
    {
        string s = "Tough";
        switch (itemGrade)
        {
            case ItemGrade.ORDINARY:
                s = m_OrdinaryAdjectives[Random.Range(0, m_OrdinaryAdjectives.Length)];
                break;
            case ItemGrade.RARE:
                s = m_RareAdjectives[Random.Range(0, m_RareAdjectives.Length)];
                break;
            case ItemGrade.VERY_RARE:
                s = m_VeryRareAdjectives[Random.Range(0, m_VeryRareAdjectives.Length)];
                break;
            case ItemGrade.EPIC:
                s = m_EpicAdjectives[Random.Range(0, m_EpicAdjectives.Length)];
                break;
            case ItemGrade.LEGENDARY:
                s = m_LegendaryAdjectives[Random.Range(0, m_LegendaryAdjectives.Length)];
                break;
        }
        return s;// LocalizationManager.single.GetLocalizedValue(s);
    }


    public Color GetColor(ItemGrade itemGrade)
    {
        Color c = m_OrdinaryColor;
        switch (itemGrade)
        {
            case ItemGrade.ORDINARY:
                break;
            case ItemGrade.RARE:
                c = m_RareColor;
                break;
            case ItemGrade.VERY_RARE:
                c = m_VeryRareColor;
                break;
            case ItemGrade.EPIC:
                c = m_EpicColor;
                break;
            case ItemGrade.LEGENDARY:
                c = m_LegendaryColor;
                break;
            case ItemGrade.UI_ONLY:
                c = m_UIOnlyColor;
                break;
        }
        return c;
    }


    #region ITEM CREATION METHODS
    public Item CreateItemStats(Item item)
    {
        Item newItem = Utilities.DeepClone(item);
        newItem.baseStat = new Stat();

        if (newItem.m_ItemType == ItemType.WEAPON)
        {
            newItem.baseStat.dmg = (int)Random.Range(item.rollStat.dmgMin, item.rollStat.dmgMax);
        }

        if (newItem.m_ItemType == ItemType.ARMOR)
        {
            //newItem.baseStat.hpMax = (int)Random.Range(item.rollStat.hpMin, item.rollStat.hpMax_rolled);
            newItem.baseStat.def = (int)Random.Range(item.rollStat.defMin, item.rollStat.defMax);
        }

        if (newItem.m_ItemType == ItemType.HELMET)
        {
            //newItem.baseStat.hpMax = (int)Random.Range(item.rollStat.hpMin, item.rollStat.hpMax_rolled);
            newItem.baseStat.def = (int)Random.Range(item.rollStat.defMin, item.rollStat.defMax);
        }

        if (newItem.m_ItemType == ItemType.ACCESSORY)
        {
            newItem.baseStat.hpMax = (int)Random.Range(item.rollStat.hpMin, item.rollStat.hpMax_rolled);
        }

        if (newItem.m_ItemType == ItemType.SHIELD)
        {
            newItem.baseStat.def = (int)Random.Range(item.rollStat.defMin, item.rollStat.defMax);
        }

        //Now choose random chance for more bonus stats
        //Debug.Log("PercChance1:" + item.m_BaseStats.percChanceBonus1);
        if (Random.Range(0, 1f) <= newItem.rollStat.percChanceBonus1)
        {
            //Debug.Log("Add Bonus 1!");
            AddBonusStat(ref newItem, item);
        }
        if (Random.Range(0, 1f) <= newItem.rollStat.percChanceBonus2)
        {
            //Debug.Log("Add Bonus 2!");
            AddBonusStat(ref newItem, item);
        }

        if (Random.Range(0, 1f) <= newItem.rollStat.percChanceBonus3)
        {
            //Debug.Log("Add Bonus 2!");
            AddBonusStat(ref newItem, item);
        }

        if (Random.Range(0, 1f) <= newItem.rollStat.percChanceBonus4)
        {
            //Debug.Log("Add Bonus 2!");
            AddBonusStat(ref newItem, item);
        }

        if (newItem.rollStat.defPercMax > 0 & newItem.rollStat.defPercMax < .01) newItem.baseStat.defPerc = .01f;
        if (newItem.rollStat.dmgPercMax > 0 & newItem.rollStat.dmgPercMax < .01) newItem.baseStat.dmgPerc = .01f;
        if (newItem.rollStat.hpPercMax > 0 & newItem.rollStat.hpPercMax < .01) newItem.baseStat.hpPerc = .01f;


        return newItem;
    }

    public Item AddBonusStat(ref Item item, Item templateItem)
    {
        //get all the possible stats 
        List<string> possibleStats = new List<string>();
        if (item.baseStat.dmg == 0 & templateItem.rollStat.dmgMax > 0) possibleStats.Add("DMG");
        if (item.baseStat.def == 0 & templateItem.rollStat.defMax > 0) possibleStats.Add("DEF");
        if (item.baseStat.dmgPerc == 0 & templateItem.rollStat.dmgPercMax > 0) possibleStats.Add("DMG PERC");
        if (item.baseStat.defPerc == 0 & templateItem.rollStat.defPercMax > 0) possibleStats.Add("DEF PERC");
        if (item.baseStat.hpMax == 0 & templateItem.rollStat.hpMax_rolled > 0) possibleStats.Add("HP");
        if (item.baseStat.hpPerc == 0 & templateItem.rollStat.hpPercMax > 0) possibleStats.Add("HP PERC");
        if (item.baseStat.crit == 0 & templateItem.rollStat.critMax > 0) possibleStats.Add("CRIT");
        if (item.baseStat.critDmg == 0 & templateItem.rollStat.critDmgMax > 0) possibleStats.Add("CRIT DMG");

        if (item.baseStat.atkSpd == 0 & templateItem.rollStat.atkSpdMax > 0) possibleStats.Add("ATK SPD");

        //Debug.Log("Possible Stats Count:" + possibleStats.Count);
        if (possibleStats.Count == 0) return item;
        string chosenStat = possibleStats[Random.Range(0, possibleStats.Count)];
        //Debug.Log("ChosenStat was:" + chosenStat);
        switch (chosenStat)
        {
            case "DMG":
                item.baseStat.dmg = (int)Random.Range(templateItem.rollStat.dmgMin, templateItem.rollStat.dmgMax);
                break;
            case "DEF":
                item.baseStat.def = (int)Random.Range(templateItem.rollStat.defMin, templateItem.rollStat.defMax);
                break;
            case "DMG PERC":
                item.baseStat.dmgPerc = (int)Utilities.Truncate(Random.Range(templateItem.rollStat.dmgPercMin, templateItem.rollStat.dmgPercMax), 2);
                if (item.baseStat.dmgPerc < 1) item.baseStat.dmgPerc = 1;
                break;
            case "DEF PERC":
                item.baseStat.defPerc = (int)Utilities.Truncate(Random.Range(templateItem.rollStat.defPercMin, templateItem.rollStat.defPercMax), 2);
                if (item.baseStat.defPerc < 1) item.baseStat.defPerc = 1;
                break;
            case "HP":
                item.baseStat.hpMax = (int)Random.Range(templateItem.rollStat.hpMin, templateItem.rollStat.hpMax_rolled);
                break;
            case "HP PERC":
                item.baseStat.hpPerc = (int)Random.Range(templateItem.rollStat.hpPercMin, templateItem.rollStat.hpPercMax);
                break;
            case "CRIT":
                item.baseStat.crit = (int)Utilities.Truncate(Random.Range(templateItem.rollStat.critMin, templateItem.rollStat.critMax), 2);
                if (item.baseStat.crit < 1) item.baseStat.crit = 1;
                //round to .01
                break;
            case "CRIT DMG":
                item.baseStat.critDmg = (int)Utilities.Truncate(Random.Range(templateItem.rollStat.critDmgMin, templateItem.rollStat.critDmgMax), 2);
                //round to .01
                break;
            case "ATK SPD":
                item.baseStat.atkSpd = (int)Random.Range(templateItem.rollStat.atkSpdMin, templateItem.rollStat.atkSpdMax);
                break;
        }

        return item;
    }


    #endregion


    public string GetItemGradeTitle(ItemGrade itemGrade)
    {
        string s = "Ordinary";
        switch (itemGrade)
        {
            case ItemGrade.ORDINARY:
                break;
            case ItemGrade.RARE:
                s = "Rare";
                break;
            case ItemGrade.VERY_RARE:
                s = "Very Rare";
                break;
            case ItemGrade.EPIC:
                s = "Epic";
                break;
            case ItemGrade.LEGENDARY:
                s = "Legendary";
                break;
            case ItemGrade.UI_ONLY:
                s = "UI Only";
                break;
        }
        return s;// LocalizationManager.single.GetLocalizedValue(s);
    }

    public string GetTitle(ItemGrade itemGrade)
    {
        string s = "Tough";
        switch (itemGrade)
        {
            case ItemGrade.ORDINARY:
                s = m_OrdinaryTitles[Random.Range(0, m_OrdinaryTitles.Length)];
                break;
            case ItemGrade.RARE:
                s = m_RareTitles[Random.Range(0, m_RareTitles.Length)];
                break;
            case ItemGrade.VERY_RARE:
                s = m_VeryRareTitles[Random.Range(0, m_VeryRareTitles.Length)];
                break;
            case ItemGrade.EPIC:
                s = m_EpicTitles[Random.Range(0, m_EpicTitles.Length)];
                break;
            case ItemGrade.LEGENDARY:
                s = m_LegendaryTitles[Random.Range(0, m_LegendaryTitles.Length)];
                break;
        }
        return s;// LocalizationManager.single.GetLocalizedValue(s);
    }


    public Sprite GetSpriteIcon(string s)
    {
        SpritePair iconMap = m_ItemIcons.Where(x => x.spriteName == s).FirstOrDefault();
        if (iconMap != null) return iconMap.sprite;
        return null;
    }

    public int m_MaxItemStorage = 50;
    public bool m_Warned = false;
    public void AddItem(Item nextItem)
    {
        if (GetTotalItemCount() >= m_MaxItemStorage && m_Warned == false)
        {
            //Debug.Log("ITEM OVER MAX");
            //  UniversalCanvas.Instance.m_WarningPopup.Init();
            m_Warned = true;
        }

        Item item = null;
        if (nextItem.m_ItemType == ItemType.WEAPON)
        {
            weaponFlag = true;
            item = Utilities.DeepClone<Item>(nextItem);
            weaponsOwned.Add(item);
        }
        if (nextItem.m_ItemType == ItemType.ARMOR)
        {
            armorFlag = true;
            item = Utilities.DeepClone<Item>(nextItem);
            armorsOwned.Add(item);
        }

        if (nextItem.m_ItemType == ItemType.SHIELD)
        {
            shieldFlag = true;
            item = Utilities.DeepClone<Item>(nextItem);
            shieldsOwned.Add(item);
        }
        if (nextItem.m_ItemType == ItemType.HELMET)
        {
            helmetFlag = true;
            item = Utilities.DeepClone<Item>(nextItem);
            helmetsOwned.Add(item);
        }
        if (nextItem.m_ItemType == ItemType.ACCESSORY)
        {
            accFlag = true;
            item = Utilities.DeepClone<Item>(nextItem);
            accOwned.Add(item);
        }


        //InventoryPanel.Instance.AddSlot(item);
    }

    public void RemoveItem(Item item)
    {        // weapon/shield/helmet/armor/accessories

        if (item.m_ItemType == ItemType.WEAPON)
        {
            weaponsOwned.Remove(item);
            for (int i = 0; i < weaponsOwned.Count; i++)
            {
                if (weaponsOwned[i].uniqueId == item.uniqueId)
                {
                    weaponsOwned.Remove(weaponsOwned[i]);
                }
            }
        }
        if (item.m_ItemType == ItemType.SHIELD)
        {
            shieldsOwned.Remove(item);
            for (int i = 0; i < shieldsOwned.Count; i++)
            {
                if (shieldsOwned[i].uniqueId == item.uniqueId)
                {
                    shieldsOwned.Remove(shieldsOwned[i]);
                }
            }
        }
        if (item.m_ItemType == ItemType.HELMET)
        {
            helmetsOwned.Remove(item);
            for (int i = 0; i < helmetsOwned.Count; i++)
            {
                if (helmetsOwned[i].uniqueId == item.uniqueId)
                {
                    helmetsOwned.Remove(helmetsOwned[i]);
                }
            }
        }
        if (item.m_ItemType == ItemType.ARMOR)
        {
            armorsOwned.Remove(item);
            for (int i = 0; i < armorsOwned.Count; i++)
            {
                if (armorsOwned[i].uniqueId == item.uniqueId)
                {
                    armorsOwned.Remove(accOwned[i]);
                }
            }
        }
        if (item.m_ItemType == ItemType.ACCESSORY)
        {
            accOwned.Remove(item);
            for (int i = 0; i < accOwned.Count; i++)
            {
                if (accOwned[i].uniqueId == item.uniqueId)
                {
                    accOwned.Remove(accOwned[i]);
                }
            }
        }
        if (GetTotalItemCount() <= m_MaxItemStorage) m_Warned = false;  //reset the flag
        //InventoryPanel.single.RemoveSlot(item.uniqueId);
    }

    public void ClearInventory()
    {
        // weapon/shield/helmet/armor/accessories
        weaponsOwned.Clear();
        shieldsOwned.Clear();
        helmetsOwned.Clear();
        armorsOwned.Clear();
        accOwned.Clear();
        chestsOwned.Clear();
    }

    public void TryAddChest(Chest chest)
    {
        string uniqueKey = chest.chestMapName + Random.Range(1000, 999999);
        chest.uniqueKey = uniqueKey;
        lastChest = chest;
        if (chestsOwned.Count >= 4) return;//cant get another
        chestsOwned.Add(Utilities.DeepClone(chest));
    }

    public void RemoveChest(Chest chest)
    {
        for (int i = 0; i < chestsOwned.Count; i++)
        {
            if (chestsOwned[i].uniqueKey == chest.uniqueKey)
            {
                chestsOwned.Remove(chestsOwned[i]);
            }
        }
    }
}


[System.Serializable]
public class SpritePair
{
    public string spriteName;
    public Sprite sprite;
}

public enum ItemGrade
{
    ORDINARY,
    RARE,
    VERY_RARE,
    EPIC,
    LEGENDARY,
    UI_ONLY  //this is so we can get the gray color
}

public enum ItemType
{
    // weapon/shield/helmet/armor/accessories
    WEAPON,
    SHIELD,
    HELMET,
    ARMOR,
    ACCESSORY
}

[System.Serializable]
public class Item
{
    public string m_ItemBasicTitle;
    public string m_ItemFinalTitle;//with all the adjective + grade stuff
    public int uniqueId;
    [Range(0, 1f)]
    public float m_ChanceDrop;
    public string m_IconName;
    public ItemType m_ItemType;
    [Header("Item Sound used for Weapons")]
    public string m_SkinIfApplicable;
    public string m_AttachmentIfApplicable;

    public ItemGrade itemGrade;
    public Stat baseStat;
    public RollStat rollStat;


    public float fusionExp = 1;
}

[System.Serializable]
public class Chest
{
    public string uniqueKey;
    public ChestGrade chestGrade;
    public string chestMapName;
    public float crystalsMin, crystalsMax = 20;
    public List<Item> possibleRewards;
    public List<Item> possibleGoldRewards;
    public List<Item> possibleDiamondRewards;

    public string rewardTime;
}
