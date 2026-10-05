using System.Collections;
using System.Collections.Generic;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class ChestButton : MonoBehaviour
{
    Timer myTimer;
    public Image chestImg;
    public Text timerText;
    public Chest myChest;

    public void SetMe(Chest _chest)
    {
        myChest = _chest;

        switch (myChest.chestGrade)
        {
            case ChestGrade.NORMAL:
                chestImg.sprite = InventoryManager.single.GetSpriteIcon("NORMAL_CHEST");
                break;
            case ChestGrade.GOLD:
                chestImg.sprite = InventoryManager.single.GetSpriteIcon("GOLD_CHEST");
                break;
            case ChestGrade.DIAMOND:
                chestImg.sprite = InventoryManager.single.GetSpriteIcon("DIAMOND_CHEST");
                break;
        }

        myTimer = gameObject.AddComponent<Timer>();


        //string back to a long
        long restoreDateLong = System.Convert.ToInt64(myChest.rewardTime);
        //long to date

        System.DateTime rewardTime = System.DateTime.FromBinary(restoreDateLong);

        myTimer.InitializeTimerByDT(rewardTime, timerText, Color.yellow);
    }

    public void Clicked()
    {
        //if (myTimer.IsReady())
        //{
        //    Debug.Log("Chest Button Clicked");
        //}
        //else
        //{
        //    Debug.Log("Chest button clicked not ready tho");
        //}
        SoundManager.Instance.PlayClip("CLICK");
        Router.single.CallChestFromCamp(myChest);
    }
}



public enum ChestGrade
{
    NORMAL,
    GOLD,
    DIAMOND,
    TURD
}


