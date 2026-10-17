using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RebirthButton : MonoBehaviour
{
    private void OnEnable()
    {
        Refresh();
        EventManager.OnUIRefresh += HandleUIRefresh;
    }

    private void OnDisable()
    {
        EventManager.OnUIRefresh -= HandleUIRefresh;
    }

    void HandleUIRefresh(string key)
    {
        if (key == "RebirthButton") Refresh();
    }

    void Refresh()
    {
        //check if should show, hard coded to true for now
        bool shouldShow = LevelController.Instance.IsNextRebirthReady();
        foreach (Transform t in transform)
        {
            t.gameObject.SetActive(shouldShow);//turn them all off
        }
    }

    public void RebirthPressed()
    {
        Debug.Log("RebirthPress");
        Router.single.CallRebirthFromCamp();
    }
}
