using UnityEngine;
using UnityEngine.UI;
namespace RedHoodPrototype {
 public sealed class RedHoodPreviewHud:MonoBehaviour {
  public RectTransform previewArea;
  public GameObject animationPanel;
  public CanvasGroup catalogGroup;
  public bool CatalogOpen {get;private set;}
  public Button selector,reset,pause,replay,previousFrame,nextFrame;
  public Button[] animationButtons;
  public Text title,status,timeLabel,speedLabel,zoomLabel,pauseLabel,selectorLabel;
  public Slider timeSlider,speedSlider,zoomSlider;
  public Toggle loopToggle,cycleToggle,attackToggle,returnToggle;
  public InputField search;
  public Text empty;
  RedHoodAnimationPreview model;
  public void Bind(RedHoodAnimationPreview m){
   model=m;animationPanel.SetActive(false);
   selector.onClick.AddListener(()=>SetCatalog(!CatalogOpen));
   reset.onClick.AddListener(model.ResetView);pause.onClick.AddListener(()=>model.SetPaused(!model.paused));replay.onClick.AddListener(model.Replay);
   previousFrame.onClick.AddListener(()=>model.Step(-1));nextFrame.onClick.AddListener(()=>model.Step(1));
   timeSlider.onValueChanged.AddListener(model.Seek);speedSlider.onValueChanged.AddListener(v=>model.speed=v);zoomSlider.onValueChanged.AddListener(v=>model.zoom=v);
   loopToggle.onValueChanged.AddListener(model.SetLoop);cycleToggle.onValueChanged.AddListener(model.SetCycle);attackToggle.onValueChanged.AddListener(model.SetAutoAttack);returnToggle.onValueChanged.AddListener(v=>model.returnToIdle=v);
   foreach(var b in animationButtons){string name=b.name;b.onClick.AddListener(()=>{model.Select(name);SetCatalog(false);});}
   search.onValueChanged.AddListener(Filter);Filter("");Refresh();
  }
  public void SetCatalog(bool open){CatalogOpen=open;catalogGroup.blocksRaycasts=open;catalogGroup.interactable=open;if(open)animationPanel.SetActive(true);}
  public void Filter(string query){int count=0;foreach(var b in animationButtons){bool visible=string.IsNullOrWhiteSpace(query)||b.name.ToLowerInvariant().Contains(query.Trim().ToLowerInvariant());b.gameObject.SetActive(visible);if(visible)count++;}empty.gameObject.SetActive(count==0);}
  void LateUpdate(){
   if(catalogGroup){catalogGroup.alpha=Mathf.MoveTowards(catalogGroup.alpha,CatalogOpen?1:0,Time.unscaledDeltaTime/ .12f);if(!CatalogOpen&&catalogGroup.alpha==0)animationPanel.SetActive(false);}
   if(model)Refresh();
  }
  void Refresh(){
   selectorLabel.text=model.Selected+"     /     "+(CatalogOpen?"收起 −":"全部动画 +");
   status.text="正在播放  "+model.Playing+"    命中 "+model.HitCount+"    完成 "+model.CompleteCount;
   timeLabel.text=model.Timeline.ToString("0.00")+" / "+model.Duration.ToString("0.00")+" s";
   timeSlider.maxValue=Mathf.Max(.01f,model.Duration);timeSlider.SetValueWithoutNotify(model.Timeline);
   speedSlider.SetValueWithoutNotify(model.speed);zoomSlider.SetValueWithoutNotify(model.zoom);
   speedLabel.text=model.speed.ToString("0.00")+"×"+(model.Playing=="Death1"?" · 失败1.2×":"");zoomLabel.text=model.zoom.ToString("0.00")+"×";
   loopToggle.SetIsOnWithoutNotify(model.Current!=null && model.Current.Loop);cycleToggle.SetIsOnWithoutNotify(model.autoCycle);attackToggle.SetIsOnWithoutNotify(model.autoAttack);returnToggle.SetIsOnWithoutNotify(model.returnToIdle);
   pauseLabel.text=model.paused?"继续":"暂停";
   foreach(var b in animationButtons)b.GetComponent<Image>().color=b.name==model.Selected?new Color(.38f,.085f,.085f):new Color(.10f,.12f,.14f);
   previousFrame.interactable=model.Timeline>.0001f;nextFrame.interactable=model.Timeline<model.Duration-.0001f;
  }
 }
}
