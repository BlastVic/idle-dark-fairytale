using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services;
using IdleKnightHero.UI;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    public Button myButton;
    public string mapFullName;//this causes a lookup to GM
    //[Header("What map needs to be beaten first")]
    //public string mapPrerequisiteFulName;
    public Text waves;
    public Text mapTitle;
    public GameObject[] linesLeadingToMe;

    public GameObject goNormal;
    public GameObject goMiniBOSS;
    public Image imgNormal;
    public Image imgMiniBOSS;
    public Image imgMiniBOSSBG;
    public GameObject[] listArrows;
    public HideIfNotMap hideMap;

    private void OnEnable()
    {
        myButton = GetComponentInChildren<Button>();
        SetMe();


    }

    private void OnDisable()
    {


    }

    public void Clicked()
    {
        if (MapPackageUI.single.isDragging) return;
        StartCoroutine(ClickedChain());
    }

    public IEnumerator ClickedChain()
    {
        SoundManager.Instance.PlayClip("CLICK");

        GameManager.single.lastBattleKey = mapFullName;
        TransitionCanvas.single.ToBlack("Map Node");
        yield return new WaitForSeconds(.8f);

        LevelController.Instance.AssetManager.mapPackageObj.SetActive(false);
        GameplayCanvas.single.ToggleButtonGrid(false);
        GameplayCanvas.single.ToggleFusionBarUI(false);
        WaveManager.single.CallStartLevel(mapFullName);
    }

    void SetMe()
    {

        Waveset myWaveset = WaveManager.single.GetWavesetByKey(mapFullName);
        int highest = WaveManager.single.GetWaveseReachedByKey(mapFullName);
        //if waves beat, turn it green
        if (highest == myWaveset.waves.Length)
        {
            //beaten
            waves.color = LevelController.Instance.AssetManager._colorsDb[1];
        }
        else
        {
            waves.color = Color.white;
        }

        //if waves not beaten turn it to color 0 white

        waves.text = highest + "/" + myWaveset.waves.Length;
        mapTitle.text = myWaveset.levelName;

    }

    // Legacy map authoring was retired with the old map prefab and artwork.


}
