using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroScript : MonoBehaviour
{
    public GameObject parentObj;
    public GameObject[] objectsToTurnOnAfter;

    private void Start()
    {
        GameplayCanvas.single.gameObject.SetActive(false);
    }

    public void AnimationCompleted()
    {
        foreach (GameObject obj in objectsToTurnOnAfter)
        {
            obj.SetActive(true);
        }
        parentObj.SetActive(false);
        GameplayCanvas.single.gameObject.SetActive(true);
        TutorialManager.single.PlayTutorial(0);
        GameplayCanvas.single.TryShowDailyCherries(GameManager.single.dailyCherriesAmount);
    }

    public void AnimationNearlyComplete()
    {
        if (LevelController.Instance.AssetManager.campPackageObj == null)
            GameManager.single.StartCoroutine("WaitForCamp");


    }



}
