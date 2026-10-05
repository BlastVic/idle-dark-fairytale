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

#if UNITY_EDITOR

    public void UpdateInfo(Waveset waveSet)
    {
        this.mapFullName = waveSet.levelKey;
        this.goNormal.SetActive(!waveSet.isMiniBoss);
        this.goMiniBOSS.SetActive(waveSet.isMiniBoss);
        this.hideMap.mapPrerequisiteFullName = waveSet.precondition;
        this.hideMap.gameObject.SetActive(!string.IsNullOrEmpty(waveSet.precondition));
        Sprite sprite = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath(string.Format("Assets/Grfx/Menus/WorldMap/{0}.png",waveSet.icon), typeof(Sprite));
        this.imgNormal.sprite = sprite;
        this.imgMiniBOSS.sprite = sprite;
        if(!string.IsNullOrEmpty(waveSet.color))
        {
            string[] colors = waveSet.color.Split(',');
            this.imgMiniBOSSBG.color = new Color(float.Parse(colors[0]) / 255f, float.Parse(colors[1]) / 255f, float.Parse(colors[2]) / 255f, 1f);
        }
        for (int i = 0; i < this.listArrows.Length; i++)
        {
            this.listArrows[i].SetActive(false);
        }
        if (!string.IsNullOrEmpty(waveSet.precondition))
        {
            Waveset preWave = WaveManager.single.GetWavesetByKey(waveSet.precondition);
            string[] currentPos = waveSet.position.Split(',');
            string[] prePos = preWave.position.Split(',');
            Vector2 vecCurrent = new Vector2(float.Parse(currentPos[0]), float.Parse(currentPos[1]));
            Vector2 vecPre = new Vector2(float.Parse(prePos[0]), float.Parse(prePos[1]));
            for (int i = 0; i < this.listArrows.Length; i++)
            {
                if (vecPre.x < vecCurrent.x && vecPre.y == vecCurrent.y)
                {
                    this.listArrows[0].SetActive(true);
                }
                else if (vecPre.x > vecCurrent.x && vecPre.y == vecCurrent.y)
                {
                    this.listArrows[1].SetActive(true);
                }
                else if (vecPre.x == vecCurrent.x && vecPre.y < vecCurrent.y)
                {
                    this.listArrows[2].SetActive(true);
                }
                else if (vecPre.x == vecCurrent.x && vecPre.y > vecCurrent.y)
                {
                    this.listArrows[3].SetActive(true);
                }
                else if (vecPre.x < vecCurrent.x && vecPre.y < vecCurrent.y)
                {
                    this.listArrows[4].SetActive(true);
                }
                else if (vecPre.x > vecCurrent.x && vecPre.y < vecCurrent.y)
                {
                    this.listArrows[5].SetActive(true);
                }
            }
        }
    }

#endif

}
