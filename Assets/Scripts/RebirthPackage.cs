using System.Collections;
using System.Collections.Generic;
using Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

public class RebirthPackage : MonoBehaviour
{
    private static RebirthPackage _instance;

    public static RebirthPackage single
    {
        get
        {
            //If _instance is null then we find it from the scene 
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<RebirthPackage>();
            return _instance;
        }
    }

    public GameObject cutsceneObj;
    public Dummy dummy;
    public Text oneLiner;
    public GameObject rebirthButton;
    public GameObject closeButton;
    public Text levelText, levelNextText;
    public Text statPtsText, skillText, xpText, crystalsText;
    public Text statPtsNextText, skillNextText, xpNextText, crystalsNextText;
    public Text statPtsCurrentText, skillCurrentText, xpCurrentText, crystalsCurrentText;

    public GameObject rebirthCanvas;
    GameManager gm;
    private void OnEnable()
    {
        gm = GameManager.single;

        Refresh();
    }

    void Refresh()
    {
        //funny text

        //get the level info
        RebirthLevel currentRebirthLvl = LevelDataDescriptions.RebirthLevelDb[gm.rebirthLevel + 1];
        RebirthLevel nowRebirthLvl = LevelDataDescriptions.RebirthLevelDb[gm.rebirthLevel];


        //stack the xp
        float stackedCurrentXp = LevelController.Instance.GetRebirthXpByLevel(gm.playerLevel, gm.rebirthLevel + 1, gm.stackedXp);// currentRebirthLvl.expGiven;
        float stackedNextXp = LevelController.Instance.GetRebirthXpByLevel(gm.playerLevel + 1, gm.rebirthLevel + 1, gm.stackedXp);

        //stack the xp
        float stackedCurrentCrystals = LevelController.Instance.GetRebirthCrystalsByLevel(gm.playerLevel, gm.rebirthLevel + 1, gm.stackedCrystals);// currentRebirthLvl.expGiven;
        float stackedNextCrystals = LevelController.Instance.GetRebirthCrystalsByLevel(gm.playerLevel + 1, gm.rebirthLevel + 1, gm.stackedCrystals);


        //apply it to the UI
        levelText.text = "Lv " + (gm.playerLevel);
        statPtsCurrentText.text = "+" + currentRebirthLvl.statPointsGiven + "";
        skillCurrentText.text = "+" + currentRebirthLvl.skillPointsGiven + "";
        crystalsCurrentText.text = stackedCurrentCrystals + "";
        xpCurrentText.text = stackedCurrentXp + "%";

        levelNextText.text = "Lv " + (gm.playerLevel + 1);
        statPtsNextText.text = "+" + currentRebirthLvl.statPointsGiven + "";
        skillNextText.text = "+" + currentRebirthLvl.skillPointsGiven + "";
        crystalsNextText.text = stackedNextCrystals + "";
        xpNextText.text = stackedNextXp + "%";

        statPtsText.text = "+" + nowRebirthLvl.statPointsGiven + "";
        skillText.text = "+" + nowRebirthLvl.skillPointsGiven + "";
        crystalsText.text = gm.stackedCrystals + "";
        xpText.text = gm.stackedXp + "%";

        //reset this to "on"
        rebirthButton.SetActive(true);
        closeButton.SetActive(true);

        //oneLiner.text = StatManager.single.rebirthOneLiners[Random.Range(0, StatManager.single.rebirthOneLiners.Length)];
        oneLiner.text = "You are about to do Rebirth Nr: " + (GameManager.single.rebirthLevel + 1);
        if (GameManager.single.rebirthLevel + 1 == 0)
        {
            oneLiner.text = "You are new around here...";
        }
        Invoke("LateDummyIdle", .03f);
    }

    void LateDummyIdle()
    {
        dummy.IdleAnimationStart();

    }

    public void ToggleCanvas(bool b)
    {
        rebirthCanvas.SetActive(b);
    }

    public void CloseButton()
    {
        closeButton.SetActive(false);
        Router.single.loadingChestFromCamp = false;//for some reason this is getting flagged

        Router.single.CallCampFromRebirth();
    }

    bool rebirthPressed = false;
    public void RebirthButton()
    {
        if (rebirthPressed) return;
        ToggleAreYouSure(false);
        rebirthPressed = true;
        closeButton.SetActive(false);
        rebirthButton.SetActive(false);
        LevelController.Instance.ProcessRebirth();
        dummy.RebirthAnimationStart();
        Invoke("TurnOffFirstCam", 1);
        cutsceneObj.SetActive(true);
        rebirthCanvas.gameObject.SetActive(false);
        Invoke("CloseButton", 8);

    }

    public GameObject AreYouSurePanel;
    public void ToggleAreYouSure(bool b)
    {
        AreYouSurePanel.SetActive(b);
    }

    void TurnOffFirstCam()
    {
        firstCamera.SetActive(false);

    }

    public GameObject firstCamera;


}
