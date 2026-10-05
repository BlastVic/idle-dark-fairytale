using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRequiredObject : MonoBehaviour
{
    public bool alwaysUnlocked = false;
    public string mapPrerequisiteFullName;
    CanvasGroup cg;
    private void OnEnable()
    {
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        if (alwaysUnlocked) return;//dont bother turning it off
        int highest = WaveManager.single.GetWaveseReachedByKey(mapPrerequisiteFullName);
        int max = WaveManager.single.GetWavesetByKey(mapPrerequisiteFullName).waves.Length ;
        if (highest != max)
        {
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        else
        {
            cg.alpha = 1;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        EventManager.OnUnlockMaps += HandleUnlockMaps;
    }

    private void OnDisable()
    {
        EventManager.OnUnlockMaps -= HandleUnlockMaps;
    }

    void HandleUnlockMaps()
    {
        cg.alpha = 1;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }
}
