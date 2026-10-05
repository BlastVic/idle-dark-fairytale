using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    Router r;
    private void OnEnable()
    {
        r = Router.single;
        GetComponent<Canvas>().worldCamera = LevelController.Instance.AssetManager.menuCamera.GetComponent<Camera>();
    }
    public void CloseButton()
    {
        r.StartCoroutine(r.GoToCampFromMenu());
    }

}
