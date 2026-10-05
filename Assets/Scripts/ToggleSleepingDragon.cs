using System.Collections;
using System.Collections.Generic;
using Scripts.Skills;
using UnityEngine;

public class ToggleSleepingDragon : MonoBehaviour
{
    public GameObject dragon;
    // Start is called before the first frame update
    void OnEnable()
    {
        if (SkillsController.Instance.GetSkillLevel(SkillsTypeEnum.Tairon_The_Dragon) > 0)
        {
            dragon.SetActive(true);
        }
        else
        {
            dragon.SetActive(false);
        }

    }


}
