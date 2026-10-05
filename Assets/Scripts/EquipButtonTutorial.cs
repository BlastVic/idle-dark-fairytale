using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipButtonTutorial : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameManager.single.HasSeenThis("Tutorial-4") && InventoryManager.single.hasWeapon)
        {
            TutorialManager.single.PlayTutorial(5);
        }
        else
        {
            TutorialManager.single.PlayTutorial(4);

        }

    }
}
