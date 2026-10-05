using System;
using System.Collections;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Purchasing.Purchased;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class CashMallPackage : MonoBehaviour
{
    private static CashMallPackage _instance;
    public static CashMallPackage single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<CashMallPackage>();
            return _instance;
        }
    }

    [SerializeField]
    private Text buttonTextLable_80;
    [SerializeField]
    private Text buttonTextLable_500;

    [SerializeField]
    private GameObject[] TurnOffForPurchaseConfirmation;
    [SerializeField]
    private GameObject TurnOnForPurchaseConfirmation;
    private void Awake()
    {
        PurchaserManager.PurchaseSucceeded += OnPurchaseSucceededHandle;
        SetTextProduct();
    }

    private void OnEnable()
    {
        if (GameplayCanvas.single.nextIsRanOutOfCherries)
        {
            SetExtraText("Whoa you ran out of cherries! Get more!");
            GameplayCanvas.single.nextIsRanOutOfCherries = false;//reset flag
        }
        else
        {
            SetExtraText("Do a good deed and feed a starving Game Developer!");
        }
        if (GameplayCanvas.single.nextCashMallIsDailyCherries)
        {
            DailyCherriesUI();
            GameplayCanvas.single.nextCashMallIsDailyCherries = false;//reset
        }


    }

    private void OnDestroy()
    {
        PurchaserManager.PurchaseSucceeded -= OnPurchaseSucceededHandle;
    }

    private void SetTextProduct()
    {
        String text_80 = buttonTextLable_80.text;
        String text_500 = buttonTextLable_500.text;
        if (PurchaserManager.Instance.IsInitialized)
        {
            var product_80 = PurchaserManager.StoreController.products.WithID(PurchaserManager.cherries_consumable_80);
            var product_500 = PurchaserManager.StoreController.products.WithID(PurchaserManager.cherries_consumable_500);
            buttonTextLable_80.text = String.Format(text_80, product_80.metadata.localizedPriceString);
            buttonTextLable_500.text = String.Format(text_500, product_500.metadata.localizedPriceString);
        }
        else
        {
            buttonTextLable_80.text = String.Format(text_80, "$0.99");
            buttonTextLable_500.text = String.Format(text_500, "$4.99");
        }

    }

    public void DailyCherriesUI()
    {
        StartCoroutine(ShowPurchaseCompleteGraphics(GameManager.single.dailyCherriesAmount)); //use this once purchase is confirmed through IOS and player can see "receive button and Congratulations"

    }

    public void FreeCherriesButton()
    {
        SoundManager.Instance.PlayClip("FUSION_REWARD");
        GameManager.single.premiumCurrency += 100;
        StartCoroutine(ShowPurchaseCompleteGraphics(100)); //use this once purchase is confirmed through IOS and player can see "receive button and Congratulations"
    }

    public GameObject[] buttons;//0 tap open, 1 regular pay for, 2 watch vid
    bool closeClicked = false;
    public void CloseButton()
    {
        if (closeClicked) return;
        closeClicked = true;
        Router.single.CallCampFromCashMall();
    }

    public void SetExtraText(string s)
    {
        TurnOffForPurchaseConfirmation[1].GetComponent<Text>().text = s;
    }

    private IEnumerator ShowPurchaseCompleteGraphics(int amount)
    {
        foreach (GameObject obj in TurnOffForPurchaseConfirmation)
        {
            obj.SetActive(false);
        }
        TurnOnForPurchaseConfirmation.SetActive(true);
        cherriesLabel.text = amount.ToString();
        LevelController.Instance.AssetManager.SpawnGeneralObj("CherriesChestDropIn");//drop the cherries chest and open it
        yield return new WaitForSeconds(2);
        SoundManager.Instance.PlayClip("CHERRY");

        yield return new WaitForSeconds(2f);
        CloseButton();
    }

    [SerializeField]
    private Text cherriesLabel;



    public void BuyProduct80()
    {
        PurchaserManager.Instance.BuyProductID(PurchaserManager.cherries_consumable_80);
    }

    public void BuyProduct500()
    {
        PurchaserManager.Instance.BuyProductID(PurchaserManager.cherries_consumable_500);
    }

    private void OnPurchaseSucceededHandle(PurchasedProduct product, String source)
    {
        
        if (product.Id.Equals(PurchaserManager.cherries_consumable_80))
        {
            GameManager.single.premiumCurrency += 80;
            GameplayCanvas.single.RefreshCampUI();
            StartCoroutine(ShowPurchaseCompleteGraphics(80)); //use this once purchase is confirmed through IOS and player can see "receive button and Congratulations"

        }
        else if (product.Id.Equals(PurchaserManager.cherries_consumable_500))
        {
            GameManager.single.premiumCurrency += 500;
            GameplayCanvas.single.RefreshCampUI();
            StartCoroutine(ShowPurchaseCompleteGraphics(500)); //use this once purchase is confirmed through IOS and player can see "receive button and Congratulations"
        }
    }

    
}
