using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private static InventoryUI _instance;
    public static InventoryUI single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<InventoryUI>();
            return _instance;
        }
    }
    public Image itemIcon;
    public Text itemTitle, itemTitle_Equipped;
    public Text itemGradeText;
    public Image itemGradeImg;
    public StatBoxUI_Item eDmg, eDef, eHpMax, eAtkSpd, eCrit, eCritDmg, eDefPerc, eHpPerc, eDmgPerc; //e denotes EQUIPPED WEAPON 
    public StatBoxUI_Item nDmg, nDef, nHpMax, nAtkSpd, nCrit, nCritDmg, nDefPerc, nHpPerc, nDmgPerc; //n denotes NEW WEAPON
    public Text[] categoryButtonTexts;
    public GameObject[] categoryButtonNewIcons;
    public GameObject nButtons;
    public GameObject itemPrefab, itemSpacer;
    public GameObject itemContainer;
    public GameObject nothingToSeeHereText;
    public GameObject[] turnOffNothingToSeeHere;
    public GameObject noItemToCompareBox;

    public GameObject[] rebirthObjs;
    public Text rebirthLevelText;

    // public ItemContainerExpansion ice;
    Vector3 startingContainerPos;
    private void OnEnable()
    {
        startingContainerPos = itemContainer.transform.localPosition;
        StartCoroutine(RefreshMenu(ItemType.WEAPON));
        RefreshRebirth();
        GameplayCanvas.single.RefreshFusion();
    }

    void RefreshRebirth()
    {
        bool showing = false;
        if (GameManager.single.rebirthLevel > 0) showing = true;

        foreach (GameObject obj in rebirthObjs)
        {
            obj.SetActive(showing);
        }
        if (showing)
        {
            rebirthLevelText.text = GameManager.single.rebirthLevel.ToString();
        }
    }

    public void CampButton()
    {
        Router.single.StartCoroutine(Router.single.GoToCampFromInventory());
    }

    public ItemType itemTypeHighlighted = ItemType.WEAPON;
    IEnumerator RefreshMenu(ItemType itemTypeChosen)
    {
        itemTypeHighlighted = itemTypeChosen;
        ClearContainer();
        ClearHighlightedItem();
        CategoryPicked(itemTypeChosen);
        yield return new WaitForEndOfFrame();
        //waiting for end of frame allows destroyed objects to clear before trying to assess width
        itemContainer.transform.localPosition = startingContainerPos;
        RectTransform rt = itemContainer.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector3(500, rt.anchoredPosition.y, 0);

        //ice.ExpandContanerWidth();//set us to the right width based upon child items
        ClearEquippedItem();
        RefreshEquippedItem(itemTypeChosen);
        RefreshStats();
        RefreshNewIcons();
    }

    void RefreshNewIcons()
    {
        foreach (GameObject obj in categoryButtonNewIcons)
        {
            obj.SetActive(false);
        }
        InventoryManager im = InventoryManager.single;

        if (im.weaponFlag) categoryButtonNewIcons[0].SetActive(true);
        if (im.shieldFlag) categoryButtonNewIcons[1].SetActive(true);
        if (im.helmetFlag) categoryButtonNewIcons[2].SetActive(true);
        if (im.armorFlag) categoryButtonNewIcons[3].SetActive(true);
        if (im.accFlag) categoryButtonNewIcons[4].SetActive(true);
    }

    public void CategoryPicked(string itemType)
    {
        if (itemType == "WEAPON") StartCoroutine(RefreshMenu(ItemType.WEAPON));
        if (itemType == "ACCESSORY") StartCoroutine(RefreshMenu(ItemType.ACCESSORY));
        if (itemType == "ARMOR") StartCoroutine(RefreshMenu(ItemType.ARMOR));
        if (itemType == "HELMET") StartCoroutine(RefreshMenu(ItemType.HELMET));
        if (itemType == "SHIELD") StartCoroutine(RefreshMenu(ItemType.SHIELD));
    }
    public void CategoryPicked(ItemType itemType)
    {
        InventoryManager im = InventoryManager.single;
        ClearContainer();
        TurnCategoryTextWhite();
        //nothing to see here turn offs
        foreach (GameObject obj in turnOffNothingToSeeHere)
        {
            obj.SetActive(true);
        }
        nothingToSeeHereText.SetActive(false);

        List<Item> itemsFound = new List<Item>();
        bool hasEquipped = false;
        switch (itemType)
        {
            case ItemType.WEAPON:
                if (im.hasWeapon) hasEquipped = true;
                im.weaponFlag = false;
                categoryButtonTexts[0].color = LevelController.Instance.AssetManager._colorsDb[5];
                itemsFound = InventoryManager.single.weaponsOwned;
                RefreshEquippedItem(ItemType.WEAPON);
                break;
            case ItemType.HELMET:
                if (im.hasHelmet) hasEquipped = true;
                im.helmetFlag = false;
                categoryButtonTexts[1].color = LevelController.Instance.AssetManager._colorsDb[5];
                itemsFound = InventoryManager.single.helmetsOwned;
                RefreshEquippedItem(ItemType.HELMET);
                break;
            case ItemType.SHIELD:
                if (im.hasShield) hasEquipped = true;
                im.shieldFlag = false;
                categoryButtonTexts[2].color = LevelController.Instance.AssetManager._colorsDb[5];
                itemsFound = InventoryManager.single.shieldsOwned;
                RefreshEquippedItem(ItemType.SHIELD);
                break;
            case ItemType.ARMOR:
                if (im.hasArmor) hasEquipped = true;
                im.armorFlag = false;
                categoryButtonTexts[3].color = LevelController.Instance.AssetManager._colorsDb[5];
                itemsFound = InventoryManager.single.armorsOwned;
                RefreshEquippedItem(ItemType.ARMOR);
                break;
            case ItemType.ACCESSORY:
                if (im.hasAcc) hasEquipped = true;
                im.accFlag = false;
                categoryButtonTexts[4].color = LevelController.Instance.AssetManager._colorsDb[5];
                itemsFound = InventoryManager.single.accOwned;
                RefreshEquippedItem(ItemType.ACCESSORY);
                break;
        }

        if (itemsFound.Count > 0)
        {
            nothingToSeeHereText.SetActive(false);

            PopulateContainer(itemsFound);
        }
        else
        {
            if (!hasEquipped)
            {
                foreach (GameObject obj in turnOffNothingToSeeHere)
                {
                    obj.SetActive(false);
                }
                nothingToSeeHereText.SetActive(true);
                ToggleNoItemToCompareBox(false);
                ToggleNewTag(false);
            }
        }
    }

    public void TurnCategoryTextWhite()
    {
        foreach (Text t in categoryButtonTexts)
        {
            t.color = Color.white;
        }
    }

    public void PopulateContainer(List<Item> itemsFound)
    {
        //if (itemsFound.Count > 0) nButtons.SetActive(true);
        foreach (Item item in itemsFound)
        {
            //Debug.Log("Make a deep clone of:" + item);
            GameObject.Instantiate(itemPrefab, itemContainer.transform).GetComponent<InventoryMenuItem>().SetMe(item);
        }
        GameObject.Instantiate(itemSpacer, itemContainer.transform).GetComponent<InventoryMenuItem>();

    }


    public void ClearContainer()
    {
        highlightedItem = null;
        foreach (Transform t in itemContainer.transform)
        {
            Destroy(t.gameObject);
        }
        ToggleNoItemToCompareBox(true);
        ToggleNewTag(false);
    }

    public void Dequip(ItemType itemType)
    {
        InventoryManager im = InventoryManager.single;
        switch (itemType)
        {
            case ItemType.WEAPON:
                if (im.hasWeapon)
                {
                    im.hasWeapon = false;
                    im.AddItem(Utilities.DeepClone(im.weaponEq));//move it back to the owned list
                }
                break;
            case ItemType.HELMET:
                if (im.hasHelmet)
                {
                    im.hasHelmet = false;
                    im.AddItem(Utilities.DeepClone(im.helmetEq));//move it back to the owned list
                }
                break;
            case ItemType.SHIELD:
                if (im.hasShield)
                {
                    im.hasShield = false;
                    im.AddItem(Utilities.DeepClone(im.shieldEq));//move it back to the owned list
                }
                break;
            case ItemType.ARMOR:
                if (im.hasArmor)
                {
                    im.hasArmor = false;
                    im.AddItem(Utilities.DeepClone(im.armorEq));//move it back to the owned list
                }
                break;
            case ItemType.ACCESSORY:
                if (im.hasAcc)
                {
                    im.hasAcc = false;
                    im.AddItem(Utilities.DeepClone(im.accEq));//move it back to the owned list
                }
                break;
        }
    }

    public void RefreshEquippedItem(ItemType itemType)
    {
        InventoryManager im = InventoryManager.single;
        bool hasItem = false;
        Item item = null;
        switch (itemType)
        {
            case ItemType.WEAPON:
                if (im.hasWeapon)
                {
                    itemTitle_Equipped.text = im.weaponEq.m_ItemFinalTitle;
                    itemIcon.sprite = InventoryManager.single.GetSpriteIcon(im.weaponEq.m_IconName);
                    itemIcon.color = Color.white;
                    itemIcon.enabled = true;
                    hasItem = true;
                    item = im.weaponEq;
                    OrderEquippedItem(item);

                }
                else
                {
                    itemIcon.color = Color.clear;
                    itemTitle_Equipped.text = "- Empty -";
                }
                break;
            case ItemType.HELMET:
                if (im.hasHelmet)
                {
                    itemTitle_Equipped.text = im.helmetEq.m_ItemFinalTitle;
                    itemIcon.sprite = InventoryManager.single.GetSpriteIcon(im.helmetEq.m_IconName);
                    itemIcon.color = Color.white;
                    itemIcon.enabled = true;
                    hasItem = true;
                    item = im.helmetEq;
                    OrderEquippedItem(item);

                }
                else
                {
                    itemIcon.color = Color.clear;
                    itemTitle_Equipped.text = "Empty";
                }
                break;
            case ItemType.SHIELD:
                if (im.hasShield)
                {
                    itemTitle_Equipped.text = im.shieldEq.m_ItemFinalTitle;
                    itemIcon.sprite = InventoryManager.single.GetSpriteIcon(im.shieldEq.m_IconName);
                    itemIcon.color = Color.white;
                    itemIcon.enabled = true;
                    hasItem = true;
                    item = im.shieldEq;
                    OrderEquippedItem(item);

                }
                else
                {
                    itemIcon.color = Color.clear;
                    itemTitle_Equipped.text = "Empty";
                }
                break;
            case ItemType.ARMOR:
                if (im.hasArmor)
                {
                    itemTitle_Equipped.text = im.armorEq.m_ItemFinalTitle;
                    itemIcon.sprite = InventoryManager.single.GetSpriteIcon(im.armorEq.m_IconName);
                    itemIcon.color = Color.white;
                    itemIcon.enabled = true;
                    hasItem = true;
                    item = im.armorEq;
                    OrderEquippedItem(item);

                }
                else
                {
                    itemIcon.color = Color.clear;
                    itemTitle_Equipped.text = "Empty";
                }
                break;
            case ItemType.ACCESSORY:
                if (im.hasAcc)
                {
                    itemTitle_Equipped.text = im.accEq.m_ItemFinalTitle;
                    itemIcon.sprite = InventoryManager.single.GetSpriteIcon(im.accEq.m_IconName);
                    itemIcon.color = Color.white;
                    itemIcon.enabled = true;
                    hasItem = true;
                    item = im.accEq;
                    OrderEquippedItem(item);

                }
                else
                {
                    itemIcon.color = Color.clear;
                    itemTitle_Equipped.text = "Empty";
                }
                break;
        }

        if (item != null)
        {
            //Set the stats
            //DAMAGE
            if (item.baseStat.dmg > 0)
            {
                eDmg.gameObject.SetActive(true);
                eDmg.SetMe(item);
            }
            else
            {
                eDmg.gameObject.SetActive(false);
            }
            //DEFENSE
            if (item.baseStat.def > 0)
            {
                eDef.gameObject.SetActive(true);
                eDef.SetMe(item);
            }
            else
            {
                eDef.gameObject.SetActive(false);
            }
            //ATTACK SPEED
            if (item.baseStat.atkSpd > 0)
            {
                eAtkSpd.gameObject.SetActive(true);
                eAtkSpd.SetMe(item);
            }
            else
            {
                eAtkSpd.gameObject.SetActive(false);
            }
            //CRIT
            if (item.baseStat.crit > 0)
            {
                eCrit.gameObject.SetActive(true);
                eCrit.SetMe(item);
            }
            else
            {
                eCrit.gameObject.SetActive(false);
            }
            //HP MAX
            if (item.baseStat.hpMax > 0)
            {
                eHpMax.gameObject.SetActive(true);
                eHpMax.SetMe(item);
            }
            else
            {
                eHpMax.gameObject.SetActive(false);
            }
            //CRIT DMG
            if (item.baseStat.critDmg > 0)
            {
                eCritDmg.gameObject.SetActive(true);
                eCritDmg.SetMe(item);
            }
            else
            {
                eCritDmg.gameObject.SetActive(false);
            }
            //DEF PERC
            if (item.baseStat.defPerc > 0)
            {
                eDefPerc.gameObject.SetActive(true);
                eDefPerc.SetMe(item);
            }
            else
            {
                eDefPerc.gameObject.SetActive(false);
            }
            //HP PERC
            if (item.baseStat.hpPerc > 0)
            {
                eHpPerc.gameObject.SetActive(true);
                eHpPerc.SetMe(item);
            }
            else
            {
                eHpPerc.gameObject.SetActive(false);
            }
            //DAMAGE PERC
            if (item.baseStat.dmgPerc > 0)
            {
                eDmgPerc.gameObject.SetActive(true);
                eDmgPerc.SetMe(item);
            }
            else
            {
                eDmgPerc.gameObject.SetActive(false);
            }
        }
    }

    public void ClearHighlightedItem()
    {
        highlightedItem = null;
        itemTitle.text = "";
        itemGradeText.text = "";
        itemGradeImg.enabled = false;
        nDmg.gameObject.SetActive(false);
        nCrit.gameObject.SetActive(false);
        nCritDmg.gameObject.SetActive(false);
        nDef.gameObject.SetActive(false);
        nDefPerc.gameObject.SetActive(false);
        nHpPerc.gameObject.SetActive(false);
        nDmgPerc.gameObject.SetActive(false);
        nHpMax.gameObject.SetActive(false);
        nAtkSpd.gameObject.SetActive(false);
        nButtons.gameObject.SetActive(false);
    }

    public void ClearEquippedItem()
    {
        itemTitle_Equipped.text = "";
        //itemGradeText.text = "";
        itemIcon.enabled = false;
        eDmg.gameObject.SetActive(false);
        eCrit.gameObject.SetActive(false);
        eCritDmg.gameObject.SetActive(false);
        eDef.gameObject.SetActive(false);
        eDefPerc.gameObject.SetActive(false);
        eHpPerc.gameObject.SetActive(false);
        eDmgPerc.gameObject.SetActive(false);
        eHpMax.gameObject.SetActive(false);
        eAtkSpd.gameObject.SetActive(false);

    }

    public Item highlightedItem;
    public void SetHighlightedItem(Item item)
    {
        nButtons.SetActive(true);
        ToggleNoItemToCompareBox(false);
        ToggleNewTag(true);
        SetHighlightedGrade(item.itemGrade);
        highlightedItem = item;//in case we need this ref later
        InventoryManager im = InventoryManager.single;
        itemTitle.text = item.m_ItemFinalTitle;

        //Set the stats
        //DAMAGE
        if (item.baseStat.dmg > 0)
        {
            nDmg.gameObject.SetActive(true);
            nDmg.SetMe(item);
            nDmg.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));
        }
        else
        {
            nDmg.gameObject.SetActive(false);
        }
        //DEFENSE
        if (item.baseStat.def > 0)
        {
            nDef.gameObject.SetActive(true);
            nDef.SetMe(item);
            nDef.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));
        }
        else
        {
            nDef.gameObject.SetActive(false);
        }
        //ATTACK SPEED
        if (item.baseStat.atkSpd > 0)
        {
            nAtkSpd.gameObject.SetActive(true);
            nAtkSpd.SetMe(item);
            nAtkSpd.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nAtkSpd.gameObject.SetActive(false);
        }
        //CRIT
        if (item.baseStat.crit > 0)
        {
            nCrit.gameObject.SetActive(true);
            nCrit.SetMe(item);
            nCrit.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nCrit.gameObject.SetActive(false);
        }
        //HP MAX
        if (item.baseStat.hpMax > 0)
        {
            nHpMax.gameObject.SetActive(true);
            nHpMax.SetMe(item);
            nHpMax.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nHpMax.gameObject.SetActive(false);
        }
        //CRIT DMG
        if (item.baseStat.critDmg > 0)
        {
            nCritDmg.gameObject.SetActive(true);
            nCritDmg.SetMe(item);
            nCritDmg.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nCritDmg.gameObject.SetActive(false);
        }
        //DEF PERC
        if (item.baseStat.defPerc > 0)
        {
            nDefPerc.gameObject.SetActive(true);
            nDefPerc.SetMe(item);
            nDefPerc.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nDefPerc.gameObject.SetActive(false);
        }
        //HP PERC
        if (item.baseStat.hpPerc > 0)
        {
            nHpPerc.gameObject.SetActive(true);
            nHpPerc.SetMe(item);
            nHpPerc.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nHpPerc.gameObject.SetActive(false);
        }
        //DAMAGE PERC
        if (item.baseStat.dmgPerc > 0)
        {
            nDmgPerc.gameObject.SetActive(true);
            nDmgPerc.SetMe(item);
            nDmgPerc.ColorAndArrow(item, GetEquippedItem(item.m_ItemType));

        }
        else
        {
            nDmgPerc.gameObject.SetActive(false);
        }

        OrderHighlightedItem(item);

    }

    public void OrderHighlightedItem(Item item)
    {
        if (item.m_ItemType == ItemType.WEAPON)
        {
            nDmg.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.ARMOR)
        {
            nDef.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.ACCESSORY)
        {
            nHpMax.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.SHIELD)
        {
            nDef.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.HELMET)
        {
            nDef.gameObject.transform.SetAsFirstSibling();
        }
    }

    public void OrderEquippedItem(Item item)
    {
        if (item.m_ItemType == ItemType.WEAPON)
        {
            eDmg.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.ARMOR)
        {
            eDef.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.ACCESSORY)
        {
            eHpMax.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.SHIELD)
        {
            eDef.gameObject.transform.SetAsFirstSibling();
        }
        if (item.m_ItemType == ItemType.HELMET)
        {
            eDef.gameObject.transform.SetAsFirstSibling();
        }
    }
    public Item GetEquippedItem(ItemType itemType)
    {
        Item item = null;
        switch (itemType)
        {
            case ItemType.WEAPON:
                if (InventoryManager.single.hasWeapon) item = InventoryManager.single.weaponEq;
                break;
            case ItemType.HELMET:
                if (InventoryManager.single.hasHelmet) item = InventoryManager.single.helmetEq;
                break;
            case ItemType.SHIELD:
                if (InventoryManager.single.hasShield) item = InventoryManager.single.shieldEq;
                break;
            case ItemType.ARMOR:
                if (InventoryManager.single.hasArmor) item = InventoryManager.single.armorEq;
                break;
            case ItemType.ACCESSORY:
                if (InventoryManager.single.hasAcc) item = InventoryManager.single.accEq;
                break;
        }
        return item;
    }

    public void SetHighlightedGrade(ItemGrade itemGrade)
    {
        itemGradeText.text = itemGrade.ToString().ToUpper();
        itemGradeImg.enabled = true;

        itemGradeImg.sprite = InventoryManager.single.GetGradeIcon(itemGrade);
        itemGradeImg.color = InventoryManager.single.GetGradeColor(itemGrade);
        itemGradeText.color = InventoryManager.single.GetGradeColor(itemGrade);
    }

    public void EquipButton()
    {
        //start with a dequip
        Dequip(highlightedItem.m_ItemType);

        InventoryManager im = InventoryManager.single;
        //to use set equipped item, you first have to set the variables, THEN run it

        //refresh the menu now
        ClearEquippedItem();
        ItemType lastItemHighlighted = highlightedItem.m_ItemType;
        EquipItem(highlightedItem);
        RefreshEquippedItem(lastItemHighlighted);
        ClearContainer();
        CategoryPicked(lastItemHighlighted);
        RefreshStats();
    }

    public Text statPoints;
    public StatBoxUI dmgBox, defBox, atkSpdBox, critBox, critDmgBox, hpBox;
    public void RefreshStats()
    {
        GameManager.single.CalcPlayer();//first get player up to speed! then update the numbers
        statPoints.text = GameManager.single.statPoints.ToString();
        dmgBox.RefreshMe();
        atkSpdBox.RefreshMe();
        defBox.RefreshMe();
        critBox.RefreshMe();
        critDmgBox.RefreshMe();
        hpBox.RefreshMe();

        //refresh all the stat boxes
    }

    void EquipItem(Item item)
    {
        InventoryManager im = InventoryManager.single;
        SoundManager.Instance.PlayClip("EQUIP_ITEM");
        switch (item.m_ItemType)
        {
            case ItemType.WEAPON:
                im.hasWeapon = true;
                im.weaponEq = Utilities.DeepClone(item);
                break;
            case ItemType.HELMET:
                im.hasHelmet = true;
                im.helmetEq = Utilities.DeepClone(item);
                break;
            case ItemType.SHIELD:
                im.hasShield = true;
                im.shieldEq = Utilities.DeepClone(item);
                break;
            case ItemType.ARMOR:
                im.hasArmor = true;
                im.armorEq = Utilities.DeepClone(item);
                break;
            case ItemType.ACCESSORY:
                im.hasAcc = true;
                im.accEq = Utilities.DeepClone(item);
                break;
        }
        im.RemoveItem(item);
        Player.single.SetSkinsAndAttachments();
        OrderEquippedItem(item);

        StartCoroutine(RefreshMenu(item.m_ItemType));

    }

    public void FuseButton(bool skipRefresh = false)
    {

        Debug.Log("Fuse:" + highlightedItem);
        // SoundManager.Instance.PlayClip("FUSION_FILL");
        GameManager.single.IncFusion(highlightedItem.fusionExp);
        ItemType itemTypeCurrent = highlightedItem.m_ItemType;
        InventoryManager.single.RemoveItem(highlightedItem);
        if (!skipRefresh)
            StartCoroutine(RefreshMenu(itemTypeCurrent));

    }

    //used when doing sell all
    public void CollectFusionThisItem(Item item)
    {
        Debug.Log("Fuse:" + item);
        GameManager.single.IncFusion(item.fusionExp);
        //InventoryManager.single.RemoveItem(highlightedItem);

    }

    public GameObject sellAllButton;
    public void SellAllClicked()
    {


        if (itemTypeHighlighted == ItemType.WEAPON)
        {
            foreach (Item i in InventoryManager.single.weaponsOwned)
            {
                CollectFusionThisItem(i);
            }
            InventoryManager.single.weaponsOwned.Clear();//remove them all at end
        }

        if (itemTypeHighlighted == ItemType.ARMOR)
        {
            foreach (Item i in InventoryManager.single.armorsOwned)
            {
                CollectFusionThisItem(i);
            }
            InventoryManager.single.armorsOwned.Clear();//remove them all at end
        }

        if (itemTypeHighlighted == ItemType.SHIELD)
        {
            foreach (Item i in InventoryManager.single.shieldsOwned)
            {
                CollectFusionThisItem(i);
            }
            InventoryManager.single.shieldsOwned.Clear();//remove them all at end
        }

        if (itemTypeHighlighted == ItemType.ACCESSORY)
        {
            foreach (Item i in InventoryManager.single.accOwned)
            {
                CollectFusionThisItem(i);
            }
            InventoryManager.single.accOwned.Clear();//remove them all at end
        }

        if (itemTypeHighlighted == ItemType.HELMET)
        {
            foreach (Item i in InventoryManager.single.helmetsOwned)
            {
                CollectFusionThisItem(i);
            }
            InventoryManager.single.helmetsOwned.Clear();//remove them all at end
        }

        StartCoroutine(RefreshMenu(itemTypeHighlighted));

    }

    //public MeterMover fusionMeter;
    //public Text fusionLevelText;
    //public Text fusionPercText;
    //public void RefreshFusion()
    //{
    //    GameplayCanvas.single.RefreshFusion();
    //    //float fusionPerc = (GameManager.single.fxpNow / GameManager.single.fxpMax) * 100;
    //    //fusionPercText.text = Utilities.ConvertNumber(fusionPerc) + "%";
    //    //if (fusionPerc >= 100) fusionPercText.text = "Maxed!";
    //    //fusionMeter.SetValue(null, GameManager.single.fxpNow / GameManager.single.fxpMax);
    //    //fusionLevelText.text = "Level " + GameManager.single.fusionLevel.ToString();
    //    //SetMarker(p.ab.currentStat.hpNow / p.ab.currentStat.hpMax);
    //}

    public GameObject newTag;
    public void ToggleNewTag(bool b)
    {
        newTag.SetActive(b);
    }

    public void ToggleNoItemToCompareBox(bool b)
    {
        noItemToCompareBox.SetActive(b);
    }

}
