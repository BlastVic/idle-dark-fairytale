using UnityEngine;
using UnityEngine.UI;
public sealed class StorybookDailyReminder : MonoBehaviour
{
    public Button close,open;
    public Text amount;
    public CanvasGroup group;
    bool navigating;
    void Awake(){close.onClick.AddListener(Hide);open.onClick.AddListener(GoToShop);}
    public void Show(){navigating=false;amount.text=GameManager.single.dailyCherriesAmount.ToString();gameObject.SetActive(true);group.alpha=1;group.blocksRaycasts=group.interactable=true;open.interactable=true;}
    void Update(){if(Input.GetKeyDown(KeyCode.Escape))Hide();}
    public void Hide(){group.alpha=0;group.blocksRaycasts=group.interactable=false;gameObject.SetActive(false);}
    void GoToShop(){if(navigating)return;navigating=true;open.interactable=false;Hide();GameplayCanvas.single.nextCashMallIsDailyCherries=true;Router.single.DailyCherriesButton();}
}
