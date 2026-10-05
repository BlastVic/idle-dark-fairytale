using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryMenuItem : MonoBehaviour
{
    public Item myItem;
    //public Text itemTitle;
    public Image itemIcon;
    public void SetMe(Item item)
    {
        myItem = item;
        //itemTitle.text = item.m_ItemFinalTitle;
        itemIcon.sprite = InventoryManager.single.GetSpriteIcon(item.m_IconName);
    }


    public void SetMeAsChosen()
    {
        if (InventoryUI.single.highlightedItem == myItem) return;//we are already chosen item
        Debug.Log("SetMeAsChosen");
        InventoryUI.single.SetHighlightedItem(myItem);

    }
}
