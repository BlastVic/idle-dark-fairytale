using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneEvents : MonoBehaviour
{
    private void OnEnable()
    {
        Invoke("CutsceneOver", 5.5f);
    }
    public void CutsceneOver()
    {
        WaveManager.single.CallNextWave();
        Camera.main.enabled = false;
        LevelController.Instance.AssetManager.battleCampCamera.SetActive(true);
        LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>().enabled = true;
    }
}
