using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorGirl : MonoBehaviour
{
    private void OnEnable()
    {
        TutorialManager.single.tutorialCharacter.transform.position = transform.position;
    }
}
