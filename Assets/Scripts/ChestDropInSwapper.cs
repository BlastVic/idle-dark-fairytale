using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestDropInSwapper : MonoBehaviour
{
    public GameObject[] chests;

    private void OnEnable()
    {
        transform.localPosition = new Vector3(0, 2f, 0);

    }

    public void SetMe(ChestGrade myChestGrade)
    {

        switch (myChestGrade)
        {
            case ChestGrade.NORMAL:
                chests[0].SetActive(true);
                break;
            case ChestGrade.GOLD:
                chests[1].SetActive(true);

                break;
            case ChestGrade.DIAMOND:
                chests[2].SetActive(true);

                break;
            case ChestGrade.TURD:
                chests[3].SetActive(true);

                break;
        }

    }

    public void OpenTheChest(int amount)
    {
        AnimationSetter[] animationSetters = GetComponentsInChildren<AnimationSetter>();
        foreach (AnimationSetter animSet in animationSetters)
        {
            animSet.OpenChestNow();

        }
        if (isCherries)
        {
            //amount = 1;
            cherryParticles.SetActive(true);
            //if (amount == 2) cherryParticles2.SetActive(true); //later if we want more cherries particles
            //if (amount == 3) cherryParticles3.SetActive(true); //later if we want more cherries particles
        }
        else
        {
            crystalsParticles.SetActive(true);
            if (amount == 2) crystalsParticles2.SetActive(true);
            if (amount == 3) crystalsParticles3.SetActive(true);
        }
    }

    public bool isCherries = false;
    public GameObject crystalsParticles, crystalsParticles2, crystalsParticles3;
    public GameObject cherryParticles;

}