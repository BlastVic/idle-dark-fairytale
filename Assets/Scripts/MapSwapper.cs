using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSwapper : MonoBehaviour
{
    public GameObject mapNormal, mapCutscene;
    private void OnEnable()
    {
        if (GameManager.single.thingsSeenPermanent.Contains("Cutscene-0"))
        {
            mapNormal.SetActive(true);
            mapCutscene.SetActive(false);
        }
        else
        {
            mapCutscene.SetActive(true);
            GameManager.single.thingsSeenPermanent.Add("Cutscene-0");

        }
    }
}
