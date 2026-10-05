using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HideIfNotMap : MonoBehaviour
{
    public bool useSpriteRenderer = false;
    public bool useImage;
    public bool useCanvasCG = true;
    public bool alwaysUnlocked = false;
    public string mapPrerequisiteFullName;
    public bool permaLocked = false;

    CanvasGroup cg;
    private void OnEnable()
    {
        if (permaLocked) return;
        if (useCanvasCG && cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        if (alwaysUnlocked) return;//dont bother turning it off
        int highest = WaveManager.single.GetWaveseReachedByKey(mapPrerequisiteFullName);
        int max = WaveManager.single.GetWavesetByKey(mapPrerequisiteFullName).waves.Length;
        //Debug.Log("Map:" + mapPrerequisiteFullName + " " + highest + " / " + max);
        if (highest != max)
        {
            if (useCanvasCG)
                cg.alpha = 1;
            if (useSpriteRenderer)
                GetComponent<SpriteRenderer>().enabled = true;
            if (useImage)
                GetComponent<Image>().enabled = true;
        }
        else
        {
            if (useCanvasCG)
                cg.alpha = 0;
            if (useSpriteRenderer)
                GetComponent<SpriteRenderer>().enabled = false;
            if (useImage)
                GetComponent<Image>().enabled = false;
        }


        EventManager.OnUnlockMaps += HandleUnlockMaps;
    }

    private void OnDisable()
    {
        EventManager.OnUnlockMaps -= HandleUnlockMaps;
    }

    void HandleUnlockMaps()
    {
        if (useCanvasCG)
            cg.alpha = 0;
        if (useSpriteRenderer)
            GetComponent<SpriteRenderer>().enabled = false;
        if (useImage)
            GetComponent<Image>().enabled = false;
    }
}
