using System.Collections;
using IdleKnightHero.UI;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Purchasing.Purchased;
using UnityEngine;

/// <summary>Preserves legacy Addressable key, IAP IDs and currency/save fields.</summary>
public class CashMallPackage : MonoBehaviour
{
    public static CashMallPackage single => FindFirstObjectByType<CashMallPackage>();
    public StorybookShopUI view;
    bool closing;
    Coroutine completion;
    void Awake(){PurchaserManager.PurchaseStarted+=Started;PurchaserManager.PurchaseSucceeded+=Succeeded;PurchaserManager.PurchaseFailed+=Failed;PurchaserManager.Initialized+=Initialized;}
    void Start(){view.HideReward();if(GameplayCanvas.single.nextIsRanOutOfCherries){SetExtraText("血月结晶不足，请补充后继续。");GameplayCanvas.single.nextIsRanOutOfCherries=false;}if(GameplayCanvas.single.nextCashMallIsDailyCherries){GameplayCanvas.single.nextCashMallIsDailyCherries=false;view.FocusDaily();}}
    void OnDestroy(){PurchaserManager.PurchaseStarted-=Started;PurchaserManager.PurchaseSucceeded-=Succeeded;PurchaserManager.PurchaseFailed-=Failed;PurchaserManager.Initialized-=Initialized;}
    void Initialized(bool ready){if(view.Busy)return;view.SetBusy(false,ready?"价格以商店显示为准":"商店暂不可用，请稍后重试。");}
    void Started(string id){if(IsCurrency(id))view.SetBusy(true,"正在等待商店确认…");}
    void Failed(PurchasedProduct p){if(IsCurrency(p.Id))view.SetBusy(false,"购买未完成，可稍后重试。");}
    static bool IsCurrency(string id)=>id==PurchaserManager.cherries_consumable_80||id==PurchaserManager.cherries_consumable_500;
    void Succeeded(PurchasedProduct p,string source){if(!IsCurrency(p.Id))return;int amount=p.Id==PurchaserManager.cherries_consumable_80?80:500;GameManager.single.premiumCurrency+=amount;GameplayCanvas.single.RefreshCampUI();PresentReward(amount);}
    public void BuyProduct80()=>Buy(PurchaserManager.cherries_consumable_80);
    public void BuyProduct500()=>Buy(PurchaserManager.cherries_consumable_500);
    void Buy(string id){if(closing||view.Busy)return;var p=PurchaserManager.StoreController?.products.WithID(id);if(p==null||!p.availableToPurchase){view.SetBusy(false,"商店暂不可用，请稍后重试。");return;}view.SetBusy(true,"正在等待商店确认…");PurchaserManager.Instance.BuyProductID(id);}
    public void DailyCherriesUI(){if(closing||view.Busy)return;if(StorybookDailyReward.TryClaim())PresentReward(GameManager.single.dailyCherriesAmount);view.Refresh();}
    public void FreeCherriesButton(){GameManager.single.premiumCurrency+=100;PresentReward(100);}
    public void SetExtraText(string text)=>view.status.text=text;
    public void PresentReward(int amount){if(completion!=null)StopCoroutine(completion);completion=StartCoroutine(Complete(amount));}
    IEnumerator Complete(int amount){view.ShowReward(amount);SoundManager.Instance.PlayClip("CHERRY");yield return new WaitForSecondsRealtime(4);CloseButton();}
    public void CloseButton(){if(closing)return;closing=true;view.SetLeaving();Router.single.CallCampFromCashMall();}
}
