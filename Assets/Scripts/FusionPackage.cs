using System.Collections;
using System.Collections.Generic;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class FusionPackage : MonoBehaviour
{

    private static FusionPackage _instance;

    public static FusionPackage single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<FusionPackage>();
            return _instance;
        }
    }

    public Animator statueAnim;


    GameManager gm;
    private void OnEnable()
    {
        gm = GameManager.single;
        Refresh();
        ShowBubble();
    }

    public GameObject bubble1, bubble2;
    public Text bubble1Text, bubble2Text;
    public string[] bigTexts;
    public string[] littleTexts;
    public string[] finishingTexts;

    void ShowBubble()
    {
        bubble1.SetActive(false);
        bubble2.SetActive(false);
        if (Random.Range(0, 2) == 1)
        {
            //little
            bubble1.SetActive(true);
            bubble1Text.text = bigTexts[Random.Range(0, littleTexts.Length - 1)];

        }
        else
        {
            bubble2.SetActive(true);
            bubble2Text.text = bigTexts[Random.Range(0, bigTexts.Length - 1)];
        }
    }


    void FinishingBubble()
    {
        bubble1.SetActive(false);
        bubble2.SetActive(true);
        bubble2Text.text = finishingTexts[Random.Range(0, finishingTexts.Length - 1)]; ;
    }

    public Text crystalCost;
    public MeterMover meter;
    public Text meterPerc;
    public Text fusionLevel;
    public Text crystalsRemain;
    void Refresh()
    {
        fusionLevel.text = gm.fusionLevel.ToString();

        float cost = (int)(gm.fxpMax - gm.fxpNow);
        crystalCost.text = "Will cost: " + cost.ToString();
        crystalsRemain.text = ((int)gm.crystals).ToString();
        float fusionPerc = (GameManager.single.fxpNow / GameManager.single.fxpMax) * 100;
        meterPerc.text = Utilities.ConvertNumber(fusionPerc) + "%";
        if (fusionPerc >= 100) meterPerc.text = "Maxed!";

        Invoke("DelayedMeter", .15f);
    }

    void DelayedMeter()
    {
        meter.SetValue(null, gm.fxpNow / gm.fxpMax);

    }

    public GameObject closeButton;
    public GameObject[] hideThese;
    bool isFeeding = false;
    public void Feed()
    {
        if (isFeeding) return;
        isFeeding = true;
        closeButton.SetActive(false);
        meter.SetValue(null, 1);
        SoundManager.Instance.PlayClip("FUSION_FILL");
        SoundManager.Instance.PlayClip("CLICK");
        meterPerc.text = "Feeding Idol...";
        foreach (GameObject obj in hideThese)
        {
            obj.SetActive(false);
        }
        Invoke("FinishingBubble", .5f);
        statueAnim.CrossFade("Statue_Eat", 0);
        Invoke("FinishFeed", 3);
    }

    void FinishFeed()
    {
        isFeeding = false;
        float cost = (int)(gm.fxpMax - gm.fxpNow);

        gm.crystals -= (int)cost;
        gm.fxpNow = gm.fxpMax - 1;
        gm.IncFusion(1);

        CloseFinishFeeding();
    }

    public CameraEffects myCamEffects;

    public void Close()
    {
        if (isFeeding) return;
        Router.single.CallCampFromFusion();
    }

    public void CloseFinishFeeding()
    {
        if (isFeeding) return;
        Router.single.CallCampFromFusion(true);
    }


}
