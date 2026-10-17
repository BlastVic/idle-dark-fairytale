using System.Globalization;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data and interaction bindings for the authored rebirth prefab.</summary>
public sealed class StorybookRebirthUI : MonoBehaviour
{
    public Button back,rebirth,adventure,confirm,cancel;
    public Text number,requirement,currentHeading,nextHeading,rebirthCaption,confirmBody;
    public Text[] currentValues,nextValues;
    public Image readySeal;
    public GameObject confirmation;
    RebirthPackage owner;
    bool bound;
    int lastLevel=-1,lastRebirth=-1;float lastXp,lastCrystals;bool lastBusy;
    void Awake()
    {
        back.onClick.AddListener(Leave);adventure.onClick.AddListener(Leave);
        rebirth.onClick.AddListener(()=>owner.ToggleAreYouSure(true));
        cancel.onClick.AddListener(()=>owner.ToggleAreYouSure(false));
        confirm.onClick.AddListener(()=>owner.RebirthButton());
    }
    public void Bind(RebirthPackage value){owner=value;bound=true;lastLevel=-1;confirmation.SetActive(false);Refresh();}
    void Update(){if(!bound)return;Refresh();if(Input.GetKeyDown(KeyCode.Escape)){if(confirmation.activeSelf)owner.ToggleAreYouSure(false);else Leave();}}
    void Leave(){if(owner)owner.CloseButton();}
    static string Number(float value)=>value.ToString("#,0.##",CultureInfo.InvariantCulture);
    public void Refresh()
    {
        if(!owner||!GameManager.single||!LevelController.Instance)return;
        var gm=GameManager.single;
        if(lastLevel==gm.playerLevel&&lastRebirth==gm.rebirthLevel&&lastXp==gm.stackedXp&&lastCrystals==gm.stackedCrystals&&lastBusy==owner.Busy)return;
        lastLevel=gm.playerLevel;lastRebirth=gm.rebirthLevel;lastXp=gm.stackedXp;lastCrystals=gm.stackedCrystals;lastBusy=owner.Busy;
        int index=gm.rebirthLevel+1;bool max=index>=LevelDataDescriptions.RebirthLevelDb.Length;
        bool ready=owner.CanRebirth;number.text=max?"转生已达上限":"第 "+index+" 次转生";
        requirement.text=max?"已完成全部转生":"Lv. "+gm.playerLevel+" · "+(ready?"已满足条件":"需达到 Lv. "+LevelDataDescriptions.RebirthLevelDb[index].minLevel);
        requirement.fontSize=max||ready?33:28;
        readySeal.enabled=ready;rebirth.interactable=confirm.interactable=ready;
        back.interactable=adventure.interactable=cancel.interactable=!owner.Busy;
        rebirthCaption.text=max?"已达上限":ready?"转 生":"尚未满足条件";
        currentHeading.text=ready?"现在转生":"达标后转生";
        nextHeading.text="升至 Lv. "+(gm.playerLevel+1);
        if(max){foreach(var t in currentValues)t.text="—";foreach(var t in nextValues)t.text="—";return;}
        var level=LevelDataDescriptions.RebirthLevelDb[index];
        for(int column=0;column<2;column++){
            var texts=column==0?currentValues:nextValues;int playerLevel=Mathf.Max(gm.playerLevel+column,(int)level.minLevel);
            // Below the gate show the attainable threshold reward, never negative extrapolation.
            texts[0].text="+"+level.statPointsGiven;texts[1].text="+"+level.skillPointsGiven;
            texts[2].text="+"+Number(LevelController.Instance.GetRebirthXpByLevel(playerLevel,index,gm.stackedXp))+"%";
            texts[3].text=Number(LevelController.Instance.GetRebirthCrystalsByLevel(playerLevel,index,gm.stackedCrystals));
        }
        if(!ready)nextHeading.text="达到 Lv. "+level.minLevel;
        confirmBody.text="等级将回到 Lv. 1\n装备、技能、属性、融合与关卡进度重置\n\n获得属性点 +"+level.statPointsGiven+"　技能点 +"+level.skillPointsGiven+"\n经验加成 "+currentValues[2].text+"　起始水晶 "+currentValues[3].text;
    }
}
