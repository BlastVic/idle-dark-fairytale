using System;
using Assets.Scripts.Extentions;
using Assets.Scripts.Services.Purchasing;
using Assets.Scripts.Services.Purchasing.Purchased;

public class GameController : MonoSingleton<GameController>
{
    protected override void Awake()
    {
        base.Awake();
        PurchaserManager.Initialized += PurchaserOnInitialized;
        PurchaserManager.PurchaseStarted += OnPurchaseStarterHandle;
        PurchaserManager.PurchaseSucceeded += OnPurchaseSucceededHandle;
        PurchaserManager.PurchaseFailed += OnPurchaseFailedHandle;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PurchaserManager.Initialized -= PurchaserOnInitialized;
        PurchaserManager.PurchaseStarted -= OnPurchaseStarterHandle;
        PurchaserManager.PurchaseSucceeded -= OnPurchaseSucceededHandle;
        PurchaserManager.PurchaseFailed -= OnPurchaseFailedHandle;
    }

    private void PurchaserOnInitialized(Boolean flag)
    {

    }

    private void OnPurchaseStarterHandle(String productId)
    {
        TransitionCanvas.single.ToBlack();
    }

    private void OnPurchaseSucceededHandle(PurchasedProduct product, String source)
    {
        TransitionCanvas.single.ToClear();
    }

    private void OnPurchaseFailedHandle(PurchasedProduct product)
    {
        TransitionCanvas.single.ToClear();
    }
}
